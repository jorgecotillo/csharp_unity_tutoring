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
            var extraction = MakeSpriteObject("Extraction", new Vector2(0f, 0f),
                new Vector3(5f, 2.5f, 1f), new Color(0.2f, 0.8f, 0.3f, 0.25f), sorting: -5)
                .AddComponent<ExtractionZone>();

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
            go.transform.localScale = Vector3.one * 0.8f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.2f, 0.9f, 0.5f);
            sr.sortingOrder = 3;

            var pi = go.AddComponent<PlayerInput>();
            pi.actions = _inputAsset;
            pi.defaultActionMap = "Player";
            pi.neverAutoSwitchControlSchemes = true;

            var warlord = go.AddComponent<WarlordController>();
            warlord.SetAlarm(_alarm);

            var commander = go.AddComponent<SquadCommander>();
            commander.Setup(_raid, Camera.main);

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
            };
            _raid.OnRaidEnded += (result, looted, quota, surplus) =>
            {
                _resultText.text = result switch
                {
                    RaidResult.Won => $"VICTORY\nYou took what you needed and lived to spend it.\nLooted {looted} / {quota}  ·  Surplus {surplus}",
                    RaidResult.LostSquadWipe => "DEFEAT\nThe warband is broken.",
                    RaidResult.LostAlarmMaxed => "DEFEAT\nYou reached for one more chest. The horns never stopped.",
                    _ => "RAID OVER"
                };
            };
        }
    }
}
