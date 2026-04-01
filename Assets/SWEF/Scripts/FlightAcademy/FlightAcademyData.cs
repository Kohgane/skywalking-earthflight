using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAcademy
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Pilot license grades awarded upon completing all modules in a tier.</summary>
    public enum LicenseGrade
    {
        StudentPilot,
        PPL,
        CPL,
        ATPL,
        InstructorRating,
        TestPilot
    }

    /// <summary>Category of skill being tested in an exam.</summary>
    public enum ExamType
    {
        Landing,
        TakeOff,
        InstrumentFlight,
        FormationFlying,
        EmergencyProcedure,
        Navigation,
        WeatherFlight,
        NightFlight,
        Aerobatics,
        CargoOperations
    }

    /// <summary>Difficulty tier of an exam, which also determines the passing threshold.</summary>
    public enum ExamDifficulty
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    // ── ScriptableObject ─────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single training module (theory + practical exam).
    /// Stored under Resources/Academy/ so the manager can load them at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrainingModule", menuName = "SWEF/FlightAcademy/Training Module")]
    public class TrainingModule : ScriptableObject
    {
        [Tooltip("Unique identifier used for persistence.")]
        public string moduleId;

        [Tooltip("License tier this module belongs to.")]
        public LicenseGrade licenseGrade;

        [Tooltip("Localization key for the module title.")]
        public string titleLocKey;

        [Tooltip("Localization key for the module description.")]
        public string descriptionLocKey;

        [Tooltip("Module IDs that must be completed before this one is available.")]
        public List<string> prerequisiteModuleIds = new List<string>();

        [Tooltip("Skill category being examined.")]
        public ExamType examType;

        [Tooltip("Difficulty tier and passing threshold.")]
        public ExamDifficulty examDifficulty;

        /// <summary>Minimum score (0–100) required to pass. Derived from difficulty if left at 0.</summary>
        [Range(0, 100)]
        public float passingScore;

        [Tooltip("Time limit in seconds (0 = unlimited).")]
        public float timeLimit;

        [Tooltip("Ordered list of objectives the player must satisfy.")]
        public List<ExamObjective> objectives = new List<ExamObjective>();

        [Tooltip("XP rewarded on first pass.")]
        public int rewardXP = 100;

        [Tooltip("Skill points rewarded on first pass.")]
        public int rewardSkillPoints = 1;
    }

    // ── Plain Data Classes ────────────────────────────────────────────────────────

    /// <summary>Describes a single measurable objective within an exam.</summary>
    [Serializable]
    public class ExamObjective
    {
        [Tooltip("Localization key for a human-readable description.")]
        public string descriptionLocKey;

        [Tooltip("Machine-readable objective type used by ExamController.")]
        public string objectiveType;

        [Tooltip("Target value the player must reach or stay within.")]
        public float targetValue;

        [Tooltip("Fractional weight of this objective in the final score (all weights should sum to 1).")]
        [Range(0f, 1f)]
        public float weight = 0.1f;

        [Tooltip("Bonus objectives do not count toward the pass threshold.")]
        public bool isBonus;
    }

    /// <summary>
    /// Per-objective result captured after an exam attempt.
    /// </summary>
    [Serializable]
    public class ObjectiveScore
    {
        public string objectiveType;
        /// <summary>0–100 score for this objective.</summary>
        public float score;
        public bool completed;
    }

    /// <summary>
    /// Final result of a single exam attempt.
    /// </summary>
    [Serializable]
    public class ExamResult
    {
        /// <summary>Composite weighted score 0–100.</summary>
        public float score;

        /// <summary>Letter grade: A+, A, A-, B+, B, B-, C+, C, C-, D, F.</summary>
        public string grade;

        /// <summary>Whether the score met the passing threshold for this exam's difficulty.</summary>
        public bool passed;

        /// <summary>Per-objective scores used to compute the composite.</summary>
        public List<ObjectiveScore> objectiveScores = new List<ObjectiveScore>();

        /// <summary>Total time taken in seconds.</summary>
        public float totalTime;

        /// <summary>Total penalty deductions applied.</summary>
        public float penaltyPoints;

        /// <summary>Total bonus additions applied.</summary>
        public float bonusPoints;

        /// <summary>UTC timestamp of the attempt.</summary>
        public string timestamp;
    }

    /// <summary>
    /// Signed certificate issued when a player earns a license grade.
    /// </summary>
    [Serializable]
    public class Certificate
    {
        public string certificateId;
        public LicenseGrade licenseGrade;
        public string playerName;
        public string issueDate;

        /// <summary>Module ID → best exam score achieved for that module.</summary>
        public Dictionary<string, float> examScores = new Dictionary<string, float>();

        public float totalFlightHours;

        /// <summary>SHA-256 hash of the canonical certificate fields for verification.</summary>
        public string signatureHash;
    }

    /// <summary>
    /// Persisted academy progress for the local player.
    /// Serialized to academy_progress.json.
    /// </summary>
    [Serializable]
    public class AcademyProgress
    {
        public LicenseGrade currentLicense = LicenseGrade.StudentPilot;
        public List<string> completedModules = new List<string>();

        /// <summary>Module ID → best ExamResult.</summary>
        public Dictionary<string, ExamResult> examResults = new Dictionary<string, ExamResult>();

        public float totalTrainingHours;
        public List<Certificate> certificates = new List<Certificate>();
    }

    // ── Serialization helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Wrapper that lets Unity's JsonUtility serialize <see cref="AcademyProgress"/>
    /// (JsonUtility does not support Dictionary natively; converters handle this).
    /// </summary>
    [Serializable]
    internal class ExamResultEntry
    {
        public string moduleId;
        public ExamResult result;
    }

    [Serializable]
    internal class CertificateExamScoreEntry
    {
        public string moduleId;
        public float score;
    }

    [Serializable]
    internal class AcademyProgressSaveData
    {
        public LicenseGrade currentLicense;
        public List<string> completedModules = new List<string>();
        public List<ExamResultEntry> examResults = new List<ExamResultEntry>();
        public float totalTrainingHours;
        public List<CertificateSaveData> certificates = new List<CertificateSaveData>();
    }

    [Serializable]
    internal class CertificateSaveData
    {
        public string certificateId;
        public LicenseGrade licenseGrade;
        public string playerName;
        public string issueDate;
        public List<CertificateExamScoreEntry> examScores = new List<CertificateExamScoreEntry>();
        public float totalFlightHours;
        public string signatureHash;
    }
}
