using System.Collections;
using GoblinSiege.Core;
using GoblinSiege.Player;
using GoblinSiege.Systems;
using GoblinSiege.Units;
using GoblinSiege.Visual;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GoblinSiege.Bootstrap
{
    /// <summary>
    /// ONE-CLICK PLAYABLE DEMO. Drop this on an empty GameObject in an empty scene and press Play.
    /// It builds the entire raid in code — a tilted 3D RTS camera, dusk lighting, ground, the
    /// systems, the Warlord + squads, loot caches, gates, defenders, the new Input System
    /// bindings, and a minimal HUD — with no manual scene/prefab authoring.
    ///
    /// 3D MIGRATION (3D_MIGRATION_SPEC Phase B): this used to set up an ORTHOGRAPHIC top-down
    /// camera and build flat SpriteRenderer squares. It now builds:
    ///   • a TILTED PERSPECTIVE RTS camera (~58° pitch) looking down the XZ board (G1),
    ///   • a warm DIRECTIONAL "dusk" light + ambient so the 3D primitives read clearly,
    ///   • every visual through <see cref="VisualLibrary"/> keys (primitive fallback now,
    ///     real art prefabs later with ZERO code changes — the art seam, §2),
    ///   • all positions mapped 2D (x,y) → 3D (x,0,y) so north = +Z, south = −Z.
    /// The HUD, onboarding banner, threshold callouts and screen-pulse are unchanged.
    ///
    /// Controls: WASD/Arrows move the Warlord. 1/2/3 select a squad, ` selects all,
    /// right-click orders them (raycast onto the ground), H sounds the Warhorn.
    /// </summary>
    public class RaidBootstrap : MonoBehaviour
    {
        [Header("Camera (tilted RTS — GUARDRAIL G1)")]
        [Tooltip("World position of the perspective camera (raised + pulled south).")]
        // Phase D widened/raised the framing: the hamlet now extends further north
        // (barracks z≈16, chapel z≈19) and the field/tree-line further south. We
        // pulled the camera south (−Z) and up (+Y) and opened the FOV a touch so the
        // WHOLE board still reads at a glance — WITHOUT lowering the tilt (still a
        // top-down RTS cam, ~58°, never behind-the-shoulder). G1 preserved.
        [SerializeField] private Vector3 cameraPosition = new(0f, 26f, -13f);
        [Tooltip("Downward pitch in degrees (~50–60 keeps the whole board readable).")]
        [SerializeField] private float cameraPitch = 58f;
        [SerializeField] private float cameraFov = 56f;

        /// <summary>
        /// AUTO-START: with no setup at all, just press Play. Unity calls this after the
        /// first scene loads and spawns the bootstrap for you. If you've already placed a
        /// RaidBootstrap in the scene manually, this does nothing (avoids a duplicate).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindFirstObjectByType<RaidBootstrap>() != null) return;
            var go = new GameObject("RaidBootstrap (auto)");
            go.AddComponent<RaidBootstrap>();
        }

        private InputActionAsset _inputAsset;
        private RaidManager _raid;
        private AlarmSystem _alarm;
        private QuotaSystem _quota;

        // HUD references updated each event.
        private RectTransform _goldFill;
        private RectTransform _alarmFill;
        private Image _alarmFillImg;
        private Text _goldText;
        private Text _alarmText;
        private Text _resultText;
        private float _barWidth = 320f;

        // T6: Onboarding UI — goal banner fades out, controls card stays.
        private Text _goalBannerText;

        // T7: Threshold callout + screen-pulse overlay for escalation feedback.
        private Text _thresholdCalloutText;
        private Image _screenPulseOverlay;

        // Selection HUD — bottom-of-screen unit panel.
        private GoblinSiege.UI.SelectionHUD _selectionHud;

        // Embedded starter raid (mirrors Data/Raids/raid-01.json) so no Resources load is needed.
        // Positions are JSON [x, y]; RaidManager maps them to world (x, 0, y).
        private const string RaidJson = @"{
            ""id"": 1, ""name"": ""Thornbrook Hamlet"", ""act"": 1,
            ""quota"": 100, ""alarmFillPerSecond"": 0.8,
            ""garrisonRoster"": [""Militia"", ""Crossbow"", ""Pikeman""],
            ""reinforceIntervalByThreshold"": { ""alerted"": 16, ""mobilized"": 9 },
            ""gates"": [ { ""pos"": [0, 4], ""hp"": 70 } ],
            ""caches"": [
                { ""type"": ""Crate"", ""pos"": [-4, 6] },
                { ""type"": ""Crate"", ""pos"": [4, 6] },
                { ""type"": ""Chest"", ""pos"": [-3, 9] },
                { ""type"": ""Chest"", ""pos"": [3, 9] },
                { ""type"": ""Granary"", ""pos"": [0, 12] }
            ],
            ""extractionEdge"": ""south""
        }";

        private void Start()
        {
            _inputAsset = BuildInputAsset();

            SetupCamera();
            SetupLighting();
            SetupGround();

            // Phase D: code-built terrain dressing (village + field) via the art seam.
            // Pure scenery — no gameplay components — so it can run before the systems.
            BuildHamlet();

            // --- Systems live on one child GameObject ---
            var sysGo = new GameObject("Systems");
            sysGo.transform.SetParent(transform);
            _alarm = sysGo.AddComponent<AlarmSystem>();
            _quota = sysGo.AddComponent<QuotaSystem>();
            var garrison = sysGo.AddComponent<GarrisonSpawner>();

            // --- Background music: speeds up when the alarm rises (enemies appear) ---
            var musicGo = new GameObject("MusicManager");
            musicGo.transform.SetParent(sysGo.transform);
            var music = musicGo.AddComponent<MusicManager>();
            // Configure AFTER the alarm is set up so it can subscribe to threshold events.
            // Drop a .wav/.mp3/.ogg AudioClip onto the MusicManager's "Music Clip" field
            // in the Inspector to hear music; it will still wire up and speed up without one.

            // --- Extraction zone (south edge, XZ origin) ---
            // G4: a BLUE translucent "goal" marker, distinct from green goblins, gold
            // caches, brown gates and red humans. Spawned through the art seam so a real
            // extraction prop (Phase D tree-line) can replace it with no code change.
            GameObject extractionGo = VisualLibrary.Spawn(VisualLibrary.KeyExtraction,
                new Vector3(0f, 0.02f, 0f), Quaternion.identity, transform);
            var extraction = extractionGo.AddComponent<ExtractionZone>();
            // T1 polish: gentle pulse draws the eye to the goal without distracting.
            extractionGo.AddComponent<ZonePulse>();

            // --- Garrison spawn points (north / +Z) ---
            Transform[] spawns =
            {
                MakePoint("Spawn0", new Vector3(0f,   0f, 16f)),
                MakePoint("Spawn1", new Vector3(-5f,  0f, 14f)),
                MakePoint("Spawn2", new Vector3(5f,   0f, 14f)),
                MakePoint("Spawn3", new Vector3(-9f,  0f, 16f)),
                MakePoint("Spawn4", new Vector3(9f,   0f, 16f)),
            };
            garrison.SetSpawnAssets(spawns);

            Transform deploy = MakePoint("Deploy", new Vector3(0f, 0f, 1f));

            // --- RaidManager: inject everything (no prefabs — visuals via VisualLibrary) ---
            var raidGo = new GameObject("RaidManager");
            raidGo.transform.SetParent(transform);
            _raid = raidGo.AddComponent<RaidManager>();
            _raid.Inject(RaidJson, _alarm, _quota, garrison, extraction, deploy);

            // Configure music NOW — after _alarm.Configure() is called inside Inject.
            music.Configure(_alarm);

            // --- Door at the barracks entrance (Warlord walks up and it swings open) ---
            // Position it at the barracks threshold; the warlord approaches from the south.
            BuildDoor(new Vector3(0f, 0f, 15.0f), parent: transform);

            // --- Static defenders: more variety and spread across the village ---
            SpawnDefender(HumanType.Militia,  new Vector3(-3f,  0f,  9.5f));
            SpawnDefender(HumanType.Militia,  new Vector3(3f,   0f,  9.5f));
            SpawnDefender(HumanType.Militia,  new Vector3(0f,   0f, 12.5f));
            SpawnDefender(HumanType.Crossbow, new Vector3(-7f,  0f, 10.5f));
            SpawnDefender(HumanType.Crossbow, new Vector3(7f,   0f, 10.5f));
            SpawnDefender(HumanType.Pikeman,  new Vector3(-2f,  0f, 14.5f));
            SpawnDefender(HumanType.Pikeman,  new Vector3(2f,   0f, 14.5f));
            SpawnDefender(HumanType.Militia,  new Vector3(0f,   0f, 17.0f));

            // --- Player (Warlord hero + squad commander share one PlayerInput) ---
            BuildPlayer();

            // --- HUD + wiring ---
            BuildHud();
            HookHud();
        }

        // ---------------------------------------------------------------- scene/camera/light

        private void SetupCamera()
        {
            // GUARDRAIL G1: a TILTED TOP-DOWN PERSPECTIVE camera — NOT behind-the-shoulder
            // or first-person. Raised high and pulled south (−Z) so it looks down the board
            // toward the village (+Z); the whole playfield stays readable at a glance.
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = false;                 // perspective (was orthographic in 2D)
            cam.fieldOfView = cameraFov;
            cam.farClipPlane = 250f;
            cam.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(cameraPitch, 0f, 0f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.17f, 0.24f); // deep dusk sky

            // Attach the scroll-wheel zoom controller. Starts at the configured FOV.
            var zoom = cam.gameObject.GetComponent<CameraZoom>()
                    ?? cam.gameObject.AddComponent<CameraZoom>();
            zoom.SetInitialFov(cameraFov);
        }

        private void SetupLighting()
        {
            // A single warm DIRECTIONAL "dusk" sun so the 3D primitives have shape and the
            // role tints stay readable (G4). Plus a flat ambient fill so shadowed sides of
            // units aren't pure black.
            var lightGo = new GameObject("Sun (Directional)");
            lightGo.transform.SetParent(transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.82f, 0.62f); // warm dusk
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.32f, 0.40f);
        }

        private void SetupGround()
        {
            // ═══════════════════════════════════════════════════════════════════
            // GROUND — two visual zones on one flat XZ plane (3D_MIGRATION §3 Phase D)
            // ═══════════════════════════════════════════════════════════════════
            // SOUTH (−Z) = FIELD (grass green) where the warband deploys/extracts.
            // NORTH (+Z) = VILLAGE (tan/dirt) past the gate. Both are flat (G2): the
            // "gentle rise toward the village" is a SUBTLE cosmetic step (village top
            // sits a hair above the field top) — never enough to clip the units, whose
            // Y is frozen at the play plane.
            //
            // Both come through the art seam (GroundField / GroundVillage keys). We
            // override the returned transform's localScale to size each zone (Spawn
            // intentionally has no scale arg — sizing scenery is the caller's job).

            // Field base — covers the WHOLE board; the village tan overlays its north.
            GameObject field = VisualLibrary.Spawn(VisualLibrary.KeyGroundField,
                new Vector3(0f, -0.12f, -2f), Quaternion.identity, transform);
            field.transform.localScale = new Vector3(56f, 0.20f, 60f); // x ±28, z −32..28

            // Village dirt overlay — north of the gate threshold (z≈4) up to the chapel.
            GameObject village = VisualLibrary.Spawn(VisualLibrary.KeyGroundVillage,
                new Vector3(0f, -0.06f, 14f), Quaternion.identity, transform);
            village.transform.localScale = new Vector3(34f, 0.20f, 22f); // x ±17, z 3..25

            // ── Order-raycast safety collider (the user asked for one) ──────────
            // SquadCommander casts against a MATH Plane(y=0), so a physics collider
            // isn't strictly required — but a big flat ground collider is belt-and-
            // suspenders for any future collider-based raycast and costs nothing (one
            // static box, G3). Its TOP sits just BELOW the play plane (y≈−0.06) and
            // unit Y is frozen, so it can never push, jitter, or trap a unit (G2).
            var colliderGo = new GameObject("GroundCollider");
            colliderGo.transform.SetParent(transform);
            colliderGo.transform.position = new Vector3(0f, -5.06f, -2f);
            var box = colliderGo.AddComponent<BoxCollider>();
            box.size = new Vector3(56f, 10f, 60f); // top at y = -5.06 + 5 = -0.06
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PHASE D — TERRAIN + VILLAGE (3D_MIGRATION_SPEC §3)
        // ═══════════════════════════════════════════════════════════════════════════
        // Builds the hamlet-being-raided-from-the-south as code, entirely through
        // VisualLibrary keys so the user can later drop real art prefabs under
        // Assets/Resources/Prefabs/<key>.prefab with ZERO code changes (the §2 seam).
        //
        // GUARDRAIL G4 (readability is paramount) governs EVERY placement here:
        //   • Buildings are wide + LOW (footprint-first) and pushed to the flanks so
        //     they never sit between the camera and a unit / cache / the gate.
        //   • The central lane (|x| ≲ 5) and the cache cluster stay UNOBSTRUCTED.
        //   • The tree line frames the extraction from the FLANKS, leaving the central
        //     extraction lane open so escaping goblins are never hidden.
        // GUARDRAIL G3: ~45 cheap primitives, one shared material per prop type, no
        // per-frame work — this is all spawned once at Start and never touched again.
        // GUARDRAIL G5: nothing here animates; if a brazier flicker is added later it
        // MUST use Time.time/Time.deltaTime (see the nice-to-have TODO at the bottom).
        // ═══════════════════════════════════════════════════════════════════════════
        private void BuildHamlet()
        {
            var village = new GameObject("Village (props)").transform;
            village.SetParent(transform);
            var fieldRoot = new GameObject("Field (props)").transform;
            fieldRoot.SetParent(transform);

            BuildThreshold(village); // wall + gate-flanking watchtowers at z≈4
            BuildVillage(village);   // well, cottages, chapel, barn, barracks, dressing
            BuildField(fieldRoot);   // road, stream+bridge, tree line, rocks, fences
        }

        /// <summary>
        /// THE ACT 1→2 THRESHOLD: a timber palisade spanning the map at z≈4 with the
        /// existing breachable Gate centered (spawned by RaidManager from the raid
        /// data) and watchtowers flanking it. This is the "barrier the user asked for".
        /// </summary>
        private void BuildThreshold(Transform parent)
        {
            const float z = 4f; // matches the gate's data position (0,4)

            // Palisade segments left & right of the 3-wide central gate (x −1.5..1.5).
            // Each Wall fallback is 4 wide; placed so the line reads continuous.
            float[] wallX = { -3f, -7f, -11f, 3f, 7f, 11f };
            foreach (float x in wallX)
                SpawnProp(VisualLibrary.KeyWall, x, z, 0f, parent);

            // Two thin watchtowers hugging the gate — vertical landmarks marking the
            // breach point. Thin (G4) so they punctuate without walling off the view.
            SpawnProp(VisualLibrary.KeyWatchtower, -2.0f, z, 0f, parent);
            SpawnProp(VisualLibrary.KeyWatchtower,  2.0f, z, 0f, parent);
        }

        /// <summary>
        /// The village proper (north / +Z): a central well + square, a ring of low
        /// cottages on the flanks, the chapel landmark at the far north, the barn by
        /// the granary cache, the barracks at the garrison spawn origin, plus stall +
        /// haystack dressing. Everything hugs the sides so the cache cluster
        /// (crates ±4·z6, chests ±3·z9, granary 0·z12) and the central combat lane
        /// stay clear from the camera (G4).
        /// </summary>
        private void BuildVillage(Transform parent)
        {
            // Heart of the hamlet: the well in the central square (low — never occludes).
            SpawnProp(VisualLibrary.KeyWell, 0f, 7f, 0f, parent);

            // Five cottages, all on the flanks (|x| ≥ 7), yawed for variety.
            SpawnProp(VisualLibrary.KeyCottage, -8.0f,  7.0f,  25f, parent);
            SpawnProp(VisualLibrary.KeyCottage,  8.0f,  7.0f, -25f, parent);
            SpawnProp(VisualLibrary.KeyCottage, -9.0f, 11.0f,  10f, parent);
            SpawnProp(VisualLibrary.KeyCottage,  9.0f, 11.0f, -15f, parent);
            SpawnProp(VisualLibrary.KeyCottage,  7.5f, 15.0f, -30f, parent);

            // Barn standing in for the GRANARY cache (data granary is at 0·z12). Placed
            // to the NW of it so — with the camera looking north — the barn never sits
            // in front of (occluding) the gold cache the player must reach (G4).
            SpawnProp(VisualLibrary.KeyBarn, -6.0f, 14.0f, 15f, parent);

            // Barracks at the north = the garrison spawn origin (spawn points are at
            // z14..16). Humans now visibly muster FROM the barracks. Wide + low.
            SpawnProp(VisualLibrary.KeyBarracks, 0f, 16.5f, 0f, parent);

            // Chapel: the far-north landmark closing the skyline. The spec frames it as
            // "housing the Vault cache" — the CURRENT raid data (raid-01) has no Vault
            // (its richest cache is the Granary), so the chapel is a pure landmark here
            // and gameplay is unchanged. If a Vault cache is later added to the data,
            // this is exactly where it belongs. (No balance change — see CONSTRAINTS.)
            SpawnProp(VisualLibrary.KeyChapel, 0f, 19.0f, 0f, parent);

            // Dressing — kept off the flanks' building footprints and the center lane.
            SpawnProp(VisualLibrary.KeyStall,    -6.5f,  6.0f, 20f, parent);
            SpawnProp(VisualLibrary.KeyHaystack,  5.0f, 13.0f,  0f, parent);
            SpawnProp(VisualLibrary.KeyHaystack, -10.0f, 8.0f,  0f, parent);
        }

        /// <summary>
        /// The field (south / −Z): a dirt road guiding the route north to the gate, a
        /// muddy stream/ford with a Bridge as a midfield landmark, the southern tree
        /// line framing the extraction (blue) marker, scattered rocks, and a small
        /// pasture fence. All clutter is knee-high and kept to the flanks (G4).
        /// </summary>
        private void BuildField(Transform parent)
        {
            // Dirt road: a thin tan (dirt) strip from the deep south up to the gate,
            // guiding the eye along the assault route. Reuses the GroundVillage (dirt)
            // material/key — a dirt road IS dirt — and is sized by the caller.
            SpawnGroundStrip(VisualLibrary.KeyGroundVillage,
                new Vector3(0f, -0.02f, -2f), new Vector3(3.0f, 0.16f, 14f), parent);

            // Midfield landmark: a muddy creek/ford crossing east–west, with a timber
            // Bridge where the road meets it. NOTE: this is a *dirt-toned* ford, not
            // open water — true animated blue water is intentionally NOT used because
            // (a) §2 has no Water key and (b) blue is RESERVED for the extraction goal
            // (G4). A proper animated stream is a clearly-marked nice-to-have (below).
            SpawnGroundStrip(VisualLibrary.KeyGroundVillage,
                new Vector3(0f, -0.03f, -3.5f), new Vector3(26f, 0.14f, 1.6f), parent);
            SpawnProp(VisualLibrary.KeyBridge, 0f, -3.5f, 0f, parent);

            // Southern tree line = the extraction marker's frame. Trees sit on the
            // FLANKS (|x| ≥ 8) only, leaving the central extraction lane (x ±4, z≈0)
            // wide open so escaping goblins/warlord are NEVER hidden behind a trunk (G4).
            (float x, float z)[] trees =
            {
                (-9f, -1f), (-11f, -3f), (-8f, -6f), (-12f, -7f),
                ( 9f, -1f), ( 11f, -3f), ( 8f, -6f), ( 12f, -7f),
                (-13f,  3f), (13f,  5f),
            };
            foreach (var (tx, tz) in trees)
                SpawnProp(VisualLibrary.KeyTree, tx, tz, 0f, parent);

            // Scattered boulders at the field edges (low, never in a play lane).
            (float x, float z)[] rocks = { (-14f, 1f), (14f, -2f), (-10f, -9f), (10f, -8f), (13f, 9f) };
            foreach (var (rx, rz) in rocks)
                SpawnProp(VisualLibrary.KeyRock, rx, rz, 0f, parent);

            // A small pasture fence in the east field (decorative Fence is field-only,
            // per the spec — never used as the village wall). Knee-high (G4).
            SpawnProp(VisualLibrary.KeyFence,  9.0f, 1.0f,  0f, parent);
            SpawnProp(VisualLibrary.KeyFence, 11.2f, 1.0f,  0f, parent);
            SpawnProp(VisualLibrary.KeyFence, 12.3f, 2.6f, 90f, parent);
            SpawnProp(VisualLibrary.KeyFence,  8.0f, 2.6f, 90f, parent);

            // ── NICE-TO-HAVE TODOs (left out to protect G3/G4 + the key contract) ──
            // • Alarm-reactive cottage window lights + a square brazier: subscribe to
            //   AlarmSystem.OnThresholdChanged and emissive-boost a few props. Any
            //   flicker MUST be driven by Time.time/Time.deltaTime (G5), never a frame
            //   counter — see ZonePulse for the correct pattern.
            // • Breakable barrels, animals, a skyline windmill, and a true animated
            //   blue stream (needs its OWN VisualLibrary key so it doesn't clash with
            //   the reserved extraction-blue, G4).
        }

        // Spawn a scenery prop on the ground plane via the art seam.
        private GameObject SpawnProp(string key, float x, float z, float yawDegrees, Transform parent)
        {
            return VisualLibrary.Spawn(key,
                new Vector3(x, 0f, z), Quaternion.Euler(0f, yawDegrees, 0f), parent);
        }

        /// <summary>
        /// Builds a door the Warlord can open. The door is a thin brown cube with a Door
        /// component — walk up and it swings open with a smooth animation. Placed on
        /// layer 0 (default) so the warlord's Rigidbody is physically blocked until opened.
        /// A pulsing golden disc on the floor marks it as interactive (G4).
        /// </summary>
        private GameObject BuildDoor(Vector3 pos, Transform parent)
        {
            var doorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorGo.name = "Door";
            doorGo.transform.SetParent(parent);
            doorGo.transform.position = pos;
            doorGo.transform.localScale = new Vector3(2.0f, 2.2f, 0.2f);

            // Bold, glowing door tint so it POPS out from the plain-brown wall and the
            // watchtower pillars around it — Warren should instantly spot THE door (G4).
            var doorColor = new Color(0.75f, 0.30f, 0.18f); // warm reddish "big door" wood
            var rend = doorGo.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(rend.sharedMaterial);
                mat.color = doorColor;
                // A soft glow so the door reads as special/interactive, not scenery.
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", doorColor * 0.45f);
                rend.material = mat;
            }

            // Attach the gameplay Door component (adds its own trigger child).
            doorGo.AddComponent<Door>();

            // A glowing golden handle so it unmistakably reads as a DOOR, not a wall.
            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "DoorHandle";
            handle.transform.SetParent(doorGo.transform, worldPositionStays: false);
            // Offset to one side of the panel, at grab height (local space of the door).
            handle.transform.localPosition = new Vector3(0.30f, 0.10f, -0.6f);
            handle.transform.localScale = new Vector3(0.12f, 0.11f, 1.1f);
            var handleRend = handle.GetComponent<Renderer>();
            if (handleRend != null)
            {
                var handleMat = new Material(handleRend.sharedMaterial);
                handleMat.color = VisualLibrary.LootGold;
                handleMat.EnableKeyword("_EMISSION");
                if (handleMat.HasProperty("_EmissionColor"))
                    handleMat.SetColor("_EmissionColor", VisualLibrary.LootGold * 0.6f);
                handleRend.material = handleMat;
            }
            Object.Destroy(handle.GetComponent<Collider>()); // decoration only

            // G4 interactive hint: a pulsing gold disc on the floor in front of the door
            // so Warren can see "something important is here — walk up to it!".
            var hintDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hintDisc.name = "DoorHint";
            hintDisc.transform.SetParent(parent);
            hintDisc.transform.position = new Vector3(pos.x, 0.02f, pos.z - 1.8f);
            hintDisc.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            var discRend = hintDisc.GetComponent<Renderer>();
            if (discRend != null)
            {
                var discMat = new Material(discRend.sharedMaterial);
                discMat.color = VisualLibrary.LootGold;
                discRend.material = discMat;
            }
            Object.Destroy(hintDisc.GetComponent<Collider>()); // decoration only
            hintDisc.AddComponent<ZonePulse>();

            return doorGo;
        }

        // Spawn a flat ground strip (road / ford) and size it. The GroundField/Village
        // fallbacks are flat (not lifted), so the passed center IS the strip's center.
        private GameObject SpawnGroundStrip(string key, Vector3 center, Vector3 scale, Transform parent)
        {
            GameObject go = VisualLibrary.Spawn(key, center, Quaternion.identity, parent);
            go.transform.localScale = scale;
            return go;
        }

        private Transform MakePoint(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = pos;
            return go.transform;
        }

        private void SpawnDefender(HumanType type, Vector3 post)
        {
            // ART SEAM: human visual via VisualLibrary("Human"); gameplay attached here.
            // No SetVisualTint — HumanUnit.Init enters Guard which sets its own dim-red
            // tint (G4). Guard/Alert color is FSM-driven.
            GameObject go = VisualLibrary.Spawn(VisualLibrary.KeyHuman, post, Quaternion.identity, transform);
            HumanUnit h = go.GetComponent<HumanUnit>() ?? go.AddComponent<HumanUnit>();
            h.Init(type, post);
            h.OnDied += _ => _alarm.Add(Balance.AlarmPerHumanKilled);
        }

        // ---------------------------------------------------------------- input

        private static InputActionAsset BuildInputAsset()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = asset.AddActionMap("Player");

            InputAction move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            map.AddAction("Warhorn", InputActionType.Button, "<Keyboard>/h");
            map.AddAction("Point", InputActionType.Value, "<Mouse>/position");
            map.AddAction("Order", InputActionType.Button, "<Mouse>/rightButton");
            map.AddAction("Select", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("SelectSquad1", InputActionType.Button, "<Keyboard>/1");
            map.AddAction("SelectSquad2", InputActionType.Button, "<Keyboard>/2");
            map.AddAction("SelectSquad3", InputActionType.Button, "<Keyboard>/3");
            map.AddAction("SelectAll", InputActionType.Button, "<Keyboard>/backquote");
            return asset;
        }

        private void BuildPlayer()
        {
            // ART SEAM: the hero visual comes from VisualLibrary("Warlord") — a bigger cyan
            // capsule fallback now, or a real prefab later. Built INACTIVE so PlayerInput.actions
            // is assigned before any OnEnable runs, then activated.
            GameObject go = VisualLibrary.Spawn(VisualLibrary.KeyWarlord,
                new Vector3(0f, 0f, 1f), Quaternion.identity, transform);
            go.name = "Warlord";
            go.SetActive(false);

            var pi = go.AddComponent<PlayerInput>();
            pi.actions = _inputAsset;
            pi.defaultActionMap = "Player";
            pi.neverAutoSwitchControlSchemes = true;
            // Use C# events ONLY. WarlordController/SquadCommander subscribe directly
            // to the InputActions; they do NOT expose OnMove(InputValue)-style methods.
            // The default 'SendMessages' behavior reflectively hunts for those methods
            // and throws MissingMethodException ('WarlordController.OnMove not found')
            // every input update — which also stalled the hero. InvokeCSharpEvents
            // stops that reflection entirely.
            pi.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            var warlord = go.AddComponent<WarlordController>();
            warlord.SetAlarm(_alarm);

            var commander = go.AddComponent<SquadCommander>();
            commander.Setup(_raid, Camera.main);

            // Wire the selection HUD: rebuild cards whenever the player picks squads.
            commander.OnSelectionChanged += selection =>
            {
                if (_selectionHud != null) _selectionHud.Refresh(selection);
            };

            // T5: Make the Warlord a real combat target that can die and end the raid.
            // WarlordUnit is a Unit subclass on Team.Goblin (humans target it via
            // FindNearestEnemy) but excluded from loot/extraction/win (INonObjectiveRaider).
            var warlordUnit = go.AddComponent<WarlordUnit>();

            go.SetActive(true); // Awake/OnEnable run now, with actions assigned

            warlordUnit.Init();                                       // 50 HP, 0 dmg/speed, Goblin team
            warlordUnit.SetVisualTint(VisualLibrary.WarlordCyan);     // G4: warlord is cyan + bigger
            warlordUnit.OnDied += _ => _raid.NotifyWarlordDown();     // Warlord falls -> LostWarlordDown

            _inputAsset.FindActionMap("Player").Enable();
        }

        // ---------------------------------------------------------------- HUD (UGUI, no OnGUI)

        private void BuildHud()
        {
            // Selection HUD: bottom-of-screen unit panel (its own canvas).
            var selHudGo = new GameObject("SelectionHUD");
            selHudGo.transform.SetParent(transform);
            _selectionHud = selHudGo.AddComponent<GoblinSiege.UI.SelectionHUD>();

            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Gold bar (top-left).
            MakeBar(canvasGo.transform, new Vector2(20f, -24f), new Color(0.25f, 0.22f, 0.1f),
                new Color(0.95f, 0.8f, 0.2f), out _goldFill, out _);
            _goldText = MakeLabel(canvasGo.transform, font, new Vector2(20f, -24f), "Gold 0 / 100");

            // Alarm bar (below gold).
            MakeBar(canvasGo.transform, new Vector2(20f, -64f), new Color(0.2f, 0.2f, 0.2f),
                new Color(0.3f, 0.8f, 0.3f), out _alarmFill, out _alarmFillImg);
            _alarmText = MakeLabel(canvasGo.transform, font, new Vector2(20f, -64f), "Alarm 0%");

            // Centered result text (hidden until raid ends).
            _resultText = MakeLabel(canvasGo.transform, font, new Vector2(0f, 0f), "");
            var rt = _resultText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(700f, 120f);
            _resultText.alignment = TextAnchor.MiddleCenter;
            _resultText.fontSize = 30;

            // ═══════════════════════════════════════════════════════════════════
            // T6: ONBOARDING — Goal Banner (auto-fades) + Controls Card (persistent)
            // ═══════════════════════════════════════════════════════════════════
            _goalBannerText = MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.5f, 0.85f),
                "Send your SAPPER squad to breach the gate · Loot the quota\nWalk near the GOLD disc (DOOR) to open it · Reach the BLUE zone to escape!",
                fontSize: 20);
            StartCoroutine(FadeOutAfterDelay(_goalBannerText, delaySeconds: 5f, fadeDuration: 1f));

            // Controls Card: small persistent reference in bottom-left corner.
            MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.02f, 0.02f),
                "WASD move · 1/2/3 select squad · ` select all\nRight-click order · H = Warhorn (once) · Walk near GOLD disc = open door",
                fontSize: 12,
                anchor: TextAnchor.LowerLeft,
                pivotAnchor: new Vector2(0f, 0f));

            // ═══════════════════════════════════════════════════════════════════
            // T7: THRESHOLD CALLOUTS + SCREEN-PULSE OVERLAY
            // ═══════════════════════════════════════════════════════════════════
            _thresholdCalloutText = MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.5f, 0.65f),
                "",
                fontSize: 28);
            _thresholdCalloutText.color = new Color(1f, 1f, 1f, 0f);

            var pulseGo = new GameObject("ScreenPulseOverlay");
            pulseGo.transform.SetParent(canvasGo.transform, false);
            _screenPulseOverlay = pulseGo.AddComponent<Image>();
            _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, 0f); // Red, start invisible.
            _screenPulseOverlay.raycastTarget = false; // DO NOT BLOCK INPUT.
            var pulseRt = _screenPulseOverlay.rectTransform;
            pulseRt.anchorMin = Vector2.zero;
            pulseRt.anchorMax = Vector2.one;
            pulseRt.offsetMin = Vector2.zero;
            pulseRt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// T6/T7 Helper: Creates a centered Text element with flexible anchor/pivot.
        /// </summary>
        private Text MakeCenteredText(Transform parent, Font font, Vector2 normalizedAnchor,
            string text, int fontSize, TextAnchor anchor = TextAnchor.MiddleCenter,
            Vector2? pivotAnchor = null)
        {
            var go = new GameObject("CenteredText");
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.color = Color.white;
            label.fontSize = fontSize;
            label.alignment = anchor;
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = normalizedAnchor;
            rt.pivot = pivotAnchor ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600f, 80f);
            return label;
        }

        /// <summary>
        /// T6: Coroutine that waits, then fades a Text's alpha to 0, then hides the GameObject.
        /// Frame-rate independent (G5). Null-guarded against teardown.
        /// </summary>
        private IEnumerator FadeOutAfterDelay(Text target, float delaySeconds, float fadeDuration)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (target == null || target.gameObject == null) yield break;

            Color startColor = target.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (target == null || target.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                target.color = new Color(startColor.r, startColor.g, startColor.b,
                    Mathf.Lerp(startColor.a, 0f, t));
                yield return null;
            }

            if (target != null && target.gameObject != null)
            {
                target.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
                target.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// T7: Shows threshold callout text with fade-in then fade-out. Null-guarded.
        /// </summary>
        private IEnumerator ShowThresholdCallout(string message, float displayDuration, float fadeDuration)
        {
            if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;

            _thresholdCalloutText.text = message;
            _thresholdCalloutText.gameObject.SetActive(true);

            float elapsed = 0f;
            float fadeInTime = fadeDuration * 0.5f;
            while (elapsed < fadeInTime)
            {
                if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInTime);
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, alpha);
                yield return null;
            }

            yield return new WaitForSeconds(displayDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, alpha);
                yield return null;
            }

            if (_thresholdCalloutText != null && _thresholdCalloutText.gameObject != null)
            {
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, 0f);
            }
        }

        /// <summary>
        /// T7: Flashes a red full-screen overlay to viscerally signal escalation. Null-guarded.
        /// </summary>
        private IEnumerator FlashScreenPulse(float startAlpha, float duration)
        {
            if (_screenPulseOverlay == null || _screenPulseOverlay.gameObject == null) yield break;

            float elapsed = 0f;
            _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, startAlpha);

            while (elapsed < duration)
            {
                if (_screenPulseOverlay == null || _screenPulseOverlay.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, alpha);
                yield return null;
            }

            if (_screenPulseOverlay != null && _screenPulseOverlay.gameObject != null)
            {
                _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, 0f);
            }
        }

        private void MakeBar(Transform parent, Vector2 anchoredPos, Color bg, Color fill,
            out RectTransform fillRect, out Image fillImg)
        {
            var bgGo = new GameObject("BarBg");
            bgGo.transform.SetParent(parent, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = bg;
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0f, 1f);
            bgRt.pivot = new Vector2(0f, 1f);
            bgRt.anchoredPosition = anchoredPos;
            bgRt.sizeDelta = new Vector2(_barWidth, 26f);

            var fillGo = new GameObject("BarFill");
            fillGo.transform.SetParent(bgGo.transform, false);
            fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fill;
            fillRect = fillImg.rectTransform;
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(_barWidth, 26f);
        }

        private Text MakeLabel(Transform parent, Font font, Vector2 anchoredPos, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.color = Color.white;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleLeft;
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos + new Vector2(8f, 0f);
            rt.sizeDelta = new Vector2(_barWidth, 26f);
            return label;
        }

        private void HookHud()
        {
            _quota.OnGoldChanged += (looted, quota) =>
            {
                float frac = quota > 0 ? Mathf.Clamp01((float)looted / quota) : 0f;
                _goldFill.sizeDelta = new Vector2(_barWidth * frac, _goldFill.sizeDelta.y);
                _goldText.text = $"Gold {looted} / {quota}";
            };
            _alarm.OnAlarmChanged += percent =>
            {
                _alarmFill.sizeDelta = new Vector2(_barWidth * Mathf.Clamp01(percent / 100f), _alarmFill.sizeDelta.y);
                _alarmText.text = $"Alarm {Mathf.RoundToInt(percent)}%";
            };
            _alarm.OnThresholdChanged += t =>
            {
                _alarmFillImg.color = t switch
                {
                    AlarmThreshold.Alerted => new Color(0.95f, 0.75f, 0.2f),
                    AlarmThreshold.Mobilized => new Color(0.95f, 0.4f, 0.15f),
                    AlarmThreshold.FullSally => new Color(0.9f, 0.15f, 0.15f),
                    _ => new Color(0.3f, 0.8f, 0.3f)
                };

                string calloutMessage = t switch
                {
                    AlarmThreshold.Alerted => "ALERTED — they've seen you",
                    AlarmThreshold.Mobilized => "MOBILIZED — the garrison musters",
                    AlarmThreshold.FullSally => "FULL SALLY — RUN!",
                    _ => null
                };

                if (!string.IsNullOrEmpty(calloutMessage))
                {
                    StartCoroutine(ShowThresholdCallout(calloutMessage, displayDuration: 1.5f, fadeDuration: 0.5f));
                    StartCoroutine(FlashScreenPulse(startAlpha: 0.4f, duration: 0.4f));
                }
            };
            _raid.OnRaidEnded += (result, looted, quota, surplus) =>
            {
                _resultText.text = result switch
                {
                    RaidResult.Won => $"VICTORY\nYou took what you needed and lived to spend it.\nLooted {looted} / {quota}  ·  Surplus {surplus}",
                    RaidResult.LostWarlordDown => "DEFEAT\nThe warlord fell. A leaderless warband scatters.",
                    RaidResult.LostSquadWipe => "DEFEAT\nThe warband is broken.",
                    RaidResult.LostAlarmMaxed => "DEFEAT\nYou reached for one more chest. The horns never stopped.",
                    _ => "RAID OVER"
                };
            };
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // T1: ZonePulse — Gentle extraction zone pulse for visual polish
        // ═══════════════════════════════════════════════════════════════════════════
        // Oscillates the attached GameObject's localScale around its original value.
        // WebGL-SAFE: uses only Update(), Mathf, Time and Transform.
        //
        // GUARDRAIL G5: the oscillation is a function of Time.time (absolute clock),
        // NOT a per-frame accumulator — so the pulse speed is identical at any frame
        // rate. No raw per-frame increments.
        // ═══════════════════════════════════════════════════════════════════════════
        private class ZonePulse : MonoBehaviour
        {
            private Vector3 _baseScale;
            private const float Amplitude = 0.05f;  // ±5% scale variation
            private const float Frequency = 2f;     // cycles per second

            private void Start()
            {
                _baseScale = transform != null ? transform.localScale : Vector3.one;
            }

            private void Update()
            {
                if (transform == null) return;
                float pulse = 1f + Amplitude * Mathf.Sin(Time.time * Frequency * Mathf.PI * 2f);
                transform.localScale = _baseScale * pulse;
            }
        }
    }
}
