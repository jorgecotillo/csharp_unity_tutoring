'use strict';

// Warren's Game Studio — front-of-house behavior for the 3-pane shell.
// Talks to the gated API in server.js. Any 401 means the session lapsed, so we
// bounce back to the login door.

(function () {
  const $ = (sel) => document.querySelector(sel);

  // One chat session id for the whole conversation, so Copilot remembers what
  // we already talked about (multi-turn memory). "New chat" rotates this.
  let chatSessionId = newSessionId();

  // A screenshot Warren pasted / dropped / picked, waiting to be sent with the
  // next message. Held as a base64 data URL.
  let pendingImage = null;
  let pendingImageName = '';
  const MAX_IMAGE_MB = 10;

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
  let lastBuiltAt = null;

  // Auto-build (rebuild-in-progress) tracking.
  let buildActive = false;
  let buildStartedAt = null; // ms epoch
  let buildTicker = null; // 1s interval that updates the elapsed display

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
        // Reload the iframe when a build first appears OR when a fresh build
        // (new builtAt) lands after a rebuild — so Warren sees his change.
        const builtChanged = data.builtAt && data.builtAt !== lastBuiltAt;
        if (lastAvailable === false || (lastAvailable && builtChanged)) {
          frame.src = '/game/index.html?t=' + Date.now();
        }
        lastBuiltAt = data.builtAt || lastBuiltAt;
      } else {
        statusEl.textContent = '🛠️ No build yet';
        statusEl.title = '';
        missingEl.hidden = false;
        frame.style.display = 'none';
        frame.src = 'about:blank';
        lastBuiltAt = null;
      }
      lastAvailable = !!data.available;
      updateBuildUI(data.build);
    } catch (err) {
      statusEl.textContent = '⚠️ Status check failed';
      statusEl.title = err.message;
    }
  }

  // Drive the "rebuilding your game" animation from the server's build state.
  function updateBuildUI(b) {
    const building = !!(b && b.building);
    const startedIso = b && b.startedAt;
    setBuildActive(building, startedIso);
  }

  // Show/hide the rebuild overlay + spin up a local 1s elapsed-time ticker so
  // the animation feels alive between the (slower) status polls.
  function setBuildActive(active, startedIso) {
    const overlay = $('#rebuildOverlay');
    const statusEl = $('#buildStatus');

    if (active && !buildActive) {
      // transition idle -> building
      buildStartedAt = startedIso ? new Date(startedIso).getTime() : Date.now();
      if (overlay) overlay.hidden = false;
      startBuildTicker();
      // Poll faster while building so the "done -> reload" is snappy.
      scheduleNextPoll();
    } else if (!active && buildActive) {
      // transition building -> done
      if (overlay) overlay.hidden = true;
      stopBuildTicker();
      buildStartedAt = null;
      scheduleNextPoll();
    } else if (active && buildActive && startedIso) {
      // still building; keep the start time in sync (in case a queued build
      // restarted the clock)
      const t = new Date(startedIso).getTime();
      if (!isNaN(t)) buildStartedAt = t;
    }
    buildActive = active;
    if (statusEl && active) {
      statusEl.textContent = '🔨 Rebuilding… ' + formatElapsed(buildStartedAt);
      statusEl.title = 'Auto-rebuilding your game';
    }
  }

  function startBuildTicker() {
    stopBuildTicker();
    tickBuild();
    buildTicker = setInterval(tickBuild, 1000);
  }
  function stopBuildTicker() {
    if (buildTicker) { clearInterval(buildTicker); buildTicker = null; }
  }
  function tickBuild() {
    const el = $('#rebuildTime');
    const statusEl = $('#buildStatus');
    const txt = formatElapsed(buildStartedAt);
    if (el) el.textContent = txt;
    if (statusEl && buildActive) statusEl.textContent = '🔨 Rebuilding… ' + txt;
  }
  function formatElapsed(startMs) {
    if (!startMs) return '0:00';
    const secs = Math.max(0, Math.floor((Date.now() - startMs) / 1000));
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return m + ':' + (s < 10 ? '0' + s : s);
  }

  // Optimistically show the overlay the instant a chat reply reports it kicked
  // off a rebuild, without waiting for the next status poll.
  function showRebuildingNow() {
    setBuildActive(true, new Date().toISOString());
  }

  // Adaptive status polling: fast while a rebuild is running (so the reload is
  // snappy), relaxed when idle.
  let pollTimer = null;
  function scheduleNextPoll() {
    if (pollTimer) clearTimeout(pollTimer);
    const delay = buildActive ? 4000 : 15000;
    pollTimer = setTimeout(async () => {
      await refreshGameStatus();
      scheduleNextPoll();
    }, delay);
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

    if (ge.rebuilding) {
      const rb = document.createElement('div');
      rb.className = 'game-edit-rebuild';
      rb.textContent = ge.rebuildQueued
        ? '🔨 Queued a rebuild — the preview refreshes after the current one finishes.'
        : '🔨 Rebuilding your game — the preview refreshes on its own in a few minutes!';
      el.appendChild(rb);
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
    const attach = $('#attachBtn');
    if (input) input.disabled = busy;
    if (send) send.disabled = busy;
    if (attach) attach.disabled = busy;
    if (!busy && input) input.focus();
  }

  // ---- Image attachments (paste / drag-drop / 📎 button) -----------------
  function setPendingImage(dataUrl, name) {
    pendingImage = dataUrl;
    pendingImageName = name || 'screenshot.png';
    const wrap = $('#imgPreview');
    const thumb = $('#imgPreviewThumb');
    const nameEl = $('#imgPreviewName');
    if (thumb) thumb.src = dataUrl;
    if (nameEl) nameEl.textContent = pendingImageName;
    if (wrap) wrap.hidden = false;
    scrollChat();
  }

  function clearPendingImage() {
    pendingImage = null;
    pendingImageName = '';
    const wrap = $('#imgPreview');
    const thumb = $('#imgPreviewThumb');
    if (thumb) thumb.removeAttribute('src');
    if (wrap) wrap.hidden = true;
    const fileInput = $('#imgFileInput');
    if (fileInput) fileInput.value = '';
  }

  function isAllowedImage(file) {
    return !!file && /^image\/(png|jpe?g|webp|gif)$/i.test(file.type || '');
  }

  // Read a File/Blob into a data URL and stage it as the pending screenshot.
  function readImageFile(file) {
    if (!isAllowedImage(file)) {
      addBubble('bot', '<p class="chat-error">Only images (PNG, JPG, WebP, GIF) can be attached. 🖼️</p>');
      return;
    }
    if (file.size > MAX_IMAGE_MB * 1024 * 1024) {
      addBubble('bot', '<p class="chat-error">That image is a bit big — keep it under ' + MAX_IMAGE_MB + ' MB. 📦</p>');
      return;
    }
    const reader = new FileReader();
    reader.onload = () => setPendingImage(String(reader.result), file.name || 'screenshot.png');
    reader.onerror = () => addBubble('bot', '<p class="chat-error">Couldn\'t read that image. Try again! 🖼️</p>');
    reader.readAsDataURL(file);
  }

  function wireImageAttach() {
    const attachBtn = $('#attachBtn');
    const fileInput = $('#imgFileInput');
    const removeBtn = $('#imgPreviewRemove');
    const input = $('#chatInput');
    const pane = document.querySelector('.pane-right');
    const dropHint = $('#chatDropHint');

    if (attachBtn && fileInput) {
      attachBtn.addEventListener('click', () => fileInput.click());
      fileInput.addEventListener('change', () => {
        const f = fileInput.files && fileInput.files[0];
        if (f) readImageFile(f);
      });
    }
    if (removeBtn) removeBtn.addEventListener('click', clearPendingImage);

    // Paste a screenshot from the clipboard (Ctrl+V) while typing in the chat.
    if (input) {
      input.addEventListener('paste', (e) => {
        const items = (e.clipboardData && e.clipboardData.items) || [];
        for (const it of items) {
          if (it.kind === 'file' && /^image\//i.test(it.type)) {
            const f = it.getAsFile();
            if (f) { e.preventDefault(); readImageFile(f); return; }
          }
        }
      });
    }

    // Drag-drop a screenshot anywhere on the chat pane.
    if (pane) {
      let dragDepth = 0;
      const hasFiles = (e) => !!(e.dataTransfer && Array.from(e.dataTransfer.types || []).indexOf('Files') !== -1);
      const showHint = (show) => { if (dropHint) dropHint.hidden = !show; };
      pane.addEventListener('dragenter', (e) => {
        if (hasFiles(e)) { e.preventDefault(); dragDepth++; showHint(true); }
      });
      pane.addEventListener('dragover', (e) => { if (hasFiles(e)) e.preventDefault(); });
      pane.addEventListener('dragleave', () => {
        dragDepth = Math.max(0, dragDepth - 1);
        if (dragDepth === 0) showHint(false);
      });
      pane.addEventListener('drop', (e) => {
        if (!hasFiles(e)) return;
        e.preventDefault();
        dragDepth = 0;
        showHint(false);
        const f = e.dataTransfer.files && e.dataTransfer.files[0];
        if (f) readImageFile(f);
      });
    }
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

  async function sendChat(message, image) {
    // User bubble shows the screenshot (if any) above the typed text.
    let userHtml = '';
    if (image) userHtml += '<img class="chat-img" src="' + image + '" alt="attached screenshot" />';
    if (message) userHtml += '<p>' + escapeHtml(message) + '</p>';
    addBubble('user', userHtml || '<p>🖼️</p>');

    // Build a "working" bubble with a live activity line (what Copilot is doing
    // right now) + a seconds counter, and a separate area the answer streams
    // into. This is what turns the old frozen "thinking…" box into something
    // Warren can actually watch. ⚔️
    const bot = addBubble('bot working', '');
    bot.innerHTML = '';

    const statusRow = document.createElement('div');
    statusRow.className = 'chat-status';
    const spin = document.createElement('span');
    spin.className = 'status-spin';
    spin.textContent = '⚙️';
    const label = document.createElement('span');
    label.className = 'status-label';
    label.textContent = '🤖 Warming up the forge…';
    const timeEl = document.createElement('span');
    timeEl.className = 'status-time';
    statusRow.appendChild(spin);
    statusRow.appendChild(label);
    statusRow.appendChild(timeEl);

    const stream = document.createElement('div');
    stream.className = 'chat-stream';

    bot.appendChild(statusRow);
    bot.appendChild(stream);
    scrollChat();

    // Seconds counter so the bubble is visibly alive even during a long, quiet
    // stretch (cold model start, big file edit) when no events are arriving.
    const started = Date.now();
    function tick() {
      const secs = Math.floor((Date.now() - started) / 1000);
      timeEl.textContent = secs > 0 ? '· ' + secs + 's' : '';
    }
    tick();
    const timer = setInterval(tick, 1000);
    let timerStopped = false;
    function stopTimer() {
      if (timerStopped) return;
      timerStopped = true;
      clearInterval(timer);
    }

    let streamed = '';
    let gotDelta = false;

    setChatBusy(true);
    try {
      const payload = { message, sessionId: chatSessionId };
      if (image) payload.image = image;
      const res = await api('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      // Non-streaming error (e.g. 400/429) comes back as JSON, not SSE.
      if (!res.ok) {
        stopTimer();
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

          if (parsed.event === 'status') {
            // Live activity: "📖 Reading Enemy.cs", "✏️ Editing Player.cs", …
            const text = parsed.data && parsed.data.label;
            if (text) label.textContent = text;
            scrollChat();
          } else if (parsed.event === 'delta') {
            if (!gotDelta) {
              gotDelta = true;
              label.textContent = '✍️ Writing the answer…';
            }
            streamed += (parsed.data && parsed.data.text) || '';
            stream.textContent = streamed;
            scrollChat();
          } else if (parsed.event === 'done') {
            stopTimer();
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
              if (ge.rebuilding) showRebuildingNow();
            }
            scrollChat();
          } else if (parsed.event === 'error') {
            stopTimer();
            bot.className = 'chat-bubble bot';
            const errMsg = (parsed.data && parsed.data.error) || 'My forge sputtered. 🔧 Try again!';
            bot.innerHTML = '<p class="chat-error">' + escapeHtml(errMsg) + '</p>';
            scrollChat();
          }
        }
      }
    } catch (err) {
      stopTimer();
      bot.className = 'chat-bubble bot';
      bot.innerHTML = '<p class="chat-error">Couldn\'t reach Copilot. 📡 Check your connection and try again!</p>';
      scrollChat();
    } finally {
      stopTimer();
      setChatBusy(false);
    }
  }

  function startNewChat() {
    chatSessionId = newSessionId();
    clearPendingImage();
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
      const image = pendingImage; // snapshot before we clear the preview
      if (!message && !image) return;
      input.value = '';
      clearPendingImage();
      sendChat(message, image);
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
    wireImageAttach();

    loadMe();
    loadSpec();
    loadFileTree();
    refreshGameStatus();

    // Keep the build status fresh so a freshly-forged game shows up on its own.
    // Adaptive: polls faster while an auto-rebuild is in progress.
    scheduleNextPoll();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
