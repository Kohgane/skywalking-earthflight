using UnityEngine;
using SWEF.Flight;
using SWEF.Util;
using SWEF.Weather;

namespace SWEF.Audio
{
    /// <summary>
    /// Procedurally synthesises wind audio by modulating an AudioSource's volume and
    /// lowpass filter frequency based on flight speed, altitude, and weather state.
    /// Turbulence adds random volume/pitch modulation.
    /// </summary>
    public class WindAudioGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Wind Source")]
        [SerializeField] private AudioClip windNoiseClip;
        [Range(0f, 1f)]
        [SerializeField] private float baseWindVolume    = 0.3f;
        [SerializeField] private float speedMultiplier   = 0.002f;
        [SerializeField] private float turbulenceAmount  = 0.05f;

        [Header("Lowpass filter")]
        [SerializeField] private float minCutoffFreq  = 500f;
        [SerializeField] private float maxCutoffFreq  = 5000f;

        [Header("Altitude fade (thin atmosphere)")]
        [SerializeField] private float windFadeStartAlt = 30000f;
        [SerializeField] private float windFadeEndAlt   = 80000f;

        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 4f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private FlightController    flightController;
        [SerializeField] private AltitudeController  altitudeController;
        [SerializeField] private WeatherStateManager weatherStateManager;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private AudioSource         _source;
        private AudioLowPassFilter  _lowPass;
        private float               _currentVolume;
        private float               _currentCutoff;
        private float               _turbPhase;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
            if (weatherStateManager == null) weatherStateManager = FindFirstObjectByType<WeatherStateManager>();

            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.clip        = windNoiseClip;
            _source.loop        = true;
            _source.spatialBlend = 0f;
            _source.volume      = 0f;
            _source.playOnAwake = false;

            _lowPass = gameObject.GetComponent<AudioLowPassFilter>();
            if (_lowPass == null) _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.cutoffFrequency = maxCutoffFreq;

            if (windNoiseClip != null) _source.Play();
        }

        private void Update()
        {
            float dt    = Time.deltaTime;
            float speed = flightController  != null ? flightController.Velocity.magnitude : 0f;
            float alt   = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;

            // Weather intensity boost (normalized: 30 m/s gale = 2x boost)
            float weatherBoost = 1f;
            if (weatherStateManager != null && weatherStateManager.CurrentWeather != null)
                weatherBoost = 1f + weatherStateManager.CurrentWeather.windSpeedMs / 30f;

            // Target volume: speed-driven, altitude-faded
            float altFade   = 1f - Mathf.InverseLerp(windFadeStartAlt, windFadeEndAlt, alt);
            float targetVol = Mathf.Clamp01(baseWindVolume + speed * speedMultiplier * weatherBoost) * altFade;

            // Turbulence
            _turbPhase += dt * 3.7f;
            float turb = turbulenceAmount * Mathf.PerlinNoise(_turbPhase, 0.5f) * 2f - turbulenceAmount;
            targetVol = Mathf.Clamp01(targetVol + turb);

            // Target lowpass cutoff: higher speed → higher cutoff (brighter wind)
            float targetCutoff = Mathf.Lerp(minCutoffFreq, maxCutoffFreq,
                Mathf.Clamp01(speed * speedMultiplier));

            _currentVolume = ExpSmoothing.ExpLerp(_currentVolume, targetVol,    smoothSpeed, dt);
            _currentCutoff = ExpSmoothing.ExpLerp(_currentCutoff, targetCutoff, smoothSpeed, dt);

            _source.volume = _currentVolume;
            _lowPass.cutoffFrequency = _currentCutoff;
        }

        /// <summary>Exposes current wind volume for debug display.</summary>
        public float CurrentVolume => _currentVolume;

        /// <summary>Exposes current lowpass cutoff frequency for debug display.</summary>
        public float CurrentCutoffFreq => _currentCutoff;

        /// <summary>Exposes turbulence noise value (0–1) from last frame.</summary>
        public float TurbulenceLevel => turbulenceAmount;
    }
}
