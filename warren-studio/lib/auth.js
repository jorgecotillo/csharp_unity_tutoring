'use strict';

const bcrypt = require('bcryptjs');

// A bcrypt hash of random bytes — compared against when the username is unknown
// so failed logins take roughly the same time as real ones (avoids user enumeration).
const DUMMY_HASH = '$2a$10$CwTycUXWue0Thq9StjUM0uJ8Dvf1bnv0Fzg2dXr9Yd9Yd9Yd9Yd9';

/**
 * Load the user list. Two supported sources, in priority order:
 *   1. STUDIO_USERS — JSON array: [{ "username": "jorge", "hash": "$2a$..." }, ...]
 *      (this is the production path — hashes live in Key Vault / env, never in code)
 *   2. STUDIO_DEV_PASSWORD — dev convenience only. Creates jorge + warren, both using
 *      this plaintext password (hashed at boot). Clearly logged as DEV MODE.
 */
function loadUsers() {
  const raw = process.env.STUDIO_USERS;
  if (raw) {
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed
          .filter((u) => u && u.username && u.hash)
          .map((u) => ({ username: String(u.username), hash: String(u.hash) }));
      }
    } catch (err) {
      console.error('[auth] STUDIO_USERS is not valid JSON — ignoring it.', err.message);
    }
  }

  const devPw = process.env.STUDIO_DEV_PASSWORD;
  if (devPw) {
    const hash = bcrypt.hashSync(devPw, 10);
    console.warn(
      '[auth] ⚠ DEV MODE: jorge + warren are using STUDIO_DEV_PASSWORD. ' +
        'Set STUDIO_USERS with real bcrypt hashes before deploying.'
    );
    return [
      { username: 'jorge', hash },
      { username: 'warren', hash },
    ];
  }

  return [];
}

const users = loadUsers();

/**
 * Verify a username/password pair.
 * @returns {Promise<{username: string}|null>}
 */
async function verify(username, password) {
  const uname = String(username || '').trim().toLowerCase();
  const pw = String(password || '');
  const match = users.find((u) => u.username.toLowerCase() === uname);

  if (!match) {
    // Constant-ish time even for unknown users.
    await bcrypt.compare(pw, DUMMY_HASH);
    return null;
  }

  const ok = await bcrypt.compare(pw, match.hash);
  return ok ? { username: match.username } : null;
}

function userCount() {
  return users.length;
}

function usernames() {
  return users.map((u) => u.username);
}

module.exports = { verify, userCount, usernames };
