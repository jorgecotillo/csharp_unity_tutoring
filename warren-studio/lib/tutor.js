'use strict';

// Code Tutor — generates a kid-friendly, tutor-style explanation of the code
// Copilot JUST changed, so Warren can read "here's what I wrote for you and WHY"
// during the ~6-minute build wait (shown by the Build Coach overlay).
//
// HOW IT FITS: after a chat turn edits the game and a rebuild is kicked off,
// server.js calls generate(sha, files, message) fire-and-forget. This spawns a
// SHORT, CHEAP Copilot run (claude-haiku-4.5) with the actual git diff inline and
// a "explain like a tutor to an 8th grader" prompt that returns JSON lesson cards.
// The result is cached by commit SHA. coach.js polls /api/tutor/lesson and swaps
// these real, code-specific cards in, falling back to its hardcoded generic deck
// while generating or if anything fails.
//
// SAFETY: this is purely additive and fail-safe. It never throws into its caller,
// never touches the build/chat paths, and on ANY error simply leaves the generic
// deck in place. It reuses chat.js's proven fast-spawn config (the same loader +
// MCP-disable flags that avoid the ~90s cold start).

const { spawn } = require('child_process');
const { execFileSync } = require('child_process');
const repo = require('./repo');
const chat = require('./chat');

// Cheap + fast model for explanations. Overridable without a code change.
const TUTOR_MODEL = process.env.TUTOR_MODEL || 'claude-haiku-4.5';
// Hard ceiling on a single generation so a wedged spawn can't leak.
const TUTOR_TIMEOUT_MS = parseInt(process.env.TUTOR_TIMEOUT_MS || String(120 * 1000), 10);
// Cap the diff we inline so the prompt stays small → the run stays fast + cheap.
const MAX_DIFF_CHARS = parseInt(process.env.TUTOR_MAX_DIFF || '9000', 10);
// How many past lessons to keep (by SHA). Small — only the latest matters.
const CACHE_CAP = 12;

// sha -> { status:'generating'|'ready'|'failed', cards, message, at, files }
const cache = new Map();
let latestSha = null;

function trimCache() {
  while (cache.size > CACHE_CAP) {
    const oldest = cache.keys().next().value;
    cache.delete(oldest);
  }
}

