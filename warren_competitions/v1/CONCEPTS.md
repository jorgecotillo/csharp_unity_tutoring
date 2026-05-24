# Project Concepts — Option Cards

Six options, ordered from most-recommended to most-ambitious. Each is
realistic for a focused 8th grader working 4–8 hours per week for 3–6
months with AI-assisted coding. Warren should pick **one**.

Acronyms used throughout this document:
- **CAC** = Congressional App Challenge (U.S. Congress competition).
- **G4C** = Games for Change (nonprofit that runs the Student Challenge).
- **NPC** = Non-Player Character (any character in a game controlled by
  code, not by the human player).
- **MVP** = Minimum Viable Product (smallest version of the project that
  can be shown to a judge or user).
- **AI / LLM** = Artificial Intelligence / Large Language Model (e.g.,
  the technology behind tools like ChatGPT and Claude).
- **WebGL** = a browser technology Unity can build to, so the game runs
  in any web browser without installation.

For every option, "Why it wins" is written against the rubrics used by
the primary target competitions (CAC and G4C). MIT THINK is noted as a
future target for Warren's 9th-grade year (he is not eligible as an
8th grader).

---

## Option 1 — "Counsel" ⭐ primary recommendation (app)

**Tagline:** *Ask any question. Three philosophers answer.*

