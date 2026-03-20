using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Achievement
{
    /// <summary>
    /// Displays a slide-in notification popup at the top of the screen when an
    /// achievement is unlocked.  Multiple unlocks are queued and shown one at a time.
    /// </summary>
    public class AchievementNotificationUI : MonoBehaviour
    {
        // ── Tier colours ──────────────────────────────────────────────────────────
        private static readonly Color32 ColourBronze   = new Color32(0xCD, 0x7F, 0x32, 0xFF);
        private static readonly Color32 ColourSilver   = new Color32(0xC0, 0xC0, 0xC0, 0xFF);
        private static readonly Color32 ColourGold     = new Color32(0xFF, 0xD7, 0x00, 0xFF);
        private static readonly Color32 ColourPlatinum = new Color32(0xE5, 0xE4, 0xE2, 0xFF);
        private static readonly Color32 ColourDiamond  = new Color32(0xB9, 0xF2, 0xFF, 0xFF);

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Panel References")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Image         iconImage;
        [SerializeField] private Text          titleText;
        [SerializeField] private Text          xpText;
        [SerializeField] private Image         tierBadge;

        [Header("Animation")]
        [SerializeField] private float slideInSeconds  = 0.4f;
        [SerializeField] private float displaySeconds  = 4f;
        [SerializeField] private float slideOutSeconds = 0.4f;
        [SerializeField] private float hiddenYOffset   = -200f;

        [Header("Optional Effects")]
        [SerializeField] private ParticleSystem achievementParticles;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Queue<AchievementDefinition> _queue = new Queue<AchievementDefinition>();
        private bool _showing;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += Enqueue;
        }

        private void OnDisable()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= Enqueue;
        }

        private void Start()
        {
            HideImmediate();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adds an achievement to the notification queue.</summary>
        public void Enqueue(AchievementDefinition def)
        {
            _queue.Enqueue(def);
            if (!_showing)
                StartCoroutine(ProcessQueue());
        }

        // ── Internal coroutines ───────────────────────────────────────────────────
        private IEnumerator ProcessQueue()
        {
            _showing = true;
            while (_queue.Count > 0)
            {
                var def = _queue.Dequeue();
                yield return ShowNotification(def);
            }
            _showing = false;
        }

        private IEnumerator ShowNotification(AchievementDefinition def)
        {
            Populate(def);

            // Slide in
            yield return SlidePanel(hiddenYOffset, 0f, slideInSeconds);

            achievementParticles?.Play();

            // Hold
            yield return new WaitForSeconds(displaySeconds);

            // Slide out
            yield return SlidePanel(0f, hiddenYOffset, slideOutSeconds);
        }

        private IEnumerator SlidePanel(float fromY, float toY, float duration)
        {
            if (panelRoot == null) yield break;

            float elapsed = 0f;
            var   pos     = panelRoot.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                pos.y    = Mathf.Lerp(fromY, toY, t);
                panelRoot.anchoredPosition = pos;
                yield return null;
            }

            pos.y = toY;
            panelRoot.anchoredPosition = pos;
        }

        private void Populate(AchievementDefinition def)
        {
            if (iconImage != null)
            {
                iconImage.sprite  = def.icon;
                iconImage.enabled = def.icon != null;
            }

            if (titleText != null)
            {
                var locMgr = LocalizationManager.Instance;
                titleText.text = locMgr != null
                    ? locMgr.Get(def.titleKey)
                    : def.titleKey;
            }

            if (xpText != null)
                xpText.text = $"+{def.xpReward} XP";

            if (tierBadge != null)
                tierBadge.color = TierColour(def.tier);
        }

        private void HideImmediate()
        {
            if (panelRoot == null) return;
            var pos = panelRoot.anchoredPosition;
            pos.y = hiddenYOffset;
            panelRoot.anchoredPosition = pos;
        }

        private static Color TierColour(AchievementTier tier) => tier switch
        {
            AchievementTier.Silver   => ColourSilver,
            AchievementTier.Gold     => ColourGold,
            AchievementTier.Platinum => ColourPlatinum,
            AchievementTier.Diamond  => ColourDiamond,
            _                        => ColourBronze
        };
    }
}
