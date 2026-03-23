// LandingConfig.cs — SWEF Landing & Airport System (Phase 68)

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Static configuration constants for the Landing &amp; Airport system.
    ///
    /// <para>All numeric defaults are defined here so that individual components
    /// stay consistent without duplicating magic numbers.</para>
    /// </summary>
    public static class LandingConfig
    {
        // ── Glide Slope ───────────────────────────────────────────────────────

        /// <summary>Default ILS glide slope angle in degrees.</summary>
        public const float DefaultGlideSlopeAngle = 3f;

        /// <summary>Default decision altitude in meters AGL.</summary>
        public const float DefaultDecisionAltitude = 60f;

        // ── Touchdown Speeds ──────────────────────────────────────────────────

        /// <summary>Maximum vertical speed (m/s) for a safe landing.</summary>
        public const float MaxSafeTouchdownSpeed = 3f;

        /// <summary>Maximum vertical speed (m/s) before a crash is recorded.</summary>
        public const float MaxSurvivableTouchdownSpeed = 8f;

        // ── Landing Gear ──────────────────────────────────────────────────────

        /// <summary>Default time in seconds to fully deploy landing gear.</summary>
        public const float GearDeployTime = 3f;

        /// <summary>Default time in seconds to fully retract landing gear.</summary>
        public const float GearRetractTime = 3f;

        /// <summary>AGL altitude (m) below which auto-deploy suggestion fires.</summary>
        public const float AutoDeployAltitude = 300f;

        // ── Auto-land ─────────────────────────────────────────────────────────

        /// <summary>AGL altitude (m) at which the auto-land system captures the approach.</summary>
        public const float AutoLandCaptureAltitude = 500f;

        /// <summary>Maximum crosswind (m/s) within which auto-land may engage.</summary>
        public const float MaxAutoLandCrosswind = 15f;

        // ── Flare ─────────────────────────────────────────────────────────────

        /// <summary>AGL altitude (m) at which the flare manoeuvre begins.</summary>
        public const float FlareAltitude = 15f;

        // ── Approach Speed ────────────────────────────────────────────────────

        /// <summary>Multiplier applied to stall speed to compute approach target speed.</summary>
        public const float ApproachSpeedFactor = 1.3f;

        // ── Landing Score Weights ─────────────────────────────────────────────

        /// <summary>Weight of centreline alignment in the composite landing score (0–1).</summary>
        public const float CenterlineWeight = 0.3f;

        /// <summary>Weight of touchdown vertical speed in the composite landing score (0–1).</summary>
        public const float VerticalSpeedWeight = 0.4f;

        /// <summary>Weight of overall smoothness in the composite landing score (0–1).</summary>
        public const float SmoothnessWeight = 0.3f;

        // ── Grade Thresholds ──────────────────────────────────────────────────

        /// <summary>Minimum score for a "Perfect" grade.</summary>
        public const float PerfectThreshold = 90f;

        /// <summary>Minimum score for a "Good" grade.</summary>
        public const float GoodThreshold = 70f;

        /// <summary>Minimum score for an "Acceptable" grade.</summary>
        public const float AcceptableThreshold = 50f;
    }
}
