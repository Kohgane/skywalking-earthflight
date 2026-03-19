using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 주간 챌린지 배너 UI (HUD 상단, 접기/펼치기 가능).
    /// Weekly challenge banner displayed at the top of the HUD.
    /// </summary>
    public class WeeklyChallengeUI : MonoBehaviour
    {
        [Header("Banner")]
        [SerializeField] private GameObject bannerRoot;
        [SerializeField] private Button     collapseTrigger;

        [Header("Challenge Info")]
        [SerializeField] private Text  titleText;
        [SerializeField] private Text  descriptionText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text  timeRemainingText;
        [SerializeField] private Button viewLeaderboardButton;

        [Header("Refs")]
        [SerializeField] private LeaderboardUI leaderboardUI;

        // ── Internal state ────────────────────────────────────────────────────────
        private WeeklyChallenge _challenge;
        private bool            _collapsed;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            collapseTrigger?.onClick.AddListener(ToggleCollapse);
            viewLeaderboardButton?.onClick.AddListener(OpenLeaderboard);

            if (leaderboardUI == null)
                leaderboardUI = FindFirstObjectByType<LeaderboardUI>();
        }

        private void Start()
        {
            var manager = WeeklyChallengeManager.Instance;
            if (manager != null)
            {
                manager.OnNewChallengeAvailable += OnChallengeReceived;
                manager.GetCurrentChallenge(OnChallengeReceived);
            }

            if (bannerRoot != null)
                bannerRoot.SetActive(true);
        }

        private void OnDestroy()
        {
            if (WeeklyChallengeManager.Instance != null)
                WeeklyChallengeManager.Instance.OnNewChallengeAvailable -= OnChallengeReceived;
        }

        private void Update()
        {
            if (_challenge == null) return;

            // 남은 시간 계산
            if (DateTime.TryParseExact(_challenge.endDate, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            {
                TimeSpan remaining = end - DateTime.UtcNow;
                if (remaining.TotalSeconds <= 0)
                {
                    if (timeRemainingText != null)
                        timeRemainingText.text = "챌린지 종료 / Ended";
                }
                else if (remaining.TotalDays >= 1)
                {
                    if (timeRemainingText != null)
                        timeRemainingText.text = $"{(int)remaining.TotalDays}일 {remaining.Hours}시간 남음";
                }
                else
                {
                    if (timeRemainingText != null)
                        timeRemainingText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2} 남음";
                }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnChallengeReceived(WeeklyChallenge challenge)
        {
            _challenge = challenge;
            if (challenge == null) return;

            if (titleText != null)
                titleText.text = challenge.title;

            if (descriptionText != null)
                descriptionText.text = challenge.description;

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value    = 0f; // 실제 진행률은 외부에서 UpdateProgress()로 설정
            }
        }

        /// <summary>
        /// 챌린지 진행률을 업데이트합니다 (0–1 범위).
        /// Updates the challenge progress bar (0–1 range).
        /// </summary>
        public void UpdateProgress(float currentValue)
        {
            if (_challenge == null || progressSlider == null) return;
            float t = _challenge.targetValue > 0f ? Mathf.Clamp01(currentValue / _challenge.targetValue) : 0f;
            progressSlider.value = t;
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            // 배너 내부 콘텐츠만 숨기고 루트는 유지
            if (titleText != null)       titleText.gameObject.SetActive(!_collapsed);
            if (descriptionText != null) descriptionText.gameObject.SetActive(!_collapsed);
            if (progressSlider != null)  progressSlider.gameObject.SetActive(!_collapsed);
            if (timeRemainingText != null) timeRemainingText.gameObject.SetActive(!_collapsed);
            if (viewLeaderboardButton != null) viewLeaderboardButton.gameObject.SetActive(!_collapsed);
        }

        private void OpenLeaderboard()
        {
            leaderboardUI?.Open();
        }
    }
}
