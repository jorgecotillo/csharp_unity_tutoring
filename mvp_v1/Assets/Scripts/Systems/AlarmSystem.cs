using System;
using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// THE HEART OF THE GAME (spec section 1 and 3).
    /// Tracks the Alarm meter (0-100%). Rises from time, kills, looting, and breaching.
    /// Rising alarm escalates the garrison; at 100% the garrison fully sallies (soft deadline).
    /// This system is what turns "loot the bar" into "greed vs. survival" — do not remove it.
    /// </summary>
    public class AlarmSystem : MonoBehaviour
    {
        [Tooltip("Alarm percent added every second the raid runs.")]
        [SerializeField] private float fillPerSecond = 1f;

        [SerializeField] private float current;

        /// <summary>Fires when the alarm value changes. Arg: new percent (0-100).</summary>
        public event Action<float> OnAlarmChanged;

        /// <summary>Fires when the alarm crosses into a new threshold. Arg: new threshold.</summary>
        public event Action<AlarmThreshold> OnThresholdChanged;

        /// <summary>Fires once when the alarm first reaches 100% (Full Sally).</summary>
        public event Action OnFullSally;

        public float Current => current;
        public AlarmThreshold Threshold { get; private set; } = AlarmThreshold.Unaware;
        public bool IsFullSally => Threshold == AlarmThreshold.FullSally;

        private bool _running;
        private bool _sallyFired;

        public void Configure(float alarmFillPerSecond)
        {
            fillPerSecond = alarmFillPerSecond;
            current = 0f;
            Threshold = AlarmThreshold.Unaware;
            _sallyFired = false;
            OnAlarmChanged?.Invoke(current);
            OnThresholdChanged?.Invoke(Threshold);
        }

        public void SetRunning(bool running) => _running = running;

        private void Update()
        {
            if (!_running || _sallyFired) return;
            Add(fillPerSecond * Time.deltaTime);
        }

        /// <summary>Raise the alarm. Used by loot, kills, breaches.</summary>
        public void Add(float amount)
        {
            if (amount == 0f || _sallyFired) return;
            SetCurrent(current + amount);
        }

        /// <summary>Lower the alarm. Used by the Warlord's one-time Warhorn safety valve.</summary>
        public void Reduce(float amount) => SetCurrent(current - Mathf.Abs(amount));

        private void SetCurrent(float value)
        {
            current = Mathf.Clamp(value, 0f, Balance.FullSallyAt);
            OnAlarmChanged?.Invoke(current);

            AlarmThreshold next = Balance.ThresholdFor(current);
            if (next != Threshold)
            {
                Threshold = next;
                OnThresholdChanged?.Invoke(Threshold);

                if (Threshold == AlarmThreshold.FullSally && !_sallyFired)
                {
                    _sallyFired = true;
                    OnFullSally?.Invoke();
                }
            }
        }
    }
}
