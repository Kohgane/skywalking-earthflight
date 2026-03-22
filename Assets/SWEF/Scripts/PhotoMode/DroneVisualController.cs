using System;
using UnityEngine;
using SWEF.Audio;
using SWEF.Haptic;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Manages the visual representation of the drone camera: 3D model positioning,
    /// propeller spin, LED indicators, gimbal animation, particle/trail effects, and
    /// spatial sound effects.
    /// </summary>
    public class DroneVisualController : MonoBehaviour
    {
        #region Constants
        private const float PropellerIdleRPM   = 3000f;
        private const float PropellerMaxRPM    = 9000f;
        private const float PropellerSmoothTime = 0.2f;
        private const float HumPitchMin        = 0.8f;
        private const float HumPitchMax        = 1.4f;
        private const float LEDBlinkInterval   = 0.5f;
        private const int   SFXIndexShutter    = 2;
        private const int   SFXIndexDeploy     = 3;
        private const int   SFXIndexRecall     = 3;
        #endregion

        #region Inspector
        [Header("Drone Model")]
        [Tooltip("Root transform of the 3-D drone model (follows DroneCameraController).")]
        [SerializeField] private Transform droneModel;

        [Header("Propellers")]
        [Tooltip("Front-left propeller transform.")]
        [SerializeField] private Transform propFL;

        [Tooltip("Front-right propeller transform.")]
        [SerializeField] private Transform propFR;

        [Tooltip("Rear-left propeller transform.")]
        [SerializeField] private Transform propRL;

        [Tooltip("Rear-right propeller transform.")]
        [SerializeField] private Transform propRR;

        [Header("Gimbal")]
        [Tooltip("Camera gimbal transform (pitch / yaw should match Camera rotation).")]
        [SerializeField] private Transform gimbal;

        [Header("LED Indicators")]
        [Tooltip("LED renderer whose material emission colour is changed by state.")]
        [SerializeField] private Renderer ledRenderer;

        [Tooltip("Material property name for emission colour.")]
        [SerializeField] private string ledEmissionProperty = "_EmissionColor";

        [Header("Particles")]
        [Tooltip("Engine exhaust particle system.")]
        [SerializeField] private ParticleSystem engineParticles;

        [Tooltip("Optional light trail VFX.")]
        [SerializeField] private TrailRenderer lightTrail;

        [Header("Shadow")]
        [Tooltip("Blob shadow projector or shadow mesh that follows the drone over terrain.")]
        [SerializeField] private Projector shadowProjector;

        [Header("References (auto-found if null)")]
        [SerializeField] private DroneCameraController droneController;
        [SerializeField] private PhotoCaptureManager   captureManager;
        [SerializeField] private AudioManager          audioManager;
        [SerializeField] private HapticManager         hapticManager;

        [Header("Audio")]
        [Tooltip("AudioSource for drone hum. Created automatically if null.")]
        [SerializeField] private AudioSource humSource;
        [SerializeField] private AudioClip   deployClip;
        [SerializeField] private AudioClip   recallClip;
        [SerializeField] private AudioClip   shutterClip;
        #endregion

        #region Private state
        private float     _targetRPM;
        private float     _currentRPM;
        private float     _rpmVelocity;
        private bool      _ledBlinkState;
        private float     _ledBlinkTimer;
        private MaterialPropertyBlock _ledPropBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (droneController == null) droneController = FindObjectOfType<DroneCameraController>();
            if (captureManager  == null) captureManager  = FindObjectOfType<PhotoCaptureManager>();
            if (audioManager    == null) audioManager    = FindObjectOfType<AudioManager>();
            if (hapticManager   == null) hapticManager   = FindObjectOfType<HapticManager>();

            EnsureHumSource();
            _ledPropBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (droneController != null)
            {
                droneController.OnDeployed    += HandleDeployed;
                droneController.OnRecalled    += HandleRecalled;
                droneController.OnBatteryLow  += HandleBatteryLow;
                droneController.OnMaxRangeReached += HandleMaxRange;
            }
            if (captureManager != null)
                captureManager.OnPhotoCaptured += HandlePhotoCaptured;
        }

        private void OnDisable()
        {
            if (droneController != null)
            {
                droneController.OnDeployed        -= HandleDeployed;
                droneController.OnRecalled        -= HandleRecalled;
                droneController.OnBatteryLow      -= HandleBatteryLow;
                droneController.OnMaxRangeReached -= HandleMaxRange;
            }
            if (captureManager != null)
                captureManager.OnPhotoCaptured -= HandlePhotoCaptured;
        }

        private void Update()
        {
            if (droneController == null || !droneController.IsDeployed) return;

            SyncModelPosition();
            UpdatePropellers();
            UpdateHumPitch();
            UpdateLEDs();
            UpdateGimbal();
            UpdateShadow();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Shows or hides the drone model (e.g., hide during photo capture to avoid
        /// self-capture artefacts).
        /// </summary>
        /// <param name="visible">True to show the model.</param>
        public void SetModelVisible(bool visible)
        {
            if (droneModel != null) droneModel.gameObject.SetActive(visible);
        }
        #endregion

        #region Event handlers
        private void HandleDeployed()
        {
            SetModelVisible(true);
            PlayOneShot(deployClip);
            engineParticles?.Play();
            if (lightTrail != null) lightTrail.emitting = true;
        }

        private void HandleRecalled()
        {
            SetModelVisible(false);
            PlayOneShot(recallClip);
            engineParticles?.Stop();
            if (lightTrail != null) lightTrail.emitting = false;
            SetLEDColor(Color.white);
        }

        private void HandleBatteryLow()
        {
            // LED colour is now driven by UpdateLEDs() blinking yellow
        }

        private void HandleMaxRange()
        {
            // LED colour driven by UpdateLEDs() blinking red
        }

        private void HandlePhotoCaptured(PhotoMetadata meta)
        {
            PlayOneShot(shutterClip);
            HapticManager.Instance?.Trigger(HapticPattern.ScreenshotSnap);

            // Briefly hide drone model during capture
            SetModelVisible(false);
            Invoke(nameof(ShowModel), 0.1f);
        }
        #endregion

        #region Private helpers
        private void SyncModelPosition()
        {
            if (droneModel == null || droneController == null) return;
            droneModel.position = droneController.transform.position;
            droneModel.rotation = droneController.transform.rotation;
        }

        private void UpdatePropellers()
        {
            if (droneController == null) return;

            _targetRPM = droneController.IsDeployed ? PropellerMaxRPM : PropellerIdleRPM;
            _currentRPM = Mathf.SmoothDamp(_currentRPM, _targetRPM, ref _rpmVelocity, PropellerSmoothTime);

            float degreesPerFrame = _currentRPM / 60f * 360f * Time.deltaTime;
            SpinProp(propFL,  degreesPerFrame);
            SpinProp(propFR, -degreesPerFrame);
            SpinProp(propRL, -degreesPerFrame);
            SpinProp(propRR,  degreesPerFrame);
        }

        private static void SpinProp(Transform prop, float degrees)
        {
            if (prop == null) return;
            prop.Rotate(Vector3.up, degrees, Space.Self);
        }

        private void UpdateHumPitch()
        {
            if (humSource == null) return;
            float t = Mathf.InverseLerp(PropellerIdleRPM, PropellerMaxRPM, _currentRPM);
            humSource.pitch = Mathf.Lerp(HumPitchMin, HumPitchMax, t);
        }

        private void UpdateLEDs()
        {
            if (droneController == null) return;

            Color target;
            float bat = droneController.BatteryNormalized;

            if (bat <= 0.10f)
            {
                // Blink red
                _ledBlinkTimer += Time.deltaTime;
                if (_ledBlinkTimer >= LEDBlinkInterval)
                {
                    _ledBlinkState = !_ledBlinkState;
                    _ledBlinkTimer = 0f;
                }
                target = _ledBlinkState ? Color.red : Color.black;
            }
            else if (bat <= 0.25f)
            {
                target = Color.yellow;
            }
            else
            {
                target = Color.green;
            }

            SetLEDColor(target);
        }

        private void SetLEDColor(Color c)
        {
            if (ledRenderer == null) return;
            _ledPropBlock.SetColor(ledEmissionProperty, c);
            ledRenderer.SetPropertyBlock(_ledPropBlock);
        }

        private void UpdateGimbal()
        {
            if (gimbal == null || droneController == null) return;
            gimbal.rotation = Quaternion.Lerp(gimbal.rotation, droneController.transform.rotation, 8f * Time.deltaTime);
        }

        private void UpdateShadow()
        {
            if (shadowProjector == null || droneController == null) return;
            // Keep shadow projector above the drone
            shadowProjector.transform.position = droneController.transform.position + Vector3.up * 0.5f;
        }

        private void EnsureHumSource()
        {
            if (humSource != null) return;
            humSource = gameObject.AddComponent<AudioSource>();
            humSource.loop        = true;
            humSource.spatialBlend = 1f;
            humSource.volume       = 0.4f;
            humSource.playOnAwake  = false;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || humSource == null) return;
            humSource.PlayOneShot(clip);
        }

        private void ShowModel() => SetModelVisible(true);
        #endregion
    }
}
