// AdvancedPhotographyData.cs — SWEF Advanced Photography & Drone Camera System (Phase 89)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AdvancedPhotography
{
    // ── DroneWaypoint ─────────────────────────────────────────────────────────────

    /// <summary>A single point in a <see cref="DroneFlightPath"/>.</summary>
    [Serializable]
    public class DroneWaypoint
    {
        /// <summary>World-space position of this waypoint.</summary>
        public Vector3 position;

        /// <summary>Camera rotation at this waypoint.</summary>
        public Quaternion rotation = Quaternion.identity;

        /// <summary>Travel speed (m/s) from the previous waypoint to this one.</summary>
        [Min(0.1f)] public float speed = 10f;

        /// <summary>Time (seconds) the drone hovers at this waypoint before continuing.</summary>
        [Min(0f)] public float holdTime = 0f;

        /// <summary>When true, the drone rotates to look at the player transform at this waypoint.</summary>
        public bool lookAtTarget = false;
    }

    // ── DroneFlightPath ───────────────────────────────────────────────────────────

    /// <summary>A sequence of <see cref="DroneWaypoint"/> objects defining an autonomous drone route.</summary>
    [Serializable]
    public class DroneFlightPath
    {
        /// <summary>Ordered list of waypoints.</summary>
        public List<DroneWaypoint> waypoints = new List<DroneWaypoint>();

        /// <summary>When true, the drone loops back to the first waypoint after the last.</summary>
        public bool loop = false;

        /// <summary>Total estimated flight duration in seconds (sum of segment travel times + hold times).</summary>
        public float totalDuration = 0f;
    }

    // ── CompositionAnalysis ───────────────────────────────────────────────────────

    /// <summary>Result of a single composition analysis frame.</summary>
    [Serializable]
    public class CompositionAnalysis
    {
        /// <summary>Composition rule that was evaluated.</summary>
        public CompositionRule rule;

        /// <summary>Normalised score in [0, 1] where 1 is a perfect composition.</summary>
        [Range(0f, 1f)] public float score;

        /// <summary>Human-readable suggestion text shown to the player.</summary>
        public string suggestion = "";

        /// <summary>
        /// Screen-space guide points (normalised 0–1) used to draw composition overlays
        /// (e.g. rule-of-thirds intersections, golden ratio spiral control points).
        /// </summary>
        public Vector2[] guidePoints = System.Array.Empty<Vector2>();
    }

    // ── PhotoMetadata ─────────────────────────────────────────────────────────────

    /// <summary>Rich metadata record saved alongside each advanced photo capture.</summary>
    [Serializable]
    public class PhotoMetadata
    {
        /// <summary>Unique GUID identifier for this photo.</summary>
        public string photoId = "";

        /// <summary>UTC capture timestamp as an ISO-8601 string.</summary>
        public string timestamp = "";

        // ── Location ──────────────────────────────────────────────────────────────

        /// <summary>Latitude at capture time (WGS-84 degrees).</summary>
        public double latitude;

        /// <summary>Longitude at capture time (WGS-84 degrees).</summary>
        public double longitude;

        /// <summary>Altitude at capture time (metres above sea level).</summary>
        public double altitude;

        // ── Context ───────────────────────────────────────────────────────────────

        /// <summary>Biome name at capture location.</summary>
        public string biome = "";

        /// <summary>Weather condition description at capture time.</summary>
        public string weather = "";

        /// <summary>Filter applied at capture time.</summary>
        public string filter = "";

        /// <summary>Frame style applied at capture time.</summary>
        public string frame = "";

        /// <summary>Camera field of view at capture time.</summary>
        public float fieldOfView = 60f;

        /// <summary>Camera aperture f-stop at capture time.</summary>
        public float aperture = 5.6f;

        /// <summary>Camera ISO at capture time.</summary>
        public int iso = 400;

        /// <summary>Normalised composition score at capture time.</summary>
        [Range(0f, 1f)] public float compositionScore;

        /// <summary>Photo subject categories identified at capture time.</summary>
        public List<PhotoSubject> subjects = new List<PhotoSubject>();

        // ── File ──────────────────────────────────────────────────────────────────

        /// <summary>Absolute path to the saved photo file.</summary>
        public string filePath = "";

        /// <summary>Pixel width of the captured image.</summary>
        public int width;

        /// <summary>Pixel height of the captured image.</summary>
        public int height;
    }

    // ── PhotoChallenge ────────────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject defining a photography challenge mission.
    /// Create via <i>Assets → Create → SWEF/AdvancedPhotography/Photo Challenge</i>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "SWEF/AdvancedPhotography/Photo Challenge",
        fileName = "PhotoChallenge")]
    public class PhotoChallenge : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string identifier for this challenge.")]
        public string challengeId = "";

        [Tooltip("Display title shown in the challenge UI.")]
        public string title = "";

        [Tooltip("Longer description explaining the photography objective.")]
        [TextArea(2, 4)] public string description = "";

        [Header("Category & Timing")]
        [Tooltip("Time-window category: Daily, Weekly, Seasonal, Special, Community.")]
        public ChallengeCategory category = ChallengeCategory.Daily;

        [Tooltip("UTC start time as an ISO-8601 string.")]
        public string startTime = "";

        [Tooltip("UTC end time as an ISO-8601 string.")]
        public string endTime = "";

        [Header("Criteria")]
        [Tooltip("Comma-separated list of scoring criteria tags.")]
        public string criteria = "";

        [Tooltip("Primary subject type required for this challenge.")]
        public PhotoSubject targetSubject = PhotoSubject.Landscape;

        [Tooltip("Required biome name; empty = any biome.")]
        public string targetBiome = "";

        [Tooltip("Composition rule that must be demonstrated.")]
        public CompositionRule requiredCompositionRule = CompositionRule.RuleOfThirds;

        [Header("Reward")]
        [Tooltip("Experience points awarded upon challenge completion.")]
        [Min(0)] public int rewardXP = 100;
    }

    // ── PhotoSpot ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// A scenic photography location registered in <see cref="PhotoSpotDiscovery"/>.
    /// </summary>
    [Serializable]
    public class PhotoSpot
    {
        /// <summary>Unique string identifier for this spot.</summary>
        public string spotId = "";

        /// <summary>World-space position of the spot centre.</summary>
        public Vector3 position;

        /// <summary>Subject types that are well-suited to this location.</summary>
        public List<PhotoSubject> recommendedSubjects = new List<PhotoSubject>();

        /// <summary>Best time of day range (0–24 h) for photography. x=start, y=end.</summary>
        public Vector2 bestTimeOfDayRange = new Vector2(6f, 10f);

        /// <summary>Weather condition name that produces the best shots here; empty = any.</summary>
        public string bestWeather = "";

        /// <summary>Real-world season name (Spring/Summer/Autumn/Winter) for best shots; empty = any.</summary>
        public string bestSeason = "";

        /// <summary>Difficulty score (1–5) indicating how hard the spot is to reach.</summary>
        [Range(1, 5)] public int difficulty = 1;

        /// <summary>Whether the player has already discovered this spot.</summary>
        public bool discovered = false;
    }
}
