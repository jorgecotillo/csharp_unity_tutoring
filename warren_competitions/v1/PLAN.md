# Warren — 3 to 6 Month Competition Plan

## 1. Executive summary

Warren is an 8th-grade student in New York who has already shipped a working
Unity project with NPC mechanics. He likes **strategy games**, is drawn to
**NPC/AI-driven systems**, wants to work **mostly solo**, and does his best
work when a project is **fun, interactive, and visually rewarding**.

The parents' goal is twofold:

1. Produce a **concrete portfolio artifact** (a game or app) that is
   impressive on a college application.
2. Win or place in a **recognized competition** so there is a verifiable
   credential, not just a GitHub repo.

The strongest strategy over the next 3–6 months is:

> **Ship one polished project on a pro-social theme (mental health,
> empathy, community, or philosophy) and submit it to 2–3 competitions
> whose deadlines fall inside the 6-month window.**

One build, multiple submissions. This is how most teen winners actually
operate — the project is reused across Congressional App Challenge, a game
jam, and a research/innovation program.

## 2. Warren's profile signals (for designing the project)

These are the signals I used to pick the candidate project concepts:

- **Unity + NPC experience already exists** → a 2D/low-poly-3D strategy
  or simulation game is a realistic build, not a stretch.
- **Strategy-oriented** → turn-based, management, or "god-game" / social-sim
  mechanics are the right genre; avoid twitchy real-time combat.
- **Non-violent constraint** → fits the cozy-sim, management-sim,
  philosophical-app, and empathy-game genres very well.
- **Mental health + community theme from the parents** → this aligns
  almost perfectly with the **Digital Thriving Playbook** (UNICEF +
  Harvard "Thriving in Games" group), which is the current gold-standard
  research frame for pro-social game design. Judges at every competition
  below reward projects that cite real frameworks.
- **Prefers solo** → the **Congressional App Challenge (CAC)** allows
  solo entries. Game jams accept solo developers. Hackathons are
  optional, not required.
- **AI (Artificial Intelligence) allowed** → Warren can use Copilot CLI
  (command-line interface), Claude Code, spec-kit, ralph-loop, and
  sub-agents for implementation. AI as a *tool* is expected at this
  level; AI as a *feature* (**NPCs** — Non-Player Characters — with
  **LLM** (Large Language Model)-backed dialogue, philosopher-advisor
  chat, sentiment-aware game systems) is what the 2025 CAC winners
  actually shipped.

## 3. Competition strategy — "one build, multiple submissions"

Instead of building a separate project for each contest, we pick **one
core project** and re-submit it (sometimes with small adjustments) to the
competitions whose windows are open.

### Primary targets (strongly recommended)

| Priority | Competition | Why it's the core target | Submission format |
|----------|-------------|-------------------------|-------------------|
| 🥇 P1 | **Congressional App Challenge (CAC)** — Warren's NY congressional district | Solo-allowed, prestigious, recognized by the U.S. Congress. 2025 winners skewed heavily toward AI + social good + personal story. Registration opens **May 1, 2026**; submissions due **October 26–30, 2026**. | Working app (web, mobile, or desktop) + 3-minute demo video + written description. |
| 🥈 P2 | **Games for Change (G4C) Student Challenge** *(only if the build is a game, not a pure app)* | Explicitly rewards games with a social-impact theme — exactly the parents' ask. Middle-school "Junior" division (grades 5–8) is a natural fit. **Next cycle submissions open late 2026; deadline typically late March of the following year (~March 2027).** The 2026 cycle's March 30, 2026 deadline has already passed. | Playable game build + designer statement. |

### Stretch / opportunistic targets

| Priority | Competition | Role in the plan |
|----------|-------------|-----------------|
| S1 | **MLH (Major League Hacking) Junior / local hackathons** | Practice runs during months 2–4. 24–48 hour events. Low stakes, résumé-friendly. Solo participation allowed at most events; minors typically need parental permission. |
| S2 | **Game Development World Championship (GDWC)** (if we go the game route) | Summer submission window; re-use the same Unity build. Zero extra design cost. |
| S3 | **CodeDay** (New York, when scheduled) | Co-ed, open to middle and high school students, solo **or** team. Fun social experience but not part of the critical path since Warren prefers solo deep work. Keep an eye on the NY event calendar and opt in if a date lands inside the 6-month window. |

### Future target (not this cycle — eligibility gap)

- **MIT THINK Scholars Program** — restricted to **U.S. high school
  students, grades 9–12**. Warren is in 8th grade, so he is **not
  eligible this cycle**. This is a strong target for next year once he
  enters 9th grade. Keep the project design in mind so the same idea
  can be reframed as a research proposal later.

### Explicitly deprioritized

- **Canadian Computing Olympiad (CCO)** — Canada only.
- **Codeforces / AtCoder** — pure algorithm-sprint contests; do not
  produce a portfolio artifact, don't match Warren's strengths, and
  don't align with the mental-health / community theme.
- **Google Summer of Code (GSoC)** — aimed at contributing to existing
  open-source projects, not shipping original work, and historically
  oriented to older students. Not the right fit for an 8th grader
  trying to build a résumé-grade artifact.

