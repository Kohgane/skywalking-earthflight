// ExhaustEffect.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Controls the visual engine exhaust plume — scaling the exhaust length and
    /// colour with throttle, toggling afterburner flame, and adjusting heat
    /// distortion based on altitude.
    ///
    /// <para>Attach to the aircraft root or an engine GameObject and assign the
    /// <see cref="exhaustNozzles"/> array and the required particle systems.</para>
    /// </summary>
    [AddComponentMenu("SWEF/Contrail/Exhaust Effect")]
    public class ExhaustEffect : MonoBehaviour
    {
        #region Inspector

        [Header("Nozzle Transforms")]
        [Tooltip("World-space positions of each engine exhaust nozzle. Used to align particle systems.")]
        /// <summary>World-space transforms of each engine exhaust nozzle.</summary>
        public Transform[] exhaustNozzles;

        [Header("Particle Systems")]
        [Tooltip("Main engine exhaust particle system (idle → full throttle plume).")]
        /// <summary>Main engine exhaust particle system.</summary>
        public ParticleSystem exhaustParticles;

        [Tooltip("Afterburner-specific high-intensity flame particle system.")]
        /// <summary>Afterburner-specific high-intensity flame particle system.</summary>
        public ParticleSystem afterburnerParticles;

        [Tooltip("Heat-shimmer distortion particle system. Strongest at low altitude.")]
        /// <summary>Heat-distortion shimmer particle system.</summary>
        public ParticleSystem heatDistortion;

        [Header("Lighting")]
        [Tooltip("Point light that simulates the orange glow of an active afterburner.")]
        /// <summary>Point light for afterburner glow. May be null.</summary>
        public Light afterburnerLight;

        [Header("Exhaust Length")]
        [Tooltip("Exhaust plume length (metres) at idle throttle.")]
        /// <summary>Exhaust plume length (metres) at idle throttle.</summary>
        [Min(0f)]
        public float baseExhaustLength = ContrailConfig.ExhaustBaseLength;

        [Tooltip("Exhaust plume length (metres) at full throttle.")]
        /// <summary>Exhaust plume length (metres) at full throttle.</summary>
        [Min(0f)]
        public float maxExhaustLength = ContrailConfig.ExhaustMaxLength;

        [Tooltip("Multiplier applied to the plume length when the afterburner is active.")]
        /// <summary>Length multiplier applied during afterburner operation.</summary>
        [Min(1f)]
        public float afterburnerLengthMultiplier = 2f;

        [Header("Colors")]
        [Tooltip("Exhaust particle start color at idle throttle.")]
        /// <summary>Exhaust particle start color at idle throttle.</summary>
        public Color idleExhaustColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

        [Tooltip("Exhaust particle start color at full throttle.")]
        /// <summary>Exhaust particle start color at full throttle.</summary>
        public Color fullThrottleColor = new Color(1f, 0.8f, 0.4f, 0.8f);

        [Tooltip("Afterburner particle start color.")]
        /// <summary>Afterburner particle start color.</summary>
        public Color afterburnerColor = new Color(1f, 0.4f, 0.1f, 1f);

        [Header("Heat Distortion")]
        [Tooltip("Baseline intensity of heat-shimmer distortion particles. Scaled inversely with altitude.")]
        /// <summary>Baseline heat-distortion intensity (0–1).</summary>
        [Range(0f, 1f)]
        public float heatDistortionIntensity = 0.5f;

        #endregion

        #region Public State

        /// <summary><c>true</c> while the afterburner is active.</summary>
        public bool isAfterburnerActive { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetAfterburnerActive(false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Scales exhaust visuals to match the supplied flight parameters.
        /// Call this every frame (or on a throttled interval) from
        /// <see cref="ContrailManager"/>.
        /// </summary>
        /// <param name="throttle">Normalised throttle in [0, 1].</param>
        /// <param name="afterburner"><c>true</c> when the afterburner is engaged.</param>
        /// <param name="altitude">Aircraft altitude in metres, used to blend heat distortion.</param>
        public void UpdateExhaust(float throttle, bool afterburner, float altitude)
        {
            isAfterburnerActive = afterburner;

            if (exhaustParticles != null)
                UpdateMainExhaust(throttle);

            SetAfterburnerActive(afterburner);
            UpdateHeatDistortion(throttle, altitude);
        }

        #endregion

        #region Private Helpers

        private void UpdateMainExhaust(float throttle)
        {
            float targetLength = Mathf.Lerp(baseExhaustLength, maxExhaustLength, throttle);
            if (isAfterburnerActive)
                targetLength *= afterburnerLengthMultiplier;

            // Drive start speed to simulate plume length: longer plume = higher start speed.
            var main = exhaustParticles.main;
            main.startSpeedMultiplier = targetLength;

            // Blend color between idle and full-throttle shades.
            Color currentColor = isAfterburnerActive
                ? afterburnerColor
                : Color.Lerp(idleExhaustColor, fullThrottleColor, throttle);
            main.startColor = currentColor;

            // Keep the system playing.
            if (!exhaustParticles.isPlaying)
                exhaustParticles.Play();
        }

        private void SetAfterburnerActive(bool active)
        {
            if (afterburnerParticles != null)
            {
                if (active && !afterburnerParticles.isPlaying)
                    afterburnerParticles.Play();
                else if (!active && afterburnerParticles.isPlaying)
                    afterburnerParticles.Stop();
            }

            if (afterburnerLight != null)
                afterburnerLight.enabled = active;
        }

        private void UpdateHeatDistortion(float throttle, float altitude)
        {
            if (heatDistortion == null)
                return;

            // Heat distortion is strongest at low altitude (dense air) and fades at high altitude.
            float altitudeFade = Mathf.Clamp01(1f - altitude / ContrailConfig.MinContrailAltitude);
            float effectiveIntensity = heatDistortionIntensity * throttle * altitudeFade;

            var emission = heatDistortion.emission;
            emission.rateOverTimeMultiplier = effectiveIntensity * 50f;   // 50 = baseline particle rate

            if (effectiveIntensity > 0.01f && !heatDistortion.isPlaying)
                heatDistortion.Play();
            else if (effectiveIntensity <= 0.01f && heatDistortion.isPlaying)
                heatDistortion.Stop();
        }

        #endregion
    }
}
