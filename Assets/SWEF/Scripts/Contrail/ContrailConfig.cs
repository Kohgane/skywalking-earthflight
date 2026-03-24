// ContrailConfig.cs — SWEF Contrail & Exhaust Trail System (Phase 71)
using UnityEngine;

namespace SWEF.Contrail
{
    /// <summary>
    /// Compile-time constants shared across the Contrail &amp; Exhaust Trail system.
    ///
    /// <para>These values serve as sensible defaults; runtime behaviour can be
    /// fine-tuned through <see cref="ContrailConditions"/> ScriptableObject fields
    /// and individual MonoBehaviour inspector fields.</para>
    /// </summary>
    public static class ContrailConfig
    {
        // ── Altitude Thresholds ───────────────────────────────────────────────

        /// <summary>Minimum altitude in metres at which condensation contrails can form.</summary>
        public const float MinContrailAltitude = 8000f;

        /// <summary>Maximum altitude in metres beyond which the atmosphere is too thin for visible contrails.</summary>
        public const float MaxContrailAltitude = 15000f;

        // ── Temperature ───────────────────────────────────────────────────────

        /// <summary>Air temperature threshold (°C) below which condensation contrails form.</summary>
        public const float ContrailTempThreshold = -40f;

        // ── Humidity ──────────────────────────────────────────────────────────

        /// <summary>Relative humidity (0–1) above which contrails persist rather than quickly evaporating.</summary>
        public const float ContrailHumidityThreshold = 0.6f;

        // ── Formation ─────────────────────────────────────────────────────────

        /// <summary>Seconds of delay between the aircraft position and the point where the trail first appears.</summary>
        public const float FormationDelay = 0.5f;

        // ── Speed Thresholds ──────────────────────────────────────────────────

        /// <summary>Minimum aircraft speed (m/s) required for any contrail trail to be emitted.</summary>
        public const float MinTrailSpeed = 50f;

        /// <summary>Minimum aircraft speed (m/s) required for wingtip vortex trails.</summary>
        public const float VortexMinSpeed = 80f;

        // ── G-Force ───────────────────────────────────────────────────────────

        /// <summary>G-force threshold above which wingtip vortex generation begins.</summary>
        public const float VortexGForceThreshold = 2f;

        // ── Trail Widths ──────────────────────────────────────────────────────

        /// <summary>Contrail trail width (metres) at the emission point.</summary>
        public const float BaseContrailWidth = 1f;

        /// <summary>Maximum expanded contrail trail width (metres).</summary>
        public const float MaxContrailWidth = 8f;

        /// <summary>Maximum wingtip vortex trail width (metres).</summary>
        public const float MaxVortexWidth = 3f;

        // ── Persistence Durations ─────────────────────────────────────────────

        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Short"/>.</summary>
        public const float ShortDuration = 5f;

        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Medium"/>.</summary>
        public const float MediumDuration = 30f;

        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Long"/>.</summary>
        public const float LongDuration = 120f;

        /// <summary>Trail lifetime (seconds) for <see cref="TrailPersistence.Permanent"/>.</summary>
        public const float PermanentDuration = 600f;

        // ── Exhaust Lengths ───────────────────────────────────────────────────

        /// <summary>Engine exhaust plume length (metres) at idle throttle.</summary>
        public const float ExhaustBaseLength = 2f;

        /// <summary>Engine exhaust plume length (metres) at maximum throttle.</summary>
        public const float ExhaustMaxLength = 10f;

        // ── Quality / Particle Count Multipliers ──────────────────────────────

        /// <summary>
        /// Returns the particle count multiplier that corresponds to
        /// <paramref name="intensity"/>.  Apply to a baseline particle count to
        /// scale system load proportionally.
        /// </summary>
        /// <param name="intensity">Global trail intensity level.</param>
        /// <returns>A positive float multiplier (0 = disabled, 1 = full quality).</returns>
        public static float GetParticleMultiplier(TrailIntensity intensity)
        {
            switch (intensity)
            {
                case TrailIntensity.None:    return 0f;
                case TrailIntensity.Light:   return 0.25f;
                case TrailIntensity.Medium:  return 0.5f;
                case TrailIntensity.Heavy:   return 0.75f;
                case TrailIntensity.Maximum: return 1f;
                default:                     return 0.5f;
            }
        }

        // ── UI Colors ─────────────────────────────────────────────────────────

        /// <summary>Default white contrail start color (fully opaque).</summary>
        public static readonly Color ContrailStartColor = new Color(1f, 1f, 1f, 0.85f);

        /// <summary>Default contrail end color (fully transparent white).</summary>
        public static readonly Color ContrailEndColor = new Color(1f, 1f, 1f, 0f);

        /// <summary>Default exhaust start color (orange-tinged).</summary>
        public static readonly Color ExhaustStartColor = new Color(1f, 0.6f, 0.2f, 0.9f);

        /// <summary>Default exhaust end color (dark grey, fully transparent).</summary>
        public static readonly Color ExhaustEndColor = new Color(0.2f, 0.2f, 0.2f, 0f);
    }
}
