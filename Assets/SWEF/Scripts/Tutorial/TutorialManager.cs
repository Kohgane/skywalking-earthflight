using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Tutorial
{
    /// <summary>Step data for the onboarding tutorial overlay.</summary>
    [System.Serializable]
    public struct TutorialStep
    {
        [TextArea(2, 4)]
        public string message;
        /// <summary>Optional: name of a scene GameObject to highlight (future use).</summary>
        public string highlightObjectName;
    }

    /// <summary>
    /// First-run onboarding overlay.
    /// Shown once when "SWEF_TutorialCompleted" PlayerPrefs key is absent or 0.
    /// The player advances through sequential steps with Next, or skips all at once.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        private const string PrefKey = "SWEF_TutorialCompleted";

        [Header("UI")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Text       messageText;
        [SerializeField] private Button     nextButton;
        [SerializeField] private Button     skipButton;

        [Header("Steps")]
        [SerializeField] private TutorialStep[] steps = new TutorialStep[]
        {
            new TutorialStep
            {
                message = "Welcome to Skywalking: Earth Flight! 🚀\nYou're about to fly from your current location to the edge of space.",
                highlightObjectName = ""
            },
            new TutorialStep
            {
                message = "Drag the screen to look around.\nUse the throttle slider to control speed.",
                highlightObjectName = ""
            },
            new TutorialStep
            {
                message = "Use the altitude slider to climb higher.\nWatch the sky change as you ascend!",
                highlightObjectName = ""
            },
            new TutorialStep
            {
                message = "Tap the roll buttons (◀ ▶) to bank left and right.",
                highlightObjectName = ""
            },
            new TutorialStep
            {
                message = "Toggle Comfort Mode for a smoother ride.\nOpen Settings (⚙) to customize your experience.",
                highlightObjectName = ""
            },
            new TutorialStep
            {
                message = "Search for places with Teleport (🔍) and save favorites (⭐).\nEnjoy the flight! ✈️",
                highlightObjectName = ""
            },
        };

        private int _currentStep;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);
        }

        private void Start()
        {
            bool completed = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            if (completed || steps.Length == 0)
            {
                if (tutorialPanel != null) tutorialPanel.SetActive(false);
                return;
            }

            _currentStep = 0;
            ShowStep(_currentStep);
            if (tutorialPanel != null) tutorialPanel.SetActive(true);
        }

        private void ShowStep(int index)
        {
            if (messageText != null && index < steps.Length)
                messageText.text = steps[index].message;
        }

        private void OnNext()
        {
            _currentStep++;
            if (_currentStep >= steps.Length)
            {
                Complete();
                return;
            }
            ShowStep(_currentStep);
        }

        private void OnSkip()
        {
            Complete();
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
        }
    }
}
