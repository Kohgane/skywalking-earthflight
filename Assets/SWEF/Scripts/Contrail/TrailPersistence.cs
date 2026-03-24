// TrailPersistence.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Controls how long trail segments remain visible in the world before fading out.
    ///
    /// <para>Adjusts the <c>time</c> property of every <see cref="TrailRenderer"/>
    /// and the <c>startLifetime</c> of every <see cref="ParticleSystem"/> managed by
    /// <see cref="ContrailManager"/>, applying wind and turbulence dissipation
    /// factors on top of the baseline persistence duration.</para>
    ///
    /// <para>Attach to the same GameObject as <see cref="ContrailManager"/>.</para>
    /// </summary>
    [AddComponentMenu("SWEF/Contrail/Trail Persistence")]
    public class TrailLifetimeController : MonoBehaviour
    {
        #region Inspector

        [Header("Persistence Setting")]
        [Tooltip("Baseline persistence level that determines the default trail duration.")]
        /// <summary>Baseline persistence level.</summary>
        public TrailPersistence persistenceLevel = TrailPersistence.Medium;

        [Header("Duration Presets (seconds)")]
        [Tooltip("Trail lifetime in seconds for the Short persistence level.")]
        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Short"/>.</summary>
        [Min(0f)]
        public float shortDuration = ContrailConfig.ShortDuration;

        [Tooltip("Trail lifetime in seconds for the Medium persistence level.")]
        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Medium"/>.</summary>
        [Min(0f)]
        public float mediumDuration = ContrailConfig.MediumDuration;

        [Tooltip("Trail lifetime in seconds for the Long persistence level.")]
        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Long"/>.</summary>
        [Min(0f)]
        public float longDuration = ContrailConfig.LongDuration;

        [Tooltip("Trail lifetime in seconds for the Permanent persistence level.")]
        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Permanent"/>.</summary>
        [Min(0f)]
        public float permanentDuration = ContrailConfig.PermanentDuration;

        [Header("Environmental Dissipation")]
        [Tooltip("Rate at which wind speed accelerates trail fading. Multiplied by wind speed (m/s).")]
        /// <summary>How much wind speed (m/s) speeds up trail fading.</summary>
        [Min(0f)]
        public float windDissipation = 0.1f;

        [Tooltip("Rate at which turbulence breaks up trails. Multiplied by turbulence intensity (0–1).")]
        /// <summary>How much turbulence intensity (0–1) accelerates trail dissipation.</summary>
        [Min(0f)]
        public float turbulenceDissipation = 0.2f;

        [Header("Fade Curve")]
        [Tooltip("How trail opacity decreases over its normalised lifetime (0 = birth, 1 = death).")]
        /// <summary>Opacity over normalised trail lifetime (0 = birth, 1 = death).</summary>
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        #endregion

        #region Public State

        /// <summary>Effective trail lifetime (seconds) computed from <see cref="persistence"/> and dissipation factors.</summary>
        public float currentDuration { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            currentDuration = GetBaseDuration(persistenceLevel);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Updates the persistence level and immediately propagates the new duration
        /// to all managed trail components.
        /// </summary>
        /// <param name="level">New persistence level to apply.</param>
        public void SetPersistence(TrailPersistence level)
        {
            persistenceLevel = level;
            currentDuration  = GetBaseDuration(level);
            PropagateToAllTrails(currentDuration);
        }

        /// <summary>
        /// Applies a wind effect that both shifts existing trail positions (via particle velocity)
        /// and reduces effective trail duration proportionally to wind speed.
        /// </summary>
        /// <param name="windSpeed">Wind speed in m/s.</param>
        /// <param name="windDirection">Wind direction in degrees (0 = north, clockwise).</param>
        public void ApplyWindEffect(float windSpeed, float windDirection)
        {
            float dissipatedDuration = GetBaseDuration(persistenceLevel)
                / (1f + windSpeed * windDissipation);
            dissipatedDuration = Mathf.Max(dissipatedDuration, 0.5f);

            currentDuration = dissipatedDuration;
            PropagateToAllTrails(currentDuration);

            // Shift particle velocities to simulate wind-blown trail drift.
            float windRad = windDirection * Mathf.Deg2Rad;
            Vector3 windVelocity = new Vector3(Mathf.Sin(windRad), 0f, Mathf.Cos(windRad)) * windSpeed * 0.3f;
            ApplyWindToParticleSystems(windVelocity);
        }

        /// <summary>
        /// Applies a turbulence effect that reduces effective trail duration proportionally
        /// to the turbulence intensity.
        /// </summary>
        /// <param name="turbulenceIntensity">Turbulence intensity in [0, 1].</param>
        public void ApplyTurbulenceEffect(float turbulenceIntensity)
        {
            float factor = 1f + turbulenceIntensity * turbulenceDissipation * 10f;
            currentDuration = Mathf.Max(GetBaseDuration(persistenceLevel) / factor, 0.5f);
            PropagateToAllTrails(currentDuration);
        }

        #endregion

        #region Private Helpers

        private float GetBaseDuration(TrailPersistence level)
        {
            switch (level)
            {
                case TrailPersistence.Short:     return shortDuration;
                case TrailPersistence.Medium:    return mediumDuration;
                case TrailPersistence.Long:      return longDuration;
                case TrailPersistence.Permanent: return permanentDuration;
                default:                         return mediumDuration;
            }
        }

        private void PropagateToAllTrails(float duration)
        {
            ContrailManager manager = ContrailManager.Instance;
            if (manager == null)
                return;

            foreach (ContrailEmitter emitter in manager.Emitters)
            {
                if (emitter == null) continue;

                if (emitter.trailRenderer != null)
                    emitter.trailRenderer.time = duration;

                if (emitter.trailParticles != null)
                {
                    var main = emitter.trailParticles.main;
                    main.startLifetime = duration;
                }
            }

            // Also update WingTipVortex trails.
            WingTipVortex vortex = manager.WingTipVortex;
            if (vortex != null)
            {
                if (vortex.leftTrail  != null) vortex.leftTrail.time  = duration;
                if (vortex.rightTrail != null) vortex.rightTrail.time = duration;
                if (vortex.leftVortex  != null) { var m = vortex.leftVortex.main;  m.startLifetime = duration; }
                if (vortex.rightVortex != null) { var m = vortex.rightVortex.main; m.startLifetime = duration; }
            }
        }

        private void ApplyWindToParticleSystems(Vector3 windVelocity)
        {
            ContrailManager manager = ContrailManager.Instance;
            if (manager == null)
                return;

            foreach (ContrailEmitter emitter in manager.Emitters)
            {
                if (emitter?.trailParticles == null) continue;

                var velocityModule = emitter.trailParticles.velocityOverLifetime;
                velocityModule.enabled = true;
                velocityModule.space   = ParticleSystemSimulationSpace.World;
                velocityModule.x       = windVelocity.x;
                velocityModule.y       = windVelocity.y;
                velocityModule.z       = windVelocity.z;
            }
        }

        #endregion
    }
}
