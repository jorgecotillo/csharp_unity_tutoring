using System;
using System.Collections.Generic;
using GoblinSiege.Core;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Spawns human reinforcements based on the Alarm threshold (spec section 3).
    /// Higher alarm = shorter spawn interval + tougher unit mix. At Full Sally,
    /// every existing defender is forced to attack and a final big wave drops.
    /// </summary>
    public class GarrisonSpawner : MonoBehaviour
    {
        [SerializeField] private HumanUnit humanPrefab;
        [SerializeField] private Transform[] spawnPoints;

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
            _timer = 0f;
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
            if (humanPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

            int count = _alarm.Threshold switch
            {
                AlarmThreshold.Alerted => 2,
                AlarmThreshold.Mobilized => 3,
                AlarmThreshold.FullSally => 4,
                _ => 1
            };

            for (int i = 0; i < count; i++)
            {
                Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                HumanType type = PickType();
                HumanUnit unit = Instantiate(humanPrefab, sp.position, Quaternion.identity, transform);
                unit.Init(type, sp.position);
                if (_sallyTriggered) unit.ForceAlert();
                unit.OnDied += HandleHumanDied;
                _live.Add(unit);
            }
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
