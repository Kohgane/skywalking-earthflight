using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.WeatherChallenge
{
    /// <summary>
    /// Phase 53 — Core singleton manager for the Weather Challenges &amp; Dynamic Route system.
    /// Handles challenge generation, activation, progress tracking, scoring,
    /// persistence, and event dispatch.
    /// </summary>
    public class WeatherChallengeManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static WeatherChallengeManager Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private DynamicRouteGenerator _routeGenerator;

        /// <summary>Default origin latitude used when no explicit origin is provided.</summary>
        [SerializeField] private double defaultOriginLat  = 37.5665;

        /// <summary>Default origin longitude used when no explicit origin is provided.</summary>
        [SerializeField] private double defaultOriginLon  = 126.9780;

        /// <summary>Default origin altitude (metres ASL).</summary>
        [SerializeField] private float  defaultOriginAlt  = 500f;

        #endregion

        #region Events

        /// <summary>Fired when a new challenge has been procedurally generated.</summary>
        public event Action<WeatherChallenge> OnChallengeGenerated;

        /// <summary>Fired when a challenge transitions to the <see cref="ChallengeStatus.Active"/> state.</summary>
        public event Action<WeatherChallenge> OnChallengeStarted;

        /// <summary>Fired when the player reaches a waypoint during an active challenge.</summary>
        public event Action<RouteWaypoint> OnWaypointReached;

        /// <summary>Fired when a challenge is successfully completed.</summary>
        public event Action<WeatherChallenge> OnChallengeCompleted;

        /// <summary>Fired when a challenge fails (timeout or crash).</summary>
        public event Action<WeatherChallenge> OnChallengeFailed;

        #endregion

        #region State

        /// <summary>All challenges currently held in memory (available + historical).</summary>
        public List<WeatherChallenge> allChallenges { get; private set; } = new List<WeatherChallenge>();

        /// <summary>The challenge currently being played, or <c>null</c> if none is active.</summary>
        public WeatherChallenge activeChallenge { get; private set; }

        private const string SaveFileName = "/weatherchallenges.json";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadChallenges();
        }

        private void Update()
        {
            UpdateActiveChallenge();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveChallenges();
        }

        private void OnApplicationQuit()
        {
            SaveChallenges();
        }

        #endregion

        #region Challenge Generation

        /// <summary>
        /// Procedurally creates a new <see cref="WeatherChallenge"/> and adds it to
        /// <see cref="allChallenges"/>. Fires <see cref="OnChallengeGenerated"/>.
        /// </summary>
        /// <param name="weatherType">The weather scenario for the new challenge.</param>
        /// <param name="difficulty">The difficulty tier of the new challenge.</param>
        /// <returns>The newly generated challenge.</returns>
        public WeatherChallenge GenerateChallenge(ChallengeWeatherType weatherType, ChallengeDifficulty difficulty)
        {
            var challenge = new WeatherChallenge
            {
                challengeId = Guid.NewGuid().ToString(),
                weatherType = weatherType,
                difficulty  = difficulty,
                status      = ChallengeStatus.Available,
                createdAt   = DateTime.UtcNow.ToString("o"),
                expiresAt   = DateTime.UtcNow.AddHours(GetExpiryHours(difficulty)).ToString("o")
            };

            ApplyChallengeTemplate(challenge);

            if (_routeGenerator != null)
            {
                challenge.waypoints = _routeGenerator.GenerateRoute(
                    defaultOriginLat, defaultOriginLon, defaultOriginAlt, difficulty, weatherType);
            }

            allChallenges.Add(challenge);
            OnChallengeGenerated?.Invoke(challenge);
            SaveChallenges();
            return challenge;
        }

        #endregion

        #region Challenge Lifecycle

        /// <summary>
        /// Activates a challenge by its identifier, begins the timer, and fires
        /// <see cref="OnChallengeStarted"/>. Any previously active challenge is first abandoned.
        /// </summary>
        /// <param name="challengeId">The <see cref="WeatherChallenge.challengeId"/> to start.</param>
        public void StartChallenge(string challengeId)
        {
            WeatherChallenge challenge = FindChallenge(challengeId);
            if (challenge == null)
            {
                Debug.LogWarning($"[WeatherChallengeManager] Challenge not found: {challengeId}");
                return;
            }

            if (challenge.IsExpired())
            {
                challenge.status = ChallengeStatus.Expired;
                Debug.LogWarning($"[WeatherChallengeManager] Challenge {challengeId} is expired.");
                return;
            }

            if (activeChallenge != null && activeChallenge.challengeId != challengeId)
                FailChallenge(activeChallenge.challengeId);

            challenge.status      = ChallengeStatus.Active;
            challenge.elapsedTime = 0f;
            challenge.currentScore = 0;
            foreach (RouteWaypoint wp in challenge.waypoints)
                wp.isReached = false;

            activeChallenge = challenge;
            OnChallengeStarted?.Invoke(challenge);
        }

        /// <summary>
        /// Called every frame via <see cref="Update"/> to advance elapsed time and check
        /// waypoint proximity.  Player position is read from the <c>SWEF.Flight.FlightController</c>
        /// singleton when available; otherwise falls back to the default origin.
        /// </summary>
        public void UpdateActiveChallenge()
        {
            if (activeChallenge == null || activeChallenge.status != ChallengeStatus.Active)
                return;

            activeChallenge.elapsedTime += Time.deltaTime;

            // Timeout
            if (activeChallenge.TimeRemaining() <= 0f)
            {
                FailChallenge(activeChallenge.challengeId);
                return;
            }

            // Read player position — falls back to default origin when FlightController unavailable
            GetPlayerPosition(out double lat, out double lon, out double alt);

            bool allRequired = true;
            foreach (RouteWaypoint wp in activeChallenge.waypoints)
            {
                if (wp.isReached) continue;
                if (wp.isOptional) continue;

                if (wp.IsReached(lat, lon, alt))
                {
                    wp.isReached = true;
                    OnWaypointReached?.Invoke(wp);
                }
                else
                {
                    allRequired = false;
                }
            }

            // Also check optional waypoints for bonus
            foreach (RouteWaypoint wp in activeChallenge.waypoints)
            {
                if (!wp.isOptional || wp.isReached) continue;
                if (wp.IsReached(lat, lon, alt))
                {
                    wp.isReached = true;
                    OnWaypointReached?.Invoke(wp);
                }
            }

            if (allRequired && activeChallenge.waypoints.Count > 0)
                CompleteChallenge(activeChallenge.challengeId);
        }

        /// <summary>
        /// Finalises a challenge as <see cref="ChallengeStatus.Completed"/>, calculates the
        /// final score, and fires <see cref="OnChallengeCompleted"/>.
        /// </summary>
        /// <param name="challengeId">The challenge to complete.</param>
        public void CompleteChallenge(string challengeId)
        {
            WeatherChallenge challenge = FindChallenge(challengeId);
            if (challenge == null) return;

            challenge.status       = ChallengeStatus.Completed;
            challenge.currentScore = CalculateScore(challenge);

            if (activeChallenge != null && activeChallenge.challengeId == challengeId)
                activeChallenge = null;

            OnChallengeCompleted?.Invoke(challenge);
            SaveChallenges();
        }

        /// <summary>
        /// Marks a challenge as <see cref="ChallengeStatus.Failed"/> (due to timeout or crash)
        /// and fires <see cref="OnChallengeFailed"/>.
        /// </summary>
        /// <param name="challengeId">The challenge to fail.</param>
        public void FailChallenge(string challengeId)
        {
            WeatherChallenge challenge = FindChallenge(challengeId);
            if (challenge == null) return;

            challenge.status       = ChallengeStatus.Failed;
            challenge.currentScore = CalculateScore(challenge);

            if (activeChallenge != null && activeChallenge.challengeId == challengeId)
                activeChallenge = null;

            OnChallengeFailed?.Invoke(challenge);
            SaveChallenges();
        }

        #endregion

        #region Queries

        /// <summary>Returns all non-expired challenges with <see cref="ChallengeStatus.Available"/> status.</summary>
        public List<WeatherChallenge> GetAvailableChallenges()
        {
            var result = new List<WeatherChallenge>();
            foreach (WeatherChallenge c in allChallenges)
            {
                if (c.status == ChallengeStatus.Available && !c.IsExpired())
                    result.Add(c);
            }
            return result;
        }

        /// <summary>Returns all challenges matching the specified weather type.</summary>
        /// <param name="type">The weather scenario to filter by.</param>
        public List<WeatherChallenge> GetChallengesByWeather(ChallengeWeatherType type)
        {
            var result = new List<WeatherChallenge>();
            foreach (WeatherChallenge c in allChallenges)
            {
                if (c.weatherType == type) result.Add(c);
            }
            return result;
        }

        #endregion

        #region Scoring

        /// <summary>
        /// Calculates the score for the given challenge based on elapsed time, waypoints
        /// reached, difficulty multiplier, and any completed bonus objective.
        /// </summary>
        /// <param name="challenge">The challenge to score.</param>
        /// <returns>Integer score value, clamped between 0 and <see cref="WeatherChallenge.maxScore"/>.</returns>
        public int CalculateScore(WeatherChallenge challenge)
        {
            if (challenge == null) return 0;

            float completion    = challenge.CompletionPercentage();
            float timeBonus     = challenge.TimeRemaining() / Mathf.Max(1f, challenge.timeLimit);
            float difficultyMul = GetDifficultyMultiplier(challenge.difficulty);
            int   baseScore     = Mathf.RoundToInt(challenge.maxScore * completion * (0.7f + 0.3f * timeBonus) * difficultyMul);

            if (challenge.bonusCompleted) baseScore += challenge.bonusScore;

            return Mathf.Clamp(baseScore, 0, challenge.maxScore + challenge.bonusScore);
        }

        #endregion

        #region Maintenance

        /// <summary>Removes all challenges whose status is <see cref="ChallengeStatus.Expired"/> or that have passed their expiry time.</summary>
        public void CleanupExpiredChallenges()
        {
            for (int i = allChallenges.Count - 1; i >= 0; i--)
            {
                WeatherChallenge c = allChallenges[i];
                if (c.IsExpired() && c.status == ChallengeStatus.Available)
                    c.status = ChallengeStatus.Expired;

                if (c.status == ChallengeStatus.Expired)
                    allChallenges.RemoveAt(i);
            }
        }

        #endregion

        #region Persistence

        /// <summary>Serialises <see cref="allChallenges"/> to JSON at <c>Application.persistentDataPath/weatherchallenges.json</c>.</summary>
        public void SaveChallenges()
        {
            try
            {
                var wrapper = new ChallengeListWrapper { challenges = allChallenges };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(Application.persistentDataPath + SaveFileName, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeatherChallengeManager] SaveChallenges failed: {ex.Message}");
            }
        }

        /// <summary>Deserialises challenges from JSON; initialises an empty list on first run or error.</summary>
        public void LoadChallenges()
        {
            string path = Application.persistentDataPath + SaveFileName;
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<ChallengeListWrapper>(json);
                    allChallenges = wrapper?.challenges ?? new List<WeatherChallenge>();
                }
                else
                {
                    allChallenges = new List<WeatherChallenge>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeatherChallengeManager] LoadChallenges failed: {ex.Message}");
                allChallenges = new List<WeatherChallenge>();
            }

            CleanupExpiredChallenges();
        }

        #endregion

        #region Private Helpers

        private WeatherChallenge FindChallenge(string id)
        {
            foreach (WeatherChallenge c in allChallenges)
                if (c.challengeId == id) return c;
            return null;
        }

        private void ApplyChallengeTemplate(WeatherChallenge c)
        {
            switch (c.weatherType)
            {
                case ChallengeWeatherType.Fog:
                    c.title                = "Zero Visibility Navigation";
                    c.description          = "Navigate the route using instruments only. Visibility is near zero.";
                    c.visibilityMultiplier = 0.15f;
                    c.windSpeedMultiplier  = 0.8f;
                    break;
                case ChallengeWeatherType.Thunderstorm:
                    c.title               = "Storm Chaser";
                    c.description         = "Fly through the storm cell and collect data waypoints before it dissipates.";
                    c.visibilityMultiplier = 0.4f;
                    c.windSpeedMultiplier  = 2.5f;
                    break;
                case ChallengeWeatherType.Thermal:
                    c.title               = "Thermal Rider";
                    c.description         = "Ride thermals between waypoints to gain altitude without engine power.";
                    c.visibilityMultiplier = 1f;
                    c.windSpeedMultiplier  = 1.2f;
                    break;
                case ChallengeWeatherType.Snow:
                    c.title               = "Arctic Approach";
                    c.description         = "Winter conditions — watch for icing and maintain safe airspeed.";
                    c.visibilityMultiplier = 0.5f;
                    c.windSpeedMultiplier  = 1.5f;
                    break;
                case ChallengeWeatherType.Crosswind:
                    c.title               = "Crosswind Master";
                    c.description         = "Persistent crosswind requires constant crab-angle correction.";
                    c.visibilityMultiplier = 0.9f;
                    c.windSpeedMultiplier  = 3f;
                    break;
                case ChallengeWeatherType.Turbulence:
                    c.title               = "Rough Air";
                    c.description         = "Severe turbulence throughout — keep altitude and heading steady.";
                    c.visibilityMultiplier = 0.8f;
                    c.windSpeedMultiplier  = 1.8f;
                    break;
                case ChallengeWeatherType.Icing:
                    c.title               = "Icing Corridor";
                    c.description         = "Structural icing risk above the freezing level — stay in the safe altitude band.";
                    c.visibilityMultiplier = 0.6f;
                    c.windSpeedMultiplier  = 1.3f;
                    break;
                case ChallengeWeatherType.Rain:
                    c.title               = "Monsoon Run";
                    c.description         = "Heavy rain reduces visibility. Complete the route before conditions worsen.";
                    c.visibilityMultiplier = 0.55f;
                    c.windSpeedMultiplier  = 1.4f;
                    break;
                default: // ClearSkies
                    c.title               = "Precision Flight";
                    c.description         = "Perfect conditions — show off your precision flying skills.";
                    c.visibilityMultiplier = 1f;
                    c.windSpeedMultiplier  = 1f;
                    break;
            }

            switch (c.difficulty)
            {
                case ChallengeDifficulty.Easy:    c.timeLimit = 600f; c.maxScore = 500;  c.bonusScore = 100; break;
                case ChallengeDifficulty.Medium:  c.timeLimit = 420f; c.maxScore = 1000; c.bonusScore = 200; break;
                case ChallengeDifficulty.Hard:    c.timeLimit = 300f; c.maxScore = 2000; c.bonusScore = 500; break;
                case ChallengeDifficulty.Extreme: c.timeLimit = 180f; c.maxScore = 5000; c.bonusScore = 1000; break;
            }
        }

        private static float GetDifficultyMultiplier(ChallengeDifficulty d)
        {
            switch (d)
            {
                case ChallengeDifficulty.Easy:    return 0.8f;
                case ChallengeDifficulty.Hard:    return 1.3f;
                case ChallengeDifficulty.Extreme: return 1.8f;
                default:                          return 1f;
            }
        }

        private static double GetExpiryHours(ChallengeDifficulty d)
        {
            switch (d)
            {
                case ChallengeDifficulty.Easy:    return 48;
                case ChallengeDifficulty.Hard:    return 12;
                case ChallengeDifficulty.Extreme: return 6;
                default:                          return 24;
            }
        }

        private void GetPlayerPosition(out double lat, out double lon, out double alt)
        {
            // Attempt to read from FlightController if available
#if SWEF_FLIGHT_AVAILABLE
            var fc = SWEF.Flight.FlightController.Instance;
            if (fc != null)
            {
                lat = fc.Latitude;
                lon = fc.Longitude;
                alt = fc.Altitude;
                return;
            }
#endif
            lat = defaultOriginLat;
            lon = defaultOriginLon;
            alt = defaultOriginAlt;
        }

        #endregion

        #region Inner Types

        [Serializable]
        private class ChallengeListWrapper
        {
            public List<WeatherChallenge> challenges = new List<WeatherChallenge>();
        }

        #endregion
    }
}
