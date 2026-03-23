// Altimeter.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that displays altitude information.
    ///
    /// <para>Shows MSL altitude and AGL altitude with color coding based on
    /// proximity to the ground. An optional scrolling tape visual can be driven
    /// by setting <see cref="altitudeTape"/>.</para>
    /// </summary>
    public class Altimeter : HUDInstrument
    {
        /// <summary>Altitude display unit.</summary>
        public enum AltitudeUnit { Meters, Feet }

        #region Inspector

        [Header("Altimeter — Text")]
        [Tooltip("Primary MSL altitude readout.")]
        [SerializeField] private TextMeshProUGUI altitudeText;

        [Tooltip("Secondary AGL altitude readout (smaller text).")]
        [SerializeField] private TextMeshProUGUI altitudeAGLText;

        [Header("Altimeter — Tape")]
        [Tooltip("Optional scrolling tape image (anchored position driven by altitude).")]
        [SerializeField] private Image altitudeTape;

        [Tooltip("Pixels of tape scroll per meter of altitude change.")]
        [SerializeField] private float tapeScrollSpeed = 1f;

        [Header("Altimeter — Units")]
        [Tooltip("Unit used to display altitude values.")]
        [SerializeField] private AltitudeUnit displayUnit = AltitudeUnit.Meters;

        [Header("Altimeter — Thresholds")]
        [Tooltip("AGL altitude (in display units) below which the readout turns yellow.")]
        [SerializeField] private float lowAltitudeThreshold = 100f;

        [Tooltip("AGL altitude (in display units) below which the readout turns red.")]
        [SerializeField] private float criticalAltitudeThreshold = 30f;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            float alt    = Convert(data.altitude);
            float altAGL = Convert(data.altitudeAGL);

            // ── MSL text ─────────────────────────────────────────────────────
            if (altitudeText != null)
            {
                altitudeText.text  = FormatAlt(alt);
                altitudeText.color = PickColor(altAGL);
            }

            // ── AGL text ─────────────────────────────────────────────────────
            if (altitudeAGLText != null)
            {
                altitudeAGLText.text  = $"AGL {FormatAlt(altAGL)}";
                altitudeAGLText.color = PickColor(altAGL);
            }

            // ── Tape scroll ──────────────────────────────────────────────────
            if (altitudeTape != null)
            {
                RectTransform rt    = altitudeTape.rectTransform;
                Vector2       pos   = rt.anchoredPosition;
                pos.y               = alt * tapeScrollSpeed;
                rt.anchoredPosition = pos;
            }
        }

        #endregion

        #region Helpers

        private float Convert(float meters)
            => displayUnit == AltitudeUnit.Feet
                ? CockpitHUDConfig.MetersToFeet(meters)
                : meters;

        private string FormatAlt(float val)
            => displayUnit == AltitudeUnit.Feet
                ? $"{val:F0} ft"
                : $"{val:F0} m";

        private Color PickColor(float aglInDisplayUnits)
        {
            if (aglInDisplayUnits < criticalAltitudeThreshold)
                return CockpitHUDConfig.CriticalColor;
            if (aglInDisplayUnits < lowAltitudeThreshold)
                return CockpitHUDConfig.CautionColor;
            return CockpitHUDConfig.SafeColor;
        }

        #endregion
    }
}
