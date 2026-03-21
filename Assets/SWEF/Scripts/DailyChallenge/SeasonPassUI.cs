using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Full-screen Season Pass / Battle Pass view.
    /// Displays the season header, a horizontal scrollable tier track (free + premium rows),
    /// current progress, an optional premium upsell panel, and a reward preview popup.
    /// </summary>
    public class SeasonPassUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Root")]
        [Tooltip("Root panel — shown/hidden by Open/Close.")]
        [SerializeField] private GameObject _rootPanel;

        [Header("Season Header")]
        [SerializeField] private Text  _seasonNameText;
        [SerializeField] private Text  _timeRemainingText;
        [SerializeField] private Image _themeImage;

        [Header("Tier Track")]
        [Tooltip("ScrollRect that contains the tier nodes.")]
        [SerializeField] private ScrollRect _tierScrollRect;

        [Tooltip("Container inside the scroll view that holds tier node GameObjects.")]
        [SerializeField] private Transform _tierTrackContainer;

        [Tooltip("Prefab for a single tier node in the track.")]
        [SerializeField] private GameObject _tierNodePrefab;

        [Header("Progress")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text   _progressText;

        [Header("Premium")]
        [Tooltip("Panel shown when the premium track is not yet unlocked.")]
        [SerializeField] private GameObject _premiumUpsellPanel;
        [SerializeField] private Button     _unlockPremiumButton;

        [Header("Reward Preview")]
        [SerializeField] private GameObject _rewardPreviewPanel;
        [SerializeField] private Text       _rewardPreviewText;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<TierNodeWidget> _nodes = new List<TierNodeWidget>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (_rootPanel != null) _rootPanel.SetActive(false);
            if (_unlockPremiumButton != null)
                _unlockPremiumButton.onClick.AddListener(OnUnlockPremiumClicked);
            SubscribeEvents();
        }

        private void OnDestroy() => UnsubscribeEvents();

        private void Update()
        {
            // Update time-remaining ticker.
            if (_rootPanel != null && _rootPanel.activeSelf)
                UpdateTimeRemaining();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Opens the Season Pass full-screen UI and refreshes all content.</summary>
        public void Open()
        {
            if (_rootPanel != null) _rootPanel.SetActive(true);
            RefreshAll();
        }

        /// <summary>Closes the Season Pass UI.</summary>
        public void Close()
        {
            if (_rootPanel != null) _rootPanel.SetActive(false);
        }

        /// <summary>Scrolls the tier track so the player's current tier is centred.</summary>
        public void ScrollToCurrentTier()
        {
            var spm = SeasonPassManager.Instance;
            if (spm == null || _tierScrollRect == null || _tierTrackContainer == null) return;

            int currentTier = spm.GetCurrentTier();
            if (currentTier <= 0 || _nodes.Count == 0) return;

            float normalised = Mathf.Clamp01((float)(currentTier - 1) / Mathf.Max(1, _nodes.Count - 1));
            _tierScrollRect.horizontalNormalizedPosition = normalised;
        }

        /// <summary>Refreshes all UI sections from current manager state.</summary>
        public void RefreshAll()
        {
            RefreshHeader();
            BuildTierTrack();
            RefreshProgress();
            RefreshPremiumPanel();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void RefreshHeader()
        {
            var spm = SeasonPassManager.Instance;
            var season = spm?.GetActiveSeason();
            if (season == null) return;

            if (_seasonNameText != null)
                _seasonNameText.text = season.seasonNameKey; // Localization key
            if (_themeImage != null)
            {
                _themeImage.color = season.themeColor;
                if (season.themeIcon != null)
                    _themeImage.sprite = season.themeIcon;
            }
        }

        private void UpdateTimeRemaining()
        {
            var spm = SeasonPassManager.Instance;
            if (spm == null || _timeRemainingText == null) return;
            var ts = spm.GetTimeRemaining();
            _timeRemainingText.text = $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m";
        }

        private void BuildTierTrack()
        {
            if (_tierTrackContainer == null || _tierNodePrefab == null) return;
            foreach (Transform child in _tierTrackContainer) Destroy(child.gameObject);
            _nodes.Clear();

            var spm = SeasonPassManager.Instance;
            var season = spm?.GetActiveSeason();
            if (season == null || spm == null) return;

            int currentTier = spm.GetCurrentTier();

            for (int tier = 1; tier <= season.totalTiers; tier++)
            {
                var go = Instantiate(_tierNodePrefab, _tierTrackContainer);
                var widget = go.GetComponent<TierNodeWidget>();
                if (widget == null) widget = go.AddComponent<TierNodeWidget>();

                bool isCurrent = tier == currentTier;
                bool isPast    = tier < currentTier;

                widget.Bind(tier, isPast, isCurrent, spm.IsPremiumUnlocked, OnTierTapped);
                _nodes.Add(widget);
            }
            ScrollToCurrentTier();
        }

        private void RefreshProgress()
        {
            var spm = SeasonPassManager.Instance;
            if (spm == null) return;

            if (_progressBar != null)
                _progressBar.value = spm.GetProgressToNextTier01();

            if (_progressText != null)
                _progressText.text = $"{spm.GetPointsToNextTier()} pts to next tier";
        }

        private void RefreshPremiumPanel()
        {
            var spm = SeasonPassManager.Instance;
            if (_premiumUpsellPanel != null)
                _premiumUpsellPanel.SetActive(spm != null && !spm.IsPremiumUnlocked);
        }

        private void OnTierTapped(int tier)
        {
            ShowRewardPreview(tier);
        }

        private void ShowRewardPreview(int tier)
        {
            if (_rewardPreviewPanel == null || _rewardPreviewText == null) return;
            _rewardPreviewPanel.SetActive(true);
            _rewardPreviewText.text = $"Tier {tier} rewards"; // Full implementation would look up rewards
        }

        private void OnUnlockPremiumClicked()
        {
            SeasonPassManager.Instance?.UnlockPremium();
            RefreshAll();
        }

        private void SubscribeEvents()
        {
            var spm = SeasonPassManager.Instance;
            if (spm == null) return;
            spm.OnTierAdvanced       += _ => RefreshAll();
            spm.OnSeasonPointsGained += _ => RefreshProgress();
        }

        private void UnsubscribeEvents()
        {
            // Events are unsubscribed automatically when the manager is destroyed.
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helper component — one per tier node
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Widget for a single tier node in the season pass track.
    /// Shows tier number, state (locked/available/claimed), and triggers tap callback.
    /// </summary>
    public class TierNodeWidget : MonoBehaviour
    {
        [SerializeField] private Text   _tierLabel;
        [SerializeField] private Image  _nodeImage;
        [SerializeField] private Button _button;

        private System.Action<int> _onTapped;
        private int _tier;

        /// <summary>Binds this node to a tier and configures its appearance.</summary>
        public void Bind(int tier, bool isPast, bool isCurrent, bool premiumUnlocked, System.Action<int> onTapped)
        {
            _tier    = tier;
            _onTapped = onTapped;

            if (_tierLabel != null) _tierLabel.text = tier.ToString();
            if (_nodeImage != null)
            {
                if (isCurrent)       _nodeImage.color = Color.yellow;
                else if (isPast)     _nodeImage.color = Color.green;
                else                 _nodeImage.color = Color.gray;
            }
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _onTapped?.Invoke(_tier));
            }
        }
    }
}
