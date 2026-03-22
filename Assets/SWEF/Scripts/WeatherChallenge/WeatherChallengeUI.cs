using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.WeatherChallenge
{
    /// <summary>
    /// Phase 53 — UI controller for the Weather Challenges &amp; Dynamic Route system.
    /// Manages three main panels: the challenge browser, the active-challenge HUD,
    /// and the post-challenge results panel.
    /// Attach to a persistent canvas root in the scene.
    /// </summary>
    public class WeatherChallengeUI : MonoBehaviour
    {
        #region Serialized Fields — Panels

        /// <summary>Root panel that lists available challenges grouped by weather type.</summary>
        [SerializeField] private GameObject challengeBrowserPanel;

        /// <summary>HUD overlay displayed while a challenge is active (timer, score, waypoints).</summary>
        [SerializeField] private GameObject activeChallengeHUDPanel;

        /// <summary>End-of-challenge summary panel (score, time, waypoints hit, bonus).</summary>
        [SerializeField] private GameObject resultsPanel;

        /// <summary>Challenge detail panel showing full info before starting.</summary>
        [SerializeField] private GameObject detailPanel;

        #endregion

        #region Serialized Fields — Browser

        /// <summary>Scroll-view content parent that holds dynamically spawned challenge list items.</summary>
        [SerializeField] private Transform challengeListContent;

        /// <summary>Prefab for a single row in the challenge browser list.</summary>
        [SerializeField] private GameObject challengeListItemPrefab;

        #endregion

        #region Serialized Fields — Detail

        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailDescriptionText;
        [SerializeField] private Text detailDifficultyText;
        [SerializeField] private Text detailWeatherTypeText;
        [SerializeField] private Text detailWaypointCountText;
        [SerializeField] private Text detailTimeLimitText;
        [SerializeField] private Text detailBonusObjectiveText;

        #endregion

        #region Serialized Fields — HUD

        [SerializeField] private Text hudTimerText;
        [SerializeField] private Text hudScoreText;
        [SerializeField] private Text hudWaypointsRemainingText;
        [SerializeField] private Text hudNextWaypointText;
        [SerializeField] private Text hudWeatherTypeText;

        #endregion

        #region Serialized Fields — Results

        [SerializeField] private Text resultsTitleText;
        [SerializeField] private Text resultsScoreText;
        [SerializeField] private Text resultsTimeText;
        [SerializeField] private Text resultsWaypointsText;
        [SerializeField] private Text resultsBonusText;
        [SerializeField] private Text resultsOutcomeText;

        #endregion

        #region Private State

        private string _selectedChallengeId;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (WeatherChallengeManager.Instance != null)
            {
                WeatherChallengeManager.Instance.OnChallengeStarted     += HandleChallengeStarted;
                WeatherChallengeManager.Instance.OnChallengeCompleted   += HandleChallengeCompleted;
                WeatherChallengeManager.Instance.OnChallengeFailed      += HandleChallengeFailed;
                WeatherChallengeManager.Instance.OnWaypointReached      += HandleWaypointReached;
                WeatherChallengeManager.Instance.OnChallengeGenerated   += HandleChallengeGenerated;
            }
        }

        private void OnDisable()
        {
            if (WeatherChallengeManager.Instance != null)
            {
                WeatherChallengeManager.Instance.OnChallengeStarted     -= HandleChallengeStarted;
                WeatherChallengeManager.Instance.OnChallengeCompleted   -= HandleChallengeCompleted;
                WeatherChallengeManager.Instance.OnChallengeFailed      -= HandleChallengeFailed;
                WeatherChallengeManager.Instance.OnWaypointReached      -= HandleWaypointReached;
                WeatherChallengeManager.Instance.OnChallengeGenerated   -= HandleChallengeGenerated;
            }
        }

        private void Update()
        {
            if (activeChallengeHUDPanel != null && activeChallengeHUDPanel.activeSelf)
                UpdateHUD();
        }

        #endregion

        #region Public Panel Methods

        /// <summary>
        /// Shows the challenge browser panel, hiding all other panels, and populates
        /// the list with currently available challenges grouped by weather type.
        /// </summary>
        public void ShowChallengeBrowser()
        {
            SetPanelActive(challengeBrowserPanel, true);
            SetPanelActive(activeChallengeHUDPanel, false);
            SetPanelActive(resultsPanel, false);
            SetPanelActive(detailPanel, false);
            PopulateBrowserList();
        }

        /// <summary>
        /// Shows the detail panel for the specified challenge, displaying full metadata
        /// before the player chooses to start it.
        /// </summary>
        /// <param name="challengeId">The <see cref="WeatherChallenge.challengeId"/> to display.</param>
        public void ShowChallengeDetail(string challengeId)
        {
            if (WeatherChallengeManager.Instance == null) return;

            WeatherChallenge challenge = null;
            foreach (WeatherChallenge c in WeatherChallengeManager.Instance.allChallenges)
            {
                if (c.challengeId == challengeId) { challenge = c; break; }
            }
            if (challenge == null) return;

            _selectedChallengeId = challengeId;

            SetTextSafe(detailTitleText,          challenge.title);
            SetTextSafe(detailDescriptionText,    challenge.description);
            SetTextSafe(detailDifficultyText,     challenge.difficulty.ToString());
            SetTextSafe(detailWeatherTypeText,    challenge.weatherType.ToString());
            SetTextSafe(detailWaypointCountText,  challenge.waypoints.Count.ToString());
            SetTextSafe(detailTimeLimitText,      FormatTime(challenge.timeLimit));
            SetTextSafe(detailBonusObjectiveText, string.IsNullOrEmpty(challenge.bonusObjective)
                                                       ? "—" : challenge.bonusObjective);

            SetPanelActive(detailPanel, true);
            SetPanelActive(challengeBrowserPanel, false);
        }

        /// <summary>
        /// Activates and populates the active-challenge HUD overlay with live data.
        /// </summary>
        /// <param name="challenge">The challenge currently being played.</param>
        public void ShowActiveHUD(WeatherChallenge challenge)
        {
            if (challenge == null) return;
            SetPanelActive(activeChallengeHUDPanel, true);
            SetPanelActive(challengeBrowserPanel, false);
            SetPanelActive(resultsPanel, false);
            SetPanelActive(detailPanel, false);

            SetTextSafe(hudWeatherTypeText, challenge.weatherType.ToString());
            UpdateHUD();
        }

        /// <summary>Hides the active-challenge HUD panel.</summary>
        public void HideActiveHUD()
        {
            SetPanelActive(activeChallengeHUDPanel, false);
        }

        /// <summary>
        /// Shows the end-of-challenge results panel with the final score,
        /// elapsed time, waypoints reached, and bonus status.
        /// </summary>
        /// <param name="challenge">The challenge that has just ended.</param>
        public void ShowResults(WeatherChallenge challenge)
        {
            if (challenge == null) return;

            SetPanelActive(resultsPanel, true);
            SetPanelActive(activeChallengeHUDPanel, false);

            string outcome = challenge.status == ChallengeStatus.Completed ? "COMPLETED!" : "FAILED";
            SetTextSafe(resultsOutcomeText,  outcome);
            SetTextSafe(resultsTitleText,    challenge.title);
            SetTextSafe(resultsScoreText,    $"Score: {challenge.currentScore} / {challenge.maxScore + challenge.bonusScore}");
            SetTextSafe(resultsTimeText,     $"Time: {FormatTime(challenge.elapsedTime)}");

            int reached = 0;
            foreach (RouteWaypoint wp in challenge.waypoints) if (wp.isReached) reached++;
            SetTextSafe(resultsWaypointsText, $"Waypoints: {reached} / {challenge.waypoints.Count}");
            SetTextSafe(resultsBonusText,
                challenge.bonusCompleted ? $"Bonus: +{challenge.bonusScore} ✓" : "Bonus: not completed");
        }

        #endregion

        #region Public Button Handlers

        /// <summary>
        /// Called when the player taps Start on the challenge detail panel.
        /// Delegates to <see cref="WeatherChallengeManager.StartChallenge"/>.
        /// </summary>
        /// <param name="challengeId">The challenge to start.</param>
        public void OnStartChallengeClicked(string challengeId)
        {
            if (WeatherChallengeManager.Instance == null) return;
            WeatherChallengeManager.Instance.StartChallenge(challengeId);
        }

        /// <summary>
        /// Called when the player requests a freshly generated challenge.
        /// Generates via <see cref="WeatherChallengeManager.GenerateChallenge"/> then refreshes the browser.
        /// </summary>
        /// <param name="type">Weather type for the new challenge.</param>
        /// <param name="difficulty">Difficulty tier for the new challenge.</param>
        public void OnGenerateNewClicked(ChallengeWeatherType type, ChallengeDifficulty difficulty)
        {
            if (WeatherChallengeManager.Instance == null) return;
            WeatherChallengeManager.Instance.GenerateChallenge(type, difficulty);
        }

        /// <summary>Rebuilds all UI panels to reflect the latest state.</summary>
        public void RefreshUI()
        {
            if (challengeBrowserPanel != null && challengeBrowserPanel.activeSelf)
                PopulateBrowserList();
        }

        #endregion

        #region HUD Update

        /// <summary>
        /// Refreshes the HUD every frame while a challenge is active.
        /// Updates timer, score, waypoints remaining, and next waypoint name.
        /// </summary>
        public void UpdateHUD()
        {
            if (WeatherChallengeManager.Instance == null) return;
            WeatherChallenge c = WeatherChallengeManager.Instance.activeChallenge;
            if (c == null) return;

            SetTextSafe(hudTimerText, FormatTime(c.TimeRemaining()));
            SetTextSafe(hudScoreText, $"{c.currentScore}");

            int remaining = 0;
            RouteWaypoint next = null;
            foreach (RouteWaypoint wp in c.waypoints)
            {
                if (!wp.isReached && !wp.isOptional)
                {
                    remaining++;
                    if (next == null) next = wp;
                }
            }
            SetTextSafe(hudWaypointsRemainingText, $"WP: {remaining}");
            SetTextSafe(hudNextWaypointText, next != null ? $"→ {next.waypointName}" : "All done!");
        }

        #endregion

        #region Private Helpers

        private void PopulateBrowserList()
        {
            if (challengeListContent == null) return;

            // Clear existing items
            for (int i = challengeListContent.childCount - 1; i >= 0; i--)
                Destroy(challengeListContent.GetChild(i).gameObject);

            if (WeatherChallengeManager.Instance == null) return;

            List<WeatherChallenge> available = WeatherChallengeManager.Instance.GetAvailableChallenges();
            foreach (WeatherChallenge c in available)
            {
                if (challengeListItemPrefab == null) continue;
                GameObject item = Instantiate(challengeListItemPrefab, challengeListContent);

                // Populate name label if prefab has a Text child
                Text label = item.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"[{c.difficulty}] {c.weatherType} — {c.title}";

                // Wire up button click
                Button btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    string id = c.challengeId;
                    btn.onClick.AddListener(() => ShowChallengeDetail(id));
                }
            }
        }

        private void HandleChallengeStarted(WeatherChallenge c)     => ShowActiveHUD(c);
        private void HandleChallengeCompleted(WeatherChallenge c)   => ShowResults(c);
        private void HandleChallengeFailed(WeatherChallenge c)      => ShowResults(c);
        private void HandleWaypointReached(RouteWaypoint wp)        => UpdateHUD();
        private void HandleChallengeGenerated(WeatherChallenge c)   => RefreshUI();

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private static void SetTextSafe(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return $"{m:D2}:{s:D2}";
        }

        #endregion
    }
}
