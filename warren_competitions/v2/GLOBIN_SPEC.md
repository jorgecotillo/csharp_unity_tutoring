# Game Spec — Globin: Good Manners Win

A sprint plan to ship a Unity 2D arcade-strategy game to **itch.io**, submit it to the **Congressional App Challenge (CAC)**, and build a portfolio piece for the **NYU Game Center pipeline**. Each sprint is one Sunday session, one hour. The plan is built around an AI-assisted, mentor-led workflow inspired by Russinovich & Hanselman's *Redefining the Software Engineering Profession for AI* (Communications of the ACM, 2026, DOI: 10.1145/3779312).

---

## Acronyms (defined on first use)

| Acronym | Meaning |
|---|---|
| **CAC** | Congressional App Challenge — annual U.S. House of Representatives student coding competition |
| **G4C** | Games for Change — nonprofit that runs the Student Challenge for social-impact games |
| **MVP** | Minimum Viable Product — the smallest working version that's still worth shipping |
| **NPC** | Non-Player Character — characters in a game controlled by code, not the player |
| **URP** | Universal Render Pipeline — Unity's modern, performance-friendly graphics pipeline |
| **WebGL** | Web Graphics Library — lets a Unity game run inside a browser tab |
| **SFX** | Sound Effects |
| **JSON** | JavaScript Object Notation — a simple text format for storing data like level configs |
| **FSM** | Finite State Machine — a programming pattern where an NPC has a set of states (idle, cooperating, hostile) and rules for switching between them |
| **CLI** | Command-Line Interface |
| **Copilot CLI** | GitHub's terminal-based AI coding agent (the tool generating this very spec) |
| **Ralph loop** | A spec-first iterative loop where a written spec drives an AI agent to generate, test, and refine code in cycles |
| **EiC** | Early-in-Career engineer (term used in the Russinovich/Hanselman article) |
| **RTS** | Real-Time Strategy — a game genre where events happen continuously (not turn-by-turn) |
| **WBWWB** | We Become What We Behold — Nicky Case's 2016 game about media feedback loops |
| **EoT** | Evolution of Trust — Nicky Case's interactive game-theory explainer |

---

## 1. Why this spec is shaped this way (the Russinovich frame)

Same preceptorship model as the Kindred Village spec:

| Role | Who | What they do |
|---|---|---|
| **Senior / Preceptor** | Jorge | Owns architecture. Writes specs. Reviews every AI diff out loud. Catches the mistakes AI makes. Decides what gets merged. |
| **EiC / Apprentice** | The student | Writes the prompt. Runs the AI agent. Reads the diff. Tests in Play Mode. Reports what broke. Iterates. |
| **AI agent** | Copilot CLI / Ralph loop | Implements. Refactors. Generates boilerplate. Suggests next steps. Never the final decider. |

The key difference from Kindred Village: **this game has real game-theory underpinnings.** Each sprint includes a "game-theory moment" where Jorge explains the real-world concept behind the mechanic you just implemented. You're not just learning to code — you're learning game theory through building.

---

## 2. Design inspirations — where the ideas come from

### We Become What We Behold (Nicky Case, 2016)

A 5-minute game where the player is a cameraman photographing a crowd of circle and square people. What you photograph gets displayed on a TV screen; the crowd reacts to what they see. Photograph anger → anger spreads. Photograph love → love gets shamed off screen because "who tunes in to watch people get along?"

**What Globin borrows:**
- **The Amplify mechanic.** Globin can broadcast interactions to nearby NPCs. What gets amplified shapes the world — cooperation spreads cooperation; conflict spreads conflict.
- **Feedback loops as the core engine.** Small choices cascade. The game's message emerges from mechanics, not from cutscenes or text.
- **Simple mechanics, deep message.** One verb (take a photo) carries the entire game. Globin aims for the same economy: a few social actions, a universe of consequences.

**What Globin does NOT borrow:**
- The 5-minute length (Globin is replayable, 30+ minutes across levels)
- The inevitably dark ending (Globin proves cooperation *can* win)
- The passive-observer framing (Globin is an active participant, not just a camera)

### The Evolution of Trust (Nicky Case, 2017)

An interactive web explainer that teaches the iterated Prisoner's Dilemma through play. You play rounds of cooperate-or-cheat against NPCs that use named strategies:

| Strategy | Behavior |
|---|---|
| **Always Cooperate** | Always cooperates, even if you cheat them |
| **Always Cheat** | Always defects, never cooperates |
| **Copycat** (tit-for-tat) | Cooperates first, then mirrors your last move |
| **Grudger** | Cooperates until you cheat once — then never cooperates again |
| **Copykitten** | Like Copycat but forgives one mistake before retaliating |

