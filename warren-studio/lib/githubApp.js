'use strict';

// Mint short-lived GitHub App installation tokens with NO npm dependencies.
//
// Flow:
//   1. Build an RS256-signed JWT from the App's private key (built-in crypto).
//   2. POST it to GitHub to exchange for an installation access token scoped to
//      the single repo the App is installed on (contents: write).
//   3. Cache the token until ~5 min before it expires, then re-mint.
//
// This token is what git.js injects into the push URL so the container can push
// to `main` WITHOUT a personal access token. The App is installed on ONLY
// `csharp_unity_tutoring`, so the credential literally cannot touch other repos.
//
// Required env (set in Azure app settings / Key Vault):
//   GITHUB_APP_ID                 numeric App ID
//   GITHUB_APP_INSTALLATION_ID    installation ID (App installed on the repo)
//   GITHUB_APP_PRIVATE_KEY        the PEM, inline (supports \n-escaped newlines)
//     OR
//   GITHUB_APP_PRIVATE_KEY_PATH   path to the .pem file on disk

const fs = require('fs');
const crypto = require('crypto');

const APP_ID = process.env.GITHUB_APP_ID || '';
const INSTALLATION_ID = process.env.GITHUB_APP_INSTALLATION_ID || '';

function loadPrivateKey() {
  const inline = process.env.GITHUB_APP_PRIVATE_KEY;
  if (inline && inline.trim()) {
    // App settings often store the PEM with literal \n — normalize them.
    return inline.includes('\\n') ? inline.replace(/\\n/g, '\n') : inline;
  }
  const keyPath = process.env.GITHUB_APP_PRIVATE_KEY_PATH;
  if (keyPath && fs.existsSync(keyPath)) {
    return fs.readFileSync(keyPath, 'utf8');
  }
  return '';
}

function isConfigured() {
  return !!(APP_ID && INSTALLATION_ID && loadPrivateKey());
}

function base64url(input) {
  return Buffer.from(input)
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

function buildJwt(privateKey) {
  const now = Math.floor(Date.now() / 1000);
  const header = { alg: 'RS256', typ: 'JWT' };
  // iat back-dated 60s to tolerate clock skew; exp capped at 10 min (GitHub max).
  const payload = { iat: now - 60, exp: now + 540, iss: APP_ID };
  const signingInput =
    base64url(JSON.stringify(header)) + '.' + base64url(JSON.stringify(payload));
  const signer = crypto.createSign('RSA-SHA256');
  signer.update(signingInput);
  signer.end();
  const signature = signer
    .sign(privateKey)
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
  return signingInput + '.' + signature;
}

let cached = { token: null, expiresAt: 0 };

async function getInstallationToken() {
  if (!isConfigured()) {
    throw new Error('GitHub App is not configured');
  }
  // Reuse the cached token until 5 min before it expires.
  if (cached.token && Date.now() < cached.expiresAt - 5 * 60 * 1000) {
    return cached.token;
  }

  const jwt = buildJwt(loadPrivateKey());
  const url = `https://api.github.com/app/installations/${INSTALLATION_ID}/access_tokens`;
  const res = await fetch(url, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${jwt}`,
      Accept: 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28',
      'User-Agent': 'warren-studio',
    },
  });

  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(
      `Failed to mint installation token (${res.status}): ${body.slice(0, 300)}`
    );
  }

  const data = await res.json();
  cached = {
    token: data.token,
    expiresAt: data.expires_at ? Date.parse(data.expires_at) : Date.now() + 55 * 60 * 1000,
  };
  return cached.token;
}

module.exports = { isConfigured, getInstallationToken };
