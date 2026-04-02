// FlightPlanManager.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_ATC_AVAILABLE
using SWEF.ATC;
#endif

#if SWEF_WEATHER_AVAILABLE
using SWEF.Weather;
#endif

#if SWEF_DISASTER_AVAILABLE
using SWEF.NaturalDisaster;
#endif

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — Central singleton that manages the complete lifecycle of a flight
    /// plan: creation, editing, ATC filing, activation, in-flight waypoint tracking,
    /// fuel/ETA calculations, diversion handling, and alert broadcasting.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class FlightPlanManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static FlightPlanManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Plan Library")]
        [Tooltip("Plan templates pre-loaded at startup.")]
        public List<FlightPlanData> preloadedPlans = new List<FlightPlanData>();

        [Header("References")]
        [Tooltip("Player aircraft transform — auto-found if null.")]
        [SerializeField] private Transform _playerTransform;

        #endregion

        #region Public State

        /// <summary>The currently loaded (active or pending) flight plan route.</summary>
        public FlightPlanRoute activePlan { get; private set; }

        /// <summary>All plans stored in the local library.</summary>
        public IReadOnlyList<FlightPlanRoute> savedPlans => _savedPlans;

        /// <summary>Index of the next waypoint that must be reached.</summary>
        public int activeWaypointIndex { get; private set; }

        /// <summary>The next waypoint the aircraft must fly to.</summary>
        public FlightPlanWaypoint ActiveWaypoint =>
            activePlan != null
            && activeWaypointIndex >= 0
            && activeWaypointIndex < activePlan.waypoints.Count
                ? activePlan.waypoints[activeWaypointIndex]
                : null;

        /// <summary>Current status of the active plan.</summary>
        public FlightPlanStatus currentStatus =>
            activePlan != null ? activePlan.status : FlightPlanStatus.Draft;

        #endregion

        #region Events

        /// <summary>Raised whenever the active plan's status changes.</summary>
        public event Action<FlightPlanStatus> OnPlanStatusChanged;

        /// <summary>Raised when the aircraft captures a waypoint and advances.</summary>
        public event Action<FlightPlanWaypoint> OnWaypointCaptured;

        /// <summary>Raised when the aircraft is within approach distance of the next waypoint.</summary>
        public event Action<FlightPlanWaypoint> OnWaypointApproaching;

        /// <summary>Raised for informational / advisory alerts during flight.</summary>
        public event Action<FlightPlanAlertType> OnPlanAlert;

        #endregion

        #region Private State

        private readonly List<FlightPlanRoute> _savedPlans = new List<FlightPlanRoute>();
        private Coroutine _waypointCheckCoroutine;
        private Coroutine _etaRecalcCoroutine;
        private bool _approachAlertFired;

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

            foreach (var data in preloadedPlans)
                if (data?.route != null)
                    _savedPlans.Add(data.route);

            Debug.Log("[SWEF] FlightPlanManager: initialised.");
        }

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null)
                {
                    _playerTransform = fc.transform;
                    Debug.Log("[SWEF] FlightPlanManager: auto-found FlightController as player transform.");
                }
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        #endregion

        #region Plan Construction

        /// <summary>
        /// Creates a new draft flight plan between <paramref name="departure"/> and
        /// <paramref name="arrival"/> ICAO codes, assigns a GUID, and loads it as the
        /// active plan.  Waypoints are auto-generated from departure/arrival positions
        /// when a <see cref="NavigationDatabase"/> is available.
        /// </summary>
        public void CreatePlan(string departure, string arrival)
        {
            var plan = new FlightPlanRoute
            {
                planId           = Guid.NewGuid().ToString(),
                departureAirport = departure,
                arrivalAirport   = arrival,
                status           = FlightPlanStatus.Draft,
                cruiseAltitude   = FlightPlanConfig.DefaultCruiseAltitudeFt,
                cruiseSpeed      = FlightPlanConfig.DefaultCruiseSpeedKts
            };

            // Auto-add departure and arrival as airport waypoints
            plan.waypoints.Clear();
            plan.waypoints.Add(BuildAirportWaypoint(departure, WaypointCategory.Airport));
            plan.waypoints.Add(BuildAirportWaypoint(arrival,   WaypointCategory.Airport));

            plan.totalDistanceNm    = CalculateTotalDistance(plan);
            plan.estimatedTimeEnRoute = CalculateETE(plan);
            plan.fuelRequired       = CalculateFuelRequired(plan);

            LoadPlan(plan);
            Debug.Log($"[SWEF] FlightPlanManager: created plan {plan.planId} ({departure}→{arrival}).");
        }

        /// <summary>Loads an existing <see cref="FlightPlanRoute"/> as the active plan.</summary>
        public void LoadPlan(FlightPlanRoute plan)
        {
            activePlan          = plan;
            activeWaypointIndex = 0;
            _approachAlertFired = false;
            SetStatus(plan.status);
            Debug.Log($"[SWEF] FlightPlanManager: loaded plan {plan.planId}.");
        }

        #endregion

        #region Plan Filing & Activation

        /// <summary>
        /// Files the active plan with ATC.
        /// Integrates with <see cref="ATCManager"/> when the symbol
        /// <c>SWEF_ATC_AVAILABLE</c> is defined.
        /// </summary>
        public void FileFlightPlan()
        {
            if (activePlan == null)
            {
                Debug.LogWarning("[SWEF] FlightPlanManager.FileFlightPlan: no active plan.");
                return;
            }

            activePlan.filedTime = DateTime.UtcNow;
            SetStatus(FlightPlanStatus.Filed);

#if SWEF_ATC_AVAILABLE
            if (ATCManager.Instance != null)
            {
                // Notify ATC of the filed plan via the available public API.
                ATCManager.Instance.NotifyFlightPlanFiled(activePlan.callsign,
                                                          activePlan.departureAirport,
                                                          activePlan.arrivalAirport);
                Debug.Log("[SWEF] FlightPlanManager: filed with ATCManager.");
            }
#else
            Debug.Log("[SWEF] FlightPlanManager: filed (ATC integration not compiled).");
#endif
        }

        /// <summary>
        /// Activates the flight plan and begins waypoint proximity tracking.
        /// Switches status to <see cref="FlightPlanStatus.Active"/>.
        /// </summary>
        public void ActivatePlan()
        {
            if (activePlan == null)
            {
                Debug.LogWarning("[SWEF] FlightPlanManager.ActivatePlan: no active plan.");
                return;
            }
            if (activePlan.status == FlightPlanStatus.Active) return;

            activeWaypointIndex = 0;
            _approachAlertFired = false;
            SetStatus(FlightPlanStatus.Active);

            _waypointCheckCoroutine = StartCoroutine(WaypointProximityLoop());
            _etaRecalcCoroutine     = StartCoroutine(ETARecalcLoop());

            Debug.Log($"[SWEF] FlightPlanManager: plan {activePlan.planId} ACTIVE.");
        }

        #endregion

        #region Waypoint Manipulation

        /// <summary>Jumps navigation directly to <paramref name="waypoint"/>.</summary>
        public void DirectTo(FlightPlanWaypoint waypoint)
        {
            if (activePlan == null || waypoint == null) return;
            int idx = activePlan.waypoints.IndexOf(waypoint);
            if (idx >= 0)
                activeWaypointIndex = idx;
            else
            {
                // Insert as next waypoint
                activePlan.waypoints.Insert(activeWaypointIndex, waypoint);
            }
            _approachAlertFired = false;
            Debug.Log($"[SWEF] FlightPlanManager: DirectTo {waypoint.name}.");
        }

        /// <summary>Inserts <paramref name="wp"/> at position <paramref name="index"/> in the route.</summary>
        public void InsertWaypoint(FlightPlanWaypoint wp, int index)
        {
            if (activePlan == null || wp == null) return;
            index = Mathf.Clamp(index, 0, activePlan.waypoints.Count);
            activePlan.waypoints.Insert(index, wp);
            if (index <= activeWaypointIndex) activeWaypointIndex++;
            RefreshEstimates();
        }

        /// <summary>Removes the waypoint at <paramref name="index"/> from the active plan.</summary>
        public void RemoveWaypoint(int index)
        {
            if (activePlan == null
                || index < 0
                || index >= activePlan.waypoints.Count) return;

            activePlan.waypoints.RemoveAt(index);
            if (index < activeWaypointIndex) activeWaypointIndex--;
            activeWaypointIndex = Mathf.Clamp(activeWaypointIndex, 0,
                                              Mathf.Max(0, activePlan.waypoints.Count - 1));
            RefreshEstimates();
        }

        /// <summary>
        /// Called by the proximity loop (or externally) when the current waypoint is
        /// captured.  Advances the index and fires events.
        /// </summary>
        public void AdvanceWaypoint()
        {
            if (activePlan == null) return;

            var captured = ActiveWaypoint;
            activeWaypointIndex++;
            _approachAlertFired = false;

            if (captured != null)
                OnWaypointCaptured?.Invoke(captured);

            Debug.Log($"[SWEF] FlightPlanManager: waypoint captured → index {activeWaypointIndex}.");

            if (activeWaypointIndex >= activePlan.waypoints.Count)
            {
                SetStatus(FlightPlanStatus.Completed);
                StopAllCoroutines();
                Debug.Log("[SWEF] FlightPlanManager: plan COMPLETED.");
            }
        }

        /// <summary>
        /// Initiates an emergency diversion to <paramref name="airportId"/>.
        /// Clears remaining waypoints and inserts the diversion airport as the final destination.
        /// </summary>
        public void DivertTo(string airportId)
        {
            if (activePlan == null) return;

            // Remove all waypoints beyond the current position
            if (activeWaypointIndex < activePlan.waypoints.Count)
                activePlan.waypoints.RemoveRange(activeWaypointIndex,
                                                 activePlan.waypoints.Count - activeWaypointIndex);

            activePlan.waypoints.Add(BuildAirportWaypoint(airportId, WaypointCategory.Airport));
            activePlan.alternateAirport = airportId;
            SetStatus(FlightPlanStatus.Diverted);
            RefreshEstimates();

            Debug.Log($"[SWEF] FlightPlanManager: diverting to {airportId}.");
        }

        #endregion

        #region Distance / ETA / Fuel Queries

        /// <summary>Returns the straight-line distance to the next waypoint in nautical miles.</summary>
        public float GetDistanceToNextNm()
        {
            if (_playerTransform == null || ActiveWaypoint == null) return 0f;

            // Convert world position to approximate lat/lon using NavigationDatabase helpers
            double pLat = _playerTransform.position.z / 111320.0;
            double pLon = _playerTransform.position.x / 111320.0;
            return (float)NavigationDatabase.HaversineNm(pLat, pLon,
                                                          ActiveWaypoint.latitude,
                                                          ActiveWaypoint.longitude);
        }

        /// <summary>Returns the ETA to the next waypoint in minutes based on current speed.</summary>
        public float GetETAMinutes()
        {
            float distNm = GetDistanceToNextNm();
            float speedKts = GetCurrentSpeedKts();
            if (speedKts < 1f) return float.MaxValue;
            return (distNm / speedKts) * 60f;
        }

        /// <summary>Returns the total remaining route distance from the active waypoint to destination in nm.</summary>
        public float GetTotalRemainingNm()
        {
            if (activePlan == null) return 0f;

            float total = 0f;
            for (int i = activeWaypointIndex; i < activePlan.waypoints.Count - 1; i++)
            {
                var a = activePlan.waypoints[i];
                var b = activePlan.waypoints[i + 1];
                total += (float)NavigationDatabase.HaversineNm(a.latitude, a.longitude,
                                                                b.latitude, b.longitude);
            }
            // Add distance from current position to next waypoint
            total += GetDistanceToNextNm();
            return total;
        }

        /// <summary>
        /// Calculates the total fuel required for the active plan in kg, including a
        /// <see cref="FlightPlanConfig.MinFuelReserveRatio"/> reserve.
        /// </summary>
        public float CalculateFuelRequired()
        {
            return activePlan != null ? CalculateFuelRequired(activePlan) : 0f;
        }

        /// <summary>
        /// Validates the active plan: checks fuel sufficiency, no-fly zones from
        /// <see cref="DisasterManager"/>, and airspace constraints.
        /// Returns <c>true</c> if the plan is safe to file/activate.
        /// </summary>
        public bool ValidatePlan()
        {
            if (activePlan == null) return false;

            // Fuel check
            if (activePlan.fuelOnBoard < activePlan.fuelRequired * (1f + FlightPlanConfig.MinFuelReserveRatio))
            {
                OnPlanAlert?.Invoke(FlightPlanAlertType.FuelWarning);
                Debug.LogWarning("[SWEF] FlightPlanManager.ValidatePlan: insufficient fuel.");
                return false;
            }

#if SWEF_DISASTER_AVAILABLE
            // Disaster no-fly check along route
            if (DisasterManager.Instance != null)
            {
                foreach (var wp in activePlan.waypoints)
                {
                    // Convert to world-space approximation
                    var pos = new Vector3(
                        (float)(wp.longitude * 111320.0),
                        wp.altitude * 0.3048f,
                        (float)(wp.latitude  * 111320.0));

                    if (DisasterManager.Instance.IsInNoFlyZone(pos, wp.altitude * 0.3048f))
                    {
                        OnPlanAlert?.Invoke(FlightPlanAlertType.DisasterHazard);
                        Debug.LogWarning($"[SWEF] FlightPlanManager.ValidatePlan: waypoint {wp.name} is in a no-fly zone.");
                        return false;
                    }
                }
            }
#endif

            Debug.Log("[SWEF] FlightPlanManager.ValidatePlan: plan is valid.");
            return true;
        }

        #endregion

        #region Plan Library

        /// <summary>Saves the active plan to the local library.</summary>
        public void SaveActivePlan()
        {
            if (activePlan == null) return;
            if (!_savedPlans.Contains(activePlan))
                _savedPlans.Add(activePlan);
        }

        /// <summary>Removes a plan from the local library.</summary>
        public void DeleteSavedPlan(FlightPlanRoute plan) => _savedPlans.Remove(plan);

        #endregion

        #region Coroutines

        private IEnumerator WaypointProximityLoop()
        {
            var wait = new WaitForSeconds(FlightPlanConfig.WaypointCheckIntervalSec);
            while (activePlan != null && activePlan.status == FlightPlanStatus.Active)
            {
                yield return wait;
                if (ActiveWaypoint == null) yield break;

                float dist = GetDistanceToNextNm();

                // Approaching alert
                if (!_approachAlertFired && dist <= FlightPlanConfig.WaypointApproachingAlertNm)
                {
                    _approachAlertFired = true;
                    OnWaypointApproaching?.Invoke(ActiveWaypoint);
                    OnPlanAlert?.Invoke(FlightPlanAlertType.WaypointApproaching);
                }

                // Capture waypoint
                if (dist <= FlightPlanConfig.WaypointCaptureRadiusNm)
                    AdvanceWaypoint();

                // Fuel warning
                if (activePlan.fuelOnBoard > 0
                    && activePlan.fuelOnBoard < FlightPlanConfig.FuelWarningThresholdKg)
                    OnPlanAlert?.Invoke(FlightPlanAlertType.FuelWarning);

#if SWEF_DISASTER_AVAILABLE
                // Disaster hazard check at current position
                if (_playerTransform != null && DisasterManager.Instance != null)
                {
                    float alt = _playerTransform.position.y;
                    if (DisasterManager.Instance.IsInNoFlyZone(_playerTransform.position, alt))
                        OnPlanAlert?.Invoke(FlightPlanAlertType.DisasterHazard);
                }
#endif
            }
        }

        private IEnumerator ETARecalcLoop()
        {
            var wait = new WaitForSeconds(FlightPlanConfig.ETARecalcIntervalSec);
            while (activePlan != null && activePlan.status == FlightPlanStatus.Active)
            {
                yield return wait;
                RefreshEstimates();
                OnPlanAlert?.Invoke(FlightPlanAlertType.ETAUpdate);
            }
        }

        #endregion

        #region Private Helpers

        private void SetStatus(FlightPlanStatus status)
        {
            if (activePlan == null) return;
            activePlan.status = status;
            OnPlanStatusChanged?.Invoke(status);
        }

        private void RefreshEstimates()
        {
            if (activePlan == null) return;
            activePlan.totalDistanceNm    = CalculateTotalDistance(activePlan);
            activePlan.estimatedTimeEnRoute = CalculateETE(activePlan);
            activePlan.fuelRequired       = CalculateFuelRequired(activePlan);
        }

        private static float CalculateTotalDistance(FlightPlanRoute plan)
        {
            float total = 0f;
            for (int i = 0; i < plan.waypoints.Count - 1; i++)
            {
                var a = plan.waypoints[i];
                var b = plan.waypoints[i + 1];
                total += (float)NavigationDatabase.HaversineNm(a.latitude, a.longitude,
                                                                b.latitude, b.longitude);
            }
            return total;
        }

        private static float CalculateETE(FlightPlanRoute plan)
        {
            if (plan.cruiseSpeed < 1f) return 0f;
            return (plan.totalDistanceNm / plan.cruiseSpeed) * 60f;
        }

        private static float CalculateFuelRequired(FlightPlanRoute plan)
        {
            float wind = 0f; // default — no wind data available without WeatherManager
#if SWEF_WEATHER_AVAILABLE
            if (WeatherManager.Instance != null)
            {
                // Use a rough head/tail wind component from the route bearing
                wind = WeatherManager.Instance.GetAverageWindSpeed();
            }
#endif
            float burnKg = FuelCalculator.CalculateFuelBurn(
                plan.totalDistanceNm, plan.cruiseAltitude, plan.cruiseSpeed, wind);
            return burnKg * (1f + FlightPlanConfig.MinFuelReserveRatio);
        }

        private static FlightPlanWaypoint BuildAirportWaypoint(string icao, WaypointCategory cat)
        {
            // Try to look up from NavigationDatabase; fall back to a blank waypoint
            var wp = new FlightPlanWaypoint
            {
                waypointId = icao,
                name       = icao,
                category   = cat
            };

            if (NavigationDatabase.Instance != null)
            {
                var entry = NavigationDatabase.Instance.FindById(icao);
                if (entry != null)
                {
                    wp.latitude  = entry.latitude;
                    wp.longitude = entry.longitude;
                }
            }
            return wp;
        }

        private float GetCurrentSpeedKts()
        {
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc != null)
                return fc.CurrentSpeedMps * 1.94384f; // m/s → knots
            return activePlan?.cruiseSpeed ?? FlightPlanConfig.DefaultCruiseSpeedKts;
        }

        #endregion
    }
}
