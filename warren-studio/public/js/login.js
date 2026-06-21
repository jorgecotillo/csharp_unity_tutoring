'use strict';

const errorEl = document.getElementById('loginError');
const githubBtn = document.getElementById('githubBtn');
const loginOr = document.getElementById('loginOr');
const devLogin = document.getElementById('devLogin');
const form = document.getElementById('loginForm');
const btn = document.getElementById('loginBtn');

// Friendly messages for the ?e= codes the OAuth callback redirects with.
const ERROR_MESSAGES = {
  state: 'Your sign-in link expired. Please try again.',
  denied: "That GitHub account isn't on the guest list for this studio.",
  failed: "GitHub sign-in didn't go through. Please try again.",
  oauth_off: 'GitHub sign-in is not set up yet — use the dev sign-in below.',
};

function showError(msg) {
  errorEl.textContent = '❌ ' + msg;
  errorEl.hidden = false;
}

// Surface any error the OAuth callback sent us back with, then clean the URL.
(function showRedirectError() {
  const code = new URLSearchParams(window.location.search).get('e');
  if (code) {
    showError(ERROR_MESSAGES[code] || 'Sign-in failed. Please try again.');
    const clean = window.location.pathname;
    window.history.replaceState({}, document.title, clean);
  }
})();

// Ask the server which sign-in methods are available and show only those.
(async function configureUi() {
  let cfg = { github: false, password: true };
  try {
    const res = await fetch('/api/auth/config');
    if (res.ok) cfg = await res.json();
  } catch (_) {
    /* fall back to showing the dev form */
  }

  if (cfg.github) {
    githubBtn.hidden = false;
  }
  if (cfg.github && cfg.password) {
    loginOr.hidden = false;
  }
  if (cfg.password) {
    devLogin.hidden = false;
    // If GitHub is the primary path, keep the dev form tucked away.
    devLogin.open = !cfg.github;
  } else {
    devLogin.hidden = true;
  }
})();

if (form) {
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    errorEl.hidden = true;

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;
    if (!username || !password) {
      showError('Enter your hero name and password.');
      return;
    }

    btn.disabled = true;
    btn.textContent = '⏳ Opening the gate…';

    try {
      const res = await fetch('/api/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      const data = await res.json().catch(() => ({}));
      if (res.ok && data.ok) {
        window.location.href = '/';
        return;
      }
      throw new Error(data.error || 'Login failed.');
    } catch (err) {
      showError(err.message);
      btn.disabled = false;
      btn.textContent = '🛡️ Enter the Studio';
    }
  });
}
