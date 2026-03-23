using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Automatic route recommendation engine.
    /// <para>
    /// Produces a scored list of <see cref="RouteRecommendation"/> objects by evaluating
    /// each saved route against a set of weighted factors:
    /// <list type="bullet">
    ///   <item><b>Proximity</b> — nearby routes score higher.</item>
    ///   <item><b>Difficulty</b> — matches the player's preferred difficulty.</item>
    ///   <item><b>Time of day</b> — scenic routes score higher at golden-hour/night.</item>
    ///   <item><b>Weather</b> — calm/storm routes matched to current conditions.</item>
    ///   <item><b>Unexplored</b> — routes through areas the player hasn't visited.</item>
    ///   <item><b>Popularity</b> — highly-rated routes receive a bonus.</item>
    /// </list>
    /// </para>
    /// Requires a <see cref="RoutePlannerManager"/> to be present in the scene.
    /// </summary>
    public class AutoRouteRecommender : MonoBehaviour
    {
        #region Constants

        private const float CacheRefreshInterval  = 60f;   // seconds between recommendation refreshes
        private const float ProximityMaxDistance  = 5000f; // metres — beyond this, proximity score is 0
        private const float GoldenHourStartH      = 5.5f;  // 05:30 local
        private const float GoldenHourEndH        = 8.0f;  // 08:00 local
        private const float GoldenHourStartHDusk  = 16.5f; // 16:30
        private const float GoldenHourEndHDusk    = 19.5f; // 19:30
        private const float NightStartH           = 20.5f; // 20:30
        private const float NightEndH             = 5.5f;  // 05:30

        #endregion

        #region Inspector

        [Header("Weights (0–1)")]
        [Range(0f, 1f)] [SerializeField] private float weightProximity   = 0.30f;
        [Range(0f, 1f)] [SerializeField] private float weightDifficulty  = 0.20f;
        [Range(0f, 1f)] [SerializeField] private float weightTimeOfDay   = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float weightWeather     = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float weightUnexplored  = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float weightPopularity  = 0.15f;

        [Header("Player Preferences")]
        [SerializeField] private RouteDifficulty preferredDifficulty = RouteDifficulty.Intermediate;

        [Header("Weather (set externally)")]
        [Tooltip("True when current weather is stormy (affects route scoring).")]
        [SerializeField] private bool isStormy;

        [Header("Player Transform")]
        [SerializeField] private Transform playerTransform;

        #endregion

        #region Events

        /// <summary>Fired each time the recommendation list is refreshed.</summary>
        public event Action<List<RouteRecommendation>> OnRecommendationsUpdated;

        #endregion

        #region Public Properties

        /// <summary>Most recently cached recommendations.</summary>
        public IReadOnlyList<RouteRecommendation> CachedRecommendations => _cache;

        #endregion

        #region Private State

        private List<RouteRecommendation> _cache = new List<RouteRecommendation>();
        private readonly HashSet<string>  _exploredRouteIds = new HashSet<string>();
        private Coroutine                 _refreshCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (playerTransform == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) playerTransform = go.transform;
            }
        }

        private void OnEnable()  => _refreshCoroutine = StartCoroutine(PeriodicRefresh());
        private void OnDisable() { if (_refreshCoroutine != null) StopCoroutine(_refreshCoroutine); }

        #endregion

        #region Public API

        /// <summary>Returns the top <paramref name="count"/> route recommendations.</summary>
        /// <param name="count">Maximum number of recommendations to return.</param>
        public List<RouteRecommendation> GetRecommendations(int count)
        {
            if (_cache.Count == 0) RefreshRecommendations();

            int take = Mathf.Min(count, _cache.Count);
            return _cache.GetRange(0, take);
        }

        /// <summary>Marks a route as explored (lowers its unexplored score).</summary>
        public void MarkExplored(string routeId)
        {
            if (!string.IsNullOrEmpty(routeId)) _exploredRouteIds.Add(routeId);
        }

        /// <summary>Sets the current weather state used for scoring.</summary>
        public void SetStormy(bool stormy) => isStormy = stormy;

        /// <summary>Sets the player's preferred difficulty.</summary>
        public void SetPreferredDifficulty(RouteDifficulty difficulty) => preferredDifficulty = difficulty;

        /// <summary>Forces an immediate cache refresh.</summary>
        public void ForceRefresh() => RefreshRecommendations();

        #endregion

        #region Scoring

        private void RefreshRecommendations()
        {
            var manager = RoutePlannerManager.Instance;
            if (manager == null) return;

            var allRoutes = manager.GetAllRoutes();
            if (allRoutes == null || allRoutes.Count == 0)
            {
                _cache.Clear();
                OnRecommendationsUpdated?.Invoke(_cache);
                return;
            }

            float hour = (float)DateTime.Now.TimeOfDay.TotalHours;
            var scored = new List<RouteRecommendation>(allRoutes.Count);

            foreach (var route in allRoutes)
            {
                if (route == null) continue;

                float score  = 0f;
                var   tags   = new List<string>();
                var   reason = new System.Text.StringBuilder();

                // Proximity
                float proximity = ScoreProximity(route);
                if (proximity > 0f)
                {
                    score += proximity * weightProximity;
                    tags.Add("nearby");
                    reason.Append("Route is nearby. ");
                }

                // Difficulty
                float diffScore = ScoreDifficulty(route);
                score += diffScore * weightDifficulty;
                if (diffScore > 0.8f) { tags.Add("good difficulty"); reason.Append("Matches your skill level. "); }

                // Time of day
                float todScore = ScoreTimeOfDay(route, hour);
                score += todScore * weightTimeOfDay;
                if (todScore > 0.7f) { tags.Add("great time of day"); reason.Append("Ideal conditions right now. "); }

                // Weather
                float weatherScore = ScoreWeather(route);
                score += weatherScore * weightWeather;
                if (weatherScore > 0.7f) { tags.Add("good weather match"); reason.Append("Suits current weather. "); }

                // Unexplored
                float unexplored = _exploredRouteIds.Contains(route.routeId) ? 0f : 1f;
                score += unexplored * weightUnexplored;
                if (unexplored > 0f) { tags.Add("unexplored"); reason.Append("You haven't flown this route yet. "); }

                // Popularity
                float pop = Mathf.Clamp01(route.rating / 5f);
                score += pop * weightPopularity;
                if (pop > 0.7f) { tags.Add("popular"); reason.Append("Highly rated by other pilots. "); }

                scored.Add(new RouteRecommendation
                {
                    route                 = route,
                    matchScore            = score,
                    recommendationReason  = reason.ToString().Trim(),
                    tags                  = tags
                });
            }

            // Sort descending
            scored.Sort((a, b) => b.matchScore.CompareTo(a.matchScore));
            _cache = scored;
            OnRecommendationsUpdated?.Invoke(_cache);
        }

        private float ScoreProximity(FlightRoute route)
        {
            if (playerTransform == null || route.waypoints == null || route.waypoints.Count == 0)
                return 0f;

            // Use the first waypoint's lat/lon projected loosely onto the world
            // (approximation — exact geo projection not available here)
            float dist = Vector3.Distance(playerTransform.position,
                new Vector3((float)route.startLongitude * 100f, route.startAltitude,
                            (float)route.startLatitude  * 100f));

            return 1f - Mathf.Clamp01(dist / ProximityMaxDistance);
        }

        private float ScoreDifficulty(FlightRoute route)
        {
            // Map preferred difficulty to expected route difficulty 1–5 range
            int preferred = (int)preferredDifficulty + 1; // 1-4
            int routeDiff = Mathf.Clamp(route.difficulty, 1, 5);
            float delta   = Mathf.Abs(preferred - routeDiff);
            return 1f - Mathf.Clamp01(delta / 4f);
        }

        private float ScoreTimeOfDay(FlightRoute route, float hour)
        {
            bool isScenicRoute = route.routeType == RouteType.Scenic ||
                                 route.routeType == RouteType.Photography;

            bool isGoldenHour = (hour >= GoldenHourStartH && hour <= GoldenHourEndH) ||
                                (hour >= GoldenHourStartHDusk && hour <= GoldenHourEndHDusk);
            bool isNight = hour >= NightStartH || hour <= NightEndH;

            if (isScenicRoute && isGoldenHour) return 1f;
            if (isScenicRoute && isNight)      return 0.7f;
            if (route.routeType == RouteType.Race && !isNight) return 0.9f;
            return 0.5f;
        }

        private float ScoreWeather(FlightRoute route)
        {
            bool isChallengeRoute = route.routeType == RouteType.Challenge;
            if (isStormy && isChallengeRoute) return 1f;
            if (!isStormy && !isChallengeRoute) return 0.8f;
            return 0.2f;
        }

        #endregion

        #region Periodic Refresh

        private IEnumerator PeriodicRefresh()
        {
            while (true)
            {
                RefreshRecommendations();
                yield return new WaitForSeconds(CacheRefreshInterval);
            }
        }

        #endregion
    }
}
