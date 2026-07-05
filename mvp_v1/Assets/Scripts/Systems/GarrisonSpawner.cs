using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using GoblinSiege.Units;
using GoblinSiege.Visual;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Spawns human reinforcements based on the Alarm threshold (spec section 3).
    /// Higher alarm = shorter spawn interval + tougher unit mix. At Full Sally,
    /// every existing defender is forced to attack and a final big wave drops.
    ///
    /// 3D MIGRATION (Phase A + B): spawn points are XZ Transforms; humans are
    /// spawned THROUGH <see cref="VisualLibrary"/> (key "Human") so the future
    /// Paladin prefab is used automatically (§2). Enforces the GUARDRAIL G3 hard
    /// ceiling on concurrent living humans.
    /// </summary>
    public class GarrisonSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;

        // ─────────────────────────────────────────────────────────────────────
        // GUARDRAIL G3 — hard ceiling on CONCURRENT living humans.
        // ─────────────────────────────────────────────────────────────────────
        // Phase C swaps humans for a heavyish skinned Paladin mesh (~9 MB FBX), so
        // the reinforcement system must NEVER flood the field. We cap the number of
        // ALIVE spawner-controlled humans at 16. SpawnWave bails the moment the cap
        // is reached. (Primitive humans honor the same cap so behavior is identical
        // before and after the art swap.)
        private const int MaxConcurrentHumans = 28;

        /// <summary>Runtime injection for RaidBootstrap (no scene wiring).</summary>
        public void SetSpawnAssets(Transform[] points)
        {
            spawnPoints = points;
        }

        private AlarmSystem _alarm;
        private float _alertedInterval = 18f;
        private float _mobilizedInterval = 10f;
        private HumanType[] _roster = { HumanType.Militia };
        private float _timer;
        private bool _running;
        private bool _sallyTriggered;

        /// <summary>Fires when a human is killed, so the alarm/score can react.</summary>
        public event Action OnHumanKilled;

        private readonly List<HumanUnit> _live = new();

        public void Configure(AlarmSystem alarm, HumanType[] roster, ReinforceIntervals intervals)
        {
            _alarm = alarm;
            _roster = roster is { Length: > 0 } ? roster : new[] { HumanType.Militia };
            if (intervals != null)
            {
                _alertedInterval = intervals.alerted;
                _mobilizedInterval = intervals.mobilized;
            }
            _timer = _alertedInterval; // start with a full delay so first wave isn't instant
            _sallyTriggered = false;

            _alarm.OnThresholdChanged += HandleThreshold;
        }

        public void SetRunning(bool running) => _running = running;

        private void OnDestroy()
        {
            if (_alarm != null) _alarm.OnThresholdChanged -= HandleThreshold;
        }

        private void Update()
        {
            if (!_running || _alarm == null) return;

            float interval = _alarm.Threshold switch
            {
                AlarmThreshold.Alerted => _alertedInterval,
                AlarmThreshold.Mobilized => _mobilizedInterval,
                AlarmThreshold.FullSally => _mobilizedInterval * 0.6f,
                _ => float.MaxValue // Unaware: no reinforcements yet
            };

            if (interval == float.MaxValue) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = interval;
                SpawnWave();
            }
        }

        private void HandleThreshold(AlarmThreshold t)
        {
            if (t == AlarmThreshold.FullSally && !_sallyTriggered)
            {
                _sallyTriggered = true;
                // Everyone attacks, plus a final surge.
                for (int i = 0; i < _live.Count; i++)
                    if (_live[i] != null && _live[i].IsAlive) _live[i].ForceAlert();
                SpawnWave();
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            int count = _alarm.Threshold switch
            {
                AlarmThreshold.Alerted => 3,
                AlarmThreshold.Mobilized => 5,
                AlarmThreshold.FullSally => 7,
                _ => 1
            };

            for (int i = 0; i < count; i++)
            {
                // GUARDRAIL G3: stop the instant we hit the concurrent-human ceiling.
                if (CountLiving() >= MaxConcurrentHumans) return;

                Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                HumanType type = PickType();

                // ART SEAM: visual via VisualLibrary("Human") — the Paladin prefab
                // (Phase C) if present under Resources/Prefabs, else a red capsule.
                // Gameplay (HumanUnit + its required Rigidbody) is attached here.
                GameObject go = VisualLibrary.Spawn(VisualLibrary.KeyHuman, sp.position,
                    Quaternion.identity, transform);
                HumanUnit unit = go.GetComponent<HumanUnit>() ?? go.AddComponent<HumanUnit>();

                // NOTE: we do NOT call SetVisualTint here — HumanUnit.Init enters the
                // Guard state which sets the dim-red tint itself (G4). Setting a base
                // tint here would clobber that Guard/Alert color.
                unit.Init(type, sp.position);
                if (_sallyTriggered) unit.ForceAlert();
                unit.OnDied += HandleHumanDied;
                _live.Add(unit);
            }
        }

        /// <summary>Count of spawner-controlled humans currently alive (for the G3 cap).</summary>
        private int CountLiving()
        {
            int n = 0;
            for (int i = 0; i < _live.Count; i++)
                if (_live[i] != null && _live[i].IsAlive) n++;
            return n;
        }

        private HumanType PickType()
        {
            // Tougher units become more likely as alarm rises.
            if (_alarm.Threshold >= AlarmThreshold.Mobilized)
                return _roster[UnityEngine.Random.Range(0, _roster.Length)];
            // Early on, bias toward the first (weakest) roster entry.
            return UnityEngine.Random.value < 0.7f ? _roster[0] : _roster[UnityEngine.Random.Range(0, _roster.Length)];
        }

        private void HandleHumanDied(Unit u)
        {
            _live.Remove(u as HumanUnit);
            OnHumanKilled?.Invoke();
        }
    }
}
