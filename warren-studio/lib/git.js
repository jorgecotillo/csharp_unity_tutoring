'use strict';

// Phase 3 write-back: commit (and optionally push) the game edits the Copilot
// agent just made on disk. The agent itself can NEVER run git (chat.js keeps
// --deny-tool shell); THIS module is the single, deterministic gate between
// "files changed on disk" and the live branch.
//
// Safety model:
//   * Only ever stages an allow-list of GAME paths (mvp_v1/** + the spec). Even
//     if the agent edited warren-studio/** or anything else, those changes are
//     NOT staged, committed, or pushed.
//   * Commits locally whenever staged game files actually changed.
//   * PUSHES only when GIT_PUSH_ENABLED is truthy. Default OFF → local dev and
//     testing never touch the real remote. Azure flips it ON.
//   * Target branch = GIT_TARGET_BRANCH || 'main'. On a non-fast-forward push it
//     rebases on the remote once and retries (the "fix merge conflicts" path).

const { execFileSync } = require('child_process');
const repo = require('./repo');

const TARGET_BRANCH = process.env.GIT_TARGET_BRANCH || 'main';
const REMOTE = process.env.GIT_REMOTE || 'origin';
const PUSH_ENABLED = /^(1|true|yes|on)$/i.test(String(process.env.GIT_PUSH_ENABLED || ''));

// Pathspecs (relative to REPO_ROOT) the studio is allowed to commit. NEVER
// include warren-studio so the portal can't rewrite its own backend.
const COMMIT_PATHSPECS = ['mvp_v1', repo.SPEC_FILE_REL];

const COAUTHOR =
  'Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>';

function git(args, opts) {
  return execFileSync('git', args, {
    cwd: repo.REPO_ROOT,
    encoding: 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'],
    ...opts,
  });
}

// Run a git command, returning its exit code instead of throwing. Used for the
// `--quiet` diff probes where a non-zero status is the meaningful signal.
function gitCode(args) {
  try {
    git(args);
    return 0;
  } catch (e) {
    return typeof e.status === 'number' ? e.status : 1;
  }
}

// Which allow-listed game files changed on disk (for a friendly UI message).
function changedGameFiles() {
  let out = '';
  try {
    out = git(['status', '--porcelain', '--', ...COMMIT_PATHSPECS]);
  } catch (_) {
    return [];
  }
  return out
    .split('\n')
    .map((l) => l.slice(3).trim()) // strip the 2-char XY status + space
    .filter(Boolean);
}

/**
 * Stage the allow-listed game paths, commit if anything changed, and (when
 * GIT_PUSH_ENABLED) push to the target branch, rebasing once on conflict.
 *
 * @param {string} message  Warren's request — used as the commit subject.
 * @returns {{changed:boolean, files:string[], sha?:string, pushed?:boolean,
 *           rebased?:boolean, conflict?:boolean, pushError?:string,
 *           pushNote?:string}}
 */
function commitGameEdits(message) {
  const files = changedGameFiles();
  if (files.length === 0) {
    return { changed: false, files: [] };
  }

  // Stage ONLY the allow-listed game paths. Anything the agent touched outside
  // this list stays unstaged and is never committed.
  git(['add', '--', ...COMMIT_PATHSPECS]);

  // Edits that net to no change leave nothing staged → nothing to commit.
  if (gitCode(['diff', '--cached', '--quiet']) === 0) {
    return { changed: false, files: [] };
  }

  const subject =
    (message && String(message).replace(/\s+/g, ' ').trim().slice(0, 72)) ||
    'Warren game update';
  const commitMsg = `${subject}\n\nMade in Warren's Game Studio.\n\n${COAUTHOR}\n`;
  git(['commit', '-m', commitMsg]);

  let sha = '';
  try {
    sha = git(['rev-parse', '--short', 'HEAD']).trim();
  } catch (_) {
    /* sha is best-effort */
  }

  const result = { changed: true, files, sha, pushed: false };

  if (!PUSH_ENABLED) {
    result.pushNote = 'local-only (push disabled)';
    return result;
  }

  // Push to the target branch; on a non-fast-forward, rebase on the remote once
  // and retry — this is the "fix any merge conflicts" path the user asked for.
  try {
    git(['push', REMOTE, `HEAD:${TARGET_BRANCH}`]);
    result.pushed = true;
  } catch (_firstErr) {
    try {
      git(['pull', '--rebase', REMOTE, TARGET_BRANCH]);
      git(['push', REMOTE, `HEAD:${TARGET_BRANCH}`]);
      result.pushed = true;
      result.rebased = true;
    } catch (secondErr) {
      result.pushed = false;
      result.conflict = true;
      result.pushError = (
        (secondErr && secondErr.message) ? secondErr.message : String(secondErr)
      ).slice(0, 300);
    }
  }
  return result;
}

module.exports = { commitGameEdits, TARGET_BRANCH, REMOTE, PUSH_ENABLED };
