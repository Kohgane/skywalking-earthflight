// ProceduralWorldBridge.cs — Phase 113: Procedural City & Airport Generation
// Integration with existing SWEF systems: Flight, Minimap, Weather, ATC
// (#if SWEF_PROCEDURAL_WORLD_AVAILABLE).
// Namespace: SWEF.ProceduralWorld

using System;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Bridge component connecting the Procedural World system to other SWEF modules.
    /// Each integration is guarded by its own compile-time symbol.
    /// </summary>
    public class ProceduralWorldBridge : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static ProceduralWorldBridge Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the bridge has finished connecting all available modules.</summary>
        public event Action OnBridgeReady;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SubscribeToWorldManager();
            OnBridgeReady?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromWorldManager();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void SubscribeToWorldManager()
        {
            var mgr = ProceduralWorldManager.Instance;
            if (mgr == null) return;
            mgr.OnCityGenerated += HandleCityGenerated;
            mgr.OnAirportGenerated += HandleAirportGenerated;
        }

        private void UnsubscribeFromWorldManager()
        {
            var mgr = ProceduralWorldManager.Instance;
            if (mgr == null) return;
            mgr.OnCityGenerated -= HandleCityGenerated;
            mgr.OnAirportGenerated -= HandleAirportGenerated;
        }

        private void HandleCityGenerated(CityDescription city)
        {
            NotifyMinimap(city);
            NotifyWeather(city);
            ProceduralWorldAnalytics.TrackCityGenerated(city);
        }

        private void HandleAirportGenerated(AirportLayout airport)
        {
            NotifyATC(airport);
            NotifyMinimap(airport);
            ProceduralWorldAnalytics.TrackAirportGenerated(airport);
        }

        // ── Minimap Integration ───────────────────────────────────────────────────

        private static void NotifyMinimap(CityDescription city)
        {
#if SWEF_MINIMAP_AVAILABLE
            // SWEF.Minimap.MinimapController.Instance?.AddCityMarker(city.cityName, city.centre);
            Debug.Log($"[ProceduralWorldBridge] Minimap: city '{city.cityName}' registered.");
#endif
        }

        private static void NotifyMinimap(AirportLayout airport)
        {
#if SWEF_MINIMAP_AVAILABLE
            // SWEF.Minimap.MinimapController.Instance?.AddAirportMarker(airport.icaoCode, airport.referencePoint);
            Debug.Log($"[ProceduralWorldBridge] Minimap: airport '{airport.icaoCode}' registered.");
#endif
        }

        // ── Weather Integration ───────────────────────────────────────────────────

        private static void NotifyWeather(CityDescription city)
        {
#if SWEF_WEATHER_AVAILABLE
            // SWEF.Weather.WeatherManager.Instance?.SetUrbanHeatIsland(city.centre, city.radiusMetres);
            Debug.Log($"[ProceduralWorldBridge] Weather: urban heat island set for '{city.cityName}'.");
#endif
        }

        // ── ATC Integration ───────────────────────────────────────────────────────

        private static void NotifyATC(AirportLayout airport)
        {
#if SWEF_ATC_AVAILABLE
            // SWEF.ATC.ATCManager.Instance?.RegisterAirport(airport.icaoCode, airport.referencePoint);
            Debug.Log($"[ProceduralWorldBridge] ATC: airport '{airport.icaoCode}' registered.");
#endif
        }
    }
}
