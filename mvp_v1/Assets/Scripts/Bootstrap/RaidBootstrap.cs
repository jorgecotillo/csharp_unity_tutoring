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
        [SerializeField] private Vector3 cameraPosition = new(0f, 24f, -9f);
        [Tooltip("Downward pitch in degrees (~50–60 keeps the whole board readable).")]
        [SerializeField] private float cameraPitch = 58f;
        [SerializeField] private float cameraFov = 52f;

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

        // Embedded starter raid (mirrors Data/Raids/raid-01.json) so no Resources load is needed.
        // Positions are JSON [x, y]; RaidManager maps them to world (x, 0, y).
        private const string RaidJson = @"{
            ""id"": 1, ""name"": ""Thornbrook Hamlet"", ""act"": 1,
            ""quota"": 100, ""alarmFillPerSecond"": 0.8,
            ""garrisonRoster"": [""Militia""],
            ""reinforceIntervalByThreshold"": { ""alerted"": 18, ""mobilized"": 11 },
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

            // --- Systems live on one child GameObject ---
            var sysGo = new GameObject("Systems");
            sysGo.transform.SetParent(transform);
            _alarm = sysGo.AddComponent<AlarmSystem>();
            _quota = sysGo.AddComponent<QuotaSystem>();
            var garrison = sysGo.AddComponent<GarrisonSpawner>();

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
                MakePoint("Spawn0", new Vector3(0f, 0f, 16f)),
                MakePoint("Spawn1", new Vector3(-5f, 0f, 14f)),
                MakePoint("Spawn2", new Vector3(5f, 0f, 14f))
            };
            garrison.SetSpawnAssets(spawns);

            Transform deploy = MakePoint("Deploy", new Vector3(0f, 0f, 1f));

            // --- RaidManager: inject everything (no prefabs — visuals via VisualLibrary) ---
            var raidGo = new GameObject("RaidManager");
            raidGo.transform.SetParent(transform);
            _raid = raidGo.AddComponent<RaidManager>();
            _raid.Inject(RaidJson, _alarm, _quota, garrison, extraction, deploy);

            // --- A few static defenders so there's combat before reinforcements ---
            SpawnDefender(HumanType.Militia, new Vector3(-3f, 0f, 9.5f));
            SpawnDefender(HumanType.Militia, new Vector3(3f, 0f, 9.5f));
            SpawnDefender(HumanType.Militia, new Vector3(0f, 0f, 12.5f));

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
            // Big flat ground via the art seam (GroundField key). Its top sits at y≈0, the
            // plane the order-raycast (SquadCommander) intersects. Phase D will split this
            // into field + village zones; for Phase B one field plane is enough.
            VisualLibrary.Spawn(VisualLibrary.KeyGroundField,
                new Vector3(0f, -0.1f, 6f), Quaternion.identity, transform);
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

            var warlord = go.AddComponent<WarlordController>();
            warlord.SetAlarm(_alarm);

            var commander = go.AddComponent<SquadCommander>();
            commander.Setup(_raid, Camera.main);

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
                "Breach the gate. Loot the quota.\nReach the BLUE zone before the alarm fills.",
                fontSize: 22);
            StartCoroutine(FadeOutAfterDelay(_goalBannerText, delaySeconds: 5f, fadeDuration: 1f));

            // Controls Card: small persistent reference in bottom-left corner.
            MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.02f, 0.02f),
                "WASD move · 1/2/3 select squad · ` select all\nRight-click order · H = Warhorn (once)",
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
