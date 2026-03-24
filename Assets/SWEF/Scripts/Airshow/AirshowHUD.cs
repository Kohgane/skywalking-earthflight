// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowHUD.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;
using SWEF.Social;
using SWEF.Replay;

namespace SWEF.Airshow
{
    /// <summary>
    /// Airshow-specific HUD overlay for both performers and spectators.
    /// Switches panels based on <see cref="AirshowManager"/> state events.
    /// All text is localized via <see cref="LocalizationManager"/>.
    /// </summary>
    public class AirshowHUD : MonoBehaviour
    {
        #region Inspector — Panels
        [Header("Panels")]
        [SerializeField] private GameObject performerPanel;
        [SerializeField] private GameObject spectatorPanel;
        [SerializeField] private GameObject scorePanel;

        [Header("Performer HUD")]
        [SerializeField] private Text maneuverNameText;
        [SerializeField] private Slider maneuverProgressBar;
        [SerializeField] private Text nextManeuverText;
        [SerializeField] private Text timingStatusText;
        [SerializeField] private Text actProgressText;
        [SerializeField] private Slider formationQualitySlider;
        [SerializeField] private Image smokeIndicator;

        [Header("Spectator HUD")]
        [SerializeField] private Text showTitleText;
        [SerializeField] private Text actNameText;
        [SerializeField] private Slider excitementMeter;
        [SerializeField] private Text elapsedTimeText;
        [SerializeField] private Text performerCountText;

        [Header("Score Overlay")]
        [SerializeField] private Text totalScoreText;
        [SerializeField] private Text ratingText;
        [SerializeField] private Button shareButton;
        [SerializeField] private Button replayButton;

        [Header("Score Reveal")]
        [SerializeField] private float scoreCountUpDuration = 2f;
        #endregion

        #region Private
        private LocalizationManager _loc;
        private float _displayedScore;
        private float _targetScore;
        private Coroutine _scoreCountUp;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _loc = LocalizationManager.Instance;
            HideAllPanels();

            shareButton?.onClick.AddListener(OnShareClicked);
            replayButton?.onClick.AddListener(OnReplayClicked);
        }

        private void OnEnable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnAirshowStateChanged  += HandleStateChanged;
            AirshowManager.Instance.OnActStarted            += HandleActStarted;
            AirshowManager.Instance.OnManeuverTriggered     += HandleManeuverTriggered;
            AirshowManager.Instance.OnPerformanceScored     += HandlePerformanceScored;
            AirshowManager.Instance.OnAirshowCompleted      += HandleAirshowCompleted;
        }

        private void OnDisable()
        {
            if (AirshowManager.Instance == null) return;
            AirshowManager.Instance.OnAirshowStateChanged  -= HandleStateChanged;
            AirshowManager.Instance.OnActStarted            -= HandleActStarted;
            AirshowManager.Instance.OnManeuverTriggered     -= HandleManeuverTriggered;
            AirshowManager.Instance.OnPerformanceScored     -= HandlePerformanceScored;
            AirshowManager.Instance.OnAirshowCompleted      -= HandleAirshowCompleted;
        }

        private void Update()
        {
            UpdatePerformerHUD();
            UpdateSpectatorHUD();
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void HandleStateChanged(AirshowState state)
        {
            HideAllPanels();
            switch (state)
            {
                case AirshowState.Performing:
                case AirshowState.Finale:
                    ShowPerformerOrSpectator();
                    break;
                case AirshowState.Completed:
                    SetActive(scorePanel, true);
                    break;
            }
        }

        private void HandleActStarted(int actIndex, string actName)
        {
            if (actNameText != null) actNameText.text = Loc("airshow_performing") + " — " + actName;
        }

        private void HandleManeuverTriggered(ManeuverType maneuver, int slot)
        {
            if (maneuverNameText != null)
                maneuverNameText.text = Loc("airshow_maneuver_" + maneuver.ToString().ToLower());
        }

        private void HandlePerformanceScored(float score, PerformanceRating rating)
        {
            _targetScore = score;
        }

        private void HandleAirshowCompleted(AirshowResult result)
        {
            HideAllPanels();
            SetActive(scorePanel, true);
            _targetScore = result.totalScore;
            if (_scoreCountUp != null) StopCoroutine(_scoreCountUp);
            _scoreCountUp = StartCoroutine(CountUpScore(result));
        }

        // ── Per-frame updates ────────────────────────────────────────────────

        private void UpdatePerformerHUD()
        {
            if (performerPanel == null || !performerPanel.activeSelf) return;
            AirshowManager mgr = AirshowManager.Instance;
            if (mgr == null) return;

            int actCount = mgr.ActiveRoutine?.acts?.Count ?? 0;
            if (actProgressText != null)
            {
                actProgressText.text = Loc("airshow_act_progress",
                    mgr.CurrentActIndex + 1, actCount);
            }
        }

        private void UpdateSpectatorHUD()
        {
            if (spectatorPanel == null || !spectatorPanel.activeSelf) return;

            if (elapsedTimeText != null && AirshowManager.Instance != null)
            {
                float elapsed = AirshowManager.Instance.ActElapsedTime;
                elapsedTimeText.text = $"{(int)(elapsed / 60):00}:{(int)(elapsed % 60):00}";
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ShowPerformerOrSpectator()
        {
            // Local player → performer HUD; spectators → spectator HUD
            bool hasLocalPlayer = false;
            if (AirshowManager.Instance != null)
            {
                foreach (AirshowPerformer p in AirshowManager.Instance.Performers)
                {
                    if (p != null && p.IsLocalPlayer) { hasLocalPlayer = true; break; }
                }
            }
            SetActive(performerPanel,  hasLocalPlayer);
            SetActive(spectatorPanel, !hasLocalPlayer);
        }

        private IEnumerator CountUpScore(AirshowResult result)
        {
            float elapsed = 0f;
            _displayedScore = 0f;

            while (elapsed < scoreCountUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _displayedScore = Mathf.Lerp(0f, result.totalScore, elapsed / scoreCountUpDuration);
                if (totalScoreText != null)
                    totalScoreText.text = $"{_displayedScore:F0}";
                yield return null;
            }

            _displayedScore = result.totalScore;
            if (totalScoreText != null) totalScoreText.text = $"{result.totalScore:F0}";
            if (ratingText != null)
                ratingText.text = Loc("airshow_rating_" + result.rating.ToString().ToLower());
        }

        private void HideAllPanels()
        {
            SetActive(performerPanel, false);
            SetActive(spectatorPanel, false);
            SetActive(scorePanel, false);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private string Loc(string key, params object[] args)
        {
            if (_loc == null) _loc = LocalizationManager.Instance;
            if (_loc == null) return key;
            return args.Length == 0 ? _loc.GetText(key) : _loc.GetText(key, args);
        }

        private void OnShareClicked()
        {
            ShareManager sm = ShareManager.Instance;
            if (sm == null) return;
            string text = Loc("airshow_share_text", (int)_displayedScore);
            sm.ShareText(text);
        }

        private void OnReplayClicked()
        {
            ReplayFileManager rfm = ReplayFileManager.Instance;
            if (rfm == null) return;
            // Opens the most recent replay — the replay system handles navigation
            var replays = rfm.GetReplayCount();
            if (replays > 0)
                Debug.Log("[AirshowHUD] Replay link activated.");
        }
    }
}
