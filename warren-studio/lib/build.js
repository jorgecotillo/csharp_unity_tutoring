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
// CRITICAL: this must match ONLY a headless -batchmode WebGL build of THIS
// game (mvp_v1) — NOT an interactive Unity Editor the user may have open on
// another project. A plain tasklist can't tell them apart (both are Unity.exe),
// so we query the process COMMAND LINE via CIM and require '-batchmode' + the
// 'mvp_v1' project. Async (spawned PowerShell) so it never blocks the loop.
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
      "Where-Object { $_.CommandLine -and $_.CommandLine -match '-batchmode' -and $_.CommandLine -match 'mvp_v1' }; " +
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
  startedAt: null, // ISO string of the current build's start
  lastResult: null, // 'success' | 'failed' | 'error' | 'timeout'
  lastFinishedAt: null,
  lastReason: null,
};

let child = null;
let timeoutHandle = null;
let adoptedInterval = null; // poller when we're watching an external/orphaned build

// Snapshot of build state for the /api/game/status endpoint (drives the UI).
// Pure + synchronous — never shells out — so it's safe to call on every poll.
// state.building is kept accurate by the builds we start AND by the startup
// adopt-check that detects an orphaned build after a restart.
function status() {
  return {
    enabled: autoBuildEnabled(),
    building: state.building,
    queued: state.queued,
    startedAt: state.startedAt,
    lastResult: state.lastResult,
    lastFinishedAt: state.lastFinishedAt,
  };
}

// Public entry point. Ask for a rebuild; coalesces while one is running.
// Returns { ok, queued } or { ok:false, reason }.
function requestBuild(reason) {
  if (!autoBuildEnabled()) return { ok: false, reason: 'disabled' };
  if (state.building) {
    state.queued = true;
    console.log('[build] busy — coalesced another rebuild request', reason ? `(${reason})` : '');
    return { ok: true, queued: true };
  }
  startBuild(reason);
  return { ok: true, queued: false };
}

function startBuild(reason) {
  state.building = true;
  state.queued = false;
  state.startedAt = new Date().toISOString();
  state.lastReason = reason || null;

  console.log('[build] starting Unity rebuild', reason ? `(${reason})` : '');

  // Write the build-start header ourselves (the parent), then let the DETACHED
  // child append its own output. IMPORTANT (Windows): we do NOT pass inherited
  // file descriptors to a detached child — that combination silently fails here
  // (the child never runs). Instead the child is `cmd.exe` doing its OWN `>>`
  // redirection with stdio:'ignore', which is the reliable Windows pattern for a
  // fire-and-forget background process that (a) survives a parent/server restart
  // and (b) actually logs. We still get the child's 'close' event for completion,
  // and taskkill /T reaches the whole tree (cmd → powershell → Unity) on timeout.
  try {
    fs.appendFileSync(BUILD_LOG, `\n\n===== build start ${state.startedAt} ${reason || ''} =====\n`);
  } catch (_) { /* logging is best-effort */ }

  // Build the inner command line. PowerShell runs the refresh script and merges
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