Key insight: **Copycat (tit-for-tat) consistently wins tournaments** because it's nice (cooperates first), retaliatory (punishes cheating), forgiving (goes back to cooperating once you do), and clear (its behavior is predictable).

**What Globin borrows:**
- **Named NPC strategies** as literal game characters. Each NPC type in Globin IS one of these strategies, behaving exactly as game theory predicts.
- **The core lesson:** cooperation is not naive — it's the winning strategy, IF you do it right.
- **The simulation engine:** NPCs interact with each other (not just with the player), creating emergent social dynamics.

**What Globin does NOT borrow:**
- The slideshow/essay format (Globin is a game you play, not a lesson you read)
- The abstract circles (Globin has a character, a world, and levels)

### Pac-Man (Namco, 1980)

The game should feel linear "like Pac-Man." What that means for Globin:

- **Level-based progression.** Each level is a self-contained arena with a clear win condition. Complete it, move to the next.
- **Real-time action.** Things happen continuously. The player moves and acts in real time; NPCs don't wait for the player's turn.
- **Simple controls, deep strategy.** Pac-Man has one verb (move) and four enemies with distinct AI behaviors. Globin has four verbs (Wave, Share, Shield, Amplify) and six NPC types with distinct game-theory behaviors.
- **Scoring and grades.** Pac-Man has a high score. Globin has per-level grades (S / A / B / C / D) for replayability.
- **"One more try" pacing.** Each level is short enough to replay immediately when you fail or want a better grade.

---

## 3. The game (concept lock — do not expand)

**Title:** *Globin: Good Manners Win*
**Pitch:** A real-time arcade-strategy game where you play as Globin, a small friendly creature in a world of NPCs with real game-theory personalities. Your only weapons are good manners — waving, sharing, shielding, and amplifying cooperation. Each level teaches a game-theory concept through gameplay. Fill the Trust Meter to clear the level. Prove that good manners are the winning strategy.
**Genre:** Arcade-strategy / social simulation / educational.
**Target platform:** WebGL build hosted on itch.io. Same build submitted to CAC.
**Vibe:** Dynamic and engaging. Not frantic, not cozy — *interesting*. Think: "what happens if I wave at the Grudger after I accidentally bumped them?" Real-time, responsive, surprising, fair.

### Core Mechanics

**Globin (the player character):**
- Moves freely in a 2D top-down arena using WASD or arrow keys
- Has 4 social actions mapped to keys (details below)
- Cannot attack, damage, or destroy NPCs in any way
- Has a small **Patience meter** — if Globin gets yelled at by hostile NPCs too many times without shielding, Globin gets "frustrated" and can't act for 2 seconds (this is the penalty, not death)

**Social Actions:**

| Action | Key | Effect | Trust impact |
|---|---|---|---|
| **Wave** 👋 | Spacebar | Initiates a friendly interaction with the nearest NPC within range | Small +trust with that NPC |
| **Share** 🎁 | E | Gives a trust token to an NPC (limited supply per level — forces strategic choices) | Medium +trust with that NPC |
| **Shield** 🛡️ | Q | Blocks a hostile interaction directed at Globin or a nearby NPC | Prevents trust loss; small +trust with the shielded NPC |
| **Amplify** 📢 | F | Broadcasts the most recent nearby interaction to all NPCs in a radius | If the interaction was cooperative: +trust ripple. If it was hostile: −trust ripple. **This is the WBWWB mechanic.** |

**NPC Strategies (FSMs):**

Each NPC has a **strategy type** that governs how it reacts to Globin and to other NPCs. These are directly from game-theory research:

| Type | Visual cue | Behavior | How to "win" them |
|---|---|---|---|
| **Friendly** 😊 | Green tint | Always cooperates | Already on your side. Protect them. |
| **Copycat** 🪞 | Blue tint | Mirrors your last interaction. Wave → they wave back. Ignore → they ignore you. | Be consistently nice. They'll mirror it. |
| **Grudger** 😤 | Orange tint | Cooperates until you make ONE mistake (bump them, fail to shield them), then turns hostile permanently | Be careful. No mistakes. |
| **Hostile** 👊 | Red tint | Always defects. Yells at nearby NPCs, lowering their trust. | Can't convert directly. Build cooperative mass around them so their hostility is drowned out. |
| **Random** 🎲 | Purple tint | Cooperates or defects unpredictably | Manage uncertainty. Shield against bad interactions. Amplify their good moments. |
| **Copykitten** 😺 | Teal tint | Like Copycat but forgives one mistake before retaliating | More forgiving. A lesson in resilience. |

**Trust Meter:**
- Global level score from 0 to 100
- Rises when NPCs cooperate with each other (not just with Globin)
- Drops when hostile interactions happen and get amplified
- Fill to 100 → level complete
- Drops to 0 → level failed

