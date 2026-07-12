using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using GoblinSiege.Meta;
using GoblinSiege.Units;
using GoblinSiege.Visual;
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

        // 3D MIGRATION (§2): no more sprite "prefab" templates. Caches, gates and
        // goblins are spawned through VisualLibrary keys, so dropping real art needs
        // no code change. Only the deploy point and systems remain as references.

        [Header("Deploy")]
        [SerializeField] private Transform deployPoint;

        [Header("Startup")]
        [Tooltip("If false, BeginRaid must be called manually (e.g. by RaidBootstrap.Inject).")]
        [SerializeField] private bool autoStartOnPlay = true;

        private string _rawJsonOverride;

        /// <summary>Fires when the raid ends. Args: (result, looted, quota, surplus).</summary>
        public event Action<RaidResult, int, int, int> OnRaidEnded;

        /// <summary>Fires when a cache is fully looted. Args: (worldPosition, goldGained).</summary>
        public event Action<Vector2, int> OnCacheLootedAt;

        /// <summary>Fires when a gate is breached. Arg: worldPosition of the gate.</summary>
        public event Action<Vector2> OnGateBreachedAt;

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
        /// 3D MIGRATION: unit/cache/gate visuals now come from VisualLibrary, so no
        /// prefab references are passed in any more.
        /// </summary>
        public void Inject(string rawJson, AlarmSystem alarmSys, QuotaSystem quotaSys,
            GarrisonSpawner garrisonSys, ExtractionZone extractionZone, Transform deploy)
        {
            _rawJsonOverride = rawJson;
            alarm = alarmSys;
            quota = quotaSys;
            garrison = garrisonSys;
            extraction = extractionZone;
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
            if (_data.gates == null) return;
            foreach (GateSpawn g in _data.gates)
            {
                Vector3 pos = ToVec(g.pos);
                // ART SEAM: gate visual via VisualLibrary("Gate"); gameplay attached here.
                GameObject go = VisualLibrary.Spawn(VisualLibrary.KeyGate, pos, Quaternion.identity, transform);
                Gate gate = go.GetComponent<Gate>() ?? go.AddComponent<Gate>();
                gate.Init(g.hp);
                gate.OnBreached += HandleGateBreached;
                _gates.Add(gate);
            }
        }

        private void SpawnCaches()
        {
            if (_data.caches == null) return;
            foreach (CacheSpawn c in _data.caches)
            {
                if (!Enum.TryParse(c.type, true, out CacheType type)) type = CacheType.Crate;
                Vector3 pos = ToVec(c.pos);
                // ART SEAM: per-type cache visual via VisualLibrary("Cache_<Type>").
                GameObject go = VisualLibrary.Spawn(CacheKey(type), pos, Quaternion.identity, transform);
                LootCache cache = go.GetComponent<LootCache>() ?? go.AddComponent<LootCache>();
                cache.Init(type);
                cache.OnLooted += HandleCacheLooted;
                cache.OnLooted += (gold, _) => OnCacheLootedAt?.Invoke(cache.transform.position, gold);
                _caches.Add(cache);
            }
        }

        /// <summary>Maps a cache type to its stable VisualLibrary key.</summary>
        private static string CacheKey(CacheType type) => type switch
        {
            CacheType.Crate => VisualLibrary.KeyCacheCrate,
            CacheType.Chest => VisualLibrary.KeyCacheChest,
            CacheType.Granary => VisualLibrary.KeyCacheGranary,
            CacheType.Vault => VisualLibrary.KeyCacheVault,
            _ => VisualLibrary.KeyCacheCrate
        };

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

            Vector3 origin = deployPoint != null ? deployPoint.position : Vector3.zero;
            for (int i = 0; i < types.Count; i++)
            {
                var go = new GameObject($"Squad_{i}_{types[i]}");
                go.transform.SetParent(transform);
                Squad squad = go.AddComponent<Squad>();
                // Spread squads along X (the flat plane's left-right axis).
                Vector3 squadOrigin = origin + new Vector3((i - (types.Count - 1) * 0.5f) * 2f, 0f, 0f);
                squad.Build(types[i], Balance.UnitsPerSquad, squadOrigin);
                squad.OnDestroyed += HandleSquadDestroyed;
                _squads.Add(squad);
            }
        }

        private static List<GoblinType> DefaultBand() => new()
        {
            GoblinType.Grunt, GoblinType.Grunt, GoblinType.Spearthrower, GoblinType.Sapper
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

        private void HandleGateBreached(Gate gate)
        {
            alarm.Add(Balance.AlarmPerGateBreached);
            OnGateBreachedAt?.Invoke(gate.transform.position);
            // Blowing the gate open alerts the whole village: wake the garrison NOW so
            // the humans stop ignoring the player (no more "I'm invisible" until the
            // interior barracks door). The Warlord's door is still a valid trigger too.
            CombatGate.Unlock();
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

        // ═══════════════════════════════════════════════════════════════════
        // NotifyWarlordDown — NEW (T5): Warlord death = defeat
        // ═══════════════════════════════════════════════════════════════════
        // Called by WarlordUnit.Die() when the Warlord proxy's HP reaches zero.
        // A leaderless warband scatters — instant defeat regardless of quota.
        //
        // WHY PUBLIC?
        // WarlordUnit calls this directly from its Die() override. The coupling
        // is acceptable: this is a game-critical event, not a generic callback.
        // Making it public keeps the "Warlord death = defeat" logic explicit
        // and localized rather than scattered across event subscriptions.
        //
        // GUARD: Only acts if the raid is still InProgress. If the raid has
        // already ended (e.g., Won via extraction), this is a no-op.
        // ═══════════════════════════════════════════════════════════════════
        /// <summary>Called when the Warlord proxy dies. Ends the raid as a defeat if still in progress.</summary>
        public void NotifyWarlordDown()
        {
            if (Result != RaidResult.InProgress) return;
            EndRaid(RaidResult.LostWarlordDown);
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

        // 2D→3D coordinate mapping (§0): JSON [x, y] becomes world (x, 0, y), so the
        // old 2D-Y axis is the new world-Z (north/south on the flat board).
        private static Vector3 ToVec(float[] xy)
            => xy is { Length: >= 2 } ? new Vector3(xy[0], 0f, xy[1]) : Vector3.zero;

        private void OnDestroy()
        {
            if (alarm != null) alarm.OnFullSally -= HandleFullSally;
            if (garrison != null) garrison.OnHumanKilled -= HandleHumanKilled;
        }
    }
}
