// Speedometer.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that displays airspeed information.
    ///
    /// <para>Shows speed in the selected unit, Mach number when above 0.8M, a
    /// scrolling tape visual, and a trend arrow indicating acceleration or
    /// deceleration.</para>
    /// </summary>
    public class Speedometer : HUDInstrument
    {
        /// <summary>Speed display unit.</summary>
        public enum SpeedUnit { MetersPerSecond, Knots, KilometersPerHour }

        #region Inspector

        [Header("Speedometer — Text")]
        [Tooltip("Primary speed readout.")]
        [SerializeField] private TextMeshProUGUI speedText;

        [Tooltip("Mach number readout (shown when speed ≥ 0.8M).")]
        [SerializeField] private TextMeshProUGUI machText;

        [Header("Speedometer — Tape")]
        [Tooltip("Optional scrolling tape image.")]
        [SerializeField] private Image speedTape;

        [Tooltip("Pixels of tape scroll per unit of speed change.")]
        [SerializeField] private float tapeScrollSpeed = 1f;

        [Header("Speedometer — Trend Arrow")]
        [Tooltip("Small arrow image rotated to indicate acceleration (+) or deceleration (−).")]
        [SerializeField] private RectTransform trendArrow;

        [Header("Speedometer — Units")]
        [Tooltip("Unit used for the primary speed readout.")]
        [SerializeField] private SpeedUnit displayUnit = SpeedUnit.Knots;

        [Header("Speedometer — Thresholds")]
        [Tooltip("Speed (in display units) above which the readout turns yellow.")]
        [SerializeField] private float overspeedThreshold = 300f;

        [Tooltip("Speed (in display units) above which the readout turns red.")]
        [SerializeField] private float criticalSpeedThreshold = 350f;

        #endregion

        #region Private State

        private float _previousSpeed;
        private const float MachShowThreshold = 0.8f;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            float displaySpeed = ConvertSpeed(data.speed);

            // ── Primary speed text ────────────────────────────────────────────
            if (speedText != null)
            {
                speedText.text  = FormatSpeed(displaySpeed);
                speedText.color = PickColor(displaySpeed);
            }

            // ── Mach text (visible when ≥ 0.8M) ─────────────────────────────
            if (machText != null)
            {
                bool showMach   = data.speedMach >= MachShowThreshold;
                machText.gameObject.SetActive(showMach);
                if (showMach)
                    machText.text = $"M {data.speedMach:F2}";
            }

            // ── Tape scroll ──────────────────────────────────────────────────
            if (speedTape != null)
            {
                RectTransform rt    = speedTape.rectTransform;
                Vector2       pos   = rt.anchoredPosition;
                pos.y               = displaySpeed * tapeScrollSpeed;
                rt.anchoredPosition = pos;
            }

            // ── Trend arrow ───────────────────────────────────────────────────
            if (trendArrow != null)
            {
                float delta = displaySpeed - _previousSpeed;
                // Rotate: 0° = level, +90° = accelerating, −90° = decelerating.
                float targetAngle = Mathf.Clamp(delta * 90f, -90f, 90f);
                trendArrow.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            }

            _previousSpeed = displaySpeed;
        }

        #endregion

        #region Helpers

        private float ConvertSpeed(float ms) => displayUnit switch
        {
            SpeedUnit.Knots              => CockpitHUDConfig.MsToKnots(ms),
            SpeedUnit.KilometersPerHour  => CockpitHUDConfig.MsToKph(ms),
            _                            => ms
        };

        private string FormatSpeed(float val) => displayUnit switch
        {
            SpeedUnit.Knots             => $"{val:F0} kt",
            SpeedUnit.KilometersPerHour => $"{val:F0} km/h",
            _                           => $"{val:F1} m/s"
        };

        private Color PickColor(float displaySpeed)
        {
            if (displaySpeed > criticalSpeedThreshold) return CockpitHUDConfig.CriticalColor;
            if (displaySpeed > overspeedThreshold)     return CockpitHUDConfig.WarningColor;
            return CockpitHUDConfig.SafeColor;
        }

        #endregion
    }
}
