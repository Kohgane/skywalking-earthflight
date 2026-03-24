// RadarConfig.cs — SWEF Radar & Threat Detection System (Phase 67)
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Static configuration constants for the Radar &amp; Threat Detection system.
    /// <para>
    /// All runtime components reference these values so that tuning a single
    /// constant propagates throughout the system.
    /// </para>
    /// </summary>
    public static class RadarConfig
    {
        // ── Core Radar ────────────────────────────────────────────────────────

        /// <summary>Default maximum radar detection range in metres.</summary>
        public const float DefaultRadarRange = 10000f;

        /// <summary>Default interval in seconds between full radar scans.</summary>
        public const float ScanInterval = 0.5f;

        /// <summary>Default maximum number of simultaneously tracked contacts.</summary>
        public const int MaxContacts = 50;

        /// <summary>
        /// Seconds after the last update before a contact is removed from the
        /// active contact list.
        /// </summary>
        public const float ContactTimeout = 5f;

        /// <summary>
        /// Fraction of <see cref="DefaultRadarRange"/> beyond which signal
        /// strength starts to degrade (0–1).
        /// </summary>
        public const float SignalFalloffStart = 0.8f;

        // ── Threat Thresholds ─────────────────────────────────────────────────

        /// <summary>Range in metres below which a contact is considered at close range.</summary>
        public const float CloseRange = 1000f;

        /// <summary>Range in metres below which a contact is considered at medium range.</summary>
        public const float MediumRange = 3000f;

        /// <summary>Closing speed in m/s above which the threat level is escalated.</summary>
        public const float ClosingSpeedThreshold = 50f;

        // ── Jammer Defaults ───────────────────────────────────────────────────

        /// <summary>Default effective jamming radius in metres.</summary>
        public const float JamRange = 2000f;

        /// <summary>Default jamming effectiveness (0–1).</summary>
        public const float JamEffectiveness = 0.7f;

        /// <summary>Default jammer cooldown in seconds after deactivation.</summary>
        public const float JamCooldown = 30f;

        // ── Zoom Presets ──────────────────────────────────────────────────────

        /// <summary>
        /// Preset display-range zoom levels (in metres) for the radar display.
        /// </summary>
        public static readonly float[] ZoomPresets = { 2000f, 5000f, 10000f };

        // ── Display Colors ────────────────────────────────────────────────────

        /// <summary>Blip color for <see cref="ContactClassification.Friendly"/> contacts.</summary>
        public static readonly Color FriendlyColor = new Color(0.20f, 0.85f, 0.20f, 1f);

        /// <summary>Blip color for <see cref="ContactClassification.Hostile"/> contacts.</summary>
        public static readonly Color HostileColor = new Color(0.95f, 0.15f, 0.15f, 1f);

        /// <summary>Blip color for <see cref="ContactClassification.Neutral"/> contacts.</summary>
        public static readonly Color NeutralColor = Color.white;

        /// <summary>Blip color for <see cref="ContactClassification.Unknown"/> contacts.</summary>
        public static readonly Color UnknownColor = new Color(1.00f, 0.85f, 0.00f, 1f);

        /// <summary>Blip color for <see cref="ContactClassification.Event"/> contacts.</summary>
        public static readonly Color EventColor = new Color(0.00f, 0.85f, 1.00f, 1f);

        /// <summary>Blip color for <see cref="ContactClassification.Civilian"/> contacts.</summary>
        public static readonly Color CivilianColor = new Color(0.40f, 0.60f, 1.00f, 1f);

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the display color that corresponds to the given
        /// <see cref="ContactClassification"/>.
        /// </summary>
        /// <param name="classification">Contact classification to look up.</param>
        /// <returns>The matching <see cref="Color"/>.</returns>
        public static Color GetClassificationColor(ContactClassification classification)
        {
            switch (classification)
            {
                case ContactClassification.Friendly:  return FriendlyColor;
                case ContactClassification.Hostile:   return HostileColor;
                case ContactClassification.Neutral:   return NeutralColor;
                case ContactClassification.Civilian:  return CivilianColor;
                case ContactClassification.Event:     return EventColor;
                default:                              return UnknownColor;
            }
        }
    }
}
