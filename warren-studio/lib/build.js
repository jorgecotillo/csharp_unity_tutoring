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

// Snapshot of build state for the /api/game/status endpoint (drives the UI).
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
      stdio: ['ignore', outFd, outFd],
    });
  } catch (err) {
    if (typeof outFd === 'number') { try { fs.closeSync(outFd); } catch (_) {} }
    finishBuild('error', err && err.message);
    return;
  }

  // Close our copy of the fd in the parent; the child keeps its own.
  if (typeof outFd === 'number') { try { fs.closeSync(outFd); } catch (_) {} }

  timeoutHandle = setTimeout(() => {
    console.error('[build] timeout — killing the stuck build');
    try { child && child.kill(); } catch (_) {}
    finishBuild('timeout', `exceeded ${Math.round(BUILD_TIMEOUT_MS / 60000)}m`);
  }, BUILD_TIMEOUT_MS);

  child.on('error', (err) => finishBuild('error', err && err.message));
  child.on('close', (code) => {
    // If the timeout already fired, finishBuild is a no-op guard below.
    finishBuild(code === 0 ? 'success' : 'failed', code === 0 ? null : `exit ${code}`);
  });
}

function finishBuild(result, errMsg) {
  if (!state.building) return; // guard against double-fire (close + timeout)
  if (timeoutHandle) { clearTimeout(timeoutHandle); timeoutHandle = null; }
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

module.exports = { requestBuild, status, autoBuildEnabled };