**The strategic depth:** Globin's actions directly affect nearby NPCs, but the real score comes from **NPC-to-NPC interactions**. The player's job is to *catalyze* cooperation chains — get two Copycats mirroring each other positively, protect Friendlies from Hostiles, and amplify the right moments. This is indirect control — like SimCity, not like a shooter.

### Level Progression

**MVP: 5 levels.** Stretch: 8 levels. Each level is a compact arena (fits on one screen) with a specific NPC mix that teaches one concept.

| Level | Name | Concept | NPCs | New mechanic | Target time |
|---|---|---|---|---|---|
| 1 | "Hello World" | Movement + Wave | 4 Friendly | Wave | 2 min |
| 2 | "Mirror Mirror" | Tit-for-tat | 3 Friendly + 2 Copycat | Share | 3 min |
| 3 | "No Second Chances" | Grudger; consistency | 2 Friendly + 2 Copycat + 2 Grudger | Shield | 4 min |
| 4 | "The Bully Problem" | Isolating hostility | Mix + 1 Hostile | — | 4 min |
| 5 | "What Gets Amplified" | Feedback loops | Mix + 2 Hostile | **Amplify** | 5 min |
| 6 *(stretch)* | "Trust Crisis" | Rebuilding trust | Start at Trust 20, many hostile | — | 5 min |
| 7 *(stretch)* | "The Dilemma" | Scarcity | Very few Share tokens | — | 5 min |
| 8 *(stretch)* | "Good Manners Win" | Everything combined | All types, all mechanics | — | 6 min |

**End-of-level cards:** After each level, a brief card (2–3 sentences) names the real game-theory concept the player just experienced. Example:

> **Level 2 — "Mirror Mirror"**
> *You just met a Copycat. In game theory, this is called "tit-for-tat" — a strategy that mirrors your last move. Mathematician Robert Axelrod proved in 1984 that tit-for-tat wins more tournaments than any other strategy. It's nice, retaliatory, forgiving, and clear.*

This is the *explorable explanation* layer. The player learns by doing, then gets the vocabulary.

**Scoring:**

| Grade | Criteria |
|---|---|
| **S** | Trust Meter 100 + all NPCs cooperative + fast time + tokens remaining |
| **A** | Trust Meter 100 + most NPCs cooperative |
| **B** | Trust Meter 100 (bare pass) |
| **C** | Trust Meter 70–99 (partial clear, allowed on some levels) |
| **D** | Trust Meter < 70 (fail, retry) |

### What Globin looks like

You should sketch this. Starting direction:
- Small, round, expressive creature (think: Kirby-scale, not human-scale)
- Big eyes, no mouth (emotions through eyes and body language)
- Color: warm green or soft blue (visually distinct from all NPC tints)
- Animations: idle bounce, walk, wave, share (hand out), shield (arms up), amplify (pulse ring)

### Final scope (locked)

- Globin character with movement + 4 social actions
- 5 levels (Levels 1–5) with unique NPC mixes
- 6 NPC types with FSM behavior
- Trust Meter (0–100) per level
- Level-complete screen with grade + game-theory card
- Title screen with level select (unlocked levels)
- Basic SFX + background music loop
- WebGL build on itch.io

### Out of scope (do not add)

Dialogue trees, inventory, crafting, procedural generation, multiplayer, day/night cycle, weather, save/load (level grades stored in PlayerPrefs only), story cutscenes, character creation, skill trees, in-app purchases, voice acting.

---

## 4. Tech stack

| Layer | Choice | Why |
|---|---|---|
| Engine | Unity 2D (LTS, URP) | Continues from existing Unity work |
| Art style | Simple 2D sprites (pixel art or flat vector) | Keeps art scope tiny; Nicky Case's style proves simple works |
| NPC AI | Finite State Machines (one MonoBehaviour per strategy type, inheriting from a base `NPCStrategy` class) | Game-theory strategies are rule-based; no LLM needed. FSMs are debuggable. |
| Level data | JSON files (NPC placements, types, win conditions, share-token count) | Easy to add/tweak levels without recompiling |
| Trust system | Central `TrustManager` singleton tracking global trust + pairwise NPC trust values | One source of truth; inspector-visible for debugging |
| Build target | WebGL | Required by itch.io browser play; CAC accepts URL |
| Source control | Git (GitHub, public repo) | Required for AI agents, portfolio visibility, and competition submissions |
| AI tooling | Copilot CLI + Ralph loop | Spec-first, agentic implementation |

### Architecture sketch

