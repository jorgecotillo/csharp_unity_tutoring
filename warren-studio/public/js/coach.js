'use strict';

// Build Coach — turns the ~6-minute build wait into a teaching moment for Warren.
//
// DESIGN GOAL: be COMPLETELY isolated from the build machinery. This module does
// not touch build.js, the polling loop, or setBuildActive. It only *watches* the
// existing #rebuildOverlay via a MutationObserver on its `hidden` attribute:
//   overlay shown  (build started)  -> start the coach (prediction + lessons)
//   overlay hidden (build finished) -> stop; if a prediction was locked, pop the
//                                      "were you right?" verify toast over the game.
// So if anything here ever misbehaves, the build/preview still works exactly as
// before — the coach is pure additive UI.
//
// The lessons are tied to Warren's own Goblin Siege game and the real bugs he hit
// (the door spawning in the back = coordinates; key 4 doing nothing = variables +
// state; the missing boom = events). That grounding is what makes them stick.

(function () {
  var $ = function (id) { return document.getElementById(id); };

  var overlay = $('rebuildOverlay');
  if (!overlay) return; // nothing to attach to

  // ---- Lesson deck ---------------------------------------------------------
  // Each: eyebrow title, kid-friendly body, and a Socratic "ask/try" prompt the
  // teacher (Jorge) can throw to Warren. Kept short — a compile is not a lecture.
  var LESSONS = [
    {
      title: '🧙 Your code is being TRANSLATED',
      body: "Right now your C# is being turned into WebAssembly — the secret language web browsers actually run. You wrote it human-style; the computer is translating it line by line. That translation is called <b>compiling</b>.",
      ask: '🎯 Ask: why might translating a whole game take a few minutes?'
    },
    {
      title: '📍 Everything has an address: (x, y, z)',
      body: "Remember the door that spawned way in the <i>back</i>? Every object in your game lives at a coordinate — <b>(x, y, z)</b>. Its z was too big, so it appeared far away. Move the number, move the door.",
      ask: '🎯 Try: which number would slide the gate left or right?'
    },
    {
      title: '🔢 Variables are labeled boxes',
      body: "“Max 3 squads” was a <b>variable</b> — a labeled box holding a number. Key 4 did nothing until you bumped that box to 4. Your game only knows the numbers you give it.",
      ask: '🎯 Ask: what else in your game is secretly just a number you could change?'
    },
    {
      title: '💾 Programs REMEMBER (that\u2019s state)',
      body: "Your game remembers things between plays — that\u2019s <b>state</b>. The old save still “remembered” 3 squads, so you had to clear it. The code AND the memory both have to agree.",
      ask: '🎯 Ask: why didn\u2019t the 4th squad show up until you threw away the old save?'
    },
    {
      title: '🔊 Events shout, listeners answer',
      body: "When your door breaks, the game shouts an <b>event</b>: “DOOR DESTROYED!” Something has to be <i>listening</i> to play the boom. No listener = silence. That\u2019s why the sound was missing.",
      ask: '🎯 Ask: who should be listening for the door-break event?'
    },
    {
      title: '⚙️ Your game has a heartbeat',
      body: "<code>Update()</code> runs about <b>60 times every second</b>. Each beat it moves goblins, checks who got hit, and redraws the screen. 60 tiny pictures a second = smooth motion.",
      ask: '🎯 Ask: if it ran only 5 times a second, how would the game feel?'
    },
    {
      title: '🧩 Functions = reusable moves',
      body: "A <b>function</b> is a named move you can reuse — like <code>Attack()</code> or <code>BuildWall()</code>. Write it once, call it a hundred times. Less typing, and fewer places for bugs to hide.",
      ask: '🎯 Ask: what action in your game happens over and over and could be one function?'
    },
    {
      title: '🐛 Debugging is detective work',
      body: "The door, the missing squad, the silent boom — you <b>solved</b> every one. That\u2019s the real skill: change one thing, test it, learn from it, repeat. Pros do exactly this all day.",
      ask: '🎯 Ask: what\u2019s the FIRST thing you\u2019ll check the moment it loads?'
    }
  ];

  var ROTATE_MS = 14000;

  // ---- Element handles (all optional-safe) --------------------------------
  var predictWrap = $('coachPredict');
  var predictInput = $('coachPredictInput');
  var predictLock = $('coachPredictLock');
  var predictSkip = $('coachPredictSkip');
  var lockedWrap = $('coachLocked');
  var lockedText = $('coachLockedText');
  var lessonTitle = $('coachLessonTitle');
  var lessonBody = $('coachLessonBody');
  var lessonAsk = $('coachLessonAsk');
  var dotsEl = $('coachDots');
  var prevBtn = $('coachPrev');
  var nextBtn = $('coachNext');

  var verifyWrap = $('coachVerify');
  var verifyPred = $('coachVerifyPred');
  var verifyYes = $('coachVerifyYes');
  var verifyNo = $('coachVerifyNo');
  var verifyClose = $('coachVerifyClose');
  var verifyNote = $('coachVerifyNote');

  // ---- State ---------------------------------------------------------------
  var idx = 0;
  var rotateTimer = null;
  var running = false;
  var prediction = '';
  var lockedThisBuild = false;

  // ---- Lesson rendering ----------------------------------------------------
  function renderLesson() {
    var l = LESSONS[idx];
    if (!l) return;
    if (lessonTitle) lessonTitle.innerHTML = l.title;
    if (lessonBody) lessonBody.innerHTML = l.body;
    if (lessonAsk) lessonAsk.textContent = l.ask;
    if (dotsEl) {
      var s = '';
      for (var i = 0; i < LESSONS.length; i++) s += (i === idx ? '\u25CF' : '\u25CB');
      dotsEl.textContent = s;
    }
    // little fade-in
    if (lessonBody) {
      var card = lessonBody.parentNode;
      if (card) { card.classList.remove('coach-pop'); void card.offsetWidth; card.classList.add('coach-pop'); }
    }
  }
  function go(delta) {
    idx = (idx + delta + LESSONS.length) % LESSONS.length;
    renderLesson();
    restartRotate(); // manual nav resets the auto-advance clock
  }
  function restartRotate() {
    if (rotateTimer) clearInterval(rotateTimer);
    rotateTimer = setInterval(function () { go(1); }, ROTATE_MS);
  }
  function stopRotate() {
    if (rotateTimer) { clearInterval(rotateTimer); rotateTimer = null; }
  }

  // ---- Prediction ----------------------------------------------------------
  function resetPredict() {
    prediction = '';
    lockedThisBuild = false;
    if (predictInput) predictInput.value = '';
    if (predictWrap) predictWrap.hidden = false;
    if (lockedWrap) lockedWrap.hidden = true;
  }
  function lockPrediction() {
    var v = (predictInput && predictInput.value ? predictInput.value : '').trim();
    if (!v) { if (predictInput) predictInput.focus(); return; }
    prediction = v;
    lockedThisBuild = true;
    if (lockedText) lockedText.textContent = v;
    if (predictWrap) predictWrap.hidden = true;
    if (lockedWrap) lockedWrap.hidden = false;
  }
  function skipPrediction() {
    lockedThisBuild = false;
    if (predictWrap) predictWrap.hidden = true;
    if (lockedWrap) lockedWrap.hidden = true;
  }

  // ---- Verify toast (after the build) -------------------------------------
  var verifyDismissTimer = null;
  function showVerify(text) {
    if (!verifyWrap) return;
    if (verifyPred) verifyPred.textContent = '\u201C' + text + '\u201D';
    if (verifyNote) { verifyNote.hidden = true; verifyNote.textContent = ''; }
    verifyWrap.hidden = false;
    // auto-dismiss after a while so it never lingers over the game
    if (verifyDismissTimer) clearTimeout(verifyDismissTimer);
    verifyDismissTimer = setTimeout(hideVerify, 30000);
  }
  function hideVerify() {
    if (verifyDismissTimer) { clearTimeout(verifyDismissTimer); verifyDismissTimer = null; }
    if (verifyWrap) verifyWrap.hidden = true;
  }
  function verifyAnswer(right) {
    if (verifyNote) {
      verifyNote.hidden = false;
      verifyNote.textContent = right
        ? '🏆 Boom! You called it. That\u2019s exactly how engineers think — predict, then prove it.'
        : '💡 Nice — a surprise means you just learned something new. What actually happened?';
    }
    if (verifyDismissTimer) clearTimeout(verifyDismissTimer);
    verifyDismissTimer = setTimeout(hideVerify, 8000);
  }

  // ---- Build lifecycle -----------------------------------------------------
  function startCoach() {
    if (running) return;
    running = true;
    hideVerify();
    resetPredict();
    idx = Math.floor(Math.random() * LESSONS.length);
    renderLesson();
    restartRotate();
  }
  function stopCoach() {
    if (!running) return;
    running = false;
    stopRotate();
    // The teachable payoff: if Warren locked a guess, ask him to check it.
    if (lockedThisBuild && prediction) showVerify(prediction);
    lockedThisBuild = false;
  }

  // ---- Wire controls -------------------------------------------------------
  if (predictLock) predictLock.addEventListener('click', lockPrediction);
  if (predictSkip) predictSkip.addEventListener('click', skipPrediction);
  if (predictInput) predictInput.addEventListener('keydown', function (e) {
    if (e.key === 'Enter') { e.preventDefault(); lockPrediction(); }
  });
  if (prevBtn) prevBtn.addEventListener('click', function () { go(-1); });
  if (nextBtn) nextBtn.addEventListener('click', function () { go(1); });
  if (verifyYes) verifyYes.addEventListener('click', function () { verifyAnswer(true); });
  if (verifyNo) verifyNo.addEventListener('click', function () { verifyAnswer(false); });
  if (verifyClose) verifyClose.addEventListener('click', hideVerify);

  // ---- Observe the overlay (our ONLY coupling to the build) ----------------
  function overlayShown() { return !overlay.hasAttribute('hidden'); }
  var lastShown = overlayShown();
  if (lastShown) startCoach(); // in case a build is already running at load

  var mo = new MutationObserver(function () {
    var shown = overlayShown();
    if (shown === lastShown) return;
    lastShown = shown;
    if (shown) startCoach(); else stopCoach();
  });
  mo.observe(overlay, { attributes: true, attributeFilter: ['hidden'] });

  // Pause rotation when the tab is hidden (saves CPU); resume when back.
  document.addEventListener('visibilitychange', function () {
    if (!running) return;
    if (document.hidden) stopRotate(); else restartRotate();
  });
})();
