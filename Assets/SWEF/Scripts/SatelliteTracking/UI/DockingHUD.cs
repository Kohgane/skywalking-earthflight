// DockingHUD.cs — Phase 114: Satellite & Space Debris Tracking
// Docking interface: alignment crosshair, distance readout, velocity vector, capture indicator.
// Namespace: SWEF.SatelliteTracking

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Docking HUD controller: shows alignment crosshair, range tape, closing-velocity
    /// readout, lateral offset bar, go/no-go annunciator, and capture/hard-dock indicators.
    /// </summary>
    public class DockingHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Crosshair / Alignment")]
        [SerializeField] private RectTransform alignmentCrosshair;
        [Tooltip("Max pixel offset the crosshair travels at full misalignment.")]
        [SerializeField] private float crosshairMaxOffsetPx = 80f;
        [SerializeField] private float maxMisalignmentDeg = 10f;

        [Header("Readouts")]
        [SerializeField] private Text rangeText;
        [SerializeField] private Text velocityText;
        [SerializeField] private Text lateralOffsetText;
        [SerializeField] private Text alignmentText;
        [SerializeField] private Text stateLabel;

        [Header("Go / No-Go")]
        [SerializeField] private Text goNoGoText;
        [SerializeField] private Image goNoGoBackground;
        [SerializeField] private Color goColor   = new Color(0f,  0.8f, 0f);
        [SerializeField] private Color noGoColor = new Color(0.9f, 0.1f, 0.1f);

        [Header("Capture Indicator")]
        [SerializeField] private GameObject captureIndicator;
        [SerializeField] private GameObject hardDockIndicator;

        // ── Private state ─────────────────────────────────────────────────────────
        private DockingScenarioController _scenario;
        private DockingGuidanceSystem     _guidance;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _scenario = FindObjectOfType<DockingScenarioController>();
            _guidance = FindObjectOfType<DockingGuidanceSystem>();

            if (_scenario != null) _scenario.OnStateChanged += HandleStateChanged;

            if (captureIndicator  != null) captureIndicator.SetActive(false);
            if (hardDockIndicator != null) hardDockIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_scenario != null) _scenario.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (_scenario == null) return;
            UpdateCrosshair();
            UpdateReadouts();
            UpdateGoNoGo();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateCrosshair()
        {
            if (alignmentCrosshair == null) return;
            float normalised = Mathf.Clamp(_scenario.AlignmentAngleDeg / maxMisalignmentDeg, -1f, 1f);
            alignmentCrosshair.anchoredPosition = new Vector2(
                normalised * crosshairMaxOffsetPx,
                -(_scenario.LateralOffsetM / 5f) * crosshairMaxOffsetPx);
        }

        private void UpdateReadouts()
        {
            SetText(rangeText,         $"{_scenario.RangeToPortM:F1} m");
            SetText(velocityText,      $"{_scenario.ClosingVelocityMs:F3} m/s");
            SetText(lateralOffsetText, $"{_scenario.LateralOffsetM:F2} m");
            SetText(alignmentText,     $"{_scenario.AlignmentAngleDeg:F1}°");
            SetText(stateLabel,        _scenario.CurrentState.ToString());
        }

        private void UpdateGoNoGo()
        {
            if (_guidance == null) return;

            bool go = _guidance.IsGo;
            if (goNoGoText != null)
                goNoGoText.text = go ? "GO" : "NO GO";
            if (goNoGoBackground != null)
                goNoGoBackground.color = go ? goColor : noGoColor;
        }

        private void HandleStateChanged(DockingState state)
        {
            if (captureIndicator  != null)
                captureIndicator.SetActive(state == DockingState.Capture);
            if (hardDockIndicator != null)
                hardDockIndicator.SetActive(state == DockingState.HardDock);
        }

        private static void SetText(Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
