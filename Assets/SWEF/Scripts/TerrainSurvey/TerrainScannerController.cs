// TerrainScannerController.cs — SWEF Terrain Scanning & Geological Survey System
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Optional dependency guard — FlightController
#if SWEF_FLIGHT_AVAILABLE
using SWEF.Flight;
#endif

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Singleton MonoBehaviour that drives the terrain scan loop.
    /// Fires raycast-based terrain samples in a configurable grid below the aircraft,
    /// then raises <see cref="OnScanCompleted"/> with the collected <see cref="SurveySample"/> array.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class TerrainScannerController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TerrainScannerController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private TerrainSurveyConfig config;

        [Header("Layer Masks")]
        [Tooltip("LayerMask used for terrain raycasts.")]
        [SerializeField] private LayerMask terrainLayerMask = ~0;

        [Tooltip("Raycast origin height above the aircraft position.")]
        [SerializeField] private float raycastOriginHeight = 5000f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired immediately before a new scan cycle begins.</summary>
        public event Action OnScanStarted;

        /// <summary>
        /// Fired after a scan cycle completes, with the full sample array.
        /// Listeners should treat the array as read-only for the duration of the callback.
        /// </summary>
        public event Action<SurveySample[]> OnScanCompleted;

        /// <summary>Fired when scanning is paused (e.g., UI overlay opened).</summary>
        public event Action OnScanPaused;

        // ── State ─────────────────────────────────────────────────────────────────
        /// <summary>True while the scanner is actively running.</summary>
        public bool IsScanning { get; private set; }

        /// <summary>True if scanning has been manually paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Seconds remaining until the next scan is allowed.</summary>
        public float CooldownRemaining { get; private set; }

        private Transform _playerTransform;
        private Coroutine _scanRoutine;
        private readonly List<SurveySample> _sampleBuffer = new List<SurveySample>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ResolvePlayerTransform();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Starts the scan loop. No-op if already scanning.</summary>
        public void StartScanning()
        {
            if (IsScanning) return;
            IsScanning = true;
            IsPaused   = false;
            _scanRoutine = StartCoroutine(ScanLoop());
        }

        /// <summary>Stops the scan loop completely.</summary>
        public void StopScanning()
        {
            if (!IsScanning) return;
            IsScanning = false;
            if (_scanRoutine != null)
            {
                StopCoroutine(_scanRoutine);
                _scanRoutine = null;
            }
        }

        /// <summary>Pauses the scan loop without stopping it.</summary>
        public void PauseScanning()
        {
            if (!IsScanning || IsPaused) return;
            IsPaused = true;
            OnScanPaused?.Invoke();
        }

        /// <summary>Resumes a paused scan loop.</summary>
        public void ResumeScanning()
        {
            IsPaused = false;
        }

        // ── Internal scan loop ────────────────────────────────────────────────────

        private IEnumerator ScanLoop()
        {
            while (IsScanning)
            {
                if (IsPaused)
                {
                    yield return null;
                    continue;
                }

                if (CooldownRemaining > 0f)
                {
                    CooldownRemaining -= Time.deltaTime;
                    yield return null;
                    continue;
                }

                yield return StartCoroutine(ExecuteScan());
                CooldownRemaining = config != null ? config.cooldown : 5f;
            }
        }

        private IEnumerator ExecuteScan()
        {
            if (_playerTransform == null)
            {
                ResolvePlayerTransform();
                if (_playerTransform == null) yield break;
            }

            OnScanStarted?.Invoke();

            float   radius     = config != null ? config.scanRadius     : 500f;
            int     resolution = config != null ? config.scanResolution : 10;
            Vector3 origin     = _playerTransform.position;

            _sampleBuffer.Clear();

            float step = (resolution > 1) ? (radius * 2f / (resolution - 1)) : 0f;

            for (int xi = 0; xi < resolution; xi++)
            {
                for (int zi = 0; zi < resolution; zi++)
                {
                    float x = origin.x - radius + xi * step;
                    float z = origin.z - radius + zi * step;

                    var rayOrigin    = new Vector3(x, origin.y + raycastOriginHeight, z);
                    var rayDirection = Vector3.down;

                    SurveySample sample = default;
                    sample.position  = new Vector3(x, 0f, z);
                    sample.biomeId   = 0;
                    sample.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit,
                                        raycastOriginHeight + 1000f, terrainLayerMask))
                    {
                        sample.position = hit.point;
                        sample.altitude = hit.point.y;
                        sample.slope    = Vector3.Angle(hit.normal, Vector3.up);
                    }

                    sample.featureType = GeologicalClassifier.Classify(
                        sample.altitude, sample.slope, sample.biomeId, EstimateTemperature(sample.altitude));

                    _sampleBuffer.Add(sample);
                }

                // Yield once per row to avoid frame spikes on large grids
                yield return null;
            }

            OnScanCompleted?.Invoke(_sampleBuffer.ToArray());
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ResolvePlayerTransform()
        {
#if SWEF_FLIGHT_AVAILABLE
            if (FlightController.Instance != null)
                _playerTransform = FlightController.Instance.transform;
#else
            if (_playerTransform == null)
                _playerTransform = transform;
#endif
        }

        /// <summary>Simple lapse-rate temperature estimate for classification purposes.</summary>
        private static float EstimateTemperature(float altitudeMetres)
        {
            // Standard atmosphere: 15°C at sea level, -6.5°C per 1000m
            return 15f - (altitudeMetres / 1000f) * 6.5f;
        }
    }
}
