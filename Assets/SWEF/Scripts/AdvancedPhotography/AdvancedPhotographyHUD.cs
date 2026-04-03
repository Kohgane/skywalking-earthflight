// AdvancedPhotographyHUD.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AdvancedPhotography
{
    /// <summary>
    /// Phase 89 — MonoBehaviour that drives the UGUI photography HUD overlay.
    ///
    /// <para>Displays: composition guide overlay (rule-of-thirds grid, golden ratio points),
    /// composition score bar, AI suggestion text, drone status panel (battery, mode, altitude,
    /// distance), active challenge progress, and a photo spot direction indicator.</para>
    ///
    /// <para>All <see cref="SerializeField"/> references are null-safe.</para>
    /// </summary>
    public sealed class AdvancedPhotographyHUD : MonoBehaviour
    {
        // ── Composition Overlay ───────────────────────────────────────────────────

        [Header("Composition Overlay")]
        [Tooltip("Root CanvasGroup for the composition guide overlay (rule-of-thirds / golden ratio).")]
        [SerializeField] private CanvasGroup _compositionOverlayGroup;

        [Tooltip("Slider representing the composition quality score (0–1).")]
        [SerializeField] private Slider _compositionScoreSlider;

        [Tooltip("Text label displaying the AI composition suggestion.")]
        [SerializeField] private Text _suggestionText;

        [Tooltip("Image displaying the rule-of-thirds or golden ratio guide lines.")]
        [SerializeField] private Image _compositionGuideImage;

        // ── Drone Status ──────────────────────────────────────────────────────────

        [Header("Drone Status Panel")]
        [Tooltip("Root GameObject for the drone status panel.")]
        [SerializeField] private GameObject _dronePanelRoot;

        [Tooltip("Slider representing current battery percentage (0–1).")]
        [SerializeField] private Slider _batterySlider;

        [Tooltip("Text label for battery percentage.")]
        [SerializeField] private Text _batteryLabel;

        [Tooltip("Text label for current flight mode name.")]
        [SerializeField] private Text _flightModeLabel;

        [Tooltip("Text label for drone altitude in metres.")]
        [SerializeField] private Text _altitudeLabel;

        [Tooltip("Text label for drone distance from player in metres.")]
        [SerializeField] private Text _distanceLabel;

        // ── Challenge Progress ────────────────────────────────────────────────────

        [Header("Challenge Progress")]
        [Tooltip("Root GameObject for the active challenge progress section.")]
        [SerializeField] private GameObject _challengePanelRoot;

        [Tooltip("Text label for the current challenge title.")]
        [SerializeField] private Text _challengeTitleText;

        [Tooltip("Slider representing challenge completion progress (0–1).")]
        [SerializeField] private Slider _challengeProgressSlider;

        // ── Photo Spot Indicator ──────────────────────────────────────────────────

        [Header("Photo Spot Indicator")]
        [Tooltip("Root GameObject for the direction-arrow spot indicator.")]
        [SerializeField] private GameObject _spotIndicatorRoot;

        [Tooltip("RectTransform of the arrow image that rotates to point toward the nearest spot.")]
        [SerializeField] private RectTransform _spotArrow;

        [Tooltip("Text label showing the distance to the nearest photo spot.")]
        [SerializeField] private Text _spotDistanceText;

        // ── Histogram Placeholder ─────────────────────────────────────────────────

        [Header("Histogram")]
        [Tooltip("Placeholder image for the live exposure histogram.")]
        [SerializeField] private Image _histogramImage;

        // ── Private State ─────────────────────────────────────────────────────────

        private DroneAutonomyController _drone;
        private AICompositionAssistant  _aiAssistant;
        private PhotoSpotDiscovery      _spotDiscovery;
        private Camera                  _mainCamera;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _mainCamera = Camera.main;

            _drone         = DroneAutonomyController.Instance;
            _aiAssistant   = AICompositionAssistant.Instance;
            _spotDiscovery = PhotoSpotDiscovery.Instance;

            if (_drone != null)
            {
                _drone.OnFlightModeChanged += OnFlightModeChanged;
                _drone.OnBatteryLow        += OnBatteryLow;
            }

            if (_aiAssistant != null)
            {
                _aiAssistant.OnCompositionScoreChanged += OnCompositionScoreChanged;
                _aiAssistant.OnSuggestionUpdated       += OnSuggestionUpdated;
            }
        }

        private void OnDestroy()
        {
            if (_drone != null)
            {
                _drone.OnFlightModeChanged -= OnFlightModeChanged;
                _drone.OnBatteryLow        -= OnBatteryLow;
            }

            if (_aiAssistant != null)
            {
                _aiAssistant.OnCompositionScoreChanged -= OnCompositionScoreChanged;
                _aiAssistant.OnSuggestionUpdated       -= OnSuggestionUpdated;
            }
        }

        private void Update()
        {
            RefreshDronePanel();
            RefreshSpotIndicator();
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void OnFlightModeChanged(DroneFlightMode mode)
        {
            if (_flightModeLabel != null)
                _flightModeLabel.text = mode.ToString();
        }

        private void OnBatteryLow(float pct)
        {
            if (_batteryLabel != null)
                _batteryLabel.text = $"{pct * 100f:0}% ⚠";
        }

        private void OnCompositionScoreChanged(float score)
        {
            if (_compositionScoreSlider != null)
                _compositionScoreSlider.value = score;
        }

        private void OnSuggestionUpdated(string suggestion)
        {
            if (_suggestionText != null)
                _suggestionText.text = suggestion;
        }

        // ── Per-Frame Refresh ─────────────────────────────────────────────────────

        private void RefreshDronePanel()
        {
            if (_dronePanelRoot == null || _drone == null) return;

            float battery = _drone.GetBatteryPercent();

            if (_batterySlider != null) _batterySlider.value = battery;
            if (_batteryLabel  != null) _batteryLabel.text   = $"{battery * 100f:0}%";

            // Drone altitude / distance require the drone transform — not directly accessible
            // here; subscribe via DroneAutonomyController public properties if extended.
        }

        private void RefreshSpotIndicator()
        {
            if (_spotIndicatorRoot == null || _spotDiscovery == null || _mainCamera == null)
                return;

            Transform camTrans = _mainCamera.transform;
            var nearby = _spotDiscovery.GetNearbySpots(
                camTrans.position, AdvancedPhotographyConfig.PhotoSpotDiscoveryRadius);

            if (nearby.Count == 0)
            {
                _spotIndicatorRoot.SetActive(false);
                return;
            }

            _spotIndicatorRoot.SetActive(true);

            PhotoSpot nearest = nearby[0];
            float minDist = Vector3.Distance(camTrans.position, nearest.position);

            foreach (var spot in nearby)
            {
                float d = Vector3.Distance(camTrans.position, spot.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = spot;
                }
            }

            if (_spotDistanceText != null)
                _spotDistanceText.text = $"{minDist:0} m";

            if (_spotArrow != null)
            {
                Vector3 dir   = nearest.position - camTrans.position;
                Vector2 screenDir = new Vector2(
                    Vector3.Dot(dir, camTrans.right),
                    Vector3.Dot(dir, camTrans.up));

                float angle = Mathf.Atan2(screenDir.x, screenDir.y) * Mathf.Rad2Deg;
                _spotArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }
    }
}
