# Goblin Siege: Warlord's Cut — Game Design Spec (v2)

> **One-liner:** You are a goblin Warlord. Command your war-bands to smash human settlements, loot enough gold to hit the raid quota, and escape before the garrison overwhelms you — across a campaign that escalates from a sleepy hamlet to a full-scale siege.

**Engine:** Unity 6 (latest LTS) · **Input:** new **Input System** package only (no legacy `Input.GetKey` / Input Manager) · **Render:** URP · **Platform:** WebGL (itch.io / Congressional App Challenge) + Windows
**Supersedes / layers onto:** [warren_game_design.md](warren_game_design.md) (camera, units, siege systems) and [GOBLIN_SIEGE_SPEC.md](GOBLIN_SIEGE_SPEC.md) (economy, unit tables). This v2 **inverts the loop**: you are the attacker raiding for a quota, not the defender holding a wall.

---

## 0. Player's answers (the design brief)

| Question | Answer | What it becomes in this spec |
|---|---|---|
| What am I? | Goblin commander / leader | **The Warlord** — an on-field hero who also commands squads (§3) |
| How do I win? | Hit a gold quota every level by looting human settlements | **Quota win condition** at a deadline (§5) |
| What kills me? | Missing the quota, or losing all squads | **Two distinct lose states** (§5) |
| What do I do every second? | Maneuver squads, use goblin abilities to attack & destroy fortifications | **Real-time RTS core loop** (§3) |
| What makes it harder? | Ramping quota, AI difficulty, more human units | **Difficulty curve** across the campaign (§6) |
| Coolest moment? | Large-scale, large-number battles | **The climax — the Town Siege** (§4) + swarm-scale tech note (§9) |

---

## 1. Diagnosis — is this a game, or a progress bar?

This concept is **already a game, not a progress bar**, *as long as we install one thing the brief is missing.* Run the checklist:

| Question | Status | Notes |
|---|---|---|
| Can the player **lose**? | ✅ | Two ways: miss quota, or lose all squads. |
| Can the player make a **wrong choice**? | ✅ | Overextend into a kill-zone; loot the wrong cache; trip the alarm too early. |
| Is a resource **scarce**? | ✅ | Squads, troop HP, ability cooldowns, and **time** are all finite. |
| Can the main number **go DOWN**? | ⚠️ **This is the gap.** | "Gold looted" only goes *up*. If looting has no downside, every second is equally correct and the raid is a chore. **Fix: install the Alarm.** |
| Does difficulty **escalate**? | ✅ | Ramping quota + AI + new human units (the brief asks for this). |
| Reason to **care** who's on screen? | ✅ (with meta) | Persistent named squads you recruit and upgrade between raids. |
| Would two players make **different** choices? | ✅ | Greedy "grab the vault" vs. cautious "clear the crates and extract." |

**The one missing pillar = pressure.** Looting gold that only rises is a progress bar. The fix is the **Alarm / Garrison Response** system (§3): every kill, every smashed cache, and every passing second raises the alarm, and a rising alarm spawns more, tougher human reinforcements. Now the main tension is **greed vs. survival** — the quota *forces* you to be greedy, the alarm *punishes* greed. That single downward-pressure system turns "loot the bar" into "how much do I dare take before I run?"

---

## 2. The four pillars

- **Goal:** *"Loot the gold quota from this settlement and get out alive."* (One sentence the player can repeat.)
- **Obstacle:** The settlement is walled and garrisoned, and the **Alarm** keeps rising — the longer you stay, the more humans pour in.
- **Choice:** Under a rising alarm and finite squads, **what do you breach, what do you loot, and when do you stop?** Push for the heavily-guarded vault, or bank the easy crates and extract?
- **Consequence:** Every choice moves you toward a win (quota met) **or** a loss (squads wiped / alarm maxed before quota) — and survivors carry into the next raid.

If any of these is unclear in a build, the build has drifted — return here.

---

## 3. Core loop (what you do every second)

A single raid is a real-time loop, ~3–6 minutes:

```
SELECT squads  →  MANEUVER to a fortification  →  BREACH it (sappers/abilities)
      ↑                                                      │
      │                                                      ▼
  EXTRACT  ←  decide: push deeper or bank?  ←  LOOT the cache (gold ↑, ALARM ↑)
```

