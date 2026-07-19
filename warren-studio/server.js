'use strict';

// Load config from a local .env file when present (dev convenience). In
// production, Container Apps / Key Vault inject these as real env vars, so this
// is a harmless no-op there.
require('dotenv').config();

const path = require('path');
const crypto = require('crypto');
const fs = require('fs');
const os = require('os');
const express = require('express');
const helmet = require('helmet');
const compression = require('compression');
const cookieSession = require('cookie-session');
const { marked } = require('marked');

const repo = require('./lib/repo');
const chat = require('./lib/chat');
const git = require('./lib/git');
const build = require('./lib/build');
const tutor = require('./lib/tutor');
const pull = require('./lib/pull');
const oauth = require('./lib/oauth');
const tokens = require('./lib/tokens');

const app = express();
const PORT = process.env.PORT || 8080;
const IS_PROD = process.env.NODE_ENV === 'production';

// Idle timeout for a logged-in session (minutes).
const SESSION_IDLE_MIN = parseInt(process.env.SESSION_IDLE_MIN || '120', 10);
const SESSION_IDLE_MS = SESSION_IDLE_MIN * 60 * 1000;

// Shape of a client-supplied chat session id (a v4-ish UUID). The browser owns
// the id so the Copilot CLI can resume the same conversation across messages;
// anything that doesn't match gets replaced with a fresh server-minted UUID.
const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Behind Azure Container Apps / App Service we sit behind a TLS-terminating proxy.
app.set('trust proxy', 1);
app.disable('x-powered-by');

app.use(compression());

// Helmet, but relaxed enough to allow the Unity WebGL iframe + CDN highlight.js.
app.use(
  helmet({
    contentSecurityPolicy: false, // Unity WebGL needs wasm-eval/blob; we keep CSP simple for Phase 1.
    crossOriginEmbedderPolicy: false,
  })
);

// Most routes only need a tiny JSON body. The chat route may carry a pasted
// screenshot (base64 data URL), so it gets a much larger limit — scoped to just
// that one route so the rest of the API keeps a tight 64kb cap.
const smallJson = express.json({ limit: '64kb' });
const chatJson = express.json({ limit: '16mb' });
app.use((req, res, next) => {
  if (req.method === 'POST' && req.path === '/api/chat') return chatJson(req, res, next);
  return smallJson(req, res, next);
});

const sessionSecret = process.env.SESSION_SECRET || (!IS_PROD ? 'dev-insecure-secret-change-me' : null);
if (!sessionSecret) {
  console.error('[fatal] SESSION_SECRET must be set in production.');
  process.exit(1);
}

app.use(
  cookieSession({
    name: 'wgs.sid',
    secret: sessionSecret,
    httpOnly: true,
    sameSite: 'lax',
    secure: IS_PROD,
    maxAge: SESSION_IDLE_MS,
  })
);

// ---- Auth helpers --------------------------------------------------------

function isLoggedIn(req) {
  const s = req.session;
  if (!s || !s.username || !s.lastSeen) return false;
  if (Date.now() - s.lastSeen > SESSION_IDLE_MS) return false;
  return true;
}

function touch(req) {
  if (req.session) req.session.lastSeen = Date.now();
}

// Gate for API routes — returns JSON 401.
function requireApiAuth(req, res, next) {
  if (!isLoggedIn(req)) return res.status(401).json({ error: 'Not logged in.' });
  touch(req);
  next();
}

// Gate for page/asset routes — redirects to the login page.
function requirePageAuth(req, res, next) {
  if (!isLoggedIn(req)) return res.redirect('/login');
  touch(req);
  next();
}

// ---- Public auth endpoints ----------------------------------------------

// What login methods are available (drives the login page UI). GitHub OAuth is
// the only door — there is no password fallback.
app.get('/api/auth/config', (req, res) => {
  res.json({ github: oauth.isConfigured() });
});

// ---- GitHub OAuth login (interactive) -----------------------------------
// "Sign in with GitHub" — this IS the auth gate AND the push credential.