```
Scripts/
├── Player/
│   ├── GlobinController.cs       (movement + action input)
│   └── GlobinActions.cs          (Wave, Share, Shield, Amplify logic)
├── NPC/
│   ├── NPCBase.cs                (base class: wander, interact, visual state)
│   ├── FriendlyNPC.cs            (always cooperates)
│   ├── CopycatNPC.cs             (mirrors last interaction)
│   ├── GrudgerNPC.cs             (cooperates until one mistake)
│   ├── HostileNPC.cs             (always defects)
│   ├── RandomNPC.cs              (random cooperate/defect)
│   └── CopykittenNPC.cs          (forgives once before retaliating)
├── Systems/
│   ├── TrustManager.cs           (global trust meter + pairwise trust)
│   ├── InteractionResolver.cs    (resolves cooperate/defect between two entities)
│   ├── AmplifySystem.cs          (broadcasts interaction to NPCs in radius)
│   └── LevelManager.cs           (load level JSON, check win/lose, grades)
├── UI/
│   ├── TrustMeterUI.cs           (bar at top of screen)
│   ├── ActionButtonsUI.cs        (bottom bar showing available actions)
│   ├── LevelCompleteUI.cs        (grade + game-theory card)
│   ├── TitleScreenUI.cs          (title + level select)
│   └── NPCIndicatorUI.cs         (floating mood/type icon above each NPC)
└── Data/
    ├── levels.json               (level definitions)
    └── theory-cards.json         (end-of-level explanation text)
```

---

## 5. Pre-work (Jorge solo — before Sprint 1)

**This is not optional.** Same principle as Kindred Village: the student never sees a blank Unity project.

Estimated time: **4–6 hours**, spread across evenings.

- [ ] New Unity 2D project, URP template, Git from commit 1
- [ ] Globin sprite placeholder (colored circle with eyes — replace with real art later)
- [ ] GlobinController.cs — WASD movement on a simple arena (single-screen, camera locked)
- [ ] NPCBase.cs — one NPC that wanders to random points in the arena (no pathfinding needed for 2D — just pick a point, move toward it, pick another)
- [ ] TrustManager.cs — a global trust value (int 0–100) with `AddTrust(int)` and `RemoveTrust(int)`, inspector-visible
- [ ] TrustMeterUI — a simple bar at the top of the screen bound to TrustManager
- [ ] One level JSON file (`level-1.json`) that spawns 4 Friendly NPCs at defined positions
- [ ] LevelManager.cs — reads the JSON, spawns NPCs, detects trust == 100 → win
- [ ] WebGL build configured and **uploaded to a private itch.io page once**, end-to-end
- [ ] Repo has a `specs/` folder ready for sprint specs
- [ ] Copilot CLI works in the repo; one trivial AI-generated commit landed

**The deliverable of pre-work is:** Globin moves around an arena with 4 wandering placeholder NPCs. Nothing interactive yet — that's Sprint 1's job.

---

## 6. The sprints

Each sprint is **one hour** and follows the same shape as Kindred Village:

1. **Read the spec together (5 min).** Senior reads the sprint spec aloud. Junior asks questions.
2. **Junior prompts the AI (10 min).** Junior types the prompt into Copilot CLI / Ralph loop. Senior coaches the prompt.
3. **AI generates a diff (5 min).** Both watch.
4. **Review the diff out loud (15 min).** Senior narrates reasoning, catches AI mistakes.
5. **Run in Play Mode (10 min).** Junior tests. Reports what works, what doesn't.
6. **Fix the inevitable break (10 min).** Inspector references, collider issues, etc.
7. **Commit + write 2-line journal entry (5 min).** What did AI do well? Where did it need correction?

**Game-theory moment (new for Globin):** At the end of each sprint, Jorge spends 2 minutes explaining the real-world concept behind what you just built. This is the *preceptorship* version of the end-of-level cards — you understand the theory because you built the mechanic.

---

### Sprint 1 — Wave action + Friendly NPCs (Level 1 playable)

**Spec the junior prompts against:**

> Make the 4 Friendly NPCs respond to Globin. When Globin presses Spacebar near an NPC (within a trigger radius), play a "wave" animation (sprite scale bounce for now) on both Globin and the NPC. Each wave adds +5 to the Trust Meter. The NPC should show a small floating "+5" text that fades. When Trust reaches 100, show a "Level Complete!" panel with a "Next Level" button (button does nothing yet). NPCs should only be waveable once every 3 seconds (cooldown).

**Teachable moments to plant:**
- AI will probably forget the cooldown. Junior catches it when spamming Spacebar fills the meter instantly.
- AI may use `OnCollisionEnter2D` instead of `OnTriggerEnter2D`. Senior explains the difference.
- AI may not show the floating text correctly (canvas vs. world space). Expected break.

**Game-theory moment:** *"You just built the simplest possible cooperative world — everyone is friendly. In real game theory, this is called 'always cooperate.' It's nice, but it's also naive. Next week we add NPCs that aren't so easy."*

