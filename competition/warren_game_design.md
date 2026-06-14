# Goblin Siege - Third-Person RTS (Spec Draft)

One-liner: Command goblin war-bands from a hybrid third-person / RTS view to raid and siege human strongholds in a medieval, Middle-earth-inspired campaign.

## High-level goals (for approval)

- Provide an immersive hybrid camera so players feel like a battlefield commander and a goblin war-leader at once.
- Make sieges meaningful with simple, testable siege mechanics (sappers, gates, towers) and human garrison AI.
- Prioritize quick, repeatable missions with emergent tactical decisions rather than deep base building.

## Scope (vertical slice)

- One mission type: small keep siege (approach -> breach -> garrison fight -> vault loot).
- Units: 4 goblin types + hero; 4 human unit types + garrison elite.
- Mechanics: hybrid RTS 3rd-person camera, squad/group selection, formations, sappers (gate-breach), destructible gate/wall segment, basic resource (loot) and recruit/upgrade menu.
- AI: patrols, garrison behavior, tower targeting, simple reinforcement timers.

## Core Systems (concise tasks)

- Camera and controls: implement camera that follows hero but allows zoom-out to tactical view; keyboard/mouse controls for selection and issuing orders; toggle mode.
- Unit framework: generic Unit component (HP, armor, damage, range, morale), squad component, formation system.
- Siege system: walls/gates with HP, simple ram/sapper interactions, tower firing behavior, destructible gate prefab.
- AI: state machine for human garrison (idle/patrol/alert/garrison), spawn points for reinforcements, tower targeting priorities.
- UI: squad HUD, minimap, resource counters, hotkey group assignment.

## Design Notes - Camera and Player Experience

- Camera defaults to a third-person chase of the Chieftain when a single hero is selected.
- When multiple squads are selected or player toggles Tactical Mode, camera pulls up and back to a strategic angle and enables click-drag selection.
- Zoom and rotation must be smooth; provide an overview hotkey to snap to minimap center.
- When controlling a hero in third-person, issuing an order to a distant squad should optionally create a cinematic command cue (arrow + unit move). Provide setting to disable cinematic cues.

## Design Notes - Combat and Siege

- Fortifications are layered: Outer wall -> Gate -> Courtyard -> Keep.
- Vertical slice includes only Outer wall and Gate plus Tower.
- Sapper mechanic: Sapper unit can be ordered to plant a timed charge at gates; charge progress is interruptible by enemy action (guarding counterplay).
- Towers and walls provide firing advantage; units on wall get range and damage bonuses.

## Design Notes - Factions and Units

- Goblin archetypes emphasize numbers and unconventional tactics; humans emphasize discipline and positional defense.
- Initial stat table (to be tuned in playtest):
- Goblin Grunt: HP 30, DMG 6, Speed 3, Cost 1.
- Spearthrower: HP 20, DMG 8 (ranged), Range 5, Speed 2.5, Cost 2.
- Sapper: HP 18, DMG 4, Speed 2, BreachTime 10s, Cost 3.
- Chieftain: HP 120, DMG 20, Aura +10% morale, Abilities: Rally (cooldown), Berserk.
- Human Militia: HP 40, DMG 8, Shield (reduces frontal damage), Cost 1.5.
- Human Crossbow: HP 25, DMG 12 (ranged), Range 7, Cost 2.
- Pikeman/Guard: HP 45, DMG 10, Anti-charge bonus, Cost 2.5.
- Garrison Elite: HP 100, DMG 18, Armor 10, Cost 5.

## AI Priorities and Behavior

- Towers prioritize high-threat targets (siege tools, chieftain, grouped units).
- Crossbowmen prioritize visible clusters.
- Garrison toggles to Alert state when gate HP drops below 70% or when sapper progress starts; in Alert, spawn reinforcement timer shortens.

## Missions and Objectives (examples)

- Primary: Destroy Gate -> Eliminate Garrison Captain -> Secure Vault (loot).
- Secondary: Sabotage Beacon (reduces reinforcement calls), Capture Banner (morale boost).

## Progression and Meta

- Loot grants recruit points and materials.
- Recruit points are used immediately between missions to hire more goblins or buy single-use siege items (explosive barrel, extra sapper charge).

## Art and Audio (brief)

- Art: low-to-mid fidelity stylized realism; keep shapes readable from camera distances; emphasize silhouettes for goblins and humans.
- Audio: short cues for alarm, breach, wall fall, vault open, recruitment, and action confirms.

## Acceptance Criteria (vertical slice)

- Player can select squads and issue orders in Tactical Mode.
- Camera supports third-person follow and tactical zoom with smooth transitions.
- Gate can be breached via sapper or ram; tower deals damage to units on approach.
- Human garrison behaves defensively and spawns a small reinforcement wave.
- HUD shows squads, health, and a minimap; player can recruit units with loot.

## Open Questions (approve/adjust)

- Do you want persistent metagame progression between missions (unlock tribes/units), or per-run only? Recommended: persistent for campaign.
- Tone: How faithful to Tolkien-like Middle-earth aesthetics? Strict homage might raise IP concerns; recommended: inspired-by medieval fantasy.
- Multiplayer: Do you want co-op raids later? Recommended: single-player for vertical slice.

## Next steps after approval

1. Sign off spec; convert major systems into implementation tasks for the build agent.
2. Priority dev order: Camera and controls -> Unit framework -> Siege system (gate + sapper) -> Human AI -> UI -> Art integration.
3. Create a short audio brief and unit stat CSV for tuning.
