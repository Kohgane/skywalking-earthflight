using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Journal
{
    /// <summary>
    /// Companion to <see cref="JournalManager"/>.
    /// Handles all real-time data collection during an active flight session:
    /// altitude sampling, distance accumulation, speed tracking, and location detection.
    /// </summary>
    [RequireComponent(typeof(JournalManager))]
    public class JournalAutoRecorder : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sampling Settings")]
        [Tooltip("Interval in seconds between altitude profile samples.")]
        [SerializeField] private float altitudeSampleInterval = 5f;

        [Tooltip("Minimum flight duration (seconds) before the entry is committed.")]
        [SerializeField] private float minFlightDuration = 10f;

        [Header("GPS Precision")]
        [Tooltip("Number of decimal places used when formatting GPS coordinates.")]
        [SerializeField] private int gpsDecimalPlaces = 4;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when real-time recording begins for a new flight.</summary>
        public event Action OnRecordingStarted;

        /// <summary>Fired when real-time recording stops (flight ended or cancelled).</summary>
        public event Action OnRecordingStopped;

        // ── Public read-only state ─────────────────────────────────────────────────
        /// <summary>Whether a flight is currently being recorded.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Cumulative distance flown during the current flight in kilometres.</summary>
        public float AccumulatedDistanceKm { get; private set; }

        /// <summary>Peak speed observed during the current flight in km/h.</summary>
        public float MaxSpeedKmh { get; private set; }

        /// <summary>Running average speed in km/h.</summary>
        public float AvgSpeedKmh { get; private set; }

        /// <summary>Elapsed time in seconds since recording started.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>XP earned during the current (or most recently completed) flight.</summary>
        public int XpEarnedDuringFlight { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────────
        private FlightController _flight;
        private AltitudeController _altitude;

        private Vector3 _lastPosition;
        private float   _speedAccum;
        private int     _speedSampleCount;
        private float   _maxAltitude;
        private string  _highestAtmosphereLayer;
        private readonly List<float> _altitudeSamples = new List<float>();
        private Coroutine _altitudeSampleCoroutine;

        // ── Atmosphere layer thresholds (metres) ──────────────────────────────────
        private static readonly (float threshold, string name)[] AltitudeLayers =
        {
            (0f,      "Troposphere"),
            (12000f,  "Stratosphere"),
            (50000f,  "Mesosphere"),
            (80000f,  "Thermosphere"),
            (700000f, "Exosphere"),
        };

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _flight   = FindFirstObjectByType<FlightController>();
            _altitude = FindFirstObjectByType<AltitudeController>();
        }

        private void Update()
        {
            if (!IsRecording || _flight == null) return;

            float dt = Time.deltaTime;
            ElapsedSeconds += dt;

            // Distance accumulation.
            Vector3 pos   = _flight.transform.position;
            float   delta = Vector3.Distance(pos, _lastPosition);
            AccumulatedDistanceKm += delta / 1000f;
            _lastPosition = pos;

            // Speed tracking (convert m/s → km/h).
            float speedKmh = _flight.CurrentSpeedMps * 3.6f;
            if (speedKmh > MaxSpeedKmh) MaxSpeedKmh = speedKmh;
            _speedAccum      += speedKmh;
            _speedSampleCount++;
            AvgSpeedKmh = _speedSampleCount > 0 ? _speedAccum / _speedSampleCount : 0f;

            // Altitude tracking.
            if (_altitude != null)
            {
                float alt = _altitude.CurrentAltitudeMeters;
                if (alt > _maxAltitude) _maxAltitude = alt;
                UpdateAtmosphereLayer(alt);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins recording for the given entry.  Called by <see cref="JournalManager"/>.
        /// </summary>
        public void BeginRecording(FlightLogEntry entry)
        {
            if (IsRecording) StopRecording(entry);

            IsRecording           = true;
            AccumulatedDistanceKm = 0f;
            MaxSpeedKmh           = 0f;
            AvgSpeedKmh           = 0f;
            ElapsedSeconds        = 0f;
            XpEarnedDuringFlight  = 0;
            _speedAccum           = 0f;
            _speedSampleCount     = 0;
            _maxAltitude          = 0f;
            _highestAtmosphereLayer = AltitudeLayers[0].name;
            _altitudeSamples.Clear();

            if (_flight != null)
                _lastPosition = _flight.transform.position;

            entry.departureLocation = GetLocationString();

            _altitudeSampleCoroutine = StartCoroutine(AltitudeSampleCoroutine());
            OnRecordingStarted?.Invoke();
            Debug.Log("[SWEF] JournalAutoRecorder: Recording started.");
        }

        /// <summary>
        /// Finalises in-flight data into <paramref name="entry"/> and stops recording.
        /// Called by <see cref="JournalManager"/>.
        /// </summary>
        public void StopRecording(FlightLogEntry entry)
        {
            if (!IsRecording) return;
            IsRecording = false;

            if (_altitudeSampleCoroutine != null)
            {
                StopCoroutine(_altitudeSampleCoroutine);
                _altitudeSampleCoroutine = null;
            }

            entry.durationSeconds    = ElapsedSeconds;
            entry.distanceKm         = AccumulatedDistanceKm;
            entry.maxAltitudeM       = _maxAltitude;
            entry.avgSpeedKmh        = AvgSpeedKmh;
            entry.maxSpeedKmh        = MaxSpeedKmh;
            entry.altitudeProfile    = _altitudeSamples.ToArray();
            entry.atmosphereLayer    = _highestAtmosphereLayer;
            entry.arrivalLocation    = GetLocationString();

            OnRecordingStopped?.Invoke();
            Debug.Log($"[SWEF] JournalAutoRecorder: Recording stopped — {ElapsedSeconds:F0}s / {AccumulatedDistanceKm:F2}km");
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private IEnumerator AltitudeSampleCoroutine()
        {
            var wait = new WaitForSeconds(altitudeSampleInterval);
            while (IsRecording)
            {
                float alt = _altitude != null ? _altitude.CurrentAltitudeMeters : 0f;
                _altitudeSamples.Add(alt);
                yield return wait;
            }
        }

        private void UpdateAtmosphereLayer(float altitudeM)
        {
            for (int i = AltitudeLayers.Length - 1; i >= 0; i--)
            {
                if (altitudeM >= AltitudeLayers[i].threshold)
                {
                    if (i > IndexOf(_highestAtmosphereLayer))
                        _highestAtmosphereLayer = AltitudeLayers[i].name;
                    break;
                }
            }
        }

        private int IndexOf(string layerName)
        {
            for (int i = 0; i < AltitudeLayers.Length; i++)
                if (AltitudeLayers[i].name == layerName) return i;
            return 0;
        }

        private string GetLocationString()
        {
            if (_flight == null) return string.Empty;
            Vector3 p = _flight.transform.position;
            // Encode as compact GPS-style string.
            return $"{p.x.ToString($"F{gpsDecimalPlaces}")},{p.z.ToString($"F{gpsDecimalPlaces}")}";
        }
    }
}