**Done when:** Globin moves, waves at Friendly NPCs, trust rises, level-complete screen appears at 100, committed to Git.

---

### Sprint 2 — Copycat NPCs + Share action (Level 2)

**Spec the junior prompts against:**

> Create a new NPC type: `CopycatNPC`. The Copycat starts neutral (yellow tint). When Globin waves at a Copycat, it turns friendly (green) and waves back (+5 trust). If Globin bumps into a Copycat without waving (walks into their collider without pressing Spacebar), the Copycat turns unfriendly (orange) for 5 seconds and nearby trust drops −3. Copycats remember Globin's *last interaction* with them and mirror it — if Globin's last action was friendly, they stay friendly; if it was a bump, they stay unfriendly until Globin waves again.
>
> Also add the Share action: pressing E near an NPC gives them a trust token. Each level has a limited number of tokens (defined in the level JSON — Level 2 has 5). Sharing gives +10 trust with that NPC. Show the remaining token count in the UI.

**Teachable moments to plant:**
- AI will likely make Copycats respond to every interaction globally instead of tracking per-NPC history. Senior asks: *"What if Globin waves at Copycat A but bumps Copycat B? Should B turn unfriendly?"*
- Token limit forces real strategy. Junior discovers that spamming Share on one NPC wastes tokens. Senior: *"That's resource allocation — a real strategy concept."*

**Game-theory moment:** *"The Copycat is the most famous strategy in game theory. Robert Axelrod, a political scientist, ran tournaments in the 1980s. Tit-for-tat — which is exactly what your Copycat does — won against every other strategy. It's nice (cooperates first), retaliatory (punishes cheating), forgiving (goes back to cooperating), and clear (its behavior is predictable). You just coded that."*

**Done when:** Level 2 loads from JSON, Copycats mirror behavior, Share works with limited tokens, level completes at trust 100, committed.

---

### Sprint 3 — Grudger NPCs + Shield action (Level 3)

**Spec the junior prompts against:**

> Create `GrudgerNPC`. The Grudger starts cooperative (green). If Globin *ever* bumps them or fails to shield them from a hostile interaction, the Grudger turns permanently hostile (red) and cannot be converted back. The Grudger's hostility lowers trust of nearby NPCs by −2 every 5 seconds.
>
> Add the Shield action: pressing Q creates a brief shield bubble around Globin (1-second duration). If a hostile NPC's "yell" hits the bubble instead of Globin or a friendly NPC, the yell is absorbed — no trust loss. Successfully shielding gives +3 trust.
>
> Create `level-3.json` with: 2 Friendly, 2 Copycat, 2 Grudger. Trust target: 100.

