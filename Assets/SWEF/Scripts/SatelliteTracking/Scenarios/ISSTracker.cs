// ISSTracker.cs — Phase 114: Satellite & Space Debris Tracking
// Dedicated ISS tracking: real-time position, crew info, upcoming passes, live feed integration.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Dedicated tracker for the International Space Station (NORAD ID 25544).
    /// Provides real-time position, orbital elements, crew information,
    /// visible pass prediction, and events for live telemetry integration.
    /// </summary>
    public class ISSTracker : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>NORAD Catalogue ID of the ISS (ZARYA module).</summary>
        public const int ISSNoradId = 25544;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Tracking")]
        [Tooltip("How often (seconds) the ISS position is updated.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float updateInterval = 2f;

        [Header("Pass Prediction")]
        [Tooltip("Observer latitude for pass prediction (degrees).")]
        [SerializeField] private float observerLatDeg = 51.5f;

        [Tooltip("Observer longitude for pass prediction (degrees).")]
        [SerializeField] private float observerLonDeg = -0.1f;

        [Tooltip("Observer altitude ASL (metres).")]
        [SerializeField] private float observerAltM = 0f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised every time the ISS position is updated.</summary>
        public event Action<OrbitalState> OnISSPositionUpdated;

        /// <summary>Raised when a new visible pass is predicted.</summary>
        public event Action<SatellitePass> OnPassPredicted;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Most recent orbital state of the ISS.</summary>
        public OrbitalState CurrentState { get; private set; }

        /// <summary>TLE data for the ISS.</summary>
        public TLEData CurrentTLE { get; private set; }

        /// <summary>Simulated current crew complement.</summary>
        public int CrewCount { get; private set; } = 7;

        /// <summary>Simulated crew member names.</summary>
        public IReadOnlyList<string> CrewNames { get; private set; } = new List<string>
        {
            "Commander A. Okonkwo",
            "Pilot M. Tanaka",
            "Mission Specialist L. Petrov",
            "Mission Specialist S. Park",
            "Flight Engineer B. Müller",
            "Flight Engineer R. Singh",
            "Visiting Crew D. Chen"
        };

        // ── Private state ─────────────────────────────────────────────────────────
        private OrbitalMechanicsEngine _engine;
        private bool _hasValidTLE;

        private Coroutine _trackingCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _engine = FindObjectOfType<OrbitalMechanicsEngine>();
        }

        private void Start()
        {
            // Try to load ISS TLE from the database
            var db = SatelliteDatabase.Instance;
            if (db != null)
            {
                var record = db.FindByNoradId(ISSNoradId);
                if (record?.tle != null)
                {
                    CurrentTLE = record.tle;
                    _hasValidTLE = true;
                }
            }

            // Fall back to built-in mock TLE
            if (!_hasValidTLE)
                LoadMockTLE();

            _trackingCoroutine = StartCoroutine(TrackingLoop());
        }

        private void OnDestroy()
        {
            if (_trackingCoroutine != null) StopCoroutine(_trackingCoroutine);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates the ISS TLE data.</summary>
        public void SetTLE(TLEData tle)
        {
            CurrentTLE = tle;
            _hasValidTLE = tle != null;
        }

        /// <summary>
        /// Predicts the next visible pass of the ISS from the configured observer location.
        /// Searches up to <paramref name="searchHours"/> hours ahead.
        /// </summary>
        public SatellitePass PredictNextPass(float searchHours = 48f)
        {
            if (!_hasValidTLE || _engine == null) return null;

            var now  = DateTime.UtcNow;
            const float stepMin = 0.5f;
            bool aboveHorizon = false;
            SatellitePass pass = null;

            float obsLatRad = observerLatDeg * Mathf.Deg2Rad;
            float obsLonRad = observerLonDeg * Mathf.Deg2Rad;
            float obsR = (6371f + observerAltM / 1000f); // km

            for (float t = 0; t < searchHours * 60f; t += stepMin)
            {
                var state = _engine.Propagate(CurrentTLE, now.AddMinutes(t));
                if (state == null) continue;

                float el = CalculateElevation(state, obsLatRad, obsLonRad, obsR);

                if (!aboveHorizon && el > 0f)
                {
                    aboveHorizon = true;
                    pass = new SatellitePass
                    {
                        noradId     = ISSNoradId,
                        riseTime    = now.AddMinutes(t),
                        peakMagnitude = -4f,
                        isVisibleNight = true
                    };
                }
                else if (aboveHorizon && el <= 0f)
                {
                    if (pass != null)
                    {
                        pass.setTime = now.AddMinutes(t);
                        OnPassPredicted?.Invoke(pass);
                        return pass;
                    }
                    aboveHorizon = false;
                }
            }
            return pass;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private IEnumerator TrackingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);
                if (_hasValidTLE && _engine != null)
                {
                    CurrentState = _engine.Propagate(CurrentTLE, DateTime.UtcNow);
                    OnISSPositionUpdated?.Invoke(CurrentState);
                }
            }
        }

        private float CalculateElevation(OrbitalState state, float obsLatRad,
                                          float obsLonRad, float obsR)
        {
            float satLatRad = state.latitudeDeg * Mathf.Deg2Rad;
            float satLonRad = state.longitudeDeg * Mathf.Deg2Rad;
            float satR      = obsR + state.altitudeKm;

            // Vector from Earth centre to observer
            var obsVec = new Vector3(
                obsR * Mathf.Cos(obsLatRad) * Mathf.Cos(obsLonRad),
                obsR * Mathf.Sin(obsLatRad),
                obsR * Mathf.Cos(obsLatRad) * Mathf.Sin(obsLonRad));

            // Vector from Earth centre to satellite
            var satVec = new Vector3(
                satR * Mathf.Cos(satLatRad) * Mathf.Cos(satLonRad),
                satR * Mathf.Sin(satLatRad),
                satR * Mathf.Cos(satLatRad) * Mathf.Sin(satLonRad));

            var toSat = satVec - obsVec;
            float angle = Vector3.Angle(obsVec, toSat) - 90f;
            return -angle; // elevation above horizon
        }

        private void LoadMockTLE()
        {
            CurrentTLE = new TLEData
            {
                name                  = "ISS (ZARYA)",
                noradId               = ISSNoradId,
                inclinationDeg        = 51.64,
                raanDeg               = 347.43,
                eccentricity          = 0.0001798,
                argOfPerigeeDeg       = 356.3,
                meanAnomalyDeg        = 119.5,
                meanMotionRevPerDay   = 15.498,
                epochJulian           = 2460310.0,
                bstar                 = 1.027e-4,
                revNumberAtEpoch      = 44299
            };
            _hasValidTLE = true;
        }
    }
}