app.get('/auth/github/login', (req, res) => {
  if (!oauth.isConfigured()) return res.redirect('/login?e=oauth_off');
  const state = oauth.newState();
  const redirectUri = oauth.callbackUrl(req);
  req.session.oauthState = state;
  req.session.oauthRedirect = redirectUri;
  res.redirect(oauth.authorizeUrl(state, redirectUri));
});

app.get('/auth/github/callback', async (req, res) => {
  try {
    if (!oauth.isConfigured()) return res.redirect('/login?e=oauth_off');
    const { code, state } = req.query || {};
    const wantState = req.session && req.session.oauthState;
    const redirectUri = (req.session && req.session.oauthRedirect) || oauth.callbackUrl(req);
    // One-time use: clear the CSRF state regardless of outcome.
    if (req.session) {
      req.session.oauthState = null;
      req.session.oauthRedirect = null;
    }
    if (!code || !state || !wantState || state !== wantState) {
      return res.redirect('/login?e=state');
    }
    const token = await oauth.exchangeCode(String(code), redirectUri);
    if (!token) return res.redirect('/login?e=failed');
    const user = await oauth.fetchUser(token);
    if (!user || !user.login) return res.redirect('/login?e=failed');
    if (!oauth.isAllowed(user.login)) return res.redirect('/login?e=denied');

    const sid = crypto.randomUUID();
    tokens.put(sid, {
      token,
      login: user.login,
      name: user.name || user.login,
      email: user.email,
    });
    req.session.username = user.login;
    req.session.name = user.name || user.login;
    req.session.sid = sid;
    req.session.via = 'github';
    req.session.lastSeen = Date.now();
    return res.redirect('/');
  } catch (err) {
    console.error('[oauth] callback error', err);
    return res.redirect('/login?e=failed');
  }
});

app.post('/api/logout', (req, res) => {
  if (req.session && req.session.sid) tokens.del(req.session.sid);
  req.session = null;
  res.json({ ok: true });
});

app.get('/api/me', (req, res) => {
  if (!isLoggedIn(req)) return res.status(401).json({ error: 'Not logged in.' });
  touch(req);
  res.json({
    username: req.session.username,
    name: req.session.name || req.session.username,
    via: req.session.via || 'github',
  });
});

// ---- Protected API -------------------------------------------------------

// Spec, rendered to HTML.
app.get('/api/spec', requireApiAuth, (req, res) => {
  const md = repo.readSpec();
  if (md == null) return res.status(404).json({ error: 'Spec file not found.' });
  res.json({ file: repo.SPEC_FILE_REL, html: marked.parse(md) });
});

// Code tree (folders + .cs/etc files under mvp_v1/Assets/Scripts).
app.get('/api/files', requireApiAuth, (req, res) => {
  try {
    res.json(repo.getCodeTree());
  } catch (err) {
    console.error('[files] error', err);
    res.status(500).json({ error: 'Could not read the code tree.' });
  }
});

// One file's contents (path allow-listed inside repo.js).
app.get('/api/file', requireApiAuth, (req, res) => {
  try {
    res.json(repo.readFile(req.query.path));
  } catch (err) {
    const status = err.code === 'FORBIDDEN' ? 403 : err.code ? 400 : 500;
    res.status(status).json({ error: err.message || 'Could not read the file.' });
  }
});

// Build/preview status for the game pane — includes live auto-build state so the
// UI can show a "rebuilding your game" animation.
app.get('/api/game/status', requireApiAuth, (req, res) => {
  res.json({
    available: repo.webglExists(),
    builtAt: repo.webglBuiltAt(),
    build: build.status(),
  });
});

// Latest Code Tutor lesson — a dynamic, kid-friendly explanation of the code
// Copilot just changed. The Build Coach polls this during a rebuild and swaps
// these real, code-specific cards in (falling back to its generic deck while
// this is still generating or if generation failed). Read-only + fail-safe.
app.get('/api/tutor/lesson', requireApiAuth, (req, res) => {
  try {
    res.json(tutor.latest());
  } catch (_) {
    res.json({ ready: false, status: 'error' });
  }
});

// ---- Copilot chat (Phase 2: read / propose only) -------------------------
// One spawn of the headless Copilot CLI per message, streamed back to the
// browser over Server-Sent Events. Per-user rate limited to protect the
// subscription.

