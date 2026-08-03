using System;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_COCKPITHUD_AVAILABLE
using SWEF.CockpitHUD;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Central singleton that manages the Ground Proximity Warning System.
    ///
    /// <para>Runs all seven standard GPWS modes per-frame, queries terrain height
    /// via raycasts, and reads aircraft state from
    /// <c>SWEF.CockpitHUD.FlightDataProvider</c> (null-safe via
    /// <c>#if SWEF_COCKPITHUD_AVAILABLE</c>).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSManager : MonoBehaviour
    {
        #region Singleton

        public static GPWSManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Configuration")]
        [SerializeField] private GPWSConfig config = new GPWSConfig();

        [Header("References")]
        [Tooltip("Override aircraft transform. Resolved from Camera.main at runtime if null.")]
        [SerializeField] private Transform aircraftTransform;

        [Header("Terrain Raycast")]
        [SerializeField] private LayerMask terrainLayerMask = ~0;
        [SerializeField] private float maxRaycastDistance = 10000f;

        #endregion

        #region Events

        /// <summary>Fired when a new GPWS alert is raised.</summary>
        public event Action<GPWSAlert> OnGPWSAlert;

        /// <summary>Fired when a GPWS alert is cleared.</summary>
        public event Action<GPWSAlert> OnGPWSAlertCleared;

        /// <summary>Fired when a radio-altitude advisory callout triggers.</summary>
        public event Action<GPWSCalloutType> OnAltitudeCallout;

        /// <summary>Fired when windshear is detected.</summary>
        public event Action<float> OnWindshearDetected;

        /// <summary>Fired when the forward-looking terrain scan finds a threat ahead.</summary>
        public event Action<TerrainScanResult> OnTerrainAhead;

        #endregion

        #region Public Properties

        public GPWSConfig Config => config;
        public IReadOnlyList<GPWSAlert> ActiveAlerts => _activeAlerts;
        public float TerrainAltitudeFt { get; private set; }
        public float AircraftAltitudeAglFt { get; private set; }
        public float VerticalSpeedFpm { get; private set; }
        public float AirspeedKnots { get; private set; }
        public bool GearDown { get; private set; }
        public float FlapPosition { get; private set; }
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region Private State

        private readonly List<GPWSAlert> _activeAlerts = new List<GPWSAlert>();
        private GPWSModeController _modeController;
        private float _previousAltitudeAglFt;
        private float _previousAirspeedKnots;
        private float _maxAltitudeAfterTakeoffFt;
        private bool _inTakeoffPhase;

        private const float MetresToFeet = 3.28084f;
        private const float MpsToFpm = 196.8504f;
        private const float MpsToKnots = 1.94384f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (aircraftTransform == null)
            {
                var cam = Camera.main;
                if (cam != null) aircraftTransform = cam.transform;
            }

            _modeController = GetComponent<GPWSModeController>();
            if (_modeController == null)
                _modeController = gameObject.AddComponent<GPWSModeController>();
        }

        private void Update()
        {
            if (!IsEnabled || aircraftTransform == null) return;

            UpdateAircraftState();
            SampleTerrainBelow();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public API

        /// <summary>Raises a new GPWS alert or escalates an existing one.</summary>
        public void RaiseAlert(GPWSMode mode, GPWSAlertLevel level, string messageKey)
        {
            var existing = _activeAlerts.Find(a => a.mode == mode);
            if (existing != null)
            {
                if (existing.level >= level) return;
                ClearAlertInternal(existing);
            }

            var alert = new GPWSAlert(mode, level, messageKey)
            {
                terrainAltitude = TerrainAltitudeFt,
                aircraftAltitude = AircraftAltitudeAglFt
            };
            _activeAlerts.Add(alert);
            OnGPWSAlert?.Invoke(alert);
        }

        /// <summary>Clears the alert for the specified GPWS mode.</summary>
        public void ClearAlert(GPWSMode mode)
        {
            var alert = _activeAlerts.Find(a => a.mode == mode);
            if (alert != null)
                ClearAlertInternal(alert);
        }

        /// <summary>Fires an advisory altitude callout event.</summary>
        public void TriggerCallout(GPWSCalloutType callout)
        {
            OnAltitudeCallout?.Invoke(callout);
        }

        /// <summary>Fires the windshear-detected event.</summary>
        public void TriggerWindshearAlert(float intensityKts)
        {
            OnWindshearDetected?.Invoke(intensityKts);
        }

        /// <summary>Fires the terrain-ahead event with scan results.</summary>
        public void TriggerTerrainAhead(TerrainScanResult result)
        {
            OnTerrainAhead?.Invoke(result);
        }

        /// <summary>Notifies the manager that the aircraft has begun a takeoff roll.</summary>
        public void NotifyTakeoff()
        {
            _inTakeoffPhase = true;
            _maxAltitudeAfterTakeoffFt = AircraftAltitudeAglFt;
        }

        /// <summary>Notifies the manager that the aircraft is established on approach.</summary>
        public void NotifyApproachEstablished()
        {
            _inTakeoffPhase = false;
        }

        public bool IsInTakeoffPhase => _inTakeoffPhase;
        public float MaxAltitudeAfterTakeoffFt => _maxAltitudeAfterTakeoffFt;

        #endregion

        #region Aircraft State

        private void UpdateAircraftState()
        {
            _previousAltitudeAglFt = AircraftAltitudeAglFt;
            _previousAirspeedKnots = AirspeedKnots;

#if SWEF_COCKPITHUD_AVAILABLE
            var fdp = FindFirstObjectByType<FlightDataProvider>();
            if (fdp != null && fdp.CurrentData != null)
            {
                var d = fdp.CurrentData;
                AircraftAltitudeAglFt = d.altitudeAGL * MetresToFeet;
                VerticalSpeedFpm = d.verticalSpeed * MpsToFpm;
                AirspeedKnots = d.speedKnots;
            }
            else
            {
                FallbackAircraftState();
            }
#else
            FallbackAircraftState();
#endif

#if SWEF_LANDING_AVAILABLE
            var gear = FindFirstObjectByType<SWEF.Landing.LandingGearController>();
            GearDown = gear != null && gear.IsFullyDeployed;
#endif

            if (_inTakeoffPhase && AircraftAltitudeAglFt > _maxAltitudeAfterTakeoffFt)
                _maxAltitudeAfterTakeoffFt = AircraftAltitudeAglFt;
        }

        private void FallbackAircraftState()
        {
            if (aircraftTransform == null) return;
            if (Physics.Raycast(aircraftTransform.position, Vector3.down, out var hit, maxRaycastDistance, terrainLayerMask))
                AircraftAltitudeAglFt = hit.distance * MetresToFeet;
            else
                AircraftAltitudeAglFt = aircraftTransform.position.y * MetresToFeet;

            VerticalSpeedFpm = (AircraftAltitudeAglFt - _previousAltitudeAglFt) / Time.deltaTime * 60f;
        }

        private void SampleTerrainBelow()
        {
            if (aircraftTransform == null) return;
            if (Physics.Raycast(aircraftTransform.position, Vector3.down, out var hit, maxRaycastDistance, terrainLayerMask))
                TerrainAltitudeFt = (aircraftTransform.position.y - hit.distance) * MetresToFeet;
        }

        #endregion

        #region Internal

        private void ClearAlertInternal(GPWSAlert alert)
        {
            alert.isActive = false;
            _activeAlerts.Remove(alert);
            OnGPWSAlertCleared?.Invoke(alert);
        }

        #endregion
    }
}
