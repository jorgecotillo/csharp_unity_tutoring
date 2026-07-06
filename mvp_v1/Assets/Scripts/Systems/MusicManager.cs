using GoblinSiege.Core;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Background music controller for Goblin Siege.
    /// Plays a looping track and smoothly speeds up the pitch whenever the alarm
    /// threshold rises (enemies show up / situation gets more dangerous).
    ///
    /// Pitch ladder (matches AlarmThreshold):
    ///   Unaware  → 1.0  (calm patrol music)
    ///   Alerted  → 1.15 (something's up...)
    ///   Mobilized→ 1.30 (guards coming!)
    ///   FullSally→ 1.50 (PANIC MODE ⚔️)
    ///
    /// Usage: attach to any scene GameObject; call Configure(alarmSystem) from
    /// RaidBootstrap after the AlarmSystem is ready. Drop an AudioClip into
    /// musicClip in the Inspector (or leave blank — the manager still wires up).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Looping background music clip. Leave blank if you'll assign it in code.")]
        [SerializeField] private AudioClip musicClip;

        [Tooltip("Master volume (0-1).")]
        [SerializeField] private float volume = 0.45f;

        [Header("Pitch per Alarm Threshold")]
        [SerializeField] private float pitchUnaware   = 1.00f;
        [SerializeField] private float pitchAlerted   = 1.15f;
        [SerializeField] private float pitchMobilized = 1.30f;
        [SerializeField] private float pitchSally     = 1.50f;

        [Tooltip("How quickly pitch slides to the new target (units per second).")]
        [SerializeField] private float pitchSlideSpeed = 0.8f;

        private AudioSource _source;
        private AlarmSystem _alarm;
        private float _targetPitch;

        private void Awake()
        {
            _source           = GetComponent<AudioSource>();
            _source.loop      = true;
            _source.volume    = volume;
            _source.pitch     = pitchUnaware;
            _source.playOnAwake = false;
            _targetPitch      = pitchUnaware;

            if (musicClip != null)
            {
                _source.clip = musicClip;
                _source.Play();
            }
        }

        /// <summary>
        /// Wire up the AlarmSystem so the music reacts to escalation events.
        /// Call this from RaidBootstrap right after AlarmSystem.Configure().
        /// </summary>
        public void Configure(AlarmSystem alarm)
        {
            _alarm = alarm;
            _alarm.OnThresholdChanged += HandleThreshold;

            // If music wasn't assigned in the Inspector, still start playing
            // (silence is better than a crash, and a real clip can be added later).
            if (!_source.isPlaying && _source.clip != null)
                _source.Play();
        }

        private void OnDestroy()
        {
            if (_alarm != null) _alarm.OnThresholdChanged -= HandleThreshold;
        }

        private void HandleThreshold(AlarmThreshold t)
        {
            _targetPitch = t switch
            {
                AlarmThreshold.Alerted    => pitchAlerted,
                AlarmThreshold.Mobilized  => pitchMobilized,
                AlarmThreshold.FullSally  => pitchSally,
                _                         => pitchUnaware,
            };
        }

        private void Update()
        {
            // Smoothly slide the AudioSource pitch toward the target each frame.
            // MoveTowards is frame-rate independent (multiplied by Time.deltaTime).
            if (Mathf.Abs(_source.pitch - _targetPitch) > 0.001f)
                _source.pitch = Mathf.MoveTowards(_source.pitch, _targetPitch,
                                                   pitchSlideSpeed * Time.deltaTime);
        }
    }
}
