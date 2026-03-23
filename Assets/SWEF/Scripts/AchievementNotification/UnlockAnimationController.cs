// UnlockAnimationController.cs — SWEF Achievement Notification & Popup System

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// MonoBehaviour that orchestrates the full-screen achievement unlock celebration
    /// sequence played when a significant achievement is earned.
    ///
    /// <para>The animation runs through five phases:</para>
    /// <list type="number">
    ///   <item>Dark overlay fade-in.</item>
    ///   <item>Achievement icon scale-up with glow pulse.</item>
    ///   <item>Title and description text reveal via a typewriter effect.</item>
    ///   <item>Reward display with an animated counter.</item>
    ///   <item>Particle burst followed by overlay fade-out.</item>
    /// </list>
    ///
    /// <para>The player can skip the sequence at any time.
    /// Check <see cref="IsPlaying"/> and call <see cref="Skip"/> if needed.</para>
    /// </summary>
    public class UnlockAnimationController : MonoBehaviour
    {
        #region Inspector

        [Header("Overlay")]
        [Tooltip("Full-screen dark overlay CanvasGroup for phase 1.")]
        [SerializeField] private CanvasGroup overlayCanvasGroup;

        [Tooltip("Target alpha for the dark overlay (0–1).")]
        [SerializeField] [Range(0f, 1f)] private float overlayTargetAlpha = 0.85f;

        [Tooltip("Duration (seconds) of the overlay fade-in.")]
        [SerializeField] private float overlayFadeDuration = 0.4f;

        [Header("Icon")]
        [Tooltip("Image component that displays the achievement icon.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Glow graphic that pulses behind the icon.")]
        [SerializeField] private Graphic glowGraphic;

        [Tooltip("Duration (seconds) of the icon scale-up animation.")]
        [SerializeField] private float iconScaleDuration = 0.5f;

        [Header("Text Reveal")]
        [Tooltip("Text component for the achievement title.")]
        [SerializeField] private Text titleText;

        [Tooltip("Text component for the achievement description.")]
        [SerializeField] private Text descriptionText;

        [Tooltip("Seconds between each character reveal in the typewriter effect.")]
        [SerializeField] [Range(0.01f, 0.1f)] private float typewriterInterval = 0.03f;

        [Header("Reward")]
        [Tooltip("RewardDisplayManager used in phase 4.")]
        [SerializeField] private RewardDisplayManager rewardDisplayManager;

        [Header("Particles")]
        [Tooltip("Particle system burst played in phase 5.")]
        [SerializeField] private ParticleSystem celebrationParticles;

        [Tooltip("Duration (seconds) of the final fade-out.")]
        [SerializeField] private float finalFadeDuration = 0.6f;

        [Header("Skip Input")]
        [Tooltip("KeyCode used to skip the animation. Set to None to disable skip.")]
        [SerializeField] private KeyCode skipKey = KeyCode.Space;

        #endregion

        #region Events

        /// <summary>Fired whenever the animation transitions to a new phase (1–5).</summary>
        public event Action<int> OnAnimationPhaseChanged;

        /// <summary>Fired when the full animation sequence completes or is skipped.</summary>
        public event Action OnAnimationComplete;

        #endregion

        #region State

        /// <summary>Whether the unlock animation is currently running.</summary>
        public bool IsPlaying { get; private set; }

        private bool      _skipRequested;
        private Coroutine _animCoroutine;

        #endregion

        #region Public API

        /// <summary>Plays the full unlock animation for the given achievement.</summary>
        /// <param name="info">Achievement display data including title, description, icon and tier.</param>
        public void PlayUnlockAnimation(AchievementDisplayInfo info)
        {
            if (IsPlaying && _animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _skipRequested  = false;
            IsPlaying       = true;
            _animCoroutine  = StartCoroutine(AnimationSequence(info));
        }

        /// <summary>Requests an early skip of the currently playing animation.</summary>
        public void Skip()
        {
            _skipRequested = true;
        }

        #endregion

        #region Animation sequence

        private IEnumerator AnimationSequence(AchievementDisplayInfo info)
        {
            // ── Phase 1: Dark overlay fade-in ─────────────────────────────────────
            FirePhase(1);
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.gameObject.SetActive(true);
                overlayCanvasGroup.alpha = 0f;
                yield return StartCoroutine(FadeCanvasGroup(
                    overlayCanvasGroup, 0f, overlayTargetAlpha, overlayFadeDuration));
            }
            if (_skipRequested) { yield return FinishSequence(); yield break; }

            // ── Phase 2: Achievement icon scale-up with glow ──────────────────────
            FirePhase(2);
            if (iconImage != null)
            {
                LoadIcon(info);
                iconImage.transform.localScale = Vector3.zero;
                iconImage.gameObject.SetActive(true);
                yield return StartCoroutine(ScaleUp(iconImage.transform, iconScaleDuration));
                yield return StartCoroutine(GlowPulse());
            }
            if (_skipRequested) { yield return FinishSequence(); yield break; }

            // ── Phase 3: Title + description typewriter ───────────────────────────
            FirePhase(3);
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                yield return StartCoroutine(TypewriterReveal(titleText, info.title));
            }
            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(true);
                yield return StartCoroutine(TypewriterReveal(descriptionText, info.description));
            }
            if (_skipRequested) { yield return FinishSequence(); yield break; }

            // ── Phase 4: Reward counter animation ────────────────────────────────
            FirePhase(4);
            if (rewardDisplayManager != null && info.xpReward > 0)
            {
                var reward = new RewardDisplayConfig
                {
                    rewardType    = RewardType.XP,
                    amount        = info.xpReward,
                    displayString = "XP"
                };
                rewardDisplayManager.DisplayReward(reward);
                yield return new WaitUntil(() => !rewardDisplayManager.IsDisplaying || _skipRequested);
            }
            if (_skipRequested) { yield return FinishSequence(); yield break; }

            // ── Phase 5: Particle burst + fade-out ───────────────────────────────
            FirePhase(5);
            if (celebrationParticles != null)
                celebrationParticles.Play();

            yield return new WaitForSeconds(1.2f);
            yield return FinishSequence();
        }

        private IEnumerator FinishSequence()
        {
            if (overlayCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(
                    overlayCanvasGroup, overlayCanvasGroup.alpha, 0f, finalFadeDuration));

            ResetUI();
            IsPlaying = false;
            OnAnimationComplete?.Invoke();
        }

        private void FirePhase(int phase)
        {
            OnAnimationPhaseChanged?.Invoke(phase);
        }

        #endregion

        #region Skip input

        private void Update()
        {
            if (IsPlaying && skipKey != KeyCode.None && Input.GetKeyDown(skipKey))
                Skip();
        }

        #endregion

        #region Tween helpers

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            cg.alpha = from;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed  += Time.deltaTime;
                cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (!_skipRequested) cg.alpha = to;
        }

        private IEnumerator ScaleUp(Transform t, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                float s  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (!_skipRequested) t.localScale = Vector3.one;
        }

        private IEnumerator GlowPulse()
        {
            if (glowGraphic == null) yield break;
            float duration = 0.6f;
            float elapsed  = 0f;
            Color baseColor = glowGraphic.color;
            while (elapsed < duration && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.PingPong(elapsed * 3f, 1f);
                glowGraphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            glowGraphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        }

        private IEnumerator TypewriterReveal(Text text, string fullText)
        {
            text.text = string.Empty;
            foreach (char c in fullText)
            {
                if (_skipRequested) break;
                text.text += c;
                yield return new WaitForSeconds(typewriterInterval);
            }
            text.text = fullText;
        }

        private void LoadIcon(AchievementDisplayInfo info)
        {
            if (string.IsNullOrEmpty(info.iconPath)) return;
            var sprite = Resources.Load<Sprite>(info.iconPath);
            if (sprite != null) iconImage.sprite = sprite;
        }

        private void ResetUI()
        {
            if (iconImage != null)        iconImage.gameObject.SetActive(false);
            if (titleText != null)        titleText.gameObject.SetActive(false);
            if (descriptionText != null)  descriptionText.gameObject.SetActive(false);
            if (overlayCanvasGroup != null) overlayCanvasGroup.gameObject.SetActive(false);
        }

        #endregion
    }
}
