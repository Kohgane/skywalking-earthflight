// VRInstrumentPanel.cs — Phase 112: VR/XR Flight Experience
// VR-native instrument panel: altimeter, airspeed, attitude, compass.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Renders a physically-placed 3D instrument panel in the VR cockpit.
    /// Each instrument reads live flight data and updates its transform or
    /// material properties accordingly.
    /// </summary>
    public class VRInstrumentPanel : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Instrument Transforms")]
        [SerializeField] private Transform altimeterNeedle;
        [SerializeField] private Transform airspeedNeedle;
        [SerializeField] private Transform attitudeIndicator;
        [SerializeField] private Transform compassCard;
        [SerializeField] private Transform vsiNeedle;

        [Header("Ranges")]
        [SerializeField] private float maxAltitudeFt     = 45000f;
        [SerializeField] private float maxAirspeedKnots  = 600f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current indicated altitude in feet.</summary>
        public float AltitudeFt    { get; private set; }

        /// <summary>Current indicated airspeed in knots.</summary>
        public float AirspeedKnots { get; private set; }

        /// <summary>Current magnetic heading in degrees [0..360).</summary>
        public float HeadingDeg    { get; private set; }

        /// <summary>Current pitch angle in degrees.</summary>
        public float PitchDeg      { get; private set; }

        /// <summary>Current bank/roll angle in degrees.</summary>
        public float BankDeg       { get; private set; }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all instrument visuals with new flight data.
        /// </summary>
        public void UpdateInstruments(float altitudeFt, float airspeedKnots,
                                      float headingDeg, float pitchDeg, float bankDeg)
        {
            AltitudeFt    = altitudeFt;
            AirspeedKnots = airspeedKnots;
            HeadingDeg    = headingDeg;
            PitchDeg      = pitchDeg;
            BankDeg       = bankDeg;

            UpdateAltimeter();
            UpdateAirspeed();
            UpdateAttitude();
            UpdateCompass();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateAltimeter()
        {
            if (altimeterNeedle == null) return;
            // Each 1000 ft = 360° rotation (one full revolution per 1000 ft, 10-wrap drum)
            float angle = (AltitudeFt % 1000f) / 1000f * 360f;
            altimeterNeedle.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void UpdateAirspeed()
        {
            if (airspeedNeedle == null) return;
            float fraction = Mathf.Clamp01(AirspeedKnots / maxAirspeedKnots);
            float angle    = fraction * 300f; // 300° sweep
            airspeedNeedle.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private void UpdateAttitude()
        {
            if (attitudeIndicator == null) return;
            attitudeIndicator.localRotation = Quaternion.Euler(PitchDeg, 0f, -BankDeg);
        }

        private void UpdateCompass()
        {
            if (compassCard == null) return;
            compassCard.localRotation = Quaternion.Euler(0f, 0f, HeadingDeg);
        }
    }
}
