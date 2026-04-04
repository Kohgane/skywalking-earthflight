// AcademyConfig.cs — SWEF Flight Academy & Certification System (Phase 104)
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// ScriptableObject that holds all global configuration values for the Flight Academy
    /// system.  Create via <em>Assets → Create → SWEF/Academy/Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Academy/Config", fileName = "AcademyConfig")]
    public class AcademyConfig : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Display name of the academy shown in UI.")]
        public string academyName = "SkyWalking Flight Academy";

        [Tooltip("Version tag used to invalidate cached progress on curriculum changes.")]
        public string dataVersion = "1.0.0";

        [Header("Curricula")]
        [Tooltip("All curricula available in this build.  Order determines display order.")]
        public FlightCurriculum[] curricula;

        [Header("Examinations")]
        [Tooltip("All formal exams available.  Each exam should match a curriculum's targetTier.")]
        public FlightExam[] exams;

        [Header("XP & Rewards")]
        [Tooltip("Flat XP bonus awarded when the player earns a certificate.")]
        [Min(0)]
        public int certificateXpBonus = 500;

        [Tooltip("XP multiplier applied to all Academy lesson rewards.")]
        [Min(0.1f)]
        public float xpMultiplier = 1f;

        [Header("Session Settings")]
        [Tooltip("Maximum time (seconds) a training session can remain idle before auto-save.")]
        [Min(10f)]
        public float sessionIdleTimeoutSeconds = 300f;

        [Tooltip("Whether to auto-resume the most recent in-progress session on startup.")]
        public bool autoResumeSession = true;

        [Header("Persistence")]
        [Tooltip("Filename (no path) used to persist academy progress in Application.persistentDataPath.")]
        public string saveFileName = "academy_progress.json";
    }
}
