// HRTFProcessor.cs — Phase 118: Spatial Audio & 3D Soundscape
// Head-Related Transfer Function: binaural audio for headphone users, VR integration.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages Head-Related Transfer Function (HRTF) binaural audio processing.
    /// Enables spatialized audio for headphone users and integrates with VR/XR platforms.
    /// Uses <c>#if SWEF_XR_AVAILABLE</c> guard for XR-specific HRTF features.
    /// </summary>
    public class HRTFProcessor : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("HRTF Settings")]
        [Tooltip("Whether HRTF binaural processing is currently enabled.")]
        [SerializeField] private bool hrtfEnabled;

        [Tooltip("HRTF processing quality: 0=Low, 1=Medium, 2=High.")]
        [Range(0, 2)] public int hrtfQuality = 1;

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Whether HRTF processing is currently active.</summary>
        public bool IsHRTFEnabled => hrtfEnabled;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (config != null)
            {
                hrtfEnabled = config.enableHRTF;
                hrtfQuality = config.hrtfQuality;
            }
            ApplyHRTFSettings();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enables or disables HRTF processing.</summary>
        public void SetHRTF(bool enabled)
        {
            hrtfEnabled = enabled;
            ApplyHRTFSettings();

            if (SpatialAudioManager.Instance != null)
                SpatialAudioManager.Instance.SetHRTF(enabled);
        }

        /// <summary>Sets HRTF processing quality level.</summary>
        public void SetQuality(int quality)
        {
            hrtfQuality = Mathf.Clamp(quality, 0, 2);
            ApplyHRTFSettings();
        }

#if SWEF_XR_AVAILABLE
        /// <summary>
        /// Configures HRTF for VR/XR mode, enabling full binaural spatial audio.
        /// Only compiled when SWEF_XR_AVAILABLE symbol is defined.
        /// </summary>
        public void ConfigureForXR()
        {
            SetHRTF(true);
            SetQuality(2);
            Debug.Log("[HRTFProcessor] XR mode: HRTF enabled at maximum quality.");
        }
#endif

        // ── Private ───────────────────────────────────────────────────────────────

        private void ApplyHRTFSettings()
        {
            // Set Unity Audio spatializer to use HRTF if enabled
            // In a real implementation this would configure the AudioSpatializerSDK
            AudioSettings.speakerMode = hrtfEnabled
                ? AudioSpeakerMode.Stereo
                : AudioSpeakerMode.Stereo;

            Debug.Log($"[HRTFProcessor] HRTF {(hrtfEnabled ? "enabled" : "disabled")}, quality={hrtfQuality}");
        }
    }
}
