// RadioAudioController.cs — Phase 118: Spatial Audio & 3D Soundscape
// Radio communication audio: ATC voices, co-pilot callouts, ATIS, squelch effects.
// Namespace: SWEF.SpatialAudio

using System.Collections;
using UnityEngine;

namespace SWEF.SpatialAudio
{
    /// <summary>
    /// Manages radio communication audio: ATC transmissions, ATIS broadcast playback,
    /// squelch open/close effects, and co-pilot audio callouts.
    /// Triggers auto-ducking of non-radio audio via <see cref="AudioDynamicRange"/>.
    /// </summary>
    public class RadioAudioController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private SpatialAudioConfig config;

        [Header("Radio Source")]
        [SerializeField] private AudioSource radioSource;
        [SerializeField] private AudioClip   squelchOpenClip;
        [SerializeField] private AudioClip   squelchCloseClip;

        [Header("Radio Filter")]
        [Tooltip("Apply bandpass filter effect to simulate radio bandwidth (300–3000 Hz).")]
        public bool applyRadioFilter = true;

        [Range(0f, 1f)] public float radioVolume = 0.8f;

        // ── State ─────────────────────────────────────────────────────────────────

        private bool _isTransmitting;
        private AudioLowPassFilter  _lowPassFilter;
        private AudioHighPassFilter _highPassFilter;

        /// <summary>Whether a radio transmission is currently playing.</summary>
        public bool IsTransmitting => _isTransmitting;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (radioSource != null && applyRadioFilter)
            {
                _lowPassFilter  = radioSource.gameObject.AddComponent<AudioLowPassFilter>();
                _highPassFilter = radioSource.gameObject.AddComponent<AudioHighPassFilter>();
                _lowPassFilter.cutoffFrequency  = 3000f;
                _highPassFilter.cutoffFrequency = 300f;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Plays an ATC or co-pilot voice clip with squelch effects.
        /// </summary>
        /// <param name="clip">Voice clip to play.</param>
        public void PlayRadioTransmission(AudioClip clip)
        {
            if (clip == null) return;
            StartCoroutine(TransmissionCoroutine(clip));
        }

        /// <summary>Plays the squelch open sound (start of transmission).</summary>
        public void PlaySquelchOpen()
        {
            if (radioSource != null && squelchOpenClip != null)
                radioSource.PlayOneShot(squelchOpenClip, radioVolume * 0.5f);
        }

        /// <summary>Plays the squelch close sound (end of transmission).</summary>
        public void PlaySquelchClose()
        {
            if (radioSource != null && squelchCloseClip != null)
                radioSource.PlayOneShot(squelchCloseClip, radioVolume * 0.5f);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private IEnumerator TransmissionCoroutine(AudioClip clip)
        {
            _isTransmitting = true;
            PlaySquelchOpen();
            yield return new WaitForSeconds(squelchOpenClip != null ? squelchOpenClip.length : 0.05f);

            if (radioSource != null)
            {
                radioSource.volume = radioVolume;
                radioSource.PlayOneShot(clip);
            }
            yield return new WaitForSeconds(clip.length);

            PlaySquelchClose();
            yield return new WaitForSeconds(squelchCloseClip != null ? squelchCloseClip.length : 0.05f);
            _isTransmitting = false;
        }
    }
}
