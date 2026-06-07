# Story Spec — *The Quiet That Came to Kindred Village*

The **meaning layer** for *Kindred Village: Council of Sages*. This spec turns the shipped loop — *click a sage, hear a quote, watch one meter fill to 100* — into a short, replayable game with a story, a reason to care, and a way to **win or lose**.

It is written to layer **on top of** [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md), not to replace it. It restores the depth your own [`CONCEPTS.md`](./CONCEPTS.md) (Option 2) already designed — action points, drifting trust, conflict, the loneliest villager — which the jam spec deliberately cut to ship a baseline fast. The baseline shipped. Now we put the game back in.

> **Read this first (scope note for Jorge):** [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md) §2 says *"concept lock — do not expand"* and lists day/night, economy, and timers as out of scope. **This spec consciously expands that lock.** That was the right call *to ship a baseline*; it is the wrong call *to ship a game worth judging*. Treat everything here as an **opt-in v2 enhancement**. The senior decides, per file, whether each system earns its place. Nothing here is "never cut" except the four anchors in §7.

---

## Acronyms (defined on first use)

| Acronym | Meaning |
|---|---|
| **AP** | Action Point — a unit of player time spent each day (the core scarcity resource) |
| **CAC** | Congressional App Challenge — annual U.S. House of Representatives student coding competition |
| **G4C** | Games for Change — nonprofit running the Student Challenge (games for social impact) |
| **MVP** | Minimum Viable Product — smallest working version still worth showing a judge |
| **NPC** | Non-Player Character — a villager controlled by code, not the player |
| **UI** | User Interface — the on-screen panels, bars, and buttons |
| **SFX** | Sound Effects |
| **JSON** | JavaScript Object Notation — a simple text format for data like quotes and events |
| **Trust** | A literal number, 0–100, representing how connected the village feels |
| **Drift** | The nightly downward pull on Trust — communities drift apart when neglected |
| **Loneliest Villager** | The NPC with the lowest standing; the game nudges the player to notice them |

---

## 1. Why the shipped game feels boring (the honest diagnosis)

The baseline is a **progress bar, not a game.** Three things are missing, and a player feels all three within 60 seconds:

| What's missing | Why it matters | What a player feels without it |
|---|---|---|
| **A reason to care** | No story, no stakes, no character in trouble | "Why am I clicking this?" |
| **A way to lose** | The meter only goes up | "I can't fail, so nothing I do matters." |
| **Scarcity** | Unlimited clicks | "There's no decision here, just clicking." |

A meter that only rises is the single most common first-game mistake. The fix is not *more content* — it's **friction**. A game is interesting exactly when the player can make a choice that turns out wrong. We add the cheapest possible systems that make a wrong choice *possible*.

> **The thesis, in one sentence (use this in the demo video and the CAC writeup):**
> *Communities drift apart from a thousand small silences, and heal one conversation at a time — so in this game, the message is the mechanic.*

---

## 2. The story (short and sweet)

**Title overlay:** *The Quiet That Came to Kindred Village.*

You are the new **Keeper of the Square** — the person a village quietly relies on to keep everyone connected. A slow quiet has settled over Kindred Village: neighbors who used to talk now pass each other in silence. **First frost is seven days away.** By tradition, the village holds a **Winter Feast** on the seventh night — but only if there's still a community warm enough to hold one.

You have seven days to bring the village back together. Each day you have only a little time (your **Action Points**). Spend it well, and the village gathers for the Feast. Spend it poorly — ignore the lonely, let small arguments fester — and the seventh night comes to a half-empty square.

That's the whole story. It fits on a title card. It gives every click a reason and every day a stake.

### Three-act shape (maps onto the 7 days)

| Act | Days | What happens | Player feeling |
|---|---|---|---|
| **Act 1 — The Quiet** | 1–2 | Learn the village. Trust is low and drifting. Meet the sages. | "Oh — it's slipping. I should act." |
| **Act 2 — The Fractures** | 3–5 | Small conflicts fire (someone felt left out). Triage them. | "I only have 3 AP and two problems. Which one?" |
| **Act 3 — The Feast** | 6–7 | Last pushes. The meter is close. Every choice counts. | "Do I make it?" |

---

