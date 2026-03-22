using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WeatherChallenge
{
    // ── ChallengeWeatherType ───────────────────────────────────────────────────

    /// <summary>The weather scenario that defines the environmental conditions of a challenge.</summary>
    public enum ChallengeWeatherType
    {
        /// <summary>Blue skies, no wind — baseline visibility challenge.</summary>
        ClearSkies,
        /// <summary>Reduced visibility due to thick fog layers.</summary>
        Fog,
        /// <summary>Heavy rain reducing visibility and increasing turbulence.</summary>
        Rain,
        /// <summary>Storm chasing — lightning, heavy turbulence, severe wind shear.</summary>
        Thunderstorm,
        /// <summary>Winter flying — icing risk, low visibility, crosswinds.</summary>
        Snow,
        /// <summary>Constant moderate-to-severe turbulence throughout the route.</summary>
        Turbulence,
        /// <summary>Strong persistent lateral wind requiring constant correction.</summary>
        Crosswind,
        /// <summary>Thermal soaring — ride rising air columns to gain altitude without engine.</summary>
        Thermal,
        /// <summary>Structural icing risk at altitude — must stay within safe temperature bands.</summary>
        Icing
    }

    // ── ChallengeDifficulty ────────────────────────────────────────────────────

    /// <summary>Difficulty tier of a weather challenge, affecting waypoint count, radius, and score multipliers.</summary>
    public enum ChallengeDifficulty
    {
        /// <summary>Wide waypoint radii, generous time limit, fewer waypoints.</summary>
        Easy,
        /// <summary>Standard challenge parameters.</summary>
        Medium,
        /// <summary>Tighter radii, stricter time limit, more waypoints.</summary>
        Hard,
        /// <summary>Minimal radius, shortest time, maximum waypoints, no margin for error.</summary>
        Extreme
    }

    // ── ChallengeStatus ────────────────────────────────────────────────────────

    /// <summary>Lifecycle state of a weather challenge from generation to resolution.</summary>
    public enum ChallengeStatus
    {
        /// <summary>Challenge has been generated and is ready to start.</summary>
        Available,
        /// <summary>Challenge is currently in progress.</summary>
        Active,
        /// <summary>All required waypoints reached before the time limit.</summary>
        Completed,
        /// <summary>Time limit exceeded or player crashed.</summary>
        Failed,
        /// <summary>Challenge's expiry time has passed without being started.</summary>
        Expired
    }

    // ── RouteWaypoint ──────────────────────────────────────────────────────────

    /// <summary>
    /// A single geo-referenced waypoint on a weather challenge route.
    /// Serialised as part of <see cref="WeatherChallenge"/>.
    /// </summary>
    [Serializable]
    public class RouteWaypoint
    {
        /// <summary>Unique identifier for this waypoint within the challenge.</summary>
        public string waypointId = Guid.NewGuid().ToString();

        /// <summary>Human-readable display name shown in the HUD.</summary>
        public string waypointName = "Waypoint";

        /// <summary>Geographic latitude in decimal degrees.</summary>
        public double latitude;

        /// <summary>Geographic longitude in decimal degrees.</summary>
        public double longitude;

        /// <summary>Target altitude in metres above sea level.</summary>
        public double altitude;

        /// <summary>
        /// Action the player must perform at this waypoint.
        /// E.g. <c>"fly_through"</c>, <c>"hold_altitude"</c>, <c>"avoid_zone"</c>.
        /// </summary>
        public string requiredAction = "fly_through";

        /// <summary>Proximity radius in metres within which the waypoint is considered reached.</summary>
        public float radiusMeters = 200f;

        /// <summary>
        /// When <c>true</c> the waypoint is a bonus objective and does not count toward
        /// <see cref="WeatherChallenge.CompletionPercentage"/>.
        /// </summary>
        public bool isOptional;

        /// <summary>Whether the player has already reached this waypoint in the active run.</summary>
        public bool isReached;

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when the supplied position is within <see cref="radiusMeters"/>
        /// of this waypoint, using a haversine horizontal distance check combined with a
        /// generous vertical window (±<see cref="radiusMeters"/> metres).
        /// </summary>
        /// <param name="lat">Player latitude in decimal degrees.</param>
        /// <param name="lon">Player longitude in decimal degrees.</param>
        /// <param name="alt">Player altitude in metres above sea level.</param>
        public bool IsReached(double lat, double lon, double alt)
        {
            double horizDist = HaversineMeters(latitude, longitude, lat, lon);
            double vertDiff  = Math.Abs(alt - altitude);
            return horizDist <= radiusMeters && vertDiff <= radiusMeters;
        }

        /// <summary>Computes the haversine great-circle distance in metres between two lat/lon pairs.</summary>
        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6_371_000.0; // Earth radius in metres
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }
    }

    // ── WeatherChallenge ───────────────────────────────────────────────────────

    /// <summary>
    /// Complete definition of a single weather challenge, including route waypoints,
    /// scoring parameters, weather modifiers, and lifecycle metadata.
    /// Serialised to JSON via <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class WeatherChallenge
    {
        // ── Identity ───────────────────────────────────────────────────────────

        /// <summary>Globally unique identifier (GUID) for the challenge.</summary>
        public string challengeId = Guid.NewGuid().ToString();

        /// <summary>Short display title shown in the challenge browser.</summary>
        public string title = "Weather Challenge";

        /// <summary>Longer description explaining the scenario and objectives.</summary>
        public string description = string.Empty;

        // ── Classification ─────────────────────────────────────────────────────

        /// <summary>The weather scenario governing environmental conditions.</summary>
        public ChallengeWeatherType weatherType = ChallengeWeatherType.ClearSkies;

        /// <summary>Difficulty tier affecting route parameters and scoring.</summary>
        public ChallengeDifficulty difficulty = ChallengeDifficulty.Medium;

        /// <summary>Current lifecycle state of the challenge.</summary>
        public ChallengeStatus status = ChallengeStatus.Available;

        // ── Route ──────────────────────────────────────────────────────────────

        /// <summary>Ordered list of waypoints defining the challenge route.</summary>
        public List<RouteWaypoint> waypoints = new List<RouteWaypoint>();

        // ── Timing ────────────────────────────────────────────────────────────

        /// <summary>Total time allowed to complete the challenge, in seconds.</summary>
        public float timeLimit = 300f;

        /// <summary>Elapsed time since the challenge was started, in seconds.</summary>
        public float elapsedTime;

        // ── Scoring ───────────────────────────────────────────────────────────

        /// <summary>Maximum possible score for this challenge.</summary>
        public int maxScore = 1000;

        /// <summary>Player's current score during or after the challenge.</summary>
        public int currentScore;

        // ── Bonus Objective ───────────────────────────────────────────────────

        /// <summary>Optional bonus objective description displayed to the player.</summary>
        public string bonusObjective = string.Empty;

        /// <summary>Extra score awarded for completing the bonus objective.</summary>
        public int bonusScore;

        /// <summary>Whether the player has completed the bonus objective.</summary>
        public bool bonusCompleted;

        // ── Timestamps ────────────────────────────────────────────────────────

        /// <summary>ISO 8601 timestamp at which this challenge was generated.</summary>
        public string createdAt = DateTime.UtcNow.ToString("o");

        /// <summary>ISO 8601 timestamp after which the challenge can no longer be started.</summary>
        public string expiresAt = DateTime.UtcNow.AddHours(24).ToString("o");

        // ── Altitude Constraints ──────────────────────────────────────────────

        /// <summary>Minimum altitude (metres ASL) the player must maintain throughout.</summary>
        public float requiredAltitudeMin;

        /// <summary>Maximum altitude (metres ASL) the player must stay below throughout.</summary>
        public float requiredAltitudeMax = 10000f;

        // ── Weather Modifiers ─────────────────────────────────────────────────

        /// <summary>
        /// Multiplier applied to the base wind speed for this challenge.
        /// Values greater than 1 intensify the wind; less than 1 reduce it.
        /// </summary>
        public float windSpeedMultiplier = 1f;

        /// <summary>
        /// Multiplier applied to the base visibility range for this challenge.
        /// Values less than 1 reduce visibility; 1 = normal.
        /// </summary>
        public float visibilityMultiplier = 1f;

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Returns the remaining time in seconds, clamped to zero.</summary>
        public float TimeRemaining() => Mathf.Max(0f, timeLimit - elapsedTime);

        /// <summary>
        /// Returns the fraction of required (non-optional) waypoints that have been reached,
        /// expressed as a value between 0 and 1.
        /// Returns 1 if there are no required waypoints.
        /// </summary>
        public float CompletionPercentage()
        {
            int required = 0;
            int reached  = 0;
            foreach (RouteWaypoint wp in waypoints)
            {
                if (wp.isOptional) continue;
                required++;
                if (wp.isReached) reached++;
            }
            return required == 0 ? 1f : (float)reached / required;
        }

        /// <summary>
        /// Returns <c>true</c> when the current UTC time has passed <see cref="expiresAt"/>.
        /// Falls back to <c>false</c> if the timestamp cannot be parsed.
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.TryParse(expiresAt, null,
                       System.Globalization.DateTimeStyles.RoundtripKind,
                       out DateTime expiry)
                   && DateTime.UtcNow > expiry;
        }
    }
}
