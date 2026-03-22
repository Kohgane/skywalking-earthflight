using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;

namespace SWEF.Narration
{
    /// <summary>
    /// HUD overlay that shows:
    /// <list type="bullet">
    /// <item>A proximity indicator (distance + direction arrow) when a narration-eligible landmark is nearby.</item>
    /// <item>A toast notification for fun facts during active narration.</item>
    /// <item>A brief landmark name popup when the player first enters trigger range.</item>
    /// </list>
    /// Binds to <see cref="NarrationManager"/> events; all UI elements are
    /// optional and gracefully skipped if left unassigned.
    /// </summary>
    public class NarrationHudPanel : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Proximity Indicator")]
        [Tooltip("Root panel shown when a landmark is in range.")]
        [SerializeField] private GameObject proximityPanel;

        [Tooltip("Text showing the landmark name in the proximity panel.")]
        [SerializeField] private Text proximityLandmarkName;

        [Tooltip("Text showing formatted distance to the nearest landmark.")]
        [SerializeField] private Text proximityDistanceText;

        [Tooltip("Image / arrow that points toward the nearest landmark.")]
        [SerializeField] private RectTransform directionArrow;

        [Header("Fun Fact Toast")]
        [Tooltip("Root panel for fun-fact toast notifications.")]
        [SerializeField] private GameObject funFactPanel;

        [Tooltip("Text element inside the fun-fact panel.")]
        [SerializeField] private Text funFactText;

        [Tooltip("How many seconds the fun-fact toast stays visible.")]
        [SerializeField] private float funFactDuration = 5f;

        [Header("Narration Active Banner")]
        [Tooltip("Small banner shown while narration is playing.")]
        [SerializeField] private GameObject activeBanner;

        [Tooltip("Text on the active banner (landmark name).")]
        [SerializeField] private Text activeBannerText;

        // ── State ─────────────────────────────────────────────────────────────────
        private Coroutine _funFactCoroutine;
        private Coroutine _proximityCoroutine;
        private LandmarkData _nearestLandmark;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            SetPanel(proximityPanel, false);
            SetPanel(funFactPanel,   false);
            SetPanel(activeBanner,   false);
        }

        private void OnEnable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNearbyLandmarkDetected += OnNearbyLandmarkDetected;
            mgr.OnLandmarkExitRange      += OnLandmarkExitRange;
            mgr.OnNarrationStarted       += OnNarrationStarted;
            mgr.OnNarrationFinished      += OnNarrationFinished;
            mgr.OnFunFactReady           += ShowFunFact;
        }

        private void OnDisable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNearbyLandmarkDetected -= OnNearbyLandmarkDetected;
            mgr.OnLandmarkExitRange      -= OnLandmarkExitRange;
            mgr.OnNarrationStarted       -= OnNarrationStarted;
            mgr.OnNarrationFinished      -= OnNarrationFinished;
            mgr.OnFunFactReady           -= ShowFunFact;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnNearbyLandmarkDetected(LandmarkData lm)
        {
            if (NarrationManager.Instance?.Config.showProximityIndicator != true) return;
            _nearestLandmark = lm;
            ShowProximityIndicator(lm);
        }

        private void OnLandmarkExitRange(LandmarkData lm)
        {
            if (_nearestLandmark?.landmarkId == lm.landmarkId)
            {
                _nearestLandmark = null;
                SetPanel(proximityPanel, false);
            }
        }

        private void OnNarrationStarted(NarrationQueueEntry entry)
        {
            SetPanel(proximityPanel, false);
            SetPanel(activeBanner,   true);

            if (activeBannerText != null)
                activeBannerText.text = GetLocalizedName(entry.landmark);
        }

        private void OnNarrationFinished(NarrationQueueEntry entry, NarrationState state)
        {
            SetPanel(activeBanner, false);
        }

        // ── Fun fact toast ────────────────────────────────────────────────────────

        private void ShowFunFact(string key)
        {
            if (NarrationManager.Instance?.Config.enableFunFacts != true) return;
            if (funFactPanel == null) return;

            string text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(key)
                : key;

            if (string.IsNullOrEmpty(text) || text == key) return;

            if (funFactText != null) funFactText.text = text;

            if (_funFactCoroutine != null) StopCoroutine(_funFactCoroutine);
            _funFactCoroutine = StartCoroutine(ShowToast(funFactPanel, funFactDuration));
        }

        // ── Proximity indicator ───────────────────────────────────────────────────

        private void ShowProximityIndicator(LandmarkData lm)
        {
            if (proximityPanel == null) return;

            if (proximityLandmarkName != null)
                proximityLandmarkName.text = GetLocalizedName(lm);

            SetPanel(proximityPanel, true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetLocalizedName(LandmarkData lm)
        {
            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(lm.localizedNameKey))
            {
                string loc = LocalizationManager.Instance.GetText(lm.localizedNameKey);
                if (!string.IsNullOrEmpty(loc) && loc != lm.localizedNameKey)
                    return loc;
            }
            return lm.name;
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }

        private IEnumerator ShowToast(GameObject panel, float duration)
        {
            panel.SetActive(true);
            yield return new WaitForSeconds(duration);
            panel.SetActive(false);
        }
    }
}
