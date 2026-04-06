using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Core
{
    /// <summary>
    /// Optional fallback UI panel shown when a native review prompt is unavailable.
    /// Fades in/out using a <see cref="CanvasGroup"/>.
    /// Wire the panel and buttons via the Inspector, then call <see cref="Show"/> /
    /// <see cref="Hide"/> from <see cref="RatePromptManager"/> or any other manager.
    /// </summary>
    public class RatePromptUI : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static RatePromptUI Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Panel")]
        [SerializeField] private GameObject  ratePanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button rateButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private Button neverButton;

        [Header("Animation")]
        [Tooltip("Seconds for the fade in / out animation.")]
        [SerializeField] private float fadeDuration = 0.3f;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (rateButton  != null) rateButton.onClick.AddListener(OnRateClicked);
            if (laterButton != null) laterButton.onClick.AddListener(OnLaterClicked);
            if (neverButton != null) neverButton.onClick.AddListener(OnNeverClicked);

            if (ratePanel != null) ratePanel.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Fades in the rate-prompt panel.</summary>
        public void Show()
        {
            if (ratePanel == null) return;
            ratePanel.SetActive(true);
            if (canvasGroup != null)
                StartCoroutine(Fade(0f, 1f));
        }

        /// <summary>Fades out and hides the rate-prompt panel.</summary>
        public void Hide()
        {
            StartCoroutine(FadeAndHide());
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnRateClicked()
        {
            RatePromptManager.Instance?.OpenStoreReview();
            RatePromptManager.Instance?.MarkAsRated();
            Hide();
        }

        private void OnLaterClicked()
        {
            // Dismiss without marking as rated — will re-check next session
            Hide();
        }

        private void OnNeverClicked()
        {
            RatePromptManager.Instance?.MarkAsRated();
            Hide();
        }

        // ── Animation helpers ────────────────────────────────────────────────

        private IEnumerator Fade(float from, float to)
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private IEnumerator FadeAndHide()
        {
            yield return Fade(canvasGroup != null ? canvasGroup.alpha : 1f, 0f);
            if (ratePanel != null) ratePanel.SetActive(false);
        }
    }
}
