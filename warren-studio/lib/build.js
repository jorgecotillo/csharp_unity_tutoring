'use strict';

// Auto-rebuild engine: when a chat message changes the game's code, kick off a
// real Unity WebGL rebuild in the BACKGROUND so Warren's live preview updates on
// its own — without ever blocking the chat or any other request.
//
// Design goals (exactly what Jorge asked for):
//   * NON-BLOCKING: the build is a detached child process (refresh-preview.ps1).
//     Express keeps serving chat/spec/code/status the entire time.
//   * SINGLE-FLIGHT: only ONE Unity build runs at a time. Two headless builds on
//     the same project would fight over Unity's Library lock, so we never start a
//     second concurrent build.
//   * COALESCING QUEUE: if a new code change lands while a build is running, we
//     don't queue up N builds — we just remember "something else changed" and run
//     exactly ONE more build after the current one finishes. So rapid-fire edits
//     collapse into at most one pending rebuild.
//
// The build itself reuses the existing, battle-tested refresh-preview.ps1
// (Unity build + cold-Library retry + publish to docs/game). We pass -NoPush so
// it only updates the LOCAL preview (docs/game + build-info.json) and commits
// locally — nothing is pushed to main. The portal already serves docs/game and
// polls build-info.json, so the preview reloads on its own when the build lands.

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const repo = require('./repo');

const IS_PROD = process.env.NODE_ENV === 'production';

// Path to the Unity editor + the rebuild script. Both must exist for auto-build
// to be possible (they only exist on Jorge's local machine, never in the Azure
// container — which is exactly why auto-build is a local-only feature).
const UNITY_EXE =
  process.env.UNITY_EXE ||
  'C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.15f1\\Editor\\Unity.exe';
const REFRESH_SCRIPT = path.join(repo.REPO_ROOT, 'refresh-preview.ps1');
const PUBLISH_SCRIPT = path.join(repo.REPO_ROOT, 'publish-game.ps1');

// Persistent build-daemon protocol files (see mvp_v1/Assets/Editor/BuildDaemon.cs
// and start-build-daemon.ps1). When a daemon is alive we ask it to build instead
// of launching a fresh Unity — skipping the ~1-2 min editor startup each time.
const DAEMON_DIR = path.join(repo.REPO_ROOT, 'mvp_v1', 'Builds', 'daemon');
const DAEMON_HEARTBEAT = path.join(DAEMON_DIR, 'heartbeat.txt');
const DAEMON_REQUEST = path.join(DAEMON_DIR, 'request.txt');
const DAEMON_RESULT = path.join(DAEMON_DIR, 'result.txt');
// A heartbeat newer than this = daemon considered alive.
const DAEMON_FRESH_MS = 12000;

// Hard ceiling so a wedged Unity process (e.g. the editor is open on this
// worktree and holds the project lock) can't leave us "building" forever.
const BUILD_TIMEOUT_MS = parseInt(
  process.env.BUILD_TIMEOUT_MS || String(25 * 60 * 1000),
  10
);

// Where the background build's stdout/stderr is teed for debugging.
const BUILD_LOG = path.join(__dirname, '..', 'build.log');

// Is auto-build turned on? Explicit env wins; otherwise auto-enable locally when
// the toolchain is actually present.
function autoBuildEnabled() {
  const flag = String(process.env.AUTO_BUILD_ENABLED || '').trim().toLowerCase();
  if (['off', '0', 'false', 'no'].includes(flag)) return false;
  if (['on', '1', 'true', 'yes'].includes(flag)) return true;
  // "auto": on when we're local AND the Unity toolchain + script are present.
  return !IS_PROD && fileExists(UNITY_EXE) && fileExists(REFRESH_SCRIPT);
}

function fileExists(p) {
  try { return fs.existsSync(p); } catch (_) { return false; }
}

