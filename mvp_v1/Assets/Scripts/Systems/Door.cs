using System.Collections;
using GoblinSiege.Player;
using GoblinSiege.Visual;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A door the Warlord opens by walking near it (spec: warlord opens doors).
    /// How it works:
    ///   1. A child SphereCollider (trigger) detects the Warlord.
    ///   2. When detected, the Warlord plays a short "heave the door open" animation
    ///      (see <see cref="WarlordController.BeginDoorOpen"/>), the blocking
    ///      BoxCollider is disabled, and the door swings open with a smooth Slerp
    ///      animation (like a real hinge).
    ///   3. When the door finishes opening it UNLOCKS the fight
    ///      (<see cref="CombatGate.Unlock"/>) — only THEN does the human garrison wake
    ///      up and the battle begin. Until then the humans are dormant (HumanUnit).
    ///   4. Once open the door stays open for the rest of the raid.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Tooltip("How far the door swings open (degrees around Y axis).")]
        [SerializeField] private float openAngle = 90f;

        [Tooltip("Time in seconds for the swing animation.")]
        [SerializeField] private float openDuration = 0.5f;

        [Tooltip("How close the Warlord must get to trigger the door (world units).")]
        [SerializeField] private float triggerRadius = 2.0f;

        [Tooltip("Little wind-up before the door swings, so the Warlord's shove reads first (seconds).")]
        [SerializeField] private float windUp = 0.12f;

        private bool _isOpen;
        private Collider _blockCollider;

        private void Awake()
        {
            // A door in the scene means "combat is gated": lock the fight now so the
            // human garrison stays asleep until the Warlord opens THIS door. Default
            // is unlocked, so scenes without a door are never affected. This runs
            // fresh every raid, so it is correct across repeated play sessions.
            CombatGate.Lock();

            // The BoxCollider on this GameObject is the physical door panel.
            _blockCollider = GetComponent<Collider>();
            if (_blockCollider == null)
            {
                // Provide a default blocking box in case none was added in the editor.
                var box = gameObject.AddComponent<BoxCollider>();
                box.size   = new Vector3(1.0f, 2.0f, 0.2f);
                box.center = new Vector3(0f, 1.0f, 0f);
                _blockCollider = box;
            }

            // Build a child trigger sphere so this collider stays non-trigger
            // (two colliders on the same object fighting over isTrigger is messy).
            var triggerGo   = new GameObject("DoorTrigger");
            triggerGo.transform.SetParent(transform);
            triggerGo.transform.localPosition = Vector3.zero;
            var sphere      = triggerGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius    = triggerRadius;

            // Forward trigger events back to this Door.
            triggerGo.AddComponent<DoorTriggerProxy>().Owner = this;
        }

        /// <summary>Called by DoorTriggerProxy when a collider enters the trigger.</summary>
        internal void OnWarlordNear(GameObject who)
        {
            if (_isOpen) return;
            WarlordController warlord = who.GetComponent<WarlordController>();
            if (warlord == null) return;

            // Ask the Warlord to play its "heave the door open" animation, facing us.
            warlord.BeginDoorOpen(transform.position);

            StartCoroutine(SwingOpen());
        }

        private IEnumerator SwingOpen()
        {
            _isOpen = true;

            // Disable the blocker immediately so the Warlord isn't stuck mid-swing.
            if (_blockCollider != null) _blockCollider.enabled = false;

            // Tiny wind-up so the Warlord's shove is seen to CAUSE the swing.
            if (windUp > 0f) yield return new WaitForSeconds(windUp);

            // Quick partial swing so the shove reads, THEN the door blasts off its
            // hinges (Warren wanted an explosion, not a calm swing).
            Quaternion startRot = transform.rotation;
            Quaternion endRot   = startRot * Quaternion.Euler(0f, openAngle * 0.4f, 0f);

            float elapsed = 0f;
            float shoveTime = openDuration * 0.5f;
            while (elapsed < shoveTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / shoveTime));
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            transform.rotation = endRot;

            // BOOM: blow the door off its hinges with flying debris.
            BlowOpen();

            // The door is open — NOW the fight begins. Wake the human garrison.
            CombatGate.Unlock();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BlowOpen — the barracks door bursts off its hinges (Warren's explosion).
        // ═══════════════════════════════════════════════════════════════════════
        // Flings the door panel up + inward with a spin, sprays a burst of wooden
        // debris chunks, then shrinks and removes the panel so the doorway reads as a
        // clear opening. All cheap primitives, auto-destroyed after ~2s (G3), and
        // frame-rate independent (G5).
        // ═══════════════════════════════════════════════════════════════════════
        private void BlowOpen()
        {
            // BOOM: the door blowing off its hinges (matches the visual blast).
            SfxManager.PlayExplosion();

            Vector3 center = transform.position + Vector3.up * 0.6f;
            Color woodColor = VisualLibrary.GateBrown;

            // Fling the door panel off its hinges (up + inward toward the barracks, +Z).
            var panelBody = gameObject.AddComponent<Rigidbody>();
            panelBody.useGravity = true;
            panelBody.mass = 2f;
            panelBody.AddForce(new Vector3(Random.Range(-2f, 2f), 6.5f, 4.5f),
                               ForceMode.VelocityChange);
            panelBody.AddTorque(new Vector3(Random.Range(-8f, 8f), Random.Range(-8f, 8f), 0f),
                               ForceMode.VelocityChange);

            // Spray wooden shrapnel outward.
            const int debrisCount = 10;
            for (int i = 0; i < debrisCount; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "DoorDebris";
                chunk.transform.position = center + Random.insideUnitSphere * 0.4f;
                float s = Random.Range(0.16f, 0.34f);
                chunk.transform.localScale = new Vector3(s, s, s);

                var rend = chunk.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(rend.sharedMaterial) { color = woodColor };
                    rend.material = mat;
                }

                var body = chunk.AddComponent<Rigidbody>();
                body.useGravity = true;
                Vector3 outDir = (chunk.transform.position - center).normalized + Vector3.up;
                body.AddForce(outDir * Random.Range(3f, 7f), ForceMode.VelocityChange);
                body.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

                Destroy(chunk, 2.2f);
            }

            StartCoroutine(ShrinkAndRemovePanel());
        }

        private IEnumerator ShrinkAndRemovePanel()
        {
            // Let the panel tumble briefly, then shrink it away so the doorway is clear.
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }

    /// <summary>
    /// Thin forwarder placed on the trigger child so Door can keep one clean collider
    /// for blocking and a separate one for detection — no isTrigger fighting.
    /// </summary>
    internal class DoorTriggerProxy : MonoBehaviour
    {
        internal Door Owner;

        private void OnTriggerEnter(Collider other)
        {
            Owner?.OnWarlordNear(other.gameObject);
        }
    }
}
