# Project Options — Pick One

A short menu of projects you could build over the next 3–6 months. Each
one is designed to be **fun to build**, **finishable** at 4–8 hours per
week, and **submittable** to one or more real coding competitions that
look great on a college application later.

A few quick acronyms used below:

- **CAC** = Congressional App Challenge — a coding contest run by the
  U.S. Congress where every member of Congress picks a winning app from
  their district.
- **G4C** = Games for Change Student Challenge — a contest for games
  with a positive social impact.
- **GDWC** = Game Development World Championship — a global student /
  hobbyist game contest.
- **MLH** = Major League Hacking — runs weekend hackathons; their
  "Junior" events are for students under 18.
- **NPC** = Non-Player Character — any character in a game controlled
  by code, not by a human player.
- **MVP** = Minimum Viable Product — the smallest version of the
  project that's already playable / usable.
- **WebGL** = a Unity export option that lets a Unity game run inside
  a regular web browser, no install needed.

---

## How to use this doc

1. Read all six options.
2. Star the 1–2 you'd actually want to spend a Saturday afternoon
   working on. That's the real test — pick what you'll *want* to come
   back to in week 4.
3. We'll pick one together and start Month 1.

You only build **one** project. Two of the options also unlock a
second competition for free, which is noted on each card.

---

## Option 1 — "Counsel" (web app) ⭐ smallest scope

> *Ask any real question. Three philosophers answer.*

**The idea.** A web app where you type something you're actually
wrestling with — *"Should I quit the team?", "Is it okay to lie to
protect a friend?", "Why do I keep procrastinating?"* — and three
historical philosophers (Marcus Aurelius the Stoic, Simone de
Beauvoir the Existentialist, Confucius the ethicist, etc.) each give
you a real, sourced quote that speaks to the situation, plus a short
plain-English explanation of what they'd actually mean by it. You
can save answers to a journal and watch the philosophers
"disagree" in a debate view.

**Why it's good.** Search engines give you noise. Social media gives
you opinions. AI chatbots make stuff up. There's almost no tool that
slows a teen down, hands them three real perspectives on a hard
decision, and then steps out of the way. That's a real gap, and it's
small enough to actually ship.

**Why it's a strong fit.** Each philosopher is basically an **NPC** —
a character with a voice, a worldview, and a set of beliefs. If you
like writing **NPC** dialogue, this is a 3-NPC project with no
combat and no level design. Smallest scope on the list.

**What you'd actually build.** A Next.js web app. Hard-coded list of
real philosopher quotes (so the AI never invents a fake one). The AI
extends the real quote into a modern explanation. Free hosting on
Vercel.

**Competitions this hits:**

- **Congressional App Challenge (CAC)** — primary target this year.
  Solo allowed. Submission window: late October 2026. Registration
  opens **May 1, 2026**.
- **MIT THINK Scholars Program** — for next year (9th grade). The
  same project becomes a research-proposal entry. Free second
  competition, no extra build.

---

## Option 2 — "Kindred Village" (Unity game) ⭐ if you want to make a real game

> *SimCity, but for trust. Build the village; the villagers walk
> on their own.*

**The idea.** A cozy, top-down 2D strategy game. A small village of
6–8 villagers (NPCs) walk around on their own — they have their own
schedules, favorite spots, and moods. **You don't control them.** What
you do control is the **map**: you place benches, cafes, gardens, and
murals. When two villagers cross paths *near* one of your placed
objects, they have a positive interaction and **trust** between them
goes up. When they cross paths on a bare tile, they might have a
small clash and trust goes down.

The whole game is the spatial puzzle: limited pieces, limited space,
8 villagers with different personalities, and a 3-minute "week."
You win when the village's average **Trust** and **Mood** both
cross a target — meaning you designed a village where strangers
became friends.

**There's a playable prototype of this in `kindred_village_demo/`
already.** Open `index.html` in a browser. It's rough — colored
circles, no art, no sound — but the actual mechanic is in there and
runs in real time.

**Why it's good.** Most of the online games kids actually play
reward domination, toxicity, or solo grinding. This one is a
strategy game whose *winning strategy* is paying attention to other
people. Same satisfaction-of-strategy as a tower defense or a city
builder, except the thing you're optimizing is the social fabric of
a tiny town.

