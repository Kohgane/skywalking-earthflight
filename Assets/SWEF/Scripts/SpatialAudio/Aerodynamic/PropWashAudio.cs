// PropWashAudio.cs — Phase 118: Spatial Audio & 3D Soundscape
// Propeller/rotor wash audio with directional wash sound and ground effect changes.
// Namespace: SWEF.SpatialAudio

using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Generates propeller and rotor wash audio. Modulates wash volume and pitch
    /// based on RPM, ground proximity (ground effect) and disc loading.
    /// </summary>
    public class PropWashAudio : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Audio Sources")]
        [SerializeField] private AudioSource washSource;
        [SerializeField] private AudioSource groundEffectSource;

        [Header("Thresholds")]
        [Tooltip("RPM below which wash sound is silent.")]
        [Range(0f, 500f)] public float minRpm = 100f;
        [Tooltip("RPM at which wash sound reaches maximum volume.")]
        [Range(100f, 5000f)] public float maxRpm = 2700f;
        [Tooltip("Altitude (m AGL) below which ground effect audio activates.")]
        [Range(1f, 30f)] public float groundEffectAltitude = 10f;
        [Tooltip("Maximum wash volume (0–1).")]
        [Range(0f, 1f)] public float maxWashVolume = 0.7f;

        // ── State ─────────────────────────────────────────────────────────────────

        private float _currentRpm;
        private float _altitudeAgl;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates prop wash audio.
        /// </summary>
        /// <param name="rpm">Current propeller RPM.</param>
        /// <param name="altitudeAgl">Altitude above ground in metres.</param>
        public void UpdatePropWash(float rpm, float altitudeAgl)
        {
            _currentRpm  = Mathf.Max(0f, rpm);
            _altitudeAgl = Mathf.Max(0f, altitudeAgl);

            float rpmNorm = Mathf.InverseLerp(minRpm, maxRpm, _currentRpm);
            float washVol = rpmNorm * maxWashVolume;
            float pitch   = 0.8f + rpmNorm * 0.5f;

            SetSource(washSource, washVol, pitch);

            bool inGroundEffect = _altitudeAgl < groundEffectAltitude && rpmNorm > 0.1f;
            float geBlend = inGroundEffect
                ? Mathf.InverseLerp(groundEffectAltitude, 0f, _altitudeAgl) * washVol
                : 0f;
            SetSource(groundEffectSource, geBlend, pitch * 0.85f);
        }

        /// <summary>Returns the normalised RPM wash level (0–1).</summary>
        public float GetWashLevel() =>
            Mathf.InverseLerp(minRpm, maxRpm, _currentRpm);

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
        }
    }
}
