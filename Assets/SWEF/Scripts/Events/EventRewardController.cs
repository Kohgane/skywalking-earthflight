using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Events
{
    /// <summary>
    /// MonoBehaviour that handles reward distribution and animated reward popups when
    /// world events are completed.
    /// </summary>
    public class EventRewardController : MonoBehaviour
    {
        // ── Inner types ───────────────────────────────────────────────────────────
        /// <summary>
        /// Category of a single reward item.
        /// </summary>
        public enum RewardType { XP, Achievement, Cosmetic, Title }

        /// <summary>
        /// Describes a single reward to be displayed and granted.
        /// </summary>
        [Serializable]
        public struct RewardItem
        {
            /// <summary>Category of the reward.</summary>
            public RewardType type;

            /// <summary>Identifier for the reward (achievement ID, cosmetic ID, etc.).</summary>
            public string id;

            /// <summary>Numeric amount (e.g. XP quantity, or 1 for achievements/titles).</summary>
            public int amount;

            /// <summary>Human-readable name shown in the reward popup.</summary>
            public string displayName;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Popup UI")]
        [Tooltip("Root RectTransform of the reward popup card.")]
        [SerializeField] private RectTransform popupPanel;

        [Tooltip("Content root where individual reward row prefabs are instantiated.")]
        [SerializeField] private RectTransform rewardListRoot;

        [Tooltip("Prefab for a single reward row in the popup.")]
        [SerializeField] private GameObject rewardRowPrefab;

        [Tooltip("Name of the child Text component showing the reward label.")]
        [SerializeField] private string rewardLabelTextName = "RewardLabel";

        [Tooltip("Button that dismisses the reward popup.")]
        [SerializeField] private Button dismissButton;

        [Header("Animation")]
        [Tooltip("Distance in pixels the popup slides up from off-screen.")]
        [SerializeField] private float slideUpDistance = 400f;

        [Tooltip("Duration in seconds of the slide-up animation.")]
        [SerializeField] private float animDuration = 0.5f;

        [Tooltip("Seconds the popup stays visible before auto-dismissing.")]
        [SerializeField] private float displayDuration = 5f;

        // ── Internal state ────────────────────────────────────────────────────────
        private Vector2  _shownPos;
        private Vector2  _hiddenPos;
        private Coroutine _popupCoroutine;
        private readonly List<GameObject> _rows = new List<GameObject>();

        private SWEF.Achievement.AchievementManager _achievementManager;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _achievementManager = FindFirstObjectByType<SWEF.Achievement.AchievementManager>();
            if (popupPanel != null)
            {
                _shownPos  = popupPanel.anchoredPosition;
                _hiddenPos = _shownPos + Vector2.down * slideUpDistance;
                popupPanel.anchoredPosition = _hiddenPos;
                popupPanel.gameObject.SetActive(false);
            }

            if (dismissButton != null)
                dismissButton.onClick.AddListener(HidePopup);
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Grants all rewards associated with a completed event participation and
        /// displays the animated reward popup.
        /// </summary>
        /// <param name="instance">The event instance that was completed.</param>
        /// <param name="participation">The player's participation record.</param>
        public void GrantRewards(WorldEventInstance instance,
                                  EventParticipationTracker.EventParticipation participation)
        {
            if (instance == null) return;

            var rewards = new List<RewardItem>();

            // XP reward
            if (participation.xpEarned > 0)
            {
                rewards.Add(new RewardItem
                {
                    type        = RewardType.XP,
                    id          = instance.eventData?.eventId ?? string.Empty,
                    amount      = participation.xpEarned,
                    displayName = $"+{participation.xpEarned} XP"
                });
            }

            // Achievement reward
            if (!string.IsNullOrEmpty(instance.eventData?.achievementId))
            {
                _achievementManager?.TryUnlock(instance.eventData.achievementId);

                rewards.Add(new RewardItem
                {
                    type        = RewardType.Achievement,
                    id          = instance.eventData.achievementId,
                    amount      = 1,
                    displayName = instance.eventData.achievementId
                });
            }

            if (rewards.Count > 0)
                ShowRewardPopup(rewards);
        }

        /// <summary>
        /// Displays the animated reward popup card with the given list of reward items.
        /// </summary>
        /// <param name="rewards">Rewards to display in the popup.</param>
        public void ShowRewardPopup(List<RewardItem> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;

            PopulateRewardList(rewards);

            if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
            _popupCoroutine = StartCoroutine(PopupCoroutine());
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void PopulateRewardList(List<RewardItem> rewards)
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();

            if (rewardListRoot == null || rewardRowPrefab == null) return;

            foreach (var reward in rewards)
            {
                var row = Instantiate(rewardRowPrefab, rewardListRoot);
                var child = row.transform.Find(rewardLabelTextName);
                if (child != null)
                {
                    var tmp = child.GetComponent<TMP_Text>();
                    if (tmp != null)     tmp.text = reward.displayName;
                    else
                    {
                        var legacy = child.GetComponent<Text>();
                        if (legacy != null) legacy.text = reward.displayName;
                    }
                }
                _rows.Add(row);
            }
        }

        private IEnumerator PopupCoroutine()
        {
            if (popupPanel == null) yield break;

            popupPanel.gameObject.SetActive(true);
            yield return AnimateTo(_hiddenPos, _shownPos, animDuration);

            yield return new WaitForSeconds(displayDuration);

            yield return AnimateTo(_shownPos, _hiddenPos, animDuration);
            popupPanel.gameObject.SetActive(false);
        }

        private IEnumerator AnimateTo(Vector2 from, Vector2 to, float duration)
        {
            if (popupPanel == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                popupPanel.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            popupPanel.anchoredPosition = to;
        }

        private void HidePopup()
        {
            if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
            if (popupPanel != null)
            {
                popupPanel.anchoredPosition = _hiddenPos;
                popupPanel.gameObject.SetActive(false);
            }
        }
    }
}
