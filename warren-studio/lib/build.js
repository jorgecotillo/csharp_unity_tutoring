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

  let outFd = 'ignore';
  try {
    outFd = fs.openSync(BUILD_LOG, 'a');
    fs.writeSync(outFd, `\n\n===== build start ${state.startedAt} ${reason || ''} =====\n`);
  } catch (_) { outFd = 'ignore'; }

  const args = ['-ExecutionPolicy', 'Bypass', '-NoProfile', '-File', REFRESH_SCRIPT, '-NoPush'];
  try {
    child = spawn('powershell', args, {
      cwd: repo.REPO_ROOT,
      windowsHide: true,
      // detached: run in its OWN process group so a server restart (manual or the
      // watchdog self-healing) can't kill the build mid-flight and lose the
      // publish step. If this server dies, the build finishes + publishes on its
      // own, and the next server instance adopts it via startupAdoptCheck.
      detached: true,
      stdio: ['ignore', outFd, outFd],
    });
  } catch (err) {
    if (typeof outFd === 'number') { try { fs.closeSync(outFd); } catch (_) {} }
    finishBuild('error', err && err.message);
    return;
  }
  // Don't let the running build keep the Node event loop alive; it's fully
  // independent now.
  try { child.unref(); } catch (_) {}

  // Close our copy of the fd in the parent; the child keeps its own.
  if (typeof outFd === 'number') { try { fs.closeSync(outFd); } catch (_) {} }

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
