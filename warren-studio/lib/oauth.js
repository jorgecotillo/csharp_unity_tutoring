'use strict';

// GitHub OAuth (classic OAuth App) helpers — the interactive "Sign in with
// GitHub" flow that is BOTH the studio's front-door auth AND the credential we
// use to commit & push as the logged-in user (so Warren's pushes are his own
// GitHub identity, not a shared bot).
//
// This is deliberately a CLASSIC OAuth App, not a GitHub App: we want a normal
// user access token whose pushes are attributed to that human. The token is
// long-lived, kept server-side only (see lib/tokens.js), and never written to
// the cookie or disk.
//
// Required env (see .env.example):
//   GITHUB_OAUTH_CLIENT_ID       OAuth App client id
//   GITHUB_OAUTH_CLIENT_SECRET   OAuth App client secret
//   GITHUB_OAUTH_ALLOWED_USERS   comma list of allowed GitHub logins (closed
//                                by default — empty means nobody gets in)
//   GITHUB_OAUTH_CALLBACK_URL    optional explicit callback; else derived from
//                                the incoming request
//   GITHUB_OAUTH_SCOPE           optional; defaults to 'repo read:user user:email'

const crypto = require('crypto');

const DEFAULT_SCOPE = 'repo read:user user:email';

function clientId() {
  return String(process.env.GITHUB_OAUTH_CLIENT_ID || '').trim();
}

function clientSecret() {
  return String(process.env.GITHUB_OAUTH_CLIENT_SECRET || '').trim();
}

function scope() {
  return String(process.env.GITHUB_OAUTH_SCOPE || '').trim() || DEFAULT_SCOPE;
}

// OAuth login is only available when both halves of the app credential exist.
function isConfigured() {
  return Boolean(clientId() && clientSecret());
}

// Allowed GitHub logins, lowercased. Closed by default: an empty/unset list
// means NO ONE can sign in (we never want an accidental open door).
function allowedUsers() {
  return String(process.env.GITHUB_OAUTH_ALLOWED_USERS || '')
    .split(',')
    .map((u) => u.trim().toLowerCase())
    .filter(Boolean);
}

function isAllowed(login) {
  const allow = allowedUsers();
  if (allow.length === 0) return false;
  return allow.includes(String(login || '').trim().toLowerCase());
}

// Random anti-CSRF state stored in the session before redirect, verified on
// the callback.
function newState() {
  return crypto.randomBytes(24).toString('hex');
}

// The redirect_uri GitHub will send the user back to. An explicit env override
// wins (use it when the public URL differs from what the app sees); otherwise
// derive it from the request. trust proxy=1 makes req.protocol honor
// x-forwarded-proto behind Azure's TLS terminator.
function callbackUrl(req) {
  const explicit = String(process.env.GITHUB_OAUTH_CALLBACK_URL || '').trim();
  if (explicit) return explicit;
  return `${req.protocol}://${req.get('host')}/auth/github/callback`;
}

// Build the GitHub authorize URL the browser is redirected to. allow_signup is
// off — this is a closed studio, not a place to make new accounts.
function authorizeUrl(state, redirectUri) {
  const params = new URLSearchParams({
    client_id: clientId(),
    redirect_uri: redirectUri,
    scope: scope(),
    state,
    allow_signup: 'false',
  });
  return `https://github.com/login/oauth/authorize?${params.toString()}`;
}

// Exchange the temporary ?code for a long-lived user access token. Uses the
// Node global fetch (Node >= 20) so we add no HTTP dependency.
async function exchangeCode(code, redirectUri) {
  const resp = await fetch('https://github.com/login/oauth/access_token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify({
      client_id: clientId(),
      client_secret: clientSecret(),
      code,
      redirect_uri: redirectUri,
    }),
  });
  if (!resp.ok) {
    throw new Error(`token exchange failed (${resp.status})`);
  }
  const data = await resp.json();
  if (data.error || !data.access_token) {
    throw new Error(data.error_description || data.error || 'no access_token returned');
  }
  return data.access_token;
}

// Resolve the authenticated user's identity. Email may be hidden, so fall back
// to /user/emails for the primary, then to the noreply address.
async function fetchUser(token) {
  const headers = {
    Authorization: `Bearer ${token}`,
    Accept: 'application/vnd.github+json',
    'User-Agent': 'warren-game-studio',
  };

  const uResp = await fetch('https://api.github.com/user', { headers });
  if (!uResp.ok) throw new Error(`could not load GitHub user (${uResp.status})`);
  const u = await uResp.json();

  let email = u.email || '';
  if (!email) {
    try {
      const eResp = await fetch('https://api.github.com/user/emails', { headers });
      if (eResp.ok) {
        const emails = await eResp.json();
        const primary = Array.isArray(emails)
          ? emails.find((e) => e && e.primary && e.verified) ||
            emails.find((e) => e && e.verified) ||
            emails[0]
          : null;
        if (primary && primary.email) email = primary.email;
      }
    } catch (_) {
      /* best-effort — fall through to the noreply address */
    }
  }
  if (!email) email = `${u.id}+${u.login}@users.noreply.github.com`;

  return {
    login: u.login,
    name: u.name || u.login,
    email,
    id: u.id,
  };
}

module.exports = {
  isConfigured,
  allowedUsers,
  isAllowed,
  newState,
  callbackUrl,
  authorizeUrl,
  exchangeCode,
  fetchUser,
  scope,
};
