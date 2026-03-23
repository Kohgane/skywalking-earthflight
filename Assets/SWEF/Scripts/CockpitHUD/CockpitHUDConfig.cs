// CockpitHUDConfig.cs — SWEF Cockpit Instrument & HUD Dashboard System (Phase 65)
using UnityEngine;

namespace SWEF.CockpitHUD
{
    /// <summary>
    /// Phase 65 — Static configuration class for the Cockpit HUD system.
    ///
    /// <para>Contains unit conversion helpers, default thresholds, and shared
    /// color constants used by all HUD instruments and the warning system.</para>
    /// </summary>
    public static class CockpitHUDConfig
    {
        // ── Default Thresholds ────────────────────────────────────────────────

        /// <summary>Default stall angle in degrees.</summary>
        public const float DefaultStallAngle = 25f;

        /// <summary>Default overspeed threshold in knots.</summary>
        public const float DefaultOverspeed = 300f;

        /// <summary>Default low-fuel threshold (0–1).</summary>
        public const float DefaultLowFuel = 0.2f;

        /// <summary>Default HUD opacity (0–1).</summary>
        public const float DefaultHUDOpacity = 1f;

        /// <summary>Seconds of idle before the HUD auto-hides.</summary>
        public const float AutoHideDelay = 5f;

        /// <summary>Speed of sound at sea level (m/s) for Mach calculation.</summary>
        public const float SpeedOfSound = 343f;

        // ── Color Constants ───────────────────────────────────────────────────

        /// <summary>Color used for safe / nominal instrument readings.</summary>
        public static readonly Color SafeColor     = new Color(0.20f, 0.85f, 0.20f, 1f);

        /// <summary>Color used for caution-level instrument readings.</summary>
        public static readonly Color CautionColor  = new Color(1.00f, 0.85f, 0.00f, 1f);

        /// <summary>Color used for warning-level instrument readings.</summary>
        public static readonly Color WarningColor  = new Color(1.00f, 0.50f, 0.00f, 1f);

        /// <summary>Color used for critical / emergency instrument readings.</summary>
        public static readonly Color CriticalColor = new Color(1.00f, 0.10f, 0.10f, 1f);

        // ── Unit Conversion Helpers ───────────────────────────────────────────

        /// <summary>Converts meters to feet.</summary>
        /// <param name="m">Distance in meters.</param>
        /// <returns>Distance in feet.</returns>
        public static float MetersToFeet(float m) => m * 3.28084f;

        /// <summary>Converts meters-per-second to knots.</summary>
        /// <param name="ms">Speed in m/s.</param>
        /// <returns>Speed in knots.</returns>
        public static float MsToKnots(float ms) => ms * 1.94384f;

        /// <summary>Converts meters-per-second to kilometers-per-hour.</summary>
        /// <param name="ms">Speed in m/s.</param>
        /// <returns>Speed in km/h.</returns>
        public static float MsToKph(float ms) => ms * 3.6f;
    }
}
