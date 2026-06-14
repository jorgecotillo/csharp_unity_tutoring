using GoblinSiege.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Title screen (spec section 7): New Campaign / Continue / Quit.
    /// New Campaign resets the persistent war-band; Continue keeps PlayerPrefs progress.
    /// Wire the buttons' OnClick to these public methods in the Inspector.
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [SerializeField] private string raidSceneName = "Raid";
        [SerializeField] private Button continueButton;

        private void Start()
        {
            // Continue is only meaningful if a campaign exists.
            if (continueButton != null && WarbandState.Instance != null)
                continueButton.interactable = WarbandState.Instance.HighestRaidCleared > 0
                    || WarbandState.Instance.AliveSquadCount() > 0;
        }

        public void OnNewCampaign()
        {
            if (WarbandState.Instance != null) WarbandState.Instance.NewCampaign();
            SceneManager.LoadScene(raidSceneName);
        }

        public void OnContinue()
        {
            SceneManager.LoadScene(raidSceneName);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
