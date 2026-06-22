# STORY_SPEC — Goblin Siege: "Turn the lights on"

> Build target: `mvp_v1` (Unity 2D, top-down, WebGL-safe). The whole demo runs from
> `Assets/Scripts/Bootstrap/RaidBootstrap.cs` (auto-starts, builds the scene + HUD in code).
> Keep every change WebGL-safe (no threads, no reflection-heavy APIs) and beginner-readable.

## 1. Diagnosis (why it feels boring)
The systems are good — there is a real **win** (loot quota banked at the extraction zone), two real
**losses** (squad wipe / alarm maxed), and a real **3-act escalation** (the `AlarmSystem` thresholds).
The game is invisible to the player:
1. Everything is an undifferentiated square — no visual language for who's who.
2. The red blocks (`HumanUnit`) look inert because Guard and Alert states render identically.
3. The thing the player actually drives (the Warlord) is in no danger and can't be targeted — zero stakes.
4. No onboarding: the player is never told the loop or the controls.
5. No moment-to-moment feedback (no hit flash, no death pop, no felt alarm).

## 2. The loop, win & lose (state it on screen)
**Loop (one sentence):** *Breach the gate, loot enough gold to hit quota, and get a squad to the
extraction zone — before your own noise raises the alarm to a Full Sally.*
- **WIN:** `quota.QuotaMet && extraction.AnyGoblinInside()` — already implemented (`RaidManager.CheckWin`).
- **LOSE:** all squads destroyed (`LostSquadWipe`) OR alarm reaches Full Sally before quota banked
  (`LostAlarmMaxed`) — already implemented. **NEW:** Warlord killed (`LostWarlordDown`) — see T5.

## 3. Three-act arc — reuse `AlarmThreshold`, build nothing new
The `AlarmSystem.OnThresholdChanged` event already fires the act transitions. Just dramatize them.

| Act | Threshold | Already happens | Feeling to surface |
|-----|-----------|-----------------|--------------------|
| 1 Sneak | Unaware (0–33%) | humans Guard, slow waves | "getting away with it" |
| 2 Pressure | Alerted→Mobilized (34–99%) | more humans charge, bigger waves | "they're waking up" |
| 3 Run | FullSally (100%) | `ForceAlert()` all + final wave | "RUN" |

---

## 4. Tasks (implementation-ready)

> Each task lists **Files**, **Do**, and **Acceptance**. Match existing code style (heavy teaching
> comments are expected in this repo). Do not add packages. Do not change balance numbers.

### T1 — Readability: color/shape language
**Files:** `Assets/Scripts/Bootstrap/RaidBootstrap.cs`
**Do:** Make roles instantly distinguishable by tint/scale where the bootstrap builds them:
- Goblins (`MakeGoblinPrefab`): keep green `(0.35,0.8,0.3)`.
- Warlord (`BuildPlayer`): change to **bright cyan** e.g. `(0.2,0.95,0.95)` and scale **1.0** (currently
  0.8 and greenish — too close to goblins). It must read as the unique hero.
- Loot caches (`MakeCachePrefab`): keep gold `(0.95,0.8,0.2)`.
- Gate (`MakeGatePrefab`): keep brown `(0.5,0.35,0.2)`.
- Extraction zone (built in `Start`): make it a clearly distinct **blue** translucent `(0.2,0.5,0.95,0.3)`
  and add a gentle pulse (small scale oscillation via a tiny `MonoBehaviour` or `Update` on a helper) so it
  reads as "the goal". Pulse is optional polish; tint is required.
**Acceptance:** On Play, a paused screenshot lets you identify warlord vs goblins vs loot vs gate vs
extraction at a glance. Warlord is visually unique (cyan, larger).

