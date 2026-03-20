using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SWEF.Achievement;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Persists tour completion data including best times, waypoints visited,
    /// and star ratings.  Integrates with <see cref="AchievementManager"/> to grant
    /// achievements on tour milestones.
    /// </summary>
    public class TourProgressTracker : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Singleton instance.</summary>
        public static TourProgressTracker Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Star Rating Thresholds")]
        [Tooltip("Time multiplier relative to estimatedDurationMinutes for 3-star rating (e.g. 1.0 = on-time).")]
        [SerializeField] private float threeStarMultiplier = 1.0f;
        [Tooltip("Time multiplier for a 2-star rating.")]
        [SerializeField] private float twoStarMultiplier   = 1.5f;

        [Header("Achievement IDs")]
        [SerializeField] private string firstTourAchievementId = "tour_first_complete";
        [SerializeField] private string allToursAchievementId  = "tour_all_complete";
        [SerializeField] private string perfectTourAchievementId = "tour_three_stars";

        // ── Persistence ───────────────────────────────────────────────────────────
        private static readonly string SaveFileName = "tour_progress.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private Dictionary<string, TourResult> _results = new Dictionary<string, TourResult>();

        // ── Inner types ───────────────────────────────────────────────────────────
        /// <summary>Recorded outcome of a single tour completion attempt.</summary>
        [Serializable]
        public struct TourResult
        {
            /// <summary>Total time taken to complete the tour in seconds.</summary>
            public float  completionTime;
            /// <summary>Number of waypoints the player visited during this run.</summary>
            public int    waypointsVisited;
            /// <summary>Stars earned (1–3) for this result.</summary>
            public int    starsEarned;
            /// <summary>UTC ISO-8601 timestamp of completion.</summary>
            public string completedDate;
        }

        [Serializable]
        private class SaveData
        {
            public List<ResultEntry> entries = new List<ResultEntry>();
        }

        [Serializable]
        private class ResultEntry
        {
            public string     tourId;
            public TourResult result;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadData();

            // Hook into TourManager events.
            var tourManager = FindFirstObjectByType<TourManager>();
            if (tourManager != null)
            {
                float startTime = 0f;
                int   visitedCount = 0;

                tourManager.OnTourStarted += _ =>
                {
                    startTime    = Time.realtimeSinceStartup;
                    visitedCount = 0;
                };

                tourManager.OnWaypointReached += (idx, _) => visitedCount = idx + 1;

                tourManager.OnTourCompleted += tour =>
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    int   stars   = ComputeStars(elapsed, visitedCount,
                                                  tour.waypoints.Count,
                                                  tour.estimatedDurationMinutes * 60f);
                    var result = new TourResult
                    {
                        completionTime  = elapsed,
                        waypointsVisited = visitedCount,
                        starsEarned     = stars,
                        completedDate   = DateTime.UtcNow.ToString("o"),
                    };
                    SaveTourResult(tour.tourId, result);
                };
            }
        }

        private void OnApplicationPause(bool pause) { if (pause) SaveData(); }
        private void OnApplicationQuit()              { SaveData(); }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the best <see cref="TourResult"/> recorded for the given tour id.
        /// Returns a default (zeroed) struct if no result has been saved.
        /// </summary>
        /// <param name="tourId">The tour's unique identifier.</param>
        public TourResult GetTourProgress(string tourId)
        {
            return _results.TryGetValue(tourId, out var result) ? result : default;
        }

        /// <summary>
        /// Persists a tour result, keeping the best stars earned across all attempts.
        /// Triggers any relevant achievements.
        /// </summary>
        /// <param name="tourId">The tour's unique identifier.</param>
        /// <param name="result">The result data to store.</param>
        public void SaveTourResult(string tourId, TourResult result)
        {
            if (_results.TryGetValue(tourId, out var existing))
            {
                // Keep highest stars; on tie, keep shortest time.
                if (result.starsEarned > existing.starsEarned
                    || (result.starsEarned == existing.starsEarned
                        && result.completionTime < existing.completionTime))
                {
                    _results[tourId] = result;
                }
            }
            else
            {
                _results[tourId] = result;
            }

            SaveData();
            TriggerAchievements(tourId, result);
            Debug.Log($"[SWEF] TourProgressTracker: Saved result for '{tourId}' — {result.starsEarned}★.");
        }

        /// <summary>Returns the total number of tours with at least one completion.</summary>
        public int GetCompletedTourCount()
        {
            int count = 0;
            foreach (var kvp in _results)
                if (kvp.Value.starsEarned > 0) count++;
            return count;
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private int ComputeStars(float elapsedSec, int visited, int total, float targetSec)
        {
            if (visited < total) return 1; // Missed waypoints → 1 star max.
            if (elapsedSec <= targetSec * threeStarMultiplier) return 3;
            if (elapsedSec <= targetSec * twoStarMultiplier)   return 2;
            return 1;
        }

        private void TriggerAchievements(string tourId, TourResult result)
        {
            var am = AchievementManager.Instance;
            if (am == null) return;

            // First ever tour completion.
            if (GetCompletedTourCount() == 1)
                am.TryUnlock(firstTourAchievementId);

            // Perfect (3-star) tour.
            if (result.starsEarned >= 3)
                am.TryUnlock(perfectTourAchievementId);

            // Report incremental progress toward "complete all tours".
            am.ReportProgress(allToursAchievementId, 1f);
        }

        private void LoadData()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                string json = File.ReadAllText(SavePath);
                var data    = JsonUtility.FromJson<SaveData>(json);
                if (data?.entries == null) return;
                foreach (var entry in data.entries)
                    _results[entry.tourId] = entry.result;
                Debug.Log($"[SWEF] TourProgressTracker: Loaded {_results.Count} tour results.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] TourProgressTracker: Failed to load data — {ex.Message}");
            }
        }

        private void SaveData()
        {
            try
            {
                var data = new SaveData();
                foreach (var kvp in _results)
                    data.entries.Add(new ResultEntry { tourId = kvp.Key, result = kvp.Value });
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] TourProgressTracker: Failed to save data — {ex.Message}");
            }
        }
    }
}
