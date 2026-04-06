// SatellitePassPredictor.cs — Phase 114: Satellite & Space Debris Tracking
// Visible pass prediction from player location: rise/set times, max elevation, magnitude.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Predicts visible passes of satellites over a fixed ground observer location.
    /// </summary>
    public class SatellitePassPredictor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Observer Location")]
        [SerializeField] private float observerLatDeg = 51.5f;
        [SerializeField] private float observerLonDeg = -0.1f;
        [SerializeField] private float observerAltM   = 0f;

        [Header("Prediction Parameters")]
        [Tooltip("How many hours ahead to search for passes.")]
        [Range(1f, 168f)]
        [SerializeField] private float predictionHorizonHours = 48f;

        [Tooltip("Time step (minutes) between prediction samples.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float stepSizeMin = 0.5f;

        [Tooltip("Minimum elevation for a pass to be considered visible (degrees).")]
        [Range(0f, 30f)]
        [SerializeField] private float minElevationDeg = 10f;

        // ── Private state ─────────────────────────────────────────────────────────
        private OrbitalMechanicsEngine _engine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _engine = FindObjectOfType<OrbitalMechanicsEngine>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Predicts all visible passes of the given satellite over the observer location.
        /// </summary>
        public List<SatellitePass> PredictPasses(SatelliteRecord satellite)
        {
            var results = new List<SatellitePass>();
            if (satellite?.tle == null || _engine == null) return results;

            var now = DateTime.UtcNow;
            float obsLatRad = observerLatDeg * Mathf.Deg2Rad;
            float obsLonRad = observerLonDeg * Mathf.Deg2Rad;
            float obsR = (6371f + observerAltM / 1000f);

            bool passActive = false;
            float passMaxEl = 0f;
            DateTime? riseTime = null;
            DateTime? maxElTime = null;
            float riseAz = 0f;

            for (float t = 0; t < predictionHorizonHours * 60f; t += stepSizeMin)
            {
                var time = now.AddMinutes(t);
                var state = _engine.Propagate(satellite.tle, time);
                if (state == null) continue;

                float el = CalculateElevation(state, obsLatRad, obsLonRad, obsR);
                float az = CalculateAzimuth(state, obsLatRad, obsLonRad, obsR);

                if (!passActive && el >= minElevationDeg)
                {
                    passActive  = true;
                    riseTime    = time;
                    passMaxEl   = el;
                    maxElTime   = time;
                    riseAz      = az;
                }
                else if (passActive)
                {
                    if (el > passMaxEl)
                    {
                        passMaxEl = el;
                        maxElTime = time;
                    }
                    if (el < minElevationDeg)
                    {
                        results.Add(new SatellitePass
                        {
                            noradId            = satellite.noradId,
                            riseTime           = riseTime!.Value,
                            maxElevationTime   = maxElTime!.Value,
                            setTime            = time,
                            maxElevationDeg    = passMaxEl,
                            riseAzimuthDeg     = riseAz,
                            setAzimuthDeg      = az,
                            peakMagnitude      = satellite.visualMagnitude,
                            isVisibleNight     = true
                        });
                        passActive = false;
                        passMaxEl  = 0f;
                    }
                }
            }
            return results;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float CalculateElevation(OrbitalState state, float obsLatRad,
                                          float obsLonRad, float obsR)
        {
            float satLatRad = state.latitudeDeg  * Mathf.Deg2Rad;
            float satLonRad = state.longitudeDeg * Mathf.Deg2Rad;
            float satR      = obsR + state.altitudeKm;

            var obsVec = new Vector3(
                obsR * Mathf.Cos(obsLatRad) * Mathf.Cos(obsLonRad),
                obsR * Mathf.Sin(obsLatRad),
                obsR * Mathf.Cos(obsLatRad) * Mathf.Sin(obsLonRad));
            var satVec = new Vector3(
                satR * Mathf.Cos(satLatRad) * Mathf.Cos(satLonRad),
                satR * Mathf.Sin(satLatRad),
                satR * Mathf.Cos(satLatRad) * Mathf.Sin(satLonRad));

            var toSat = satVec - obsVec;
            float angle = Vector3.Angle(obsVec, toSat) - 90f;
            return -angle;
        }

        private float CalculateAzimuth(OrbitalState state, float obsLatRad,
                                        float obsLonRad, float obsR)
        {
            float satLatRad = state.latitudeDeg  * Mathf.Deg2Rad;
            float satLonRad = state.longitudeDeg * Mathf.Deg2Rad;
            float dLon = satLonRad - obsLonRad;

            float y = Mathf.Sin(dLon) * Mathf.Cos(satLatRad);
            float x = Mathf.Cos(obsLatRad) * Mathf.Sin(satLatRad)
                    - Mathf.Sin(obsLatRad) * Mathf.Cos(satLatRad) * Mathf.Cos(dLon);

            float az = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            return (az + 360f) % 360f;
        }
    }
}
