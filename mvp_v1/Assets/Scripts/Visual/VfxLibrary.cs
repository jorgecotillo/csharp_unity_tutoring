using UnityEngine;

namespace GoblinSiege.Visual
{
    // ═══════════════════════════════════════════════════════════════════════════
    // VfxLibrary — ONE-CALL special effects, with a drop-in seam for real VFX art.
    // ═══════════════════════════════════════════════════════════════════════════
    // Mirrors VisualLibrary's art-swap philosophy, but for particle EFFECTS:
    //
    //   VfxLibrary.Play("Slash_Red", pos, rot);
    //
    //   1) ART PATH — first tries Resources.Load<GameObject>("VFX/<key>"). So the
    //      moment you drop a prefab named <key>.prefab into
    //      Assets/Resources/VFX/, that real effect plays with ZERO code changes.
    //      >>> This is exactly where the "Magic Effects FREE" pack plugs in. <<<
    //      Import the pack, then copy (or make a prefab of) the effect you want and
    //      put it at Resources/VFX/<key>.prefab using these key names:
    //         • Slash_Red  → the red melee slash for the Warlord's attack
    //         • Explosion  → the explosion for when a Sapper blows the wall
    //         • Smoke      → the smoke for the same wall breach
    //      (See Assets/Resources/VFX/README_DropVFXHere.txt.)
    //
    //   2) FALLBACK PATH — until those prefabs exist, a code-built ParticleSystem
    //      plays so the game ALWAYS has the effect (WebGL-safe, no art required).
    //
    // Every spawned effect auto-destroys after its lifetime so nothing piles up (G3).
    // WebGL-SAFE: only Resources.Load, ParticleSystem, Material, Shader.Find.
    // ═══════════════════════════════════════════════════════════════════════════
    public static class VfxLibrary
    {
        // Stable VFX keys — the swap contract (a typo here is a compile error).
        public const string KeySlashRed  = "Slash_Red";
        public const string KeyExplosion = "Explosion";
        public const string KeySmoke     = "Smoke";

        private static Material _sharedParticleMat;

        /// <summary>
        /// Plays the effect for <paramref name="key"/> at a world pose. Uses a real
        /// prefab from Resources/VFX/&lt;key&gt; if present, else a code-built fallback.
        /// The spawned object auto-destroys; returns it in case the caller wants it.
        /// </summary>
        public static GameObject Play(string key, Vector3 pos, Quaternion rot, float scale = 1f)
        {
            // 1) ART PATH — a real VFX prefab (e.g. from Magic Effects FREE).
            GameObject prefab = Resources.Load<GameObject>($"VFX/{key}");
            if (prefab != null)
            {
                GameObject fx = Object.Instantiate(prefab, pos, rot);
                fx.name = key;
                if (!Mathf.Approximately(scale, 1f))
                    fx.transform.localScale *= scale;
                Object.Destroy(fx, LifetimeOf(fx));
                return fx;
            }

            // 2) FALLBACK PATH — code-built particle burst so the effect always plays.
            return BuildFallback(key, pos, rot, scale);
        }

        // Longest-living particle system on the prefab (plus a margin), so we don't
        // destroy a real effect before it finishes. Defaults to 3s if none found.
        private static float LifetimeOf(GameObject go)
        {
            float longest = 0f;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                float life = main.duration + main.startLifetime.constantMax;
                if (life > longest) longest = life;
            }
            return longest > 0f ? longest + 0.5f : 3f;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Code-built fallbacks (used only until the real prefabs are dropped in).
        // ─────────────────────────────────────────────────────────────────────────
        private static GameObject BuildFallback(string key, Vector3 pos, Quaternion rot, float scale)
        {
            var go = new GameObject($"VFX_{key}_fallback");
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * scale;

            switch (key)
            {
                case KeyExplosion: BuildExplosion(go); break;
                case KeySmoke:     BuildSmoke(go);     break;
                case KeySlashRed:
                default:           BuildRedSlash(go);  break;
            }
            return go;
        }

        // A fast RED fan of particles that fires forward along the object's +Z — a
        // clear "sword slash" pop for the Warlord's attack.
        private static void BuildRedSlash(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = 0.28f;
            main.startSpeed = 9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = new Color(1f, 0.12f, 0.12f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 34) });

            // Narrow flat fan so it reads as a horizontal SLASH along facing (+Z).
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(0f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(new Color(1f, 0.2f, 0.2f), new Color(0.6f, 0f, 0f));

            ApplyParticleMaterial(ps);
            ps.Play();
        }

        // A billowing gray SMOKE puff that rises, expands, and fades — for the breach.
        private static void BuildSmoke(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.gravityModifier = -0.05f; // drift gently upward
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, GrowCurve());

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient(new Color(0.55f, 0.55f, 0.55f), new Color(0.35f, 0.35f, 0.35f));

            ApplyParticleMaterial(ps);
            ps.Play();
        }

        // A bright orange/yellow EXPLOSION burst that blasts outward fast — pairs with
        // the gate's flying debris when a Sapper breaches the wall.
        private static void BuildExplosion(GameObject go)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startColor = new Color(1f, 0.6f, 0.1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            // White-hot core → orange → dark smoke as it dies.
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f),
                    new GradientColorKey(new Color(0.3f, 0.15f, 0.1f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            col.color = grad;

            ApplyParticleMaterial(ps);
            ps.Play();
        }

        // ── shared helpers ───────────────────────────────────────────────────────
        private static Gradient FadeGradient(Color from, Color to)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static AnimationCurve GrowCurve()
        {
            return new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1.6f));
        }

        private static void ApplyParticleMaterial(ParticleSystem ps)
        {
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null) rend.material = GetSharedParticleMaterial();
        }

        private static Material GetSharedParticleMaterial()
        {
            if (_sharedParticleMat != null) return _sharedParticleMat;

            // A soft additive-ish particle shader, resolving across pipelines.
            Shader sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            _sharedParticleMat = new Material(sh);
            return _sharedParticleMat;
        }
    }
}
