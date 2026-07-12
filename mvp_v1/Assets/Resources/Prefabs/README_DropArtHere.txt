Drop <VisualLibraryKey>.prefab files here (e.g. Human.prefab, Goblin.prefab, Gate.prefab). VisualLibrary.Spawn loads Resources/Prefabs/<key> automatically; otherwise it falls back to a tinted primitive. No code changes needed.

────────────────────────────────────────────────────────────────────────────
PARTICLE EFFECTS (VfxLibrary): create a folder Assets/Resources/VFX and drop
effect prefabs there. VfxLibrary.Play("<key>", ...) loads Resources/VFX/<key>
if present, otherwise it plays a built-in code effect, so the game always works.

To use the "Magic Effects FREE" asset pack, import it, then copy the effect you
want into Assets/Resources/VFX with these exact names:
  Slash_Red.prefab  -> RED slash; plays on the Warlord's melee slash (Spacebar).
  Explosion.prefab  -> explosion; plays when a Sapper blows the wall.
  Smoke.prefab      -> smoke; plays with the explosion on a wall breach.
No code changes needed.