using System.Collections.Generic;
using UnityEngine;

namespace GoblinSiege.Visual
{
    // ═══════════════════════════════════════════════════════════════════════════
    // VisualLibrary — THE ART-SWAP SEAM (3D_MIGRATION_SPEC §2)
    // ═══════════════════════════════════════════════════════════════════════════
    // WHY THIS EXISTS:
    // The whole game is currently code-built (no hand-authored prefabs). When the
    // user adds real art later, we do NOT want to hunt through gameplay scripts
    // editing `GameObject.CreatePrimitive(...)` calls. Instead, EVERY visual goes
    // through ONE indirection: VisualLibrary.Spawn(key, ...).
    //
    // THE CONTRACT (do not break the key names — they are the swap surface):
    //   1. Spawn first tries Resources.Load<GameObject>("Prefabs/<key>"). If a
    //      prefab with that exact name exists under Assets/Resources/Prefabs/, it
    //      is instantiated AS-IS (its own meshes, materials, rig, etc.).
    //   2. Otherwise Spawn FALLS BACK to a code-built tinted primitive (capsule/
    //      cube/cylinder) so the game is fully playable with zero art.
    //
    // So "dropping in real art" = put `<key>.prefab` in Assets/Resources/Prefabs/.
    // No script changes. That is the entire point of this file.
    //
    // SEPARATION OF CONCERNS:
    //   - VisualLibrary builds the *visual* only (a GameObject you can see).
    //   - The SPAWNER (Squad, GarrisonSpawner, RaidBootstrap, RaidManager) attaches
    //     gameplay components (Unit, Rigidbody, …) onto whatever Spawn returns.
    //   This keeps "how it looks" (data/art) cleanly separate from "how it plays"
    //   (code), exactly as §2 requires.
    //
    // PERFORMANCE (GUARDRAIL G3):
    //   - One SHARED Material per role-color is cached here and reused across all
    //     instances (goblins share one green material, caches share one gold, …).
    //   - Per-instance color variation (hit-flash, Guard/Alert) is done by the Unit
    //     via MaterialPropertyBlock — NOT by instancing materials — so the shared
    //     material stays shared and there are zero per-frame material allocations.
    //
    // WebGL-SAFE: only Resources.Load, CreatePrimitive, Material, Shader.Find and a
    // Dictionary cache. No threads, no reflection, no System.IO.
    // ═══════════════════════════════════════════════════════════════════════════
    public static class VisualLibrary
    {
        // ───────────────────────────────────────────────────────────────────────
        // ROLE COLORS — single source of truth for GUARDRAIL G4 (readability).
        // Goblin green · Warlord cyan · Human red · Loot gold · Gate brown ·
        // Extraction blue. Spawners reference these so the readability language is
        // defined in exactly ONE place.
        // ───────────────────────────────────────────────────────────────────────
        public static readonly Color GoblinGreen    = new(0.35f, 0.80f, 0.30f);
        public static readonly Color HumanRed        = new(0.85f, 0.25f, 0.25f);
        public static readonly Color WarlordCyan     = new(0.20f, 0.95f, 0.95f);
        public static readonly Color LootGold        = new(0.95f, 0.80f, 0.20f);
        public static readonly Color GateBrown       = new(0.50f, 0.35f, 0.20f);
        public static readonly Color ExtractionBlue  = new(0.20f, 0.50f, 0.95f, 0.45f);
        public static readonly Color FieldGreen      = new(0.30f, 0.42f, 0.22f);
        public static readonly Color VillageTan      = new(0.55f, 0.47f, 0.33f);

        // ── Phase D prop palette (3D_MIGRATION_SPEC §3 Phase D) ──────────────────
        // Earthy, low-saturation tints so DECORATION never competes with the
        // high-contrast gameplay role colors above (G4). These are scenery, not
        // signals — they must read as "background hamlet", not "click me".
        public static readonly Color WoodBrown    = new(0.42f, 0.32f, 0.22f); // palisade / fence / bridge / tower
        public static readonly Color CottageTimber = new(0.46f, 0.34f, 0.24f); // cottage walls
        public static readonly Color StoneGrey    = new(0.62f, 0.60f, 0.56f); // chapel / well stone
        public static readonly Color BarnRed      = new(0.50f, 0.27f, 0.20f); // barn / granary timber
        public static readonly Color BarracksWood = new(0.38f, 0.30f, 0.24f); // garrison barracks
        public static readonly Color FoliageGreen = new(0.20f, 0.42f, 0.18f); // tree canopy
        public static readonly Color RockGrey     = new(0.50f, 0.50f, 0.50f); // boulders
        public static readonly Color HayYellow    = new(0.74f, 0.62f, 0.28f); // haystacks
        public static readonly Color StallWood    = new(0.55f, 0.42f, 0.28f); // market stall

