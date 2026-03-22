using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Narration
{
    /// <summary>
    /// Displays narration subtitle text in a HUD panel, synchronised with
    /// <see cref="NarrationSegment"/> timing.  Supports keyword highlighting
    /// and respects the <see cref="NarrationConfig.showSubtitles"/> setting.
    /// </summary>
    public class NarrationSubtitleUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Root panel GameObject that contains the subtitle UI.")]
        [SerializeField] private GameObject subtitlePanel;

        [Tooltip("Text element that shows the subtitle text.")]
        [SerializeField] private Text subtitleText;

        [Tooltip("Text element for the landmark title (header).")]
        [SerializeField] private Text titleText;

        [Tooltip("CanvasGroup for fade-in/out of the subtitle panel.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Style")]
        [Tooltip("Base colour of the subtitle text.")]
        [SerializeField] private Color textColor = Color.white;

        [Tooltip("Colour used to highlight keywords.")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.3f);

        [Tooltip("Duration in seconds to fade in/out the panel.")]
        [SerializeField] private float fadeDuration = 0.4f;

        // ── State ─────────────────────────────────────────────────────────────────
        private Coroutine _fadeCoroutine;
        private string    _currentLandmarkName;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            if (canvasGroup != null)   canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNarrationStarted  += OnNarrationStarted;
            mgr.OnNarrationFinished += OnNarrationFinished;
            mgr.OnSegmentChanged    += OnSegmentChanged;
        }

        private void OnDisable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNarrationStarted  -= OnNarrationStarted;
            mgr.OnNarrationFinished -= OnNarrationFinished;
            mgr.OnSegmentChanged    -= OnSegmentChanged;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnNarrationStarted(NarrationQueueEntry entry)
        {
            if (!ShouldShow()) return;

            _currentLandmarkName = entry.landmark.name;

            if (titleText != null)
                titleText.text = GetLocalizedName(entry.landmark);

            ApplyFontSize();
            ShowPanel();
        }

        private void OnNarrationFinished(NarrationQueueEntry entry, NarrationState state)
        {
            HidePanel();
        }

        private void OnSegmentChanged(NarrationSegment segment)
        {
            if (!ShouldShow() || subtitleText == null) return;
            subtitleText.text = BuildHighlightedText(segment.text, segment.highlightKeywords);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool ShouldShow()
        {
            var mgr = NarrationManager.Instance;
            return mgr != null && mgr.Config.showSubtitles;
        }

        private void ApplyFontSize()
        {
            if (subtitleText == null) return;
            var mgr = NarrationManager.Instance;
            if (mgr != null)
                subtitleText.fontSize = Mathf.RoundToInt(mgr.Config.subtitleFontSize);
        }

        private string GetLocalizedName(LandmarkData lm)
        {
            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(lm.localizedNameKey))
            {
                string loc = LocalizationManager.Instance.GetText(lm.localizedNameKey);
                if (!string.IsNullOrEmpty(loc) && loc != lm.localizedNameKey)
                    return loc;
            }
            return lm.name;
        }

        /// <summary>Wraps matched keywords in a colour-coded rich-text tag.</summary>
        private string BuildHighlightedText(string text, List<string> keywords)
        {
            if (keywords == null || keywords.Count == 0 || string.IsNullOrEmpty(text))
                return text;

            string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
            string result   = text;

            foreach (var kw in keywords)
            {
                if (string.IsNullOrEmpty(kw)) continue;
                result = result.Replace(kw, $"<color=#{hexColor}><b>{kw}</b></color>");
            }
            return result;
        }

        // ── Show / Hide with fade ─────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(true);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f, fadeDuration));
        }

        private void HidePanel()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(HidePanelCoroutine());
        }

        private IEnumerator HidePanelCoroutine()
        {
            yield return StartCoroutine(FadeCanvas(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, fadeDuration));
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
