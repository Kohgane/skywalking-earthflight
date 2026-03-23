// VerticalSpeedIndicator.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that shows rate of climb or descent.
    ///
    /// <para>Displays a smoothed numeric readout, an optional analog needle, and an
    /// optional vertical bar graph.  Color codes the reading by severity of descent.</para>
    /// </summary>
    public class VerticalSpeedIndicator : HUDInstrument
    {
        #region Inspector

        [Header("VSI — Text")]
        [Tooltip("Numeric vertical-speed readout (e.g., \"+15.2 m/s\").")]
        [SerializeField] private TextMeshProUGUI vsiText;

        [Header("VSI — Analog Needle")]
        [Tooltip("Needle image — rotated proportionally to vertical speed.")]
        [SerializeField] private Image vsiNeedle;

        [Header("VSI — Bar Graph")]
        [Tooltip("Vertical fill-bar alternative to the analog needle.")]
        [SerializeField] private Image vsiBargraph;

        [Header("VSI — Range")]
        [Tooltip("Maximum vertical speed (m/s) represented at full gauge deflection.")]
        [SerializeField] private float maxDisplayRate = 50f;

        [Header("VSI — Smoothing")]
        [Tooltip("Lerp speed for smoothing the vertical speed reading (reduces jitter).")]
        [SerializeField] private float smoothing = 5f;

        [Header("VSI — Thresholds")]
        [Tooltip("Vertical speed ≤ ±levelThreshold m/s is considered level flight (green).")]
        [SerializeField] private float levelThreshold = 2f;

        [Tooltip("Descent rate (positive m/s) above which the display turns red (critical).")]
        [SerializeField] private float criticalDescentRate = 30f;

        #endregion

        #region Private State

        private float _smoothedVS;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            // Smooth the raw reading.
            _smoothedVS = Mathf.Lerp(_smoothedVS, data.verticalSpeed, smoothing * Time.deltaTime);

            Color color = PickColor(_smoothedVS);

            // ── Text readout ─────────────────────────────────────────────────
            if (vsiText != null)
            {
                string sign   = _smoothedVS >= 0f ? "+" : "";
                vsiText.text  = $"{sign}{_smoothedVS:F1} m/s";
                vsiText.color = color;
            }

            // ── Analog needle (−maxRate → −180°, 0 → 0°, +maxRate → +180°) ──
            if (vsiNeedle != null)
            {
                float normalised = Mathf.Clamp(_smoothedVS / maxDisplayRate, -1f, 1f);
                vsiNeedle.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, normalised * 180f);
            }

            // ── Bar graph (fill 0.5 = level, fill 1 = max climb, fill 0 = max descent) ──
            if (vsiBargraph != null)
            {
                float normalised  = Mathf.Clamp(_smoothedVS / maxDisplayRate, -1f, 1f);
                vsiBargraph.fillAmount = (normalised + 1f) * 0.5f;
                vsiBargraph.color      = color;
            }
        }

        #endregion

        #region Helpers

        private Color PickColor(float vs)
        {
            if (vs < -criticalDescentRate)      return CockpitHUDConfig.CriticalColor;
            if (vs < -levelThreshold)           return CockpitHUDConfig.WarningColor;  // descending
            if (vs > levelThreshold)            return new Color(0.20f, 0.60f, 1.00f); // climbing (blue)
            return CockpitHUDConfig.SafeColor;                                          // level
        }

        #endregion
    }
}
