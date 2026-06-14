using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using GoblinSiege.Meta;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Orchestrates one raid (spec sections 3 and 5). Loads the raid JSON, spawns caches,
    /// gates, garrison, and the player's deployable squads, then runs the win/lose checks:
    ///   WIN  -> quota looted AND a squad reaches the extraction zone (bank it).
    ///   LOSE -> all squads destroyed, OR alarm hits Full Sally without the quota banked.
    /// </summary>
    public class RaidManager : MonoBehaviour
    {
        [Header("Raid data")]
        [Tooltip("raid-NN.json placed in Resources or assigned directly.")]
        [SerializeField] private TextAsset raidJson;

        [Header("Systems")]
        [SerializeField] private AlarmSystem alarm;
        [SerializeField] private QuotaSystem quota;
        [SerializeField] private GarrisonSpawner garrison;
        [SerializeField] private ExtractionZone extraction;

        [Header("Prefabs")]
        [SerializeField] private GoblinUnit goblinUnitPrefab;
        [SerializeField] private LootCache cachePrefab;
        [SerializeField] private Gate gatePrefab;

        [Header("Deploy")]
        [SerializeField] private Transform deployPoint;

        [Header("Startup")]
        [Tooltip("If false, BeginRaid must be called manually (e.g. by RaidBootstrap.Inject).")]
        [SerializeField] private bool autoStartOnPlay = true;

        private string _rawJsonOverride;

        /// <summary>Fires when the raid ends. Args: (result, looted, quota, surplus).</summary>
        public event Action<RaidResult, int, int, int> OnRaidEnded;

        public RaidResult Result { get; private set; } = RaidResult.InProgress;
        public AlarmSystem Alarm => alarm;
        public QuotaSystem Quota => quota;
        public IReadOnlyList<Squad> Squads => _squads;

        private readonly List<Squad> _squads = new();
        private readonly List<LootCache> _caches = new();
        private readonly List<Gate> _gates = new();
        private RaidData _data;
        private bool _running;

        private void Start()
        {
            if (autoStartOnPlay) BeginRaid();
        }

        /// <summary>
        /// Runtime dependency injection used by RaidBootstrap (no scene/prefab wiring).
        /// Sets all references plus the raw raid JSON, then starts the raid.
        /// </summary>
        public void Inject(string rawJson, AlarmSystem alarmSys, QuotaSystem quotaSys,
            GarrisonSpawner garrisonSys, ExtractionZone extractionZone,
            GoblinUnit goblinPrefab, LootCache cachePrefabRef, Gate gatePrefabRef, Transform deploy)
        {
            _rawJsonOverride = rawJson;
            alarm = alarmSys;
            quota = quotaSys;
            garrison = garrisonSys;
            extraction = extractionZone;
            goblinUnitPrefab = goblinPrefab;
            cachePrefab = cachePrefabRef;
            gatePrefab = gatePrefabRef;
            deployPoint = deploy;
            autoStartOnPlay = false;
            BeginRaid();
        }

        public void BeginRaid()
        {
            CombatRegistry.Clear();
            _data = ParseRaidData();
            if (_data == null)
            {
                Debug.LogError("[RaidManager] No raid data — cannot start raid.");
                return;
            }

            ConfigureSystems();
            SpawnGates();
            SpawnCaches();
            SpawnGarrison();
            DeploySquads();

            Result = RaidResult.InProgress;
            _running = true;
            alarm.SetRunning(true);
            garrison.SetRunning(true);
        }

        private RaidData ParseRaidData()
        {
            if (!string.IsNullOrEmpty(_rawJsonOverride))
                return JsonUtility.FromJson<RaidData>(_rawJsonOverride);
            if (raidJson == null)
            {
                Debug.LogError("[RaidManager] raidJson TextAsset not assigned.");
                return null;
            }
            return JsonUtility.FromJson<RaidData>(raidJson.text);
        }

        private void ConfigureSystems()
        {
            quota.Configure(_data.quota);
            alarm.Configure(_data.alarmFillPerSecond);

            HumanType[] roster = ParseRoster(_data.garrisonRoster);
            garrison.Configure(alarm, roster, _data.reinforceIntervalByThreshold);

            alarm.OnFullSally += HandleFullSally;
            garrison.OnHumanKilled += HandleHumanKilled;
        }

        private static HumanType[] ParseRoster(string[] names)
        {
            if (names == null || names.Length == 0) return new[] { HumanType.Militia };
            var list = new List<HumanType>();
            foreach (string n in names)
                if (Enum.TryParse(n, true, out HumanType t)) list.Add(t);
            return list.Count > 0 ? list.ToArray() : new[] { HumanType.Militia };
        }

        private void SpawnGates()
        {
            if (_data.gates == null || gatePrefab == null) return;
            foreach (GateSpawn g in _data.gates)
            {
                Vector2 pos = ToVec(g.pos);
                Gate gate = Instantiate(gatePrefab, pos, Quaternion.identity, transform);
                gate.Init(g.hp);
                gate.OnBreached += HandleGateBreached;
                _gates.Add(gate);
            }
        }

        private void SpawnCaches()
        {
            if (_data.caches == null || cachePrefab == null) return;
            foreach (CacheSpawn c in _data.caches)
            {
                if (!Enum.TryParse(c.type, true, out CacheType type)) type = CacheType.Crate;
                Vector2 pos = ToVec(c.pos);
                LootCache cache = Instantiate(cachePrefab, pos, Quaternion.identity, transform);
                cache.Init(type);
                cache.OnLooted += HandleCacheLooted;
                _caches.Add(cache);
            }
        }

        private void SpawnGarrison()
        {
            // Initial static defenders are placed in-scene or spawned by the spawner over time.
            // The spawner handles all reinforcement waves; nothing to pre-spawn here for MVP.
        }

        private void DeploySquads()
        {
            List<GoblinType> types = WarbandState.Instance != null
                ? WarbandState.Instance.DeployableTypes()
                : DefaultBand();

            Vector2 origin = deployPoint != null ? (Vector2)deployPoint.position : Vector2.zero;
            for (int i = 0; i < types.Count; i++)
            {
                var go = new GameObject($"Squad_{i}_{types[i]}");
                go.transform.SetParent(transform);
                Squad squad = go.AddComponent<Squad>();
                Vector2 squadOrigin = origin + new Vector2((i - (types.Count - 1) * 0.5f) * 2f, 0f);
                squad.Build(types[i], Balance.UnitsPerSquad, goblinUnitPrefab, squadOrigin);
                squad.OnDestroyed += HandleSquadDestroyed;
                _squads.Add(squad);
            }
        }

        private static List<GoblinType> DefaultBand() => new()
        {
            GoblinType.Grunt, GoblinType.Grunt, GoblinType.Spearthrower
        };

        private void Update()
        {
            if (!_running) return;
            TickLooting();
            TickBreaching();
            CheckWin();
        }

        private void TickLooting()
        {
            for (int i = 0; i < _caches.Count; i++)
            {
                LootCache cache = _caches[i];
                if (cache == null || cache.Looted) continue;
                cache.Tick(Time.deltaTime, cache.HasGoblinInRange());
            }
        }

        private void TickBreaching()
        {
            for (int i = 0; i < _gates.Count; i++)
            {
                Gate gate = _gates[i];
                if (gate == null || gate.IsBreached) continue;
                gate.TickSapperBreach(Time.deltaTime);
            }
        }

        private void HandleCacheLooted(int gold, float alarmCost)
        {
            quota.AddGold(gold);
            alarm.Add(alarmCost);
        }

        private void HandleGateBreached(Gate _)
        {
            alarm.Add(Balance.AlarmPerGateBreached);
        }

        private void HandleHumanKilled()
        {
            alarm.Add(Balance.AlarmPerHumanKilled);
        }

        private void CheckWin()
        {
            if (Result != RaidResult.InProgress) return;
            if (quota.QuotaMet && extraction != null && extraction.AnyGoblinInside())
                EndRaid(RaidResult.Won);
        }

        private void HandleSquadDestroyed(Squad _)
        {
            if (Result != RaidResult.InProgress) return;
            for (int i = 0; i < _squads.Count; i++)
                if (_squads[i] != null && !_squads[i].IsDestroyed) return;
            EndRaid(RaidResult.LostSquadWipe);
        }

        private void HandleFullSally()
        {
            if (Result != RaidResult.InProgress) return;
            // The garrison sallies in full. If the quota isn't banked, the raid is lost.
            if (!quota.QuotaMet)
                EndRaid(RaidResult.LostAlarmMaxed);
        }

        private void EndRaid(RaidResult result)
        {
            Result = result;
            _running = false;
            alarm.SetRunning(false);
            garrison.SetRunning(false);

            if (WarbandState.Instance != null)
            {
                WarbandState.Instance.ApplyRaidOutcome(SurvivorFlags());
                if (result == RaidResult.Won)
                {
                    WarbandState.Instance.BankSurplus(quota.Surplus);
                    WarbandState.Instance.MarkRaidCleared(_data.id);
                }
            }

            OnRaidEnded?.Invoke(result, quota.Looted, quota.Quota, quota.Surplus);
        }

        private List<bool> SurvivorFlags()
        {
            var flags = new List<bool>(_squads.Count);
            for (int i = 0; i < _squads.Count; i++)
                flags.Add(_squads[i] != null && !_squads[i].IsDestroyed);
            return flags;
        }

        private static Vector2 ToVec(float[] xy)
            => xy is { Length: >= 2 } ? new Vector2(xy[0], xy[1]) : Vector2.zero;

        private void OnDestroy()
        {
            if (alarm != null) alarm.OnFullSally -= HandleFullSally;
            if (garrison != null) garrison.OnHumanKilled -= HandleHumanKilled;
        }
    }
}