        // Stable VisualLibrary keys (the swap contract). Spawners use these
        // constants instead of magic strings so a typo is a compile error.
        public const string KeyHuman         = "Human";
        public const string KeyGoblin        = "Goblin";
        public const string KeyWarlord       = "Warlord";
        public const string KeyCacheCrate    = "Cache_Crate";
        public const string KeyCacheChest    = "Cache_Chest";
        public const string KeyCacheGranary  = "Cache_Granary";
        public const string KeyCacheVault    = "Cache_Vault";
        public const string KeyGate          = "Gate";
        public const string KeyWall          = "Wall";
        public const string KeyExtraction    = "Extraction";
        public const string KeyGroundField   = "GroundField";
        public const string KeyGroundVillage = "GroundVillage";

        // ── Phase D structure/scenery keys (stable swap contract, spec §2) ───────
        public const string KeyWatchtower = "Watchtower";
        public const string KeyCottage    = "Cottage";
        public const string KeyChapel     = "Chapel";
        public const string KeyBarn       = "Barn";
        public const string KeyBarracks   = "Barracks";
        public const string KeyWell       = "Well";
        public const string KeyTree       = "Tree";
        public const string KeyRock       = "Rock";
        public const string KeyFence      = "Fence";
        public const string KeyBridge     = "Bridge";
        public const string KeyStall      = "Stall";
        public const string KeyHaystack   = "Haystack";

        // Shared-material cache (G3). Key = a stable color tag; value = the one
        // Material every instance of that role reuses. Guarded against Unity's
        // "destroyed object becomes null after a domain reload / play-stop".
        private static readonly Dictionary<string, Material> RoleMaterials = new();

        // ───────────────────────────────────────────────────────────────────────
        // THE ONE PUBLIC METHOD (per §2). Everything else is private helper.
        // ───────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Spawns the visual for <paramref name="key"/>. Tries a real prefab under
        /// Resources/Prefabs/&lt;key&gt; first; otherwise builds a tinted primitive
        /// fallback. Returns the root GameObject so the caller can attach gameplay
        /// components (Unit, Rigidbody, …) to it.
        /// </summary>
        public static GameObject Spawn(string key, Vector3 pos, Quaternion rot, Transform parent)
        {
            // 1) ART PATH — a real prefab overrides the primitive with ZERO code edits.
            //    Real models are authored with the pivot at the feet, so we place
            //    them exactly at the requested ground point (no lift).
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/{key}");
            if (prefab != null)
            {
                GameObject art = Object.Instantiate(prefab, pos, rot, parent);
                art.name = key;
                return art;
            }

            // 2) FALLBACK PATH — code-built primitive, tinted by role (G4).
            return BuildPrimitive(key, pos, rot, parent);
        }

        // ───────────────────────────────────────────────────────────────────────
        // Primitive fallback. A small descriptor table maps each key to a shape,
        // a shared role color, a scale, and a couple of flags. New Phase-D keys can
        // be added here without touching any gameplay script.
        // ───────────────────────────────────────────────────────────────────────
        private static GameObject BuildPrimitive(string key, Vector3 pos, Quaternion rot, Transform parent)
        {
            PrimDesc d = Describe(key);

            var go = GameObject.CreatePrimitive(d.Shape);
            go.name = key;
            go.transform.localScale = d.Scale;

            // Strip the auto-added collider. The 2D original had NO unit colliders
            // (combat is range/registry-based, not physics-contact), and the order
            // raycast uses a math Plane — so physics colliders are unnecessary and
            // would let units bump each other / props (changing feel). Removing them
            // keeps the game playing "exactly like before" and is cheaper (G3).
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Lift center-pivoted primitives so their BASE rests on the ground point
            // the caller passed (gameplay positions are the flat XZ ground, y≈0).
            // Real art (feet-pivot) skips this — handled on the prefab path above.
            float y = pos.y;
            if (d.LiftToGround)
                y += d.Scale.y * NativeHalfHeight(d.Shape);
            go.transform.SetPositionAndRotation(new Vector3(pos.x, y, pos.z), rot);

            if (parent != null) go.transform.SetParent(parent, true);

            // Assign the SHARED role material (G3). Per-instance tint (hit-flash,
            // Guard/Alert) is layered on top later via MaterialPropertyBlock.
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = GetRoleMaterial(d.ColorTag, d.Color, d.Transparent);

            return go;
        }

