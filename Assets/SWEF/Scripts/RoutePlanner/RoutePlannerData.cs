using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    #region Enumerations

    /// <summary>Classifies the primary purpose and tone of a flight route.</summary>
    public enum RouteType
    {
        /// <summary>Relaxed sightseeing route designed for scenery.</summary>
        Scenic,
        /// <summary>Optimised for minimum travel time.</summary>
        Speed,
        /// <summary>Off-the-beaten-path discovery route.</summary>
        Exploration,
        /// <summary>Skill-testing route with obstacles or constraints.</summary>
        Challenge,
        /// <summary>Guided tour converted into a reusable route.</summary>
        Tour,
        /// <summary>Player-created route with no predefined purpose.</summary>
        Custom,
        /// <summary>Competitive route used in timed races.</summary>
        Race,
        /// <summary>Route curated for optimal photo opportunities.</summary>
        Photography
    }

    /// <summary>Specifies the role of an individual waypoint within a route.</summary>
    public enum WaypointType
    {
        /// <summary>Regular navigation waypoint.</summary>
        Standard,
        /// <summary>Waypoint co-located with a named landmark.</summary>
        Landmark,
        /// <summary>Recommended photography vantage point.</summary>
        Photo,
        /// <summary>Timed or ranked checkpoint.</summary>
        Checkpoint,
        /// <summary>Route origin waypoint.</summary>
        Start,
        /// <summary>Route destination waypoint.</summary>
        Finish,
        /// <summary>Designated hover/pause location.</summary>
        RestStop,
        /// <summary>Secret location tied to the HiddenGem system.</summary>
        HiddenGem,
        /// <summary>Waypoint requiring a minimum or exact altitude.</summary>
        Altitude,
        /// <summary>Waypoint requiring the player to reach a target speed.</summary>
        SpeedGate
    }

    /// <summary>Controls who can view and download a shared route.</summary>
    public enum RouteVisibility
    {
        /// <summary>Visible only to the creator.</summary>
        Private,
        /// <summary>Visible to the creator's friends.</summary>
        FriendsOnly,
        /// <summary>Visible to all players.</summary>
        Public
    }

    /// <summary>Determines how strictly the player must follow the planned path.</summary>
    public enum NavigationStyle
    {
        /// <summary>Player may deviate freely; waypoints are suggestions.</summary>
        FreeFollow,
        /// <summary>Player must stay within the off-path threshold.</summary>
        StrictPath,
        /// <summary>Route is scored on elapsed time.</summary>
        TimeAttack,
        /// <summary>No constraints; enjoy the journey at any pace.</summary>
        Relaxed
    }

    /// <summary>Lifecycle state of a route navigation session.</summary>
    public enum RouteStatus
    {
        /// <summary>Route is being authored but is not yet ready to fly.</summary>
        Draft,
        /// <summary>Route is complete and ready to navigate.</summary>
        Ready,
        /// <summary>Player is currently navigating the route.</summary>
        InProgress,
        /// <summary>All required waypoints have been reached.</summary>
        Completed,
        /// <summary>Navigation was cancelled before completion.</summary>
        Abandoned
    }

    #endregion

    #region Waypoint

    /// <summary>
    /// Represents a single point of interest within a <see cref="FlightRoute"/>.
    /// Serialised as part of the route JSON when exported.
    /// </summary>
    [Serializable]
    public class RouteWaypoint
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        /// <summary>Unique identifier for this waypoint (GUID).</summary>
        public string waypointId = Guid.NewGuid().ToString();

        /// <summary>Zero-based position of this waypoint in the route's ordered list.</summary>
        public int index;

        // ── Position ──────────────────────────────────────────────────────────────
        /// <summary>Latitude in decimal degrees (WGS-84).</summary>
        public double latitude;

        /// <summary>Longitude in decimal degrees (WGS-84).</summary>
        public double longitude;

        /// <summary>Altitude above sea level in metres.</summary>
        public float altitude;

        // ── Classification ────────────────────────────────────────────────────────
        /// <summary>Functional role of this waypoint.</summary>
        public WaypointType waypointType = WaypointType.Standard;

        // ── Labels ────────────────────────────────────────────────────────────────
        /// <summary>Optional short label shown in the HUD and waypoint list.</summary>
        public string name = string.Empty;

        /// <summary>Optional longer description shown in the detail panel.</summary>
        public string description = string.Empty;

        // ── Links ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// Id referencing a <c>SWEF.Narration.LandmarkData</c> entry.
        /// <c>null</c> when the waypoint is not tied to a known landmark.
        /// </summary>
        public string landmarkId;

        // ── Behaviour ─────────────────────────────────────────────────────────────
        /// <summary>Distance in metres within which arrival is detected.</summary>
        public float triggerRadius = 100f;

        /// <summary>
        /// Required airspeed in km/h for speed-gate waypoints.
        /// Negative value means no requirement.
        /// </summary>
        public float requiredSpeed = -1f;

        /// <summary>
        /// Required altitude in metres for altitude-checkpoint waypoints.
        /// Negative value means no requirement.
        /// </summary>
        public float requiredAltitude = -1f;

        /// <summary>Seconds the player must remain inside the trigger radius (rest stops).</summary>
        public float stayDuration;

        /// <summary>Suggested camera look direction for photo waypoints (world-space Euler angles).</summary>
        public Vector3 cameraAngleHint;

        /// <summary>
        /// Id referencing a narration clip in <c>SWEF.Narration.NarrationManager</c>.
        /// Triggered automatically when this waypoint is reached.
        /// </summary>
        public string narrationId;

        /// <summary>When <c>true</c> the player may skip this waypoint without failing the route.</summary>
        public bool isOptional;
    }

    #endregion

    #region Route

    /// <summary>
    /// Complete definition of a flight route including all waypoints, metadata, and settings.
    /// Serialised to <c>.swefroute</c> (JSON) files for import/export/sharing.
    /// </summary>
    [Serializable]
    public class FlightRoute
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        /// <summary>Globally unique identifier (GUID).</summary>
        public string routeId = Guid.NewGuid().ToString();

        /// <summary>Display name of the route.</summary>
        public string name = "New Route";

        /// <summary>Brief description of what makes this route interesting.</summary>
        public string description = string.Empty;

        // ── Classification ────────────────────────────────────────────────────────
        /// <summary>Primary purpose/tone of the route.</summary>
        public RouteType routeType = RouteType.Custom;

        /// <summary>Difficulty rating from 1 (easiest) to 5 (hardest).</summary>
        public int difficulty = 1;

        // ── Waypoints ─────────────────────────────────────────────────────────────
        /// <summary>Ordered list of waypoints that define the route.</summary>
        public List<RouteWaypoint> waypoints = new List<RouteWaypoint>();

        // ── Estimates ─────────────────────────────────────────────────────────────
        /// <summary>Pre-calculated estimated flight time in minutes.</summary>
        public float estimatedDuration;

        /// <summary>Pre-calculated total path distance in kilometres.</summary>
        public float estimatedDistance;

        // ── Altitude bounds ───────────────────────────────────────────────────────
        /// <summary>Highest altitude waypoint in metres ASL.</summary>
        public float maxAltitude;

        /// <summary>Lowest altitude waypoint in metres ASL.</summary>
        public float minAltitude;

        // ── Start / End ───────────────────────────────────────────────────────────
        /// <summary>Starting latitude in decimal degrees.</summary>
        public double startLatitude;
        /// <summary>Starting longitude in decimal degrees.</summary>
        public double startLongitude;
        /// <summary>Starting altitude in metres ASL.</summary>
        public float startAltitude;

        /// <summary>Ending latitude in decimal degrees.</summary>
        public double endLatitude;
        /// <summary>Ending longitude in decimal degrees.</summary>
        public double endLongitude;
        /// <summary>Ending altitude in metres ASL.</summary>
        public float endAltitude;

        /// <summary><c>true</c> when the route's last waypoint connects back to the start.</summary>
        public bool isLoop;

        // ── Authorship ────────────────────────────────────────────────────────────
        /// <summary>Platform user-id of the route creator.</summary>
        public string creatorId = string.Empty;

        /// <summary>Display name of the route creator.</summary>
        public string creatorName = string.Empty;

        /// <summary>ISO-8601 creation timestamp.</summary>
        public string createdAt = DateTime.UtcNow.ToString("o");

        /// <summary>ISO-8601 last-updated timestamp.</summary>
        public string updatedAt = DateTime.UtcNow.ToString("o");

        // ── Sharing ───────────────────────────────────────────────────────────────
        /// <summary>Who can see this route.</summary>
        public RouteVisibility visibility = RouteVisibility.Private;

        /// <summary>Searchable tags (e.g. "mountains", "sunset", "fast").</summary>
        public List<string> tags = new List<string>();

        // ── Community stats ───────────────────────────────────────────────────────
        /// <summary>Average community rating, 0–5.</summary>
        public float rating;

        /// <summary>Number of community ratings received.</summary>
        public int ratingCount;

        /// <summary>Number of times this route has been downloaded.</summary>
        public int downloadCount;

        /// <summary>Number of times this route has been completed by any player.</summary>
        public int completionCount;

        // ── Media ─────────────────────────────────────────────────────────────────
        /// <summary>Relative or absolute path to the route thumbnail image.</summary>
        public string thumbnailPath = string.Empty;

        // ── Versioning ────────────────────────────────────────────────────────────
        /// <summary>Incremented each time the route is modified and saved.</summary>
        public int version = 1;

        // ── Navigation ────────────────────────────────────────────────────────────
        /// <summary>Preferred navigation behaviour for this route.</summary>
        public NavigationStyle navigationStyle = NavigationStyle.FreeFollow;

        // ── Recommendations ───────────────────────────────────────────────────────
        /// <summary>Human-readable weather recommendation (e.g. "Best in clear weather").</summary>
        public string weatherRecommendation = string.Empty;

        /// <summary>Human-readable time-of-day recommendation (e.g. "Best at sunset").</summary>
        public string timeOfDayRecommendation = string.Empty;
    }

    #endregion

    #region Progress

    /// <summary>
    /// Tracks the player's live and historical progress for a single route navigation session.
    /// </summary>
    [Serializable]
    public class RouteProgress
    {
        /// <summary>Id of the route being navigated.</summary>
        public string routeId = string.Empty;

        /// <summary>Current lifecycle state of the navigation session.</summary>
        public RouteStatus status = RouteStatus.Draft;

        /// <summary>Index of the waypoint the player is currently heading toward.</summary>
        public int currentWaypointIndex;

        /// <summary>Indices of waypoints that have already been reached in this session.</summary>
        public List<int> waypointsReached = new List<int>();

        /// <summary>UTC epoch seconds when navigation began.</summary>
        public float startTime;

        /// <summary>Seconds elapsed since navigation began.</summary>
        public float elapsedTime;

        /// <summary>Total distance flown during this session in kilometres.</summary>
        public float distanceTraveled;

        /// <summary>Number of times the player went beyond the off-path threshold.</summary>
        public int deviations;

        /// <summary>Player's personal-best completion time in seconds. Negative if never completed.</summary>
        public float bestTime = -1f;

        /// <summary>Player's own rating for this route (0–5). Negative if not yet rated.</summary>
        public float rating = -1f;
    }

    #endregion

    #region Config

    /// <summary>
    /// Persistent user preferences for the Route Planner &amp; navigation system.
    /// </summary>
    [Serializable]
    public class RoutePlannerConfig
    {
        /// <summary>Render the planned 3D path line in the world before and during flight.</summary>
        public bool showPathPreview = true;

        /// <summary>Colour of the rendered path line.</summary>
        public Color pathColor = Color.cyan;

        /// <summary>Uniform scale applied to all waypoint 3D markers.</summary>
        public float waypointScale = 1f;

        /// <summary>Automatically advance to the next waypoint once the trigger radius is entered.</summary>
        public bool autoAdvanceWaypoints = true;

        /// <summary>Show distance-to-next-waypoint indicator in the HUD.</summary>
        public bool showDistanceToNext = true;

        /// <summary>Show estimated time-of-arrival indicator in the HUD.</summary>
        public bool showETA = true;

        /// <summary>Display a warning when the player deviates beyond <see cref="offPathThreshold"/>.</summary>
        public bool offPathWarning = true;

        /// <summary>Distance in metres before an off-path warning is triggered.</summary>
        public float offPathThreshold = 500f;

        /// <summary>Show the route path and waypoints on the minimap.</summary>
        public bool showRouteOnMinimap = true;

        /// <summary>Default navigation style applied when starting a route without an explicit override.</summary>
        public NavigationStyle navigationStyle = NavigationStyle.FreeFollow;

        /// <summary>Enable text-to-speech turn-by-turn navigation voice.</summary>
        public bool enableVoiceNavigation;

        /// <summary>Hard limit on the number of waypoints allowed in a single user-created route.</summary>
        public int maxWaypointsPerRoute = 50;

        /// <summary>Automatically save route drafts while the builder is open.</summary>
        public bool autoSaveDrafts = true;
    }

    #endregion
}
