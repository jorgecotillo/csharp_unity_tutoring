'use strict';

// Phase 3 brain: drive the headless GitHub Copilot CLI as Jorge, scoped to this
// one repo, in EDIT mode — it can read AND change the game's files on disk. One
// spawn per message.
//
// Two things make this fast and safe:
//   1. We disable the builtin MCP servers AND every user-level MCP server found
//      in ~/.copilot/mcp-config.json. On a dev box those are 20+ heavy
//      kusto/ado servers that add ~90s of cold-start to every spawn; in the
//      deployed container the file won't exist, so the list is empty and this
//      is automatically correct.
//   2. --mode interactive + write ALLOWED, but --deny-tool shell KEPT: the agent
//      edits files only, it can NEVER run git/shell. The studio's own git.js is
//      the single deterministic gate that commits/pushes those edits, and it
//      stages an allow-list (mvp_v1/** + the spec) so edits outside the game
//      can never reach the branch even if the agent strays.

const { spawn, execSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const repo = require('./repo');

// Model + reasoning config for the portal chat. Jorge set this to Opus 4.8 with
// the 1M "long_context" window and MAX reasoning — top quality, but far pricier
// per message (~15 premium requests each vs ~1 for sonnet-4.6). All three are
// env-overridable so cost can be dialed back WITHOUT a code change, e.g.:
//   CHAT_MODEL=claude-sonnet-4.6   CHAT_EFFORT=high   CHAT_CONTEXT=default
const CHAT_MODEL = process.env.CHAT_MODEL || 'claude-opus-4.8';
// Context window tier: 'default' or 'long_context' (the 1M window).
const CHAT_CONTEXT = process.env.CHAT_CONTEXT || 'long_context';
// Reasoning effort: none|minimal|low|medium|high|xhigh|max.
const CHAT_EFFORT = process.env.CHAT_EFFORT || 'max';

// Which chat sessions we've already sent the kid-grounding preamble to. The
// Copilot CLI resumes a session by its UUID (--session-id) and remembers the
// whole conversation itself, so we only inject the big system preamble on a
// session's FIRST turn; every later turn passes Warren's raw message and lets
// the CLI's own memory carry the context. Bounded so a long-lived server
// process can't leak; re-priming an old session is harmless (just redundant).
const primedSessions = new Set();
const PRIMED_CAP = 1000;

// Resolve the Copilot CLI's real JS entrypoint (npm-loader.js) ONCE at startup.
// We spawn `node <npm-loader.js> ...args` with shell:false so the OS hands each
// arg to argv verbatim — the multi-line prompt stays a single quoted argument.
// (Spawning the `copilot` shim with shell:true concatenates args unescaped and
// the CLI rejects the split-up prompt with "your prompt was not quoted".)
const COPILOT_LOADER = (() => {
  const rel = path.join('@github', 'copilot', 'npm-loader.js');
  const candidates = [];
  if (process.env.APPDATA) {
    candidates.push(path.join(process.env.APPDATA, 'npm', 'node_modules', rel));
  }
  candidates.push(path.join('/usr', 'local', 'lib', 'node_modules', rel));
  candidates.push(path.join('/usr', 'lib', 'node_modules', rel));
  for (const c of candidates) {
    try { if (fs.existsSync(c)) return c; } catch (_) { /* keep trying */ }
  }
  // Fallback: ask npm where global modules live (one cached spawn at load).
  try {
    const root = execSync('npm root -g', { encoding: 'utf8' }).trim();
    const guess = path.join(root, rel);
    if (fs.existsSync(guess)) return guess;
  } catch (_) { /* npm not found / not global */ }
  return null;
})();

function discoverMcpServerNames() {
  try {
    const cfgPath = path.join(os.homedir(), '.copilot', 'mcp-config.json');
    const cfg = JSON.parse(fs.readFileSync(cfgPath, 'utf8'));
    if (cfg && cfg.mcpServers && typeof cfg.mcpServers === 'object') {
      return Object.keys(cfg.mcpServers);
    }
  } catch (_) {
    /* no config / unreadable → nothing to disable */
  }
  return [];
}

const MCP_DISABLE_FLAGS = (() => {
  const flags = [];
  for (const name of discoverMcpServerNames()) {
    flags.push('--disable-mcp-server', name);
  }
  return flags;
})();

// Kid-facing grounding so the assistant stays on-task and easy to read.
function buildPrompt(userMessage) {
  return [
    "You are Copilot, a friendly coding helper inside \"Warren's Game Studio.\"",
    'You are talking to Warren, an 8th-grade student building a Unity game called "Goblin Siege."',
    '',
    'RULES:',
    '- You may ONLY help with the Goblin Siege Unity game in THIS repository. Politely refuse anything else.',
    `- The design spec is the file "${repo.SPEC_FILE_REL}". The game code lives under "${repo.CODE_ROOT_REL}". Read those files yourself whenever you need them.`,
    `- You CAN edit the game's files directly to make Warren's changes happen. ONLY edit files under "${repo.CODE_ROOT_REL}" (and the wider mvp_v1 game folder) or the design spec "${repo.SPEC_FILE_REL}". NEVER edit anything in the "warren-studio" folder or any other part of the repo.`,
    '- After you finish editing, write ONE short friendly sentence saying what you changed and which file(s) — Warren will see his game rebuild automatically.',
    '- You cannot run terminal/git commands, and you do not need to — just save your file edits and the studio handles the rest.',
    '- Keep answers short, upbeat, and easy for a middle-schooler. Simple words, a little fun (emojis welcome ⚔️). Avoid scary jargon; explain any term you must use.',
    '- Warren may attach a SCREENSHOT of his game. If an image is attached, look at it carefully and use what you see (positions, colors, on-screen text) to understand and fix his problem.',
    '',
    "Warren's message:",
    userMessage,
  ].join('\n');
}

// ---- Live activity → kid-friendly status labels --------------------------
// The CLI's JSON stream emits tool + reasoning events while it works. We turn
// those into short, upbeat one-liners ("📖 Reading Enemy.cs", "✏️ Editing
// PlayerController.cs") so Warren can SEE what Copilot is doing instead of
// staring at a frozen "thinking" bubble.

function baseName(p) {
  if (!p || typeof p !== 'string') return '';
  const parts = p.split(/[\\/]/);
  return parts[parts.length - 1] || p;
}

// Map one tool.execution_start event to a friendly status line.
function friendlyToolStatus(data) {
  const tool = String((data && data.toolName) || '').toLowerCase();
  const args = (data && data.arguments) || {};
  const file = baseName(args.path || args.filePath || args.file || args.notebookPath || '');
  if (/^(view|read|open|cat|head|tail|get_file|read_file)$/.test(tool)) {
    return file ? `📖 Reading ${file}` : '📖 Reading your game…';
  }
  if (/^(create|new|create_file|createfile|write_file)$/.test(tool)) {
    return file ? `✨ Creating ${file}` : '✨ Creating a new file…';
  }
  if (/(edit|str_replace|write|insert|apply_patch|patch|replace)/.test(tool)) {
    return file ? `✏️ Editing ${file}` : '✏️ Editing your game…';
  }
  if (/(grep|glob|search|find|ripgrep)/.test(tool)) {
    return '🔍 Searching through the code…';
  }
  if (/(ls|list|tree|dir)/.test(tool)) {
    return '📂 Looking around the project…';
  }
  return file ? `🔧 Working on ${file}…` : '🔧 Working on your game…';
}

// Map one assistant.reasoning event (the model's plan) to a short status. We
// show a trimmed first line so it stays readable for a middle-schooler.
function reasoningStatus(content) {
  if (!content || typeof content !== 'string') return '🧠 Thinking…';
  const firstLine = content.split('\n').map((s) => s.trim()).find(Boolean) || '';
  if (!firstLine) return '🧠 Thinking…';
  const clipped = firstLine.length > 90 ? firstLine.slice(0, 89).trimEnd() + '…' : firstLine;
  return '💭 ' + clipped;
}

// Spawn the CLI for one message. Calls handlers.onDelta(text) as tokens stream,
// handlers.onDone({text, usage, model}) on success, handlers.onError(err) on
// failure. `attachments` is an optional array of local file paths (e.g. a
// screenshot Warren pasted) forwarded to the model via --attachment. Returns
// { cancel() } to kill the child if the client disconnects.
function streamChat(userMessage, handlers, sessionId, attachments) {
  const onDelta = (handlers && handlers.onDelta) || (() => {});
  const onDone = (handlers && handlers.onDone) || (() => {});
  const onError = (handlers && handlers.onError) || (() => {});
  const onStatus = (handlers && handlers.onStatus) || (() => {});

  // First turn of a session → send the full grounding preamble. Later turns →
  // send Warren's raw message and let the CLI's own session memory carry the
  // context (it already knows the rules + earlier conversation).
  const isFirstTurn = !sessionId || !primedSessions.has(sessionId);
  if (sessionId) {
    if (primedSessions.size >= PRIMED_CAP) primedSessions.clear();
    primedSessions.add(sessionId);
  }
  const promptText = isFirstTurn ? buildPrompt(userMessage) : userMessage;

  const args = [
    '-p', promptText,
    '--model', CHAT_MODEL,
    ...(CHAT_CONTEXT ? ['--context', CHAT_CONTEXT] : []),
    ...(CHAT_EFFORT ? ['--effort', CHAT_EFFORT] : []),
    // Forward any attached files (e.g. a pasted screenshot) so the model can SEE
    // them. Each path becomes its own --attachment flag.
    ...(Array.isArray(attachments)
      ? attachments.filter(Boolean).flatMap((a) => ['--attachment', a])
      : []),
    '--silent',
    '--no-color',
    '--no-ask-user',
    '--allow-all-tools',
    '--deny-tool', 'shell',
    '--mode', 'interactive',
    ...(sessionId ? ['--session-id', sessionId] : []),
    '--no-custom-instructions',
    '--disable-builtin-mcps',
    ...MCP_DISABLE_FLAGS,
    '--output-format', 'json',
    '--log-level', 'none',
    '-C', repo.REPO_ROOT,
  ];

  let child;
  try {
    if (COPILOT_LOADER) {
      // Preferred: spawn the CLI's JS entrypoint directly. shell:false → every
      // arg is a discrete argv entry, so the quoted prompt is never re-split.
      child = spawn(process.execPath, [COPILOT_LOADER, ...args],
        { shell: false, cwd: repo.REPO_ROOT });
    } else {
      // Fallback if the loader couldn't be located: use the `copilot` shim.
      child = spawn('copilot', args, { shell: true, cwd: repo.REPO_ROOT });
    }
  } catch (err) {
    console.error('[chat] spawn threw:', err && err.message);
    onError(err);
    return { cancel() {} };
  }

  let stdoutBuf = '';
  let stderrBuf = '';
  let fullText = '';
  let usage = null;
  let model = CHAT_MODEL;
  let finished = false;

  function handleEvent(evt) {
    if (!evt || typeof evt !== 'object' || !evt.type) return;
    const type = evt.type;
    // Filter session.* noise (mcp status, skills loaded, tools updated, etc.).
    if (type.indexOf('session.') === 0) return;

    if (type === 'assistant.message_delta') {
      const piece = evt.data && evt.data.deltaContent;
      if (piece) {
        fullText += piece;
        onDelta(piece);
      }
    } else if (type === 'assistant.reasoning') {
      // The model narrating its plan ("Let me read the EnemyAI script…").
      onStatus(reasoningStatus(evt.data && evt.data.content));
    } else if (type === 'tool.execution_start') {
      // The agent actually touching a file — the most concrete signal we have.
      onStatus(friendlyToolStatus(evt.data));
    } else if (type === 'assistant.message_start') {
      // About to write the final answer text (deltas follow immediately).
      onStatus('✍️ Writing the answer…');
    } else if (type === 'assistant.message') {
      // Authoritative full text for the turn (also covers a non-streaming run).
      if (evt.data && typeof evt.data.content === 'string' &&
          evt.data.content.length >= fullText.length) {
        fullText = evt.data.content;
      }
      if (evt.data && evt.data.model) model = evt.data.model;
    } else if (type === 'result') {
      usage = (evt.data && evt.data.usage) || evt.usage || null;
    }
  }

  function drainLines(isFinal) {
    let idx;
    while ((idx = stdoutBuf.indexOf('\n')) >= 0) {
      const line = stdoutBuf.slice(0, idx).trim();
      stdoutBuf = stdoutBuf.slice(idx + 1);
      if (!line) continue;
      try { handleEvent(JSON.parse(line)); } catch (_) { /* non-JSON noise */ }
    }
    if (isFinal && stdoutBuf.trim()) {
      try { handleEvent(JSON.parse(stdoutBuf.trim())); } catch (_) { /* ignore */ }
      stdoutBuf = '';
    }
  }

  child.stdout.on('data', (chunk) => {
    stdoutBuf += chunk.toString('utf8');
    drainLines(false);
  });
  child.stderr.on('data', (chunk) => {
    stderrBuf += chunk.toString('utf8');
  });

  child.on('error', (err) => {
    if (finished) return;
    finished = true;
    console.error('[chat] child error:', err && err.message);
    onError(err);
  });

  child.on('close', (code) => {
    if (finished) return;
    finished = true;
    drainLines(true);
    if (code !== 0 && !fullText) {
      const msg = stderrBuf.trim() || ('Copilot exited with code ' + code + '.');
      console.error('[chat] child closed code=' + code + ' stderr=' + stderrBuf.slice(0, 800));
      onError(new Error(msg));
      return;
    }
    onDone({ text: fullText, usage, model });
  });

  return {
    cancel() {
      if (finished) return;
      try { child.kill(); } catch (_) { /* already gone */ }
    },
  };
}

module.exports = { streamChat, CHAT_MODEL };
