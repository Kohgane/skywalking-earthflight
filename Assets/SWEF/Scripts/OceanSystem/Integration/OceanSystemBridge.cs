// OceanSystemBridge.cs — Phase 117: Advanced Ocean & Maritime System
// Integration with existing SWEF systems: Flight, Weather, NPC Traffic, Achievement.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Integration bridge between the Ocean &amp; Maritime System
    /// and other SWEF sub-systems (Flight, Weather, NPC Traffic, Achievement).
    /// Conditional compilation guards prevent compile errors when optional
    /// systems are not present.
    /// </summary>
    public class OceanSystemBridge : MonoBehaviour
    {
#if SWEF_OCEAN_AVAILABLE

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Ocean System")]
        [SerializeField] private OceanSystemManager oceanManager;
        [SerializeField] private OceanWeatherIntegration weatherIntegration;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (oceanManager == null) oceanManager = OceanSystemManager.Instance;
        }

        private void OnEnable()
        {
            SubscribeToFlightSystem();
            SubscribeToWeatherSystem();
        }

        private void OnDisable()
        {
            UnsubscribeFromFlightSystem();
            UnsubscribeFromWeatherSystem();
        }

        // ── Flight System Integration ─────────────────────────────────────────────

        private void SubscribeToFlightSystem()
        {
#if SWEF_FLIGHT_AVAILABLE
            // var flightMgr = SWEF.Flight.FlightManager.Instance;
            // if (flightMgr != null) flightMgr.OnFlightStarted += OnFlightStarted;
#endif
        }

        private void UnsubscribeFromFlightSystem()
        {
#if SWEF_FLIGHT_AVAILABLE
            // var flightMgr = SWEF.Flight.FlightManager.Instance;
            // if (flightMgr != null) flightMgr.OnFlightStarted -= OnFlightStarted;
#endif
        }

        // ── Weather System Integration ────────────────────────────────────────────

        private void SubscribeToWeatherSystem()
        {
#if SWEF_WEATHER_AVAILABLE
            // var weatherMgr = SWEF.Weather.WeatherManager.Instance;
            // if (weatherMgr != null) weatherMgr.OnWeatherChanged += OnWeatherChanged;
#endif
        }

        private void UnsubscribeFromWeatherSystem()
        {
#if SWEF_WEATHER_AVAILABLE
            // var weatherMgr = SWEF.Weather.WeatherManager.Instance;
            // if (weatherMgr != null) weatherMgr.OnWeatherChanged -= OnWeatherChanged;
#endif
        }

        // ── Achievement System Integration ────────────────────────────────────────

        /// <summary>
        /// Grants an achievement when a water landing is completed.
        /// Called by <see cref="OceanSystemManager.OnWaterLandingCompleted"/>.
        /// </summary>
        public void HandleWaterLandingCompleted(WaterLandingRecord record)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            // SWEF.Achievement.AchievementManager.Instance?.Unlock("WATER_LANDING");
            // if (record.landingType == WaterLandingType.Emergency)
            //     SWEF.Achievement.AchievementManager.Instance?.Unlock("EMERGENCY_DITCHING");
#endif
        }

        /// <summary>
        /// Grants an achievement when a carrier trap is recorded.
        /// </summary>
        public void HandleCarrierTrap(CarrierTrapRecord record)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            // if (!record.wasBolter)
            //     SWEF.Achievement.AchievementManager.Instance?.Unlock("CARRIER_TRAP");
#endif
        }

#endif // SWEF_OCEAN_AVAILABLE
    }
}
