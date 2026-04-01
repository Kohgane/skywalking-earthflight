// DockingGuidanceHUD.cs — SWEF Space Station & Orbital Docking System
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpaceStation
{
    /// <summary>
    /// MonoBehaviour that renders the docking approach HUD overlay.
    /// Displays port-relative offset crosshair, distance, closing speed,
    /// attitude deviation indicators, phase label, and approach corridor
    /// colour (green / yellow / red).
    /// </summary>
    public class DockingGuidanceHUD : MonoBehaviour
    {
        // ── Inspector — UI references ─────────────────────────────────────────────

        [Header("Root")]
        [SerializeField] private GameObject _hudRoot;

        [Header("Crosshair")]
        [SerializeField] private RectTransform _crosshair;
        [SerializeField] private float         _crosshairScale = 200f;

        [Header("Readouts")]
        [SerializeField] private Text _distanceText;
        [SerializeField] private Text _closingSpeedText;
        [SerializeField] private Text _phaseText;

        [Header("Attitude")]
        [SerializeField] private Text _pitchText;
        [SerializeField] private Text _yawText;
        [SerializeField] private Text _rollText;

        [Header("Corridor")]
        [SerializeField] private Image _corridorIndicator;
        [SerializeField] private Color _colorGood    = Color.green;
        [SerializeField] private Color _colorCaution = Color.yellow;
        [SerializeField] private Color _colorAbort   = Color.red;

        // ── Private state ─────────────────────────────────────────────────────────

        private DockingController _controller;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            _controller = DockingController.Instance;
            if (_controller != null)
            {
                _controller.OnPhaseChanged   += HandlePhaseChanged;
                _controller.OnDockingComplete += HandleDockingComplete;
                _controller.OnDockingAborted  += HandleDockingAborted;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnPhaseChanged   -= HandlePhaseChanged;
                _controller.OnDockingComplete -= HandleDockingComplete;
                _controller.OnDockingAborted  -= HandleDockingAborted;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates all HUD elements with the latest approach telemetry.</summary>
        /// <param name="distance">Distance to docking port in metres.</param>
        /// <param name="closingSpeed">Closing speed in m/s (positive = approaching).</param>
        /// <param name="portOffset">Port-relative 2D offset for crosshair (normalised −1 to +1).</param>
        /// <param name="pitchDeg">Pitch deviation in degrees.</param>
        /// <param name="yawDeg">Yaw deviation in degrees.</param>
        /// <param name="rollDeg">Roll deviation in degrees.</param>
        public void UpdateHUD(float distance, float closingSpeed, Vector2 portOffset,
                              float pitchDeg, float yawDeg, float rollDeg)
        {
            if (_distanceText    != null) _distanceText.text    = $"{distance:F1} m";
            if (_closingSpeedText != null) _closingSpeedText.text = $"{closingSpeed:F2} m/s";

            if (_crosshair != null)
                _crosshair.anchoredPosition = portOffset * _crosshairScale;

            if (_pitchText != null) _pitchText.text = $"P {pitchDeg:+0.0;-0.0}°";
            if (_yawText   != null) _yawText.text   = $"Y {yawDeg:+0.0;-0.0}°";
            if (_rollText  != null) _rollText.text  = $"R {rollDeg:+0.0;-0.0}°";

            UpdateCorridorColour(distance, closingSpeed, pitchDeg, yawDeg);
        }

        /// <summary>Shows or hides the entire HUD.</summary>
        public void SetVisible(bool visible)
        {
            if (_hudRoot != null)
                _hudRoot.SetActive(visible);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateCorridorColour(float distance, float closingSpeed,
                                          float pitchDeg, float yawDeg)
        {
            if (_corridorIndicator == null) return;

            float alignDev = Mathf.Sqrt(pitchDeg * pitchDeg + yawDeg * yawDeg);
            bool   speedOk  = closingSpeed <= 5f;
            bool   alignOk  = alignDev <= 5f;

            Color c = (speedOk && alignOk) ? _colorGood
                    : (!speedOk || alignDev > 15f) ? _colorAbort
                    : _colorCaution;

            _corridorIndicator.color = c;
        }

        private void HandlePhaseChanged(DockingApproachPhase phase)
        {
            if (_phaseText != null)
                _phaseText.text = phase.ToString();
            SetVisible(phase != DockingApproachPhase.FreeApproach);
        }

        private void HandleDockingComplete()
        {
            SetVisible(false);
        }

        private void HandleDockingAborted(string reason)
        {
            SetVisible(false);
        }
    }
}
