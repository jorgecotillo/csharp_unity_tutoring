'use strict';

// Phase 2 brain: drive the headless GitHub Copilot CLI as Jorge, scoped to this
// one repo, in READ / PROPOSE mode (no writes, no shell). One spawn per message.
//
// Two things make this fast and safe:
//   1. We disable the builtin MCP servers AND every user-level MCP server found
//      in ~/.copilot/mcp-config.json. On a dev box those are 20+ heavy
//      kusto/ado servers that add ~90s of cold-start to every spawn; in the
//      deployed container the file won't exist, so the list is empty and this
//      is automatically correct.
//   2. --mode plan + --deny-tool write/shell = it can read & propose, never act.

const { spawn, execSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const repo = require('./repo');

// Default model — strong for code, far lower premium cost than opus.
const CHAT_MODEL = process.env.CHAT_MODEL || 'claude-sonnet-4.6';

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
    '- You can READ files and EXPLAIN code, and you can PROPOSE changes by showing the exact code. Do NOT edit, write, or run anything yet — proposing is enough.',
    '- Keep answers short, upbeat, and easy for a middle-schooler. Simple words, a little fun (emojis welcome ⚔️). Avoid scary jargon; explain any term you must use.',
    '- When you suggest a code change, show a small code block and say which file it goes in.',
    '',
    "Warren's message:",
    userMessage,
  ].join('\n');
}

// Spawn the CLI for one message. Calls handlers.onDelta(text) as tokens stream,
// handlers.onDone({text, usage, model}) on success, handlers.onError(err) on
// failure. Returns { cancel() } to kill the child if the client disconnects.
function streamChat(userMessage, handlers) {
  const onDelta = (handlers && handlers.onDelta) || (() => {});
  const onDone = (handlers && handlers.onDone) || (() => {});
  const onError = (handlers && handlers.onError) || (() => {});

  const args = [
    '-p', buildPrompt(userMessage),
    '--model', CHAT_MODEL,
    '--silent',
    '--no-color',
    '--no-ask-user',
    '--allow-all-tools',
    '--deny-tool', 'write',
    '--deny-tool', 'shell',
    '--mode', 'plan',
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
