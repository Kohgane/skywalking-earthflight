// SoundscapeDebugUI.cs — Phase 118: Spatial Audio & 3D Soundscape
// Debug overlay: active audio sources, zone visualization, attenuation spheres.
// Namespace: SWEF.SpatialAudio

using UnityEngine;
using UnityEngine.UI;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Debug overlay UI displaying real-time spatial audio diagnostics:
    /// active source count, current zone, Doppler factor, reverb preset,
    /// and attenuation sphere visualisation toggles.
    /// </summary>
    public class SoundscapeDebugUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Debug UI Elements")]
        [SerializeField] private Text activeSourcesLabel;
        [SerializeField] private Text currentZoneLabel;
        [SerializeField] private Text reverbPresetLabel;
        [SerializeField] private Text altitudeLabel;
        [SerializeField] private Text speedLabel;

        [Header("Visualisation")]
        [SerializeField] private Toggle showAttenuationSpheresToggle;

        [Header("References")]
        [SerializeField] private ReverbZoneManager reverbZoneManager;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _showSpheres;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            RefreshDebugDisplay();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void RefreshDebugDisplay()
        {
            var mgr = SpatialAudioManager.Instance;
            if (mgr == null) return;

            var zoneState = mgr.CurrentZoneState;

            if (currentZoneLabel  != null) currentZoneLabel.text  = $"Zone: {zoneState.currentZone}";
            if (altitudeLabel     != null) altitudeLabel.text     = $"Alt: {zoneState.altitudeMetres:F0} m";
            if (speedLabel        != null) speedLabel.text        = $"Speed: {zoneState.speedMetresPerSecond:F1} m/s";

            if (reverbZoneManager != null && reverbPresetLabel != null)
                reverbPresetLabel.text = $"Reverb: {reverbZoneManager.CurrentPreset}";
        }
    }
}