**Goal → Obstacle → Choice → Consequence, every loop:**
- **Goal:** reach the next loot cache.
- **Obstacle:** a wall/gate + defenders + the rising alarm.
- **Choice:** which squad breaches where, which ability to spend, and whether the loot is worth the alarm it costs.
- **Consequence:** gold rises toward quota; alarm rises toward a garrison sally; squads take losses.

### Commanding squads (the moment-to-moment verbs)
Reuse the hybrid camera + selection from [warren_game_design.md](warren_game_design.md):
- **Select** a squad (click / 1–4 hotkeys) and **issue move/attack-move orders** (right-click).
- **Breach** order: send a Sapper squad to plant a charge on a gate/wall segment (interruptible — guards can stop it).
- **Abilities** (per goblin type, on cooldowns) are the "coolest" verbs: Berserk, Rally, Smoke screen, Bomb toss, Wolf-charge.
- **Loot** order: send a squad to a cache; they smash it over a few seconds → gold flows in.
- **Extract:** pull squads back to the entry edge to "bank" them safely for the next raid (survivors persist).

### The Alarm / Garrison Response (the new pressure system — MVP-critical)
- A single **Alarm meter (0–100%)** shown next to the gold/quota HUD.
- Alarm **rises** from: time passing (slow trickle), each defender killed, each cache looted (big spike), and breaching the inner keep.
- As Alarm crosses thresholds, the garrison escalates:
  - **0–33% (Unaware):** sparse patrols.
  - **34–66% (Alerted):** reinforcement waves spawn from barracks; crossbows man the towers.
  - **67–99% (Mobilized):** large waves, elite units (Knights/Pikemen), shortened spawn timers.
  - **100% (Full Sally):** the entire garrison sorties at once — effectively a soft deadline; surviving here is near-impossible. **This is the "deadline" that makes the gold number able to go *down* in spirit: stay too long and you lose everything.**
- **Design intent:** the alarm converts "loot forever" into "loot *fast and smart*, then run." It is the difference between a game and a progress bar — do not cut it.

---

### Reinforcement Timer (New)

Purpose: provide a clear, tunable delay before a large group of human reinforcements arrives. This creates tension, gives players a chance to prepare or evacuate, and makes encounters feel more dramatic.

- Gameplay: when a reinforcement trigger is reached (player enters area / alarm is raised / mission timer hits threshold), start a countdown. During the countdown the player can hear/see warnings. When the timer reaches zero, a large group of human reinforcements spawns and begins moving toward objectives.
- Feel: use an audible alarm and a visual countdown to build tension. Consider staged warnings: distant footsteps/voices at T-30s, alarms at T-15s, flash red lights at T-5s.

Parameters to expose for tuning:

- `initialDelay` (float): base time between trigger and first reinforcement wave (seconds). Default 60s.
- `warningTime` (float): how long before arrival to start visual/audio warnings (seconds). Default 30s.
- `waveCount` (int): how many waves of human reinforcements arrive. Default 3.
- `spawnInterval` (float): spacing between waves after the first arrival (seconds). Default 10s.
- `reinforcementsPerWave` (int): number of humans per wave. Default 6.

UI & Feedback:

- Show a prominent countdown in the HUD (big numeric timer), and optionally a radial timer for the immediate area.
- Play escalating audio cues as timer approaches zero.
- Flashing lights or environment changes can reinforce urgency.

Balancing notes:

- Shorter `initialDelay` increases pressure and reduces player decision time; lengthen for slower-paced rooms.
- Use `waveCount` and `reinforcementsPerWave` to scale difficulty rather than only decreasing `initialDelay`.
- Consider making the timer interruptible (player can disable alarm or destroy a comms object) for player agency.

Integration:

- Use a reusable `ReinforcementTimer` MonoBehaviour to start the countdown from triggers. The script exposes a UnityEvent you can hook to your spawner(s) to actually instantiate AI units when the timer completes.
- Hook UI elements (TextMeshPro or UI Text) to the timer's on-tick callback or poll `GetRemaining()` each frame.


## 4. Short & sweet story — the message IS the mechanic

**Thesis (one sentence):** *"Greed is a clock. The more you take, the louder the world gets — and the bravest raid is knowing when to run."*

The mechanic enacts the thesis with zero lore-dumping: **looting raises the alarm**, so the player physically *feels* greed summoning its own punishment. You don't read about the cost of greed — you gamble against it every raid.