        private struct PrimDesc
        {
            public PrimitiveType Shape;
            public Color Color;
            public string ColorTag;   // cache key for the shared material
            public Vector3 Scale;
            public bool Transparent;
            public bool LiftToGround;
        }

        private static PrimDesc Describe(string key) => key switch
        {
            // --- Units (capsules; goblin ~1.1m, human ~1.8m, warlord bigger) ---
            KeyGoblin  => Prim(PrimitiveType.Capsule, GoblinGreen, "goblin",  new Vector3(0.55f, 0.55f, 0.55f)),
            KeyHuman   => Prim(PrimitiveType.Capsule, HumanRed,    "human",   new Vector3(0.70f, 0.90f, 0.70f)),
            KeyWarlord => Prim(PrimitiveType.Capsule, WarlordCyan, "warlord", new Vector3(0.95f, 1.10f, 0.95f)),

            // --- Loot caches (cubes; bigger = richer = greedier) — all loot gold (G4) ---
            KeyCacheCrate   => Prim(PrimitiveType.Cube, LootGold, "loot", new Vector3(0.80f, 0.80f, 0.80f)),
            KeyCacheChest   => Prim(PrimitiveType.Cube, LootGold, "loot", new Vector3(1.00f, 0.75f, 0.75f)),
            KeyCacheGranary => Prim(PrimitiveType.Cube, LootGold, "loot", new Vector3(1.20f, 1.20f, 1.20f)),
            KeyCacheVault   => Prim(PrimitiveType.Cube, LootGold, "loot", new Vector3(1.45f, 1.45f, 1.45f)),

            // --- Structures ---
            KeyGate => Prim(PrimitiveType.Cube, GateBrown, "gate", new Vector3(3.00f, 1.30f, 0.45f)),
            // Palisade shares the "timber" material with the watchtower/fence/bridge (G3).
            KeyWall => Prim(PrimitiveType.Cube, WoodBrown, "timber", new Vector3(4.00f, 1.30f, 0.45f)),

            // ── Phase D props (3D_MIGRATION_SPEC §3) ─────────────────────────────
            // GUARDRAIL G4: scenery is LOW and SHORT so it never hides a unit
            // silhouette from the tilted RTS camera. Buildings are footprint-first
            // (wide + low), towers are thin, ground clutter is knee-high. Each prop
            // shares ONE material per "ColorTag" (G3): no per-instance allocations.

            // Timber threshold tower — thin so it reads as a landmark, not a wall.
            KeyWatchtower => Prim(PrimitiveType.Cylinder, WoodBrown, "timber", new Vector3(0.70f, 1.30f, 0.70f)),

            // Village buildings — wide, LOW boxes (timber+thatch suggested by tint).
            KeyCottage  => Prim(PrimitiveType.Cube, CottageTimber, "cottage",  new Vector3(3.20f, 1.00f, 3.00f)),
            KeyChapel   => Prim(PrimitiveType.Cube, StoneGrey,     "chapel",   new Vector3(3.60f, 1.60f, 4.20f)),
            KeyBarn     => Prim(PrimitiveType.Cube, BarnRed,       "barn",     new Vector3(4.20f, 1.20f, 3.20f)),
            KeyBarracks => Prim(PrimitiveType.Cube, BarracksWood,  "barracks", new Vector3(5.00f, 1.20f, 3.20f)),
            KeyStall    => Prim(PrimitiveType.Cube, StallWood,     "stall",    new Vector3(1.80f, 0.85f, 1.60f)),

            // Round props (cylinders/sphere). Half-height handling is fixed in
            // NativeHalfHeight so cylinders rest ON the ground, not half-sunk.
            KeyWell     => Prim(PrimitiveType.Cylinder, StoneGrey,    "well",     new Vector3(1.00f, 0.28f, 1.00f)),
            KeyHaystack => Prim(PrimitiveType.Cylinder, HayYellow,    "hay",      new Vector3(1.10f, 0.55f, 1.10f)),
            KeyTree     => Prim(PrimitiveType.Cylinder, FoliageGreen, "tree",     new Vector3(1.10f, 0.95f, 1.10f)),
            KeyRock     => Prim(PrimitiveType.Sphere,   RockGrey,     "rock",     new Vector3(1.10f, 0.70f, 1.30f)),

            // Knee-high field clutter — must not occlude (G4).
            KeyFence    => Prim(PrimitiveType.Cube, WoodBrown, "timber", new Vector3(2.20f, 0.50f, 0.18f)),
            KeyBridge   => Prim(PrimitiveType.Cube, WoodBrown, "timber", new Vector3(3.20f, 0.28f, 2.40f)),

            // --- Markers / ground (flat; not lifted — positioned explicitly) ---
            KeyExtraction => PrimFlat(PrimitiveType.Cube, ExtractionBlue, "extraction",
                                      new Vector3(5.0f, 0.08f, 2.5f), transparent: true),
            KeyGroundField => PrimFlat(PrimitiveType.Cube, FieldGreen, "groundfield",
                                      new Vector3(44f, 0.20f, 44f)),
            KeyGroundVillage => PrimFlat(PrimitiveType.Cube, VillageTan, "groundvillage",
                                      new Vector3(22f, 0.20f, 16f)),

            // --- Unknown key: a neutral grey cube so Spawn NEVER returns null.
            //     (Phase D adds proper fallbacks for Cottage/Tree/Rock/etc.) ---
            _ => Prim(PrimitiveType.Cube, new Color(0.6f, 0.6f, 0.6f), "unknown", Vector3.one),
        };

