using System.Collections;
using GoblinSiege.Player;
using UnityEngine;

namespace GoblinSiege.Systems
{
    /// <summary>
    /// A door the Warlord opens by walking near it (spec: warlord opens doors).
    /// How it works:
    ///   1. A child SphereCollider (trigger) detects the Warlord.
    ///   2. When detected, the blocking BoxCollider is disabled and the door
    ///      swings open with a smooth Slerp animation (like a real hinge).
    ///   3. Once open the door stays open for the rest of the raid.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Tooltip("How far the door swings open (degrees around Y axis).")]
        [SerializeField] private float openAngle = 90f;

        [Tooltip("Time in seconds for the swing animation.")]
        [SerializeField] private float openDuration = 0.5f;

        [Tooltip("How close the Warlord must get to trigger the door (world units).")]
        [SerializeField] private float triggerRadius = 2.0f;

        private bool _isOpen;
        private Collider _blockCollider;

        private void Awake()
        {
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
            if (who.GetComponent<WarlordController>() == null) return;
            StartCoroutine(SwingOpen());
        }

        private IEnumerator SwingOpen()
        {
            _isOpen = true;

            // Disable the blocker immediately so the Warlord isn't stuck mid-swing.
            if (_blockCollider != null) _blockCollider.enabled = false;

            // Smooth swing around the Y axis (hinge at the door's pivot/origin).
            Quaternion startRot = transform.rotation;
            Quaternion endRot   = startRot * Quaternion.Euler(0f, openAngle, 0f);

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / openDuration));
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            transform.rotation = endRot;
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
