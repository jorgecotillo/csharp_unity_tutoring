using System.Collections;
using TMPro;
using UnityEngine;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Floating "+N gold" pop spawned in world space when a cache is looted (spec §9 juice).
    /// Place on a prefab that has a TextMeshPro (3D) component.
    /// The JuiceManager instantiates and calls Spawn(); the popup destroys itself when done.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class GoldPopup : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 1.5f;
        [SerializeField] private float duration = 1f;
        [SerializeField] private Color goldColor = new(1f, 0.85f, 0.2f);

        private TextMeshPro _tmp;

        private void Awake() => _tmp = GetComponent<TextMeshPro>();

        public void Spawn(string text)
        {
            _tmp.text = text;
            _tmp.color = goldColor;
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            Color c = _tmp.color;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position += Vector3.up * riseSpeed * Time.deltaTime;
                c.a = 1f - t;
                _tmp.color = c;
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
