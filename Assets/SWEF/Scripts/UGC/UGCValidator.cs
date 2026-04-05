// UGCValidator.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — Static utility that validates a <see cref="UGCContent"/> project
    /// and produces a <see cref="ValidationResult"/> with severity-graded issues and
    /// a computed quality score (0–100).
    ///
    /// <para>Uses <c>SWEF.Security.ProfanityFilter</c> when available (guarded by
    /// <c>SWEF_SECURITY_AVAILABLE</c>).</para>
    /// </summary>
    public static class UGCValidator
    {
        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Validates <paramref name="content"/> and returns a <see cref="ValidationResult"/>
        /// containing all found issues and a quality score.
        /// </summary>
        /// <param name="content">The content to validate. Must not be <c>null</c>.</param>
        public static ValidationResult ValidateContent(UGCContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var result = new ValidationResult();

            CheckTitle(content, result);
            CheckDescription(content, result);
            CheckWaypoints(content, result);
            CheckTriggers(content, result);
            CheckZones(content, result);
            CheckMetadata(content, result);
            CheckProfanity(content, result);
            CheckPerformanceBudget(content, result);

            result.QualityScore = ComputeQualityScore(content, result);

            return result;
        }

        // ── Private checks ─────────────────────────────────────────────────────

        private static void CheckTitle(UGCContent content, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(content.title))
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Title is empty.",
                    "Add a short descriptive title for your content."));
            }
            else if (content.title.Length < UGCConfig.MinTitleLength)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Title is too short (minimum {UGCConfig.MinTitleLength} characters).",
                    "Provide a more descriptive title."));
            }
            else if (content.title.Length > UGCConfig.MaxTitleLength)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Title exceeds maximum length of {UGCConfig.MaxTitleLength} characters.",
                    "Shorten the title."));
            }
        }

        private static void CheckDescription(UGCContent content, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(content.description))
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Description is empty.",
                    "Add a description so players know what to expect."));
            }
            else if (content.description.Length > UGCConfig.MaxDescriptionLength)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Description exceeds maximum length of {UGCConfig.MaxDescriptionLength} characters.",
                    "Shorten the description."));
            }
        }

        private static void CheckWaypoints(UGCContent content, ValidationResult result)
        {
            int count = content.waypoints.Count;

            if (count < UGCConfig.MinWaypointsRequired)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"At least {UGCConfig.MinWaypointsRequired} waypoints are required (found {count}).",
                    "Add more waypoints to define the route."));
                return;
            }

            foreach (var wp in content.waypoints)
            {
                if (wp.latitude < -90.0 || wp.latitude > 90.0 ||
                    wp.longitude < -180.0 || wp.longitude > 180.0)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Critical,
                        $"Waypoint '{wp.waypointId}' has invalid coordinates ({wp.latitude}, {wp.longitude}).",
                        "Remove or reposition the waypoint."));
                }

                if (wp.altitude < 0f || wp.altitude > UGCConfig.MaxAltitudeMetres)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Waypoint '{wp.label}' altitude {wp.altitude}m is outside the expected range.",
                        "Adjust the waypoint altitude."));
                }

                if (wp.radius <= 0f)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Waypoint '{wp.label}' has a radius of 0 — it may be impossible to trigger.",
                        "Set the waypoint radius to at least 10 m."));
                }
            }

            // Check consecutive spacing
            var sorted = new List<UGCWaypoint>(content.waypoints);
            sorted.Sort((a, b) => a.order.CompareTo(b.order));
            for (int i = 1; i < sorted.Count; i++)
            {
                double dist = GeoDistance(sorted[i - 1], sorted[i]);
                if (dist > UGCConfig.MaxWaypointSpacingMetres)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Waypoints {i} and {i + 1} are very far apart ({dist / 1000f:F0} km).",
                        "Consider adding intermediate waypoints."));
                }
            }
        }

        private static void CheckTriggers(UGCContent content, ValidationResult result)
        {
            var ids = new HashSet<string>();
            foreach (var trigger in content.triggers)
            {
                ids.Add(trigger.triggerId);

                if (trigger.radius < UGCConfig.MinTriggerRadiusMetres)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Trigger '{trigger.triggerId}' radius {trigger.radius}m is below minimum.",
                        $"Increase the radius to at least {UGCConfig.MinTriggerRadiusMetres}m."));
                }
            }

            // Check for orphan chain targets
            foreach (var trigger in content.triggers)
            {
                if (!string.IsNullOrEmpty(trigger.chainToTriggerId) && !ids.Contains(trigger.chainToTriggerId))
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        $"Trigger '{trigger.triggerId}' chains to a missing trigger ID.",
                        "Remove the chain link or add the target trigger."));
                }
            }
        }

        private static void CheckZones(UGCContent content, ValidationResult result)
        {
            foreach (var zone in content.zones)
            {
                if (zone.radius <= 0f)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Zone '{zone.zoneId}' has a zero radius.",
                        "Set a positive radius for the zone."));
                }

                if (zone.altitudeRange.x >= zone.altitudeRange.y)
                {
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        $"Zone '{zone.zoneId}' has an inverted altitude range.",
                        "Ensure min altitude is less than max altitude."));
                }
            }
        }

        private static void CheckMetadata(UGCContent content, ValidationResult result)
        {
            if (!content.metadata.hasBeenTested)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Content has not been test-played.",
                    "Use the Test Play button to verify your content is completable."));
            }
        }

        private static void CheckProfanity(UGCContent content, ValidationResult result)
        {
#if SWEF_SECURITY_AVAILABLE
            bool titleFlagged       = Security.ProfanityFilter.ContainsProfanity(content.title);
            bool descFlagged        = Security.ProfanityFilter.ContainsProfanity(content.description);

            if (titleFlagged)
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Critical,
                    "Title contains prohibited language.",
                    "Remove offensive words from the title."));

            if (descFlagged)
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Critical,
                    "Description contains prohibited language.",
                    "Remove offensive words from the description."));

            foreach (var wp in content.waypoints)
            {
                if (Security.ProfanityFilter.ContainsProfanity(wp.label))
                    result.Issues.Add(new ValidationIssue(
                        ValidationSeverity.Critical,
                        $"Waypoint label '{wp.label}' contains prohibited language.",
                        "Remove offensive language from waypoint labels."));
            }
