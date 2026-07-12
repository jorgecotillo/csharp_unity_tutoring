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
        private AudioClip _gameOverClip;

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

        /// <summary>
        /// Play the "game over" defeat jingle — a short, original descending fanfare
        /// that plays when the warlord falls (or any raid defeat). Built in code, so
        /// it needs no audio asset and works in WebGL.
        /// </summary>
        public static void PlayGameOver(float volume = 1f)
        {
            EnsureInstance();
            if (_instance == null) return;
            _instance._source.PlayOneShot(_instance._gameOverClip, Mathf.Clamp01(volume));
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
            _gameOverClip = GenerateGameOver();
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

        // ═══════════════════════════════════════════════════════════════════════
        // GenerateGameOver — an ORIGINAL "defeat" jingle (NOT copied from any game).
        // A slow brass-y fanfare of 5 notes that sag DOWNWARD in pitch (the classic
        // "wah-wah, you lost" shape), each note a soft sawtooth-ish tone with a gentle
        // attack/decay, over a low sustained drone so it feels heavy and final.
        // ═══════════════════════════════════════════════════════════════════════
        private static AudioClip GenerateGameOver()
        {
            const int sampleRate = 22050;
            const float PI2 = 2f * Mathf.PI;

            // Descending minor-ish phrase (Hz): C5 · A4 · F4 · D4 · C4-hold.
            // Original melody — a sinking "the hero has fallen" line.
            float[] notes = { 523.25f, 440.00f, 349.23f, 293.66f, 261.63f };
            float[] durs  = { 0.28f,   0.28f,   0.28f,   0.28f,   0.85f  }; // last note rings

            float totalSeconds = 0f;
            for (int n = 0; n < durs.Length; n++) totalSeconds += durs[n];
            int total = Mathf.RoundToInt(sampleRate * totalSeconds);
            var data = new float[total];

            const float droneFreq = 65.41f; // C2 — low, ominous bed under the melody.

            int cursor = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                int len = Mathf.RoundToInt(sampleRate * durs[n]);
                float freq = notes[n];
                for (int j = 0; j < len && cursor < total; j++, cursor++)
                {
                    float tn = (float)j / sampleRate;         // time into THIS note
                    float tg = (float)cursor / sampleRate;    // time into the whole jingle

                    // Soft attack, slow decay so notes bloom then fade (brass-like).
                    float atk = 1f - Mathf.Exp(-tn * 30f);
                    float dec = Mathf.Exp(-tn * 2.2f);
                    float env = atk * dec;

                    // Tone: fundamental + a couple of harmonics for a warm brass color.
                    float tone = Mathf.Sin(PI2 * freq * tn)
                               + 0.35f * Mathf.Sin(PI2 * 2f * freq * tn)
                               + 0.18f * Mathf.Sin(PI2 * 3f * freq * tn);
                    tone *= env * 0.5f;

                    // Low drone bed, fading out across the whole jingle.
                    float drone = Mathf.Sin(PI2 * droneFreq * tg)
                                  * 0.22f * Mathf.Exp(-tg * 0.5f);

                    data[cursor] = tone + drone;
                }
            }

            // Normalise to a comfortable peak.
            float peak = 0.0001f;
            for (int i = 0; i < total; i++)
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float gain = 0.85f / peak;
            for (int i = 0; i < total; i++)
                data[i] *= gain;

            var clip = AudioClip.Create("GoblinSiege_GameOver", total, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
