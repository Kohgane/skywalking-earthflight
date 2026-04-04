// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/SeasonalRewardCeremony.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Plays an animated reward reveal ceremony for tier-up and end-of-season moments.
    ///
    /// <para>Visual theme is driven by <see cref="RewardRarity"/>:</para>
    /// <list type="bullet">
    ///   <item><see cref="RewardRarity.Common"/> — white burst.</item>
    ///   <item><see cref="RewardRarity.Uncommon"/> — green glow.</item>
    ///   <item><see cref="RewardRarity.Rare"/> — blue shimmer.</item>
    ///   <item><see cref="RewardRarity.Epic"/> — purple glow with sparks.</item>
    ///   <item><see cref="RewardRarity.Legendary"/> — gold particles + screen flash.</item>
    /// </list>
    ///
    /// Attach to a persistent UI canvas. Trigger via <see cref="PlayRewardCeremony"/>
    /// or <see cref="PlayTierUpCeremony"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SeasonalRewardCeremony : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static SeasonalRewardCeremony Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when a ceremony animation begins.</summary>
        public event Action<BattlePassReward> OnCeremonyStarted;

        /// <summary>Fired when a ceremony animation completes.</summary>
        public event Action<BattlePassReward> OnCeremonyFinished;
        #endregion

        #region Inspector
        [Header("Ceremony Panel")]
        [Tooltip("Root panel shown during the ceremony. Hidden when idle.")]
        [SerializeField] private GameObject ceremonyRoot;

        [Tooltip("Text label displaying the reward name.")]
        [SerializeField] private TMP_Text rewardNameText;

        [Tooltip("Text label displaying the reward description.")]
        [SerializeField] private TMP_Text rewardDescriptionText;

        [Tooltip("Image for the reward icon (loaded at runtime from IconPath).")]
        [SerializeField] private Image rewardIconImage;

        [Tooltip("Text label showing the rarity name (e.g. 'LEGENDARY').")]
        [SerializeField] private TMP_Text rarityText;

        [Header("Particle Systems")]
        [Tooltip("Particle system played for Common rewards (white burst).")]
        [SerializeField] private ParticleSystem commonParticles;

        [Tooltip("Particle system played for Uncommon rewards (green glow).")]
        [SerializeField] private ParticleSystem uncommonParticles;

        [Tooltip("Particle system played for Rare rewards (blue shimmer).")]
        [SerializeField] private ParticleSystem rareParticles;

        [Tooltip("Particle system played for Epic rewards (purple sparks).")]
        [SerializeField] private ParticleSystem epicParticles;

        [Tooltip("Particle system played for Legendary rewards (gold shower).")]
        [SerializeField] private ParticleSystem legendaryParticles;

        [Header("Screen Flash")]
        [Tooltip("Full-screen overlay Image used for the legendary flash effect.")]
        [SerializeField] private Image screenFlashImage;

        [Header("Audio")]
        [Tooltip("AudioSource used for ceremony sound effects.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Clip played for Common / Uncommon rewards.")]
        [SerializeField] private AudioClip rewardSfxCommon;

        [Tooltip("Clip played for Rare rewards.")]
        [SerializeField] private AudioClip rewardSfxRare;

        [Tooltip("Clip played for Epic rewards.")]
        [SerializeField] private AudioClip rewardSfxEpic;

        [Tooltip("Clip played for Legendary rewards.")]
        [SerializeField] private AudioClip rewardSfxLegendary;

        [Header("Timing")]
        [Tooltip("Total ceremony display duration in seconds before auto-dismiss.")]
        [SerializeField, Range(1f, 10f)] private float ceremonyDuration = 4f;

        [Tooltip("Fade-in duration in seconds.")]
        [SerializeField, Range(0.1f, 2f)] private float fadeInDuration = 0.4f;

        [Tooltip("Fade-out duration in seconds.")]
        [SerializeField, Range(0.1f, 2f)] private float fadeOutDuration = 0.6f;
        #endregion

        #region State
        private Coroutine _ceremonyCoroutine;
        private bool _isPlaying;

        /// <summary>Returns <c>true</c> while a ceremony animation is in progress.</summary>
        public bool IsPlaying => _isPlaying;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (ceremonyRoot != null) ceremonyRoot.SetActive(false);
            if (screenFlashImage != null) SetAlpha(screenFlashImage, 0f);
        }
        #endregion

        #region Public API
        /// <summary>
        /// Plays the full reward reveal ceremony for the given <paramref name="reward"/>.
        /// Any in-progress ceremony is interrupted first.
        /// </summary>
        public void PlayRewardCeremony(BattlePassReward reward)
        {
            if (reward == null) return;
            if (_isPlaying && _ceremonyCoroutine != null)
                StopCoroutine(_ceremonyCoroutine);
            _ceremonyCoroutine = StartCoroutine(RunCeremony(reward));
        }

        /// <summary>
        /// Convenience overload that fires a lighter tier-up ceremony with no reward details.
        /// </summary>
        public void PlayTierUpCeremony(int tier, bool isPremium)
        {
            var syntheticReward = new BattlePassReward
            {
                RewardId    = $"tier_up_{tier}",
                DisplayName = $"Tier {tier} Reached",
                Description = isPremium ? "Premium track" : "Free track",
                Rarity      = tier >= 40 ? RewardRarity.Legendary
                            : tier >= 25 ? RewardRarity.Epic
                            : tier >= 10 ? RewardRarity.Rare
                            : RewardRarity.Common
            };
            PlayRewardCeremony(syntheticReward);
        }

        /// <summary>Immediately dismisses any active ceremony.</summary>
        public void Dismiss()
        {
            if (_ceremonyCoroutine != null) StopCoroutine(_ceremonyCoroutine);
            _isPlaying = false;
            if (ceremonyRoot != null) ceremonyRoot.SetActive(false);
        }
        #endregion

        #region Ceremony Coroutine
        private IEnumerator RunCeremony(BattlePassReward reward)
        {
            _isPlaying = true;
            OnCeremonyStarted?.Invoke(reward);

            // Populate UI
            if (rewardNameText        != null) rewardNameText.text        = reward.DisplayName;
            if (rewardDescriptionText != null) rewardDescriptionText.text = reward.Description;
            if (rarityText            != null) rarityText.text            = reward.Rarity.ToString().ToUpperInvariant();

            LoadRewardIcon(reward);

            if (ceremonyRoot != null) ceremonyRoot.SetActive(true);

            // Fade in
            yield return StartCoroutine(FadeCeremonyPanel(0f, 1f, fadeInDuration));

            // Play particles + audio based on rarity
            PlayRarityEffects(reward.Rarity);

            // Hold
            yield return new WaitForSeconds(ceremonyDuration);

            // Fade out
            yield return StartCoroutine(FadeCeremonyPanel(1f, 0f, fadeOutDuration));

            if (ceremonyRoot != null) ceremonyRoot.SetActive(false);

            _isPlaying = false;
            OnCeremonyFinished?.Invoke(reward);
        }

        private IEnumerator FadeCeremonyPanel(float from, float to, float duration)
        {
            if (ceremonyRoot == null) yield break;

            var canvasGroup = ceremonyRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = ceremonyRoot.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
        #endregion

        #region Rarity Effects
        private void PlayRarityEffects(RewardRarity rarity)
        {
            PlayParticles(rarity);
            PlayAudio(rarity);
            if (rarity == RewardRarity.Legendary)
                StartCoroutine(LegendaryScreenFlash());
        }

        private void PlayParticles(RewardRarity rarity)
        {
            StopAllParticles();
            switch (rarity)
            {
                case RewardRarity.Common:    Play(commonParticles);    break;
                case RewardRarity.Uncommon:  Play(uncommonParticles);  break;
                case RewardRarity.Rare:      Play(rareParticles);      break;
                case RewardRarity.Epic:      Play(epicParticles);      break;
                case RewardRarity.Legendary: Play(legendaryParticles); break;
            }
        }

        private void StopAllParticles()
        {
            Stop(commonParticles);
            Stop(uncommonParticles);
            Stop(rareParticles);
            Stop(epicParticles);
            Stop(legendaryParticles);
        }

        private static void Play(ParticleSystem ps) { if (ps != null) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.Play(); } }
        private static void Stop(ParticleSystem ps)  { if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }

        private void PlayAudio(RewardRarity rarity)
        {
            if (audioSource == null) return;
            AudioClip clip = rarity switch
            {
                RewardRarity.Legendary => rewardSfxLegendary,
                RewardRarity.Epic      => rewardSfxEpic,
                RewardRarity.Rare      => rewardSfxRare,
                _                      => rewardSfxCommon
            };
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private IEnumerator LegendaryScreenFlash()
        {
            if (screenFlashImage == null) yield break;

            float half = 0.15f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                SetAlpha(screenFlashImage, Mathf.Lerp(0f, 0.7f, elapsed / half));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half * 2f)
            {
                elapsed += Time.deltaTime;
                SetAlpha(screenFlashImage, Mathf.Lerp(0.7f, 0f, elapsed / (half * 2f)));
                yield return null;
            }
            SetAlpha(screenFlashImage, 0f);
        }
        #endregion

        #region Helpers
        private void LoadRewardIcon(BattlePassReward reward)
        {
            if (rewardIconImage == null || string.IsNullOrEmpty(reward?.IconPath)) return;
            var sprite = Resources.Load<Sprite>(reward.IconPath);
            if (sprite != null) rewardIconImage.sprite = sprite;
        }

        private static void SetAlpha(Image img, float alpha)
        {
            if (img == null) return;
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }
        #endregion
    }
}
