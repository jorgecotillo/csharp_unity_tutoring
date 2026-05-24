# Jam Sprint Spec — Kindred Village: Council of Sages

A 3-sprint plan to ship a Unity 3D game to **itch.io** and submit it to the **Congressional App Challenge (CAC)**. Each sprint is one Sunday session, one hour. The plan is built around an AI-assisted, mentor-led workflow inspired by Russinovich & Hanselman's *Redefining the Software Engineering Profession for AI* (Communications of the ACM, 2026, DOI: 10.1145/3779312).

---

## Acronyms (defined on first use)

| Acronym | Meaning |
|---|---|
| **CAC** | Congressional App Challenge — annual U.S. House of Representatives student coding competition |
| **MVP** | Minimum Viable Product — the smallest working version that's still worth shipping |
| **NPC** | Non-Player Character — characters in a game controlled by code, not the player |
| **NavMesh** | Navigation Mesh — Unity's built-in pathfinding surface so NPCs walk around obstacles |
| **URP** | Universal Render Pipeline — Unity's modern, performance-friendly graphics pipeline |
| **WebGL** | Web Graphics Library — lets a Unity game run inside a browser tab |
| **FBX** | Filmbox — 3D model file format used by Mixamo and Unity |
| **SFX** | Sound Effects |
| **JSON** | JavaScript Object Notation — a simple text format for storing data like quote lists |
| **SDK** | Software Development Kit |
| **HTML5** | HyperText Markup Language version 5 — the modern web standard a WebGL build is wrapped in |
| **CLI** | Command-Line Interface |
| **Copilot CLI** | GitHub's terminal-based AI coding agent (the tool generating this very spec) |
| **Ralph loop** | A spec-first iterative loop where a written spec drives an AI agent to generate, test, and refine code in cycles |
| **Spec Kit** | A spec-first authoring approach: write the requirement first, then let AI implement against it |
| **EiC** | Early-in-Career engineer (term used in the Russinovich/Hanselman article) |

---

## 1. Why this spec is shaped this way (the Russinovich frame)

Russinovich & Hanselman's argument, in one sentence: **AI is great at generating code, but Early-in-Career engineers can't learn judgment if a senior never sits next to them and walks them through it.** Their proposed remedy is a *preceptorship* — senior and junior working the same problem, with the senior actively narrating reasoning, reviewing AI output, and turning daily work into teachable moments.

This sprint plan applies that model directly:

| Role | Who | What they do |
|---|---|---|
| **Senior / Preceptor** | Jorge | Owns architecture. Writes specs. Reviews every AI diff out loud. Catches the mistakes AI makes. Decides what gets merged. |
| **EiC / Apprentice** | The student | Writes the prompt. Runs the AI agent. Reads the diff. Tests in Play Mode. Reports what broke. Iterates. |
| **AI agent** | Copilot CLI / Ralph loop | Implements. Refactors. Generates boilerplate. Suggests next steps. Never the final decider. |

The key is **the student is not "watching Jorge code."** The student drives the keyboard during the sprint. Jorge narrates: *"Notice the AI just hardcoded the philosopher's name — what happens when we add a second one? Let's fix that together."* That narration is the entire point.

---

## 2. The game (concept lock — do not expand)

**Title:** *Kindred Village: Council of Sages*
**Pitch:** A small 3D village where wandering philosopher NPCs offer wisdom. The player places buildings, clicks a philosopher to hear a quote, and watches a **Trust Meter** rise. Win condition: Trust Meter fills.
**Genre:** Cozy strategy / village builder / digital wellbeing.
**Target platform:** WebGL build hosted on itch.io. Same build submitted to CAC.
**Vibe:** No combat. No timers that punish. Calm music. Philosophers walk around like Sims.

### Final scope (locked)

- 3 philosopher NPCs walking on a NavMesh (Mixamo humanoids)
- 3 building prefabs the player can place by clicking the ground
- ~15 quotes total, served from a JSON file, randomly picked when a philosopher is clicked
- A Trust Meter (0–100) that goes up when the player reads a quote or places a building
- A win screen when Trust hits 100
- Title screen, basic SFX, background music

### Out of scope (do not add)

Combat, economy, save/load, multiplayer, day/night, weather, dialogue trees, multiple maps, character creation, leveling, inventory.

---

## 3. Tech stack

