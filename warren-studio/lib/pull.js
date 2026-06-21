'use strict';

// Phase 4 see-the-change loop: a background poller that brings CI-built game
// files back DOWN into the running container.
//
// Why this exists:
//   Warren chats -> git.js pushes mvp_v1 edits to main -> GitHub Actions
//   (webgl.yml) compiles the Unity WebGL build and commits it into docs/game/**
//   on main. That fresh build lives on the REMOTE, not on this container's disk.
//   This poller fast-forwards the local checkout to origin/main so server.js can
//   serve the new docs/game/ files and the iframe reloads.
//
// Safety model:
//   * Read-only against the remote: fetch + `merge --ff-only`. It NEVER creates
//     a merge commit and NEVER pushes. If the local branch has diverged (can't
//     fast-forward), it logs and skips rather than fighting git.js.
//   * Gated by GIT_PULL_ENABLED (default OFF). Local dev stays still; Azure ON.
//   * Skips any tick while git.js holds the lock (mid commit+push) so the two
//     never race on the working tree.

const { execFileSync } = require('child_process');
const repo = require('./repo');
const gitlock = require('./gitlock');

const PULL_ENABLED = /^(1|true|yes|on)$/i.test(
  String(process.env.GIT_PULL_ENABLED || '')
);
const REMOTE = process.env.GIT_REMOTE || 'origin';
const TARGET_BRANCH = process.env.GIT_TARGET_BRANCH || 'main';

// Poll cadence. Default 30s; clamp to a sane floor so a typo can't hammer git.
const INTERVAL_SEC = Math.max(
  10,
  parseInt(process.env.GAME_PULL_INTERVAL_SEC || '30', 10) || 30
);

function git(args) {
  return execFileSync('git', args, {
    cwd: repo.REPO_ROOT,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
}

// Best-effort rev-parse that returns '' instead of throwing.
function revParse(ref) {
  try {
    return git(['rev-parse', ref]).trim();
  } catch (_) {
    return '';
  }
}

let timer = null;
let lastSeen = '';

function tick() {
  // Don't touch the working tree while git.js is committing/pushing.
  if (gitlock.isBusy()) return;

  try {
    git(['fetch', '--quiet', REMOTE, TARGET_BRANCH]);
  } catch (e) {
    console.warn('[pull] fetch failed:', (e && e.message) || e);
    return;
  }

  const remoteSha = revParse(`${REMOTE}/${TARGET_BRANCH}`);
  if (!remoteSha || remoteSha === lastSeen) return; // nothing new

  // Re-check the lock right before mutating — git.js may have grabbed it
  // between the fetch and now.
  if (gitlock.isBusy()) return;

  try {
    git(['merge', '--ff-only', `${REMOTE}/${TARGET_BRANCH}`]);
    lastSeen = remoteSha;
    console.log(`[pull] fast-forwarded ${TARGET_BRANCH} to ${remoteSha.slice(0, 7)}`);
  } catch (e) {
    // Local diverged from the remote (e.g. an un-pushed local commit). Record
    // the remote SHA so we don't spam this every tick, and let git.js's own
    // rebase-retry reconcile on the next push.
    lastSeen = remoteSha;
    console.warn(
      '[pull] cannot fast-forward (local diverged); skipping:',
      (e && e.message) || e
    );
  }
}

/**
 * Start the background game-build poller. No-op (with a log line) unless
 * GIT_PULL_ENABLED is truthy. Safe to call more than once — only one timer runs.
 */
function startGamePuller() {
  if (!PULL_ENABLED) {
    console.log('[pull] disabled (set GIT_PULL_ENABLED=on to enable)');
    return null;
  }
  if (timer) return timer;

  // Seed last-seen with the current HEAD so we only act on genuinely new builds.
  lastSeen = revParse('HEAD');
  console.log(
    `[pull] watching ${REMOTE}/${TARGET_BRANCH} every ${INTERVAL_SEC}s ` +
      `(from ${lastSeen ? lastSeen.slice(0, 7) : 'unknown'})`
  );

  timer = setInterval(tick, INTERVAL_SEC * 1000);
  // Don't keep the process alive just for the poller.
  if (typeof timer.unref === 'function') timer.unref();
  return timer;
}

module.exports = { startGamePuller, PULL_ENABLED, INTERVAL_SEC };
