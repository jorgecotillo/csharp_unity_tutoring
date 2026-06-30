using GoblinSiege.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Wires up the three juice effects from spec §9:
    ///   1. Floating gold pop  — spawned in world space when a cache is looted.
    ///   2. Screen shake       — triggered when a gate is breached.
    ///   3. Alarm vignette     — a fullscreen Image overlay whose red alpha tracks the alarm.
    ///
    /// How to set up in scene:
    ///   • Add this component to any persistent GameObject (e.g., the HUD root).
    ///   • Drag the RaidManager, ScreenShake (on your Camera), and GoldPopup prefab into the fields.
    ///   • For the vignette: create a UI Image that covers the whole Canvas, set its texture to a
    ///     radial-gradient (dark edges, transparent centre) and drag it into Vignette Image.
    ///     The script drives its colour alpha — no extra code needed.
    /// </summary>
    public class JuiceManager : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private RaidManager raid;
        [SerializeField] private ScreenShake screenShake;

        [Header("Gold popup")]
        [Tooltip("Prefab with GoldPopup + TextMeshPro (3D) components.")]
        [SerializeField] private GoldPopup goldPopupPrefab;
        [Tooltip("Vertical offset above the cache so the popup starts above it.")]
        [SerializeField] private float popupYOffset = 0.6f;

        [Header("Alarm vignette")]
        [Tooltip("Fullscreen UI Image (Canvas, Screen Space - Overlay). Use a radial-gradient sprite for best results.")]
        [SerializeField] private Image vignetteImage;
        [SerializeField] private Color vignetteColor = new(0.7f, 0f, 0f, 0f);
        [SerializeField] private float maxVignetteAlpha = 0.55f;

        private void OnEnable()
        {
            if (raid == null) return;
            raid.OnCacheLootedAt += HandleCacheLootedAt;
            raid.OnGateBreachedAt += HandleGateBreachedAt;
            if (raid.Alarm != null)
                raid.Alarm.OnAlarmChanged += HandleAlarmChanged;
        }

        private void OnDisable()
        {
            if (raid == null) return;
            raid.OnCacheLootedAt -= HandleCacheLootedAt;
            raid.OnGateBreachedAt -= HandleGateBreachedAt;
            if (raid.Alarm != null)
                raid.Alarm.OnAlarmChanged -= HandleAlarmChanged;
        }

        private void HandleCacheLootedAt(Vector2 worldPos, int gold)
        {
            if (goldPopupPrefab == null) return;
            Vector3 spawnPos = new(worldPos.x, worldPos.y + popupYOffset, 0f);
            GoldPopup popup = Instantiate(goldPopupPrefab, spawnPos, Quaternion.identity);
            popup.Spawn($"+{gold}");
        }

        private void HandleGateBreachedAt(Vector2 _)
        {
            screenShake?.Shake();
        }

        private void HandleAlarmChanged(float alarmPercent)
        {
            if (vignetteImage == null) return;
            Color c = vignetteColor;
            c.a = (alarmPercent / 100f) * maxVignetteAlpha;
            vignetteImage.color = c;
        }
    }
}
