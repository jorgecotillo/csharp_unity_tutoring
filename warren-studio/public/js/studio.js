'use strict';

// Warren's Game Studio — front-of-house behavior for the 3-pane shell.
// Talks to the gated API in server.js. Any 401 means the session lapsed, so we
// bounce back to the login door.

(function () {
  const $ = (sel) => document.querySelector(sel);

  // One chat session id for the whole conversation, so Copilot remembers what
  // we already talked about (multi-turn memory). "New chat" rotates this.
  let chatSessionId = newSessionId();

  function newSessionId() {
    if (window.crypto && typeof window.crypto.randomUUID === 'function') {
      return window.crypto.randomUUID();
    }
    // Fallback for older browsers / non-secure contexts.
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  // The friendly welcome bubble shown on a fresh chat (mirrors studio.html).
  const WELCOME_HTML =
    '<div class="chat-bubble bot">' +
    '<p>Hey Warren! 👋 Ask me stuff like:</p>' +
    '<ul class="chat-ideas">' +
    '<li>"How do the goblins decide where to go? 🧠"</li>' +
    '<li>"How would I make enemies turn red when they chase me? 🔴"</li>' +
    '<li>"Explain the EnemyAI script to me 📜"</li>' +
    '</ul>' +
    '<p>I can read your game code and the spec, then explain it and suggest changes. ⚔️</p>' +
    '</div>';

  // ---- Tiny fetch helper that handles the auth gate ----------------------
  async function api(path, opts) {
    const res = await fetch(path, Object.assign({ credentials: 'same-origin' }, opts));
    if (res.status === 401) {
      window.location.href = '/login';
      throw new Error('Session expired');
    }
    return res;
  }

  async function apiJson(path, opts) {
    const res = await api(path, opts);
    if (!res.ok) {
      let msg = 'Request failed';
      try {
        const body = await res.json();
        if (body && body.error) msg = body.error;
      } catch (_) {
        /* ignore */
      }
      const err = new Error(msg);
      err.status = res.status;
      throw err;
    }
    return res.json();
  }

  // ---- Who am I + logout --------------------------------------------------
  async function loadMe() {
    try {
      const me = await apiJson('/api/me');
      $('#whoName').textContent = me.username || 'hero';
    } catch (_) {
      $('#whoName').textContent = '';
    }
  }

  function wireLogout() {
    const btn = $('#logoutBtn');
    btn.addEventListener('click', async () => {
      btn.disabled = true;
      try {
        await api('/api/logout', { method: 'POST' });
      } catch (_) {
        /* even if it fails, send them to the door */
      }
      window.location.href = '/login';
    });
  }

  // ---- Tabs (Spec / Code) ------------------------------------------------
  function wireTabs() {
    const tabs = document.querySelectorAll('.tab[data-tab]');
    const panels = document.querySelectorAll('.tab-panel[data-panel]');
    tabs.forEach((tab) => {
      tab.addEventListener('click', () => {
        const name = tab.dataset.tab;
        tabs.forEach((t) => t.classList.toggle('active', t === tab));
        panels.forEach((p) => {
          p.hidden = p.dataset.panel !== name;
        });
      });
    });
  }

  // ---- Spec panel --------------------------------------------------------
  async function loadSpec() {
    const body = $('#specBody');
    try {
      const data = await apiJson('/api/spec');
      body.innerHTML = data.html || '<p>The spec is empty.</p>';
    } catch (err) {
      body.innerHTML =
        '<p class="error-text">Could not load the spec: ' +
        escapeHtml(err.message) +
        '</p>';
    }
  }

  // ---- Code panel: tree + viewer -----------------------------------------
  let activeFileBtn = null;

  function makeFileButton(node) {
    const btn = document.createElement('button');
    btn.className = 'tree-item';
    btn.type = 'button';
    btn.textContent = '📄 ' + node.name;
    btn.title = node.path;
    btn.addEventListener('click', () => openFile(node.path, btn));
    return btn;
  }

  function makeFolder(node, depth) {
    const wrap = document.createElement('div');

    const head = document.createElement('button');
    head.className = 'tree-item tree-folder';
    head.type = 'button';
    head.style.paddingLeft = 6 + depth * 12 + 'px';

    const caret = document.createElement('span');
    caret.textContent = '▾ ';
    head.appendChild(caret);
    head.appendChild(document.createTextNode('📁 ' + node.name));

    const kids = document.createElement('div');
    kids.className = 'tree-children';
    renderNodes(node.children || [], kids, depth + 1);

    head.addEventListener('click', () => {
      const collapsed = kids.hidden;
      kids.hidden = !collapsed;
      caret.textContent = collapsed ? '▾ ' : '▸ ';
    });

    wrap.appendChild(head);
    wrap.appendChild(kids);
    return wrap;
  }

  function renderNodes(nodes, container, depth) {
    nodes.forEach((node) => {
      if (node.type === 'dir') {
        container.appendChild(makeFolder(node, depth));
      } else {
        const btn = makeFileButton(node);
        btn.style.paddingLeft = 6 + depth * 12 + 'px';
        container.appendChild(btn);
      }
    });
  }

  async function loadFileTree() {
    const tree = $('#fileTree');
    try {
      const data = await apiJson('/api/files');
      tree.innerHTML = '';
      const children = (data && data.children) || [];
      if (children.length === 0) {
        tree.innerHTML = '<p class="muted-text">No script files found.</p>';
        return;
      }
      renderNodes(children, tree, 0);
    } catch (err) {
      tree.innerHTML =
        '<p class="error-text">Could not load files: ' + escapeHtml(err.message) + '</p>';
    }
  }

  async function openFile(relPath, btn) {
    const pathEl = $('#codePath');
    const codeEl = $('#codeBody');

    if (activeFileBtn) activeFileBtn.classList.remove('active');
    if (btn) {
      btn.classList.add('active');
      activeFileBtn = btn;
    }

    pathEl.textContent = 'Loading ' + relPath + ' …';
    try {
      const data = await apiJson('/api/file?path=' + encodeURIComponent(relPath));
      pathEl.textContent = data.path || relPath;

      codeEl.textContent = data.content || '';
      codeEl.className = 'hljs';
      const lang = extToLang(data.ext);
      if (lang) codeEl.classList.add('language-' + lang);

      if (window.hljs && typeof window.hljs.highlightElement === 'function') {
        // Clear any prior highlight markers so re-highlight is clean.
        delete codeEl.dataset.highlighted;
        window.hljs.highlightElement(codeEl);
      }
    } catch (err) {
      pathEl.textContent = relPath;
      codeEl.className = '';
      codeEl.textContent = '⚠️  ' + err.message;
    }
  }

  function extToLang(ext) {
    switch ((ext || '').toLowerCase()) {
      case 'cs':
        return 'csharp';
      case 'js':
        return 'javascript';
      case 'ts':
        return 'typescript';
      case 'json':
        return 'json';
      case 'md':
        return 'markdown';
      case 'shader':
      case 'cginc':
      case 'hlsl':
        return 'glsl';
      case 'xml':
      case 'uxml':
        return 'xml';
      case 'uss':
        return 'css';
      case 'yml':
      case 'yaml':
        return 'yaml';
      default:
        return null;
    }
  }

  // ---- Game preview ------------------------------------------------------
  let lastAvailable = null;

  async function refreshGameStatus() {
    const statusEl = $('#buildStatus');
    const missingEl = $('#gameMissing');
    const frame = $('#gameFrame');
    try {
      const data = await apiJson('/api/game/status');
      if (data.available) {
        statusEl.textContent = '✅ Built ' + relativeTime(data.builtAt);
        statusEl.title = data.builtAt || '';
        missingEl.hidden = true;
        frame.style.display = '';
        if (lastAvailable === false) {
          // A build just appeared — load it.
          frame.src = '/game/index.html?t=' + Date.now();
        }
      } else {
        statusEl.textContent = '🛠️ No build yet';
        statusEl.title = '';
        missingEl.hidden = false;
        frame.style.display = 'none';
        frame.src = 'about:blank';
      }
      lastAvailable = !!data.available;
    } catch (err) {
      statusEl.textContent = '⚠️ Status check failed';
      statusEl.title = err.message;
    }
  }

  function wireReload() {
    const frame = $('#gameFrame');

    $('#reloadGame').addEventListener('click', () => {
      if (lastAvailable) {
        frame.src = '/game/index.html?t=' + Date.now();
      } else {
        refreshGameStatus();
      }
    });

    // "Play Big" — fullscreen the game so keyboard input is captured cleanly.
    const fsBtn = $('#fullscreenGame');
    if (fsBtn) {
      fsBtn.addEventListener('click', () => {
        if (frame.requestFullscreen) {
          frame.requestFullscreen().then(focusGame).catch(() => openGameTab());
        } else {
          openGameTab();
        }
      });
    }

    // Unity WebGL only receives key presses when its canvas/iframe has focus.
    // Clicking the game (or it finishing load) hands focus to it so WASD/arrows
    // work inline. We intentionally do NOT grab focus on hover, so the mouse
    // drifting over the game can't interrupt typing in the chat box.
    frame.addEventListener('load', focusGame);
    frame.addEventListener('pointerdown', focusGame);
  }

  function focusGame() {
    const frame = $('#gameFrame');
    try {
      frame.focus();
      if (frame.contentWindow) frame.contentWindow.focus();
    } catch (_) {
      /* cross-origin guard — same-origin here, so this normally succeeds */
    }
  }

  function openGameTab() {
    window.open('/game/index.html', '_blank', 'noopener');
  }

  // ---- Helpers -----------------------------------------------------------
  function relativeTime(iso) {
    if (!iso) return 'just now';
    const then = new Date(iso).getTime();
    if (isNaN(then)) return 'recently';
    const secs = Math.max(0, Math.floor((Date.now() - then) / 1000));
    if (secs < 60) return 'just now';
    const mins = Math.floor(secs / 60);
    if (mins < 60) return mins + 'm ago';
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return hrs + 'h ago';
    const days = Math.floor(hrs / 24);
    return days + 'd ago';
  }

  function escapeHtml(s) {
    return String(s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }

  // Build a friendly "what happened to the game" chip from the server's git
  // result. Returns a DOM node, or null when nothing code-worthy happened.
  function renderGameEdit(ge) {
    if (!ge || (!ge.changed && !ge.error)) return null;

    const el = document.createElement('div');
    el.className = 'game-edit';

    if (ge.error) {
      el.classList.add('warn');
      el.textContent = '⚠️ I answered, but couldn’t save the code change. Tell Jorge!';
      return el;
    }

    const files = Array.isArray(ge.files) ? ge.files : [];
    const n = files.length;
    const fileWord = n === 1 ? 'file' : 'files';
    const sha = ge.sha ? ' · ' + ge.sha : '';

    if (ge.conflict) {
      el.classList.add('warn');
      el.textContent = '⚠️ Updated ' + n + ' ' + fileWord + sha +
        ' — saved here, but couldn’t sync to main (a merge tangle). Jorge can sort it.';
    } else if (ge.pushed) {
      el.classList.add('ok');
      const how = ge.rebased ? ' (after a quick sync)' : '';
      el.textContent = '✅ Updated ' + n + ' ' + fileWord + sha + ' · pushed to main' + how +
        ' — your game is rebuilding! 🔨';
    } else {
      el.classList.add('ok');
      el.textContent = '✅ Updated ' + n + ' ' + fileWord + sha + ' · saved locally';
    }

    if (n) {
      const list = document.createElement('div');
      list.className = 'game-edit-files';
      list.textContent = files.slice(0, 6).join(', ') + (n > 6 ? ' …' : '');
      el.appendChild(list);
    }
    return el;
  }

  // ---- Copilot chat ------------------------------------------------------
  function scrollChat() {
    const body = $('#chatBody');
    if (body) body.scrollTop = body.scrollHeight;
  }

  function addBubble(kind, html) {
    const body = $('#chatBody');
    const el = document.createElement('div');
    el.className = 'chat-bubble ' + kind;
    el.innerHTML = html;
    body.appendChild(el);
    scrollChat();
    return el;
  }

  function setChatBusy(busy) {
    const input = $('#chatInput');
    const send = $('#chatSend');
    if (input) input.disabled = busy;
    if (send) send.disabled = busy;
    if (!busy && input) input.focus();
  }

  // Parse one SSE frame ("event: x\ndata: {...}") into { event, data }.
  function parseSseFrame(frame) {
    let event = 'message';
    const dataLines = [];
    for (const line of frame.split('\n')) {
      if (line.startsWith('event:')) event = line.slice(6).trim();
      else if (line.startsWith('data:')) dataLines.push(line.slice(5).replace(/^ /, ''));
    }
    if (!dataLines.length) return null;
    let data = null;
    try {
      data = JSON.parse(dataLines.join('\n'));
    } catch (_) {
      return null;
    }
    return { event, data };
  }

  async function sendChat(message) {
    addBubble('user', '<p>' + escapeHtml(message) + '</p>');

    const bot = addBubble(
      'bot thinking',
      '<p class="thinking">🤖 Copilot is forging an answer… 🔨</p>'
    );
    let streamed = '';
    let gotDelta = false;

    setChatBusy(true);
    try {
      const res = await api('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message, sessionId: chatSessionId }),
      });

      // Non-streaming error (e.g. 400/429) comes back as JSON, not SSE.
      if (!res.ok) {
        let msg = 'Something went wrong. Try again! 🔧';
        try {
          const body = await res.json();
          if (body && body.error) msg = body.error;
        } catch (_) {
          /* ignore */
        }
        bot.className = 'chat-bubble bot';
        bot.innerHTML = '<p class="chat-error">' + escapeHtml(msg) + '</p>';
        scrollChat();
        return;
      }

      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buf = '';

      for (;;) {
        const { value, done } = await reader.read();
        if (done) break;
        buf += decoder.decode(value, { stream: true });

        let sep;
        while ((sep = buf.indexOf('\n\n')) >= 0) {
          const frame = buf.slice(0, sep);
          buf = buf.slice(sep + 2);
          if (!frame.trim() || frame.startsWith(':')) continue;

          const parsed = parseSseFrame(frame);
          if (!parsed) continue;

          if (parsed.event === 'delta') {
            if (!gotDelta) {
              gotDelta = true;
              bot.className = 'chat-bubble bot';
              bot.textContent = '';
            }
            streamed += (parsed.data && parsed.data.text) || '';
            bot.textContent = streamed;
            scrollChat();
          } else if (parsed.event === 'done') {
            bot.className = 'chat-bubble bot';
            const html = parsed.data && parsed.data.html;
            if (html) bot.innerHTML = html;
            else if (streamed) bot.textContent = streamed;
            else bot.innerHTML = "<p>Hmm, I didn't catch that — try asking again! 🤔</p>";
            const pill = $('#chatModel');
            if (pill && parsed.data && parsed.data.model) pill.textContent = parsed.data.model;
            const ge = parsed.data && parsed.data.gameEdit;
            if (ge) {
              const node = renderGameEdit(ge);
              if (node) bot.appendChild(node);
            }
            scrollChat();
          } else if (parsed.event === 'error') {
            bot.className = 'chat-bubble bot';
            const errMsg = (parsed.data && parsed.data.error) || 'My forge sputtered. 🔧 Try again!';
            bot.innerHTML = '<p class="chat-error">' + escapeHtml(errMsg) + '</p>';
            scrollChat();
          }
        }
      }
    } catch (err) {
      bot.className = 'chat-bubble bot';
      bot.innerHTML = '<p class="chat-error">Couldn\'t reach Copilot. 📡 Check your connection and try again!</p>';
      scrollChat();
    } finally {
      setChatBusy(false);
    }
  }

  function startNewChat() {
    chatSessionId = newSessionId();
    const body = $('#chatBody');
    if (body) body.innerHTML = WELCOME_HTML;
    const input = $('#chatInput');
    if (input) {
      input.value = '';
      input.focus();
    }
  }

  function wireChat() {
    const form = $('#chatForm');
    const input = $('#chatInput');
    if (!form || !input) return;

    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const message = input.value.trim();
      if (!message) return;
      input.value = '';
      sendChat(message);
    });

    const newBtn = $('#newChat');
    if (newBtn) {
      newBtn.addEventListener('click', (e) => {
        e.preventDefault();
        startNewChat();
      });
    }
  }

  // ---- Boot --------------------------------------------------------------
  function init() {
    wireLogout();
    wireTabs();
    wireReload();
    wireChat();

    loadMe();
    loadSpec();
    loadFileTree();
    refreshGameStatus();

    // Keep the build status fresh so a freshly-forged game shows up on its own.
    setInterval(refreshGameStatus, 15000);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