**3 acts (campaign arc, ~10 levels):**
1. **Setup — "Easy Pickings" (Levels 1–3).** Undefended hamlets. Low quota, slow alarm. Teaches the loop: breach, loot, extract. The player feels powerful.
2. **Escalation — "They're Ready For Us" (Levels 4–7).** Walls, gates, garrisons, crossbows. New human units appear; alarm bites. Quota now demands pushing into guarded caches. Real greed-vs-safety decisions.
3. **Climax — "The Town Siege" (Levels 8–10).** Fortified towns, a Garrison Captain boss, and **large-scale battles** — your whole upgraded warband vs. a mobilized town. The coolest moment by design (§9 scale note). Win the siege → win the campaign.

**Show the thesis in the endings (both):**
- **Win screen:** *"You took what you needed and lived to spend it."*
- **Lose-to-alarm screen:** *"You reached for one more chest. The horns never stopped."* + **instant Replay Raid** button. A loss that names *why* teaches more than a win.

---

## 5. Win, lose, and progression (concrete)

### Win condition (per raid)
- **Loot ≥ Quota gold**, *then reach the extraction edge with at least one surviving squad.* (Banking the loot, not just touching it, is the win — this keeps the alarm meaningful right up to the exit.)
- Exact numbers in §7.

### Lose conditions (per raid) — both always reachable
1. **Squad wipe:** all squads destroyed → *"The warband is broken."*
2. **Quota failure:** Alarm hits 100% (Full Sally) and you have not banked the quota → you're forced to flee empty-handed → *"Not enough gold. The horde goes hungry."*

> If a build ever lets the player win by passively waiting, the alarm tuning is broken — see §1.

### Progression (the warband meta — why you care)
Between raids, spend **surplus gold** (anything looted *above* the quota) in the **War-Camp**:
- **Recruit** new squads (raise your cap) and **revive** fallen ones.
- **Upgrade** goblin types (HP/damage) and **unlock** new types and abilities (Wolfriders → Shamans → Trolls).
- Squads **persist and gain veterancy** across raids — a squad that survives three raids is stronger and *worth protecting*, which makes the "do I risk them for the vault?" choice hurt in a good way.

### Difficulty curve (answers "what makes it harder")
Each act ramps three dials in lockstep:
1. **Quota** rises (you must loot more, so you must stay longer → more alarm).
2. **AI difficulty** rises (faster reinforcement timers, smarter target priority, defenders hold chokes).
3. **Human roster** grows (Militia → Crossbow → Pikeman → Knight → Garrison Elite/Captain).

The climax raid must be the **hardest** moment — biggest quota, fastest alarm, full roster, boss.

---

## 6. Economy — playtest starting points (NOT gospel)

> **These numbers are a starting point. Tune until a careful, skilled player wins a raid with ~20–30% alarm to spare, and a greedy/careless player triggers the Full Sally and loses.** Reuse the stat tables already in [warren_game_design.md](warren_game_design.md) and [GOBLIN_SIEGE_SPEC.md](GOBLIN_SIEGE_SPEC.md); deltas below.

### Quota ramp
| Act | Levels | Quota (gold to bank) | Alarm fill speed | Garrison roster |
|---|---|---|---|---|
| 1 — Easy Pickings | 1–3 | 100 → 175 | slow | Militia only |
| 2 — They're Ready | 4–7 | 250 → 450 | medium | + Crossbow, Pikeman |
| 3 — Town Siege | 8–10 | 600 → 1000 | fast | + Knight, Garrison Elite, **Captain (boss)** |

### Loot caches (gold vs. alarm cost — the core trade-off)
| Cache | Gold | Alarm added on loot | Guarded? | Loot time |
|---|---|---|---|---|
| Crate | 10 | +3% | lightly | 2s |
| Chest | 25 | +6% | yes | 3s |
| Granary / Cart | 40 | +8% | yes | 4s |
| **Vault** (inner keep) | 100 | +20% | heavily | 6s |

> The vault is the greed trap: one vault ≈ the quota of an early level, but it spikes the alarm by a fifth and sits behind the most defenders. *That* is the central decision the whole game is built around.

