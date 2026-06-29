'use strict';

const errorEl = document.getElementById('loginError');
const githubBtn = document.getElementById('githubBtn');

// Friendly messages for the ?e= codes the OAuth callback redirects with.
const ERROR_MESSAGES = {
  state: 'Your sign-in link expired. Please try again.',
  denied: "That GitHub account isn't on the guest list for this studio.",
  failed: "GitHub sign-in didn't go through. Please try again.",
  oauth_off: 'GitHub sign-in is not set up yet — ask Jorge to configure it.',
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

// GitHub is the only sign-in. Show the button when it's configured; otherwise
// tell the user it isn't set up yet.
(async function configureUi() {
  let cfg = { github: false };
  try {
    const res = await fetch('/api/auth/config');
    if (res.ok) cfg = await res.json();
  } catch (_) {
    /* leave the button hidden */
  }

  if (cfg.github) {
    githubBtn.hidden = false;
  } else {
    showError('GitHub sign-in is not set up yet — ask Jorge to configure it.');
  }
})();
