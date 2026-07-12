using System;
using System.Collections;
using GoblinSiege.Core;
using GoblinSiege.Units;
using GoblinSiege.Visual;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A breachable gate/wall segment. Goblins damage it by attacking; Sappers breach it
    /// far faster. When destroyed it opens the path and spikes the alarm (spec section 3).
    /// </summary>
    public class Gate : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float hp = 100f;
        [SerializeField] private float breachRadius = 1.5f;

        /// <summary>Fires while damaged. Args: (this gate, hp fraction 0..1).</summary>
        public event Action<Gate, float> OnDamaged;

        /// <summary>Fires once when breached.</summary>
        public event Action<Gate> OnBreached;

        public bool IsAlive => hp > 0f;
        public bool IsBreached => hp <= 0f;

        public void Init(float hitPoints)
        {
            maxHp = Mathf.Max(1f, hitPoints);
            hp = maxHp;
        }

        /// <summary>
        /// Sapper-driven breaching: applies steady breach damage while a sapper is in range.
        /// Returns true the moment the gate breaks.
        /// </summary>
        public bool TickSapperBreach(float deltaTime)
        {
            if (IsBreached) return false;
            GoblinUnit nearest = FindSapperInRange();
            if (nearest == null) return false;

            // Whole-bar breach over Balance.GateBreachSeconds.
            float dps = maxHp / Balance.GateBreachSeconds;
            return TakeDamage(dps * deltaTime);
        }

        public bool TakeDamage(float amount)
        {
            if (IsBreached) return false;
            hp -= amount;
            if (hp <= 0f)
            {
                hp = 0f;
                // Gate is breached — disable its collider so the player can pass through.
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                OnDamaged?.Invoke(this, 0f);
                OnBreached?.Invoke(this);
                // BOOM: blast the gate apart with flying debris (Warren's explosion).
                BlowUp();
                return true;
            }
            OnDamaged?.Invoke(this, hp / maxHp);
            return false;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BlowUp — the breach explosion (Warren asked for the gate to "fly away").
        // ═══════════════════════════════════════════════════════════════════════
        // When a Sapper breaches the gate we:
        //   1. Fling the gate PANEL itself: give it a Rigidbody and launch it up +
        //      backward with a spin, so the whole gate visibly blasts off its hinges.
        //   2. Spawn a handful of small debris chunks that scatter outward with
        //      gravity (a cheap "shrapnel" burst — no particle system needed).
        //   3. Clean everything up after a couple of seconds so the scene stays tidy
        //      and performant (G3). Frame-rate independent (G5).
        // All primitives share the gate's brown look so the blast reads as the gate
        // coming apart, not random cubes.
        // ═══════════════════════════════════════════════════════════════════════
        private void BlowUp()
        {
            // BOOM: the breach explosion sound (matches the visual blast).
            SfxManager.PlayExplosion();

            Vector3 center = transform.position + Vector3.up * 0.6f;
            Color woodColor = VisualLibrary.GateBrown;

            // 1) Launch the gate panel off its hinges.
            var panelBody = gameObject.AddComponent<Rigidbody>();
            panelBody.useGravity = true;
            panelBody.mass = 2f;
            // Up + toward the raiders (−Z, the south the goblins attack from) + a shove.
            panelBody.AddForce(new Vector3(UnityEngine.Random.Range(-2f, 2f), 7f, -5f),
                               ForceMode.VelocityChange);
            panelBody.AddTorque(new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f,
                                            UnityEngine.Random.Range(-8f, 8f)),
                               ForceMode.VelocityChange);

            // 2) Scatter shrapnel chunks.
            const int debrisCount = 10;
            for (int i = 0; i < debrisCount; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "GateDebris";
                chunk.transform.position = center + UnityEngine.Random.insideUnitSphere * 0.4f;
                float s = UnityEngine.Random.Range(0.18f, 0.38f);
                chunk.transform.localScale = new Vector3(s, s, s);

                var rend = chunk.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(rend.sharedMaterial) { color = woodColor };
                    rend.material = mat;
                }

                var body = chunk.AddComponent<Rigidbody>();
                body.useGravity = true;
                // Burst outward + up so pieces arc away from the gate.
                Vector3 outDir = (chunk.transform.position - center).normalized + Vector3.up;
                body.AddForce(outDir * UnityEngine.Random.Range(3f, 7f), ForceMode.VelocityChange);
                body.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

                Destroy(chunk, 2.2f); // tidy up (G3)
            }

            // 3) Remove the flung panel shortly after so the doorway reads as "open".
            StartCoroutine(FadeAndRemovePanel());
        }

        private IEnumerator FadeAndRemovePanel()
        {
            // Let the panel tumble for a moment, then shrink it away and destroy it so
            // the breach reads as a clear opening the goblins can pour through.
            yield return new WaitForSeconds(1.2f);

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            const float shrink = 0.5f;
            while (elapsed < shrink)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shrink);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            Destroy(gameObject);
        }

        private GoblinUnit FindSapperInRange()
        {
            Unit nearest = CombatRegistry.FindNearestGoblin(transform.position);
            if (nearest is not GoblinUnit g || !g.IsSapper) return null;
            // XZ-plane breach range (G2).
            float sqr = CombatRegistry.FlatSqr(g.transform.position, transform.position);
            return sqr <= breachRadius * breachRadius ? g : null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, breachRadius);
        }
    }
}
