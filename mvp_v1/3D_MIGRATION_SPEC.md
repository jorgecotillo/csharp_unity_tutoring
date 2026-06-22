# 3D_MIGRATION_SPEC — Goblin Siege goes 3D

> Build target: `mvp_v1`. The demo is code-built and auto-starts from
> `Assets/Scripts/Bootstrap/RaidBootstrap.cs`. Unity 6000.3.x. WebGL is the deploy target.
> This migrates the game from pure 2D (Rigidbody2D/SpriteRenderer/orthographic top-down) to **3D on a
> flat XZ ground plane**, swaps the human garrison for a **Mixamo Paladin** (walk/run animated), and adds
> a code-built **terrain/village**. Everything must remain beginner-readable and heavily commented.

## 0. Locked decisions
- **Full 3D**, gameplay stays on a **flat XZ plane** (no logic verticality). Coordinate mapping for all
  existing data/positions: old `(x, y)` → new `(x, 0, y)` (2D-Y becomes world-Z).
- **Humans = Paladin model** (animated). **Everything else = tinted 3D primitives** for v1
  (capsule = goblin/warlord, cubes/cylinders = caches/gate/props), keeping the readability tints from the
  prior pass — now as material colors.
- Keep all prior gameplay intact: greed-vs-alarm loop, 4 end states (`Won`, `LostSquadWipe`,
  `LostAlarmMaxed`, `LostWarlordDown`), the `WarlordUnit`/`INonObjectiveRaider` proxy logic, alarm arc.

## 1. GUARDRAILS — hard validation gates (re-check EVERY phase)
These are not aspirations; the Verify step must confirm each. If a change violates one, it's wrong.
- **G1 Camera:** a **tilted top-down RTS camera** (~50–60° pitch), looking down at the XZ board. NOT a
  behind-the-shoulder / first-person cam. The whole playfield stays readable at a glance.
- **G2 Flat gameplay:** all unit logic, targeting, ranges, orders and win/lose operate on the **XZ plane**
  (ignore Y for distances). No slopes/jumps affect logic.
- **G3 WebGL performance budget:** the Paladin is high-ish poly (~9 MB FBX). Therefore:
  - Cap **concurrent living humans** (hard ceiling, e.g. ≤ 12–16) — GarrisonSpawner must not exceed it.
  - Goblins/warlord/props stay **cheap primitives** (no skinned meshes) in v1.
  - One shared material per role where possible; avoid per-instance material allocations in `Update`.
  - No per-frame `Find*`/`GetComponent` in hot paths; reuse cached refs and the `CombatRegistry`.
- **G4 Readability:** role color language preserved (goblin green, warlord cyan+bigger, loot gold, gate
  brown, extraction blue, humans red w/ Guard-dim vs Alert-bright). Props are low/decorative and must not
  occlude unit silhouettes from the G1 camera angle.
- **G5 Frame-rate independence:** ALL motion/time logic uses `FixedUpdate` (physics) or scales by
  `Time.deltaTime`. No raw per-frame increments. Animator speeds derive from actual velocity. The Verify
  step must scan new/changed code for frame-dependent motion.

## 2. ART-SWAP SEAM — so real art drops in later with ZERO code changes
The user WILL add visual art. Build a single indirection so visuals are data, not hardcode.
- **New `Assets/Scripts/Visual/VisualLibrary.cs`** (static or injected). One method:
  `GameObject Spawn(string key, Vector3 pos, Quaternion rot, Transform parent)` that:
  1. Tries `Resources.Load<GameObject>($"Prefabs/{key}")`; if found, instantiate it.
  2. Else **falls back** to a code-built primitive (current behavior) tinted per role.
- Keys (stable contract): `Human`, `Goblin`, `Warlord`, `Cache_Crate`, `Cache_Chest`, `Cache_Granary`,
  `Cache_Vault`, `Gate`, `Wall`, `Watchtower`, `Cottage`, `Chapel`, `Barn`, `Barracks`, `Well`, `Tree`,
  `Rock`, `Fence`, `Bridge`, `Stall`, `Haystack`, `GroundField`, `GroundVillage`.
- Folder for future art: `Assets/Resources/Prefabs/` (a prefab named `<key>.prefab` overrides the
  primitive). Also reserve `Assets/Art/` for source models/materials (Paladin already under
  `Assets/Art/Characters/Paladin/`).
- `RaidBootstrap`, `GarrisonSpawner`, `Squad` must spawn **through `VisualLibrary`**, not `GameObject.CreatePrimitive`
  directly, so swapping a key to a real prefab needs no script edit. Gameplay components (Unit, colliders,
  Rigidbody) are attached by the spawner regardless of whether the visual is a primitive or an art prefab.

## 3. Phases