## 3. The four cheap systems that fix it

Each system is a few lines of code on top of the existing Trust Meter. None requires art, multiplayer, or save/load. Listed cheapest-first.

### 3.1 Day clock (the lose deadline)
A counter `Day` from 1 to 7 and an **End Day** button. When the player ends Day 7, the game evaluates win/lose. *This alone* converts the meter from "fills eventually" to "fills **in time** or not." Faithful to `CONCEPTS.md` ("14 in-game days per run") — we use **7** for a tighter ~10-minute jam session.

### 3.2 Action Points (scarcity → real choices)
The player gets **3 AP per day**. Every meaningful action costs AP. When AP hits 0, the only option is **End Day**. Scarcity is what makes a choice a *choice* — three AP and four things worth doing is a game; unlimited clicks is a chore. Straight from `CONCEPTS.md` ("3 per day").

### 3.3 Trust drift (the meter can fall → tension)
At **End Day**, Community Trust drifts **down** by a base amount, plus extra if the **Loneliest Villager** was ignored that day. Now the meter is a tug-of-war, not an escalator. This is the `CONCEPTS.md` "drifting apart" mechanic — the reason you can't just max two villagers and coast.

### 3.4 Conflict events (Act 2 triage)
On Days 3, 4, and 5, one small conflict fires at the start of the day (*"Maya felt left out of the garden project."*). It costs Trust immediately and, optionally, keeps bleeding a little each day until repaired. The player must decide: spend AP to mend it now, or let it slide and push elsewhere? **That decision is the game.** From `CONCEPTS.md` ("Trust drops sharply after a conflict event").

---

## 4. Win, lose, and progression

| Outcome | Condition | Screen |
|---|---|---|
| **Win** | Community Trust **≥ 80** at the end of Day 7 | *The Winter Feast* — the square fills, villagers gather, warm music, **Play Again** |
| **Lose** | Community Trust **< 80** at the end of Day 7 | *A Quiet Winter* — a gentle, bittersweet card (not a punishment), one line of reflection, **Play Again** |

> **This supersedes [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md) §2's win condition.** Old win: *Trust fills to 100.* New win: *Trust ≥ 80 by the end of the season.* The 0–100 meter stays exactly as built; we change **when** it's checked and **add** a failing branch.

**Keeping the cozy promise.** The jam spec says *"no timers that punish."* We honor that: the 7-day clock is a **season**, not a stopwatch — there is no per-action countdown, no twitch pressure, no "game over, you're bad." The lose screen is *reflective and kind* ("the square was quiet this winter — try again?") and **Play Again** is one click. A soft, bittersweet loss is what gives the win meaning; it is not the punishing timer the spec warned against.

