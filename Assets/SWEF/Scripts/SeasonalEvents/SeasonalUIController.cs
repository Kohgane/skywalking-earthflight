// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonalUIController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// UI controller for all seasonal content screens.
    ///
    /// <para>Panels managed:</para>
    /// <list type="bullet">
    ///   <item>Season overview with countdown to season end.</item>
    ///   <item>Battle-pass progress bar, tier list, and next-reward preview.</item>
    ///   <item>Seasonal challenge list with per-challenge progress bars.</item>
    ///   <item>Live event banners and in-game notifications.</item>
    ///   <item>Reward claim confirmation dialog.</item>
    /// </list>
    ///
    /// All panels are toggled via <see cref="ShowPanel"/> and the layout is designed
    /// to work on PC, Mobile, and Tablet without platform-specific branches.
    /// </summary>
    [DisallowMultipleComponent]
    public class SeasonalUIController : MonoBehaviour
    {
        #region Inspector — Root Panels
        [Header("Root Panels")]
        [Tooltip("Root GameObject for the season overview panel.")]
        [SerializeField] private GameObject seasonOverviewPanel;

        [Tooltip("Root GameObject for the battle-pass panel.")]
        [SerializeField] private GameObject battlePassPanel;

        [Tooltip("Root GameObject for the seasonal challenges panel.")]
        [SerializeField] private GameObject challengesPanel;

        [Tooltip("Root GameObject for the live events panel.")]
        [SerializeField] private GameObject liveEventsPanel;
        #endregion

        #region Inspector — Season Overview
        [Header("Season Overview")]
        [SerializeField] private TMP_Text seasonNameText;
        [SerializeField] private TMP_Text seasonThemeText;
        [SerializeField] private TMP_Text countdownText;
        #endregion

        #region Inspector — Battle Pass
        [Header("Battle Pass")]
        [SerializeField] private TMP_Text currentTierText;
        [SerializeField] private Slider    xpProgressSlider;
        [SerializeField] private TMP_Text xpProgressText;
        [SerializeField] private TMP_Text nextRewardNameText;
        [SerializeField] private Image    nextRewardIcon;
        [SerializeField] private TMP_Text premiumStatusText;
        #endregion

        #region Inspector — Challenges
        [Header("Challenges")]
        [Tooltip("Scroll-view content root for challenge entry prefabs.")]
        [SerializeField] private RectTransform challengeListRoot;
        [Tooltip("Prefab for a single challenge entry row.")]
        [SerializeField] private GameObject challengeEntryPrefab;
        #endregion

        #region Inspector — Live Events
        [Header("Live Events")]
        [Tooltip("Container for live event banner prefabs.")]
        [SerializeField] private RectTransform liveEventBannerRoot;
        [Tooltip("Prefab for a single live event banner.")]
        [SerializeField] private GameObject liveEventBannerPrefab;
        [SerializeField] private TMP_Text liveEventNotificationText;
        #endregion

        #region Inspector — Reward Claim
        [Header("Reward Claim Dialog")]
        [SerializeField] private GameObject rewardClaimDialog;
        [SerializeField] private TMP_Text   rewardClaimNameText;
        [SerializeField] private TMP_Text   rewardClaimDescriptionText;
        [SerializeField] private Image      rewardClaimIcon;
        [SerializeField] private Button     rewardClaimConfirmButton;
        #endregion

        #region State
        private readonly List<GameObject> _challengeEntries  = new List<GameObject>();
        private readonly List<GameObject> _liveEventBanners  = new List<GameObject>();
        private BattlePassReward _pendingClaimReward;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            SubscribeEvents();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            RefreshCountdown();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeEvents()
        {
            if (SeasonManager.Instance != null)
            {
                SeasonManager.Instance.OnSeasonStarted += HandleSeasonStarted;
                SeasonManager.Instance.OnSeasonEnded   += HandleSeasonEnded;
            }

            if (BattlePassController.Instance != null)
            {
                BattlePassController.Instance.OnTierUnlocked += HandleTierUnlocked;
                BattlePassController.Instance.OnXPEarned     += HandleXPEarned;
            }

            if (LiveEventManager.Instance != null)
            {
                LiveEventManager.Instance.OnLiveEventStarted += HandleLiveEventStarted;
                LiveEventManager.Instance.OnLiveEventEnded   += HandleLiveEventEnded;
            }

            if (SeasonalChallengeManager.Instance != null)
                SeasonalChallengeManager.Instance.OnChallengeCompleted += HandleChallengeCompleted;
        }

        private void UnsubscribeEvents()
        {
            if (SeasonManager.Instance != null)
            {
                SeasonManager.Instance.OnSeasonStarted -= HandleSeasonStarted;
                SeasonManager.Instance.OnSeasonEnded   -= HandleSeasonEnded;
            }

            if (BattlePassController.Instance != null)
            {
                BattlePassController.Instance.OnTierUnlocked -= HandleTierUnlocked;
                BattlePassController.Instance.OnXPEarned     -= HandleXPEarned;
            }

            if (LiveEventManager.Instance != null)
            {
                LiveEventManager.Instance.OnLiveEventStarted -= HandleLiveEventStarted;
                LiveEventManager.Instance.OnLiveEventEnded   -= HandleLiveEventEnded;
            }

            if (SeasonalChallengeManager.Instance != null)
                SeasonalChallengeManager.Instance.OnChallengeCompleted -= HandleChallengeCompleted;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Shows the specified panel and hides all others.
        /// </summary>
        public void ShowPanel(SeasonalPanel panel)
        {
            SetActive(seasonOverviewPanel, panel == SeasonalPanel.Overview);
            SetActive(battlePassPanel,     panel == SeasonalPanel.BattlePass);
            SetActive(challengesPanel,     panel == SeasonalPanel.Challenges);
            SetActive(liveEventsPanel,     panel == SeasonalPanel.LiveEvents);
            SetActive(rewardClaimDialog,   panel == SeasonalPanel.RewardClaim);

            switch (panel)
            {
                case SeasonalPanel.Overview:    RefreshOverview();    break;
                case SeasonalPanel.BattlePass:  RefreshBattlePass();  break;
                case SeasonalPanel.Challenges:  RefreshChallenges();  break;
                case SeasonalPanel.LiveEvents:  RefreshLiveEvents();  break;
            }
        }

        /// <summary>Shows the reward claim dialog for the specified reward.</summary>
        public void ShowRewardClaim(BattlePassReward reward)
        {
            if (reward == null) return;
            _pendingClaimReward = reward;

            SetText(rewardClaimNameText,        reward.DisplayName);
            SetText(rewardClaimDescriptionText, reward.Description);
            SetActive(rewardClaimDialog, true);

            if (rewardClaimConfirmButton != null)
            {
                rewardClaimConfirmButton.onClick.RemoveAllListeners();
                rewardClaimConfirmButton.onClick.AddListener(ConfirmRewardClaim);
            }
        }

        /// <summary>Hides all seasonal panels.</summary>
        public void HideAll()
        {
            SetActive(seasonOverviewPanel, false);
            SetActive(battlePassPanel,     false);
            SetActive(challengesPanel,     false);
            SetActive(liveEventsPanel,     false);
            SetActive(rewardClaimDialog,   false);
        }
        #endregion

        #region Refresh Methods
        private void Refresh()
        {
            RefreshOverview();
            RefreshBattlePass();
        }

        private void RefreshOverview()
        {
            var season = SeasonManager.Instance?.CurrentSeason;
            SetText(seasonNameText,  season?.SeasonName ?? "—");
            SetText(seasonThemeText, season?.Theme      ?? "—");
        }

        private void RefreshCountdown()
        {
            var season = SeasonManager.Instance?.CurrentSeason;
            if (countdownText == null || season == null) return;

            var remaining = season.GetEndDateUtc() - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                countdownText.text = "Season Ended";
                return;
            }
            countdownText.text = $"{remaining.Days}d {remaining.Hours:00}h {remaining.Minutes:00}m";
        }

        private void RefreshBattlePass()
        {
            var bpc = BattlePassController.Instance;
            if (bpc == null) return;

            SetText(currentTierText, $"Tier {bpc.CurrentTier}");
            SetText(premiumStatusText, bpc.IsPremium ? "Premium" : "Free");

            if (xpProgressSlider != null)
                xpProgressSlider.value = bpc.TierProgressFraction;

            var next = bpc.GetNextRewardPreview(bpc.IsPremium);
            SetText(nextRewardNameText, next?.DisplayName ?? "Max Tier");
        }

        private void RefreshChallenges()
        {
            ClearList(_challengeEntries);
            if (challengeListRoot == null || challengeEntryPrefab == null) return;

            var mgr = SeasonalChallengeManager.Instance;
            if (mgr == null) return;

            foreach (var c in mgr.DailyChallenges)  SpawnChallengeEntry(c, "Daily");
            foreach (var c in mgr.WeeklyChallenges) SpawnChallengeEntry(c, "Weekly");
        }

        private void SpawnChallengeEntry(SeasonalChallenge challenge, string tag)
        {
            if (challengeEntryPrefab == null || challengeListRoot == null) return;
            var go = Instantiate(challengeEntryPrefab, challengeListRoot);
            _challengeEntries.Add(go);

            var texts = go.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            if (texts.Length > 0) texts[0].text = $"[{tag}] {challenge.Title}";
            if (texts.Length > 1) texts[1].text = challenge.IsCompleted ? "✓" : $"{challenge.Progress}/{challenge.Target}";

            var slider = go.GetComponentInChildren<Slider>(includeInactive: true);
            if (slider != null && float.TryParse(challenge.Target, out float max) && max > 0)
                slider.value = Mathf.Clamp01(challenge.Progress / max);
        }

        private void RefreshLiveEvents()
        {
            ClearList(_liveEventBanners);
            if (liveEventBannerRoot == null || liveEventBannerPrefab == null) return;

            var mgr = LiveEventManager.Instance;
            if (mgr == null) return;

            foreach (var evt in mgr.ActiveEvents)
                SpawnEventBanner(evt);
        }

        private void SpawnEventBanner(LiveEvent evt)
        {
            if (liveEventBannerPrefab == null || liveEventBannerRoot == null) return;
            var go = Instantiate(liveEventBannerPrefab, liveEventBannerRoot);
            _liveEventBanners.Add(go);

            var texts = go.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            if (texts.Length > 0) texts[0].text = evt.EventName;
            if (texts.Length > 1)
            {
                var tr = evt.TimeRemaining();
                texts[1].text = $"{tr.Hours:00}h {tr.Minutes:00}m remaining";
            }
        }
        #endregion

        #region Event Handlers
        private void HandleSeasonStarted(SeasonData season) => RefreshOverview();
        private void HandleSeasonEnded(SeasonData season)   => RefreshOverview();

        private void HandleTierUnlocked(int tier, bool isPremium)
        {
            RefreshBattlePass();
            Debug.Log($"[SWEF] SeasonalUIController: Tier {tier} unlocked notification.");
        }

        private void HandleXPEarned(int amount, string source) => RefreshBattlePass();

        private void HandleLiveEventStarted(LiveEvent evt)
        {
            SetText(liveEventNotificationText, $"🔴 LIVE: {evt.EventName}");
            RefreshLiveEvents();
        }

        private void HandleLiveEventEnded(LiveEvent evt) => RefreshLiveEvents();

        private void HandleChallengeCompleted(SeasonalChallenge challenge) => RefreshChallenges();
        #endregion

        #region Reward Claim
        private void ConfirmRewardClaim()
        {
            if (_pendingClaimReward != null)
                Debug.Log($"[SWEF] SeasonalUIController: Reward claimed — '{_pendingClaimReward.DisplayName}'");

            _pendingClaimReward = null;
            SetActive(rewardClaimDialog, false);
        }
        #endregion

        #region Helpers
        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }

        private static void ClearList(List<GameObject> list)
        {
            foreach (var go in list)
                if (go != null) Destroy(go);
            list.Clear();
        }
        #endregion
    }

    /// <summary>Panels available in the seasonal UI.</summary>
    public enum SeasonalPanel
    {
        /// <summary>Season overview with countdown.</summary>
        Overview,
        /// <summary>Battle-pass tier and XP progress.</summary>
        BattlePass,
        /// <summary>Daily and weekly challenges list.</summary>
        Challenges,
        /// <summary>Active live event banners.</summary>
        LiveEvents,
        /// <summary>Reward claim confirmation dialog.</summary>
        RewardClaim
    }
}
