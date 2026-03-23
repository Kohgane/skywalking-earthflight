// CompassHeading.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using TMPro;
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that displays magnetic heading with cardinal
    /// direction labels and an optional scrolling compass strip.
    /// </summary>
    public class CompassHeading : HUDInstrument
    {
        #region Inspector

        [Header("CompassHeading — Text")]
        [Tooltip("Numeric heading readout (e.g., \"270°\").")]
        [SerializeField] private TextMeshProUGUI headingText;

        [Tooltip("Cardinal direction readout (N, NE, E, SE, S, SW, W, NW).")]
        [SerializeField] private TextMeshProUGUI cardinalText;

        [Header("CompassHeading — Strip")]
        [Tooltip("Horizontal scrolling compass strip (RectTransform shifted by heading).")]
        [SerializeField] private RectTransform compassStrip;

        [Tooltip("Pixels of strip scroll per degree of heading.")]
        [SerializeField] private float stripPixelsPerDegree = 3f;

        [Header("CompassHeading — Heading Bug")]
        [Tooltip("Visual indicator that points toward the active waypoint heading.")]
        [SerializeField] private RectTransform headingBug;

        [Tooltip("Target heading for the active waypoint (degrees). Set at runtime.")]
        [SerializeField] private float targetHeading;

        #endregion

        #region Public API

        /// <summary>Sets the target heading shown by the heading bug (waypoint direction).</summary>
        /// <param name="heading">Target heading in degrees (0–360).</param>
        public void SetTargetHeading(float heading) => targetHeading = heading;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            float h = data.heading;

            // ── Heading text ─────────────────────────────────────────────────
            if (headingText != null)
                headingText.text = $"{h:F0}°";

            // ── Cardinal text ─────────────────────────────────────────────────
            if (cardinalText != null)
                cardinalText.text = GetCardinal(h);

            // ── Compass strip scroll ──────────────────────────────────────────
            if (compassStrip != null)
            {
                Vector2 pos   = compassStrip.anchoredPosition;
                pos.x         = -h * stripPixelsPerDegree;
                compassStrip.anchoredPosition = pos;
            }

            // ── Heading bug position ──────────────────────────────────────────
            if (headingBug != null)
            {
                float delta = Mathf.DeltaAngle(h, targetHeading);
                Vector2 pos   = headingBug.anchoredPosition;
                pos.x         = delta * stripPixelsPerDegree;
                headingBug.anchoredPosition = pos;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Converts a numeric heading (0–360°) to the nearest 8-point cardinal abbreviation.
        /// </summary>
        /// <param name="heading">Heading in degrees.</param>
        /// <returns>Cardinal direction string (N, NE, E, SE, S, SW, W, NW).</returns>
        public static string GetCardinal(float heading)
        {
            // Normalize heading to 0–360.
            heading = Mathf.Repeat(heading, 360f);
            // 8 sectors of 45° each, starting 22.5° before North.
            int sector = Mathf.RoundToInt(heading / 45f) % 8;
            return sector switch
            {
                0 => "N",
                1 => "NE",
                2 => "E",
                3 => "SE",
                4 => "S",
                5 => "SW",
                6 => "W",
                7 => "NW",
                _ => "N"
            };
        }

        #endregion
    }
}