const CHAT_RATE_PER_HOUR = parseInt(process.env.CHAT_RATE_PER_HOUR || '60', 10);
const CHAT_WINDOW_MS = 60 * 60 * 1000;
const MAX_MESSAGE_LEN = 2000;
const chatHits = new Map(); // username -> [timestamps within the rolling hour]

function rateLimit(username) {
  const now = Date.now();
  const hits = (chatHits.get(username) || []).filter((t) => now - t < CHAT_WINDOW_MS);
  if (hits.length >= CHAT_RATE_PER_HOUR) {
    const retryMs = CHAT_WINDOW_MS - (now - hits[0]);
    return { ok: false, retryMin: Math.max(1, Math.ceil(retryMs / 60000)) };
  }
  hits.push(now);
  chatHits.set(username, hits);
  return { ok: true };
}

// ---- Chat image attachments (pasted screenshots) -------------------------
// Warren can paste / drag-drop a screenshot into the chat; the client sends it
// as a base64 data URL. We validate type + size, write it to a short-lived temp
// file, hand the path to the CLI via --attachment, then delete it after the
// turn. Images only, capped well under the chat body limit.
const IMAGE_MIME_EXT = {
  'image/png': 'png',
  'image/jpeg': 'jpg',
  'image/jpg': 'jpg',
  'image/webp': 'webp',
  'image/gif': 'gif',
};
const MAX_IMAGE_BYTES = 10 * 1024 * 1024; // 10 MB (base64 stays under the 16mb json cap)
const UPLOAD_DIR = path.join(os.tmpdir(), 'warren-studio-uploads');

// Decode a base64 data URL and write it to a temp file. Returns { path } or
// { error } with a kid-friendly message.
function saveImageAttachment(dataUrl) {
  if (typeof dataUrl !== 'string') return { error: 'That image looked empty. 🖼️' };
  const m = dataUrl.match(/^data:([a-z0-9.+/-]+);base64,([\s\S]+)$/i);
  if (!m) return { error: "That image didn't look right. Try pasting it again! 🖼️" };
  const ext = IMAGE_MIME_EXT[m[1].toLowerCase()];
  if (!ext) return { error: 'Only images (PNG, JPG, WebP, GIF) can be attached. 🖼️' };
  let buf;
  try { buf = Buffer.from(m[2], 'base64'); } catch (_) { return { error: "Couldn't read that image. 🖼️" }; }
  if (!buf || buf.length === 0) return { error: 'That image was empty. 🖼️' };
  if (buf.length > MAX_IMAGE_BYTES) {
    return { error: `That image is a bit big — keep it under ${Math.round(MAX_IMAGE_BYTES / (1024 * 1024))} MB. 📦` };
  }
  try {
    if (!fs.existsSync(UPLOAD_DIR)) fs.mkdirSync(UPLOAD_DIR, { recursive: true });
    const file = path.join(UPLOAD_DIR, `${crypto.randomUUID()}.${ext}`);
    fs.writeFileSync(file, buf);
    return { path: file };
  } catch (e) {
    console.error('[chat] failed to save image attachment', e && e.message);
    return { error: "Couldn't save that image. Tell Jorge! 🔧" };
  }
}

function cleanupTempFiles(paths) {
  for (const p of paths || []) {
    try { fs.unlinkSync(p); } catch (_) { /* best effort */ }
  }
}