**Teachable moments to plant:**
- The Grudger's permanence is the key lesson. Junior will inevitably bump a Grudger and see the cascade. Senior: *"This is why the spec says 'permanently hostile.' Can you think of a real-world relationship where one mistake ended it forever? That's the Grudger."*
- Shield timing is tricky. AI may make the shield permanent or too long. Senior: *"1 second. You have to TIME it. That's the skill."*
- AI may not handle the "fail to shield" condition (Grudger turns hostile if a Hostile NPC yells at them and Globin doesn't shield in time). Complex conditional. Expected break.

**Game-theory moment:** *"The Grudger is 'grim trigger' in game theory. It cooperates until you defect once — then it defects forever. In real life, some people are like this. The lesson: with Grudgers, you can't afford a single mistake. Is that a good strategy? It protects the Grudger from exploitation, but it also means they lose out on cooperation after any accident. Tit-for-tat (Copycat) is more forgiving — and that's why it wins more often."*

**Done when:** Level 3 loads, Grudgers work as specified, Shield action blocks hostility, committed.

---

### Sprint 4 — Hostile NPCs + NPC-to-NPC interactions (Level 4)

**Spec the junior prompts against:**

> Create `HostileNPC`. The Hostile NPC has a red tint and periodically "yells" — every 4 seconds, it emits a yell that lowers trust of any NPC within range by −5. Hostile NPCs cannot be converted by any player action.
>
> **Critical new system: NPC-to-NPC interactions.** Until now, only Globin's actions affected NPCs. Now, NPCs interact with each other:
> - When two NPCs are near each other (within interaction range), they have a brief interaction. The outcome depends on both strategies: two Friendlies cooperate (+2 trust each), a Friendly and a Hostile get a −5 for the Friendly, two Copycats check their last interaction memory, etc.
> - The `InteractionResolver.cs` takes two NPC types and returns the trust change for each.
> - NPCs interact at most once per 5 seconds with the same partner.
>
> Create `level-4.json`: 2 Friendly, 2 Copycat, 1 Grudger, 1 Hostile. Trust starts at 50 (not 0 — already some trust exists), target 100.

**Teachable moments to plant:**
- This is the biggest code sprint. AI will struggle with the interaction resolver because it needs to handle every pair combination. Senior asks: *"How many pairs of strategy types are there? Let's list them."* (This is combinatorics — a real math lesson.)
- The Hostile NPC can't be converted. Junior will try. Senior: *"You can't make the bully cooperate. You CAN make the bully irrelevant. How?"* → Answer: surround them with cooperating NPCs whose trust gains outpace the Hostile's damage.
- NPC-to-NPC interactions create emergent behavior. Junior will see Copycats start cooperating with each other *without Globin's help*. Senior: *"You just built emergence. The system is doing something you didn't explicitly program."*

**Game-theory moment:** *"In real game-theory tournaments, 'always defect' (your Hostile) eventually loses — but it does a LOT of damage along the way. The winning strategy isn't to convert the bully; it's to build a cooperative network that's resilient enough to absorb the damage. That's what you're doing in this level."*

**Done when:** Level 4 loads, Hostile NPC yells periodically, NPCs interact with each other, trust dynamics are emergent, committed.

---

### Sprint 5 — Amplify mechanic + Level 5 (MVP COMPLETE)

**Spec the junior prompts against:**

> Add the Amplify action: pressing F triggers a pulse that broadcasts the most recent interaction within Globin's radius to all NPCs in a larger radius.
>
> - If the most recent interaction was cooperative (a wave, a share, two NPCs cooperating), every NPC in the amplify radius gets a +3 trust bump and a visual "ripple" effect (expanding ring, green).
> - If the most recent interaction was hostile (a yell, a bump, a Grudger turning hostile), every NPC in the amplify radius gets a −3 trust drop and a red ripple.
> - The player must be strategic: amplify at the RIGHT moment. Amplifying at the wrong time can spread hostility.
> - Amplify has a 10-second cooldown.
>
> Create `level-5.json`: 2 Friendly, 2 Copycat, 1 Grudger, 2 Hostile. Trust starts at 30, target 100. This is the first level where the player MUST use Amplify to win — there isn't enough time to wave at every NPC individually.
>
> Add end-of-level grading: S / A / B / C / D based on final trust, number of cooperative NPCs, time taken, and tokens remaining. Show the grade on the level-complete screen.
>
> Add a title screen with level select (levels 1–5, locked/unlocked). Store completion/grades in PlayerPrefs.

**Teachable moments to plant:**
- Junior will amplify a hostile interaction by accident the first time. This is the "camera moment" from WBWWB. Senior: *"You just did what the media does in We Become What We Behold. You amplified the wrong thing and it spread. Now try again — amplify the RIGHT thing."*
- The grading system introduces perfectionist replay: *"You got a B. Want to go for S?"*
- The cooldown on Amplify forces timing. Junior discovers: *"I have to WAIT for a good moment, then broadcast it."*

**Game-theory moment:** *"In We Become What We Behold, Nicky Case showed that what gets amplified shapes reality. That's literally true in media, in social media, in politics. Your Amplify button is the camera. You just learned that the most powerful thing in the game isn't being nice — it's choosing WHAT TO BROADCAST. That's media literacy in one mechanic."*

**Done when:** Amplify works, Level 5 is playable, grading system works, title screen + level select, levels 1–5 complete end-to-end, WebGL build works, committed. **This is the MVP.**

---

### Sprint 6 — Polish, ship, submit (MVP → v1.0)

**Spec the junior prompts against:**

> - Add end-of-level game-theory cards: after the grade screen, show a brief card (2–3 sentences) explaining the real concept the player just experienced. Load text from `theory-cards.json`.
> - Add background music (one looping ambient track).
> - Add SFX: wave (chime), share (gift sound), shield (shield-up), amplify-good (positive ripple), amplify-bad (negative buzz), trust-up (ding), level-complete (fanfare).
> - Polish: NPC emotions shown as floating emoji/icons above their heads. Smooth transitions between levels.
> - Produce a final WebGL build. Upload to itch.io (public page). Fill out CAC submission form.

**Stretch goals (only if core ship is locked by mid-sprint):**

> - Levels 6–8 (Trust Crisis, The Dilemma, Good Manners Win)
> - Copykitten and Random NPC types
> - A "sandbox" mode where the player picks which NPCs to place and watches the simulation run

**Teachable moments to plant:**
- AI cannot upload to itch.io or submit to CAC. Junior does both manually.
- SFX sourcing: senior shows how to find CC0 sounds on freesound.org.
- The theory-card text is your writing, not AI's. *"This is the part that makes it YOUR game, not a generic game. Write what YOU learned."*

**Done when:** Live itch.io URL plays in a browser, CAC form submitted, repo tagged `v1.0`.

---

## 7. Scope-cut ladder (when time runs out)

If a sprint is going long, cut from the bottom:

1. Levels 6–8 (stretch — defer to v1.1)
2. Copykitten and Random NPC types (defer — 4 types is enough for MVP)
3. Background music
4. SFX
5. End-of-level game-theory cards (can add as a patch)
6. Grading system (pass/fail is fine for MVP)
7. Level select screen (auto-advance to next level is fine)

**Never cut:** Globin movement, Wave + Share + Shield + Amplify, 5 levels, Friendly + Copycat + Grudger + Hostile NPCs, Trust Meter, level-complete screen, WebGL build, itch.io upload.

---

## 8. Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| First WebGL build takes 40+ minutes | High | Jorge does it in pre-work. Never block a sprint on build. |
| AI rewrites a MonoBehaviour and breaks inspector references | Very High | Plan for it. Budget 10 min/sprint. The fix IS the teachable moment. |
| NPC-to-NPC interaction resolver has too many pair combinations | High | Start with a simple matrix (4×4 = 16 pairs). Use a lookup table, not a chain of if-else. |
| Amplify mechanic feels confusing (player doesn't know what they're amplifying) | Medium | Show a preview: highlight the "most recent interaction" before the player presses F. Visual clarity is everything. |
| Scope creep ("can Globin have a sword?") | Certain | Point at locked scope in section 3. The whole point is that Globin DOESN'T have a sword. Add ideas to `BACKLOG.md`. |
| Game isn't fun because trust rises too slowly or too fast | High | Tune trust values in JSON, not in code. Jorge adjusts between sprints based on playtest feedback. |
| Junior gets frustrated by Grudger permanence | Medium | This IS the lesson. But if it feels unfair, add a visual warning ("Grudger is watching...") before the mistake triggers. |
| AI generates code that compiles but does the wrong thing | High | Review-out-loud step 4 exists for this. |
| itch.io / CAC accounts not created | Medium | Both created during pre-work, not Sprint 6. |

---

## 9. Competition & showcase strategy

### Primary targets

| Priority | Target | Deadline | What to submit |
|---|---|---|---|
| 🥇 P1 | **itch.io** (standalone publish + game jams) | Rolling | WebGL build + devlog page. Permanent portfolio URL. Submit to jams with aligned themes (social impact, game theory, charity). |
| 🥇 P1 | **Congressional App Challenge (CAC)** | Late Oct 2026 | Working WebGL app + 3-min demo video + written description. Register May 1, 2026. |
| 🥈 P2 | **Games for Change (G4C) Student Challenge** | ~March 2027 | Playable game + designer statement. Perfect fit: "game whose mechanics teach game theory and cooperation." |

### NYU pipeline (high school programs)

| Program | What | When | Cost | Eligible? |
|---|---|---|---|---|
| **Future Game Designers** (NYU Game Center) | Free 14-week spring workshop on Saturdays. HS freshmen, sophomores, juniors. Learn game design from NYU Game Center faculty. | Spring semester (apply in fall) | **FREE** | **Yes — spring 2027** (9th-grade freshman). Apply fall 2026 WITH Globin as portfolio. |
| **Tisch Summer HS Game Design** | 4-week residential. 3 college-level courses. Earn 6 NYU credits. Rising juniors/seniors only. | July–Aug (apply Dec of prior year) | ~$7,000+ (financial aid available) | **Summer 2028** at earliest (rising junior). Globin + Future Game Designers on portfolio = strong application. |

**NYU action items:**
1. **Now:** Write down what games impressed you at the showcase and why. Use as design reference.
2. **Fall 2026:** Apply for Future Game Designers with Globin as the portfolio piece.
3. **Fall 2027:** Apply for Tisch Summer HS Game Design.

### Stretch targets (same as before)

| Target | Role |
|---|---|
| **GDWC** | Summer submission window — same WebGL build, zero extra work |
| **MLH Junior** | One weekend hackathon as practice during months 2–4 |
| **MIT THINK** | 9th grade (Nov 2026 → Jan 2027). Reframe Globin as research: *"Can game-theory-based cooperative gameplay mechanics shift player behavior toward pro-social strategies?"* |

### itch.io Game Jam approach

itch.io hosts hundreds of jams per year. Strategy:
1. Publish Globin as a standalone page (free, permanent URL)
2. Monitor [itch.io/jams/upcoming](https://itch.io/jams/upcoming) for aligned themes:
   - Social impact jams
   - Educational / game-theory jams
   - GGJ Next (partnered with Games for Change)
   - Charity jams
   - General indie jams where the theme fits
3. Submitting to a jam = uploading the same build with a jam-specific description. Zero extra build work.
4. Build a devlog on the itch.io page — development journey, design decisions, game-theory lessons learned. Judges and admissions officers love this.

---

## 10. Submission templates

### itch.io page

- **Title:** Globin: Good Manners Win
- **Genre:** Strategy, Simulation, Educational
- **Tags:** game-theory, cooperation, strategy, nonviolent, social-impact, arcade, webgl, nicky-case-inspired, moral-games
- **Short description:** A real-time arcade-strategy game where your only weapons are good manners. Navigate levels of NPCs with real game-theory personalities — Copycats, Grudgers, Bullies — and prove that cooperation is the winning strategy. Inspired by Nicky Case's *The Evolution of Trust* and *We Become What We Behold*.
- **Credits:**
  - Game design & code: [your name]
  - Mentorship: Jorge
  - Art: [source]
  - Music / SFX: [source]
  - Built with Unity. AI-assisted development with GitHub Copilot CLI.
  - Inspired by Nicky Case's work on explorable explanations.

### CAC submission

- **App name:** Globin: Good Manners Win
- **Platform:** WebGL (browser)
- **What it does:** A real-time strategy game that teaches game theory through gameplay. The player controls Globin, a character whose only actions are cooperation — waving, sharing, shielding others, and amplifying positive interactions. NPCs behave according to real game-theory strategies (tit-for-tat, grim trigger, always defect). Each level teaches a concept; end-of-level cards name the real math behind what the player experienced. The game proves — through mechanics, not lectures — that good manners are the winning strategy.
- **Why I built it:** [write 2–3 sentences in your own voice]
- **Tools used:** Unity, C#, GitHub Copilot CLI, Ralph spec-driven loop
- **Demo URL:** [itch.io link]
- **Source code:** [GitHub link]

### CAC demo video script (3 minutes)

| Segment | Duration | Content |
|---|---|---|
| **Problem** | 15 sec | "Most games teach you to win by fighting. What if the winning strategy is being kind?" |
| **Personal story** | 30 sec | Talk about visiting the NYU Game Design Showcase, discovering Nicky Case's work, and asking "can you defeat them with good manners?" |
| **Live demo** | 90 sec | Play through Levels 1, 2, and 5. Show Copycat mirroring behavior, Grudger turning hostile after a mistake, Amplify spreading cooperation. Show the end-of-level game-theory card. |
| **Why it matters** | 45 sec | "This game teaches real game theory — the math of cooperation. Robert Axelrod proved tit-for-tat wins. Nicky Case showed how feedback loops shape society. I turned that into a game you can play. Good manners win — and now I can prove it." |

---

## 11. Relationship to Kindred Village

- **Kindred Village** remains a separate game with its own sprint spec (`JAM_SPRINT_SPEC.md`) and playable demo (`kindred_village_demo/`)
- **Globin** is a new, separate project and repo
- Shared DNA (trust mechanics, NPCs, cooperation themes) but different genres:
  - Kindred Village = cozy village management sim (place buildings, manage relationships over 14 days)
  - Globin = arcade-strategy with game-theory mechanics (real-time levels, Pac-Man-style progression)
- Kindred Village can serve as a completed "warm-up" portfolio piece
- The two games together tell a portfolio story: *"I explore cooperation and trust in games from multiple angles"*

---

## 12. Open decisions (before Sprint 1)

1. **What does Globin look like?** Sketch the character. The character IS the brand.
2. **Top-down or minimal isometric?** Recommend: top-down (simpler to build, cleaner for game-theory readability). Revisit after MVP.
3. **Should NPCs have names?** Pure game-theory: label by strategy ("The Copycat"). More engaging: names + strategy ("Maya the Copycat"). Your call.
4. **Background narrative?** Option A: no story, pure arcade. Option B: one-sentence frame — *"Globin's village lost trust. Help rebuild it."* Recommend B — strengthens the CAC personal-story angle without adding writing scope.
5. **MVP scope: 5 or 8 levels?** Plan says 5 for MVP, 8 as stretch. Ship 5 first. If it's fun at 5, add 6–8 post-CAC.

---

## 13. After the sprints

- **v1.1 (post-CAC):** Add Levels 6–8, Copykitten + Random NPC types, sandbox mode
- **Future Game Designers application (fall 2026):** Apply to NYU's free spring program with Globin as portfolio
- **MIT THINK (Jan 2027):** Reframe as research proposal — *"Can game-theory-based cooperative gameplay shift player behavior toward pro-social strategies?"* Run a small user study (10–20 classmates play, pre/post survey on cooperation attitudes)
- **G4C Student Challenge (March 2027):** Submit Globin with designer statement emphasizing mechanics-as-message
- **Devlog:** Publish on itch.io + Medium. *"How I turned Robert Axelrod's 1984 game-theory tournament into an arcade game."* This is the college-application essay seed.

---

*Spec authored as a senior would author it, for a junior to execute against, with AI as the third pair of hands. The game theory is real. The game is yours.*
