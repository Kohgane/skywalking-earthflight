// AttitudeIndicator.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — HUD instrument that acts as an artificial horizon / attitude
    /// indicator.
    ///
    /// <para>Rotates the horizon bar by the aircraft's roll angle and shifts it
    /// vertically according to pitch.  The pitch ladder and bank angle arc are
    /// also updated each frame.</para>
    /// </summary>
    public class AttitudeIndicator : HUDInstrument
    {
        #region Inspector

        [Header("AttitudeIndicator — Horizon")]
        [Tooltip("The horizon line RectTransform — rotated by roll, shifted by pitch.")]
        [SerializeField] private RectTransform horizonBar;

        [Tooltip("Pitch ladder markings RectTransform (child of horizonBar or independent).")]
        [SerializeField] private RectTransform pitchLadder;

        [Header("AttitudeIndicator — Bank Angle")]
        [Tooltip("Arc or indicator showing bank angle.")]
        [SerializeField] private RectTransform bankAngleArc;

        [Header("AttitudeIndicator — Aircraft Symbol")]
        [Tooltip("Fixed center aircraft reference symbol (stays stationary).")]
        [SerializeField] private RectTransform aircraftSymbol;

        [Header("AttitudeIndicator — Scale")]
        [Tooltip("Pixels the horizon shifts vertically per degree of pitch.")]
        [SerializeField] private float pitchPixelsPerDegree = 5f;

        [Tooltip("Maximum pitch (degrees) shown on the pitch ladder before clamping.")]
        [SerializeField] private float maxPitchDisplay = 45f;

        #endregion

        #region HUDInstrument

        /// <inheritdoc/>
        public override void UpdateInstrument(FlightData data)
        {
            float roll  = data.roll;
            float pitch = Mathf.Clamp(data.pitch, -maxPitchDisplay, maxPitchDisplay);

            // ── Horizon bar: rotate by −roll, shift vertically by pitch ──────
            if (horizonBar != null)
            {
                horizonBar.localRotation = Quaternion.Euler(0f, 0f, -roll);

                Vector2 pos = horizonBar.anchoredPosition;
                pos.y = pitch * pitchPixelsPerDegree;
                horizonBar.anchoredPosition = pos;
            }

            // ── Pitch ladder follows the horizon bar displacement ─────────────
            if (pitchLadder != null)
            {
                pitchLadder.localRotation = Quaternion.Euler(0f, 0f, -roll);
                Vector2 pos = pitchLadder.anchoredPosition;
                pos.y = pitch * pitchPixelsPerDegree;
                pitchLadder.anchoredPosition = pos;
            }

            // ── Bank angle arc rotates with roll ─────────────────────────────
            if (bankAngleArc != null)
                bankAngleArc.localRotation = Quaternion.Euler(0f, 0f, -roll);

            // Aircraft symbol stays fixed — no update needed.
        }

        #endregion
    }
}