// Is a Unity build already running? Guards against launching a SECOND concurrent
// Unity build (they'd fight over the project's Library lock) — e.g. if the node
// server was restarted mid-build, or someone ran refresh-preview.ps1 by hand.
// ASYNC (spawn, never execSync) so it NEVER blocks the event loop, even when
// tasklist is slow under a build's CPU/disk load. Only ever called off the
// request path (startup + the adopt poller), never from /api/game/status.
//
// CRITICAL: this must match ONLY a headless one-shot -batchmode WebGL build of
// THIS game (mvp_v1) — NOT an interactive Unity Editor on another project, and
// NOT the persistent build daemon (which is also batchmode+mvp_v1 but must never
// be mistaken for an orphaned one-shot build). A one-shot build always passes
// '-executeMethod'; the daemon never does. So we require '-batchmode' + 'mvp_v1'
// + '-executeMethod'. Async (spawned PowerShell) so it never blocks the loop.
function checkUnityRunning() {
  return new Promise((resolve) => {
    let out = '';
    let done = false;
    let child;
    const finish = (val) => {
      if (done) return;
      done = true;
      try { child && child.kill(); } catch (_) {}
      resolve(val);
    };
    const ps =
      "$p = Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" -ErrorAction SilentlyContinue | " +
      "Where-Object { $_.CommandLine -and $_.CommandLine -match '-batchmode' -and $_.CommandLine -match 'mvp_v1' -and $_.CommandLine -match '-executeMethod' }; " +
      "if ($p) { 'FOUND' } else { 'NONE' }";
    try {
      child = spawn('powershell', ['-NoProfile', '-NonInteractive', '-Command', ps], { windowsHide: true });
    } catch (_) {
      return resolve(false);
    }
    if (child.stdout) child.stdout.on('data', (d) => { out += d.toString(); });
    child.on('error', () => finish(false));
    child.on('close', () => finish(/FOUND/.test(out)));
    setTimeout(() => finish(false), 15000); // async — safe, never blocks the loop
  });
}

const state = {
  building: false,
  queued: false, // a rebuild was requested while one was already running
  pending: false, // a rebuild is scheduled (debounce window) but not started yet
  pendingSince: null, // ISO time the debounce window opened (for the UI timer)
  startedAt: null, // ISO string of the current build's start
  lastResult: null, // 'success' | 'failed' | 'error' | 'timeout'
  lastFinishedAt: null,
  lastReason: null,
};

// Debounce window: after a chat edit asks for a build, wait briefly for MORE
// edits before actually starting, so a burst of quick edits collapses into ONE
// build. Kept SHORT (default 3s) because chat replies are minutes apart, so the
// in-flight COALESCING below is what handles the common case; this only catches a
// genuine rapid double-send. Tunable via env; set to 0 to start immediately.
const DEBOUNCE_MS = Math.max(0, parseInt(process.env.AUTO_BUILD_DEBOUNCE_MS || '3000', 10) || 0);

let child = null;
let timeoutHandle = null;
let adoptedInterval = null; // poller when we're watching an external/orphaned build
let debounceTimer = null;
let pendingReason = null;

// Snapshot of build state for the /api/game/status endpoint (drives the UI).
// Pure + synchronous — never shells out — so it's safe to call on every poll.
// "building" is true for BOTH a scheduled (debounced) build and a running one,
// so the UI shows the rebuild animation continuously from the moment it's asked.
function status() {
  const building = state.building || state.pending;
  return {
    enabled: autoBuildEnabled(),
    building,
    queued: state.queued,
    startedAt: state.startedAt || state.pendingSince,
    lastResult: state.lastResult,
    lastFinishedAt: state.lastFinishedAt,
  };
}

// Public entry point. Ask for a rebuild. Two layers of collapsing:
//   * DEBOUNCE: quick successive requests (before a build starts) reset a short
//     timer, so a burst becomes ONE build.
//   * COALESCE: requests that arrive while a build is RUNNING set a single
//     "queued" flag, draining to exactly one more build when the current ends.
// Returns { ok, queued } or { ok:false, reason }.
function requestBuild(reason) {
  if (!autoBuildEnabled()) return { ok: false, reason: 'disabled' };

  // A build is already running → coalesce into exactly one follow-up rebuild.
  if (state.building) {
    state.queued = true;
    console.log('[build] busy — coalesced another rebuild request', reason ? `(${reason})` : '');
    return { ok: true, queued: true };
  }

  // Not building yet → (re)start the debounce window so rapid edits collapse.
  pendingReason = reason || pendingReason;
  if (DEBOUNCE_MS > 0) {
    const alreadyPending = state.pending;
    if (debounceTimer) clearTimeout(debounceTimer);
    if (!state.pending) {
      state.pending = true;
      state.pendingSince = new Date().toISOString();
      console.log('[build] scheduled in', DEBOUNCE_MS + 'ms', reason ? `(${reason})` : '');
    } else {
      console.log('[build] debounced — extending the window', reason ? `(${reason})` : '');
    }
    debounceTimer = setTimeout(() => {
      debounceTimer = null;
      state.pending = false;
      state.pendingSince = null;
      const r = pendingReason;
      pendingReason = null;
      startBuild(r);
    }, DEBOUNCE_MS);
    return { ok: true, queued: alreadyPending };
  }

  startBuild(reason);
  return { ok: true, queued: false };
}

