using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Full-screen UI overlay that creates a "spotlight" cutout effect.
    /// The rest of the screen is dimmed with a semi-transparent dark panel while
    /// the target <see cref="RectTransform"/> appears fully lit.
    /// Supports smooth animated transitions and a pulsing glow around the highlight.
    /// </summary>
    public class TutorialHighlight : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Image       overlayImage;

        [Header("Spotlight")]
        [SerializeField] private RectTransform spotlightRect;
        [SerializeField] private Image         glowImage;

        [Header("Animation")]
        [SerializeField] private float moveSpeed     = 8f;
        [SerializeField] private float fadeSpeed     = 5f;
        [SerializeField] private float pulseMinAlpha = 0.4f;
        [SerializeField] private float pulseMaxAlpha = 0.9f;
        [SerializeField] private float pulseSpeed    = 2f;

        private RectTransform _targetRect;
        private bool          _visible;
        private float         _pulseTimer;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Dims the screen and places the spotlight on <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The HUD element to highlight. Pass <c>null</c> to dim the full screen without a cutout.</param>
        public void Show(RectTransform target)
        {
            _targetRect = target;
            _visible    = true;
            gameObject.SetActive(true);

            if (spotlightRect != null)
                spotlightRect.gameObject.SetActive(target != null);

            if (target != null)
                SnapSpotlightToTarget(target);
        }

        /// <summary>Fades out and hides the overlay.</summary>
        public void Hide()
        {
            _visible    = false;
            _targetRect = null;
        }

        /// <summary>
        /// Smoothly moves the spotlight from its current position to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The new HUD element to highlight.</param>
        public void MoveTo(RectTransform target)
        {
            _targetRect = target;
            if (spotlightRect != null)
                spotlightRect.gameObject.SetActive(target != null);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (overlayGroup != null) overlayGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateFade();
            UpdateSpotlightPosition();
            UpdatePulse();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void UpdateFade()
        {
            if (overlayGroup == null) return;
            float targetAlpha = _visible ? 1f : 0f;
            overlayGroup.alpha = Mathf.MoveTowards(overlayGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            if (!_visible && overlayGroup.alpha <= 0f)
                gameObject.SetActive(false);
        }

        private void UpdateSpotlightPosition()
        {
            if (_targetRect == null || spotlightRect == null) return;

            Vector3 worldPos = _targetRect.position;
            Vector2 size     = _targetRect.rect.size * _targetRect.lossyScale;

            spotlightRect.position   = Vector3.Lerp(spotlightRect.position, worldPos, moveSpeed * Time.deltaTime);
            spotlightRect.sizeDelta  = Vector2.Lerp(spotlightRect.sizeDelta, size + Vector2.one * 16f, moveSpeed * Time.deltaTime);
        }

        private void UpdatePulse()
        {
            if (glowImage == null || !_visible) return;
            _pulseTimer += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(_pulseTimer) + 1f) * 0.5f;
            Color c = glowImage.color;
            c.a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);
            glowImage.color = c;
        }

        private void SnapSpotlightToTarget(RectTransform target)
        {
            if (spotlightRect == null) return;
            spotlightRect.position  = target.position;
            spotlightRect.sizeDelta = target.rect.size * target.lossyScale + Vector2.one * 16f;
        }
    }
}
