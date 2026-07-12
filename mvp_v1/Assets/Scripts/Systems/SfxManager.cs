using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// Tiny sound-effects player for one-shot raid SFX (the breach/door BOOM).
    ///
    /// Like <see cref="MusicManager"/>, the clip is SYNTHESIZED IN CODE — no .wav/.ogg
    /// asset is required, so the sound works in the one-click demo and in WebGL with
    /// zero scene wiring. The first call to <see cref="PlayExplosion"/> lazily builds a
    /// persistent GameObject with an AudioSource and generates the clip ONCE (G3: no
    /// per-shot allocations), then reuses it forever via PlayOneShot.
    ///
    /// WEBGL-SAFE: only AudioClip.Create/SetData, AudioSource.PlayOneShot, Mathf and a
    /// seeded System.Random — no threads, no file I/O.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        private static SfxManager _instance;

        private AudioSource _source;
        private AudioClip _explosionClip;

        /// <summary>
        /// Play the breach/door explosion BOOM. Safe to call from anywhere; the
        /// manager and clip are created on first use. <paramref name="volume"/> lets
        /// callers duck a distant boom if they ever want to (defaults to full).
        /// </summary>
        public static void PlayExplosion(float volume = 1f)
        {
            EnsureInstance();
            if (_instance == null) return;
            _instance._source.PlayOneShot(_instance._explosionClip, Mathf.Clamp01(volume));
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var go = new GameObject("SfxManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SfxManager>();
        }

        private void Awake()
        {
            // Guard against a duplicate ever being added in a scene.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D — the boom is a global "event" sound.

            _explosionClip = GenerateExplosion();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GenerateExplosion — a punchy ~0.75s BOOM built from three layers:
        //   1. A sharp noise "crack" at the very start (the detonation snap).
        //   2. A low sine BODY that sweeps down (~110Hz → ~40Hz) — the deep boom.
        //   3. A rumbling noise tail shaped by a fast-then-slow exponential decay.
        // Soft-clipped with tanh so it sounds beefy without harsh digital clipping.
        // ═══════════════════════════════════════════════════════════════════════
        private static AudioClip GenerateExplosion()
        {
            const int sampleRate = 22050;
            const float seconds = 0.75f;
            int total = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[total];

            const float PI2 = 2f * Mathf.PI;
            var rng = new System.Random(1337);

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;      // seconds into the boom
                float norm = t / seconds;             // 0..1 progress

                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

                // 1) Crack: a very fast noise transient at the start.
                float crackEnv = Mathf.Exp(-t * 60f);
                float crack = noise * crackEnv * 0.7f;

                // 2) Body: sine that drops in pitch as it decays (that "whump").
                float freq = Mathf.Lerp(110f, 40f, norm);
                float bodyEnv = Mathf.Exp(-t * 7f);
                float body = Mathf.Sin(PI2 * freq * t) * bodyEnv * 0.9f;

                // 3) Rumble: low-passed-ish noise tail (blend with previous sample
                //    for a rounder, less hissy rumble), medium decay.
                float rumbleEnv = Mathf.Exp(-t * 4.5f);
                float rumble = noise * rumbleEnv * 0.35f;

                float mix = crack + body + rumble;

                // Soft clip so big transients round off instead of harshly clipping.
                data[i] = (float)System.Math.Tanh(mix * 1.4f);
            }

            // Normalise to a comfortable peak.
            float peak = 0.0001f;
            for (int i = 0; i < total; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.9f / peak;
            for (int i = 0; i < total; i++)
                data[i] *= gain;

            var clip = AudioClip.Create("GoblinSiege_Boom", total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