| Layer | Choice | Why |
|---|---|---|
| Engine | Unity (LTS, URP) | Continues from prior tutoring work |
| Models | Mixamo humanoids (FBX) | Free, riggable, recognizable as people |
| Animations | Mixamo walk + idle + wave | Free, retargets to humanoid rig |
| Pathfinding | Unity NavMesh + NavMeshAgent | Built-in, robust, no plugin |
| Environment | Free low-poly asset pack from Asset Store | Buildings + ground without modeling |
| Build target | WebGL | Required by itch.io browser play; CAC accepts URL |
| Source control | Git | Required for AI agents to diff against |
| AI tooling | Copilot CLI + Ralph loop | Spec-first, agentic implementation |

---

## 4. Pre-work (Jorge solo — before Sprint 1)

**This is not optional.** The student should never see a blank Unity project. The preceptorship model only works if the senior has *already done the boring scaffolding*, so session time is spent on judgment, not on fighting the toolchain.

Estimated time: **3–5 hours**, spread across whatever evenings work.

- [ ] New Unity project, URP template, version-controlled in Git from commit 1
- [ ] One Mixamo philosopher imported, retargeted to humanoid rig, plays a walk loop
- [ ] NavMesh baked on a simple ground plane; the philosopher wanders to random points
- [ ] One building prefab placeable on click (raycast from camera to ground)
- [ ] Trust Meter UI bar that responds to a public method `AddTrust(int)`
- [ ] WebGL build configured and **actually uploaded to a private itch.io page once**, end-to-end, so the pipeline is proven before sprint 1
- [ ] Repo has a `specs/` folder ready for sprint specs
- [ ] Copilot CLI works in the repo; one trivial AI-generated commit landed

**The deliverable of pre-work is a boring-but-working baseline scene the student can immediately add to.** Mixamo retargeting and the first WebGL build are the two things most likely to eat 90 minutes; both must be solved before the student arrives.

---

## 5. The three sprints

Each sprint is **one hour** and follows the same shape. This shape *is* the lesson:

1. **Read the spec together (5 min).** Senior reads the sprint spec aloud. Junior asks questions.
2. **Junior prompts the AI (10 min).** Junior types the prompt into Copilot CLI / Ralph loop using the spec as context. Senior coaches the prompt — *"too vague, what file should it touch?"* — but does not type it.
3. **AI generates a diff (5 min).** Both watch.
4. **Review the diff out loud (15 min).** Senior narrates: *"Here it added a public field — why is that bad in Unity? What would happen if we delete this prefab?"* Junior accepts, rejects, or asks AI to revise.
5. **Run in Play Mode (10 min).** Junior tests. Reports what works, what doesn't.
6. **Fix the inevitable break (10 min).** Inspector references almost always break when AI rewrites a MonoBehaviour. Senior shows the fix once, junior does it the next time.
7. **Commit + write 2-line journal entry (5 min).** What did the AI do well? Where did it need correction? This journal is the "teachable moment" record the article calls for.

### Sprint 1 — Add a second philosopher and a second building

**Spec the junior prompts against:**

> Add a second philosopher NPC named "Marcus" using the existing Mixamo philosopher prefab. Both philosophers must wander independently on the NavMesh without overlapping start positions. Add a second building prefab variant (a "Library") to the placeable buildings list. Placing any building still adds 5 to the Trust Meter.

**Teachable moments to plant:**
- AI will likely duplicate code instead of using a list. Senior asks: *"How do we add a 3rd one without rewriting?"*
- AI may put both philosophers at the same spawn point. Junior catches it in Play Mode.
- AI may not connect the new building prefab in the inspector. Senior shows the fix.

**Done when:** Two philosophers wander, two buildings can be placed, trust still goes up, committed to Git, WebGL build still compiles.

### Sprint 2 — Quote system

**Spec the junior prompts against:**

> Create a `quotes.json` file with 15 entries, each with `philosopher` (string) and `text` (string). At runtime, when the player clicks a philosopher NPC, show a UI panel with one randomly-chosen quote *attributed to that philosopher*. Closing the panel adds 10 to the Trust Meter. Quotes must not repeat until the pool is exhausted. Add a third philosopher "Hypatia" so the pool covers three.

**Teachable moments to plant:**
- AI will probably hardcode quotes in C# first. Senior asks: *"What if we want to add a quote next week without recompiling?"* — leads junior to JSON.
- AI will likely allow repeats. Junior tests, catches it.
- Click detection on a moving NavMesh agent has gotchas (collider on the right child object). Expected break.

**Done when:** Three philosophers, clicking each gives one of their own quotes, no repeats until pool resets, trust rises, committed.

### Sprint 3 — Polish, win condition, ship

**Spec the junior prompts against:**

> Add a title screen with a "Start" button. Add a win screen that triggers when Trust reaches 100, with a "Play Again" button. Add background music (looping) and a click SFX when a quote panel opens. Produce a WebGL build and upload it to itch.io. Fill out the CAC submission form using the URL.