### T2 — Telegraph the red blocks (state-tint humans)
**Files:** `Assets/Scripts/Units/HumanUnit.cs`
**Do:** Tint the human's `SpriteRenderer` per FSM state so "guarding" and "charging" look different:
- In `GuardState.Enter()` → set color to a **dim grey-red** e.g. `(0.55,0.28,0.28)`.
- In `AlertState.Enter()` → set color to **bright red** e.g. `(1.0,0.2,0.2)`.
Cache the `SpriteRenderer` in `HumanUnit` (grab in `Awake` override or lazily). Keep it null-safe.
**Acceptance:** When a goblin enters a human's guard leash (or alarm escalates), that human visibly
brightens from dull to bright red as it switches to Alert; idle defenders are clearly dimmer.

### T3 — Hit flash (combat readability)
**Files:** `Assets/Scripts/Units/Unit.cs`
**Do:** Add a brief white flash when a unit takes damage. In `Unit` cache a `SpriteRenderer` (add
`protected SpriteRenderer Sprite;` set in `Awake`). In `TakeDamage`, if still alive, start a short
coroutine that sets color to white for ~0.08s then restores the previous color. Must not fight T2's
state-tint (restore to the *current* color, captured at flash start, not a hardcoded team color).
**Acceptance:** Every hit produces a quick visible flash; after the flash a human keeps its
Guard/Alert tint (T2) and a goblin keeps green.

### T4 — Death pop (kills feel like kills)
**Files:** `Assets/Scripts/Units/Unit.cs`
**Do:** In `Die()` (before/where `OnDied` fires), play a tiny "pop": e.g. a coroutine that quickly scales
the transform down to zero over ~0.12s, then deactivate/destroy as today. Keep it WebGL-safe (no
particle assets required; scale tween is fine). Ensure `OnDied` still fires so alarm/score logic runs.
**Acceptance:** Dying units visibly pop/shrink out instead of vanishing; raid end/score logic unaffected.

### T5 — Make the Warlord hunt-able (the standout mechanic)
**Goal:** Red blocks chase **whichever is closest — a goblin squad OR the Warlord** (player's stated
choice), and the Warlord can now die, ending the raid.
**Design:** Add a lightweight combat **proxy** so the Warlord becomes a valid target with ZERO changes to
`HumanUnit` AI, while keeping its bespoke input-driven movement.

**Files & changes:**
1. **New `Assets/Scripts/Units/WarlordUnit.cs`** — `public class WarlordUnit : Unit`:
   - Override `FixedUpdate()` to **do nothing** (no auto-attack, no velocity) so `WarlordController`
     keeps full control of movement. *Do not call `base.FixedUpdate()`.*
   - Public `Init()` that calls `ApplyStats(Team.Goblin, new UnitStats(hp:50, damage:0, speed:0,
     attackRange:0, attackInterval:1f))` so it has 50 HP, is on the Goblin team (humans treat it as an
     enemy), and never attacks.
   - Override the objective flag from change #3 to return **false** (it must not count as a looter/extractor).
2. **`Assets/Scripts/Core/GameEnums.cs`** — add `LostWarlordDown` to `RaidResult`.
3. **`Assets/Scripts/Units/Unit.cs`** — add `protected virtual bool CountsAsRaiderObjective => true;`
   (the Warlord proxy overrides to false). Used by #4.
4. **`Assets/Scripts/Units/CombatRegistry.cs`** — in `FindNearestGoblin` (and `CountAlive(Team.Goblin)`
   if it affects objectives), **skip units where `CountsAsRaiderObjective == false`**. This prevents the
   Warlord from satisfying loot range / extraction / win-by-standing-in-zone. `FindNearestEnemy` is
   **unchanged** (so humans still see the Warlord as a target).
5. **`Assets/Scripts/Systems/RaidManager.cs`** — add `public void NotifyWarlordDown()` that, if
   `Result == InProgress`, calls `EndRaid(RaidResult.LostWarlordDown)`.
6. **`Assets/Scripts/UI/RaidHUD.cs`** AND the inline HUD in **`RaidBootstrap.HookHud`** — add a result
   line for `LostWarlordDown`, e.g. title "DEFEAT", reason *"The warlord fell. A leaderless warband scatters."*
