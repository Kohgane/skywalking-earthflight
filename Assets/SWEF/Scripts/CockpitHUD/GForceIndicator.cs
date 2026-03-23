// GForceIndicator.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that displays current G-force with peak tracking
    /// and a filled meter.
    ///
    /// <para>The peak G value decays slowly back toward the current reading after
    /// the aircraft stops pulling high G.</para>
    /// </summary>
    public class GForceIndicator : HUDInstrument
    {
        #region Inspector

        [Header("G-Force — Text")]
        [Tooltip("Numeric G readout (e.g., \"2.3G\").")]
        [SerializeField] private TextMeshProUGUI gForceText;

        [Header("G-Force — Meter")]
        [Tooltip("Filled arc or circular meter image (fill 0–1 maps to 0–maxDisplayG).")]
        [SerializeField] private Image gForceMeter;

        [Header("G-Force — Range")]
        [Tooltip("G value at which the meter shows full deflection.")]
        [SerializeField] private float maxDisplayG = 9f;

        [Header("G-Force — Thresholds")]
        [Tooltip("G above which the readout turns yellow (warning).")]
        [SerializeField] private float warningG = 5f;

        [Tooltip("G above which the readout turns red (critical).")]
        [SerializeField] private float criticalG = 7f;

        [Header("G-Force — Peak")]
        [Tooltip("Rate at which the peak G indicator decays (G per second).")]
        [SerializeField] private float peakDecayRate = 0.5f;

        #endregion

        #region Private State

        private float _peakG;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            float g = data.gForce;

            // Update peak with slow decay.
            if (g > _peakG)
                _peakG = g;
            else
                _peakG = Mathf.Max(g, _peakG - peakDecayRate * Time.deltaTime);

            Color color = PickColor(g);

            // ── Numeric text ─────────────────────────────────────────────────
            if (gForceText != null)
            {
                gForceText.text  = $"{g:F1}G";
                gForceText.color = color;
            }

            // ── Filled meter ──────────────────────────────────────────────────
            if (gForceMeter != null)
            {
                gForceMeter.fillAmount = Mathf.Clamp01(g / maxDisplayG);
                gForceMeter.color      = color;
            }
        }

        /// <summary>Peak G value tracked since the last reset.</summary>
        public float PeakG => _peakG;

        /// <summary>Resets the peak G tracker to zero.</summary>
        public void ResetPeak() => _peakG = 0f;

        #endregion

        #region Helpers

        private Color PickColor(float g)
        {
            if (g >= criticalG) return CockpitHUDConfig.CriticalColor;
            if (g >= warningG)  return CockpitHUDConfig.CautionColor;
            return Color.white;
        }

        #endregion
    }
}