app.post('/api/chat', requireApiAuth, (req, res) => {
  const username = req.session.username;
  // Capture the push identity NOW (synchronously) — the token store lookup must
  // happen before the async onDone closure runs. Every user signs in with GitHub,
  // so this is their own OAuth token and commits & pushes are attributed to them.
  // If a token is somehow missing, pusher is null and git.js falls back.
  const pusher = (req.session && req.session.sid) ? tokens.get(req.session.sid) : null;
  const message = (req.body && typeof req.body.message === 'string') ? req.body.message.trim() : '';

  // Client owns the chat session id so the CLI can keep conversation context
  // across turns. Validate it hard; fall back to a fresh UUID if missing/bogus.
  let sessionId = (req.body && typeof req.body.sessionId === 'string') ? req.body.sessionId.trim() : '';
  if (!UUID_RE.test(sessionId)) sessionId = crypto.randomUUID();

  // Optional pasted / dropped screenshot (base64 data URL). Validate + write to
  // a temp file; the path is handed to the CLI and deleted after the turn.
  const attachments = [];
  const tempPaths = [];
  if (req.body && req.body.image) {
    const saved = saveImageAttachment(req.body.image);
    if (saved.error) return res.status(400).json({ error: saved.error });
    attachments.push(saved.path);
    tempPaths.push(saved.path);
  }

  // A turn needs SOMETHING — text or an image. When only an image was sent, use
  // a friendly default prompt so the model knows to look at it.
  if (!message && attachments.length === 0) {
    return res.status(400).json({ error: 'Type a message first! ✍️' });
  }
  const effectiveMessage = message ||
    'Here is a screenshot of my game. 👀 Take a look and help me!';

  if (message.length > MAX_MESSAGE_LEN) {
    cleanupTempFiles(tempPaths);
    return res.status(400).json({ error: `That message is a bit long — keep it under ${MAX_MESSAGE_LEN} characters. ✂️` });
  }

  const gate = rateLimit(username);
  if (!gate.ok) {
    cleanupTempFiles(tempPaths);
    return res.status(429).json({
      error: `Whoa, you're on fire! 🔥 You've hit the chat limit — try again in about ${gate.retryMin} minute${gate.retryMin === 1 ? '' : 's'}. ⏳`,
    });
  }

  // Open the SSE stream.
  res.writeHead(200, {
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache, no-transform',
    Connection: 'keep-alive',
    'X-Accel-Buffering': 'no',
  });
  if (typeof res.flushHeaders === 'function') res.flushHeaders();

  function send(event, data) {
    res.write(`event: ${event}\n`);
    res.write(`data: ${JSON.stringify(data)}\n\n`);
  }

  // Initial comment so proxies don't buffer the first byte.
  res.write(': forging\n\n');

  // Keepalive heartbeat. Copilot's first token can take 30–60s+ on a cold start
  // (model warm-up + tool discovery). Without periodic bytes on the wire, the
  // dev tunnel and the browser idle-kill the connection and Warren sees the
  // frontend network error "Couldn't reach Copilot." A comment frame every 15s
  // keeps the pipe warm; SSE clients ignore lines that start with ":".
  const heartbeat = setInterval(() => {
    try { res.write(': ping\n\n'); } catch (_) { /* socket gone; res.on('close') cleans up */ }
  }, 15000);

  // Track completion so the disconnect handler below never kills a healthy run.
  let finished = false;

  const session = chat.streamChat(effectiveMessage, {
    onStatus(label) {
      if (finished) return;
      send('status', { label: String(label || '') });
    },
    onDelta(text) {
      send('delta', { text });
    },
    async onDone({ text, usage, model }) {
      finished = true;
      clearInterval(heartbeat);
      const html = text ? marked.parse(text) : '';

      // The agent may have edited the game's files. Commit those (allow-listed
      // to mvp_v1/** + the spec) so Warren's change is saved and — when push is
      // enabled — lands on main. A git hiccup must never break the chat reply,
      // so this is fully wrapped. commitGameEdits is async (mints a GitHub App
      // token + pushes under the in-process lock), so we await it.
      let gameEdit = null;
      try {
        gameEdit = await git.commitGameEdits(effectiveMessage, pusher);
      } catch (e) {
        console.error('[git] commit failed', e && e.message ? e.message : e);
        gameEdit = { changed: false, error: true };
      }

      // If the agent changed actual GAME CODE (anything under mvp_v1/, not just
      // the spec .md), kick off a background Unity rebuild so Warren's preview
      // updates on its own. This is fire-and-forget and single-flight+coalesced
      // inside build.js — it never blocks this reply or the next chat message.
      if (
        gameEdit && gameEdit.changed && Array.isArray(gameEdit.files) &&
        gameEdit.files.some((f) => String(f).replace(/^\.\//, '').startsWith('mvp_v1/'))
      ) {
        const r = build.requestBuild('chat edit by ' + username);
        if (r && r.ok) {
          gameEdit.rebuilding = true;
          if (r.queued) gameEdit.rebuildQueued = true;
        }
        // Fire-and-forget: generate a kid-friendly "here's the code I just wrote
        // and why" tutor lesson (cheap/fast model) so the Build Coach can show it
        // during the ~6-min build wait. Never blocks or breaks the reply.
        try { tutor.generate(gameEdit.sha, gameEdit.files, effectiveMessage); } catch (_) {}
      }

      send('done', {
        html,
        text,
        model: model || chat.CHAT_MODEL,
        premiumRequests: usage && typeof usage.premiumRequests === 'number' ? usage.premiumRequests : null,
        gameEdit,
      });
      res.end();
    },
    onError(err) {
      finished = true;
      clearInterval(heartbeat);
      console.error('[chat] error', err && err.message ? err.message : err);
      send('error', { error: "Hmm, my forge sputtered. 🔧 Give it another try in a sec!" });
      res.end();
    },
  }, sessionId, attachments);

  // If Warren genuinely closes the tab / navigates away mid-answer, kill the CLI
  // child. We listen on the RESPONSE (not the request): req's 'close' fires as
  // soon as express.json() finishes reading the body, which would otherwise
  // cancel the child before it ever produced output.
  res.on('close', () => {
    clearInterval(heartbeat);
    if (!finished) session.cancel();
    // The CLI has read the attachment by now (success, error, or cancel all
    // land here) — delete the temp screenshot so uploads don't pile up.
    cleanupTempFiles(tempPaths);
  });
});

// ---- Game preview (the already-built WebGL bundle) -----------------------
// Served with correct MIME types so Unity loads. Auth-gated so only logged-in
// users can reach it.
app.use(
  '/game',
  requirePageAuth,
  express.static(repo.WEBGL_DIR, {
    setHeaders(res, filePath) {
      if (filePath.endsWith('.wasm')) res.setHeader('Content-Type', 'application/wasm');
      else if (filePath.endsWith('.js')) res.setHeader('Content-Type', 'application/javascript');
      else if (filePath.endsWith('.data')) res.setHeader('Content-Type', 'application/octet-stream');
      else if (filePath.endsWith('.symbols.json')) res.setHeader('Content-Type', 'application/json');
    },
  })
);

// ---- Frontend pages ------------------------------------------------------

const PUBLIC_DIR = path.join(__dirname, 'public');

// Login page — public. If already logged in, bounce to the studio.
app.get('/login', (req, res) => {
  if (isLoggedIn(req)) return res.redirect('/');
  res.sendFile(path.join(PUBLIC_DIR, 'login.html'));
});

// Static public assets (css/js) used by both pages — these are not secrets.
app.use('/assets', express.static(path.join(PUBLIC_DIR, 'assets')));
app.use('/css', express.static(path.join(PUBLIC_DIR, 'css')));
app.use('/js', express.static(path.join(PUBLIC_DIR, 'js')));

// Studio (the 3-pane app) — gated.
app.get('/', requirePageAuth, (req, res) => {
  res.sendFile(path.join(PUBLIC_DIR, 'studio.html'));
});

// Health check for Container Apps.
app.get('/healthz', (req, res) => res.json({ ok: true }));

app.listen(PORT, () => {
  console.log(`⚔️  Warren's Game Studio listening on http://localhost:${PORT}`);
  console.log(`    Repo root : ${repo.REPO_ROOT}`);
  const allowed = oauth.allowedUsers();
  console.log(`    Login     : GitHub OAuth ${oauth.isConfigured() ? 'on' : 'NOT configured'} — allowed: ${allowed.join(', ') || 'NONE'}`);
  console.log(`    WebGL build present: ${repo.webglExists()}`);
  if (!oauth.isConfigured() || allowed.length === 0) {
    console.warn('[warn] Nobody can log in. Set GITHUB_OAUTH_CLIENT_ID/SECRET and GITHUB_OAUTH_ALLOWED_USERS.');
  }

  // Start the background poller that fast-forwards the local checkout to
  // origin/main, pulling down CI-built WebGL files so the preview refreshes.
  // No-op unless GIT_PULL_ENABLED is set (Azure on; local dev off).
  pull.startGamePuller();
});
