// ThrottleFuelGauge.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that displays throttle position and fuel level.
    ///
    /// <para>Throttle and fuel are shown as vertical fill bars with percentage text.
    /// When fuel drops below <see cref="lowFuelThreshold"/> the fuel bar blinks red.</para>
    /// </summary>
    public class ThrottleFuelGauge : HUDInstrument
    {
        #region Inspector

        [Header("Throttle")]
        [Tooltip("Vertical fill bar for throttle position (0 = idle, 1 = full).")]
        [SerializeField] private Image throttleBar;

        [Tooltip("Percentage text for throttle (e.g., \"85%\").")]
        [SerializeField] private TextMeshProUGUI throttleText;

        [Header("Fuel")]
        [Tooltip("Vertical fill bar for fuel remaining (0 = empty, 1 = full).")]
        [SerializeField] private Image fuelBar;

        [Tooltip("Fuel percentage or estimated time-remaining text.")]
        [SerializeField] private TextMeshProUGUI fuelText;

        [Header("Fuel — Warnings")]
        [Tooltip("Fuel fraction (0–1) below which low-fuel blinking begins.")]
        [SerializeField] private float lowFuelThreshold = CockpitHUDConfig.DefaultLowFuel;

        [Tooltip("Blink frequency (Hz) when fuel is critically low.")]
        [SerializeField] private float fuelBlinkRate = 2f;

        #endregion

        #region Private State

        private float _blinkTimer;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            // ── Throttle ─────────────────────────────────────────────────────
            if (throttleBar != null)
                throttleBar.fillAmount = Mathf.Clamp01(data.throttlePercent);

            if (throttleText != null)
                throttleText.text = $"{data.throttlePercent * 100f:F0}%";

            // ── Fuel ──────────────────────────────────────────────────────────
            bool lowFuel = data.fuelPercent < lowFuelThreshold;

            if (fuelBar != null)
            {
                fuelBar.fillAmount = Mathf.Clamp01(data.fuelPercent);

                if (lowFuel)
                {
                    _blinkTimer += Time.deltaTime;
                    // Toggle visibility at blinkRate Hz.
                    bool blinkOn  = Mathf.Sin(_blinkTimer * fuelBlinkRate * Mathf.PI * 2f) >= 0f;
                    fuelBar.color = blinkOn
                        ? CockpitHUDConfig.CriticalColor
                        : new Color(CockpitHUDConfig.CriticalColor.r,
                                    CockpitHUDConfig.CriticalColor.g,
                                    CockpitHUDConfig.CriticalColor.b,
                                    0.2f);
                }
                else
                {
                    _blinkTimer   = 0f;
                    fuelBar.color = data.fuelPercent < 0.4f
                        ? CockpitHUDConfig.CautionColor
                        : CockpitHUDConfig.SafeColor;
                }
            }

            if (fuelText != null)
                fuelText.text = $"{data.fuelPercent * 100f:F0}%";
        }

        #endregion
    }
}