### Phase A — Core dimension flip (scripts) — todo `m3d-core`
**Files:** `Units/Unit.cs`, `Units/GoblinUnit.cs`, `Units/HumanUnit.cs`, `Units/Squad.cs`,
`Units/WarlordUnit.cs`, `Units/CombatRegistry.cs`, `Player/WarlordController.cs`,
`Systems/LootCache.cs`, `Systems/Gate.cs`, `Systems/ExtractionZone.cs`, `Systems/RaidManager.cs`,
`Systems/GarrisonSpawner.cs`.
**Do:**
- `Unit`: `[RequireComponent(typeof(Rigidbody))]`; `Rigidbody Body`; constrain so it stays upright and on
  the plane: `Body.useGravity = false`, `Body.constraints = FreezePositionY | FreezeRotationX | FreezeRotationZ`
  (allow Y-rotation for facing). Movement in `FixedUpdate` via `Body.linearVelocity` on XZ
  (`new Vector3(dir.x, 0, dir.z) * moveSpeed`). Face travel direction by yaw (cheap `LookRotation`).
- Replace every `Vector2`/`(Vector2)transform.position` with `Vector3` and XZ-plane distance helpers
  (ignore Y per G2). Keep `sqrMagnitude` comparisons.
- `WarlordController`: `Rigidbody` 3D; WASD `Vector2` input → `Vector3(x,0,y)` velocity; same constraints.
- `CombatRegistry`: `Vector3` math, distances computed on XZ (zero the Y delta) so a tall model isn't
  "farther" than a flat one.
- `LootCache`/`ExtractionZone`: range checks on XZ.
- `RaidManager.ToVec`/spawns: `[x,y]` JSON → `new Vector3(x, 0, y)`; deploy/extraction/gate/cache spawns
  in XZ. `GarrisonSpawner` spawn points → XZ; **enforce the G3 human cap** here.
- Preserve `WarlordUnit` empty `FixedUpdate`, `INonObjectiveRaider`, and `FindNearestGoblin` exclusion.
**Acceptance:** project compiles; units move/fight/loot/extract on the XZ plane; all 4 end states still
reachable; no `Vector2`/`Rigidbody2D`/`linearVelocity2D` left in gameplay scripts.

### Phase B — Scene/camera/lighting rebuild (RaidBootstrap + commander) — todo `m3d-scene`
**Files:** `Bootstrap/RaidBootstrap.cs`, `Player/SquadCommander.cs`, new `Visual/VisualLibrary.cs`.
**Do:**
- Camera: **perspective**, tilted ~55° pitch, raised and pulled south, framing the board (G1). Add a
  `Light` (Directional, dusk warm) + sensible ambient. Solid sky/bg color.
- Replace all sprite-square factories with `VisualLibrary.Spawn(key,…)` primitives (capsule/cube/cyl) +
  role material tints (G4). Attach Unit/Rigidbody/Collider as before.
- Reposition deploy/extraction/gate/caches/garrison-spawns in XZ to match the village layout (Phase D).
- `SquadCommander`: order point = `Camera.ScreenPointToRay(pointer)` raycast onto a ground plane
  (`Plane(Vector3.up, 0)` or ground collider) → XZ world point (replaces `ScreenToWorldPoint`).
- Keep the code-built HUD, onboarding banner, threshold callouts & screen-pulse from the prior pass.
**Acceptance:** pressing Play shows a tilted 3D board, lit, with tinted primitive units that play exactly
like before; right-click orders land on the correct ground point; HUD/onboarding intact; G1/G4 hold.

### Phase C — Paladin humans + animation — todo `m3d-paladin`
**Assets already in project:** `Assets/Art/Characters/Paladin/Paladin J Nordstrom.fbx` (+ `@Walking`,
`@Running`). Handled by the **mixamo-retrieve** agent for editor-side config.
**Do (asset side):**
- Import base FBX: **Rig = Humanoid**, create avatar; correct scale (Mixamo often needs ~0.01 or
  "Use File Scale" tuned so the Paladin is ~1.8 m tall next to ~1 m goblins). Import materials/textures.
- `@Walking` / `@Running`: **Rig = Humanoid** (copy avatar from base), **Loop Time = on**, root motion
  OFF (movement is driven by physics, animation is cosmetic). Extract clips.
- Build `AnimatorController` (`Assets/Art/Characters/Paladin/PaladinAnimator.controller`): states
  **Idle → Walk → Run**, blended by a float `Speed` param (e.g. Walk > 0.1, Run > moveSpeed*0.7). Idle can
  be a near-static pose (use Walk slowed or a Mixamo idle if added later).
- Create `Assets/Resources/Prefabs/Human.prefab`: the Paladin model + `Animator` (the controller) +
  `Rigidbody` (G2 constraints) + `CapsuleCollider` + **`HumanUnit`**. This satisfies the VisualLibrary
  `Human` key so the spawner uses it automatically.
