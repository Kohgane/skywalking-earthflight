using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Per-emergency-type visual effects (smoke, fire, ice, sparks),
    /// audio alarms, and camera effects (shake, vignette, screen crack).
    /// Integrates with SWEF.CockpitHUD.WarningSystem (null-safe).
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyEffectsController : MonoBehaviour
    {
        #region Inspector

        [Header("Particle Systems")]
        [Tooltip("Smoke effect prefab (engine failure, structural damage).")]
        [SerializeField] private ParticleSystem smokeEffect;

        [Tooltip("Fire effect prefab (fire on board).")]
        [SerializeField] private ParticleSystem fireEffect;

        [Tooltip("Ice/frost effect prefab (icing critical).")]
        [SerializeField] private ParticleSystem iceEffect;

        [Tooltip("Spark effect prefab (electrical failure, bird strike).")]
        [SerializeField] private ParticleSystem sparkEffect;

        [Tooltip("Fog effect prefab (depressurization).")]
        [SerializeField] private ParticleSystem fogEffect;

        [Header("Camera Shake")]
        [Tooltip("Peak shake magnitude for Mayday-level emergencies.")]
        [SerializeField] private float maxShakeMagnitude = 0.3f;

        [Tooltip("Shake frequency in Hz.")]
        [SerializeField] private float shakeFrequency = 15f;

        [Header("HUD")]
        [Tooltip("Color for Caution-level HUD tint.")]
        [SerializeField] private Color cautionColor = new Color(1f, 1f, 0f, 0.4f);

        [Tooltip("Color for Warning-level HUD tint.")]
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 0.5f);

        [Tooltip("Color for Emergency/Mayday-level HUD tint.")]
        [SerializeField] private Color emergencyColor = new Color(1f, 0f, 0f, 0.6f);

        [Header("Audio Keys")]
        [SerializeField] private string alarmClipKey          = "sfx_alarm_general";
        [SerializeField] private string fireAlarmClipKey      = "sfx_alarm_fire";
        [SerializeField] private string windNoiseClipKey      = "sfx_wind_depress";
        [SerializeField] private string masterWarningClipKey  = "sfx_master_warning";

        #endregion

        #region Private State

        private Camera _mainCamera;
        private float  _shakeIntensity;
        private bool   _shakeActive;
        private readonly Dictionary<EmergencyType, ParticleSystem> _vfxMap
            = new Dictionary<EmergencyType, ParticleSystem>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mainCamera = Camera.main;
            BuildVfxMap();
        }

        private void Update()
        {
            if (_shakeActive)
                ApplyCameraShake();
        }

        #endregion

        #region Public API

        /// <summary>Start visual and audio effects for the given emergency.</summary>
        public void ActivateEffects(ActiveEmergency emergency)
        {
            if (emergency == null) return;

            StartVFX(emergency.scenario.type);
            StartAudio(emergency.scenario.type, emergency.currentSeverity);
            SetShakeIntensity(SeverityToShake(emergency.currentSeverity));
            TriggerMasterWarning(emergency.currentSeverity);
        }

        /// <summary>Update effects when severity escalates.</summary>
        public void OnSeverityEscalated(ActiveEmergency emergency, EmergencySeverity previous)
        {
            if (emergency == null) return;
            SetShakeIntensity(SeverityToShake(emergency.currentSeverity));
            TriggerMasterWarning(emergency.currentSeverity);
        }

        /// <summary>Stop all effects for the given emergency.</summary>
        public void DeactivateEffects(ActiveEmergency emergency)
        {
            StopAllVFX();
            StopAudio();
            SetShakeIntensity(0f);
            ClearWarnings();
        }

        #endregion

        #region Private Helpers

        private void BuildVfxMap()
        {
            _vfxMap[EmergencyType.EngineFailure]         = smokeEffect;
            _vfxMap[EmergencyType.DualEngineFailure]     = smokeEffect;
            _vfxMap[EmergencyType.FuelStarvation]        = smokeEffect;
            _vfxMap[EmergencyType.FuelLeak]              = smokeEffect;
            _vfxMap[EmergencyType.BirdStrike]            = sparkEffect;
            _vfxMap[EmergencyType.StructuralDamage]      = smokeEffect;
            _vfxMap[EmergencyType.IcingCritical]         = iceEffect;
            _vfxMap[EmergencyType.ElectricalFailure]     = sparkEffect;
            _vfxMap[EmergencyType.HydraulicFailure]      = smokeEffect;
            _vfxMap[EmergencyType.FireOnboard]           = fireEffect;
            _vfxMap[EmergencyType.Depressurization]      = fogEffect;
            _vfxMap[EmergencyType.NavigationFailure]     = sparkEffect;
            _vfxMap[EmergencyType.CommunicationFailure]  = sparkEffect;
            _vfxMap[EmergencyType.ControlSurfaceJam]     = smokeEffect;
            _vfxMap[EmergencyType.LandingGearMalfunction]= sparkEffect;
        }

        private void StartVFX(EmergencyType type)
        {
            if (_vfxMap.TryGetValue(type, out var ps) && ps != null)
                ps.Play();
        }

        private void StopAllVFX()
        {
            smokeEffect?.Stop();
            fireEffect?.Stop();
            iceEffect?.Stop();
            sparkEffect?.Stop();
            fogEffect?.Stop();
        }

        private void StartAudio(EmergencyType type, EmergencySeverity severity)
        {
            string clip = type == EmergencyType.FireOnboard ? fireAlarmClipKey
                        : type == EmergencyType.Depressurization ? windNoiseClipKey
                        : alarmClipKey;
            PlayAudio(clip);
        }

        private void StopAudio()
        {
#if SWEF_AUDIO_AVAILABLE
            SWEF.Audio.AudioManager.Instance?.StopClip(alarmClipKey);
            SWEF.Audio.AudioManager.Instance?.StopClip(fireAlarmClipKey);
            SWEF.Audio.AudioManager.Instance?.StopClip(windNoiseClipKey);
#endif
        }

        private void SetShakeIntensity(float intensity)
        {
            _shakeIntensity = intensity;
            _shakeActive    = intensity > 0.001f;
        }

        private void ApplyCameraShake()
        {
            if (_mainCamera == null) return;
            float t = Time.time * shakeFrequency;
            Vector3 offset = new Vector3(
                Mathf.PerlinNoise(t, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, t) - 0.5f,
                0f) * _shakeIntensity;
            _mainCamera.transform.localPosition += offset;
        }

        private void TriggerMasterWarning(EmergencySeverity severity)
        {
            PlayAudio(masterWarningClipKey);
#if SWEF_COCKPITHUD_AVAILABLE
            SWEF.CockpitHUD.WarningSystem.Instance?.TriggerMasterWarning(SeverityToColor(severity));
#endif
        }

        private void ClearWarnings()
        {
#if SWEF_COCKPITHUD_AVAILABLE
            SWEF.CockpitHUD.WarningSystem.Instance?.ClearMasterWarning();
#endif
        }

        private void PlayAudio(string clipKey)
        {
#if SWEF_AUDIO_AVAILABLE
            SWEF.Audio.AudioManager.Instance?.PlayClip(clipKey);
#endif
        }

        private float SeverityToShake(EmergencySeverity severity)
        {
            return severity switch
            {
                EmergencySeverity.Caution    => 0f,
                EmergencySeverity.Warning    => maxShakeMagnitude * 0.25f,
                EmergencySeverity.Emergency  => maxShakeMagnitude * 0.6f,
                EmergencySeverity.Mayday     => maxShakeMagnitude,
                _                            => 0f
            };
        }

        private Color SeverityToColor(EmergencySeverity severity)
        {
            return severity switch
            {
                EmergencySeverity.Caution    => cautionColor,
                EmergencySeverity.Warning    => warningColor,
                _                            => emergencyColor
            };
        }

        #endregion
    }
}
