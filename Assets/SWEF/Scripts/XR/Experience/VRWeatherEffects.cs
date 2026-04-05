// VRWeatherEffects.cs — Phase 112: VR/XR Flight Experience
// VR-enhanced weather: volumetric clouds, rain on canopy, turbulence shake.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Drives VR-specific weather effects: volumetric cloud layer activation,
    /// canopy rain particles, and camera shake for turbulence.
    /// </summary>
    public class VRWeatherEffects : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Cloud Layer")]
        [SerializeField] private GameObject volumetricCloudLayer;

        [Header("Canopy Rain")]
        [SerializeField] private ParticleSystem canopyRainParticles;
        [SerializeField] private float          rainIntensityMax = 100f;

        [Header("Turbulence Shake")]
        [SerializeField] private Transform cameraShakeTarget;
        [SerializeField] private float     turbulenceMaxAmplitude = 0.05f;
        [SerializeField] private float     turbulenceFrequency    = 8f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current turbulence intensity [0..1].</summary>
        public float TurbulenceIntensity { get; private set; }

        /// <summary>Current rain intensity [0..1].</summary>
        public float RainIntensity { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private Vector3 _shakeOrigin;
        private float   _shakeTime;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (cameraShakeTarget != null)
                _shakeOrigin = cameraShakeTarget.localPosition;
        }

        private void Update()
        {
            ApplyTurbulenceShake();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enables/disables takeoff-phase weather effects.</summary>
        public void SetTakeoffEffects(bool enabled)
        {
            if (volumetricCloudLayer != null)
                volumetricCloudLayer.SetActive(enabled);
        }

        /// <summary>Enables/disables cruise-phase weather effects.</summary>
        public void SetCruiseEffects(bool enabled)
        {
            if (volumetricCloudLayer != null)
                volumetricCloudLayer.SetActive(enabled);
        }

        /// <summary>Sets turbulence intensity [0..1], driving camera shake.</summary>
        public void SetTurbulence(float intensity)
        {
            TurbulenceIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>Sets rain intensity [0..1], driving canopy particle rate.</summary>
        public void SetRain(float intensity)
        {
            RainIntensity = Mathf.Clamp01(intensity);
            if (canopyRainParticles == null) return;
            var emission = canopyRainParticles.emission;
            emission.rateOverTime = RainIntensity * rainIntensityMax;
            if (RainIntensity > 0f && !canopyRainParticles.isPlaying)
                canopyRainParticles.Play();
            else if (Mathf.Approximately(RainIntensity, 0f))
                canopyRainParticles.Stop();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ApplyTurbulenceShake()
        {
            if (cameraShakeTarget == null || Mathf.Approximately(TurbulenceIntensity, 0f))
                return;

            _shakeTime += Time.deltaTime * turbulenceFrequency;
            float amp = TurbulenceIntensity * turbulenceMaxAmplitude;
            float x   = (Mathf.PerlinNoise(_shakeTime, 0f) - 0.5f) * 2f * amp;
            float y   = (Mathf.PerlinNoise(0f, _shakeTime) - 0.5f) * 2f * amp;
            cameraShakeTarget.localPosition = _shakeOrigin + new Vector3(x, y, 0f);
        }
    }
}
