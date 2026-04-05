// NPCAudioController.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Distance-based engine audio, Doppler effect, and radio chatter ambiance.
// Namespace: SWEF.NPCTraffic

using UnityEngine;

namespace SWEF.NPCTraffic
{
    /// <summary>
    /// Phase 110 — Controls the audio for a single NPC aircraft.
    /// Provides distance-attenuated engine sound with a simulated Doppler pitch
    /// shift, and triggers radio chatter ambiance for nearby aircraft.
    /// Attach to the same GameObject as <see cref="NPCAircraftController"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class NPCAudioController : MonoBehaviour
    {
        #region Inspector

        [Header("Engine Audio")]
        [Tooltip("AudioClip to loop as the engine sound.")]
        [SerializeField] private AudioClip _engineClip;

        [Tooltip("Base volume for the engine sound.")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseVolume = 0.6f;

        [Tooltip("Maximum distance at which the engine is audible (metres).")]
        [SerializeField] private float _maxAudioDistanceMetres = 5000f;

        [Header("Doppler")]
        [Tooltip("Strength of the Doppler pitch shift effect (0 = off).")]
        [Range(0f, 3f)]
        [SerializeField] private float _dopplerLevel = 1f;

        [Tooltip("Speed of sound in metres per second used for Doppler calculation.")]
        [SerializeField] private float _speedOfSoundMs = 343f;

        [Header("Radio Chatter")]
        [Tooltip("AudioClip used for radio chatter ambiance (optional).")]
        [SerializeField] private AudioClip _radioChatterClip;

        [Tooltip("Volume of radio chatter ambiance.")]
        [Range(0f, 1f)]
        [SerializeField] private float _radioChatterVolume = 0.25f;

        [Tooltip("Maximum distance at which radio chatter is audible (metres).")]
        [SerializeField] private float _radioChatterMaxDistanceMetres = 500f;

        #endregion

        #region Private State

        private AudioSource          _engineSource;
        private AudioSource          _radioSource;
        private NPCAircraftController _controller;
        private Vector3              _lastPosition;
        private float                _lastUpdateTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _controller   = GetComponent<NPCAircraftController>();
            _engineSource = GetComponent<AudioSource>();
            ConfigureEngineSource();

            // Secondary audio source for radio chatter
            _radioSource = gameObject.AddComponent<AudioSource>();
            ConfigureRadioSource();
        }

        private void OnEnable()
        {
            if (_engineClip != null)
            {
                _engineSource.clip = _engineClip;
                _engineSource.Play();
            }

            _lastPosition   = transform.position;
            _lastUpdateTime = Time.time;
        }

        private void OnDisable()
        {
            _engineSource.Stop();
            _radioSource.Stop();
        }

        private void Update()
        {
            UpdateEngineAudio();
        }

        #endregion

        #region Private — Engine Audio

        private void ConfigureEngineSource()
        {
            _engineSource.loop                  = true;
            _engineSource.spatialBlend          = 1f;   // full 3D
            _engineSource.maxDistance           = _maxAudioDistanceMetres;
            _engineSource.rolloffMode           = AudioRolloffMode.Logarithmic;
            _engineSource.dopplerLevel          = _dopplerLevel;
            _engineSource.volume                = 0f;
            _engineSource.playOnAwake           = false;
        }

        private void ConfigureRadioSource()
        {
            _radioSource.loop          = true;
            _radioSource.spatialBlend  = 1f;
            _radioSource.maxDistance   = _radioChatterMaxDistanceMetres;
            _radioSource.rolloffMode   = AudioRolloffMode.Logarithmic;
            _radioSource.volume        = 0f;
            _radioSource.playOnAwake   = false;

            if (_radioChatterClip != null)
            {
                _radioSource.clip = _radioChatterClip;
                _radioSource.Play();
            }
        }

        private void UpdateEngineAudio()
        {
            if (_controller == null || _controller.Data == null) return;
            if (!_controller.Data.IsVisible)
            {
                _engineSource.volume = 0f;
                _radioSource.volume  = 0f;
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            float dist = Vector3.Distance(transform.position, mainCam.transform.position);
            float t    = 1f - Mathf.Clamp01(dist / _maxAudioDistanceMetres);

            // Speed-based volume scaling (faster = louder)
            float speedFactor = Mathf.Clamp01(
                _controller.Data.SpeedKnots / 500f);

            _engineSource.volume = _baseVolume * t * speedFactor;

            // Simulate Doppler by computing approach/recession speed
            float dt = Time.time - _lastUpdateTime;
            if (dt > 0.05f)
            {
                Vector3 velocity   = (transform.position - _lastPosition) / dt;
                Vector3 toListener = (mainCam.transform.position - transform.position).normalized;
                float   relSpeedMs = Vector3.Dot(velocity, toListener);
                float   pitchShift = _speedOfSoundMs / Mathf.Max(1f, _speedOfSoundMs + relSpeedMs);
                _engineSource.pitch = Mathf.Clamp(pitchShift, 0.5f, 2f);

                _lastPosition   = transform.position;
                _lastUpdateTime = Time.time;
            }

            // Radio chatter fades in very close range
            float radioT         = 1f - Mathf.Clamp01(dist / _radioChatterMaxDistanceMetres);
            _radioSource.volume  = _radioChatterVolume * radioT;
        }

        #endregion
    }
}
