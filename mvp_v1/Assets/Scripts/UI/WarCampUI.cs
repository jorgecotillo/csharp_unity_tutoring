using GoblinSiege.Core;
using GoblinSiege.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoblinSiege.UI
{
    /// <summary>
    /// War-Camp between raids (spec section 5): spend SURPLUS gold to recruit and revive
    /// squads up to the cap, then march on the next raid. Buttons wire to the public methods.
    /// </summary>
    public class WarCampUI : MonoBehaviour
    {
        [SerializeField] private string raidSceneName = "Raid";
        [SerializeField] private TMP_Text surplusLabel;
        [SerializeField] private TMP_Text squadsLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        [SerializeField] private Button recruitGruntButton;
        [SerializeField] private Button recruitSpearButton;
        [SerializeField] private Button recruitSapperButton;
        [SerializeField] private Button reviveButton;

        private void Start() => Refresh();

        public void OnRecruitGrunt() => Recruit(GoblinType.Grunt);
        public void OnRecruitSpear() => Recruit(GoblinType.Spearthrower);
        public void OnRecruitSapper() => Recruit(GoblinType.Sapper);

        private void Recruit(GoblinType type)
        {
            var wb = WarbandState.Instance;
            if (wb == null) return;
            if (wb.TryRecruitSquad(type))
                SetFeedback($"Recruited a {type} squad.");
            else if (wb.AliveSquadCount() >= wb.SquadCap)
                SetFeedback("Squad cap reached.");
            else
                SetFeedback($"Need {Balance.RecruitSquadCost}g surplus.");
            Refresh();
        }

        public void OnRevive()
        {
            var wb = WarbandState.Instance;
            if (wb == null) return;
            if (wb.TryReviveSquad())
                SetFeedback("Revived a fallen squad.");
            else
                SetFeedback($"Need {Balance.ReviveSquadCost}g surplus and a fallen squad.");
            Refresh();
        }

        public void OnMarch()
        {
            SceneManager.LoadScene(raidSceneName);
        }

        private void Refresh()
        {
            var wb = WarbandState.Instance;
            if (wb == null) return;
            if (surplusLabel != null) surplusLabel.text = $"Surplus gold: {wb.SurplusGold}";
            if (squadsLabel != null) squadsLabel.text = $"Squads: {wb.AliveSquadCount()} / {wb.SquadCap}";

            if (recruitGruntButton != null)
                recruitGruntButton.interactable = wb.SurplusGold >= Balance.RecruitSquadCost && wb.AliveSquadCount() < wb.SquadCap;
            if (recruitSpearButton != null)
                recruitSpearButton.interactable = wb.SurplusGold >= Balance.RecruitSquadCost && wb.AliveSquadCount() < wb.SquadCap;
            if (recruitSapperButton != null)
                recruitSapperButton.interactable = wb.SurplusGold >= Balance.RecruitSquadCost && wb.AliveSquadCount() < wb.SquadCap;
            if (reviveButton != null)
                reviveButton.interactable = wb.SurplusGold >= Balance.ReviveSquadCost;
        }

        private void SetFeedback(string msg)
        {
            if (feedbackLabel != null) feedbackLabel.text = msg;
        }
    }
}
