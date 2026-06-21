'use strict';

const form = document.getElementById('loginForm');
const errorEl = document.getElementById('loginError');
const btn = document.getElementById('loginBtn');

form.addEventListener('submit', async (e) => {
  e.preventDefault();
  errorEl.hidden = true;
  btn.disabled = true;
  btn.textContent = '⏳ Opening the gate…';

  const username = document.getElementById('username').value.trim();
  const password = document.getElementById('password').value;

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
    errorEl.textContent = '❌ ' + err.message;
    errorEl.hidden = false;
    btn.disabled = false;
    btn.textContent = '🛡️ Enter the Studio';
  }
});
