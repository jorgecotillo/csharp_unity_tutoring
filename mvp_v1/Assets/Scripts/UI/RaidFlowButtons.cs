using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoblinSiege.UI
{
    /// <summary>
    /// Result-panel buttons (spec section 7): Replay Raid and Back to War-Camp.
    /// Wire each button's OnClick to the matching public method.
    /// </summary>
    public class RaidFlowButtons : MonoBehaviour
    {
        [SerializeField] private string raidSceneName = "Raid";
        [SerializeField] private string warCampSceneName = "WarCamp";

        public void OnReplayRaid() => SceneManager.LoadScene(raidSceneName);

        public void OnBackToWarCamp() => SceneManager.LoadScene(warCampSceneName);
    }
}