#endif
        }

        private static void CheckPerformanceBudget(UGCContent content, ValidationResult result)
        {
            int total = content.waypoints.Count + content.triggers.Count + content.zones.Count;
            if (total > (UGCConfig.MaxWaypoints + UGCConfig.MaxTriggers + UGCConfig.MaxZones) * 0.9f)
            {
                result.Issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Content is approaching the maximum object budget — performance may be affected.",
                    "Consider reducing the number of triggers or zones."));
            }
        }

        // ── Quality score computation ──────────────────────────────────────────

        private static int ComputeQualityScore(UGCContent content, ValidationResult result)
        {
            int score = 100;

            // Deduct for issues
            foreach (var issue in result.Issues)
            {
                switch (issue.severity)
                {
                    case ValidationSeverity.Info:     score -= 0;  break;
                    case ValidationSeverity.Warning:  score -= 5;  break;
                    case ValidationSeverity.Error:    score -= 15; break;
                    case ValidationSeverity.Critical: score -= 30; break;
                }
            }

            // Bonuses
            if (content.metadata.hasBeenTested)                    score += 10;
            if (content.tags.Count >= 3)                            score += 5;
            if (!string.IsNullOrEmpty(content.metadata.recommendedAircraft)) score += 3;
            if (!string.IsNullOrEmpty(content.thumbnailPath))       score += 5;
            if (content.waypoints.Count >= 5)                       score += 5;
            if (content.triggers.Count >= 2)                        score += 3;

            return Mathf.Clamp(score, 0, 100);
        }

        // ── Geo helpers ────────────────────────────────────────────────────────

        private static double GeoDistance(UGCWaypoint a, UGCWaypoint b)
        {
            const double R = 6_371_000.0;
            double dLat = (b.latitude  - a.latitude)  * Math.PI / 180.0;
            double dLon = (b.longitude - a.longitude)  * Math.PI / 180.0;
            double lat1 = a.latitude * Math.PI / 180.0;
            double lat2 = b.latitude * Math.PI / 180.0;

            double s = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1) * Math.Cos(lat2)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return R * 2.0 * Math.Atan2(Math.Sqrt(s), Math.Sqrt(1.0 - s));
        }
    }
}
