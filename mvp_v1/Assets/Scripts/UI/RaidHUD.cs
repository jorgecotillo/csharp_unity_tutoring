using GoblinSiege.Core;
using GoblinSiege.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Raid HUD (spec section 7). Surfaces the two numbers that matter:
    ///   - Gold / Quota (the win number)
    ///   - Alarm meter 0-100% with threshold color shift (the pressure number)
    /// Plus the end-of-raid result panel with its reason line.
    /// Canvas + TextMeshPro only — no legacy OnGUI.
    /// </summary>
    public class RaidHUD : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private RaidManager raid;

        [Header("Gold / Quota")]
        [SerializeField] private Slider goldBar;
        [SerializeField] private TMP_Text goldLabel;

        [Header("Alarm")]
        [SerializeField] private Slider alarmBar;
        [SerializeField] private Image alarmFill;
        [SerializeField] private TMP_Text alarmLabel;
        [SerializeField] private Color calmColor = new(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color alertedColor = new(0.95f, 0.75f, 0.2f);
        [SerializeField] private Color mobilizedColor = new(0.95f, 0.4f, 0.15f);
        [SerializeField] private Color sallyColor = new(0.9f, 0.15f, 0.15f);

        [Header("Result panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultReason;
        [SerializeField] private TMP_Text resultStats;

        private void Awake()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (raid == null) return;
            raid.OnRaidEnded += HandleRaidEnded;
            if (raid.Quota != null)
            {
                raid.Quota.OnGoldChanged += HandleGold;
                HandleGold(raid.Quota.Looted, raid.Quota.Quota);
            }
            if (raid.Alarm != null)
            {
                raid.Alarm.OnAlarmChanged += HandleAlarm;
                raid.Alarm.OnThresholdChanged += HandleThreshold;
                HandleAlarm(raid.Alarm.Current);
                HandleThreshold(raid.Alarm.Threshold);
            }
        }

        private void OnDisable()
        {
            if (raid == null) return;
            raid.OnRaidEnded -= HandleRaidEnded;
            if (raid.Quota != null) raid.Quota.OnGoldChanged -= HandleGold;
            if (raid.Alarm != null)
            {
                raid.Alarm.OnAlarmChanged -= HandleAlarm;
                raid.Alarm.OnThresholdChanged -= HandleThreshold;
            }
        }

        private void HandleGold(int looted, int quota)
        {
            if (goldBar != null)
            {
                goldBar.maxValue = quota;
                goldBar.value = Mathf.Min(looted, quota);
            }
            if (goldLabel != null) goldLabel.text = $"Gold {looted} / {quota}";
        }

        private void HandleAlarm(float percent)
        {
            if (alarmBar != null)
            {
                alarmBar.maxValue = 100f;
                alarmBar.value = percent;
            }
            if (alarmLabel != null) alarmLabel.text = $"Alarm {Mathf.RoundToInt(percent)}%";
        }

        private void HandleThreshold(AlarmThreshold t)
        {
            if (alarmFill == null) return;
            alarmFill.color = t switch
            {
                AlarmThreshold.Alerted => alertedColor,
                AlarmThreshold.Mobilized => mobilizedColor,
                AlarmThreshold.FullSally => sallyColor,
                _ => calmColor
            };
        }

        private void HandleRaidEnded(RaidResult result, int looted, int quota, int surplus)
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            // ───────────────────────────────────────────────────────────────
            // CHANGE (T5): Added LostWarlordDown arm for Warlord death defeat.
            // The Warlord falling triggers an immediate defeat — a leaderless
            // warband scatters. This outcome bypasses quota and alarm checks.
            // ───────────────────────────────────────────────────────────────
            (string title, string reason) = result switch
            {
                RaidResult.Won => ("VICTORY",
                    "You took what you needed and lived to spend it."),
                RaidResult.LostSquadWipe => ("DEFEAT",
                    "The warband is broken."),
                RaidResult.LostAlarmMaxed => ("DEFEAT",
                    "You reached for one more chest. The horns never stopped."),
                RaidResult.LostWarlordDown => ("DEFEAT",
                    "The warlord fell. A leaderless warband scatters."),
                _ => ("RAID OVER", "")
            };

            if (resultTitle != null) resultTitle.text = title;
            if (resultReason != null) resultReason.text = reason;
            if (resultStats != null)
                resultStats.text = $"Looted {looted} / {quota}    Surplus banked: {surplus}";
        }
    }
}
