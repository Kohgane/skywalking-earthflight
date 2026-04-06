// SpatialAudioBridge.cs — Phase 118: Spatial Audio & 3D Soundscape
// Integration bridge with existing SWEF systems: Flight, Weather, XR, NPC Traffic, Engine.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Integration bridge that connects the Spatial Audio system with other SWEF
    /// subsystems: Flight simulation, Weather, XR/VR, NPC Traffic, and Engine.
    /// Uses <c>#if SWEF_SPATIAL_AUDIO_AVAILABLE</c> guard for cross-system references.
    /// </summary>
    public class SpatialAudioBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Spatial Audio Components")]
        [SerializeField] private SpatialAudioManager    audioManager;
        [SerializeField] private EngineSoundController  engineController;
        [SerializeField] private WindNoiseController    windController;
        [SerializeField] private WeatherAudioController weatherController;
        [SerializeField] private AudioTransitionController transitionController;
        [SerializeField] private AudioPropagationEngine propagationEngine;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _bridgeActive;

        /// <summary>Whether the bridge is actively routing data.</summary>
        public bool IsBridgeActive => _bridgeActive;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _bridgeActive = true;
        }

        // ── Public API ────────────────────────────────────────────────────────────

#if SWEF_SPATIAL_AUDIO_AVAILABLE
        /// <summary>
        /// Routes flight state data to spatial audio components.
        /// Called by the flight system each physics frame.
        /// </summary>
        /// <param name="speedMs">Airspeed in m/s.</param>
        /// <param name="altitudeM">Altitude in metres above sea level.</param>
        /// <param name="throttle">Throttle position (0–1).</param>
        /// <param name="rpm">Engine RPM.</param>
        public void OnFlightStateUpdate(float speedMs, float altitudeM, float throttle, float rpm)
        {
            if (!_bridgeActive) return;

            if (audioManager != null)
                audioManager.UpdateFlightState(altitudeM, speedMs);

            if (engineController != null)
                engineController.UpdateEngineAudio(throttle, rpm);

            if (windController != null)
                windController.UpdateWindNoise(speedMs);

            if (transitionController != null)
                transitionController.UpdateAltitudeMix(altitudeM);
        }

        /// <summary>
        /// Routes weather data to the weather audio controller.
        /// </summary>
        /// <param name="rainIntensity">Rain intensity (0–1).</param>
        /// <param name="windGusts">Wind gust intensity (0–1).</param>
        public void OnWeatherUpdate(float rainIntensity, float windGusts)
        {
            if (!_bridgeActive) return;
            if (weatherController != null)
            {
                weatherController.SetRainIntensity(rainIntensity);
                weatherController.SetWindGustIntensity(windGusts);
            }
        }

        /// <summary>
        /// Triggers a zone transition when the flight system changes environment.
        /// </summary>
        public void OnZoneChanged(AudioZoneType newZone)
        {
            if (!_bridgeActive) return;
            if (transitionController != null)
                transitionController.TransitionToZone(newZone);
        }
#endif

        /// <summary>
        /// Updates the listener position to match the player/camera transform.
        /// Safe to call regardless of compile guards.
        /// </summary>
        public void UpdateListenerTransform(Vector3 position, Quaternion rotation)
        {
            if (audioManager != null)
                audioManager.UpdateListenerPosition(position, rotation);
        }
    }
}