// Only .cs (and a couple of other code-ish) files are worth explaining — skip
// meta/asset/binary churn that a Mixamo import drags along.
function codeFiles(files) {
  if (!Array.isArray(files)) return [];
  return files
    .map((f) => String(f).replace(/^\.\//, ''))
    .filter((f) => /\.(cs|shader|hlsl|cginc|js|ts)$/i.test(f) && !/\.meta$/i.test(f));
}

// Get the diff for a commit, limited to the given paths, capped in size. Best
// effort — returns '' on any git error (the tutor prompt still works from the
// file list + Warren's request alone).
function diffFor(sha, files) {
  try {
    const args = ['show', '--no-color', '--unified=3', sha, '--'];
    for (const f of files) args.push(f);
    let out = execFileSync('git', args, {
      cwd: repo.REPO_ROOT, encoding: 'utf8', maxBuffer: 8 * 1024 * 1024,
    });
    if (out.length > MAX_DIFF_CHARS) {
      out = out.slice(0, MAX_DIFF_CHARS) + '\n… (diff trimmed) …\n';
    }
    return out;
  } catch (_) {
    return '';
  }
}

function buildTutorPrompt(message, files, diff) {
  const fileList = files.length ? files.join(', ') : '(a few game files)';
  return [
    "You are a warm, funny coding TUTOR for Warren, a curious 8th grader building a Unity",
    "game called \"Goblin Siege\" (a 2D/3D top-down army-vs-village battle game in C#).",
    "",
    "Warren just asked his AI helper to do this:",
    '  "' + String(message || 'update the game').slice(0, 300) + '"',
    "",
    "The helper changed these files: " + fileList + ".",
    "Here is the actual code diff of what changed (may be trimmed):",
    "--- DIFF START ---",
    diff || "(diff unavailable — explain from the request and file names)",
    "--- DIFF END ---",
    "",
    "Explain THIS specific change to Warren like a friendly tutor. Cover, across the cards:",
    "  • WHAT the new code does, in plain kid English.",
    "  • WHY it was written this way (the coding DECISION — e.g. why in Update(), why a",
    "    coroutine, why a variable, why a prefab). Teach the reasoning, not just the what.",
    "  • If any ANIMATION / movement / timing is involved, explain how that works.",
    "Tie everything to HIS game and the real code above. Be concrete and specific — mention",
    "the real class/method/variable names from the diff. No fluff, no lecturing.",
    "",
    "Return ONLY a JSON object, no markdown, no prose around it, in exactly this shape:",
    '{"cards":[{"title":"emoji + 4-7 word title","body":"2-4 short sentences, <=350 chars, plain text (no markdown)","ask":"one short Socratic question starting with a target emoji"}]}',
    "Give 3 or 4 cards. Keep each body punchy and readable by a 13-year-old.",
  ].join('\n');
}

// Pull a JSON object with a `cards` array out of the model's raw text. Tolerant:
// strips ``` fences and grabs the outermost {...} if there's stray prose.
function parseCards(raw) {
  if (!raw || typeof raw !== 'string') return null;
  let s = raw.trim().replace(/^```(?:json)?\s*/i, '').replace(/\s*```$/i, '').trim();
  const tryParse = (t) => { try { return JSON.parse(t); } catch (_) { return null; } };
  let obj = tryParse(s);
  if (!obj) {
    const a = s.indexOf('{'); const b = s.lastIndexOf('}');
    if (a >= 0 && b > a) obj = tryParse(s.slice(a, b + 1));
  }
  if (!obj || !Array.isArray(obj.cards)) return null;
  const cards = obj.cards
    .map((c) => ({
      title: String((c && c.title) || '').slice(0, 80),
      body: String((c && c.body) || '').slice(0, 500),
      ask: String((c && c.ask) || '').slice(0, 200),
    }))
    .filter((c) => c.title && c.body)
    .slice(0, 5);
  return cards.length ? cards : null;
}

// Spawn the cheap model once, collect its final text, resolve with the raw string
// (or reject on failure/timeout). Reuses chat.js's fast config so there's no
// ~90s MCP cold start.
function spawnTutor(prompt) {
  return new Promise((resolve, reject) => {
    const loader = chat.COPILOT_LOADER;
    const mcpFlags = Array.isArray(chat.MCP_DISABLE_FLAGS) ? chat.MCP_DISABLE_FLAGS : [];
    const args = [
      '-p', prompt,
      '--model', TUTOR_MODEL,
      '--silent', '--no-color', '--no-ask-user',
      // Read-only: the tutor never edits or runs anything.
      '--deny-tool', 'shell', '--deny-tool', 'write', '--deny-tool', 'edit',
      '--mode', 'interactive',
      '--no-custom-instructions',
      '--disable-builtin-mcps',
      ...mcpFlags,
      '--output-format', 'json',
      '--log-level', 'none',
      '-C', repo.REPO_ROOT,
    ];

    let child;
    try {
      child = loader
        ? spawn(process.execPath, [loader, ...args], { shell: false, cwd: repo.REPO_ROOT })
        : spawn('copilot', args, { shell: true, cwd: repo.REPO_ROOT });
    } catch (err) {
      return reject(err);
    }

    let stdout = '';
    let full = '';
    let done = false;
    const finish = (fn, arg) => { if (done) return; done = true; clearTimeout(timer); try { child.kill(); } catch (_) {} fn(arg); };

    const timer = setTimeout(() => finish(reject, new Error('tutor timeout')), TUTOR_TIMEOUT_MS);

    const onEvent = (evt) => {
      if (!evt || typeof evt !== 'object') return;
      const t = evt.type;
      if (t === 'assistant.message_delta') {
        const p = evt.data && evt.data.deltaContent;
        if (p) full += p;
      } else if (t === 'assistant.message') {
        if (evt.data && typeof evt.data.content === 'string' && evt.data.content.length >= full.length) {
          full = evt.data.content;
        }
      }
    };
    const drain = (isFinal) => {
      let i;
      while ((i = stdout.indexOf('\n')) >= 0) {
        const line = stdout.slice(0, i).trim(); stdout = stdout.slice(i + 1);
        if (line) { try { onEvent(JSON.parse(line)); } catch (_) {} }
      }
      if (isFinal && stdout.trim()) { try { onEvent(JSON.parse(stdout.trim())); } catch (_) {} stdout = ''; }
    };

    child.stdout.on('data', (c) => { stdout += c.toString('utf8'); drain(false); });
    child.on('error', (err) => finish(reject, err));
    child.on('close', () => { drain(true); full ? finish(resolve, full) : finish(reject, new Error('tutor produced no text')); });
  });
}

// PUBLIC: fire-and-forget. Generate (once) a tutor lesson for a commit. Idempotent
// per SHA — a second call for the same SHA is a no-op. Never throws.
function generate(sha, files, message) {
  try {
    if (!sha) return;
    const code = codeFiles(files);
    if (code.length === 0) return; // nothing code-worthy changed (e.g. only art/meta)

    latestSha = sha;
    const existing = cache.get(sha);
    if (existing && (existing.status === 'generating' || existing.status === 'ready')) return;

    cache.set(sha, { status: 'generating', cards: null, message: String(message || ''), at: Date.now(), files: code });
    trimCache();

    const diff = diffFor(sha, code);
    const prompt = buildTutorPrompt(message, code, diff);

    spawnTutor(prompt)
      .then((raw) => {
        const cards = parseCards(raw);
        const rec = cache.get(sha) || {};
        if (cards) {
          cache.set(sha, { ...rec, status: 'ready', cards, at: Date.now() });
          console.log('[tutor] lesson ready for', sha, '(' + cards.length + ' cards)');
        } else {
          cache.set(sha, { ...rec, status: 'failed', cards: null, at: Date.now() });
          console.warn('[tutor] could not parse cards for', sha);
        }
      })
      .catch((err) => {
        const rec = cache.get(sha) || {};
        cache.set(sha, { ...rec, status: 'failed', cards: null, at: Date.now() });
        console.warn('[tutor] generation failed for', sha, err && err.message);
      });
  } catch (err) {
    console.warn('[tutor] generate threw (ignored):', err && err.message);
  }
}

// PUBLIC: what the coach polls. Returns the latest lesson's state.
//   ready  → { ready:true, sha, cards, message }
//   still working / none → { ready:false, sha, status }
function latest() {
  if (!latestSha) return { ready: false, status: 'none' };
  const rec = cache.get(latestSha);
  if (!rec) return { ready: false, status: 'none' };
  if (rec.status === 'ready' && rec.cards) {
    return { ready: true, sha: latestSha, cards: rec.cards, message: rec.message };
  }
  return { ready: false, sha: latestSha, status: rec.status };
}

module.exports = { generate, latest, TUTOR_MODEL };