function startBuild(reason) {
  state.building = true;
  state.queued = false;
  state.startedAt = new Date().toISOString();
  state.lastReason = reason || null;

  // Header first so the log always shows a build was attempted.
  try {
    fs.appendFileSync(BUILD_LOG, `\n\n===== build start ${state.startedAt} ${reason || ''} =====\n`);
  } catch (_) { /* logging is best-effort */ }

  // Prefer the persistent daemon when it's alive (fast: no editor startup). Fall
  // back to a one-shot cold build otherwise. The check is async so it never
  // blocks the event loop; on any error we default to the reliable cold path.
  checkDaemonAlive().then((alive) => {
    if (!state.building) return; // finished/aborted while we were checking
    if (alive) {
      console.log('[build] using persistent daemon', reason ? `(${reason})` : '');
      daemonBuild(reason);
    } else {
      coldBuild(reason);
    }
  }).catch(() => { if (state.building) coldBuild(reason); });
}

// One-shot build: launch a fresh headless Unity via refresh-preview.ps1. This is
// the reliable fallback and the original behavior.
function coldBuild(reason) {
  console.log('[build] starting one-shot Unity rebuild', reason ? `(${reason})` : '');
  // A DETACHED cmd.exe runs PowerShell, which runs refresh-preview.ps1 and merges
  // ALL its streams (*>&1 — including Write-Host, which its "Say" helper uses and
  // which a plain `>>` would MISS) then appends them to the log as UTF-8. cmd is
  // only the detach wrapper. `exit $LASTEXITCODE` propagates the script's real
  // result so our 'close' handler sees success/failure correctly. Paths are
  // single-quoted for PowerShell (handles spaces); the whole -Command value is
  // passed verbatim (windowsVerbatimArguments) so cmd doesn't mangle it.
  const psCmd =
    `powershell -ExecutionPolicy Bypass -NoProfile -Command ` +
    `"& '${REFRESH_SCRIPT}' -NoPush *>&1 | Out-File -FilePath '${BUILD_LOG}' -Append -Encoding utf8; exit $LASTEXITCODE"`;

  try {
    child = spawn(process.env.ComSpec || 'cmd.exe', ['/d', '/s', '/c', psCmd], {
      cwd: repo.REPO_ROOT,
      windowsHide: true,
      detached: true,       // own process group → survives a server restart
      stdio: 'ignore',      // no inherited fds → reliable when detached on Windows
      // Pass the command line VERBATIM. Without this, Node re-escapes the quotes
      // and >> / 2>&1 redirect operators, which silently mangles the cmd line so
      // the child produces no output and never runs the build (the exact bug the
      // earlier detached+fd version hit). Verified required on this machine.
      windowsVerbatimArguments: true,
    });
  } catch (err) {
    finishBuild('error', err && err.message);
    return;
  }
  // Fully independent — don't keep the Node event loop alive for it.
  try { child.unref(); } catch (_) {}

  // A kill from our timeout must take the whole detached process tree with it.
  const childPid = child.pid;

  timeoutHandle = setTimeout(() => {
    console.error('[build] timeout — killing the stuck build tree');
    try {
      if (childPid) spawn('taskkill', ['/PID', String(childPid), '/T', '/F'], { windowsHide: true });
      else if (child) child.kill();
    } catch (_) {
      try { child && child.kill(); } catch (_) {}
    }
    finishBuild('timeout', `exceeded ${Math.round(BUILD_TIMEOUT_MS / 60000)}m`);
  }, BUILD_TIMEOUT_MS);

  child.on('error', (err) => finishBuild('error', err && err.message));
  child.on('close', (code) => {
    // If the timeout already fired, finishBuild is a no-op guard below.
    finishBuild(code === 0 ? 'success' : 'failed', code === 0 ? null : `exit ${code}`);
  });
}