### Your war-band (start of campaign)
| Goblin type | HP | DMG | Speed | Role | Ability (cooldown) | First unlock |
|---|---|---|---|---|---|---|
| Grunt | 30 | 6 | 3 | line breaker | Berserk: +50% dmg 5s (20s) | Lvl 1 |
| Spearthrower | 20 | 8 (rng 5) | 2.5 | ranged | Volley: AoE arrows (15s) | Lvl 1 |
| Sapper | 18 | 4 | 2 | breach gates/walls (BreachTime ~8s) | Demo Charge (25s) | Lvl 2 |
| Wolfrider | 30 | 12 | fast | flank / hit cache fast | Wolf-Charge: dash + knockback (18s) | Lvl 4 |
| Shaman | 35 | 0 | slow | heal/revive squad | Battle-Howl: heal AoE (20s) | Lvl 6 |
| **Warlord (you)** | 120 | 20 | 3 | hero + aura | **Rally** (regroup+morale) & **Warhorn** (alarm −10%, once/raid) | Lvl 1 |

- **Start:** Warlord + **3 squads** (e.g., 2 Grunt squads of 5, 1 Spearthrower squad of 5). Squad cap starts at 3, upgradable in War-Camp.
- **Warhorn** is the player's one safety valve against the alarm — a deliberate, scarce "buy time" button (use it for the vault grab or the extraction sprint).

### Human garrison (the obstacle that grows)
Reuse [GOBLIN_SIEGE_SPEC.md](GOBLIN_SIEGE_SPEC.md) enemy stats; introduction order by act:
Militia (Act 1) → Crossbow, Pikeman (Act 2) → Knight, Garrison Elite, **Garrison Captain boss** (Act 3). Higher alarm = faster spawns + elite units + tower crossbows active.

---

## 7. Screens & HUD

- **Title screen:** New Campaign / Continue (PlayerPrefs) / Quit. One-line pitch + "made for the Congressional App Challenge."
- **Raid HUD (always visible):**
  - **Gold / Quota** bar (e.g., `240 / 250`) — the win number, prominent.
  - **Alarm meter (0–100%)** with the threshold color shift (calm green → orange → throbbing red) — the pressure number.
  - **Squad roster:** portraits, HP, selection state, ability cooldown rings.
  - **Minimap** (caches, your squads, spawned reinforcements, extraction edge).
- **War-Camp (between raids):** spend surplus gold — recruit, revive, upgrade, unlock. Shows next level's quota so the player can plan.
- **Win screen:** loot banked, surplus, squads survived, → War-Camp.
- **Lose screen:** which lose state, *why* (one line), **Replay Raid** + **Back to War-Camp**.

---

## 8. Events / data-driven content

Follow the team's existing JSON-loader pattern (`levels.json`, `events.json`). Each raid is one data file so levels are tunable without recompiling:

```jsonc
// raids/raid-04.json  (schema — illustrative)
{
  "id": 4,
  "name": "Greymill Village",
  "act": 2,
  "quota": 250,
  "alarmFillPerSecond": 1.2,
  "garrisonRoster": ["Militia", "Crossbow", "Pikeman"],
  "reinforceIntervalByThreshold": { "alerted": 18, "mobilized": 10 },
  "caches": [
    { "type": "Crate", "pos": [12, 4] },
    { "type": "Chest", "pos": [20, 9], "guards": 2 },
    { "type": "Vault", "pos": [30, 15], "guards": 6, "inKeep": true }
  ],
  "extractionEdge": "south"
}
```

A small `RaidLoader` reads the file, spawns caches/garrison, and configures the `AlarmSystem` and `QuotaSystem`. (Engine work → goblin-decompose / ralph-build.)

---

## 9. Juice, scale, and audio

### The coolest moment — large-scale battles (tech note for build agents)
The brief's payoff is **lots of units on screen at once** in the Act 3 sieges. Design asks the build agents to plan for swarm scale from day one:
- **Object-pool** all units, projectiles, and loot-pop VFX (never Instantiate/Destroy mid-raid).
- Consider Unity's **Jobs/Burst** (or ECS) for crowd movement/steering if counts exceed a few hundred; flat sprites + simple flocking read great at scale.
- Squads move as **formations/groups**, not 200 individual click-targets — the player commands *bands*, the engine moves the many.
- Target a readable spectacle (silhouettes, banners, dust) over per-unit fidelity.

### Cheap juice (specify where; build agents implement)
- `+25` gold pop floating off a smashed cache; coins burst VFX.
- **Alarm escalation feedback:** screen edge reddens, a horn blares, music intensifies each threshold.
- Screen-shake on gate breach and vault crack.
- Berserk = red tint + speed-up; Wolf-Charge = dash streak; Warhorn = screen pulse + alarm bar visibly drops.
- Day-of-raid → win/lose fade transitions.

