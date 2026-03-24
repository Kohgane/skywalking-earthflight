using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Emergency landing site database, nearest-site finder, glide path
    /// calculator, and approach guidance controller.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyLandingController : MonoBehaviour
    {
        #region Inspector

        [Header("Site Database")]
        [SerializeField] private List<EmergencyLandingSite> landingSites = new List<EmergencyLandingSite>();

        [Header("Glide Parameters")]
        [Tooltip("Best-glide speed in m/s used for range calculation.")]
        [SerializeField] private float bestGlideSpeed = 80f;

        [Tooltip("Best glide ratio (distance / altitude).")]
        [SerializeField] private float bestGlideRatio = 10f;

        [Tooltip("Minimum runway length in metres required for landing.")]
        [SerializeField] private float minRunwayLength = 600f;

        [Header("Field Detection")]
        [Tooltip("LayerMask for terrain raycasts used to detect flat fields.")]
        [SerializeField] private LayerMask terrainLayerMask = 1;

        [Tooltip("Radius scanned for flat field candidates.")]
        [SerializeField] private float fieldScanRadius = 5000f;

        [Tooltip("Number of candidate rays cast per scan.")]
        [SerializeField] private int fieldScanRays = 16;

        #endregion

        #region Events

        /// <summary>Fired when a landing site is selected for divert.</summary>
        public event Action<EmergencyLandingSite> OnLandingSiteSelected;

        /// <summary>Fired once per frame with the approach deviation for HUD display.</summary>
        public event Action<float, float> OnApproachDeviationUpdated; // lateral, vertical (degrees)

        /// <summary>Fired when touchdown is detected for scoring.</summary>
        public event Action<float, float> OnTouchdownDetected; // distance from threshold, speed

        #endregion

        #region Private State

        private EmergencyLandingSite _selectedSite;
        private Transform _aircraftTransform;
        private bool _guidanceActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            RegisterDefaultSites();
        }

        private void Start()
        {
            _aircraftTransform = transform;
        }

        private void Update()
        {
            if (_guidanceActive && _selectedSite != null)
                UpdateApproachGuidance();
        }

        #endregion

        #region Public API

        /// <summary>Find the nearest suitable landing site for the given aircraft position and altitude.</summary>
        /// <param name="aircraftPosition">Current aircraft world position.</param>
        /// <param name="currentAltitude">Current altitude in metres.</param>
        /// <param name="requireServices">Whether the site must have emergency services.</param>
        /// <returns>Best candidate <see cref="EmergencyLandingSite"/>, or null if none reachable.</returns>
        public EmergencyLandingSite FindNearestSite(Vector3 aircraftPosition, float currentAltitude,
            bool requireServices = false)
        {
            float glideRange = currentAltitude * bestGlideRatio;
            EmergencyLandingSite best = null;
            float bestScore = float.MaxValue;

            foreach (var site in landingSites)
            {
                float dist = Vector3.Distance(aircraftPosition, site.position);
                if (dist > glideRange) continue;
                if (site.runwayLength < minRunwayLength && !site.isWaterLanding && !site.isFieldLanding) continue;
                if (requireServices && !site.hasEmergencyServices) continue;

                float score = dist - (site.hasEmergencyServices ? 1000f : 0f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = site;
                }
            }

            // Dynamic field detection if no formal site reachable
            if (best == null)
                best = DetectNearestFlatField(aircraftPosition, currentAltitude, glideRange);

            return best;
        }

        /// <summary>Select a landing site and begin approach guidance.</summary>
        public void SelectLandingSite(EmergencyLandingSite site)
        {
            _selectedSite = site;
            _guidanceActive = site != null;
            OnLandingSiteSelected?.Invoke(site);
        }

        /// <summary>Compute the maximum glide range from the given altitude.</summary>
        /// <param name="altitudeMetres">Altitude in metres above ground.</param>
        /// <returns>Maximum ground range in metres.</returns>
        public float ComputeGlideRange(float altitudeMetres) => altitudeMetres * bestGlideRatio;

        /// <summary>
        /// Returns true when the aircraft can reach the given site under best-glide.
        /// </summary>
        public bool CanMakeIt(Vector3 aircraftPosition, float altitude, EmergencyLandingSite site)
        {
            if (site == null) return false;
            float dist = Vector3.Distance(aircraftPosition, site.position);
            return dist <= ComputeGlideRange(altitude);
        }

        /// <summary>Report a touchdown event for scoring.</summary>
        /// <param name="touchdownPosition">World position of touchdown.</param>
        /// <param name="touchdownSpeed">Speed in m/s at touchdown.</param>
        public void ReportTouchdown(Vector3 touchdownPosition, float touchdownSpeed)
        {
            float dist = _selectedSite != null
                ? Vector3.Distance(touchdownPosition, _selectedSite.position)
                : 0f;
            OnTouchdownDetected?.Invoke(dist, touchdownSpeed);
        }

        #endregion

        #region Private Helpers

        private void UpdateApproachGuidance()
        {
            if (_aircraftTransform == null || _selectedSite == null) return;

            Vector3 acPos  = _aircraftTransform.position;
            Vector3 target = _selectedSite.position;
            Vector3 toTarget = target - acPos;

            // Lateral deviation
            float runwayRad   = _selectedSite.runwayHeading * Mathf.Deg2Rad;
            Vector3 runway    = new Vector3(Mathf.Sin(runwayRad), 0f, Mathf.Cos(runwayRad));
            float lateral     = Vector3.Cross(runway, toTarget.normalized).y * Vector3.Angle(runway, toTarget);

            // Vertical deviation (3° glideslope)
            float horizontal  = new Vector2(toTarget.x, toTarget.z).magnitude;
            float idealAlt    = horizontal * Mathf.Tan(3f * Mathf.Deg2Rad);
            float vertDev     = (acPos.y - target.y) - idealAlt;

            OnApproachDeviationUpdated?.Invoke(lateral, vertDev);
        }

        private EmergencyLandingSite DetectNearestFlatField(Vector3 origin, float altitude, float maxRange)
        {
            for (int i = 0; i < fieldScanRays; i++)
            {
                float angle = i * (360f / fieldScanRays) * Mathf.Deg2Rad;
                float radius = Mathf.Min(maxRange, fieldScanRadius);
                Vector3 dir = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                Vector3 candidate = origin + dir;

                if (Physics.Raycast(candidate + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 600f, terrainLayerMask))
                {
                    if (hit.normal.y > 0.95f) // nearly flat
                    {
                        return new EmergencyLandingSite
                        {
                            siteId           = $"field_{i}",
                            displayNameKey   = "emergency_site_field",
                            position         = hit.point,
                            runwayLength     = 1000f,
                            runwayHeading    = 0f,
                            hasEmergencyServices = false,
                            isFieldLanding   = true
                        };
                    }
                }
            }
            return null;
        }

        private void RegisterDefaultSites()
        {
            if (landingSites.Count > 0) return;

            landingSites.Add(new EmergencyLandingSite
            {
                siteId = "default_airport_main", displayNameKey = "emergency_site_main_airport",
                position = Vector3.zero, runwayLength = 3000f, runwayHeading = 90f,
                hasEmergencyServices = true,
                availableRescue = new List<RescueUnitType> { RescueUnitType.FireTruck, RescueUnitType.Ambulance }
            });

            landingSites.Add(new EmergencyLandingSite
            {
                siteId = "water_ditching_bay", displayNameKey = "emergency_site_bay",
                position = new Vector3(0f, 0f, 5000f), runwayLength = 0f, runwayHeading = 0f,
                hasEmergencyServices = true, isWaterLanding = true,
                availableRescue = new List<RescueUnitType> { RescueUnitType.CoastGuard, RescueUnitType.Helicopter }
            });
        }

        #endregion
    }
}
