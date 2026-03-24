// MissionBriefingUI.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Full-screen mission briefing panel.
    ///
    /// <para>Call <see cref="ShowBriefing"/> when <see cref="MissionManager.currentStatus"/>
    /// enters <see cref="MissionStatus.Briefing"/>.  The panel fades in with
    /// <see cref="fadeInCurve"/>, populates all text and image fields from the supplied
    /// <see cref="MissionData"/>, plays optional narration, and reveals the briefing body
    /// text with a typewriter effect.</para>
    /// </summary>
    public class MissionBriefingUI : MonoBehaviour
    {
        #region Inspector

        [Header("Panel")]
        [Tooltip("CanvasGroup controlling the full-screen briefing overlay.")]
        /// <summary>CanvasGroup for the full-screen briefing overlay.</summary>
        public CanvasGroup briefingPanel;

        [Header("Images")]
        [Tooltip("Large banner image shown at the top of the briefing screen.")]
        /// <summary>Large mission banner image at the top of the screen.</summary>
        public Image missionBannerImage;

        [Tooltip("Small mission icon shown beside the mission name.")]
        /// <summary>Small mission icon beside the mission name.</summary>
        public Image missionIconImage;

        [Header("Text Fields")]
        [Tooltip("TextMeshPro label displaying the mission name.")]
        /// <summary>Displays the mission name.</summary>
        public TextMeshProUGUI missionNameText;

        [Tooltip("TextMeshPro label displaying the mission type.")]
        /// <summary>Displays the mission type (e.g. Patrol, Escort).</summary>
        public TextMeshProUGUI missionTypeText;

        [Tooltip("TextMeshPro label displaying the difficulty rating.")]
        /// <summary>Displays the difficulty label.</summary>
        public TextMeshProUGUI difficultyText;

        [Tooltip("TextMeshPro body text for the full briefing description.")]
        /// <summary>Body text that reveals with a typewriter effect.</summary>
        public TextMeshProUGUI briefingBodyText;

        [Tooltip("TextMeshPro label showing the time limit (hidden when 0).")]
        /// <summary>Displays the time limit; hidden when the mission has no limit.</summary>
        public TextMeshProUGUI timeLimitText;

        [Tooltip("TextMeshPro preview showing the base rewards for this mission.")]
        /// <summary>Short reward preview (XP + currency).</summary>
        public TextMeshProUGUI rewardPreviewText;

        [Header("Objective List")]
        [Tooltip("Parent container; one objective item is instantiated per objective.")]
        /// <summary>Parent transform for dynamically spawned objective list items.</summary>
        public Transform objectiveListParent;

        [Tooltip("Prefab spawned for each objective row in the list.")]
        /// <summary>Prefab instantiated for each row in the objective list.</summary>
        public GameObject objectiveItemPrefab;

        [Header("Buttons")]
        [Tooltip("Button that calls MissionManager.StartMission.")]
        /// <summary>Button that starts the mission.</summary>
        public Button startButton;

        [Tooltip("Button that cancels the briefing and returns to the mission selection screen.")]
        /// <summary>Button that cancels the briefing.</summary>
        public Button cancelButton;

        [Header("Audio")]
        [Tooltip("AudioSource used to play briefing narration clips.")]
        /// <summary>AudioSource for briefing narration playback.</summary>
        public AudioSource narrationSource;

        [Header("Animation")]
        [Tooltip("Curve controlling alpha during the briefing panel fade-in animation.")]
        /// <summary>Animation curve applied to the panel alpha during fade-in.</summary>
        public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);

        #endregion

        #region Private State

        private List<GameObject> _spawnedItems = new List<GameObject>();
        private Coroutine _typewriterCoroutine;
        private Coroutine _fadeCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (briefingPanel != null)
            {
                briefingPanel.alpha = 0f;
                briefingPanel.interactable = false;
                briefingPanel.blocksRaycasts = false;
            }

            if (startButton  != null) startButton.onClick.AddListener(OnStartPressed);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelPressed);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Populates and displays the briefing panel for the given <paramref name="mission"/>.
        /// </summary>
        /// <param name="mission">Mission whose data will be displayed.</param>
        public void ShowBriefing(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogError("[MissionBriefingUI] ShowBriefing called with null mission.");
                return;
            }

            PopulateFields(mission);
            BuildObjectiveList(mission);

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeIn());

            PlayNarration(mission.briefingNarration);
        }

        /// <summary>Hides the briefing panel with a fade-out animation and stops narration.</summary>
        public void HideBriefing()
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            if (narrationSource != null && narrationSource.isPlaying) narrationSource.Stop();
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        #endregion

        #region Private — Population

        private void PopulateFields(MissionData mission)
        {
            if (missionBannerImage != null)
            {
                missionBannerImage.sprite = mission.missionBanner;
                missionBannerImage.enabled = mission.missionBanner != null;
            }

            if (missionIconImage != null)
            {
                missionIconImage.sprite = mission.missionIcon;
                missionIconImage.enabled = mission.missionIcon != null;
            }

            if (missionNameText  != null) missionNameText.text  = mission.missionName;
            if (missionTypeText  != null) missionTypeText.text  = mission.type.ToString();
            if (difficultyText   != null) difficultyText.text   = mission.difficulty.ToString();

            if (timeLimitText != null)
            {
                bool hasLimit = mission.timeLimit > 0f;
                timeLimitText.gameObject.SetActive(hasLimit);
                if (hasLimit)
                    timeLimitText.text = $"Time Limit: {FormatTime(mission.timeLimit)}";
            }

            if (rewardPreviewText != null && mission.reward != null)
            {
                rewardPreviewText.text =
                    $"XP: {mission.reward.baseExperience}  |  Credits: {mission.reward.baseCurrency}";
            }

            // Launch typewriter for briefing body
            if (briefingBodyText != null)
            {
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                briefingBodyText.text = string.Empty;
                _typewriterCoroutine = StartCoroutine(TypewriterReveal(mission.briefingText));
            }
        }

        private void BuildObjectiveList(MissionData mission)
        {
            // Clear previous items
            foreach (GameObject item in _spawnedItems)
                if (item != null) Destroy(item);
            _spawnedItems.Clear();

            if (objectiveListParent == null || objectiveItemPrefab == null) return;

            foreach (MissionObjective obj in mission.objectives)
            {
                if (obj.isHidden) continue;

                GameObject item = Instantiate(objectiveItemPrefab, objectiveListParent);
                _spawnedItems.Add(item);

                // Populate item text if it exposes a TextMeshProUGUI on its root or first child
                TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = obj.isOptional ? $"[Optional] {obj.description}" : obj.description;
            }
        }

        #endregion

        #region Private — Typewriter Effect

        private IEnumerator TypewriterReveal(string fullText)
        {
            if (briefingBodyText == null || string.IsNullOrEmpty(fullText)) yield break;

            float delay = 1f / Mathf.Max(1f, MissionConfig.BriefingTypewriterSpeed);
            briefingBodyText.text = string.Empty;

            for (int i = 1; i <= fullText.Length; i++)
            {
                briefingBodyText.text = fullText.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }
        }

        #endregion

        #region Private — Fade Animation

        private IEnumerator FadeIn()
        {
            if (briefingPanel == null) yield break;

            briefingPanel.interactable = true;
            briefingPanel.blocksRaycasts = true;

            float duration = fadeInCurve.keys.Length > 0
                ? fadeInCurve.keys[fadeInCurve.keys.Length - 1].time
                : 0.5f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                briefingPanel.alpha = fadeInCurve.Evaluate(t);
                yield return null;
            }
            briefingPanel.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (briefingPanel == null) yield break;

            float duration = fadeInCurve.keys.Length > 0
                ? fadeInCurve.keys[fadeInCurve.keys.Length - 1].time
                : 0.5f;

            float startAlpha = briefingPanel.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                briefingPanel.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                yield return null;
            }

            briefingPanel.alpha = 0f;
            briefingPanel.interactable = false;
            briefingPanel.blocksRaycasts = false;
        }

        #endregion

        #region Private — Audio

        private void PlayNarration(AudioClip clip)
        {
            if (narrationSource == null || clip == null) return;
            narrationSource.clip = clip;
            narrationSource.Play();
        }

        #endregion

        #region Private — Button Handlers

        private void OnStartPressed()
        {
            HideBriefing();
            if (MissionManager.Instance != null)
                MissionManager.Instance.StartMission();
        }

        private void OnCancelPressed()
        {
            HideBriefing();
            if (MissionManager.Instance != null)
                MissionManager.Instance.AbandonMission();
        }

        #endregion

        #region Private — Helpers

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m:D2}:{s:00}";
        }

        #endregion
    }
}
