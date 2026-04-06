// EngineAudioLayerMixer.cs — Phase 118: Spatial Audio & 3D Soundscape
// Mixes engine audio layers: intake, exhaust, turbine whine, propeller, jet wash.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Blends individual engine audio layers by throttle and RPM, producing a
    /// realistic composite engine soundscape. Layers: Intake, Exhaust, TurbineWhine,
    /// Propeller, JetWash.
    /// </summary>
    public class EngineAudioLayerMixer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Layer Sources")]
        [SerializeField] private AudioSource intakeSource;
        [SerializeField] private AudioSource exhaustSource;
        [SerializeField] private AudioSource turbineWhineSource;
        [SerializeField] private AudioSource propellerSource;
        [SerializeField] private AudioSource jetWashSource;

        [Header("Layer Volumes")]
        [Tooltip("Intake layer volume at full throttle.")]
        [Range(0f, 1f)] public float intakeMaxVolume   = 0.7f;
        [Tooltip("Exhaust layer volume at full throttle.")]
        [Range(0f, 1f)] public float exhaustMaxVolume  = 0.9f;
        [Tooltip("Turbine whine volume at max RPM.")]
        [Range(0f, 1f)] public float turbineMaxVolume  = 0.5f;
        [Tooltip("Propeller volume at cruise RPM (piston/turboprop).")]
        [Range(0f, 1f)] public float propellerMaxVolume = 0.6f;
        [Tooltip("Jet wash volume at full power.")]
        [Range(0f, 1f)] public float jetWashMaxVolume  = 0.8f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all layer volumes and pitches based on throttle (0–1) and
        /// normalised RPM (0–1).
        /// </summary>
        public void Mix(float throttle, float rpmNorm)
        {
            float t = Mathf.Clamp01(throttle);
            float r = Mathf.Clamp01(rpmNorm);

            SetLayer(intakeSource,      t * intakeMaxVolume,         1f + r * 0.2f);
            SetLayer(exhaustSource,     t * exhaustMaxVolume,         0.9f + r * 0.3f);
            SetLayer(turbineWhineSource, r * turbineMaxVolume,        0.8f + r * 0.5f);
            SetLayer(propellerSource,   r * propellerMaxVolume,       0.7f + r * 0.6f);
            SetLayer(jetWashSource,     t * t * jetWashMaxVolume,     1f);
        }

        /// <summary>Returns the weight of the named layer given current throttle/RPM.</summary>
        public static float LayerWeight(EngineSoundLayer layer, float throttle, float rpmNorm)
        {
            switch (layer)
            {
                case EngineSoundLayer.Intake:       return throttle;
                case EngineSoundLayer.Exhaust:      return throttle;
                case EngineSoundLayer.TurbineWhine: return rpmNorm;
                case EngineSoundLayer.Propeller:    return rpmNorm;
                case EngineSoundLayer.JetWash:      return throttle * throttle;
                case EngineSoundLayer.Idle:         return 1f - throttle;
                case EngineSoundLayer.Cruise:       return Mathf.InverseLerp(0.1f, 0.7f, throttle);
                case EngineSoundLayer.FullThrottle: return Mathf.InverseLerp(0.6f, 1f,   throttle);
                case EngineSoundLayer.Afterburner:  return throttle > 0.95f ? 1f : 0f;
                default:                            return 0f;
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private static void SetLayer(AudioSource src, float vol, float pitch)
        {
            if (src == null) return;
            src.volume = vol;
            src.pitch  = pitch;
            if (!src.isPlaying && src.clip != null && vol > 0.001f) src.Play();
            else if (src.isPlaying && vol < 0.001f) src.Pause();
        }
    }
}
