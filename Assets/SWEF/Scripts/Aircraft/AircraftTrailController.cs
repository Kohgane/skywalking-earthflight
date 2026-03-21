using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Manages the aircraft's contrail / wake-trail effect.
    /// Trail opacity scales with speed; trail width scales with altitude.
    /// Integrates with <see cref="FlightController"/> and
    /// <see cref="AltitudeController"/> every frame.
    /// </summary>
    public class AircraftTrailController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private FlightController flightController;
        [SerializeField] private AltitudeController altitudeController;

        [Header("Speed Scaling")]
        [SerializeField] private float minSpeedForTrail = 30f;
        [SerializeField] private float maxSpeedForFullTrail = 200f;
        [SerializeField] private float opacitySmoothing = 4f;

        [Header("Altitude Width Scaling")]
        [SerializeField] private float widthAtSeaLevel = 0.4f;
        [SerializeField] private float widthAtMaxAltitude = 2.5f;
        [SerializeField] private float maxAltitudeReference = 10000f;
        [SerializeField] private float widthSmoothing = 3f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private float _currentOpacity = 0f;
        private float _currentWidth   = 0.4f;

        private Color _primaryColor   = Color.white;
        private Color _secondaryColor = new Color(0.7f, 0.9f, 1f, 0f);

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (flightController == null)
                flightController = FindObjectOfType<FlightController>();
            if (altitudeController == null)
                altitudeController = FindObjectOfType<AltitudeController>();
        }

        private void Update()
        {
            UpdateTrailFromFlightState();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the contrail gradient with the given primary and secondary colours.
        /// </summary>
        public void SetColors(Color primary, Color secondary)
        {
            _primaryColor   = primary;
            _secondaryColor = secondary;
            RefreshGradient();
        }

        /// <summary>Enables or disables the trail renderer without clearing existing trail geometry.</summary>
        public void SetTrailActive(bool active)
        {
            if (trailRenderer != null)
                trailRenderer.emitting = active;
        }

        /// <summary>
        /// Called every frame to synchronise trail opacity and width with the current
        /// flight state. Exposed as public so external systems can call it on demand.
        /// </summary>
        public void UpdateTrailFromFlightState()
        {
            if (trailRenderer == null) return;

            float speed = flightController != null ? flightController.CurrentSpeedMps : 0f;
            float altitude = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;

            // ── Opacity (speed-driven) ────────────────────────────────────────────
            float targetOpacity = Mathf.InverseLerp(minSpeedForTrail, maxSpeedForFullTrail, speed);
            _currentOpacity = ExpSmoothing.ExpLerp(_currentOpacity, targetOpacity, opacitySmoothing, Time.deltaTime);

            // ── Width (altitude-driven) ───────────────────────────────────────────
            float altitudeRatio = Mathf.Clamp01(altitude / maxAltitudeReference);
            float targetWidth = Mathf.Lerp(widthAtSeaLevel, widthAtMaxAltitude, altitudeRatio);
            _currentWidth = ExpSmoothing.ExpLerp(_currentWidth, targetWidth, widthSmoothing, Time.deltaTime);

            trailRenderer.widthMultiplier = _currentWidth;

            // Update the colour gradient's alpha to reflect opacity.
            var gradient = trailRenderer.colorGradient;
            var alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
                alphaKeys[i].alpha = _currentOpacity;
            gradient.alphaKeys = alphaKeys;
            trailRenderer.colorGradient = gradient;

            // Disable emitting when effectively invisible.
            trailRenderer.emitting = _currentOpacity > 0.01f;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void RefreshGradient()
        {
            if (trailRenderer == null) return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(_primaryColor,   0f),
                    new GradientColorKey(_secondaryColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(_primaryColor.a * _currentOpacity,   0f),
                    new GradientAlphaKey(_secondaryColor.a * _currentOpacity, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }
    }
}
