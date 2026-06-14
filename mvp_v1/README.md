# Goblin Siege: Warlord's Cut — MVP v1

A Unity 6 implementation of the MVP scope from [../competition/warren_game_design_v2.md](../competition/warren_game_design_v2.md).

You are a goblin **Warlord**: command squads to breach human settlements, loot the **gold quota**,
and extract before the **Alarm** maxes out and the garrison sallies. Greed vs. survival.

> This folder contains the **C# scripts, the Input Actions asset, and raid data**.
> Unity scenes, prefabs, sprites, and `.meta` files are created in the Editor (see Setup).

---

## Requirements

- **Unity 6 LTS (6000.x)** — URP 2D template.
- Packages (Window ▸ Package Manager):
  - **Input System** (`com.unity.inputsystem`) — *required*. New Input System only.
  - **TextMeshPro** (`com.unity.textmeshpro`) — for HUD text.
- In **Edit ▸ Project Settings ▸ Player ▸ Active Input Handling**, choose **Input System Package (New)**
  (or *Both*). The game uses **no** legacy `UnityEngine.Input` calls.

---

## What's implemented (MVP, spec section 10)

| System | File(s) | Notes |
|---|---|---|
| **Alarm (the heart)** | `Systems/AlarmSystem.cs` | 0–100%, rises from time/kills/loot/breach; thresholds escalate the garrison; Full Sally = soft deadline. |
| **Quota (win number)** | `Systems/QuotaSystem.cs` | Looted gold vs. level quota. |
| **Loot vs. alarm trade-off** | `Systems/LootCache.cs` | Crate/Chest/Granary/Vault — more gold = bigger alarm spike. |
| **Breaching** | `Systems/Gate.cs` | Sappers breach gates over time; breach spikes alarm. |
| **Garrison response** | `Systems/GarrisonSpawner.cs` | Alarm-driven reinforcement waves; tougher units as alarm rises. |
| **Win / lose orchestration** | `Systems/RaidManager.cs` | WIN = quota looted **and** extract; LOSE = squad wipe **or** Full Sally without quota. |
| **Units + FSM AI** | `Units/*.cs` | Goblins (squad-commanded) + humans (Guard→Alert FSM). Auto-attack nearest enemy. |
| **Player (new Input System)** | `Player/WarlordController.cs`, `Player/SquadCommander.cs` | Warlord moves (WASD/arrows) + one-time Warhorn (H); squads selected 1/2/3/` and ordered (right-click). |
| **War-band meta** | `Meta/WarbandState.cs` | Persistent squads, surplus gold, recruit/revive — PlayerPrefs (WebGL-safe). |
| **HUD + screens** | `UI/*.cs` | Gold/quota + alarm meter, result panel, title, War-Camp. |
| **Data-driven raids** | `Data/Raids/raid-01,02,04.json` | Act 1 (1–2) + one Act 2 (4). |

Object pooling (`Systems/ObjectPool.cs`) is provided for the large-battle scale pass (spec section 9).

---

## Controls

| Input | Action |
|---|---|
| WASD / Arrows | Move the Warlord |
| `1` `2` `3` | Select squad 1/2/3 |
| `` ` `` (backquote) | Select all squads |
| Right-click | Order selected squad(s) to the cursor |
| `H` | Warhorn — drops the alarm by 10% (once per raid) |

Bindings live in `Assets/Input/GoblinControls.inputactions` and are remappable.

---

## Setup (one-time, in the Editor)

The scripts are engine-ready; you wire three scenes. Suggested minimal setup:

### 1. Raid scene (`Raid`)
1. Create an empty GameObject **`RaidManager`**, add `RaidManager`.
2. Add child GameObjects with `AlarmSystem`, `QuotaSystem`, `GarrisonSpawner`, `ExtractionZone`
   and assign them to the `RaidManager` fields.
3. Assign a **raid JSON** (`raid-01.json`) to `RaidManager.raidJson`
   (drag the TextAsset, or load from a `Resources` folder).
4. **Prefabs** to create and assign:
   - **Goblin unit** — sprite + `Rigidbody2D` (gravity 0) + `CircleCollider2D` + `GoblinUnit`. Assign to `RaidManager.goblinUnitPrefab` and `Squad` (via RaidManager) .
   - **Loot cache** — sprite + `LootCache`. Assign to `RaidManager.cachePrefab`.
   - **Gate** — sprite + `Gate`. Assign to `RaidManager.gatePrefab`.
   - **Human unit** — sprite + `Rigidbody2D` + collider + `HumanUnit`. Assign to `GarrisonSpawner.humanPrefab`; add a few `spawnPoints` (empty transforms).
5. **Warlord**: a GameObject with `Rigidbody2D`, `PlayerInput` (Actions = `GoblinControls`, Behavior = *Invoke C# Events* or *Send Messages*; default map = `Player`), `WarlordController` (assign `AlarmSystem`).
6. **Commander**: a GameObject with `PlayerInput` (same actions asset) + `SquadCommander` (assign `RaidManager` and the world `Camera`).
7. **HUD**: a Canvas with two Sliders + TMP labels + a result panel; add `RaidHUD` and assign bindings. Result panel gets `RaidFlowButtons`.
8. Add a **`WarbandState`** GameObject (persists via DontDestroyOnLoad; put it in the Title scene so it's created first).

### 2. Title scene (`Title`)
- Canvas with New Campaign / Continue / Quit buttons → `TitleScreenUI` methods.
- A `WarbandState` GameObject (singleton bootstrap).

### 3. War-Camp scene (`WarCamp`)
- Canvas with surplus/squads labels, recruit/revive buttons, March button → `WarCampUI` methods.

Add all three scenes to **Build Settings** with the names `Title`, `Raid`, `WarCamp`
(or change the serialized scene-name fields).

---

## Tuning

All economy numbers live in `Core/Balance.cs` and the per-raid JSON files
(`Data/Raids/*.json`). Target: a careful player wins with ~20–30% alarm to spare;
a greedy player trips Full Sally and loses (spec section 6).

---

## Unity 6 compliance

No deprecated APIs: new **Input System** only, `FindFirstObjectByType` family if needed,
Canvas + TextMeshPro (no `OnGUI`), `Rigidbody2D.linearVelocity` (not legacy `velocity`),
PlayerPrefs/JsonUtility for save (no `System.IO`), object pooling for WebGL-safe scale.
