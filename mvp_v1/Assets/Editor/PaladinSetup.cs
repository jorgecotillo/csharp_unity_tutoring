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
    // PaladinSetup — one-button (and auto-once) builder for the animated Paladin
    // human (3D_MIGRATION_SPEC Phase C). EDITOR-ONLY.
    // ═══════════════════════════════════════════════════════════════════════════
    // WHY AN EDITOR SCRIPT INSTEAD OF HAND-EDITING .meta / .prefab / .controller?
    //   A skinned-mesh prefab, a Humanoid avatar, retargeted animation clips and an
    //   AnimatorController are all binary-ish assets with internal fileIDs/GUIDs that
    //   are NOT safe to hand-author in a text editor. The supported, robust way is to
    //   drive Unity's own importer + asset APIs. That is exactly what this script does:
    //
    //     1. Import the base FBX as HUMANOID (create avatar), remap materials to the
    //        Built-in STANDARD shader and extract textures (so it isn't pink/magenta).
    //     2. Import @Walking / @Running as HUMANOID, Avatar = COPY FROM the base
    //        avatar, Loop Time = ON, root motion left to the prefab (applyRootMotion
    //        OFF — movement is physics-driven; animation is cosmetic).
    //     3. Build PaladinAnimator.controller: Idle → Walk → Run blended by a float
    //        "Speed" parameter (Walk > 0.1, Run > ~moveSpeed*0.7).
    //     4. Build Assets/Resources/Prefabs/Human.prefab = Paladin model + Animator +
    //        Rigidbody (G2 constraints) + CapsuleCollider + HumanUnit +
    //        UnitAnimatorDriver + UnitAlertIndicator(red ground ring). Once that prefab
    //        exists, VisualLibrary's "Human" key picks it up with ZERO code changes.
    //
    // HOW IT RUNS
    //   • Menu: Tools ▸ Goblin Siege ▸ Build Paladin Human  (re-run any time).
    //   • Auto-once: after scripts compile, if Human.prefab does not yet exist it runs
    //     itself one time so the human is ready with no manual clicks. Fully guarded in
    //     try/catch so a failure can never block compilation or play.
    //
    // GUARDRAILS honoured here:
    //   G3 — the prefab uses ONE shared ring material asset; no per-frame work is
    //        introduced; the human cap lives in GarrisonSpawner and is untouched.
    //   G5 — frame-rate independence is a RUNTIME concern handled by UnitAnimatorDriver
    //        (Speed from real velocity). This builder only wires it up.
    // ═══════════════════════════════════════════════════════════════════════════
    public static class PaladinSetup
    {
        // ── Asset paths (the Paladin lives here already) ──────────────────────────
        private const string Folder       = "Assets/Art/Characters/Paladin";
        private const string BaseFbx      = Folder + "/Paladin J Nordstrom.fbx";
        private const string WalkFbx      = Folder + "/Paladin J Nordstrom@Walking.fbx";
        private const string RunFbx       = Folder + "/Paladin J Nordstrom@Running.fbx";
        private const string TexFolder    = Folder + "/Textures";
        private const string RingMatPath  = Folder + "/AlertRing.mat";
        private const string Controller   = Folder + "/PaladinAnimator.controller";
        private const string PrefabPath   = "Assets/Resources/Prefabs/Human.prefab";

        // Diagnostic log file written OUTSIDE Assets/ (so Unity doesn't import it).
        // Resolves to mvp_v1/PaladinSetup.log — readable from outside the editor so a
        // build failure can be diagnosed without copy-pasting the Console.
        private static readonly string LogPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "PaladinSetup.log"));

        // Append one timestamped line to the diagnostic log (best-effort, never throws).
        private static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss}  {msg}{Environment.NewLine}"); }
            catch { /* logging must never break the build */ }
        }

        // Start a fresh log for this build attempt.
        private static void ResetLog(string header)
        {
            try { File.WriteAllText(LogPath, $"=== {header} @ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}"); }
            catch { /* ignore */ }
        }

        // ── Tuning ────────────────────────────────────────────────────────────────
        private const float TargetHeight = 1.8f;  // Paladin ≈ 1.8 m next to ~1 m goblins
        private const float WalkThreshold = 0.1f;  // Speed above which we leave Idle
        // Run when moving near full tilt. Human move speeds are ~2.2–2.6 (Balance.cs);
        // 1.8 ≈ 2.6 * 0.7, matching the spec's "Run > moveSpeed*0.7".
        private const float RunThreshold  = 1.8f;

        private const string SessionKey = "GoblinSiege.PaladinSetup.AutoBuildDone";

        // ───────────────────────────────────────────────────────────────────────
        // AUTO-ONCE: build the human after a domain reload if it doesn't exist yet.
        // ───────────────────────────────────────────────────────────────────────
        [InitializeOnLoadMethod]
        private static void Hook() => EditorApplication.delayCall += MaybeAutoBuild;

        private static void MaybeAutoBuild()
        {
            // Never fight the user while entering/playing.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // At most once per editor session.
            if (SessionState.GetBool(SessionKey, false)) return;
            // Already built? Nothing to do.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }
            // Source FBX not imported yet — try again on the next reload.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx) == null) return;

            try
            {
                Build();
                // Only mark "done for this session" on SUCCESS, so a transient
                // import-order failure auto-retries on the next domain reload
                // instead of being permanently skipped.
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                    SessionState.SetBool(SessionKey, true);
            }
            catch (Exception e)
            {
                Log($"AUTO-BUILD EXCEPTION: {e}");
                Debug.LogError($"[PaladinSetup] Auto-build failed (see {LogPath}; or use " +
                               $"Tools ▸ Goblin Siege ▸ Build Paladin Human to retry): {e}");
            }
        }

        [MenuItem("Tools/Goblin Siege/Build Paladin Human")]
        public static void Build()
        {
            ResetLog("Build Paladin Human");
            Debug.Log($"[PaladinSetup] Building animated Paladin human… (log: {LogPath})");

            try
            {
                Log("STEP 1: ConfigureBaseFbx (Humanoid avatar + materials)…");
                Avatar avatar = ConfigureBaseFbx();
                if (avatar == null)
                {
                    Log("FAIL: No Humanoid avatar produced from base FBX. " +
                        "Open the base FBX ▸ Rig ▸ Animation Type = Humanoid ▸ Apply, then retry.");
                    Debug.LogError("[PaladinSetup] No Humanoid avatar produced from base FBX — aborting. " +
                                   $"See {LogPath}. Open the base FBX ▸ Rig ▸ Animation Type = Humanoid.");
                    return;
                }
                Log($"  OK avatar='{avatar.name}' isHuman={avatar.isHuman} isValid={avatar.isValid}");

                Log("STEP 2: ConfigureAnimFbx Walking…");
                AnimationClip walk = ConfigureAnimFbx(WalkFbx, avatar);
                Log($"  walk clip = {(walk == null ? "NULL" : walk.name)}");
                Log("STEP 2: ConfigureAnimFbx Running…");
                AnimationClip run = ConfigureAnimFbx(RunFbx, avatar);
                Log($"  run clip = {(run == null ? "NULL" : run.name)}");

                Log("STEP 3: BuildController…");
                AnimatorController controller = BuildController(walk, run);
                Log($"  controller = {(controller == null ? "NULL" : controller.name)}");

                Log("STEP 4: BuildPrefab…");
                BuildPrefab(avatar, controller);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                bool ok = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
                Log(ok ? $"DONE: prefab saved at {PrefabPath}" : $"FAIL: prefab NOT found at {PrefabPath} after build");
                Debug.Log($"[PaladinSetup] {(ok ? "DONE" : "FAILED")}. See {LogPath}. " +
                          "VisualLibrary key \"Human\" now spawns the animated Paladin.");
            }
            catch (Exception e)
            {
                Log($"EXCEPTION: {e}");
                Debug.LogError($"[PaladinSetup] Build threw — see {LogPath}\n{e}");
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // 1) BASE FBX → Humanoid + Standard materials + extracted textures.
        // ───────────────────────────────────────────────────────────────────────
        private static Avatar ConfigureBaseFbx()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(BaseFbx);
            if (imp == null) { Debug.LogError($"[PaladinSetup] Missing {BaseFbx}"); return null; }

            // Rig → Humanoid, create the avatar from this model's own skeleton.
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;

            // Scale: keep Mixamo's file units; final height is guaranteed on the
            // prefab root (we measure & correct there), so this stays simple.
            imp.useFileScale = true;
            imp.globalScale  = 1f;

            // Materials → Built-in STANDARD shader via material description. In the
            // Built-in pipeline this maps to "Standard", so the model is NOT the
            // missing-shader pink/magenta. Import embedded materials first.
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation   = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();

            // Extract the embedded textures to real files so the materials reference
            // proper texture assets (and the user can edit them). Best-effort.
            try
            {
                if (!AssetDatabase.IsValidFolder(TexFolder))
                    AssetDatabase.CreateFolder(Folder, "Textures");
                if (imp.ExtractTextures(TexFolder))
                {
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(BaseFbx, ImportAssetOptions.ForceUpdate);
                }
            }
            catch (Exception e) { Debug.LogWarning($"[PaladinSetup] Texture extract skipped: {e.Message}"); }

            // Now externalise the materials so they become editable Standard .mat
            // assets (the "Extract Materials" outcome). Falls back to embedded if it
            // can't — either way they're Standard, so never pink.
            try
            {
                imp.materialLocation = ModelImporterMaterialLocation.External;
                imp.SaveAndReimport();
                AssetDatabase.Refresh();
            }
            catch (Exception e) { Debug.LogWarning($"[PaladinSetup] Material extract skipped: {e.Message}"); }

            // Hand back the freshly-baked avatar (a sub-asset of the FBX).
            return AssetDatabase.LoadAllAssetsAtPath(BaseFbx).OfType<Avatar>().FirstOrDefault();
        }

        // ───────────────────────────────────────────────────────────────────────
        // 2) ANIM FBX → Humanoid, Avatar = Copy From base, Loop = ON, no materials.
        // ───────────────────────────────────────────────────────────────────────
        private static AnimationClip ConfigureAnimFbx(string path, Avatar baseAvatar)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp == null) { Debug.LogError($"[PaladinSetup] Missing {path}"); return null; }

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup   = ModelImporterAvatarSetup.CopyFromOther;  // retarget onto the base rig
            imp.sourceAvatar  = baseAvatar;

            // These are animation-only files; don't import duplicate materials.
            imp.materialImportMode = ModelImporterMaterialImportMode.None;

            // Loop Time = ON for every clip; keep root in place (cosmetic anim — the
            // body is physics-driven, root motion is ignored on the prefab Animator).
            var clips = imp.defaultClipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.clipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = true;
                c.lockRootRotation = false;
                c.keepOriginalPositionY = true; // no vertical drift
            }
            imp.clipAnimations = clips;

            imp.SaveAndReimport();

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__"));
            if (clip == null) Debug.LogError($"[PaladinSetup] No clip found in {path}");
            return clip;
        }

        // ───────────────────────────────────────────────────────────────────────
        // 3) AnimatorController: Idle → Walk → Run blended by float "Speed".
        // ───────────────────────────────────────────────────────────────────────
        private static AnimatorController BuildController(AnimationClip walk, AnimationClip run)
        {
            // Rebuild clean for idempotency.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller) != null)
                AssetDatabase.DeleteAsset(Controller);

            var ac = AnimatorController.CreateAnimatorControllerAtPath(Controller);
            ac.AddParameter(UnitAnimatorDriver.SpeedParam, AnimatorControllerParameterType.Float);

            var sm = ac.layers[0].stateMachine;

            // No dedicated idle clip shipped → use the Walk clip almost frozen as a
            // standing "idle in place" (spec explicitly allows slowed-Walk for Idle).
            var idle = sm.AddState("Idle");
            idle.motion = walk;
            idle.speed  = 0.12f;

            var walkState = sm.AddState("Walk");
            walkState.motion = walk;

            var runState = sm.AddState("Run");
            runState.motion = run;

            sm.defaultState = idle;

            // Speed-driven transitions (snappy, no exit time — purely parameter based).
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
            t.hasExitTime = false;        // respond to Speed immediately
            t.hasFixedDuration = true;
            t.duration = 0.12f;           // brief cross-fade for a smooth blend
            t.AddCondition(mode, threshold, UnitAnimatorDriver.SpeedParam);
        }

        // ───────────────────────────────────────────────────────────────────────
        // 4) Human.prefab = Paladin model + gameplay + animation + alert ring.
        // ───────────────────────────────────────────────────────────────────────
        private static void BuildPrefab(Avatar avatar, AnimatorController controller)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx);
            if (model == null) { Debug.LogError("[PaladinSetup] Base model failed to load."); Log("  BuildPrefab FAIL: base model null"); return; }

            // Live, editable instance linked to the FBX (a prefab variant on save).
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.name = "Human";
            Log($"  instantiated model, renderers={inst.GetComponentsInChildren<Renderer>().Length}");

            // ── Guarantee ~1.8 m height regardless of the FBX's file units ──────────
            // Measure the rendered AABB and scale the root so the world height is
            // TargetHeight. This is the robust way to satisfy "≈1.8 m" without
            // guessing Mixamo's cm/m scale.
            float s = 1f;
            var rends = inst.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                if (b.size.y > 0.001f)
                {
                    s = TargetHeight / b.size.y;
                    inst.transform.localScale = Vector3.one * s;
                }
            }

            // ── Animator (cosmetic; root motion OFF — physics drives movement) ──────
            var anim = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            if (anim.avatar == null) anim.avatar = avatar;
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            // ── Rigidbody with the G2 flat-plane constraints (Unit.Awake also sets
            //    these at runtime; we author them here so the prefab is correct on disk).
            var rb = inst.GetComponent<Rigidbody>() ?? inst.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // ── CapsuleCollider sized in WORLD metres (divide by root scale s) ──────
            var cap = inst.GetComponent<CapsuleCollider>() ?? inst.AddComponent<CapsuleCollider>();
            cap.direction = 1; // Y axis
            cap.height = TargetHeight / s;
            cap.center = new Vector3(0f, (TargetHeight * 0.5f) / s, 0f);
            cap.radius = 0.30f / s;

            // ── Gameplay + animation driver ─────────────────────────────────────────
            if (inst.GetComponent<HumanUnit>() == null) inst.AddComponent<HumanUnit>();
            if (inst.GetComponent<UnitAnimatorDriver>() == null) inst.AddComponent<UnitAnimatorDriver>();

            // ── Alert ground ring (Guard hidden / Alert glowing) ────────────────────
            GameObject ring = BuildAlertRing(inst.transform, s);
            var indicator = inst.GetComponent<UnitAlertIndicator>() ?? inst.AddComponent<UnitAlertIndicator>();
            var body = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            indicator.Configure(ring, body);

            // ── Save as the deliverable prefab, then clean up the scene instance ────
            Log($"  saving prefab to {PrefabPath}…");
            PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath, out bool saveOk);
            Log($"  SaveAsPrefabAsset success={saveOk}");
            UnityEngine.Object.DestroyImmediate(inst);
        }

        // A thin emissive-red disc at the feet. One SHARED material asset (G3).
        private static GameObject BuildAlertRing(Transform parent, float rootScale)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AlertRing";

            // No physics — purely visual (matches the colliderless primitive units).
            var col = ring.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            ring.transform.SetParent(parent, false);
            // World target: ~1 m diameter, ~0.04 m tall, sitting just above ground.
            // Children inherit the root scale s, so divide by it to hit world sizes.
            ring.transform.localScale    = new Vector3(1f / rootScale, 0.02f / rootScale, 1f / rootScale);
            ring.transform.localPosition = new Vector3(0f, 0.03f / rootScale, 0f);

            ring.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateRingMaterial();

            // Guard = hidden; UnitAlertIndicator.SetAlerted(true) shows it.
            ring.SetActive(false);
            return ring;
        }

        private static Material GetOrCreateRingMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(RingMatPath);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Standard")) { name = "AlertRing" };
            var red = new Color(1f, 0.12f, 0.12f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", red);
            // Make it glow so it pops from the tilted RTS camera without lighting reliance.
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", red * 1.6f);

            AssetDatabase.CreateAsset(mat, RingMatPath);
            return mat;
        }
    }
}
#endif
