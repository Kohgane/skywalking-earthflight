using System.Collections.Generic;
using UnityEngine;
using SWEF.Analytics;

namespace SWEF.Narration
{
    /// <summary>
    /// Tracks narration engagement analytics using <see cref="UserBehaviorTracker"/>.
    /// Records: narrations started, completed, skipped, average completion rate,
    /// most popular categories, and landmark-level interaction counts.
    /// </summary>
    public class NarrationAnalytics : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static NarrationAnalytics Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────────
        private int _narrationStartCount;
        private int _narrationCompleteCount;
        private int _narrationSkipCount;
        private readonly Dictionary<string, int> _landmarkPlayCounts  = new Dictionary<string, int>();
        private readonly Dictionary<LandmarkCategory, int> _catCounts = new Dictionary<LandmarkCategory, int>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNarrationStarted  += OnNarrationStarted;
            mgr.OnNarrationFinished += OnNarrationFinished;
        }

        private void OnDisable()
        {
            var mgr = NarrationManager.Instance;
            if (mgr == null) return;

            mgr.OnNarrationStarted  -= OnNarrationStarted;
            mgr.OnNarrationFinished -= OnNarrationFinished;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnNarrationStarted(NarrationQueueEntry entry)
        {
            _narrationStartCount++;

            string id = entry.landmark.landmarkId;
            _landmarkPlayCounts.TryGetValue(id, out int cnt);
            _landmarkPlayCounts[id] = cnt + 1;

            var cat = entry.landmark.category;
            _catCounts.TryGetValue(cat, out int catCnt);
            _catCounts[cat] = catCnt + 1;

            Track("narration_started", id);
        }

        private void OnNarrationFinished(NarrationQueueEntry entry, NarrationState state)
        {
            if (state == NarrationState.Completed)
            {
                _narrationCompleteCount++;
                Track("narration_completed", entry.landmark.landmarkId);
            }
            else if (state == NarrationState.Skipped)
            {
                _narrationSkipCount++;
                Track("narration_skipped", entry.landmark.landmarkId);
            }
        }

        // ── Reporting ─────────────────────────────────────────────────────────────

        /// <summary>Completion rate 0–1 (completed / started).</summary>
        public float CompletionRate =>
            _narrationStartCount > 0
                ? (float)_narrationCompleteCount / _narrationStartCount
                : 0f;

        /// <summary>Returns the most-played landmark ID, or empty string if none.</summary>
        public string MostPlayedLandmark()
        {
            string best = string.Empty;
            int max = 0;
            foreach (var kv in _landmarkPlayCounts)
                if (kv.Value > max) { max = kv.Value; best = kv.Key; }
            return best;
        }

        /// <summary>Returns the most-played category, or default if none.</summary>
        public LandmarkCategory MostPlayedCategory()
        {
            LandmarkCategory best = LandmarkCategory.Natural;
            int max = 0;
            foreach (var kv in _catCounts)
                if (kv.Value > max) { max = kv.Value; best = kv.Key; }
            return best;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void Track(string feature, string landmarkId)
        {
            var tracker = UserBehaviorTracker.Instance;
            if (tracker == null) return;
            tracker.TrackFeatureDiscovery(feature + "_" + landmarkId);
        }
    }
}