// Is the persistent build daemon alive? True when its heartbeat file was touched
// within the freshness window. Async (fs.stat) so it never blocks the loop.
// NOTE: only reliable when the daemon is IDLE — during a code change it recompiles
// and domain-reloads, which PAUSES the heartbeat for many seconds. So this is used
// for the initial "is a daemon available?" decision, NOT for mid-build liveness
// (which uses the process PID via daemonProcessAlive).
function checkDaemonAlive() {
  return new Promise((resolve) => {
    fs.stat(DAEMON_HEARTBEAT, (err, st) => {
      if (err || !st) return resolve(false);
      resolve((Date.now() - st.mtimeMs) < DAEMON_FRESH_MS);
    });
  });
}

// Read the daemon's Unity PID (written by start-build-daemon.ps1). Used for a
// RELIABLE mid-build liveness check: a reloading daemon pauses its heartbeat but
// its process stays alive, so only a dead PROCESS means "daemon crashed".
function readDaemonPid() {
  try {
    const n = parseInt(fs.readFileSync(path.join(DAEMON_DIR, 'daemon.pid'), 'utf8').trim(), 10);
    return Number.isFinite(n) && n > 0 ? n : null;
  } catch (_) { return null; }
}

// Is a process alive? `process.kill(pid, 0)` sends no signal; it just checks
// existence. ESRCH = gone; EPERM = exists but not ours (still alive).
function pidAlive(pid) {
  if (!pid) return false;
  try { process.kill(pid, 0); return true; }
  catch (e) { return !!(e && e.code === 'EPERM'); }
}

// Fast path: ask the resident daemon to build, then publish. Polls the daemon's
// result file. Robust failure handling:
//   * SUCCESS  → publish the export → finish 'success'
//   * FAILED   → finish 'failed' (a cold retry would just fail the same way)
//   * daemon dies mid-build (heartbeat goes stale) → cold-build fallback (the
//     project lock is now free, so a fresh Unity can run)
//   * overall timeout → finish 'timeout'
function daemonBuild(reason) {
  const id = Date.now() + '-' + Math.random().toString(36).slice(2, 8);
  try { if (fs.existsSync(DAEMON_RESULT)) fs.unlinkSync(DAEMON_RESULT); } catch (_) {}
  // Log write is best-effort: a momentarily-locked build.log (EBUSY on Windows,
  // e.g. a publish still flushing it) must NEVER derail the daemon path. Only a
  // failure to write the actual REQUEST file is fatal (then cold-fall-back).
  try { fs.appendFileSync(BUILD_LOG, `[daemon] requesting build id=${id} ${reason || ''}\n`); } catch (_) {}
  try {
    fs.writeFileSync(DAEMON_REQUEST, id);
  } catch (err) {
    console.error('[build] daemon request write failed → cold fallback', err && err.message);
    return coldBuild(reason);
  }

  const startedAt = Date.now();
  const daemonPid = readDaemonPid();
  let deadChecks = 0;

  timeoutHandle = setTimeout(() => {
    finishBuild('timeout', 'daemon build exceeded limit');
  }, BUILD_TIMEOUT_MS);

  if (adoptedInterval) clearInterval(adoptedInterval);
  adoptedInterval = setInterval(() => {
    if (!state.building) return;

    // 1) Result ready for OUR id?
    let res = null;
    try { if (fs.existsSync(DAEMON_RESULT)) res = fs.readFileSync(DAEMON_RESULT, 'utf8'); } catch (_) {}
    if (res) {
      const parts = res.split('|');
      const gotId = (parts[0] || '').trim();
      if (gotId === id) {
        const status = (parts[1] || '').trim();
        const msg = parts.slice(2).join('|').trim();
        try { fs.unlinkSync(DAEMON_RESULT); } catch (_) {}
        if (adoptedInterval) { clearInterval(adoptedInterval); adoptedInterval = null; }
        if (timeoutHandle) { clearTimeout(timeoutHandle); timeoutHandle = null; }
        if (status === 'SUCCESS') {
          try { fs.appendFileSync(BUILD_LOG, `[daemon] ${msg}\n[daemon] publishing…\n`); } catch (_) {}
          runPublish((ok) => finishBuild(ok ? 'success' : 'failed', ok ? null : 'publish failed'));
        } else {
          try { fs.appendFileSync(BUILD_LOG, `[daemon] FAILED: ${msg}\n`); } catch (_) {}
          finishBuild('failed', msg || 'daemon build failed');
        }
        return;
      }
    }

    // 2) Liveness. CRITICAL: a code change makes the daemon recompile + domain
    // reload, which PAUSES its heartbeat for many seconds — so heartbeat staleness
    // does NOT mean death. We check the PROCESS instead: only a dead daemon
    // process means we can safely cold-fall-back (the project lock is now free).
    // Two consecutive dead reads (poll is every 3s) confirm it before falling back.
    if (Date.now() - startedAt > 8000 && daemonPid) {
      if (pidAlive(daemonPid)) { deadChecks = 0; return; }
      deadChecks++;
      if (deadChecks >= 2) {
        console.error('[build] daemon process exited mid-build → cold fallback');
        try { fs.appendFileSync(BUILD_LOG, `[daemon] process exited → cold fallback\n`); } catch (_) {}
        if (adoptedInterval) { clearInterval(adoptedInterval); adoptedInterval = null; }
        if (timeoutHandle) { clearTimeout(timeoutHandle); timeoutHandle = null; }
        coldBuild(reason);
      }
    }
  }, 3000);
}

