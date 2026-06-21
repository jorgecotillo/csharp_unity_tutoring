'use strict';

// Server-side, in-memory store for per-session secrets — specifically the
// GitHub OAuth access token we get for a logged-in user.
//
// WHY THIS EXISTS: the session cookie (cookie-session) is only SIGNED, not
// ENCRYPTED. Anything we put in req.session is readable by the browser. A
// GitHub access token must therefore NEVER live in the cookie. Instead the
// cookie carries only a random `sid`, and the real token sits here keyed by
// that sid.
//
// TRADE-OFF: this Map lives in process memory, so it's wiped on every restart
// (deploys, Container Apps scale events). That's fine — the user just clicks
// "Sign in with GitHub" once more and gets a fresh token. We deliberately do
// NOT persist tokens to disk/Key Vault to keep the blast radius tiny.

const store = new Map(); // sid -> { token, login, name, email, createdAt }

function put(sid, value) {
  if (!sid) return;
  store.set(sid, { ...value, createdAt: Date.now() });
}

function get(sid) {
  if (!sid) return null;
  return store.get(sid) || null;
}

function del(sid) {
  if (!sid) return;
  store.delete(sid);
}

function size() {
  return store.size;
}

module.exports = { put, get, del, size };