**Why it's a strong fit.** Pure strategy. Heavy **NPC** work. No
violence, no guns. Unity-shaped (you're already learning Unity). Art
scope is tiny — small sprites, top-down 2D.

**What you'd actually build.**

- **6–8 villagers**, each with a name, a portrait, a 2-sentence bio,
  and a small personality (one likes cafes, one avoids crowds, one
  loves the garden).
- **Autonomous wander AI** — villagers pick a random spot, walk
  there, pause, repeat. Speed and behavior shift with mood.
- **4 piece types** to place: bench, cafe, garden, mural.
- **Pairwise trust matrix** — every pair of villagers has a Trust
  number from 0 to 100. Encounters near your objects raise it;
  bare-tile encounters can lower it.
- **One small map**, **one 3-minute "week,"** end-of-week grade
  (S / A / B / C / D), instant lose if any villager hits 0 mood.
- Built in Unity, exported to **WebGL** so it runs in a browser.

**Competitions this hits:**

- **Congressional App Challenge (CAC)** — primary this year. The
  Unity **WebGL** build counts as an "app." Same May 1, 2026 reg /
  late October 2026 deadline as Option 1.
- **Games for Change (G4C) Student Challenge** — perfect fit for this
  game, but the next cycle deadline is March 2027. Same Unity build,
  free second submission.
- **Game Development World Championship (GDWC)** — summer submission
  window, same build, no extra work.

One project, three competitions. Highest-leverage option on the list.

---

## Option 3 — "NPC Republic" (sandbox simulation)

> *Design citizens. Drop them on a map. Watch a society emerge.*

**The idea.** A god-game / sandbox where you design **NPCs** by
picking a few traits (curious, anxious, generous, stubborn) and place
them on a shared map. The simulation runs and emergent stuff happens —
friendships, rivalries, factions, festivals, reconciliation. You don't
have a "win" screen; you set your own goal ("I want this town to
become a peaceful trade hub" or "I want them to be a community that
takes care of its outsiders").

**Why it's good.** It's the most "you can't predict what'll happen"
option. When it works, the demo video is amazing — you can watch a
society form in 90 seconds.

**Why it's risky.** Emergent simulations are hard to *tune* into
something fun. Lots of rebalancing. Higher ceiling, higher risk.
Pick this only if you've already shipped Option 2 or want to push
yourself.

**Competitions this hits:**

- **G4C Student Challenge** (next cycle, March 2027 deadline).
- **CAC** (Unity **WebGL** build, this year).

---

## Option 4 — "Lighthouse" (narrative puzzle game)

> *A lighthouse keeper helps lost travelers through the fog.*

**The idea.** A short, atmospheric narrative game. Each "level" is
one **NPC** going through something hard — first day at a new school,
a friendship falling apart, a family move. You play the lighthouse
keeper, and you help by choosing dialogue and small actions grounded
in real, evidence-based techniques (active listening, reframing,
grounding). Levels are short, 5–10 minutes each.

**Why it's good.** Strongest "story" of any option on this list.
Demo videos write themselves.

**Why it might not fit.** It's writing-heavy. You'd be writing
dialogue, a lot of it. Pick this one only if writing dialogue sounds
fun, not like homework.

**Competitions this hits:**

- **CAC** (Unity **WebGL** build).
- **G4C Student Challenge** (next cycle).

---

## Option 5 — "Quorum" (debate strategy game)

> *Win by convincing, not conquering.*

**The idea.** A turn-based strategy game where two players (or
player-vs-AI) represent factions on a town council debating real
civic questions — zoning, school lunches, a new park. Each turn you
play "argument cards" that use real rhetorical moves (cite data,
appeal to shared values, concede a point). You win by building
**consensus** across factions, not by defeating them.

**Why it's good.** Pure strategy. Zero violence. Unique civics
angle — and **CAC** is literally run by the U.S. Congress, so a
civic-themed project has a built-in story.

**Why it's tricky.** Card games take a lot of balancing passes.
Start with a small set (~20 cards) so it stays playable.

**Competitions this hits:**

- **CAC** — strong civic angle.
- **G4C Student Challenge** (next cycle).

---

## Option 6 — "Muse" (creative-prompt web app)

> *A philosopher, a poet, and an artist walk into your notebook.*

**The idea.** A companion app for creative work. You write what
you're stuck on. Three AI "muses" (a philosopher, a poet, a visual
artist) each give a one-paragraph push in their own voice plus a
concrete next exercise to try. Think of it as Option 1's softer
cousin — for creative block instead of life decisions.

**Why it's good.** Smallest scope on the list. Lowest risk. Solid
fallback if Options 1–5 feel too big.

**Why it might not fit.** Less ambitious than the others. Won't have
the same "wow" factor in a demo if you compare it to a finished
strategy game.

**Competitions this hits:**

- **CAC** — solid solo entry.

---

## Quick comparison

| Option | What it is | Genre | Effort | Competitions |
|--------|-----------|-------|--------|--------------|
| 1. Counsel ⭐ | Web app, 3 philosophers | Reflection tool | Small | CAC + (next year) MIT THINK |
| 2. Kindred Village ⭐ | "SimCity for trust" | Unity strategy | Medium | CAC + G4C + GDWC |
| 3. NPC Republic | Sandbox society sim | Unity sandbox | Large | CAC + G4C |
| 4. Lighthouse | Narrative puzzle | Story game | Medium | CAC + G4C |
| 5. Quorum | Debate strategy | Card / strategy | Medium | CAC |
| 6. Muse | Creative-block helper | Web app | Small | CAC |

---

## The competitions, briefly

These are the real contests we'd actually submit to.

### Congressional App Challenge (CAC) — primary target this year

- **What:** U.S. Congress's official student coding contest. One
  winner per congressional district, recognized by the local
  Representative. Submit a working app + a 3-minute demo video +
  a written description.
- **Eligibility:** U.S. middle and high school students. Solo allowed.
- **Website:** https://www.congressionalappchallenge.us
- **Registration opens:** May 1, 2026.
- **Submission deadline:** late October 2026.
- **Hits these options:** All six. Any of them can submit here.

### Games for Change (G4C) Student Challenge — only if a game

- **What:** A nonprofit contest for student games with social impact.
- **Eligibility:** Middle / high school students in participating
  regions. NY is in.
- **Website:** https://www.gamesforchange.org/studentchallenge/
- **Timing:** The 2026 cycle is already closed. Next cycle deadline
  is around late March 2027.
- **Hits these options:** Kindred Village, NPC Republic, Lighthouse,
  Quorum.

### Game Development World Championship (GDWC) — bonus, summer

- **What:** Global student / hobbyist game-dev contest. Re-submit
  the same Unity build you already have.
- **Eligibility:** Solo allowed. Global.
- **Website:** https://www.gdwc.com
- **Timing:** Summer submission window.
- **Hits these options:** Kindred Village, NPC Republic, Lighthouse,
  Quorum.

### MIT THINK Scholars Program — next year, not this year

- **What:** A research-proposal contest for high schoolers.
  Mentorship from MIT undergrads + project funding.
- **Eligibility:** **Grades 9–12 only.** Not eligible this year.
  Becomes a real target in 9th grade.
- **Website:** https://think.mit.edu
- **Timing:** Applications open Nov 2026, deadline early January 2027.
- **Hits these options:** Counsel translates the cleanest into a
  research proposal next year.

### Major League Hacking (MLH) Junior Hackathons — one practice run

- **What:** Weekend student hackathons. One weekend, build something,
  submit it.
- **Eligibility:** Under 18. Parent permission form required.
- **Website:** https://mlh.io
- **Timing:** Pick one weekend in months 2–4 as a low-stakes warm-up.
- **Hits these options:** All. It's practice, not the main event.

---

## What happens after you pick

1. We write a one-page spec for the option you picked (what's in,
   what's out, what "done" looks like).
2. Parent registers for **CAC** on **May 1, 2026** — that's day one.
3. Month 1 is the ugly first prototype. Goal: something that runs,
   even if it looks awful. Looks come later.
4. Months 2–5 are: build → playtest → rework → polish.
5. Month 6 is filming the 3-minute demo video and submitting.

You'll be using the same AI-assisted coding tools real engineers
use (GitHub, Copilot CLI, Claude Code) with a parent reviewing
changes before they're committed. The point isn't to do this alone —
the point is to ship something real and learn the workflow.

---

## Files in this folder, if you want to dig in

- `kindred_village_demo/index.html` — playable prototype of Option 2.
  Open in any browser, no install.
- `CONCEPTS.md` — longer write-ups of all six options.
- `COMPETITIONS.md` — full competition reference with judging
  criteria.
- `PLAN.md` — the 6-month plan we'd follow once an option is picked.
