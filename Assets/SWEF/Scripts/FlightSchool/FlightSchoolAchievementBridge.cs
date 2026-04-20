using System;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Unlocks SWEF.Achievement entries when Flight School milestones are
    /// reached (Phase 84):
    ///   first_lesson_completed, first_certification_earned,
    ///   flightschool_perfect_score, flightschool_all_categories.
    /// Uses reflection to call <c>SWEF.Achievement.AchievementManager.TryUnlock</c>
    /// so this bridge compiles cleanly even without the Achievement assembly.
    /// </summary>
    public class FlightSchoolAchievementBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;
        [SerializeField] private FlightGradingSystem gradingSystem;

        [Header("Achievement IDs")]
        [SerializeField] private string firstLessonId          = "first_lesson_completed";
        [SerializeField] private string firstCertificationId   = "first_certification_earned";
        [SerializeField] private string perfectScoreId         = "flightschool_perfect_score";
        [SerializeField] private string allCategoriesId        = "flightschool_all_categories";

        [Tooltip("Score (0–100) treated as a 'perfect' run for the perfect-score achievement.")]
        [Range(90f, 100f)] [SerializeField] private float perfectScoreThreshold = 100f;

        // ── Internal state ───────────────────────────────────────────────────────

        private static Type _achievementType;
        private static bool _resolveAttempted;
        private bool _anyLessonCompleted;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
            if (gradingSystem == null) gradingSystem = FlightGradingSystem.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonCompleted    += HandleLessonCompleted;
                schoolManager.OnCertificationEarned += HandleCertificationEarned;
            }
            if (gradingSystem != null)
                gradingSystem.OnGradeCalculated += HandleGradeCalculated;
        }

        private void OnDisable()
        {
            if (schoolManager != null)
            {
                schoolManager.OnLessonCompleted    -= HandleLessonCompleted;
                schoolManager.OnCertificationEarned -= HandleCertificationEarned;
            }
            if (gradingSystem != null)
                gradingSystem.OnGradeCalculated -= HandleGradeCalculated;
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            if (lesson == null) return;

            if (!_anyLessonCompleted)
            {
                _anyLessonCompleted = true;
                TryUnlockAchievement(firstLessonId);
            }

            if (schoolManager != null && AllCategoriesCompleted())
                TryUnlockAchievement(allCategoriesId);
        }

        private void HandleCertificationEarned(PilotCertification cert)
        {
            if (cert == null) return;
            TryUnlockAchievement(firstCertificationId);
        }

        private void HandleGradeCalculated(LessonGradeReport report)
        {
            if (report == null) return;
            if (report.finalScore >= perfectScoreThreshold)
                TryUnlockAchievement(perfectScoreId);
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private bool AllCategoriesCompleted()
        {
            if (schoolManager == null || schoolManager.allLessons == null) return false;

            foreach (LessonCategory cat in Enum.GetValues(typeof(LessonCategory)))
            {
                bool anyCompleted = false;
                foreach (var l in schoolManager.allLessons)
                {
                    if (l.category != cat) continue;
                    if (l.status == LessonStatus.Completed || l.status == LessonStatus.Mastered)
                    {
                        anyCompleted = true;
                        break;
                    }
                }
                if (!anyCompleted) return false;
            }
            return true;
        }

        private static Type ResolveAchievementType()
        {
            if (_resolveAttempted) return _achievementType;
            _resolveAttempted = true;
            _achievementType  = Type.GetType("SWEF.Achievement.AchievementManager, Assembly-CSharp");
            return _achievementType;
        }

        private static void TryUnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            var t = ResolveAchievementType();
            if (t == null) return;

            var instanceProp = t.GetProperty("Instance");
            var instance     = instanceProp?.GetValue(null) as MonoBehaviour;
            if (instance == null) return;

            var method = t.GetMethod("TryUnlock", new[] { typeof(string) });
            method?.Invoke(instance, new object[] { achievementId });
        }
    }
}
