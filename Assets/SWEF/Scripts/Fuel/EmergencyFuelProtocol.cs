// EmergencyFuelProtocol.cs — SWEF Fuel & Energy Management System (Phase 69)
using System;
using UnityEngine;

#if SWEF_LANDING_AVAILABLE
using SWEF.Landing;
#endif

namespace SWEF.Fuel
{
    /// <summary>
    /// Phase 69 — Handles emergency fuel scenarios: switches to the reserve tank,
    /// limits engine power to conserve fuel, and calculates whether the aircraft
    /// can glide to the nearest airport.
    ///
    /// <para>Queries <c>SWEF.Landing.AirportRegistry</c> (Phase 68) when available
    /// (compile with <c>SWEF_LANDING_AVAILABLE</c> to enable that integration).</para>
    /// </summary>
    public class EmergencyFuelProtocol : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [Tooltip("FuelManager on this aircraft. Leave null to use FuelManager.Instance.")]
        [SerializeField] private FuelManager fuelManager;

        [Header("Reserve")]
        [Tooltip("Litres kept in the reserve tank that normal consumption cannot draw from.")]
        [SerializeField] private float emergencyReserve = FuelConfig.EmergencyReserve;

        [Tooltip("ID of the tank designated as the reserve tank (e.g. \"Reserve\").")]
        [SerializeField] private string reserveTankId = "Reserve";

        [Header("Power Limits")]
        [Tooltip("Maximum throttle fraction (0–1) allowed when emergency is active. " +
                 "Limits engine power to conserve remaining fuel.")]
        [SerializeField] private float enginePowerReduction = FuelConfig.EmergencyPowerReduction;

        [Header("Glide")]
        [Tooltip("Aircraft glide ratio (horizontal distance / altitude loss) used for range estimation.")]
        [SerializeField] private float glideOptimization = FuelConfig.GlideRatioDefault;

        #endregion

        #region Public State

        /// <summary>Whether the emergency fuel protocol is currently active.</summary>
        public bool IsEmergencyActive { get; private set; }

        /// <summary>
        /// Maximum throttle allowed while emergency is active.
        /// Returns 1 when the protocol is inactive.
        /// </summary>
        public float ThrottleLimit =>
            IsEmergencyActive ? enginePowerReduction : 1f;

        /// <summary>Glide ratio (horizontal distance / altitude loss) used for range estimation.</summary>
        public float GlideRatio => glideOptimization;

#if SWEF_LANDING_AVAILABLE
        /// <summary>Nearest airport data resolved when the emergency activated.</summary>
        public AirportData NearestAirport { get; private set; }
#endif

        /// <summary>Metres to the nearest airport (0 if unavailable or not computed).</summary>
        public float DistanceToNearest { get; private set; }

        /// <summary>
        /// Whether the estimated glide range is sufficient to reach the nearest airport.
        /// </summary>
        public bool CanReachAirport { get; private set; }

        #endregion

        #region Events

        /// <summary>Raised when the emergency protocol is activated or deactivated.</summary>
        public event Action<bool> OnEmergencyProtocolChanged;

#if SWEF_LANDING_AVAILABLE
        /// <summary>Raised when the divert airport is resolved.</summary>
        public event Action<AirportData> OnDivertAirportSet;
#endif

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (fuelManager == null)
                fuelManager = FuelManager.Instance;

            if (fuelManager != null)
                fuelManager.OnFuelWarningChanged += HandleWarningChanged;
        }

        private void OnDestroy()
        {
            if (fuelManager != null)
                fuelManager.OnFuelWarningChanged -= HandleWarningChanged;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Activates the emergency fuel protocol:
        /// <list type="bullet">
        ///   <item>Switches the active tank to the reserve tank (if found).</item>
        ///   <item>Limits engine power to <see cref="enginePowerReduction"/>.</item>
        ///   <item>Resolves the nearest airport and evaluates glide range.</item>
        /// </list>
        /// </summary>
        public void ActivateEmergencyProtocol()
        {
            if (IsEmergencyActive) return;

            IsEmergencyActive = true;

            // Switch to reserve tank.
            if (fuelManager != null)
            {
                FuelTank reserveTank = FindReserveTank();
                if (reserveTank != null && !reserveTank.isEmpty)
                    fuelManager.SetActiveTank(reserveTank);
            }

            // Resolve nearest airport and glide feasibility.
            ResolveNearestAirport();

            OnEmergencyProtocolChanged?.Invoke(true);
        }

        /// <summary>Deactivates the emergency fuel protocol and restores normal throttle limits.</summary>
        public void DeactivateEmergencyProtocol()
        {
            if (!IsEmergencyActive) return;

            IsEmergencyActive = false;
            OnEmergencyProtocolChanged?.Invoke(false);
        }

        /// <summary>
        /// Estimates the horizontal glide range in metres from the current altitude.
        /// </summary>
        /// <param name="currentAltitude">Aircraft altitude above the terrain/ground in metres.</param>
        /// <param name="glideRatio">
        /// Glide ratio to use.  Pass 0 to fall back to <see cref="glideOptimization"/>.
        /// </param>
        /// <returns>Estimated glide range in metres.</returns>
        public float CalculateGlideRange(float currentAltitude, float glideRatio = 0f)
        {
            float ratio = glideRatio > 0f ? glideRatio : glideOptimization;
            return Mathf.Max(0f, currentAltitude * ratio);
        }

        #endregion

        #region Private Helpers

        private void HandleWarningChanged(FuelWarningLevel level)
        {
            if (level == FuelWarningLevel.Critical || level == FuelWarningLevel.Empty)
                ActivateEmergencyProtocol();
        }

        private FuelTank FindReserveTank()
        {
            if (fuelManager == null) return null;

            foreach (var tank in fuelManager.Tanks)
                if (string.Equals(tank.tankId, reserveTankId,
                                  StringComparison.OrdinalIgnoreCase))
                    return tank;

            return null;
        }

        private void ResolveNearestAirport()
        {
#if SWEF_LANDING_AVAILABLE
            if (AirportRegistry.Instance != null)
            {
                NearestAirport = AirportRegistry.Instance.GetNearestAirport(transform.position);
                if (NearestAirport != null)
                {
                    // Note: AirportData stores lat/lon in decimal degrees.
                    // This is a coarse Euclidean approximation for in-game use;
                    // replace with a proper geodesic calculation if precision is required.
                    // x ← longitude (East/West), y ← elevation, z ← latitude (North/South)
                    DistanceToNearest =
                        Vector3.Distance(transform.position,
                                         new Vector3((float)NearestAirport.longitude,
                                                     NearestAirport.elevation,
                                                     (float)NearestAirport.latitude));

                    float glideRange = CalculateGlideRange(transform.position.y);
                    CanReachAirport  = glideRange >= DistanceToNearest;

                    OnDivertAirportSet?.Invoke(NearestAirport);
                }
            }
#else
            // Landing system not available; skip airport resolution.
            DistanceToNearest = 0f;
            CanReachAirport   = false;
#endif
        }

        #endregion
    }
}
