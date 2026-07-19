#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using GoblinSiege.Units;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GoblinSiege.EditorTools
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CharacterSetup — one-button (and auto-once) builder for the animated Mixamo
    // characters (3D_MIGRATION_SPEC Phase C). EDITOR-ONLY.
    // ═══════════════════════════════════════════════════════════════════════════
    // Builds up to THREE art prefabs from the FBXs imported under Assets/Art:
    //   • Maw J Laygo         → Resources/Prefabs/Warlord.prefab  (the PLAYER hero)
    //   • Paladin J Nordstrom → Resources/Prefabs/Human.prefab    (the ENEMY garrison)
    //   • Goblin (3rd char)   → Resources/Prefabs/Goblin.prefab   (the PLAYER goblins)
    //     …built only once its FBXs are dropped into Assets/Art/Characters/Goblin.
    //
    // Each prefab = the Humanoid model + retargeted Walk/Run clips + an
    // AnimatorController (Idle→Walk→Run blended by a float "Speed") + a Rigidbody
    // (G2 flat-plane constraints) + a CapsuleCollider + UnitAnimatorDriver. The
    // ENEMY prefab additionally gets the HumanUnit gameplay component and a red
    // UnitAlertIndicator ground ring (Guard vs Alert). The PLAYER prefab gets
    // neither — RaidBootstrap.BuildPlayer adds WarlordController/WarlordUnit on top.
    //
    // Once a prefab exists under Resources/Prefabs/<key>, VisualLibrary's matching
    // key ("Warlord" / "Human") picks it up automatically with ZERO code changes —
    // that is the whole point of the art-swap seam (§2).
    //
    // WHY AN EDITOR SCRIPT: a skinned-mesh prefab, a Humanoid avatar, retargeted
    // clips and an AnimatorController are binary-ish assets with internal fileIDs
    // that are NOT safe to hand-author. The supported route is Unity's own importer
    // + asset APIs, which is exactly what this script drives.
    //
    // HOW IT RUNS
    //   • Menu: Tools ▸ Goblin Siege ▸ Build Mixamo Characters  (re-run any time).
    //   • Auto-once: after scripts compile, if either prefab is missing it runs
    //     itself once. Guarded so a failure can never block compilation or play.
    //
    // DIAGNOSTICS: every run writes a step-by-step trace + any exception to
    // mvp_v1/PaladinSetup.log (outside Assets/, so it isn't imported). This makes a
    // build failure diagnosable without copy-pasting the Console.
    // ═══════════════════════════════════════════════════════════════════════════
    public static class CharacterSetup
    {
        // ── A character to build (player hero or enemy) ──────────────────────────
        private struct CharDef
        {
            public string Name;             // prefab/log name ("Warlord" / "Human")
            public string Folder;           // Assets/Art/Characters/<X>
            public string BaseFbx;          // rigged model
            public string WalkFbx, RunFbx;  // animation-only clips
            public string TexFolder;        // extracted textures
            public string Controller;       // generated .controller
            public string RingMat;          // alert-ring material (enemy only)
            public string Prefab;           // Resources/Prefabs/<key>.prefab
            public bool IncludeHumanUnit;   // enemy gameplay component
            public bool IncludeAlertRing;   // enemy Guard/Alert ground ring
            public float TargetHeight;      // world height in metres
        }

        // PLAYER hero = Maw. No HumanUnit/ring — BuildPlayer adds Warlord logic.
        private static CharDef Warlord()
        {
            const string f = "Assets/Art/Characters/Maw";
            return new CharDef
            {
                Name = "Warlord", Folder = f,
                BaseFbx = f + "/Maw J Laygo.fbx",
                WalkFbx = f + "/Maw J Laygo@Walking.fbx",
                RunFbx  = f + "/Maw J Laygo@Running.fbx",
                TexFolder = f + "/Textures",
                Controller = f + "/MawAnimator.controller",
                RingMat = f + "/AlertRing.mat",
                Prefab = "Assets/Resources/Prefabs/Warlord.prefab",
                IncludeHumanUnit = false, IncludeAlertRing = false,
                TargetHeight = 1.9f, // hero reads slightly taller than the garrison
            };
        }

        // ENEMY garrison = Paladin. Full gameplay: HumanUnit FSM + red alert ring.
        private static CharDef Human()
        {
            const string f = "Assets/Art/Characters/Paladin";
            return new CharDef
            {
                Name = "Human", Folder = f,
                BaseFbx = f + "/Paladin J Nordstrom.fbx",
                WalkFbx = f + "/Paladin J Nordstrom@Walking.fbx",
                RunFbx  = f + "/Paladin J Nordstrom@Running.fbx",
                TexFolder = f + "/Textures",
                Controller = f + "/PaladinAnimator.controller",
                // AlertRing.mat is a generic red ring that only lives in the Maw
                // folder — reuse it here (Paladin has none of its own).
                RingMat = "Assets/Art/Characters/Maw/AlertRing.mat",
                Prefab = "Assets/Resources/Prefabs/Human.prefab",
                IncludeHumanUnit = true, IncludeAlertRing = true,
                TargetHeight = 1.8f,
            };
        }

        // PLAYER goblins = the THIRD Mixamo character. Visual-only, like the Warlord:
        // Squad.Build attaches GoblinUnit + Rigidbody at spawn, so this prefab carries
        // NO gameplay component and NO enemy alert ring. Goblins read a little shorter
        // than the hero so the Warlord still stands out in the pack (G4 readability).
        //
        // TO ADD THE ART: drop the character's 3 Mixamo FBX files into
        //   Assets/Art/Characters/Goblin/
        // renamed EXACTLY to:  Goblin.fbx , Goblin@Walking.fbx , Goblin@Running.fbx
        // Then the builder makes Resources/Prefabs/Goblin.prefab and every goblin uses
        // it automatically (VisualLibrary key "Goblin"). Until those files exist,
        // BuildOne SKIPS this safely and goblins stay green capsules.
        private static CharDef Goblin()
        {
            const string f = "Assets/Art/Characters/Goblin";
            return new CharDef
            {
                Name = "Goblin", Folder = f,
                BaseFbx = f + "/Goblin.fbx",
                WalkFbx = f + "/Goblin@Walking.fbx",
                RunFbx  = f + "/Goblin@Running.fbx",
                TexFolder = f + "/Textures",
                Controller = f + "/GoblinAnimator.controller",
                RingMat = "Assets/Art/Characters/Maw/AlertRing.mat", // unused (no ring)
                Prefab = "Assets/Resources/Prefabs/Goblin.prefab",
                IncludeHumanUnit = false, IncludeAlertRing = false,
                TargetHeight = 1.4f,
            };
        }

        // ── Tuning ────────────────────────────────────────────────────────────────
        private const float WalkThreshold = 0.1f;  // Speed above which we leave Idle
        private const float RunThreshold  = 1.8f;   // ≈ moveSpeed*0.7

        // ── Diagnostic log (outside Assets/ so Unity won't import it) ─────────────
        private static readonly string LogPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PaladinSetup.log"));

        private static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}"); }
            catch { /* logging must never break the build */ }
        }

        private static void ResetLog(string header)
        {
            try { File.WriteAllText(LogPath, $"=== {header} @ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}"); }
            catch { /* ignore */ }
        }

        private const string SessionKey = "GoblinSiege.CharacterSetup.AutoBuildDone.v1";

        // ───────────────────────────────────────────────────────────────────────
        // AUTO-ONCE after a domain reload, if either prefab is missing.
        // ───────────────────────────────────────────────────────────────────────
        [InitializeOnLoadMethod]
        private static void Hook() => EditorApplication.delayCall += MaybeAutoBuild;

        private static void MaybeAutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(SessionKey, false)) return;

            bool needWarlord = AssetDatabase.LoadAssetAtPath<GameObject>(Warlord().Prefab) == null;
            bool needHuman   = AssetDatabase.LoadAssetAtPath<GameObject>(Human().Prefab) == null;
            // Goblin is optional: only require a build once its FBX has been dropped in,
            // so an absent third character never triggers an endless retry.
            bool goblinFbxPresent = AssetDatabase.LoadAssetAtPath<GameObject>(Goblin().BaseFbx) != null;
            bool needGoblin  = goblinFbxPresent &&
                               AssetDatabase.LoadAssetAtPath<GameObject>(Goblin().Prefab) == null;
            if (!needWarlord && !needHuman && !needGoblin) { SessionState.SetBool(SessionKey, true); return; }

            // Don't run until at least one base FBX is imported.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Warlord().BaseFbx) == null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(Human().BaseFbx) == null) return;

            try
            {
                Build();
                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(Warlord().Prefab) != null
                       && AssetDatabase.LoadAssetAtPath<GameObject>(Human().Prefab) != null
                       && (!goblinFbxPresent ||
                           AssetDatabase.LoadAssetAtPath<GameObject>(Goblin().Prefab) != null);
                if (ok) SessionState.SetBool(SessionKey, true); // only stop retrying on full success
            }
            catch (Exception e)
            {
                Log($"AUTO-BUILD EXCEPTION: {e}");
                Debug.LogError($"[CharacterSetup] Auto-build failed (see {LogPath}; or use " +
                               $"Tools ▸ Goblin Siege ▸ Build Mixamo Characters): {e}");
            }
        }

        [MenuItem("Tools/Goblin Siege/Build Mixamo Characters")]
        public static void Build()
        {
            ResetLog("Build Mixamo Characters");
            Debug.Log($"[CharacterSetup] Building Mixamo characters… (log: {LogPath})");

            BuildOne(Warlord());
            BuildOne(Human());
            BuildOne(Goblin()); // skips safely until the third character's FBXs exist

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool w = AssetDatabase.LoadAssetAtPath<GameObject>(Warlord().Prefab) != null;
            bool h = AssetDatabase.LoadAssetAtPath<GameObject>(Human().Prefab) != null;
            bool g = AssetDatabase.LoadAssetAtPath<GameObject>(Goblin().Prefab) != null;
            Log($"RESULT: Warlord.prefab={w}  Human.prefab={h}  Goblin.prefab={g}");
            Debug.Log($"[CharacterSetup] Warlord.prefab={w}  Human.prefab={h}  Goblin.prefab={g}  (see {LogPath})");
        }

        // Build a single character prefab. Never throws past its own try — a failure
        // on one character must not block the other.
        private static void BuildOne(CharDef def)
        {
            Log($"──── {def.Name}  ({def.BaseFbx}) ────");
            try
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(def.BaseFbx) == null)
                {
                    Log($"  SKIP: base FBX not found/imported yet: {def.BaseFbx}");
                    return;
                }

                Log("  STEP 1: ConfigureBaseFbx (Humanoid avatar + materials)…");
                Avatar avatar = ConfigureBaseFbx(def);
                if (avatar == null)
                {
                    Log("  FAIL: no Humanoid avatar. Open the base FBX ▸ Rig ▸ Animation Type = Humanoid ▸ Apply.");
                    Debug.LogError($"[CharacterSetup] {def.Name}: no Humanoid avatar from {def.BaseFbx} — see {LogPath}.");
                    return;
                }
                Log($"    OK avatar='{avatar.name}' isHuman={avatar.isHuman} isValid={avatar.isValid}");

                Log("  STEP 2: retarget Walk/Run clips…");
                AnimationClip walk = ConfigureAnimFbx(def.WalkFbx, avatar);
                AnimationClip run  = ConfigureAnimFbx(def.RunFbx, avatar);
                Log($"    walk={(walk == null ? "NULL" : walk.name)}  run={(run == null ? "NULL" : run.name)}");

                Log("  STEP 3: BuildController…");
                AnimatorController controller = BuildController(def, walk, run);
                Log($"    controller={(controller == null ? "NULL" : controller.name)}");

                Log("  STEP 4: BuildPrefab…");
                BuildPrefab(def, avatar, controller);

                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(def.Prefab) != null;
                Log(ok ? $"  DONE: {def.Prefab}" : $"  FAIL: prefab missing after build: {def.Prefab}");
            }
            catch (Exception e)
            {
                Log($"  EXCEPTION building {def.Name}: {e}");
                Debug.LogError($"[CharacterSetup] {def.Name} build threw — see {LogPath}\n{e}");
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // 1) BASE FBX → Humanoid + Standard materials + extracted textures.
        // ───────────────────────────────────────────────────────────────────────
        private static Avatar ConfigureBaseFbx(CharDef def)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(def.BaseFbx);
            if (imp == null) { Log($"    missing importer for {def.BaseFbx}"); return null; }

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.useFileScale  = true;
            imp.globalScale   = 1f;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation   = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();

            // Extract embedded textures to real files (best-effort).
            try
            {
                if (!AssetDatabase.IsValidFolder(def.TexFolder))
                    AssetDatabase.CreateFolder(def.Folder, "Textures");
                if (imp.ExtractTextures(def.TexFolder))
                {
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(def.BaseFbx, ImportAssetOptions.ForceUpdate);
                }
            }
            catch (Exception e) { Log($"    texture extract skipped: {e.Message}"); }

            // Externalise materials so they become editable Standard .mat assets.
            try
            {
                imp.materialLocation = ModelImporterMaterialLocation.External;
                imp.SaveAndReimport();
                AssetDatabase.Refresh();
            }
            catch (Exception e) { Log($"    material extract skipped: {e.Message}"); }

            return AssetDatabase.LoadAllAssetsAtPath(def.BaseFbx).OfType<Avatar>().FirstOrDefault();
        }

        // ───────────────────────────────────────────────────────────────────────
        // 2) ANIM FBX → Humanoid, Avatar = Copy From base, Loop = ON, no materials.
        // ───────────────────────────────────────────────────────────────────────
        private static AnimationClip ConfigureAnimFbx(string path, Avatar baseAvatar)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Log($"    missing anim importer for {path}"); return null; }

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup   = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar  = baseAvatar;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;

            var clips = imp.defaultClipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.clipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = true;
                c.lockRootRotation = false;
                c.keepOriginalPositionY = true;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__"));
        }

        // ───────────────────────────────────────────────────────────────────────
        // 3) AnimatorController: Idle → Walk → Run blended by float "Speed".
        // ───────────────────────────────────────────────────────────────────────
        private static AnimatorController BuildController(CharDef def, AnimationClip walk, AnimationClip run)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(def.Controller) != null)
                AssetDatabase.DeleteAsset(def.Controller);

            var ac = AnimatorController.CreateAnimatorControllerAtPath(def.Controller);
            ac.AddParameter(UnitAnimatorDriver.SpeedParam, AnimatorControllerParameterType.Float);

            var sm = ac.layers[0].stateMachine;

            // No idle clip shipped → use the Walk clip almost frozen as "idle in place".
            var idle = sm.AddState("Idle");
            idle.motion = walk;
            idle.speed  = 0.12f;

            var walkState = sm.AddState("Walk");
            walkState.motion = walk;

            var runState = sm.AddState("Run");
            runState.motion = run;

            sm.defaultState = idle;

            AddTransition(idle,      walkState, AnimatorConditionMode.Greater, WalkThreshold);
            AddTransition(walkState, idle,      AnimatorConditionMode.Less,    WalkThreshold);
            AddTransition(walkState, runState,  AnimatorConditionMode.Greater, RunThreshold);
            AddTransition(runState,  walkState, AnimatorConditionMode.Less,    RunThreshold);

            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            return ac;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to,
            AnimatorConditionMode mode, float threshold)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = 0.12f;
            t.AddCondition(mode, threshold, UnitAnimatorDriver.SpeedParam);
        }

        // ───────────────────────────────────────────────────────────────────────
        // 4) <key>.prefab = model + animation + Rigidbody + collider (+ enemy gameplay).
        // ───────────────────────────────────────────────────────────────────────
        private static void BuildPrefab(CharDef def, Avatar avatar, AnimatorController controller)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(def.BaseFbx);
            if (model == null) { Log("    BuildPrefab FAIL: base model null"); return; }

            // ── ROBUST ROOT (fixes the reported MissingComponentException) ──────────
            // We do NOT add gameplay components onto the FBX MODEL instance root:
            // AddComponent on a freshly-instantiated / mid-import model prefab can
            // return null (→ "no 'Rigidbody' attached" when we then touch it). Instead
            // we build a PLAIN GameObject we fully own, add Rigidbody/collider/scripts
            // to IT, and parent the Mixamo model as a pure VISUAL CHILD. The model
            // keeps its Animator/skeleton; our root keeps the physics + gameplay.
            var root = new GameObject(def.Name);

            var modelInst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (modelInst == null) { Log("    BuildPrefab FAIL: model instance null"); UnityEngine.Object.DestroyImmediate(root); return; }
            modelInst.transform.SetParent(root.transform, false);
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;

            // UNPACK COMPLETELY so the model's Animator, avatar binding, SkinnedMesh
            // and skeleton become CONCRETE components owned by our prefab — not a
            // nested FBX prefab instance. A nested Humanoid instance can fail to drive
            // the rig at runtime (the model renders in bind/T-pose). Unpacking makes
            // the controller/avatar assignments below stick reliably.
            PrefabUtility.UnpackPrefabInstance(modelInst, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            Log($"    instantiated + unpacked model under root, renderers={modelInst.GetComponentsInChildren<Renderer>().Length}");

            // Scale the MODEL child so its rendered AABB height == def.TargetHeight
            // (root stays at scale 1, so the collider/ring below use world metres).
            var rends = modelInst.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                if (b.size.y > 0.001f)
                    modelInst.transform.localScale = Vector3.one * (def.TargetHeight / b.size.y);
            }

            // Animator stays on the MODEL (where the avatar/skeleton live). Cosmetic;
            // root motion OFF — physics drives movement. Force the avatar explicitly
            // (not just if-null) and AlwaysAnimate so the rig is driven on first frame
            // regardless of visibility — guards against the bind/T-pose symptom.
            var anim = modelInst.GetComponent<Animator>() ?? modelInst.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Log($"    animator: avatar={(anim.avatar == null ? "NULL" : anim.avatar.name)} controller={(controller == null ? "NULL" : controller.name)}");

            // Rigidbody on the OWNED root with G2 flat-plane constraints.
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // CapsuleCollider on the root, sized in WORLD metres (root scale == 1).
            var cap = root.AddComponent<CapsuleCollider>();
            cap.direction = 1; // Y axis
            cap.height = def.TargetHeight;
            cap.center = new Vector3(0f, def.TargetHeight * 0.5f, 0f);
            cap.radius = 0.30f;

            // Animation driver on the root (finds the Animator in the model child).
            root.AddComponent<UnitAnimatorDriver>();

            // Enemy-only gameplay: HumanUnit + red Guard/Alert ground ring.
            if (def.IncludeHumanUnit) root.AddComponent<HumanUnit>();

            if (def.IncludeAlertRing)
            {
                GameObject ring = BuildAlertRing(root.transform, 1f, def.RingMat);
                var indicator = root.AddComponent<UnitAlertIndicator>();
                var body = modelInst.GetComponentInChildren<SkinnedMeshRenderer>();
                indicator.Configure(ring, body);
            }

            Log($"    saving prefab → {def.Prefab}…");
            PrefabUtility.SaveAsPrefabAsset(root, def.Prefab, out bool saveOk);
            Log($"    SaveAsPrefabAsset success={saveOk}");
            UnityEngine.Object.DestroyImmediate(root);
        }

        // A thin emissive-red disc at the feet. One SHARED material asset (G3).
        private static GameObject BuildAlertRing(Transform parent, float rootScale, string ringMatPath)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AlertRing";

            var col = ring.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            ring.transform.SetParent(parent, false);
            ring.transform.localScale    = new Vector3(1f / rootScale, 0.02f / rootScale, 1f / rootScale);
            ring.transform.localPosition = new Vector3(0f, 0.03f / rootScale, 0f);

            ring.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateRingMaterial(ringMatPath);

            ring.SetActive(false); // Guard = hidden; SetAlerted(true) shows it.
            return ring;
        }

        private static Material GetOrCreateRingMaterial(string ringMatPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ringMatPath);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Standard")) { name = "AlertRing" };
            var red = new Color(1f, 0.12f, 0.12f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", red);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", red * 1.6f);

            AssetDatabase.CreateAsset(mat, ringMatPath);
            return mat;
        }
    }
}
#endif