### Audio brief (hand off to `audio-game-designer`)
Key moments and the emotion each must carry:
- **UI/command confirm** — crisp, satisfying (issuing orders feels responsive).
- **Gate breach / vault crack** — heavy, rewarding.
- **Cache loot** — greedy little "cha-ching."
- **Alarm thresholds** — rising dread (a horn motif that escalates per threshold).
- **Ambient bed** — low war-drum tension that intensifies with alarm.
- **Win stinger** — triumphant horde roar. **Lose stinger** — horns closing in / the warband breaking.
> Sourcing (CC0/royalty-free), Unity AudioManager wiring, mixer groups, and attribution are the **`audio-game-designer`** agent's job — not designed here.

---

## 10. Scope ladder (jam discipline — cut from the bottom)

1. **MVP (must ship):** new Input System movement + squad select/move; 1 goblin type breach + 1 ranged; gate breach; **gold/quota win**; **alarm → reinforcements → both lose states**; 3 raids (Act 1 + one Act 2); title + win + lose screens; basic War-Camp (recruit/revive). *This alone is a complete, tense game.*
2. **Should have:** Sapper + Wolfrider + abilities; full War-Camp upgrades/unlocks; minimap; juice (pops, shake, alarm color/horn); music + 4–6 SFX (→ audio-game-designer); Acts 1–2 (7 raids).
3. **Nice to have:** Shaman/heal, veterancy, Act 3 Town Sieges + Garrison Captain boss, large-scale crowd tech, accessibility options (colorblind-safe alarm states, remappable Input System bindings).
4. **Cut first if behind:** **veterancy** and the **boss**, then collapse Act 3 to a single big raid. **Never cut the Alarm system** — it *is* the game (per §1). If forced, ship Act 1 + 1 Act-2 raid polished over a broken Act 3.

Every tier keeps what teaches the thesis (greed-vs-alarm); decoration is what gets cut.

---

## 11. Itch.io / Congressional App Challenge framing

- **Why it matters (one line):** *"A resource-pressure strategy game about the cost of greed — how taking 'just one more' summons the very thing that destroys you."* (Honest: this is primarily a systems/skill game; the thesis is a light, real hook, not a heavy social message — see §4.)
- **2-minute first run:** a judge should breach a hut, loot, bank the quota, and hit a win **or** trip the alarm and lose — and *get the greed-vs-survival point* — inside Act 1.
- **Accessibility:** keyboard + mouse via the **new Input System** with **remappable bindings**; colorblind-safe alarm states (shape/icon, not just red); readable fonts; no fail-on-reflex (it's tactical, not twitch).
- **Honest scope:** a small, finished 3-raid campaign with a real win/lose beats a sprawling broken one.

---

## 12. Tech requirements (hard constraints from the brief)

- **Unity 6** (latest LTS), **URP**, **2D or hybrid top-down** per [warren_game_design.md](warren_game_design.md) camera notes.
- **Input System package ONLY.** No `UnityEngine.Input` legacy calls, no Input Manager axes. Use an `.inputactions` asset + `PlayerInput` / generated C# class. (Explicit brief requirement: "does not use deprecated libraries such as older input system.")
- No other deprecated APIs (no legacy networking, no `WWW`, no deprecated particle/physics calls).
- WebGL-safe: pooling over GC churn; no threads unsupported on WebGL (guard Jobs/Burst usage for the WebGL target).

---

## 13. Hand-off

- **Audio** → `audio-game-designer` (audio brief in §9).
- **Build** → `goblin-decompose` to turn §3/§5/§6/§8 into a task DAG, then parallel `goblin-build` agents. Suggested dev order: Input System + camera/selection → squad/unit framework → **Alarm + Quota systems** (the heart) → loot/breach → garrison AI/reinforcements → War-Camp meta → HUD → juice → scale pass.
- **Design verification** → `goblin-verify` (does the build keep both lose states reachable and the alarm meaningful?).
- **WebGL build + itch.io deploy** → `unity-webgl-deployer`.

> Encoded as senior→junior chunks so the decompose→build pipeline can consume each section as a self-contained task with the acceptance criteria implied by §2 (four pillars) and §5 (win/lose).
```
