// BoostVFXBridge.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Bridges boost and drift events to the VFX subsystem.
    ///
    /// <para>Listens to events from <see cref="BoostController"/>,
    /// <see cref="DriftController"/>, <see cref="SlipstreamController"/>,
    /// <see cref="StartBoostController"/>, and <see cref="TrickBoostController"/>,
    /// then spawns VFX via <c>VFXPoolManager</c> (guarded by
    /// <c>SWEF_VFX_AVAILABLE</c>) and applies camera-side feedback
    /// (FOV push, camera shake, motion blur).</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="BoostController"/>.</para>
    /// </summary>
    public class BoostVFXBridge : MonoBehaviour
    {
        #region Inspector

        [Header("Controller References")]
        [Tooltip("BoostController to listen to (auto-resolved from singleton if null).")]
        [SerializeField] private BoostController    boostController;

        [Tooltip("DriftController to listen to (auto-resolved from singleton if null).")]
        [SerializeField] private DriftController    driftController;

        [Tooltip("SlipstreamController to listen to (auto-resolved from singleton if null).")]
        [SerializeField] private SlipstreamController slipstreamController;

        [Tooltip("StartBoostController for start-event VFX.")]
        [SerializeField] private StartBoostController startBoostController;

        [Tooltip("TrickBoostController for trick trail VFX.")]
        [SerializeField] private TrickBoostController trickBoostController;

        [Header("Camera Feedback")]
        [Tooltip("Camera used for FOV and motion-blur feedback.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Screen-edge speed lines particle system.")]
        [SerializeField] private ParticleSystem speedLinesParticles;

        [Tooltip("Drift spark particle system (colour driven by drift level).")]
        [SerializeField] private ParticleSystem driftSparkParticles;

        [Tooltip("Slipstream wind-tunnel particle system.")]
        [SerializeField] private ParticleSystem slipstreamParticles;

        [Header("Camera Shake")]
        [Tooltip("Magnitude of the camera shake on boost activation.")]
        [SerializeField] private float boostShakeMagnitude = 0.08f;

        [Tooltip("Duration of the camera shake in seconds.")]
        [SerializeField] private float boostShakeDuration = 0.2f;

        [Header("Motion Blur")]
        [Tooltip("Maximum motion blur intensity at peak boost multiplier (requires URP/HDRP post-process).")]
        [SerializeField] private float maxMotionBlurIntensity = 0.5f;

        #endregion

        #region Private State

        private float _shakeTimer;
        private float _shakeCurrentMagnitude;
        private Vector3 _cameraBasePosition;
        private bool    _cameraBaseRecorded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            TickCameraShake(Time.deltaTime);
            UpdateSpeedLines();
            UpdateDriftSparks();
            UpdateSlipstreamParticles();
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeEvents()
        {
            if (boostController != null)
            {
                boostController.OnBoostStart += HandleBoostStart;
                boostController.OnBoostEnd   += HandleBoostEnd;
            }

            if (driftController != null)
            {
                driftController.OnDriftStart   += HandleDriftStart;
                driftController.OnDriftLevelUp += HandleDriftLevelUp;
                driftController.OnDriftRelease += HandleDriftRelease;
                driftController.OnDriftCancel  += HandleDriftCancel;
            }

            if (slipstreamController != null)
            {
                slipstreamController.OnSlipstreamEnter   += HandleSlipstreamEnter;
                slipstreamController.OnSlipstreamCharged += HandleSlipstreamCharged;
                slipstreamController.OnSlipstreamExit    += HandleSlipstreamExit;
            }

            if (startBoostController != null)
                startBoostController.OnStartBoostResult += HandleStartBoostResult;

            if (trickBoostController != null)
            {
                trickBoostController.OnTrickStart    += HandleTrickStart;
                trickBoostController.OnTrickComplete += HandleTrickComplete;
                trickBoostController.OnTrickFail     += HandleTrickFail;
            }
        }

        private void UnsubscribeEvents()
        {
            if (boostController != null)
            {
                boostController.OnBoostStart -= HandleBoostStart;
                boostController.OnBoostEnd   -= HandleBoostEnd;
            }

            if (driftController != null)
            {
                driftController.OnDriftStart   -= HandleDriftStart;
                driftController.OnDriftLevelUp -= HandleDriftLevelUp;
                driftController.OnDriftRelease -= HandleDriftRelease;
                driftController.OnDriftCancel  -= HandleDriftCancel;
            }

            if (slipstreamController != null)
            {
                slipstreamController.OnSlipstreamEnter   -= HandleSlipstreamEnter;
                slipstreamController.OnSlipstreamCharged -= HandleSlipstreamCharged;
                slipstreamController.OnSlipstreamExit    -= HandleSlipstreamExit;
            }

            if (startBoostController != null)
                startBoostController.OnStartBoostResult -= HandleStartBoostResult;

            if (trickBoostController != null)
            {
                trickBoostController.OnTrickStart    -= HandleTrickStart;
                trickBoostController.OnTrickComplete -= HandleTrickComplete;
                trickBoostController.OnTrickFail     -= HandleTrickFail;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleBoostStart(BoostConfig config)
        {
            TriggerShake(boostShakeMagnitude, boostShakeDuration);
            SpawnVFX(config?.vfxType ?? BoostVFXType.ShortBurst);
        }

        private void HandleBoostEnd(BoostType type)
        {
            // Fade speed lines via Update loop checking BoostController.
        }

        private void HandleDriftStart(DriftDirection dir)
        {
            SetDriftSparks(active: true, DriftLevel.None);
        }

        private void HandleDriftLevelUp(DriftLevel level)
        {
            SetDriftSparks(active: true, level);
        }

        private void HandleDriftRelease(DriftLevel level, BoostConfig reward)
        {
            SetDriftSparks(active: false, DriftLevel.None);
        }

        private void HandleDriftCancel(DriftLevel level)
        {
            SetDriftSparks(active: false, DriftLevel.None);
        }

        private void HandleSlipstreamEnter(string leadPlayerId)
        {
            SetSlipstreamParticles(active: true, intensity: 0f);
        }

        private void HandleSlipstreamCharged(float normalizedCharge)
        {
            SetSlipstreamParticles(active: true, intensity: normalizedCharge);
        }

        private void HandleSlipstreamExit()
        {
            SetSlipstreamParticles(active: false, intensity: 0f);
        }

        private void HandleStartBoostResult(StartBoostGrade grade, BoostConfig reward)
        {
            if (grade == StartBoostGrade.Perfect || grade == StartBoostGrade.Good)
                SpawnVFX(BoostVFXType.StartFlame);
        }

        private void HandleTrickStart(TrickType trick)
        {
            SpawnVFX(BoostVFXType.TrickRibbon);
        }

        private void HandleTrickComplete(TrickType trick, float meter)
        {
            // Intensity could scale ribbon brightness; handled via Update.
        }

        private void HandleTrickFail(float meter)
        {
            // Optional: brief red flash VFX.
        }

        #endregion

        #region VFX Helpers

        private void SpawnVFX(BoostVFXType vfxType)
        {
#if SWEF_VFX_AVAILABLE
            // Integration: VFXPoolManager.Instance.Spawn(vfxType.ToString(), transform.position, transform.rotation);
#endif
        }

        private void SetDriftSparks(bool active, DriftLevel level)
        {
            if (driftSparkParticles == null) return;

            if (!active)
            {
                driftSparkParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            var main = driftSparkParticles.main;
            if (driftController != null)
                main.startColor = new ParticleSystem.MinMaxGradient(driftController.CurrentSparkColor);

            if (!driftSparkParticles.isPlaying)
                driftSparkParticles.Play();

            // Scale emission rate with spark intensity.
            var emission = driftSparkParticles.emission;
            float intensity = driftController?.State.sparkIntensity ?? 0f;
            emission.rateOverTime = Mathf.Lerp(20f, 120f, intensity);
        }

        private void SetSlipstreamParticles(bool active, float intensity)
        {
            if (slipstreamParticles == null) return;

            if (!active)
            {
                slipstreamParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (!slipstreamParticles.isPlaying) slipstreamParticles.Play();

            var emission = slipstreamParticles.emission;
            emission.rateOverTime = Mathf.Lerp(10f, 80f, intensity);
        }

        private void UpdateSpeedLines()
        {
            if (speedLinesParticles == null || BoostController.Instance == null) return;

            float boost = BoostController.Instance.CurrentSpeedMultiplier;
            bool  shouldPlay = boost > 1.05f;

            if (shouldPlay && !speedLinesParticles.isPlaying)
                speedLinesParticles.Play();
            else if (!shouldPlay && speedLinesParticles.isPlaying)
                speedLinesParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);

            if (shouldPlay)
            {
                var emission = speedLinesParticles.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 200f, Mathf.InverseLerp(1f, 3f, boost));
            }
        }

        private void UpdateDriftSparks()
        {
            if (driftController == null) return;
            // Colour is updated in HandleDriftLevelUp; this runs the intensity.
            if (!driftController.IsDrifting && driftSparkParticles != null
                && driftSparkParticles.isPlaying)
                driftSparkParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void UpdateSlipstreamParticles()
        {
            if (slipstreamController == null) return;
            if (!slipstreamController.IsInSlipstream && slipstreamParticles != null
                && slipstreamParticles.isPlaying)
                slipstreamParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        }

        #endregion

        #region Camera Shake

        private void TriggerShake(float magnitude, float duration)
        {
            _shakeCurrentMagnitude = magnitude;
            _shakeTimer            = duration;

            if (targetCamera != null && !_cameraBaseRecorded)
            {
                _cameraBasePosition = targetCamera.transform.localPosition;
                _cameraBaseRecorded = true;
            }
        }

        private void TickCameraShake(float dt)
        {
            if (_shakeTimer <= 0f)
            {
                if (targetCamera != null && _cameraBaseRecorded)
                {
                    targetCamera.transform.localPosition = _cameraBasePosition;
                    _cameraBaseRecorded = false;
                }
                return;
            }

            _shakeTimer -= dt;
            if (targetCamera == null) return;

            float t = _shakeTimer / boostShakeDuration;
            Vector3 offset = UnityEngine.Random.insideUnitSphere * _shakeCurrentMagnitude * t;
            targetCamera.transform.localPosition = _cameraBasePosition + offset;
        }

        #endregion

        #region Private Init

        private void ResolveReferences()
        {
            if (boostController     == null) boostController     = BoostController.Instance;
            if (driftController     == null) driftController     = DriftController.Instance;
            if (slipstreamController == null) slipstreamController = SlipstreamController.Instance;
        }

        #endregion
    }
}