        private static PrimDesc Prim(PrimitiveType shape, Color color, string tag, Vector3 scale) => new()
        {
            Shape = shape, Color = color, ColorTag = tag, Scale = scale,
            Transparent = false, LiftToGround = true
        };

        private static PrimDesc PrimFlat(PrimitiveType shape, Color color, string tag, Vector3 scale,
            bool transparent = false) => new()
        {
            Shape = shape, Color = color, ColorTag = tag, Scale = scale,
            Transparent = transparent, LiftToGround = false
        };

        // Native half-height of a unit-scale primitive, used for ground-resting.
        // Unity's Capsule AND Cylinder are 2 units tall (half = 1.0); Cube/Sphere/
        // Quad/Plane are 1 unit (half = 0.5). Phase D added cylinders (well, tower,
        // tree, haystack) so Cylinder MUST be 1.0 here or they sink half underground.
        private static float NativeHalfHeight(PrimitiveType shape) =>
            (shape == PrimitiveType.Capsule || shape == PrimitiveType.Cylinder) ? 1.0f : 0.5f;

        // ───────────────────────────────────────────────────────────────────────
        // Shared material per role (G3). Built once, reused forever. Null-guarded
        // so it survives editor domain reloads (a destroyed Material reads as null).
        // Built-in Render Pipeline → "Standard" shader (this project has no URP).
        // ───────────────────────────────────────────────────────────────────────
        private static Material GetRoleMaterial(string tag, Color color, bool transparent)
        {
            if (RoleMaterials.TryGetValue(tag, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // ultra-safe fallback
            var mat = new Material(shader) { name = $"Role_{tag}" };
            ApplyColor(mat, color);
            if (transparent) MakeFade(mat);

            RoleMaterials[tag] = mat;
            return mat;
        }

        // Set color on whichever property the active shader exposes (Standard uses
        // _Color; URP/Lit uses _BaseColor — we set both so this is pipeline-agnostic).
        private static void ApplyColor(Material mat, Color color)
        {
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            // A little emission keeps tints readable under the dusk light (G4).
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.18f);
            }
        }

        // Standard-shader transparency recipe (for the translucent extraction marker).
        private static void MakeFade(Material mat)
        {
            mat.SetFloat("_Mode", 2f); // Fade
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
