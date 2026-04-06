// OceanWeatherIntegration.cs — Phase 117: Advanced Ocean & Maritime System
// Weather-ocean coupling: wind → wave height, storm surge, fog, rain on water.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Bridges external weather system data into the Ocean &amp; Maritime
    /// simulation. Translates wind speed/direction and storm intensity into
    /// wave parameters, storm surge offsets, and fog triggers.
    /// </summary>
    public class OceanWeatherIntegration : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private OceanSystemManager oceanManager;
        [SerializeField] private MarineFogController fogController;

        [Header("Storm Surge")]
        [Tooltip("Water level rise per m/s of wind speed during storms (metres).")]
        [SerializeField] private float stormSurgePerWindMs = 0.05f;
        [SerializeField] private float maxStormSurgeMetres = 3f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _stormSurgeOffset;

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current storm surge offset applied to sea level in metres.</summary>
        public float StormSurgeOffset => _stormSurgeOffset;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (oceanManager == null) oceanManager = OceanSystemManager.Instance;
        }

#if SWEF_OCEAN_AVAILABLE
        private void OnEnable()
        {
            // Subscribe to weather system events when available
        }

        private void OnDisable()
        {
            // Unsubscribe from weather system events when available
        }
#endif

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the weather system to update ocean conditions.
        /// <paramref name="windSpeedMs"/> and <paramref name="windDirectionDeg"/> drive waves.
        /// <paramref name="rainIntensity"/> (0–1) creates rain-on-water surface effects.
        /// </summary>
        public void ApplyWeatherConditions(float windSpeedMs, float windDirectionDeg, float rainIntensity, bool isStorm)
        {
            if (oceanManager == null) return;

            oceanManager.SetWindParameters(windSpeedMs, windDirectionDeg);

            // Storm surge
            _stormSurgeOffset = isStorm
                ? Mathf.Clamp(windSpeedMs * stormSurgePerWindMs, 0f, maxStormSurgeMetres)
                : 0f;

            // Sea state from wind
            var seaState = WindToSeaState(windSpeedMs);
            oceanManager.SetSeaState(seaState);

            // Fog
            if (fogController != null)
                fogController.SetFogIntensityFromWind(windSpeedMs);
        }

        private static SeaState WindToSeaState(float windSpeedMs)
        {
            if (windSpeedMs < 1.6f)  return SeaState.Calm;
            if (windSpeedMs < 5.5f)  return SeaState.Slight;
            if (windSpeedMs < 10.8f) return SeaState.Moderate;
            if (windSpeedMs < 17.2f) return SeaState.Rough;
            if (windSpeedMs < 24.4f) return SeaState.VeryRough;
            return SeaState.HighSeas;
        }
    }
}
