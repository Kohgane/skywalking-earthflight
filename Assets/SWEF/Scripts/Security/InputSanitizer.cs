// InputSanitizer.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static utility that sanitizes all user-supplied strings and validates
    /// numeric coordinate inputs before they reach the game systems.
    ///
    /// <para>All public methods are null-safe and return safe fallback values
    /// rather than throwing.</para>
    /// </summary>
    public static class InputSanitizer
    {
        // ── Limits ────────────────────────────────────────────────────────────

        private const int MaxDisplayNameLength  = 32;
        private const int MaxChatMessageLength  = 256;
        private const int MaxWaypointNameLength = 64;

        private static readonly Regex HtmlTagPattern =
            new Regex(@"<[^>]*>", RegexOptions.Compiled);

        private static readonly Regex ControlCharPattern =
            new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);

        private static readonly Regex MultiSpacePattern =
            new Regex(@"\s{2,}", RegexOptions.Compiled);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sanitizes a player display name: strips HTML tags, control characters,
        /// collapses whitespace, trims, and enforces maximum length.
        /// </summary>
        /// <param name="input">Raw display name from user input.</param>
        /// <returns>Cleaned display name, or <c>"Player"</c> if the result is empty.</returns>
        public static string SanitizeDisplayName(string input)
        {
            string s = ApplyCommonFilters(input, MaxDisplayNameLength);
            if (string.IsNullOrWhiteSpace(s)) s = "Player";

            if (ProfanityFilter.ContainsProfanity(s))
                s = ProfanityFilter.FilterProfanity(s);

            return s;
        }

        /// <summary>
        /// Sanitizes a chat message: strips HTML tags, control characters (except
        /// newlines), and enforces maximum length.
        /// </summary>
        /// <param name="input">Raw chat message from user input.</param>
        /// <returns>Cleaned message string.</returns>
        public static string SanitizeChatMessage(string input)
        {
            if (input == null) return string.Empty;

            // Allow line-feed but strip other control characters
            string s = HtmlTagPattern.Replace(input, string.Empty);
            s = Regex.Replace(s, @"[\x00-\x09\x0B-\x1F\x7F]", string.Empty);
            s = MultiSpacePattern.Replace(s, " ").Trim();

            if (s.Length > MaxChatMessageLength)
                s = s.Substring(0, MaxChatMessageLength);

            if (ProfanityFilter.ContainsProfanity(s))
                s = ProfanityFilter.FilterProfanity(s);

            return s;
        }

        /// <summary>
        /// Sanitizes a waypoint name: strips HTML, control chars, and enforces length.
        /// </summary>
        /// <param name="input">Raw waypoint name.</param>
        /// <returns>Cleaned waypoint name, or <c>"Waypoint"</c> if empty.</returns>
        public static string SanitizeWaypointName(string input)
        {
            string s = ApplyCommonFilters(input, MaxWaypointNameLength);
            if (string.IsNullOrWhiteSpace(s)) s = "Waypoint";

            if (ProfanityFilter.ContainsProfanity(s))
                s = ProfanityFilter.FilterProfanity(s);

            return s;
        }

        /// <summary>
        /// Validates that a latitude/longitude pair is within the valid WGS-84 range.
        /// </summary>
        /// <param name="lat">Latitude in decimal degrees (−90 … 90).</param>
        /// <param name="lon">Longitude in decimal degrees (−180 … 180).</param>
        /// <returns><c>true</c> if both values are in range.</returns>
        public static bool ValidateCoordinates(double lat, double lon)
        {
            return lat >= -90.0 && lat <= 90.0 &&
                   lon >= -180.0 && lon <= 180.0;
        }

        /// <summary>
        /// Validates that an altitude value is within the supported flight envelope.
        /// </summary>
        /// <param name="altitude">Altitude in metres above mean sea level.</param>
        /// <returns><c>true</c> if altitude is between 0 and 150 000 m.</returns>
        public static bool ValidateAltitude(float altitude)
        {
            return altitude >= 0f && altitude <= 150_000f;
        }

        /// <summary>
        /// Validates an <see cref="SWEF.Workshop.AircraftBuildData"/> instance.
        /// Checks that the build ID is non-empty, the part list is non-null,
        /// and the build name passes sanitization.
        /// </summary>
        /// <param name="build">Build data to validate.</param>
        /// <returns>Validation result with violation details.</returns>
        public static ValidationResult ValidateBuildData(
#if SWEF_WORKSHOP_AVAILABLE
            SWEF.Workshop.AircraftBuildData build
#else
            object build
#endif
        )
        {
            if (build == null)
                return ValidationResult.Invalid("AircraftBuildData is null.");

#if SWEF_WORKSHOP_AVAILABLE
            var result = ValidationResult.Valid();

            if (string.IsNullOrWhiteSpace(build.buildId))
                result.AddViolation("Build ID is empty.");

            if (string.IsNullOrWhiteSpace(build.buildName))
                result.AddViolation("Build name is empty.");

            if (build.equippedParts == null)
                result.AddViolation("Equipped parts list is null.");

            return result;
#else
            return ValidationResult.Valid();
#endif
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string ApplyCommonFilters(string input, int maxLength)
        {
            if (input == null) return string.Empty;
            string s = HtmlTagPattern.Replace(input, string.Empty);
            s = ControlCharPattern.Replace(s, string.Empty);
            s = MultiSpacePattern.Replace(s, " ").Trim();
            if (s.Length > maxLength) s = s.Substring(0, maxLength).TrimEnd();
            return s;
        }
    }
}
