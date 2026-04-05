// UGCContentData.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    // ════════════════════════════════════════════════════════════════════════════
    // Main content record
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — Full serialisable record for a piece of user-generated content.
    ///
    /// <para>Persisted as JSON inside the <c>ugc_projects/</c> directory and transmitted
    /// during publish/download operations via <see cref="UGCPublishManager"/>.</para>
    /// </summary>
    [Serializable]
    public sealed class UGCContent
    {
        /// <summary>Unique identifier (GUID) for this content record.</summary>
        public string contentId = string.Empty;

        /// <summary>Unique identifier of the player who created this content.</summary>
        public string authorId = string.Empty;

        /// <summary>Display name of the author at the time of publishing.</summary>
        public string authorName = string.Empty;

        /// <summary>Classification of the experience type.</summary>
        public UGCContentType contentType = UGCContentType.Tour;

        /// <summary>Short display title shown in the community browser.</summary>
        public string title = string.Empty;

        /// <summary>Longer description of the content experience.</summary>
        public string description = string.Empty;

        /// <summary>Estimated skill level required to complete the content.</summary>
        public UGCDifficulty difficulty = UGCDifficulty.Intermediate;

        /// <summary>Thematic category used for browsing filters.</summary>
        public UGCCategory category = UGCCategory.Sightseeing;

        /// <summary>Ordered list of navigation waypoints.</summary>
        public List<UGCWaypoint> waypoints = new List<UGCWaypoint>();

        /// <summary>Event triggers placed in the world.</summary>
        public List<UGCTrigger> triggers = new List<UGCTrigger>();

        /// <summary>Zone areas placed in the world.</summary>
        public List<UGCZone> zones = new List<UGCZone>();

        /// <summary>Extra contextual metadata (duration, aircraft, weather).</summary>
        public UGCMetadata metadata = new UGCMetadata();

        /// <summary>Schema / iteration version number; increments on each published update.</summary>
        public int version = 1;

        /// <summary>Aggregate star rating (average of all submitted reviews).</summary>
        public float rating = 0f;

        /// <summary>Total number of times this content has been downloaded.</summary>
        public int downloadCount = 0;

        /// <summary>Current lifecycle status of the content.</summary>
        public UGCStatus status = UGCStatus.Draft;

        /// <summary>Search tags supplied by the creator.</summary>
        public List<string> tags = new List<string>();

        /// <summary>Relative path to the thumbnail image file.</summary>
        public string thumbnailPath = string.Empty;

        /// <summary>ISO-8601 UTC timestamp when the content was first created.</summary>
        public string createdAt = string.Empty;

        /// <summary>ISO-8601 UTC timestamp of the most recent save or update.</summary>
        public string updatedAt = string.Empty;

        /// <summary>
        /// Creates a new <see cref="UGCContent"/> with a fresh GUID and the current UTC timestamp.
        /// </summary>
        public static UGCContent Create(string authorId, string authorName, UGCContentType type)
        {
            var now = DateTime.UtcNow.ToString("o");
            return new UGCContent
            {
                contentId  = Guid.NewGuid().ToString(),
                authorId   = authorId,
                authorName = authorName,
                contentType = type,
                status     = UGCStatus.Draft,
                createdAt  = now,
                updatedAt  = now,
            };
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Waypoint
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — A single navigation waypoint on the route.
    /// </summary>
    [Serializable]
    public sealed class UGCWaypoint
    {
        /// <summary>Unique identifier for this waypoint within its parent <see cref="UGCContent"/>.</summary>
        public string waypointId = string.Empty;

        /// <summary>Geographic latitude in decimal degrees.</summary>
        public double latitude = 0.0;

        /// <summary>Geographic longitude in decimal degrees.</summary>
        public double longitude = 0.0;

        /// <summary>Altitude in metres above sea level.</summary>
        public float altitude = 0f;

        /// <summary>Proximity radius in metres — entering this sphere counts as reaching the waypoint.</summary>
        public float radius = 100f;

        /// <summary>Short label shown above the waypoint in the HUD.</summary>
        public string label = string.Empty;

        /// <summary>Zero-based sequential index of the waypoint in the route.</summary>
        public int order = 0;

        /// <summary>If <c>true</c>, the player must visit this waypoint to complete the content.</summary>
        public bool isRequired = true;

        /// <summary>Minimum time in seconds the player must remain inside the radius before it counts.</summary>
        public float holdTime = 0f;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Trigger
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — An event trigger that fires an action when its condition is met.
    /// </summary>
    [Serializable]
    public sealed class UGCTrigger
    {
        /// <summary>Unique identifier for this trigger.</summary>
        public string triggerId = string.Empty;

        /// <summary>Condition that activates the trigger.</summary>
        public UGCTriggerType triggerType = UGCTriggerType.EnterZone;

        /// <summary>World centre position (lat, lon, alt) stored as a Vector3 (x=lat, y=lon, z=alt).</summary>
        public Vector3 position = Vector3.zero;

        /// <summary>Activation radius in metres around <see cref="position"/>.</summary>
        public float radius = 50f;

        /// <summary>Action executed when the trigger fires.</summary>
        public UGCActionType action = UGCActionType.ShowMessage;

        /// <summary>JSON-serialised key/value parameters passed to the action handler.</summary>
        public string parameters = string.Empty;

        /// <summary>Delay in seconds between the condition being met and the action executing.</summary>
        public float delay = 0f;

        /// <summary>Optional ID of another trigger that this trigger enables when it fires.</summary>
        public string chainToTriggerId = string.Empty;

        /// <summary>Whether this trigger is currently active and listening for its condition.</summary>
        public bool isEnabled = true;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Zone
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — A named area zone placed in the world.
    /// </summary>
    [Serializable]
    public sealed class UGCZone
    {
        /// <summary>Unique identifier for this zone.</summary>
        public string zoneId = string.Empty;

        /// <summary>Classification of the zone's gameplay purpose.</summary>
        public UGCZoneType zoneType = UGCZoneType.Objective;

        /// <summary>Geographic centre (lat, lon, alt) stored as a Vector3 (x=lat, y=lon, z=alt).</summary>
        public Vector3 center = Vector3.zero;

        /// <summary>Horizontal radius of the zone cylinder in metres.</summary>
        public float radius = 500f;

        /// <summary>Vertical extent: x = min altitude (m), y = max altitude (m).</summary>
        public Vector2 altitudeRange = new Vector2(0f, 10000f);

        /// <summary>JSON-serialised free-form properties specific to the zone type.</summary>
        public string properties = string.Empty;

        /// <summary>Display label shown on the in-game map overlay.</summary>
        public string label = string.Empty;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Metadata
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — Supplementary contextual metadata attached to a <see cref="UGCContent"/>.
    /// </summary>
    [Serializable]
    public sealed class UGCMetadata
    {
        /// <summary>Estimated completion time in minutes.</summary>
        public float estimatedDurationMinutes = 0f;

        /// <summary>Estimated total route distance in kilometres.</summary>
        public float estimatedDistanceKm = 0f;

        /// <summary>Name of the recommended aircraft type (e.g. "Cessna 172").</summary>
        public string recommendedAircraft = string.Empty;

        /// <summary>Comma-separated list of SWEF phase feature IDs required to play this content.</summary>
        public string requiredPhases = string.Empty;

        /// <summary>Preferred weather condition identifier (references the weather system preset name).</summary>
        public string weatherCondition = string.Empty;

        /// <summary>Preferred time-of-day setting (e.g. "Dawn", "Noon", "Dusk", "Night").</summary>
        public string timeOfDay = string.Empty;

        /// <summary>Whether the creator has successfully completed a test play of this content.</summary>
        public bool hasBeenTested = false;

        /// <summary>Completion time in seconds recorded during the last test play.</summary>
        public float testCompletionTimeSeconds = 0f;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Review
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — A community review submitted for a published <see cref="UGCContent"/>.
    /// </summary>
    [Serializable]
    public sealed class UGCReview
    {
        /// <summary>Unique identifier for this review.</summary>
        public string reviewId = string.Empty;

        /// <summary>Content ID of the reviewed piece.</summary>
        public string contentId = string.Empty;

        /// <summary>Player ID of the reviewer.</summary>
        public string reviewerId = string.Empty;

        /// <summary>Star rating given by the reviewer.</summary>
        public UGCRating rating = UGCRating.ThreeStar;

        /// <summary>Optional written comment (may be empty).</summary>
        public string comment = string.Empty;

        /// <summary>ISO-8601 UTC timestamp when the review was submitted.</summary>
        public string createdAt = string.Empty;

        /// <summary>Number of players who marked this review as helpful.</summary>
        public int helpfulCount = 0;

        /// <summary>
        /// Creates a new <see cref="UGCReview"/> with a fresh GUID and the current timestamp.
        /// </summary>
        public static UGCReview Create(string contentId, string reviewerId, UGCRating rating, string comment)
        {
            return new UGCReview
            {
                reviewId   = Guid.NewGuid().ToString(),
                contentId  = contentId,
                reviewerId = reviewerId,
                rating     = rating,
                comment    = comment,
                createdAt  = DateTime.UtcNow.ToString("o"),
            };
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Validation result
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — A single issue found by <see cref="UGCValidator"/>.
    /// </summary>
    [Serializable]
    public sealed class ValidationIssue
    {
        /// <summary>Severity of this issue.</summary>
        public ValidationSeverity severity = ValidationSeverity.Info;

        /// <summary>Human-readable description of the problem.</summary>
        public string message = string.Empty;

        /// <summary>Optional suggestion for how to fix the issue.</summary>
        public string suggestion = string.Empty;

        /// <summary>Initialises a new <see cref="ValidationIssue"/>.</summary>
        public ValidationIssue(ValidationSeverity severity, string message, string suggestion = "")
        {
            this.severity   = severity;
            this.message    = message;
            this.suggestion = suggestion;
        }
    }

    /// <summary>
    /// Phase 108 — Aggregate result returned by <see cref="UGCValidator.ValidateContent"/>.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>All issues found during validation, ordered by severity (highest first).</summary>
        public List<ValidationIssue> Issues { get; } = new List<ValidationIssue>();

        /// <summary>Computed quality score 0–100 based on completeness, variety, and test status.</summary>
        public int QualityScore { get; set; }

        /// <summary>Returns <c>true</c> when no <see cref="ValidationSeverity.Error"/> or
        /// <see cref="ValidationSeverity.Critical"/> issues are present.</summary>
        public bool IsPublishable
        {
            get
            {
                foreach (var issue in Issues)
                    if (issue.severity >= ValidationSeverity.Error) return false;
                return true;
            }
        }
    }
}
