using System.Collections;
using GoblinSiege.Core;
using GoblinSiege.Player;
using GoblinSiege.Systems;
using GoblinSiege.Units;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GoblinSiege.Bootstrap
{
    /// <summary>
    /// ONE-CLICK PLAYABLE DEMO. Drop this on an empty GameObject in an empty scene and press Play.
    /// It builds the entire raid in code — camera, sprites, prefabs, systems, the Warlord + squads,
    /// loot caches, gates, defenders, the new Input System bindings, and a minimal HUD —
    /// with no manual scene/prefab authoring. Lets you playtest the loop before making real art.
    ///
    /// Controls: WASD/Arrows move the Warlord. 1/2/3 select a squad, ` selects all,
    /// right-click orders them, H sounds the Warhorn (drops alarm once).
    /// </summary>
    public class RaidBootstrap : MonoBehaviour
    {
        [Header("Play area / camera")]
        [SerializeField] private float cameraSize = 11f;
        [SerializeField] private Vector2 cameraCenter = new(0f, 8f);

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

        private Sprite _sprite;
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
            _sprite = MakeUnitSprite();
            _inputAsset = BuildInputAsset();

            SetupCamera();

            // --- Systems live on one child GameObject ---
            var sysGo = new GameObject("Systems");
            sysGo.transform.SetParent(transform);
            _alarm = sysGo.AddComponent<AlarmSystem>();
            _quota = sysGo.AddComponent<QuotaSystem>();
            var garrison = sysGo.AddComponent<GarrisonSpawner>();

            // --- Extraction zone (south edge) ---
            // T1: Blue translucent "goal" zone, visually distinct from green goblins,
            // gold caches, brown gates, and red humans. Blue = "go here to win."
            var extractionGo = MakeSpriteObject("Extraction", new Vector2(0f, 0f),
                new Vector3(5f, 2.5f, 1f), new Color(0.2f, 0.5f, 0.95f, 0.3f), sorting: -5);
            var extraction = extractionGo.AddComponent<ExtractionZone>();
            // T1 polish: Gentle pulse draws attention to the goal zone without being distracting.
            extractionGo.AddComponent<ZonePulse>();

            // --- Template "prefabs" (kept inactive) ---
            GoblinUnit goblinPrefab = MakeGoblinPrefab();
            HumanUnit humanPrefab = MakeHumanPrefab();
            LootCache cachePrefab = MakeCachePrefab();
            Gate gatePrefab = MakeGatePrefab();

            // --- Garrison spawn points (top of the map) ---
            Transform[] spawns =
            {
                MakePoint("Spawn0", new Vector2(0f, 16f)),
                MakePoint("Spawn1", new Vector2(-5f, 14f)),
                MakePoint("Spawn2", new Vector2(5f, 14f))
            };
            garrison.SetSpawnAssets(humanPrefab, spawns);

            Transform deploy = MakePoint("Deploy", new Vector2(0f, 1f));

            // --- RaidManager: inject everything, which starts the raid ---
            var raidGo = new GameObject("RaidManager");
            raidGo.transform.SetParent(transform);
            _raid = raidGo.AddComponent<RaidManager>();
            _raid.Inject(RaidJson, _alarm, _quota, garrison, extraction,
                goblinPrefab, cachePrefab, gatePrefab, deploy);

            // --- A few static defenders so there's combat before reinforcements ---
            SpawnDefender(humanPrefab, HumanType.Militia, new Vector2(-3f, 9.5f));
            SpawnDefender(humanPrefab, HumanType.Militia, new Vector2(3f, 9.5f));
            SpawnDefender(humanPrefab, HumanType.Militia, new Vector2(0f, 12.5f));

            // --- Player (Warlord hero + squad commander share one PlayerInput) ---
            BuildPlayer();

            // --- HUD + wiring ---
            BuildHud();
            HookHud();
        }

        // ---------------------------------------------------------------- assets

        private static Sprite MakeUnitSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.transform.SetPositionAndRotation(
                new Vector3(cameraCenter.x, cameraCenter.y, -10f), Quaternion.identity);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.5f, 0.42f);
        }

        // ---------------------------------------------------------------- factories

        private GameObject MakeSpriteObject(string name, Vector2 pos, Vector3 scale, Color color, int sorting = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = color;
            sr.sortingOrder = sorting;
            return go;
        }

        private Transform MakePoint(string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.position = pos;
            return go.transform;
        }

        private GoblinUnit MakeGoblinPrefab()
        {
            var go = new GameObject("GoblinPrefab");
            go.SetActive(false);
            go.transform.localScale = Vector3.one * 0.55f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.35f, 0.8f, 0.3f);
            sr.sortingOrder = 2;
            return go.AddComponent<GoblinUnit>(); // RequireComponent adds Rigidbody2D
        }

        private HumanUnit MakeHumanPrefab()
        {
            var go = new GameObject("HumanPrefab");
            go.SetActive(false);
            go.transform.localScale = Vector3.one * 0.55f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.85f, 0.25f, 0.25f);
            sr.sortingOrder = 2;
            return go.AddComponent<HumanUnit>();
        }

        private LootCache MakeCachePrefab()
        {
            var go = new GameObject("CachePrefab");
            go.SetActive(false);
            go.transform.localScale = Vector3.one * 0.8f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.95f, 0.8f, 0.2f);
            sr.sortingOrder = 1;
            return go.AddComponent<LootCache>();
        }

        private Gate MakeGatePrefab()
        {
            var go = new GameObject("GatePrefab");
            go.SetActive(false);
            go.transform.localScale = new Vector3(3f, 0.6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.5f, 0.35f, 0.2f);
            sr.sortingOrder = 1;
            return go.AddComponent<Gate>();
        }

        private void SpawnDefender(HumanUnit prefab, HumanType type, Vector2 post)
        {
            HumanUnit h = Instantiate(prefab, post, Quaternion.identity, transform);
            h.gameObject.SetActive(true);
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
            // Built inactive so Awake/OnEnable run only AFTER PlayerInput.actions is assigned.
            var go = new GameObject("Warlord");
            go.SetActive(false);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, 1f, 0f);
            // T1: Warlord is the unique hero unit — cyan color + larger scale makes it
            // instantly distinct from the smaller green goblins (0.55 scale, 0.35/0.8/0.3).
            go.transform.localScale = Vector3.one * 1.0f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            // T1: Bright cyan stands out against green goblins, red humans, gold caches.
            // The player should always know where their hero is on screen.
            sr.color = new Color(0.2f, 0.95f, 0.95f);
            sr.sortingOrder = 3;

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
            // This creates the "standout mechanic" tension: the Warlord leads from the front
            // but can fall, causing defeat. Position the hero carefully!
            var warlordUnit = go.AddComponent<WarlordUnit>();
            warlordUnit.Init();                                   // 50 HP, 0 dmg/speed, Goblin team
            warlordUnit.OnDied += _ => _raid.NotifyWarlordDown(); // Warlord falls -> LostWarlordDown (idempotent)

            go.SetActive(true);
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
            // New players need to know: what's the goal, and how do I control this?
            // The goal banner is prominent but fades after 5s so it doesn't distract.
            // The controls card is small and stays visible for reference.

            // Goal Banner: tells the player the victory condition in ~3 seconds of reading.
            // Centered near top, fades out after 5 seconds so it doesn't obscure play.
            _goalBannerText = MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.5f, 0.85f),
                "Breach the gate. Loot the quota.\nReach the BLUE zone before the alarm fills.",
                fontSize: 22);
            // Start the fade coroutine: wait 5s, then fade alpha 1→0 over 1s.
            StartCoroutine(FadeOutAfterDelay(_goalBannerText, delaySeconds: 5f, fadeDuration: 1f));

            // Controls Card: small persistent reference in bottom-left corner.
            // Mirrors the actual bindings in BuildInputAsset (WASD, 1/2/3, `, right-click, H).
            MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.02f, 0.02f),
                "WASD move · 1/2/3 select squad · ` select all\nRight-click order · H = Warhorn (once)",
                fontSize: 12,
                anchor: TextAnchor.LowerLeft,
                pivotAnchor: new Vector2(0f, 0f));

            // ═══════════════════════════════════════════════════════════════════
            // T7: THRESHOLD CALLOUTS + SCREEN-PULSE OVERLAY
            // ═══════════════════════════════════════════════════════════════════
            // These are built once (hidden/alpha=0) and driven by HookHud coroutines.
            // The callout text appears center-screen on threshold escalation.
            // The red overlay flashes briefly to viscerally signal danger escalation.

            // Threshold Callout Text: center-screen, starts hidden (alpha 0).
            // Driven by _alarm.OnThresholdChanged in HookHud.
            _thresholdCalloutText = MakeCenteredText(canvasGo.transform, font,
                new Vector2(0.5f, 0.65f),
                "",
                fontSize: 28);
            // Start hidden (alpha 0).
            _thresholdCalloutText.color = new Color(1f, 1f, 1f, 0f);

            // Screen-Pulse Overlay: full-screen red tint that flashes on threshold step-up.
            // CRITICAL: raycastTarget=false so it doesn't block mouse input (orders, selection).
            var pulseGo = new GameObject("ScreenPulseOverlay");
            pulseGo.transform.SetParent(canvasGo.transform, false);
            _screenPulseOverlay = pulseGo.AddComponent<Image>();
            _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, 0f); // Red, start invisible.
            _screenPulseOverlay.raycastTarget = false; // DO NOT BLOCK INPUT.
            // Stretch to fill entire canvas (anchor 0,0 to 1,1, offset zero).
            var pulseRt = _screenPulseOverlay.rectTransform;
            pulseRt.anchorMin = Vector2.zero;
            pulseRt.anchorMax = Vector2.one;
            pulseRt.offsetMin = Vector2.zero;
            pulseRt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// T6/T7 Helper: Creates a centered Text element with flexible anchor/pivot.
        /// Unlike MakeLabel (left-anchored for bars), this anchors to a normalized screen position.
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
            // Anchor to normalized screen position (0.5, 0.5 = center, 0.5/0.85 = top-center).
            rt.anchorMin = rt.anchorMax = normalizedAnchor;
            // Pivot defaults to center; override for corner-anchored text like controls card.
            rt.pivot = pivotAnchor ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600f, 80f); // Wide enough for multi-line text.
            return label;
        }

        /// <summary>
        /// T6: Coroutine that waits, then fades a Text's alpha to 0, then hides the GameObject.
        /// Null-guarded: if the Text or its GameObject is destroyed mid-fade, exits cleanly.
        /// </summary>
        private IEnumerator FadeOutAfterDelay(Text target, float delaySeconds, float fadeDuration)
        {
            // Wait before starting fade.
            yield return new WaitForSeconds(delaySeconds);

            // Null-guard: the canvas/Text may have been destroyed if the scene changed.
            if (target == null || target.gameObject == null) yield break;

            // Capture starting color and lerp alpha to 0.
            Color startColor = target.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                // Null-guard each iteration: object could be destroyed mid-fade.
                if (target == null || target.gameObject == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                target.color = new Color(startColor.r, startColor.g, startColor.b,
                    Mathf.Lerp(startColor.a, 0f, t));
                yield return null;
            }

            // Final cleanup: set fully invisible and optionally disable.
            if (target != null && target.gameObject != null)
            {
                target.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
                target.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// T7: Shows threshold callout text with fade-in then fade-out.
        /// Null-guarded for robustness at raid-end teardown.
        /// </summary>
        private IEnumerator ShowThresholdCallout(string message, float displayDuration, float fadeDuration)
        {
            if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;

            _thresholdCalloutText.text = message;
            _thresholdCalloutText.gameObject.SetActive(true);

            // Fade in quickly (alpha 0→1 over fadeDuration/2).
            float elapsed = 0f;
            float fadeInTime = fadeDuration * 0.5f;
            while (elapsed < fadeInTime)
            {
                if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInTime);
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, alpha); // Warm white/yellow.
                yield return null;
            }

            // Hold at full alpha.
            yield return new WaitForSeconds(displayDuration);

            // Fade out (alpha 1→0 over fadeDuration).
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (_thresholdCalloutText == null || _thresholdCalloutText.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, alpha);
                yield return null;
            }

            // Final hide.
            if (_thresholdCalloutText != null && _thresholdCalloutText.gameObject != null)
            {
                _thresholdCalloutText.color = new Color(1f, 1f, 0.8f, 0f);
            }
        }

        /// <summary>
        /// T7: Flashes a red full-screen overlay to viscerally signal alarm escalation.
        /// Fades from startAlpha→0 over duration. Null-guarded.
        /// </summary>
        private IEnumerator FlashScreenPulse(float startAlpha, float duration)
        {
            if (_screenPulseOverlay == null || _screenPulseOverlay.gameObject == null) yield break;

            float elapsed = 0f;
            Color pulseColor = new Color(0.9f, 0.1f, 0.1f, startAlpha); // Red.
            _screenPulseOverlay.color = pulseColor;

            while (elapsed < duration)
            {
                if (_screenPulseOverlay == null || _screenPulseOverlay.gameObject == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                _screenPulseOverlay.color = new Color(0.9f, 0.1f, 0.1f, alpha);
                yield return null;
            }

            // Ensure fully transparent at end.
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
                // Existing behavior: change bar fill color based on threshold.
                _alarmFillImg.color = t switch
                {
                    AlarmThreshold.Alerted => new Color(0.95f, 0.75f, 0.2f),
                    AlarmThreshold.Mobilized => new Color(0.95f, 0.4f, 0.15f),
                    AlarmThreshold.FullSally => new Color(0.9f, 0.15f, 0.15f),
                    _ => new Color(0.3f, 0.8f, 0.3f)
                };

                // T7: Threshold callouts + screen-pulse — "feel the escalation."
                // Each step-up shows a center-screen warning and flashes the screen red.
                // Unaware threshold (initial state) shows nothing.
                // These coroutines are null-guarded for robustness at raid-end teardown.
                string calloutMessage = t switch
                {
                    AlarmThreshold.Alerted => "ALERTED — they've seen you",
                    AlarmThreshold.Mobilized => "MOBILIZED — the garrison musters",
                    AlarmThreshold.FullSally => "FULL SALLY — RUN!",
                    _ => null // Unaware: no callout.
                };

                if (!string.IsNullOrEmpty(calloutMessage))
                {
                    // Show callout text: fade in, display ~1.5s, fade out over ~0.5s.
                    StartCoroutine(ShowThresholdCallout(calloutMessage, displayDuration: 1.5f, fadeDuration: 0.5f));
                    // Flash red overlay: alpha 0.4→0 over 0.4s for a brief visceral "danger" pulse.
                    StartCoroutine(FlashScreenPulse(startAlpha: 0.4f, duration: 0.4f));
                }
            };
            _raid.OnRaidEnded += (result, looted, quota, surplus) =>
            {
                _resultText.text = result switch
                {
                    RaidResult.Won => $"VICTORY\nYou took what you needed and lived to spend it.\nLooted {looted} / {quota}  ·  Surplus {surplus}",
                    // T5: Warlord death ends the raid — the unique hero mechanic has real stakes.
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
        // A tiny nested MonoBehaviour that oscillates the attached GameObject's
        // localScale around its original value using Mathf.Sin(Time.time).
        // This draws player attention to the "goal zone" without being distracting.
        //
        // WHY A NESTED CLASS?
        // ZonePulse is only used by RaidBootstrap for the extraction zone. Putting it
        // inside the class keeps related code together and avoids polluting the global
        // namespace. Unity allows nested MonoBehaviours (they just can't be serialized
        // in the Inspector, which we don't need here — it's added purely via code).
        //
        // WebGL-SAFE: uses only Update(), Mathf, Time, and Transform — no threading,
        // no System.IO, no native plugins.
        // ═══════════════════════════════════════════════════════════════════════════
        private class ZonePulse : MonoBehaviour
        {
            // Captured on Start so we oscillate around the original scale.
            private Vector3 _baseScale;

            // Pulse parameters: amplitude ~5% of base scale, gentle frequency.
            private const float Amplitude = 0.05f;  // ±5% scale variation
            private const float Frequency = 2f;     // Cycles per second (2 Hz = gentle pulse)

            private void Start()
            {
                // Capture the base scale set by MakeSpriteObject (5, 2.5, 1 for extraction).
                // Null-safe: if transform is somehow null, default to Vector3.one.
                _baseScale = transform != null ? transform.localScale : Vector3.one;
            }

            private void Update()
            {
                // Null-guard: if transform is destroyed, do nothing.
                if (transform == null) return;

                // Oscillate scale: sin(time * frequency) gives -1 to +1, scaled by amplitude.
                // At t=0, sin=0, so scale = base. Over time it pulses ±5%.
                float pulse = 1f + Amplitude * Mathf.Sin(Time.time * Frequency * Mathf.PI * 2f);
                transform.localScale = _baseScale * pulse;
            }
        }
    }
}
