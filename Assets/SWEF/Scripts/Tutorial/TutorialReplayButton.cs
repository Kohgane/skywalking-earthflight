using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Settings panel button that allows returning players to replay the
    /// interactive tutorial from the beginning.
    /// Attach to the Settings panel and wire <see cref="replayButton"/> in the Inspector.
    /// </summary>
    public class TutorialReplayButton : MonoBehaviour
    {
        [SerializeField] private Button replayButton;

        private void Awake()
        {
            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);
        }

        private void OnDestroy()
        {
            if (replayButton != null)
                replayButton.onClick.RemoveListener(OnReplayClicked);
        }

        /// <summary>Finds the <see cref="InteractiveTutorialManager"/> and restarts the tutorial.</summary>
        private void OnReplayClicked()
        {
            InteractiveTutorialManager manager = FindFirstObjectByType<InteractiveTutorialManager>();
            if (manager != null)
                manager.RestartTutorial();
            else
                Debug.LogWarning("[SWEF] TutorialReplayButton: No InteractiveTutorialManager found in scene.");
        }
    }
}