**Progression within a run** = the day clock plus a rising sense of mastery: by Day 4 the player has learned to read the village (who's drifting, which introduction will spark). **Progression across runs** = "beat your own Trust score" and *"this time I'll save Theo earlier."* That replay line is a `CONCEPTS.md` design goal and a G4C selling point.

---

## 5. The economy (starting playtest values — tune these)

A single **Community Trust** value (0–100) is the win/lose number — cheap, and it reuses the meter already in the build. We make it *feel* like the `CONCEPTS.md` web of pair-trust by deriving the nightly **Drift** partly from the **Loneliest Villager** (full N×N pair-trust is a stretch goal, §8).

**Anchors:** Start Trust = **45**. Win threshold = **80**. Season = **7 days**. AP = **3/day** (21 total).

> **Who are "villagers" vs "sages"?** The shipped baseline has the **3 sages** (the philosophers you click for quotes). For this layer, the sages are simply the first **villagers** — count them as villagers and, ideally, add **2–3 more** so the roster is **5–6** (matches the `CONCEPTS.md` "6–8 villagers"). The roster size matters for balance: see the note under **Introduce**.

| Action | AP | Trust change | Notes |
|---|---:|---:|---|
| **Listen to a Sage** | 1 | **+6** | The existing click-for-quote action, now priced |
| **Visit a villager** | 1 | **+6** | Raises that villager's standing; tends their loneliness |
| **Introduce two villagers** | 2 | **+14** | The big play — sparks a new bond (the `CONCEPTS.md` "+8 floats up"). **Once per pair only:** a pair already bonded can't be re-introduced, so you can't spam it — you must keep building a *wider* web (and with a small roster it runs out, pushing you to Visit/Host) |
| **Host a gathering** | 2 | **+10**, and **−2 to tomorrow's drift** | **Repeatable** community action. Lower burst than Introduce, but it never runs out and it *steadies the village* — the go-to once your pairs are spent or a conflict day is coming |
| **Listening Bench** *(stretch)* | 1 | **+8** right-fit / **+3** wrong-fit | Match the right villager to the day's prompt (skill check) |

> **Why Introduce isn't a "press +14 to win" button:** the once-per-pair rule means a 5-villager roster has 10 possible bonds total — plenty for a 7-day run, but each is a *one-time* play. Once you've made the obvious matches you're choosing between **Visit** (tend the lonely), **Host** (steady the village), and **Bench** (skill). That's the decision space the scarcity creates.

| Nightly event (at End Day) | Trust change |
|---|---:|
| **Base drift** | **−5** |
| **Loneliest Villager ignored today** | additional **−2** |
| **Conflict fires (Days 3–5)** | **−10** when it triggers |
| **Conflict left unrepaired** *(optional bleed)* | **−3/day** until mended — *cut this first if it feels harsh* |

### Worked simulation (does the balance hold?)

A **skilled** run — tends the Loneliest Villager, triages every conflict, never wastes an AP. Each normal day plays **Introduce (+14) + Visit the loneliest (+6) = +20**. On a conflict day, the mend *is* a Visit to an involved villager, so that AP can't also tend the loneliest → an extra **−2** drift (the triage cost). Trust **caps at 100**; the Day-7 drift applies **before** the final check.

| Day | Start | Conflict | Actions | Pre-drift | Drift | End |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 45 | — | +20 | 65 | −5 | 60 |
| 2 | 60 | — | +20 | 80 | −5 | 75 |
| 3 | 75 | −10 | +20 | 85 | −7\* | 78 |
| 4 | 78 | −10 | +20 | 88 | −7\* | 81 |
| 5 | 81 | −10 | +20 | 91 | −7\* | 84 |
| 6 | 84 | — | +20 | 100 (cap) | −5 | 95 |
| 7 | 95 | — | +20 | 100 (cap) | −5 | **95 → WIN** |

\* −7 because mending the conflict spent the AP that would have tended the Loneliest Villager.

A **mediocre** run — wastes ~1 AP/day, ignores the lonely (drift −7), lets conflicts bleed — nets roughly **+12 gained vs −7 drifted = +5/day**, then loses ~30 to the three conflicts: `45 + (7 × 5) − 30 ≈ 50` → **Lose.** With the optional bleed on, it sinks into the 20s.

→ The skilled run wins by a comfortable but **earned** margin; the careless run loses clearly; the game is decided in the **Act 2 triage** (Days 3–5), exactly where the story puts its tension. **Tuning levers if playtest feels off:** raise/lower *start* (opening difficulty), *threshold* (overall difficulty), *drift* (tension), or *conflict damage* (Act 2 weight). Change **one at a time.**

---

## 6. Implementation — promptable chunks (senior → junior → AI)

These extend [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md) §5. Same one-hour sprint shape, same review-out-loud discipline. The junior prompts the AI with each block; the senior narrates the teachable moment. Build them **in order** — each is playable on its own, so you can stop after any step and still have something better than the baseline.

> Unity guardrails for every prompt (from `goblin-build.agent.md`): Unity 6 LTS, **new Input System** (no `Input.GetKey`), `FindFirstObjectByType<T>()` (not `FindObjectOfType`), Canvas + TextMeshPro UI, **WebGL-safe** (no `System.IO.File`, no threads), `JsonUtility` for simple data.

### Step 1 — Day clock + win/lose check *(the keystone — do this first)*
> Add a `GameClock` that tracks `Day` (1–7) and an **End Day** button. Show "Day X of 7" in the UI. When the player ends Day 7, check Community Trust: if **≥ 80**, load a Win screen ("The Winter Feast"); otherwise load a Lose screen ("A Quiet Winter"). Both screens have a **Play Again** button that resets the game. Do not change how the Trust Meter rises yet.

*Teachable moment:* the AI will likely scatter the win check across the UI. Senior: *"Where should the one place that decides win/lose live, so a designer can find it?"* → a single `EvaluateEndOfSeason()` method.

### Step 2 — Action Points (scarcity)
> Add `ActionPoints`, starting at **3** each day. Each action spends AP **per the §5 table** — **Listen to a Sage** and **Visit a villager** cost **1 AP**, **Introduce** and **Host a gathering** cost **2 AP**. Show remaining AP in the UI. When AP is too low for an action, disable that action; when AP reaches 0, disable everything except **End Day**. Refill to 3 at the start of each day.

*Teachable moment:* the AI may forget to refill, or let actions go negative. Junior catches it in Play Mode. Senior: *"What's the one method every action should ask permission from first?"* → `TrySpendAP(int cost)`.

### Step 3 — Trust drift (tension)
> At **End Day**, before the win/lose check, reduce Community Trust by a **base drift of 5**. Show a one-line log: *"The village drifted a little quieter overnight (−5)."* Add a serialized `baseDrift` field so the value can be tuned in the Inspector without recompiling.

*Teachable moment:* magic numbers. Senior: *"Hardcode 5, or expose it? What happens when we playtest and 5 feels wrong?"* → `[SerializeField] int baseDrift = 5;`.

### Step 4 — Loneliest Villager nudge
> Give each villager a `standing` value (0–100). The **Loneliest Villager** is the one with the lowest standing; highlight them in the UI. **Visiting** a villager raises their standing. At End Day, if the Loneliest Villager was **not** visited that day, apply an **extra −2** drift. Show the reason in the log.

*Teachable moment:* the `CONCEPTS.md` "readability rule" — *every Trust change must show why.* Senior makes the junior add the log line before accepting the diff.

### Step 5 — Conflict events (Act 2 triage)
> Create `conflicts.json` with a few small, non-dark conflicts (e.g. *"Maya felt left out of the garden project"*). On **Days 3, 4, and 5**, fire one at the start of the day: subtract **10** Trust and show a card naming the villagers involved. **Visiting** (1 AP) **either** involved villager that day "mends" it (the +6 Visit still applies — mending and tending are the same action, which is why a conflict day costs you the loneliest-villager visit). *(Optional, behind a flag: an unmended conflict bleeds −3 at End Day until repaired.)*

*Teachable moment:* JSON-driven content again (same lesson as the quote system). Senior: *"We just learned this pattern with quotes — where do conflicts live so we add one without recompiling?"*

### Step 6 — Story framing + the two end screens
> Add a title card with the story (Keeper of the Square, 7 days to the Winter Feast). Polish the Win screen ("The Winter Feast" — warm, villagers gathered) and the Lose screen ("A Quiet Winter" — gentle, one reflective line, **Play Again**). Add the audio hooks specified in §9.

*Teachable moment:* tone. Senior: *"The lose screen — is it punishing or kind? Why does kind make the win mean more?"*

---

## 7. Scope-cut ladder (when time runs out)

Extends [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md) §6. If a session runs long, cut from the bottom:

1. Listening Bench (stretch — defer to v1.1)
2. Conflict "bleed" (one-time −10 only; drop the per-day bleed)
3. Loneliest-Villager extra drift (keep flat base drift)
4. Conflict events entirely (ship Days clock + AP + drift — *still a real game*)
5. Story end-screen art (a styled text card is fine)

**Never cut (the four anchors that make it a game):** the **7-day clock**, **Action Points**, **downward drift**, and a **lose screen**. Remove any one and it collapses back into the boring meter. Everything else is negotiable; these four are the game. *(This rule governs the **recommended meaning layer**. The "minimal stakes patch" in §11 — clock + win/lose only — is a deliberately smaller thing that ships *less* than the full game, not a meaning layer with anchors removed.)*

---

## 8. Stretch goals (only after the core loop is fun)

- **Pair-trust web.** The full `CONCEPTS.md` model: a 0–100 Trust value between *every pair* of villagers, drawn as little bars on each character card. Far richer, much heavier — only if the single-meter version is already fun.
- **Per-villager mood curves** on the end screen (the `CONCEPTS.md` MVP end-graph), so a player can narrate *which* villager they saved.
- **Listening Bench as the "Sage's Bench"** from `JAM_SPRINT_SPEC.md` §5 — the player types a real question, a sage answers. Doubles as the meaning-layer's emotional beat.

---

## 9. Audio brief (hand to the `audio-game-designer` agent)

Audio is half of "cozy." Don't hand-pick clips ad hoc — run the **`audio-game-designer`** agent (in `.github/agents/`) with this brief. It will source **CC0 / properly-licensed** audio, wire a WebGL-safe `AudioManager`, add a volume slider, and generate a `CREDITS.md`.

| Cue | Trigger | Feel | Priority |
|---|---|---|---|
| **Ambient music loop** | Always (village) | Warm, slow, hopeful; quietly thins as Trust drops, swells near the Feast | Must-have |
| **Listen-to-sage SFX** | Quote panel opens | Soft chime / page turn | Must-have |
| **Trust-up sting** | Any positive Trust change | Gentle major-key "ding" | Must-have |
| **Conflict sting** | A conflict fires (Days 3–5) | Brief, soft minor note — concern, not alarm | Nice-to-have |
| **Win cue** | Winter Feast screen | Warm, communal swell | Must-have |
| **Lose cue** | A Quiet Winter screen | Soft, bittersweet, *kind* — never harsh | Nice-to-have |

Maps onto `JAM_SPRINT_SPEC.md` §5 Sprint 3 (background music + click SFX). License rule: **CC0 needs no attribution but credit anyway; CC-BY must be attributed** in `CREDITS.md` and the itch.io page. **Caveat:** the agent must be run from the Unity project folder (the one containing `Assets/`), which is **not** this repo — see §11.

---

## 10. How this maps to the competitions

- **G4C (Games for Change):** the rubric rewards games whose **mechanics teach** — not games *about* a message. Here the message *is* the mechanic (a kind word raises Trust; silence lowers it). The `CONCEPTS.md` GAMERS and TAGG-loop framings apply directly; overlay those letters on demo clips.
- **CAC (Congressional App Challenge):** ships as the same WebGL build. The "Why I built it" answer writes itself from the thesis in §1. The bittersweet lose screen is exactly the reflective, non-violent design CAC judges reward.
- **The demo video (≈half the CAC rubric):** show a player hit a Day-4 conflict with only 3 AP and *choose*. That one decision, narrated aloud, sells the whole game.

---

## 11. Open questions for Jorge (decide before building)

1. **Which scope?** This expands the §2 lock of [`JAM_SPRINT_SPEC.md`](./JAM_SPRINT_SPEC.md). Two honest options:
   - **Minimal stakes patch** — *Step 1 only* (day clock + win/lose screens). Cheapest possible upgrade: it gives the baseline an ending and a way to fail, which is a real improvement. **But be clear-eyed: this is not the full game.** Without AP and drift there's still no scarcity and no tension — it's a progress bar with a finish line. Ship this only if time is truly gone.
   - **Recommended meaning layer** — *Steps 1–4 at minimum* (clock + AP + drift + loneliest nudge), conflicts if time allows. This is the version worth putting in front of a judge, and the one the §7 "four anchors" rule protects.

   The "**never cut**" rule in §7 applies **within the recommended layer**: *if* you commit to the meaning layer, don't cut the four anchors out of it. Choosing the minimal patch instead is a separate, conscious decision to ship something smaller — not a partial meaning layer.
2. **The Unity project lives outside this repo.** This tutoring repo has the specs and agents but **not** the game build (no `Assets/`, no `.unity`). To build these systems and add audio, point the agents at the real project — by copying `.github/agents/*.agent.md` into that project's `.github/agents/` (or into user-global `~/.copilot/agents/`) and **restarting the Copilot CLI** so they load. What's the path to the Unity project?
3. **Day count.** 7 (tighter, ~10-min jam session, recommended) or 14 (matches the `CONCEPTS.md` MVP)?

---

*Spec authored as a senior would author it, for a junior to execute against, with AI as the third pair of hands. It layers onto `JAM_SPRINT_SPEC.md` and restores the depth of `CONCEPTS.md`; it does not replace either.*
