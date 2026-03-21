using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Always-visible compact HUD widget showing today's challenges.
    /// Displays 3–4 challenge cards with progress bars, completion checkmarks,
    /// a streak flame icon, and a countdown timer to the next daily reset.
    /// Tap a card to expand its details and show the claim button.
    /// </summary>
    public class DailyChallengeHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Card Templates")]
        [Tooltip("Parent container that holds the challenge card instances.")]
        [SerializeField] private Transform _cardContainer;

        [Tooltip("Prefab for a single challenge card. Must contain ProgressBar, TitleText, ClaimButton children.")]
        [SerializeField] private GameObject _cardPrefab;

        [Header("Streak Display")]
        [Tooltip("Text element showing the current daily streak count.")]
        [SerializeField] private Text _streakText;

        [Tooltip("Image element that lights up when streak > 0.")]
        [SerializeField] private Image _streakFlameImage;

        [Header("Timer")]
        [Tooltip("Text element showing HH:MM:SS countdown to reset.")]
        [SerializeField] private Text _timerText;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<ChallengeCardWidget> _cards = new List<ChallengeCardWidget>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            SubscribeEvents();
            BuildCards();
            RefreshStreak();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateTimer();
        }

        // ── Event subscriptions ───────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            var mgr = DailyChallengeManager.Instance;
            if (mgr == null) return;
            mgr.OnChallengeProgressUpdated += OnProgressUpdated;
            mgr.OnChallengeCompleted        += OnChallengeCompleted;
            mgr.OnDailyReset                += OnDailyReset;
            mgr.OnStreakUpdated              += OnStreakUpdated;
        }

        private void UnsubscribeEvents()
        {
            var mgr = DailyChallengeManager.Instance;
            if (mgr == null) return;
            mgr.OnChallengeProgressUpdated -= OnProgressUpdated;
            mgr.OnChallengeCompleted        -= OnChallengeCompleted;
            mgr.OnDailyReset                -= OnDailyReset;
            mgr.OnStreakUpdated             -= OnStreakUpdated;
        }

        // ── Build UI ──────────────────────────────────────────────────────────────

        private void BuildCards()
        {
            if (_cardContainer == null || _cardPrefab == null) return;

            // Clear old cards.
            foreach (Transform child in _cardContainer) Destroy(child.gameObject);
            _cards.Clear();

            var mgr = DailyChallengeManager.Instance;
            if (mgr == null) return;

            foreach (var ac in mgr.GetTodaysChallenges())
            {
                var go = Instantiate(_cardPrefab, _cardContainer);
                var widget = go.GetComponent<ChallengeCardWidget>();
                if (widget == null) widget = go.AddComponent<ChallengeCardWidget>();
                widget.Bind(ac);
                _cards.Add(widget);
            }
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void OnProgressUpdated(ActiveChallenge ac)
        {
            foreach (var card in _cards)
                if (card.ChallengeId == ac.challengeId)
                    card.RefreshProgress(ac);
        }

        private void OnChallengeCompleted(ActiveChallenge ac)
        {
            foreach (var card in _cards)
                if (card.ChallengeId == ac.challengeId)
                    card.PlayCompletionAnimation();
        }

        private void OnDailyReset()
        {
            BuildCards();
            RefreshStreak();
        }

        private void OnStreakUpdated(int streak) => RefreshStreak();

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RefreshStreak()
        {
            var mgr = DailyChallengeManager.Instance;
            int streak = mgr != null ? mgr.GetDailyStreak() : 0;

            if (_streakText != null)
                _streakText.text = streak.ToString();

            if (_streakFlameImage != null)
                _streakFlameImage.color = streak > 0 ? Color.yellow : Color.grey;
        }

        private void UpdateTimer()
        {
            var mgr = DailyChallengeManager.Instance;
            if (mgr == null || _timerText == null) return;

            var ts = mgr.GetTimeUntilReset();
            _timerText.text = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helper component — one per card in the HUD
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Small component attached to each challenge card prefab instance.
    /// Binds data and handles progress bar updates and completion animations.
    /// </summary>
    public class ChallengeCardWidget : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("UI Elements (optional auto-find)")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text   _titleText;
        [SerializeField] private Text   _progressText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Image  _completionMark;
        [SerializeField] private Image  _cardBackground;

        // ── State ─────────────────────────────────────────────────────────────────
        private ActiveChallenge _ac;

        /// <summary>Challenge id this card represents.</summary>
        public string ChallengeId => _ac?.challengeId ?? string.Empty;

        // ── Binding ───────────────────────────────────────────────────────────────

        /// <summary>Binds the card to a challenge snapshot and refreshes all UI elements.</summary>
        public void Bind(ActiveChallenge ac)
        {
            _ac = ac;
            AutoFindComponents();
            Refresh();

            if (_claimButton != null)
                _claimButton.onClick.AddListener(OnClaimClicked);
        }

        /// <summary>Refreshes the progress bar and text to reflect the latest snapshot.</summary>
        public void RefreshProgress(ActiveChallenge updated)
        {
            _ac = updated;
            Refresh();
        }

        /// <summary>Plays a brief glow/scale animation to celebrate completion.</summary>
        public void PlayCompletionAnimation()
        {
            Refresh();
            StartCoroutine(GlowRoutine());
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_ac == null || _ac.definition == null) return;

            // Title.
            if (_titleText != null)
                _titleText.text = _ac.definition.titleKey; // Localization system would resolve key.

            // Progress bar.
            float t = _ac.definition.targetValue > 0f
                ? _ac.currentProgress / _ac.definition.targetValue
                : 0f;
            if (_progressBar != null)
                _progressBar.value = Mathf.Clamp01(t);

            if (_progressText != null)
                _progressText.text = $"{_ac.currentProgress:F0}/{_ac.definition.targetValue:F0}";

            // Card tint.
            if (_cardBackground != null)
                _cardBackground.color = _ac.definition.iconColor;

            // Completion checkmark.
            if (_completionMark != null)
                _completionMark.gameObject.SetActive(_ac.isCompleted);

            // Claim button.
            if (_claimButton != null)
                _claimButton.gameObject.SetActive(_ac.isCompleted && !_ac.isClaimed);
        }

        private void OnClaimClicked()
        {
            DailyChallengeManager.Instance?.ClaimReward(_ac.challengeId);
            Refresh();
        }

        private IEnumerator GlowRoutine()
        {
            if (_cardBackground == null) yield break;
            Color original = _cardBackground.color;
            Color glow = Color.white;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                _cardBackground.color = Color.Lerp(original, glow, Mathf.PingPong(t * 4f, 1f));
                yield return null;
            }
            _cardBackground.color = original;
        }

        private void AutoFindComponents()
        {
            if (_progressBar  == null) _progressBar  = GetComponentInChildren<Slider>();
            if (_titleText    == null)
            {
                var texts = GetComponentsInChildren<Text>();
                if (texts.Length > 0) _titleText = texts[0];
                if (texts.Length > 1) _progressText = texts[1];
            }
            if (_claimButton    == null) _claimButton    = GetComponentInChildren<Button>();
            if (_completionMark == null)
            {
                var images = GetComponentsInChildren<Image>();
                if (images.Length > 1) _completionMark = images[images.Length - 1];
            }
            if (_cardBackground == null)
            {
                var img = GetComponent<Image>();
                if (img != null) _cardBackground = img;
            }
        }
    }
}
