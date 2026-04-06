// CockpitCreakController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Structural sounds: airframe creak under G-load, turbulence rattling, pressurisation.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates structural cockpit sounds driven by G-load and turbulence intensity:
    /// airframe creaking, panel rattling, and pressurisation effects.
    /// </summary>
    public class CockpitCreakController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Audio Sources")]
        [SerializeField] private AudioSource creakSource;
        [SerializeField] private AudioSource rattleSource;
        [SerializeField] private AudioSource pressurisationCreakSource;

        [Header("G-Load Thresholds")]
        [Tooltip("G-load above which creaking begins.")]
        [Range(1f, 3f)] public float creakOnsetG = 1.5f;
        [Tooltip("G-load at which creak volume is maximum.")]
        [Range(2f, 9f)] public float creakMaxG   = 4f;

        [Header("Turbulence Thresholds")]
        [Tooltip("Turbulence intensity (0–1) above which rattling begins.")]
        [Range(0f, 1f)] public float rattleOnset = 0.3f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates structural audio based on current G-load and turbulence.
        /// </summary>
        /// <param name="gLoad">Current G-load (1 = normal, >1 = high G).</param>
        /// <param name="turbulenceIntensity">Turbulence intensity (0–1).</param>
        public void UpdateStructuralAudio(float gLoad, float turbulenceIntensity)
        {
            float creakVol  = Mathf.Clamp01(Mathf.InverseLerp(creakOnsetG, creakMaxG, Mathf.Abs(gLoad)));
            float rattleVol = Mathf.Clamp01(Mathf.InverseLerp(rattleOnset, 1f, turbulenceIntensity));

            SetSource(creakSource,  creakVol,  0.9f + creakVol * 0.2f);
            SetSource(rattleSource, rattleVol, 1f   + turbulenceIntensity * 0.3f);
        }

        /// <summary>
        /// Triggers a pressurisation change creak (e.g., during climb/descent transitions).
        /// </summary>
        /// <param name="intensity">Effect intensity (0–1).</param>
        public void TriggerPressurisationCreak(float intensity)
        {
            if (pressurisationCreakSource == null) return;
            pressurisationCreakSource.volume = Mathf.Clamp01(intensity);
            pressurisationCreakSource.pitch  = 1f + intensity * 0.1f;
            if (!pressurisationCreakSource.isPlaying && pressurisationCreakSource.clip != null)
                pressurisationCreakSource.PlayOneShot(pressurisationCreakSource.clip);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private static void SetSource(AudioSource src, float vol, float pitch)
        {
            if (src == null) return;
            src.volume = vol;
            src.pitch  = pitch;
            if (!src.isPlaying && src.clip != null && vol > 0.001f)
            {
                src.loop = true;
                src.Play();
            }
            else if (src.isPlaying && vol < 0.001f)
            {
                src.Pause();
            }
        }
    }
}
