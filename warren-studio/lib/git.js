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
const githubApp = require('./githubApp');
const gitlock = require('./gitlock');

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

// Derive the "owner/repo" slug for building an authenticated push URL. Prefer
// the explicit GIT_REPO_SLUG env, else parse `git remote get-url origin`.
let _slugCache;
function repoSlug() {
  if (_slugCache !== undefined) return _slugCache;
  const fromEnv = String(process.env.GIT_REPO_SLUG || '').trim();
  if (fromEnv) {
    _slugCache = fromEnv.replace(/\.git$/i, '');
    return _slugCache;
  }
  let url = '';
  try {
    url = git(['remote', 'get-url', REMOTE]).trim();
  } catch (_) {
    _slugCache = null;
    return _slugCache;
  }
  const m = url.match(/github\.com[:/]([^/]+\/[^/]+?)(?:\.git)?$/i);
  _slugCache = m ? m[1] : null;
  return _slugCache;
}

// Resolve the push target. When a GitHub App is configured we mint a short-lived
// installation token and push to an https URL with that token embedded so the
// container needs NO stored PAT. Otherwise fall back to the named remote (works
// for local dev where the user's own git credentials are present).
// Resolve the push target. Priority:
//   1. The logged-in user's own GitHub OAuth token (pusher) — pushes are
//      attributed to that human (Warren commits as Warren).
//   2. A configured GitHub App installation token (legacy/optional fallback).
//   3. The named remote (local dev where the user's git credentials are present).
async function pushTarget(pusher) {
  if (pusher && pusher.token) {
    const slug = repoSlug();
    if (slug) {
      return `https://x-access-token:${pusher.token}@github.com/${slug}.git`;
    }
  }
  if (githubApp.isConfigured()) {
    const slug = repoSlug();
    if (slug) {
      const token = await githubApp.getInstallationToken();
      return `https://x-access-token:${token}@github.com/${slug}.git`;
    }
  }
  return REMOTE;
}

// Strip any embedded credential (x-access-token:<token>@) out of a string so a
// short-lived GitHub App token can never leak into an error surfaced to the UI.
function redact(s) {
  return String(s == null ? '' : s).replace(
    /x-access-token:[^@\s]+@/gi,
    'x-access-token:***@'
  );
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
 * @param {{token?:string, login?:string, name?:string, email?:string}} [pusher]
 *        The logged-in GitHub user (from the OAuth token store). When present,
 *        the commit is authored as that human and pushed with their token.
 * @returns {Promise<{changed:boolean, files:string[], sha?:string,
 *           pushed?:boolean, rebased?:boolean, conflict?:boolean,
 *           pushError?:string, pushNote?:string}>}
 */
async function commitGameEdits(message, pusher) {
  const files = changedGameFiles();
  if (files.length === 0) {
    return { changed: false, files: [] };
  }

  // Hold the git lock across the whole commit+push so the background puller
  // can't ff-merge origin/main into the working tree mid-operation.
  gitlock.set(true);
  try {
    // Stage ONLY the allow-listed game paths. Anything the agent touched
    // outside this list stays unstaged and is never committed.
    git(['add', '--', ...COMMIT_PATHSPECS]);

    // Edits that net to no change leave nothing staged → nothing to commit.
    if (gitCode(['diff', '--cached', '--quiet']) === 0) {
      return { changed: false, files: [] };
    }

    const subject =
      (message && String(message).replace(/\s+/g, ' ').trim().slice(0, 72)) ||
      'Warren game update';
    const commitMsg = `${subject}\n\nMade in Warren's Game Studio.\n\n${COAUTHOR}\n`;

    // When a GitHub-logged-in user is driving, attribute the commit to that
    // human (author + committer) so history shows e.g. "Warren", not a bot.
    const commitArgs = ['commit', '-m', commitMsg];
    const commitOpts = {};
    if (pusher && (pusher.login || pusher.name)) {
      const authorName = pusher.name || pusher.login;
      const authorEmail = pusher.email || `${pusher.login}@users.noreply.github.com`;
      commitArgs.push(`--author=${authorName} <${authorEmail}>`);
      commitOpts.env = {
        ...process.env,
        GIT_COMMITTER_NAME: authorName,
        GIT_COMMITTER_EMAIL: authorEmail,
      };
    }
    git(commitArgs, commitOpts);

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

    // Resolve the push target (the user's own OAuth token, a GitHub App
    // installation token, or the named remote).
    let target;
    try {
      target = await pushTarget(pusher);
    } catch (tokenErr) {
      result.pushed = false;
      result.pushError = redact(
        (tokenErr && tokenErr.message) ? tokenErr.message : String(tokenErr)
      ).slice(0, 300);
      return result;
    }

    // Push to the target branch; on a non-fast-forward, rebase on the remote
    // once and retry — the "fix any merge conflicts" path the user asked for.
    try {
      git(['push', target, `HEAD:${TARGET_BRANCH}`]);
      result.pushed = true;
    } catch (_firstErr) {
      try {
        git(['pull', '--rebase', REMOTE, TARGET_BRANCH]);
        git(['push', target, `HEAD:${TARGET_BRANCH}`]);
        result.pushed = true;
        result.rebased = true;
      } catch (secondErr) {
        result.pushed = false;
        result.conflict = true;
        result.pushError = redact(
          (secondErr && secondErr.message) ? secondErr.message : String(secondErr)
        ).slice(0, 300);
      }
    }
    return result;
  } finally {
    gitlock.set(false);
  }
}

module.exports = { commitGameEdits, TARGET_BRANCH, REMOTE, PUSH_ENABLED };
