// SurveyJournalBridge.cs — SWEF Terrain Scanning & Geological Survey System
using System.Collections.Generic;
using UnityEngine;

// Optional dependency guards
#if SWEF_JOURNAL_AVAILABLE
using SWEF.Journal;
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
using SWEF.Achievement;
#endif

namespace SWEF.TerrainSurvey
{
    /// <summary>
    /// Subscribes to <see cref="SurveyPOIManager.OnPOIDiscovered"/> and automatically
    /// creates journal entries via <c>JournalManager</c>.  Also tracks per-feature
    /// discovery counts and reports milestones to <c>AchievementManager</c>.
    /// Both integrations are null-safe and compile-guarded.
    /// </summary>
    public class SurveyJournalBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Achievement Milestones")]
        [Tooltip("Number of discoveries of the same feature type that triggers an achievement check.")]
        [SerializeField] private int milestoneEvery = 5;

        // ── Internal state ────────────────────────────────────────────────────────
        private readonly Dictionary<GeologicalFeatureType, int> _discoveryCounts =
            new Dictionary<GeologicalFeatureType, int>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered += OnPOIDiscovered;
        }

        private void OnDisable()
        {
            if (SurveyPOIManager.Instance != null)
                SurveyPOIManager.Instance.OnPOIDiscovered -= OnPOIDiscovered;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void OnPOIDiscovered(SurveyPOI poi)
        {
            if (poi == null) return;

            AddJournalEntry(poi);
            TrackAchievementMilestone(poi.featureType);
        }

        private void AddJournalEntry(SurveyPOI poi)
        {
#if SWEF_JOURNAL_AVAILABLE
            if (JournalManager.Instance == null) return;

            string featureName = GeologicalClassifier.GetFeatureDisplayName(poi.featureType);
            string note = $"[Survey] Discovered {featureName} at " +
                          $"({poi.position.x:F0}, {poi.position.y:F0}, {poi.position.z:F0}). " +
                          $"Altitude: {poi.position.y:F0}m. " +
                          $"Timestamp: {poi.discoveredTimestamp}";

            JournalManager.Instance.AddEntry(note);
#else
            Debug.Log($"[SurveyJournal] Discovery: {poi.featureType} @ {poi.position} " +
                      $"(JournalManager unavailable)");
#endif
        }

        private void TrackAchievementMilestone(GeologicalFeatureType type)
        {
            if (!_discoveryCounts.ContainsKey(type))
                _discoveryCounts[type] = 0;

            _discoveryCounts[type]++;
            int count = _discoveryCounts[type];

            if (count % milestoneEvery != 0) return;

#if SWEF_ACHIEVEMENT_AVAILABLE
            if (AchievementManager.Instance == null) return;

            string achievementId = $"survey_{type.ToString().ToLower()}_{count}";
            AchievementManager.Instance.UnlockAchievement(achievementId);
#else
            Debug.Log($"[SurveyJournal] Milestone: {count}x {type} discoveries " +
                      $"(AchievementManager unavailable)");
#endif
        }
    }
}