// Run publish-game.ps1 (copy the fresh export into docs/game, stamp, commit) as a
// detached, self-logging child — same reliable Windows pattern as coldBuild. Calls
// cb(true) on exit 0. Detached so a server restart mid-publish can't lose it, but
// we still observe 'close' while the server lives.
function runPublish(cb) {
  const psCmd =
    `powershell -ExecutionPolicy Bypass -NoProfile -Command ` +
    `"& '${PUBLISH_SCRIPT}' -NoPush *>&1 | Out-File -FilePath '${BUILD_LOG}' -Append -Encoding utf8; exit $LASTEXITCODE"`;
  let done = false;
  const finish2 = (ok) => { if (done) return; done = true; cb(!!ok); };
  let pub;
  try {
    pub = spawn(process.env.ComSpec || 'cmd.exe', ['/d', '/s', '/c', psCmd], {
      cwd: repo.REPO_ROOT, windowsHide: true, detached: true, stdio: 'ignore',
      windowsVerbatimArguments: true,
    });
  } catch (e) { return finish2(false); }
  try { pub.unref(); } catch (_) {}
  const to = setTimeout(() => finish2(false), 4 * 60 * 1000);
  pub.on('error', () => { clearTimeout(to); finish2(false); });
  pub.on('close', (code) => { clearTimeout(to); finish2(code === 0); });
}

// Watch an already-running (external/orphaned) Unity build and finish — draining
// any queued rebuild — once it exits. Used after a server restart mid-build.
// The poll is async (checkUnityRunning) so it never blocks the event loop.
function adoptRunningBuild() {
  if (timeoutHandle) { clearTimeout(timeoutHandle); }
  timeoutHandle = setTimeout(() => {
    console.error('[build] adopted build timed out');
    finishBuild('timeout', 'adopted build exceeded limit');
  }, BUILD_TIMEOUT_MS);

  if (adoptedInterval) clearInterval(adoptedInterval);
  adoptedInterval = setInterval(() => {
    checkUnityRunning().then((running) => {
      if (!running) finishBuild('success', 'external build finished');
    });
  }, 15000);
}

function finishBuild(result, errMsg) {
  if (!state.building) return; // guard against double-fire (close + timeout)
  if (timeoutHandle) { clearTimeout(timeoutHandle); timeoutHandle = null; }
  if (adoptedInterval) { clearInterval(adoptedInterval); adoptedInterval = null; }
  child = null;
  state.building = false;
  state.startedAt = null;
  state.lastResult = result;
  state.lastFinishedAt = new Date().toISOString();
  console.log('[build] finished:', result, errMsg ? `(${errMsg})` : '');

  // A change landed mid-build → run exactly one more, then stop.
  if (state.queued) {
    console.log('[build] draining queued rebuild');
    startBuild('coalesced');
  }
}

// On startup, detect a Unity build that's already running (e.g. the server was
// restarted mid-build) and adopt it, so the UI keeps showing "rebuilding" and we
// never launch a second concurrent build. Retries a few times because tasklist
// can be briefly slow under a running build's load.
function startupAdoptCheck(attempt) {
  attempt = attempt || 1;
  if (!autoBuildEnabled() || state.building) return;
  checkUnityRunning().then((running) => {
    if (state.building) return;
    if (running) {
      console.log('[build] startup: found a running Unity build — adopting it');
      state.building = true;
      state.startedAt = new Date().toISOString();
      adoptRunningBuild();
    } else if (attempt < 4) {
      setTimeout(() => startupAdoptCheck(attempt + 1), 5000);
    }
  });
}
setTimeout(() => startupAdoptCheck(1), 2500);

module.exports = { requestBuild, status, autoBuildEnabled };
