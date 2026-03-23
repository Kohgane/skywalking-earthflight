// RewardDisplayManager.cs — SWEF Achievement Notification & Popup System

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// MonoBehaviour that handles animated reward displays for achievement unlocks.
    ///
    /// <para>Features include:
    /// <list type="bullet">
    ///   <item>Lerp-based counter animation from 0 to the final value.</item>
    ///   <item>Reward icon display with a tier-tinted rarity glow effect.</item>
    ///   <item>Sequential display of multiple rewards one after another.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Call <see cref="DisplayReward"/> for a single reward, or
    /// <see cref="DisplayRewards"/> for a batch.  Listen to
    /// <see cref="OnRewardDisplayComplete"/> to know when the sequence ends.</para>
    /// </summary>
    public class RewardDisplayManager : MonoBehaviour
    {
        #region Inspector

        [Header("Counter")]
        [Tooltip("Text component that shows the animated reward counter.")]
        [SerializeField] private Text counterText;

        [Tooltip("Text component that shows the reward label (e.g. 'XP', 'Coins').")]
        [SerializeField] private Text rewardLabelText;

        [Tooltip("Duration (seconds) of the counter lerp animation.")]
        [SerializeField] [Range(0.5f, 5f)] private float counterDuration = 1.5f;

        [Header("Icon & Glow")]
        [Tooltip("Image component that shows the reward type icon.")]
        [SerializeField] private Image rewardIconImage;

        [Tooltip("Graphic used as the rarity glow behind the reward icon.")]
        [SerializeField] private Graphic glowGraphic;

        [Tooltip("Icon sprites indexed by RewardType (must be 4 entries: XP, Currency, Cosmetic, Title).")]
        [SerializeField] private Sprite[] rewardTypeIcons = new Sprite[4];

        [Header("Glow Colors")]
        [Tooltip("Glow color for XP rewards.")]
        [SerializeField] private Color xpGlowColor = new Color(0.2f, 0.8f, 1.0f, 0.8f);

        [Tooltip("Glow color for Currency rewards.")]
        [SerializeField] private Color currencyGlowColor = new Color(1.0f, 0.85f, 0.0f, 0.8f);

        [Tooltip("Glow color for Cosmetic rewards.")]
        [SerializeField] private Color cosmeticGlowColor = new Color(0.8f, 0.2f, 1.0f, 0.8f);

        [Tooltip("Glow color for Title rewards.")]
        [SerializeField] private Color titleGlowColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        [Header("Sequential Delay")]
        [Tooltip("Pause (seconds) between consecutive rewards in a multi-reward sequence.")]
        [SerializeField] [Range(0.1f, 2f)] private float sequenceDelay = 0.5f;

        #endregion

        #region Events

        /// <summary>Fired when all reward display animations have finished.</summary>
        public event Action OnRewardDisplayComplete;

        #endregion

        #region State

        /// <summary>Whether a reward display sequence is currently active.</summary>
        public bool IsDisplaying { get; private set; }

        private Coroutine _displayCoroutine;

        #endregion

        #region Public API

        /// <summary>Displays a single reward with an animated counter.</summary>
        /// <param name="reward">Reward configuration including type, amount and label.</param>
        public void DisplayReward(RewardDisplayConfig reward)
        {
            DisplayRewards(new List<RewardDisplayConfig> { reward });
        }

        /// <summary>
        /// Displays multiple rewards sequentially, one after another.
        /// Fires <see cref="OnRewardDisplayComplete"/> when all have been shown.
        /// </summary>
        /// <param name="rewards">Ordered list of rewards to display.</param>
        public void DisplayRewards(List<RewardDisplayConfig> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;

            if (_displayCoroutine != null)
                StopCoroutine(_displayCoroutine);

            IsDisplaying       = true;
            _displayCoroutine  = StartCoroutine(SequenceRoutine(rewards));
        }

        #endregion

        #region Coroutines

        private IEnumerator SequenceRoutine(List<RewardDisplayConfig> rewards)
        {
            foreach (var reward in rewards)
            {
                yield return StartCoroutine(ShowSingleReward(reward));
                yield return new WaitForSeconds(sequenceDelay);
            }

            IsDisplaying = false;
            OnRewardDisplayComplete?.Invoke();
        }

        private IEnumerator ShowSingleReward(RewardDisplayConfig reward)
        {
            // Update icon.
            if (rewardIconImage != null)
            {
                int iconIndex = (int)reward.rewardType;
                if (iconIndex < rewardTypeIcons.Length && rewardTypeIcons[iconIndex] != null)
                    rewardIconImage.sprite = rewardTypeIcons[iconIndex];
                rewardIconImage.gameObject.SetActive(true);
            }

            // Update glow color.
            if (glowGraphic != null)
            {
                glowGraphic.color = GetGlowColor(reward.rewardType);
                glowGraphic.gameObject.SetActive(true);
                StartCoroutine(GlowPulse());
            }

            // Update label.
            if (rewardLabelText != null)
                rewardLabelText.text = reward.displayString;

            // Counter animation.
            if (counterText != null)
                yield return StartCoroutine(CounterAnimation(0, reward.amount, counterDuration));
            else
                yield return new WaitForSeconds(counterDuration);
        }

        private IEnumerator CounterAnimation(int from, int to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                int   v  = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                counterText.text = v.ToString("N0");
                yield return null;
            }
            counterText.text = to.ToString("N0");
        }

        private IEnumerator GlowPulse()
        {
            if (glowGraphic == null) yield break;
            Color baseColor = glowGraphic.color;
            float elapsed   = 0f;
            float duration  = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.PingPong(elapsed * 2.5f, 1f);
                glowGraphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            glowGraphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a);
        }

        #endregion

        #region Helpers

        private Color GetGlowColor(RewardType type)
        {
            switch (type)
            {
                case RewardType.Currency: return currencyGlowColor;
                case RewardType.Cosmetic: return cosmeticGlowColor;
                case RewardType.Title:    return titleGlowColor;
                default:                  return xpGlowColor;
            }
        }

        #endregion
    }
}