7. **`RaidBootstrap.BuildPlayer()`** — after adding `WarlordController`, also add `WarlordUnit`, call its
   `Init()`, and wire death to the raid: `warlordUnit.OnDied += _ => _raid.NotifyWarlordDown();`
   (the Warlord already has a `Rigidbody2D`, satisfying `Unit`'s `[RequireComponent]`).
**Acceptance:**
- Driving the Warlord near humans makes nearby humans switch to Alert and chase/attack the Warlord when
  it's the closest goblin-team target; its 50 HP depletes and at 0 the raid ends as `LostWarlordDown`
  with the new reason line.
- Loot, extraction and win logic are unchanged (Warlord standing in the extraction zone with quota met
  does **not** win; only real goblins do). Caches do not loot from the Warlord alone.

### T6 — Onboarding (tell the player the game)
**Files:** `Assets/Scripts/Bootstrap/RaidBootstrap.cs` (extend `BuildHud`), optional new
`Assets/Scripts/UI/OnboardingUI.cs` if cleaner.
**Do:** Using the same UGUI/legacy-`Text` approach already in `BuildHud`:
- **Goal banner** at raid start (auto-fades after ~5s): 3 lines —
  *"Breach the gate. Loot the quota. Reach the BLUE zone before the alarm fills."*
- **Controls card** (small, corner, persistent or fades): *"WASD move · 1/2/3 select squad · `
  select all · Right-click order · H = Warhorn (once)"* (mirror the real bindings in `BuildInputAsset`).
**Acceptance:** On Play the player sees the goal + controls without reading code; banner auto-dismisses.

### T7 — Threshold callouts + alarm screen-pulse (feel the escalation)
**Files:** `Assets/Scripts/Bootstrap/RaidBootstrap.cs` (extend `HookHud`).
**Do:** Subscribe to `_alarm.OnThresholdChanged` (already used for the bar color) and additionally:
- Show a brief **center-screen callout** per step-up: Alerted → *"ALERTED — they've seen you"*,
  Mobilized → *"MOBILIZED — the garrison musters"*, FullSally → *"FULL SALLY — RUN!"* (fade after ~2s).
- Flash a brief **red full-screen vignette/overlay** (a full-rect `Image` faded from ~0.4 alpha to 0 over
  ~0.4s) on each step-up so the player *feels* the heat rise.
**Acceptance:** Each alarm tier change pops a readable callout and a quick red screen pulse; no errors at
raid end when objects are torn down (guard against null/destroyed during fades).

---

## 5. Parallelization notes (for goblin-decompose)
Avoid concurrent edits to the same file:
- `Unit.cs` is touched by **T3, T4, T5(#3)** → keep these in one sequential lane.
- `RaidBootstrap.cs` is touched by **T1, T5(#7), T6, T7** → one sequential lane.
- `HumanUnit.cs` (**T2**), and `GameEnums.cs`/`CombatRegistry.cs`/`RaidManager.cs`/`RaidHUD.cs`/new
  `WarlordUnit.cs` (**T5**) can progress in parallel with the above where files don't overlap, but T5
  spans several files — schedule T5's `RaidBootstrap` + `Unit.cs` edits within those files' lanes.
Suggested order if serialized: **T5 core (new file, enums, registry, RaidManager, RaidHUD) → T3 → T4 →
T2 → T1 → T6 → T7**, doing all `RaidBootstrap` edits (T1/T5#7/T6/T7) together at the end.

## 6. Out of scope (do NOT add)
Hero abilities, leveling, new unit types, pathfinding/NavMesh, new art assets, balance retuning, audio
(flag separately for the audio agent: alarm step-up horns, Warhorn relief, hit/death/loot/win-lose stings).

## 7. Definition of done
All scripts compile; the bootstrap demo plays start→finish; the five "boring" symptoms in §1 are visibly
addressed; no new console errors during a full raid (win, squad-wipe, alarm-max, and warlord-down paths).
