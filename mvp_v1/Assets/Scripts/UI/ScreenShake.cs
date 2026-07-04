using System.Collections;
using UnityEngine;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Camera shake (spec §9 juice). Attach to the main Camera.
    /// Call Shake() from JuiceManager when a gate is breached or the vault cracks.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        [SerializeField] private float defaultDuration = 0.25f;
        [SerializeField] private float defaultMagnitude = 0.18f;

        private Coroutine _current;

        /// <summary>Shake with default settings.</summary>
        public void Shake() => Shake(defaultDuration, defaultMagnitude);

        /// <summary>Shake with custom duration (seconds) and magnitude (Unity units).</summary>
        public void Shake(float duration, float magnitude)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(DoShake(duration, magnitude));
        }

        private IEnumerator DoShake(float duration, float magnitude)
        {
            Vector3 origin = transform.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float strength = (1f - elapsed / duration) * magnitude;
                transform.localPosition = origin + (Vector3)(Random.insideUnitCircle * strength);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = origin;
            _current = null;
        }
    }
}