**What it is:** A web app where you type a question you're wrestling
with ("Should I quit gymnastics?", "Is it okay to lie to protect a
friend?", "Why do I procrastinate?"). The app returns three answers in
the voice of three different philosophers — e.g. a Stoic (Marcus
Aurelius / Epictetus), an Existentialist (Camus / de Beauvoir), and an
Ethicist (Kant / Mill / Confucius). Each answer is grounded in a real
quote or doctrine and extended with an AI (Artificial Intelligence)
generated explanation the philosopher *would plausibly give* based on
their actual writings.

**The problem it solves:** Middle-schoolers and teens face constant
low-stakes-but-loaded decisions (Should I quit this activity? Should I
tell my parents? Is my friend actually a friend?) and they either
bottle them up or ask social media, which tends to amplify impulsive
answers. There is very little age-appropriate, non-preachy, non-
therapeutic content that *slows a kid down* and gives them more than
one angle on a problem.

**Why it matters:** Decision-making under emotional load is a core
adolescent skill and a documented risk factor in youth mental health.
The Digital Thriving Playbook (from the Center for Digital Thriving at
Harvard Graduate School of Education) explicitly calls for tools that
build reflection and perspective-taking. Counsel is that kind of tool
— a friction-adding, perspective-multiplying app, not a
therapy substitute.

**Impact (what "good" looks like):** A student types a real dilemma,
reads three genuinely different philosophical takes, and either
changes their mind or walks away more certain of the choice they were
already leaning toward. Success is measurable as *"did the user open
the app again the next time they had a hard decision?"* — a retention
signal we can actually instrument in the **MVP** (Minimum Viable
Product).

**Core loop:**
1. User enters a dilemma.
2. App picks (or user picks) 3 philosophers from a curated roster.
3. Each philosopher gives a short structured answer: *Real quote →
   Modern paraphrase → One question back to the user*.
4. User can save answers into a journal, reread them, and watch the
   philosophers "disagree" in a debate view.

**Why it fits Warren:**
- Smaller scope than a full game — finishable in 3–4 months.
- NPC-flavored: each philosopher is effectively a character with
  voice, tone, and beliefs. Warren's **NPC** (Non-Player Character)
  design instincts transfer directly.
- Text-first UI (User Interface), so art work is minimal.

**Why it wins (rubric alignment):**
- **Congressional App Challenge (CAC):** AI + mental health + personal
  story. 2025 winners repeatedly paired AI with an adolescent
  emotional need (anxiety, identity, decision-making). This is a
  textbook fit.
- **Future — MIT THINK (next year, when Warren is in 9th grade):**
  trivially reframes as a research question — "Can structured
  philosophical multi-perspective AI responses help adolescents make
  better decisions under stress?" — with a small user study
  (10–20 teens) as evidence.

**Technical stack:** Next.js + TypeScript + any **LLM** (Large Language
Model) API, with a hard-coded fallback of curated real quotes so the
app works offline and so we never risk the AI inventing a fake
citation. Persistence via local storage or Supabase. Deployable free
on Vercel.

**Risks / watch-outs:**
- Must **not** present itself as therapy. Copy needs a clear
  disclaimer and a crisis-resources link (988, Crisis Text Line).
  Judges notice this.
- Quotes must be **cited** to real sources. The AI extends real
  writing; it never fabricates it. This is a feature, not a hurdle.

---

## Option 2 — "Kindred Village" ⭐ primary game recommendation

**Tagline:** *A cozy strategy game where winning means your villagers
actually get along.*

### The pitch in one paragraph

Imagine *Stardew Valley* crossed with a light, kid-friendly version of
*The Sims*. You play the unofficial "mayor" of a tiny village of
6–8 villagers (NPCs — Non-Player Characters). You don't farm, you
don't fight, you don't collect gold. Each in-game day you get a small
pool of **action points** (say, 3 per day), and you spend them on the
villagers: visiting them, introducing them to each other, planning
small events, helping them through rough patches. The game runs for
30 in-game days. You "win" when the village hits a target
**Community Health score** — and you keep playing to beat your own
best score. It's a strategy game, but the strategy is social.

### What you actually see on the screen

- A **top-down 2D village** with little sprite houses, a town square, a
  cafe, a garden — the cozy pixel-art or low-poly look kids already
  know from Stardew Valley, Animal Crossing, and Unpacking.
- Villagers walking around doing their own thing. Click on one and a
  **character card** opens showing: their name, a portrait, a short
  bio, a **Mood bar** (😀 → 😟), and their **Trust meters** with every
  other villager (little bars from 0 to 100).
- A **Village Health dashboard** at the top of the screen with three
  numbers: average Mood, average Trust, and "Loneliest Villager"
  (the NPC with the lowest total trust — the game nudges you to
  notice them).
- An **Action menu** at the bottom with 4–5 buttons: *Visit*,
  *Introduce*, *Host Event*, *Build*, *End Day*.

### What "trust" actually is (the mechanic)

"Trust" is not hand-wavy — it's a literal number in the code between
every pair of villagers, from **0 to 100**.

- At the start of the game, every pair has a small random trust value
  (say, 10–30). They're acquaintances, not friends.
- Trust **goes up** when two villagers have a positive shared
  experience: the player introduces them, they attend the same event,
  they are paired on a village project (fixing the bridge, planting
  the garden).
- Trust **goes down slowly over time** if two villagers never
  interact — the "drifting apart" mechanic. This is what forces the
  player to make real strategic choices: you can't just max-out two
  villagers and ignore the rest.
- Trust **drops sharply** after a conflict event (a small, randomly
  triggered argument — nothing dark, just "Maya felt left out of the
  garden project"). The player then has to decide: do I spend my next
  action repairing that, or do I let it slide and focus elsewhere?

The "aha" moment for a judge watching the demo is seeing the player
**trade off**: you have 3 action points, 8 villagers, and a
relationship that just fractured. What do you do? That decision *is*
the game.

### A concrete example turn

> **Day 12.** The dashboard shows: Average Mood 72, Average Trust 58,
> Loneliest Villager: Theo. You click Theo. His card shows Mood 40
> (sad face), and his highest trust is only 22 (with Maya). You spend
> Action Point 1 on **Visit Theo** — a short dialog box pops up where
> Theo mentions he misses his old friend Ravi. You spend Action Point
> 2 on **Introduce Theo → Ravi** at the cafe. A little cutscene plays
> (two sprites walk to the cafe, a speech bubble, a small +8 Trust
> floats up between them). You save your last Action Point for
> tomorrow. You click **End Day**. Time passes. The next morning,
> Theo's mood has risen to 55 and a new Trust bar (Theo ↔ Ravi: 18)
> has appeared on his card. The dashboard ticks up. You feel it.

That is the whole game loop, repeated with growing complexity.

### Why the mechanics are the point

Most online games middle-schoolers play treat other people as
obstacles or resources. *Kindred Village* treats other people as the
entire game. The player's skill grows as they learn to **read a
village** — who needs attention, who's drifting, which pairing will
spark, which event will backfire. That is a transferable real-world
skill, taught through a strategy game kids actually want to replay.

This maps directly to the **Games for Change (G4C) Student Challenge**
mission ("games for social impact") and to two ideas from the Digital
Thriving Playbook (from the Center for Digital Thriving at Harvard
Graduate School of Education):

- **GAMERS framework** — *Groupness, Assurance, Mastery, Exchange,
  Repetition, Setting* — six social needs games can meet. Kindred
  Village hits all six by design.
- **TAGG loop** — *Trigger → Action → Gratitude → Glory* — the
  feedback shape of any healthy multiplayer or social experience.
  Every action in the game follows this loop (a villager's need is
  the trigger, the player's help is the action, the villager's
  reaction is the gratitude, the dashboard ticking up is the glory).

The demo video can literally overlay these letters on gameplay clips.
Judges eat that up.

### The **MVP** (Minimum Viable Product) — what ships first

To keep scope honest, the first playable version contains **only**:

- **6 villagers**, each with a 2-sentence bio, a unique portrait, one
  hobby, and one "sore spot."
- **3 action types**: Visit, Introduce, Host Event.
- **1 map**, 1 season, 1 soundtrack loop.
- **14 in-game days** per run (about a 15–20 minute play session).
- **1 end-screen**: a graph showing each villager's mood curve and
  the final Community Health score.

Everything beyond that (Build mode, seasons, more villagers, a story
mode) is stretch scope, added only if the **MVP** is fun *without* it.
If the **MVP** isn't fun at 6 villagers, adding more won't save it —
this is the single most important rule of the project.

### Impact (what "good" looks like at demo day)

A playtester (friend, classmate, sibling) plays one 15-minute
session. After, without being prompted, they can tell you:

1. Which villager they were most worried about.
2. What they did to help.
3. Whether it worked.

If they can narrate the session as a **story about people** rather
than as a score, the mechanics worked. That's the demo video:
3 real middle-schoolers playing, then recapping what happened in the
village in their own words.

### Why it fits Warren

- Strategy game. NPC-heavy. Non-violent. Unity-shaped.
- A natural next step from his existing Unity **NPC** work — same
  engine, same mental model, just a bigger world and a social
  simulation layer on top.
- Sprite / 2D top-down art keeps art scope tiny — no 3D modelling,
  no animation rigging.

### Why it wins (rubric alignment)

- **Games for Change (G4C) Student Challenge:** this is literally
  the genre they reward — games whose mechanics *teach empathy*
  rather than games that are *about* empathy.
- **Congressional App Challenge (CAC) (as a web game):** the game
  counts as an "app" if it ships in-browser as a Unity **WebGL**
  build or an itch.io embed. Pairs strongly with the personal-story
  framing CAC judges respond to ("I built this because the games my
  friends and I play don't teach us how to be friends").

### Technical stack

Unity 2D (top-down), with a **WebGL** (web-browser) build so it runs
in any browser without installing anything. NPC behavior driven by
simple **utility AI** (rule-based, not **LLM**-based — much easier
for an 8th grader to debug). Optionally layer in a lightweight **LLM**
(Large Language Model) only for flavor dialogue lines, but the game
must work without it.

### Risks / watch-outs

- **Scope creep.** Village sims are notorious for bloating. Hold the
  **MVP** line above — 6 NPCs, 3 actions, 14 days — until it's
  genuinely fun, *then* add.
- **Readability.** Players must be able to tell *why* trust went up
  or down. Every trust change must show a floating `+5` / `-3`
  number above the villager, with a one-line reason in a log panel.

---

## Option 3 — "NPC Republic" (strategy / god-game)

**Tagline:** *Design citizens. Watch a society emerge.*

**What it is:** A sandbox god-game where the player designs NPCs by
picking a handful of traits (curious, anxious, generous, stubborn, etc.)
and places them on a shared map. The simulation then runs, and emergent
social dynamics — friendships, factions, rumors, reconciliation —
play out. The player's only tools are **nudges**: suggest a
conversation, build a park, host a festival. Victory is defined by the
player ("I want them to become a peaceful trade hub", "I want them to
become a community that supports its outsiders").

**Why it fits Warren:** closest to pure strategy-game territory.

**Why it wins:** strong originality angle; it's *about* the kind of
game Warren wants to see exist instead of toxic multiplayer. Plays well
in the demo video — you can visibly speed-run a "good" society
emerging.

**Stack:** Unity ECS or plain Unity + custom behavior tree. WebGL build
for CAC submission.

**Risks:** highest-risk option on this list because emergent sims are
hard to *tune* into something fun. Only pick this if Warren has
shown he can iterate on game feel.

---

## Option 4 — "Lighthouse" (co-op-style single-player puzzle game)

**Tagline:** *Help NPCs through tough moments. One conversation at a time.*

**What it is:** A single-player narrative puzzle game. Each "level" is
an NPC going through something hard — first day at a new school, a
friendship falling apart, a family move. The player's job is to help
them by choosing dialogue and small actions grounded in real
evidence-based techniques (cognitive reframing, active listening,
grounding exercises). Levels are short (5–10 minutes each).

**Why it fits Warren:** strong narrative hook, NPC-driven, no
combat, no time pressure.

**Why it wins:** clearest mental-health story of any option, which makes
the demo video very strong. Pairs well with the MIT THINK proposal
because each technique can be cited to a peer-reviewed source.

**Stack:** Unity 2D or a web-based visual novel framework (Ink +
React).

**Risks:** very writing-heavy. Only pick if Warren enjoys writing
dialogue.

---

## Option 5 — "Quorum" (debate strategy game)

**Tagline:** *Win by convincing, not conquering.*

**What it is:** A turn-based strategy game where two or more players
(or player vs AI) represent factions in a town council debating real
civic questions — zoning, school lunches, a new park. Each turn you
play "argument cards" that use real rhetorical moves (appeal to ethos,
bring in data, concede a point). Winning requires building consensus
across factions, not defeating them.

**Why it fits Warren:** pure strategy, zero violence, very Warren.

**Why it wins:** unique civics angle. CAC (being run by *Congress*)
has a soft spot for civic-engagement projects.

**Stack:** Unity 2D or a web app with a game loop (React + Zustand is
fine if Unity feels too heavy).

**Risks:** balancing argument cards takes many passes. Start with a
small card set (~20).

---

## Option 6 — "Muse" (creative-prompt companion app)

**Tagline:** *A philosopher, a poet, and an artist walk into your
notebook.*

**What it is:** A companion app for creative work. You write what
you're stuck on. Three AI "muses" (philosopher, poet, visual artist)
each give a one-paragraph push in their own voice, plus a concrete next
exercise. Think of it as Option 1's softer cousin — aimed at creative
block, not life decisions.

**Why it fits Warren:** simplest option on the list. Good fallback if
scope on Options 1–5 feels too big.

**Why it wins:** easier rubric story for pure CAC — "I built a tool
that helps students get unstuck on homework and creative projects."
Less ambitious than Option 1, but also less risky.

**Stack:** Same as Option 1.

---

## Quick comparison

| Option | Genre | Scope | Best fit | Risk |
|--------|-------|-------|----------|------|
| 1. Counsel ⭐ | Web app | Small | CAC (this year) + MIT THINK (next year) | Low |
| 2. Kindred Village ⭐ | Unity strategy | Medium | G4C (next cycle) + CAC (this year) | Medium |
| 3. NPC Republic | Unity sandbox | Large | G4C | High |
| 4. Lighthouse | Narrative game | Medium | CAC | Medium |
| 5. Quorum | Strategy | Medium | CAC (civic angle) | Medium |
| 6. Muse | Web app | Small | CAC | Low |