**Do (code side):**
- Small `Units/UnitAnimatorDriver.cs` (or fold into `Unit`/`HumanUnit`): each frame set
  `animator.SetFloat("Speed", Body.linearVelocity.magnitude)` (G5: derived from real velocity, not fps).
  Null-safe (primitive humans without an Animator still work).
- T2 "state tint": with a textured model, a flat color tint reads poorly — instead surface Guard vs Alert
  via a small **emissive rim/indicator** (e.g., a thin red ground ring or emissive boost on Alert). Keep it
  cheap (G3). Primitive fallback keeps the original color tint.
**Acceptance:** garrison humans are animated Paladins that **walk** when moving slowly and **run** when
charging (Speed-driven), upright on the plane, facing travel direction; concurrent humans respect the G3
cap; Guard vs Alert is visually distinguishable; goblins/warlord remain primitives.

### Phase D — Terrain + village (code-built, art-swappable) — todo `m3d-terrain`
**Files:** `Bootstrap/RaidBootstrap.cs` (+ small helpers), via `VisualLibrary` keys.
**Layout (north = +Z village, south = −Z field; matches existing data):**
- **Ground:** large XZ ground; two visual zones — **field** (south, grass-green `GroundField`) and
  **village** (north, tan/dirt `GroundVillage`), gentle rise toward the village. Big collider for the
  order-raycast (Phase B).
- **Barrier (the user asked):** a **timber palisade `Wall`** spanning the map at z≈4 with the existing
  breachable **`Gate`** centered, flanked by a **`Watchtower`** — the Act 1→2 threshold. Decorative low
  **`Fence`** segments used in the field around pastures only.
- **Village (fun + characterful):** central **`Well`** + square; ~5 **`Cottage`** boxes (timber+thatch
  look via tint/shape); a **`Chapel`** at the far north housing the **Vault** cache; a **`Barn`** =
  Granary cache; a **`Barracks`** at the north = garrison spawn origin; **`Stall`**/**`Haystack`** dressing.
- **Field (nice):** a **dirt road** north→gate guiding the route; a **tree line/forest** at the south =
  the **extraction** marker (blue glow); scattered **`Tree`**/**`Rock`**, pasture **`Fence`**, and a small
  **stream + `Bridge`** midfield landmark.
- Caches placed as physical props at their data positions; extraction zone = the southern tree-line marker.
**v1 vs nice-to-have:** v1 = wall+gate+watchtower, cottages+chapel(Vault)+barn+barracks+well, road, tree
line, rocks, fences, bridge, tilted dusk light. **Nice-to-have (only if perf/time allow):**
alarm-reactive **cottage window lights + square brazier** (subscribe `AlarmSystem.OnThresholdChanged`),
breakable barrels, animals, a skyline windmill.
**Acceptance:** the board reads as a hamlet being raided from the south; props never block unit
silhouettes (G4); everything spawned via `VisualLibrary` keys (art-swappable); perf steady (G3).

### Phase E — Verify against guardrails — todo `m3d-verify`
- Compiles clean (the running editor's `Editor.log` shows zero `error CS`).
- Play through and confirm **G1–G5** explicitly, plus all 4 end states fire with correct result lines.
- Grep changed scripts for frame-dependent motion (G5) and per-frame allocations/Find (G3).

## 4. Mixamo import quick-reference (for the mixamo-retrieve agent)
- Files: `Assets/Art/Characters/Paladin/Paladin J Nordstrom.fbx` (base, has mesh+skeleton),
  `…@Walking.fbx`, `…@Running.fbx` (animation-only).
- Base: Rig→Humanoid (Create From This Model), Materials→Import/Extract, fix scale so ~1.8 m.
- Anim FBXs: Rig→Humanoid, Avatar→**Copy From Other Avatar** (the base's), set **Loop Time**, root motion
  off, extract the clip. Build `PaladinAnimator.controller` (Idle/Walk/Run by `Speed`).
- Output: `Assets/Resources/Prefabs/Human.prefab` wired with Animator + Rigidbody + CapsuleCollider +
  `HumanUnit` so `VisualLibrary` picks it up for key `Human`.

## 5. Out of scope (v1)
Hero abilities, leveling, new unit types, NavMesh pathfinding, Unity Terrain heightmap sculpting, water
sim, foliage wind, networked anything, audio (flag for audio agent: alarm horns, Warhorn, hit/death/loot,
win/lose). Goblins/warlord/props staying primitive is intentional (G3).

## 6. Definition of done
All scripts compile; the bootstrap demo plays start→finish in tilted 3D; humans are animated Paladins;
the hamlet/field reads clearly; **G1–G5 all verified**; visuals are spawned via `VisualLibrary` so the
user can add art prefabs under `Assets/Resources/Prefabs/` with no code changes; all four end states work.