## 4. 3–6 month timeline (relative months, not calendar dates)

Treat "Month 1" as the month you start. Adjust calendar dates around
CAC's published window for the current cycle.

### Month 1 — Pick one concept, lock the spec

- Warren reads `CONCEPTS.md` and picks **his favorite option**
  (parents have veto on scope, not on theme).
- Generate a formal **spec** (specification) using spec-kit / Copilot
  CLI. The spec should name:
  - Core loop (one paragraph).
  - 3 must-have features (**MVP** — Minimum Viable Product).
  - 2 nice-to-have features (only if ahead of schedule).
  - Target competitions and how the spec maps to each rubric.
  - Which Digital Thriving Playbook principle the project embodies (at
    least one — this becomes a quotable line in the demo video and any
    future research proposal such as MIT THINK next year).
- Register for the **Congressional App Challenge (CAC)** as soon as
  the 2026 portal opens on May 1, 2026.
- Set up the repository on GitHub, **CI** (Continuous Integration), and
  a ralph-loop runner for autonomous build iterations during
  weeknights.

### Month 2 — Vertical slice

- Ship a **playable / usable vertical slice**: one complete feature end
  to end. For a game: one NPC, one strategy choice, one resolution loop.
  For an app: one question answered by the system, start to finish.
- First playtest — Warren shows it to 2–3 classmates and/or a teacher.
  Write down every piece of feedback verbatim; judges love seeing
  "user research" in the submission write-up.
- Optional: enter one **MLH (Major League Hacking) Junior Hackathon**
  as a low-stakes sprint to practice shipping something in 24–48 hours.

### Month 3 — MVP complete

- All 3 must-have features working end-to-end.
- Accessibility pass: keyboard navigation, color-contrast, font size,
  audio-optional. (Every CAC rubric mentions usability; Digital Thriving
  Playbook explicitly calls out accessibility.)
- Second playtest — at least 5 users. Record short clips (with
  permission) for the demo video.
- Start a **"research journal"** — notes on what worked, what didn't,
  and citations for every claim. This becomes the raw material for a
  future MIT THINK proposal once Warren is in 9th grade.

### Month 4 — Polish + submission package

- Art/UX polish pass. Not everything needs to be original art — tools
  like Kenney assets, Itch free packs, or AI-assisted art are fine and
  explicitly allowed in all three primary competitions.
- **Write the 3-minute demo video script.** Structure: 15 s problem,
  30 s personal story / motivation, 90 s live demo, 45 s why it matters
  (cite the Digital Thriving principle) + future work.
- Record and edit the demo video. Warren narrates.
- Submit to **CAC** in his district.

### Month 5 — Iterate on feedback + secondary submissions

- Fix the 3 most common pieces of user feedback from playtesting.
- If the project is a game, prepare the build for the **Games for
  Change (G4C) Student Challenge** whose next cycle typically opens in
  late 2026 with a deadline around late March 2027.
- Submit to the **Game Development World Championship (GDWC)** if the
  relevant category window is open.
- Publish the project publicly (Itch.io for games, Vercel / Netlify for
  apps) so there's a permanent portfolio link before Month 6.

### Month 6 — Stretch goals, presentation prep, next-year planning

- If CAC district results come in: prep for any House of
  Representatives Showcase / Washington, DC presentation if Warren
  wins his district.
- Start a short **devlog (development log) post** (Medium, personal
  site, or a GitHub README page). Long-form writing about the project
  is what turns a competition entry into a college-application story.
- Decide whether to carry the project into summer with the **Game
  Development World Championship (GDWC)** or start a second, larger
  project.
- Map out **next year's MIT THINK** submission now that Warren will be
  eligible as a 9th grader (applications open November 1, deadline
  early January).

## 5. Recommended pick

If Warren has no strong preference, I recommend:

> **The "Counsel" philosopher-advice app** (see `CONCEPTS.md`) as the
> **primary** build, targeting the **Congressional App Challenge
> (CAC)** this cycle and **MIT THINK** next year.
>
> Rationale: it matches Warren's own expressed interest (the
> philosopher-eight-ball idea), it is *smaller in scope* than a full
> strategy game (meaning we can actually finish in 6 months with a
> polished result), it fits CAC's past-winner pattern almost perfectly
> (AI + personal story + social good), and it trivially converts into
> an MIT THINK research proposal next year ("Can AI-generated
> philosophical dialogue improve adolescent decision-making under
> stress?").

If Warren strongly prefers building a game, the recommended pick is:

> **"Kindred Village"** (see `CONCEPTS.md`) — a cozy strategy /
> **NPC** (Non-Player Character) management simulation where the win
> condition is the *emotional health of the village*, not combat.
> Targets **Games for Change (G4C) Student Challenge** (next cycle,
> ~March 2027) as primary and **Congressional App Challenge (CAC)**
> this fall as secondary (CAC accepts web games — they just need to
> run in a browser or as an installable app).

Either pick is defensible. The worst move is building two things at
once — we want one polished thing, multiple submissions.

## 6. What Warren needs to decide

See `DECISIONS.md` for a short checklist to go through with him before
Month 1 starts.
