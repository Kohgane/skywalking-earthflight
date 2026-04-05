// UGCEnums.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;

namespace SWEF.UGC
{
    // ────────────────────────────────────────────────────────────────────────────
    // Content type
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Classifies the kind of user-generated experience being created.
    /// Used by <see cref="UGCContent.ContentType"/> and browse filters.
    /// </summary>
    public enum UGCContentType
    {
        /// <summary>Guided sightseeing tour along a waypoint path.</summary>
        Tour,
        /// <summary>Objective-driven mission with triggers and goals.</summary>
        Mission,
        /// <summary>Timed race through a series of gates.</summary>
        RaceCourse,
        /// <summary>Open-world scenario with environmental storytelling.</summary>
        Scenario,
        /// <summary>Skill or endurance challenge for the player.</summary>
        Challenge,
        /// <summary>Curated photography hotspot with framing hints.</summary>
        PhotoSpot,
        /// <summary>Shareable collection of navigation waypoints.</summary>
        WaypointPack,
        /// <summary>Pre-planned flight route with altitude profiles.</summary>
        FlightRoute,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Lifecycle status
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents the publishing lifecycle of a <see cref="UGCContent"/> record.
    /// </summary>
    public enum UGCStatus
    {
        /// <summary>Work in progress — not submitted for review.</summary>
        Draft,
        /// <summary>Submitted and awaiting moderation.</summary>
        UnderReview,
        /// <summary>Approved and visible in the community browser.</summary>
        Published,
        /// <summary>Rejected by moderation; creator may revise and re-submit.</summary>
        Rejected,
        /// <summary>Retired by the creator; no longer listed but still installed.</summary>
        Archived,
        /// <summary>Hand-picked by curators for the Featured spotlight.</summary>
        Featured,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Difficulty
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Indicates the skill level expected of the player.
    /// Can be set manually by the creator or estimated by <see cref="UGCTestRunner"/>.
    /// </summary>
    public enum UGCDifficulty
    {
        /// <summary>Accessible to all players regardless of flying experience.</summary>
        Beginner,
        /// <summary>Requires basic flight controls and awareness.</summary>
        Intermediate,
        /// <summary>Demands precise flying and situational awareness.</summary>
        Advanced,
        /// <summary>Very demanding — tight tolerances, complex objectives.</summary>
        Expert,
        /// <summary>Near-impossible for most; intended as a prestige challenge.</summary>
        Extreme,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Category
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Thematic category used for browsing and search filtering.
    /// </summary>
    public enum UGCCategory
    {
        /// <summary>Scenic routes and landmark tours.</summary>
        Sightseeing,
        /// <summary>High-energy stunt flying and daring manoeuvres.</summary>
        Adventure,
        /// <summary>Real-world geography, aviation history, or science topics.</summary>
        Education,
        /// <summary>Competitive races and ranked time trials.</summary>
        Competition,
        /// <summary>Low-stress, ambient cruising experiences.</summary>
        Relaxation,
        /// <summary>Off-the-beaten-path discovery and exploration.</summary>
        Exploration,
        /// <summary>Recreations of historical aviation events.</summary>
        Historical,
        /// <summary>Science-fiction or fantasy world-building scenarios.</summary>
        SciFi,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Editor tools
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Active tool in the UGC editor tool palette.
    /// Controlled by <see cref="UGCEditorHUD"/>.
    /// </summary>
    public enum EditorTool
    {
        /// <summary>Select and inspect placed objects.</summary>
        Select,
        /// <summary>Place new objects (waypoints, triggers, zones).</summary>
        Place,
        /// <summary>Drag placed objects to a new position.</summary>
        Move,
        /// <summary>Rotate a selected object around its vertical axis.</summary>
        Rotate,
        /// <summary>Uniform-scale a selected object (zones and trigger radii).</summary>
        Scale,
        /// <summary>Delete the selected object.</summary>
        Delete,
        /// <summary>Sequential waypoint path drawing mode.</summary>
        Path,
        /// <summary>Paint a zone area onto the terrain.</summary>
        Zone,
        /// <summary>Place and configure event triggers.</summary>
        Trigger,
        /// <summary>Add floating text labels to the world.</summary>
        Text,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Validation severity
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Severity level of a <see cref="ValidationIssue"/> returned by <see cref="UGCValidator"/>.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational hint; does not block publishing.</summary>
        Info,
        /// <summary>Quality concern; does not block but lowers the quality score.</summary>
        Warning,
        /// <summary>Content defect; blocks publishing until resolved.</summary>
        Error,
        /// <summary>Fundamental content problem (e.g. profanity, impossible geometry).</summary>
        Critical,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Star rating
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Five-star rating used in <see cref="UGCReview"/> and aggregate content ratings.
    /// </summary>
    public enum UGCRating
    {
        /// <summary>1 star — very poor.</summary>
        OneStar = 1,
        /// <summary>2 stars — below average.</summary>
        TwoStar = 2,
        /// <summary>3 stars — average.</summary>
        ThreeStar = 3,
        /// <summary>4 stars — good.</summary>
        FourStar = 4,
        /// <summary>5 stars — outstanding.</summary>
        FiveStar = 5,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Trigger types
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Condition that activates a <see cref="UGCTrigger"/> during playback.
    /// </summary>
    public enum UGCTriggerType
    {
        /// <summary>Fires when the player enters the trigger sphere.</summary>
        EnterZone,
        /// <summary>Fires when the player leaves the trigger sphere.</summary>
        ExitZone,
        /// <summary>Fires when the player reaches a specific altitude.</summary>
        AltitudeReached,
        /// <summary>Fires when the player reaches a specific airspeed.</summary>
        SpeedReached,
        /// <summary>Fires after a countdown timer expires.</summary>
        Timer,
        /// <summary>Fires when the player passes through a checkpoint gate.</summary>
        Checkpoint,
        /// <summary>Fires when the player comes within range of a target.</summary>
        Proximity,
        /// <summary>Fires when a specific weather condition is detected.</summary>
        Weather,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Zone types
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Purpose of a <see cref="UGCZone"/> placed in the world.
    /// </summary>
    public enum UGCZoneType
    {
        /// <summary>Objective area — player must enter/exit to progress.</summary>
        Objective,
        /// <summary>Restricted area — entering triggers a penalty or failure.</summary>
        Restricted,
        /// <summary>Safe zone — respawn point or shelter from hazards.</summary>
        Safe,
        /// <summary>Decorative region with no gameplay effect.</summary>
        Decorative,
        /// <summary>Start/finish area for race courses.</summary>
        StartFinish,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Action types
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Effect executed when a <see cref="UGCTrigger"/> fires.
    /// </summary>
    public enum UGCActionType
    {
        /// <summary>Display an on-screen message to the player.</summary>
        ShowMessage,
        /// <summary>Play a named audio clip.</summary>
        PlaySound,
        /// <summary>Spawn a named game object prefab.</summary>
        SpawnObject,
        /// <summary>Transition the weather to a target preset.</summary>
        ChangeWeather,
        /// <summary>Start or reset an on-screen timer.</summary>
        StartTimer,
        /// <summary>Mark the current objective as complete.</summary>
        CompleteObjective,
        /// <summary>Fire a named custom event consumed by other systems.</summary>
        TriggerEvent,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Altitude mode used by placement
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// How the <see cref="UGCPlacementController"/> resolves altitude for placed objects.
    /// </summary>
    public enum AltitudeMode
    {
        /// <summary>Snap to the terrain surface beneath the cursor.</summary>
        GroundLevel,
        /// <summary>Place at a fixed world-space altitude in metres.</summary>
        FixedAltitude,
        /// <summary>Place at an offset above the terrain surface.</summary>
        RelativeToGround,
    }
}