**Stretch goal — "Sage's Bench" (only if core ship is locked by mid-sprint):**

> Add a clickable bench in the village. When the player clicks it, open a text input panel where the player types a real-life question. On submit, randomly pick one philosopher and display a short answer in that philosopher's voice, drawn from the same quote bank used for villager advice. No persistence required.

This preserves Warren's original "philosophical Magic 8-Ball" idea as a player-facing bonus on top of the village game. If it doesn't land in week 3, it ships as v1.1 post-competition.

**Teachable moments to plant:**
- AI cannot upload to itch.io. Junior does that step manually — *"this is the part AI can't do for you, and that's why your judgment matters."*
- AI may break the build with a missing reference. Senior shows the build log.
- Submission text needs a human voice. Junior writes it.

**Done when:** Live itch.io URL plays in a browser, CAC form submitted, repo tagged `v1.0`.

---

## 6. Scope-cut ladder (when time runs out)

If a sprint is going long, cut from the bottom:

1. Sage's Bench (stretch goal — defer to v1.1)
2. Background music
3. SFX
4. Win screen polish (just `Debug.Log("You win")` is fine)
5. Third philosopher / third building
6. JSON quotes (hardcoded strings are acceptable for v1.0)
7. NavMesh wandering (stationary philosophers are acceptable for v1.0)

**Never cut:** Two philosophers, click-for-quote, trust meter going up, WebGL build, itch.io upload, CAC submission.

---

## 7. Risks and mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| First WebGL build takes 40+ minutes | High | Jorge does it in pre-work. Never block a sprint on a first-time build. |
| AI rewrites a MonoBehaviour and breaks all inspector references | Very High | Plan for it. The fix *is* the teachable moment. Budget 10 min/sprint. |
| Mixamo retargeting fails at import | Medium | Lock the rig in pre-work. Don't import new humanoids during sprints. |
| Junior gets stuck waiting for AI output | Medium | Senior uses the wait time to narrate what the prompt could have said better. |
| Scope creep ("can it have a dragon?") | Certain | Point at the locked scope in section 2. Add to a `BACKLOG.md` for a future jam. |
| AI generates code that compiles but does the wrong thing | High | This is the entire reason for review-out-loud step 4. |
| itch.io account / CAC account not created | Medium | Both created during pre-work, not Sprint 3. |

---

## 8. Submission templates

### itch.io page

- **Title:** Kindred Village: Council of Sages
- **Genre:** Strategy, Simulation
- **Tags:** cozy, philosophy, mental-health, wholesome, no-violence, webgl
- **Short description:** A calm village where wandering philosophers share wisdom. Place buildings, listen to sages, build trust. No combat, no timers — just thoughtful play.
- **Credits:**
  - Game design & code: [student name]
  - Mentorship: Jorge
  - 3D characters: Mixamo (Adobe)
  - Environment assets: [asset pack name + author]
  - Music / SFX: [source]
  - Built with Unity. AI-assisted development with GitHub Copilot CLI.

### CAC submission

- **App name:** Kindred Village: Council of Sages
- **Platform:** WebGL (browser)
- **What it does:** A 3D strategy game where the player builds a village around three wandering philosopher NPCs. Clicking a philosopher reveals a quote; placing buildings and reading wisdom raises a Trust Meter. The game is a gentle counterweight to combat-heavy online gaming, designed to encourage reflection and community.
- **Why I built it:** [student writes 2–3 sentences in their own voice]
- **Tools used:** Unity, C#, Mixamo, GitHub Copilot CLI, Ralph spec-driven loop
- **Demo URL:** [itch.io link]
- **Source code:** [GitHub link]

---

## 9. After the jam

The philosophical-question app, *Counsel*, is **not** part of this sprint. It's a strong follow-up project for after CAC submission — it can reuse the quote JSON from Sprint 2 and the philosopher metaphor, but as a standalone web/mobile app. Capture the idea in `BACKLOG.md` and revisit once *Kindred Village* ships.

---

## 10. Open questions for Jorge

1. **Pick a specific itch.io jam.** A live jam with a deadline gives the sprints a real ship date. Want me to surface 2–3 current jams whose themes fit Kindred Village?
2. **Spec folder structure.** Do you want one `specs/sprint-1.md`, `specs/sprint-2.md`, `specs/sprint-3.md` (Spec Kit style — recommended, since it's exactly what the Ralph loop wants), or a single monolithic spec file?
3. **Pre-work checklist tracking.** Want this as a GitHub issue checklist on the repo, or kept inside this doc?

---

*Spec authored as a senior would author it, for a junior to execute against, with AI as the third pair of hands.*
