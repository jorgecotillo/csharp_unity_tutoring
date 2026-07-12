using GoblinSiege.Core;
using GoblinSiege.Units;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Background music controller for Goblin Siege.
    /// Plays a driving battle loop and smoothly speeds up its tempo as the fight
    /// heats up — both when the alarm threshold rises AND when more human defenders
    /// are actively attacking (the pitch climbs with the attacker count).
    ///
    /// Alarm floor (a minimum tempo per AlarmThreshold):
    ///   Unaware  → 1.0  (calm patrol groove)
    ///   Alerted  → 1.15 (something's up...)
    ///   Mobilized→ 1.30 (guards coming!)
    ///   FullSally→ 1.50 (PANIC MODE ⚔️)
    /// The attacker count can push the tempo above the floor, up to maxBattlePitch.
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

        [Header("Battle Intensity — speeds up as more humans attack")]
        [Tooltip("Extra pitch/tempo added for each human currently attacking.")]
        [SerializeField] private float pitchPerAttacker = 0.06f;
        [Tooltip("Highest pitch/tempo the battle music can reach.")]
        [SerializeField] private float maxBattlePitch = 1.6f;

        private AudioSource _source;
        private AlarmSystem _alarm;
        private float _targetPitch;
        private float _alarmFloorPitch = 1f;
        private float _pollTimer;
        private int _attackerCount;

        private void Awake()
        {
            _source           = GetComponent<AudioSource>();
            _source.loop      = true;
            _source.volume    = volume;
            _source.pitch     = pitchUnaware;
            _source.playOnAwake = false;
            _targetPitch      = pitchUnaware;
            _alarmFloorPitch  = pitchUnaware;

            // Generate a built-in battle groove if no real clip was dropped in the
            // Inspector. Replace it any time by assigning a .ogg / .wav / .mp3.
            if (musicClip == null)
                musicClip = GenerateBattleLoop();

            _source.clip = musicClip;
            _source.Play();
        }

        /// <summary>
        /// Procedural fallback battle track: a driving war-drum groove with a low
        /// bass pulse and a moody A-minor melody riff (8-beat loop @ 100 BPM).
        /// Speeding up the AudioSource pitch makes the whole groove race — that's how
        /// the music "speeds up" as the humans attack. Replace any time by dropping a
        /// real .ogg / .wav / .mp3 onto the Music Clip field in the Inspector.
        /// </summary>
        private static AudioClip GenerateBattleLoop()
        {
            const int   sampleRate = 22050;
            const float bpm        = 100f;
            const float secPerBeat = 60f / bpm;    // 0.6s per beat
            const int   beats      = 8;
            float loopSeconds      = secPerBeat * beats; // 4.8s
            int total              = Mathf.RoundToInt(sampleRate * loopSeconds);
            var data               = new float[total];

            // A-minor riff, one note per beat (Hz) — ominous, marching feel.
            float[] melody = { 220.00f, 329.63f, 261.63f, 329.63f,
                               293.66f, 349.23f, 261.63f, 329.63f };
            const float bassFreq = 55f; // A1 — loops cleanly (55 * 4.8 = 264 cycles)
            const float PI2 = 2f * Mathf.PI;
            var rng = new System.Random(20240711);

            for (int i = 0; i < total; i++)
            {
                float t     = (float)i / sampleRate;
                int   beat  = (int)(t / secPerBeat) % beats;
                float phase = t - Mathf.Floor(t / secPerBeat) * secPerBeat; // 0..secPerBeat

                // War drum: a punchy low thump on every beat, fast exp decay.
                float drumEnv = Mathf.Exp(-phase * 22f);
                float drum    = Mathf.Sin(PI2 * 60f * phase) * drumEnv * 0.55f;

                // Hi-hat tick on the off-beat (short noise burst).
                float offset = phase - secPerBeat * 0.5f;
                float hat    = 0f;
                if (offset >= 0f)
                    hat = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-offset * 70f) * 0.10f;

                // Bass pulse: continuous A1, amplitude ducks with each beat.
                float bassEnv = 0.6f + 0.4f * Mathf.Exp(-phase * 6f);
                float bass    = Mathf.Sin(PI2 * bassFreq * t) * 0.26f * bassEnv;

                // Melody: per-beat note with soft attack + decay so beats loop cleanly.
                float mAtk = 1f - Mathf.Exp(-phase * 45f);
                float mDec = Mathf.Exp(-phase * 5f);
                float mf   = melody[beat];
                float mel  = (Mathf.Sin(PI2 * mf * phase) + 0.3f * Mathf.Sin(PI2 * 2f * mf * phase))
                             * mAtk * mDec * 0.16f;

                data[i] = drum + hat + bass + mel;
            }

            // Normalise to a comfortable peak so nothing clips.
            float peak = 0.0001f;
            for (int i = 0; i < total; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.85f / peak;
            for (int i = 0; i < total; i++)
                data[i] *= gain;

            var clip = AudioClip.Create("GoblinSiege_BattleLoop", total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
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
            // (Awake already generated and started the fallback drone; this is a safety net).
            if (!_source.isPlaying && _source.clip != null)
                _source.Play();
        }

        private void OnDestroy()
        {
            if (_alarm != null) _alarm.OnThresholdChanged -= HandleThreshold;
        }

        private void HandleThreshold(AlarmThreshold t)
        {
            // The alarm sets a FLOOR for the music's tempo; the attacker count can
            // push it higher (see RecomputeTargetPitch).
            _alarmFloorPitch = t switch
            {
                AlarmThreshold.Alerted    => pitchAlerted,
                AlarmThreshold.Mobilized  => pitchMobilized,
                AlarmThreshold.FullSally  => pitchSally,
                _                         => pitchUnaware,
            };
            RecomputeTargetPitch();
        }

        // Aim the music tempo: base + a nudge per attacking human, never below the
        // alarm floor, capped so it never gets silly-fast.
        private void RecomputeTargetPitch()
        {
            float attackPitch = Mathf.Min(pitchUnaware + _attackerCount * pitchPerAttacker,
                                          maxBattlePitch);
            _targetPitch = Mathf.Max(_alarmFloorPitch, attackPitch);
        }

        private void Update()
        {
            // Poll the battlefield a few times a second (cheap) for how many humans
            // are attacking, then aim the music's tempo accordingly.
            _pollTimer -= Time.deltaTime;
            if (_pollTimer <= 0f)
            {
                _pollTimer = 0.2f;
                _attackerCount = CombatRegistry.CountAttackingHumans();
                RecomputeTargetPitch();
            }

            // Smoothly slide the AudioSource pitch toward the target each frame
            // (frame-rate independent — G5).
            if (Mathf.Abs(_source.pitch - _targetPitch) > 0.001f)
                _source.pitch = Mathf.MoveTowards(_source.pitch, _targetPitch,
                                                   pitchSlideSpeed * Time.deltaTime);

            // A touch louder as the battle intensifies, for extra punch.
            float intensity01 = Mathf.InverseLerp(pitchUnaware, maxBattlePitch, _targetPitch);
            _source.volume = Mathf.Lerp(volume, volume * 1.25f, intensity01);
        }
    }
}
