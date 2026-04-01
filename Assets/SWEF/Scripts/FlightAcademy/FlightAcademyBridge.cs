using UnityEngine;

// Optional integration compile guards
#if SWEF_PROGRESSION_AVAILABLE
using SWEF.Progression;
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
using SWEF.Achievement;
#endif
#if SWEF_JOURNAL_AVAILABLE
using SWEF.Journal;
#endif
#if SWEF_SOCIALHUB_AVAILABLE
using SWEF.SocialHub;
#endif
#if SWEF_FLIGHTSCHOOL_AVAILABLE
using SWEF.FlightSchool;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Integration bridge that wires <see cref="FlightAcademyManager"/> events
    /// to the broader SWEF system (Progression, Achievement, SkillTree, Journal,
    /// SocialActivityFeed, and FlightSchool).
    /// All external system calls are null-safe.
    /// </summary>
    public class FlightAcademyBridge : MonoBehaviour
    {
        // ── Unity ─────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            manager.OnExamCompleted  += HandleExamCompleted;
            manager.OnLicenseEarned  += HandleLicenseEarned;
            manager.OnCertificateIssued += HandleCertificateIssued;
        }

        private void OnDisable()
        {
            var manager = FlightAcademyManager.Instance;
            if (manager == null) return;

            manager.OnExamCompleted    -= HandleExamCompleted;
            manager.OnLicenseEarned    -= HandleLicenseEarned;
            manager.OnCertificateIssued -= HandleCertificateIssued;
        }

        // ── Handlers ─────────────────────────────────────────────────────────────
        private void HandleExamCompleted(TrainingModule module, ExamResult result)
        {
            if (module == null || result == null) return;

            // Award XP
#if SWEF_PROGRESSION_AVAILABLE
            if (result.passed && ProgressionManager.Instance != null)
                ProgressionManager.Instance.AddXP(module.rewardXP, $"exam_{module.moduleId}");
#endif

            // Report exam achievement progress
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.ReportProgress("academy_exams_completed", 1f);
                if (result.grade.StartsWith("A"))
                    AchievementManager.Instance.ReportProgress("academy_a_grade_exams", 1f);
            }
#endif
        }

        private void HandleLicenseEarned(LicenseGrade grade)
        {
            // Skill point reward
#if SWEF_PROGRESSION_AVAILABLE
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.AddSkillPoint();
#endif

            // Achievement
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (AchievementManager.Instance != null)
            {
                string achievementId = $"academy_license_{grade.ToString().ToLower()}";
                AchievementManager.Instance.ReportProgress(achievementId, 1f);

                if (grade == LicenseGrade.ATPL)
                    AchievementManager.Instance.ReportProgress("academy_atpl_earned", 1f);
            }
#endif

            // Journal entry
#if SWEF_JOURNAL_AVAILABLE
            if (JournalManager.Instance != null)
            {
                string title   = $"License Earned: {grade}";
                string content = $"Congratulations! You have earned your {grade} pilot license.";
                JournalManager.Instance.AddEntry(title, content);
            }
#endif

            // Social feed
#if SWEF_SOCIALHUB_AVAILABLE
            var feed = FindObjectOfType<SocialActivityFeed>();
            feed?.PostActivity(ActivityType.RankUp, $"Earned {grade} pilot license");
#endif

            // FlightSchool data sharing
#if SWEF_FLIGHTSCHOOL_AVAILABLE
            var schoolManager = FindObjectOfType<FlightSchoolManager>();
            schoolManager?.NotifyLicenseEarned(grade.ToString());
#endif
        }

        private void HandleCertificateIssued(Certificate certificate)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.ReportProgress("academy_certificates_earned", 1f);
#endif
        }
    }
}
