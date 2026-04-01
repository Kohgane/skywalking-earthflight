using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Static helper that generates the default content for the Flight Training Academy:
    /// 6 license grades, 30 training modules (5 per grade), exam objective templates,
    /// default passing scores, and recommended flight hours per license.
    /// </summary>
    public static class FlightAcademyDefaultData
    {
        // ── Recommended flight hours per license ──────────────────────────────────
        private static readonly Dictionary<LicenseGrade, float> RecommendedHours
            = new Dictionary<LicenseGrade, float>
        {
            { LicenseGrade.StudentPilot,    10f },
            { LicenseGrade.PPL,             40f },
            { LicenseGrade.CPL,            150f },
            { LicenseGrade.ATPL,           500f },
            { LicenseGrade.InstructorRating, 800f },
            { LicenseGrade.TestPilot,     1500f }
        };

        /// <summary>Returns the recommended total flight hours for a license grade.</summary>
        public static float GetRecommendedHours(LicenseGrade grade)
            => RecommendedHours.TryGetValue(grade, out float h) ? h : 0f;

        // ── Default passing scores ─────────────────────────────────────────────────
        /// <summary>Returns the default passing score for an exam of the given difficulty.</summary>
        public static float GetDefaultPassingScore(ExamDifficulty difficulty)
            => ExamScoringEngine.GetPassingThreshold(difficulty);

        // ── Default modules ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates and returns an array of 30 default <see cref="TrainingModule"/> instances
        /// (5 per license grade) with pre-configured objectives.
        /// These are used as a fallback when no Resources/Academy/ assets are present.
        /// </summary>
        public static TrainingModule[] CreateDefaultModules()
        {
            var modules = new List<TrainingModule>();

            // ── Student Pilot (5 modules) ─────────────────────────────────────────
            modules.Add(MakeModule("sp_basic_flight",   LicenseGrade.StudentPilot, "Basic Flight Control",
                ExamType.Landing,           ExamDifficulty.Bronze, new[] { "sp_basic_flight" }));

            modules.Add(MakeModule("sp_takeoff",        LicenseGrade.StudentPilot, "Takeoff Procedures",
                ExamType.TakeOff,           ExamDifficulty.Bronze, new[] { "sp_basic_flight" },
                prereqs: new[] { "sp_basic_flight" }));

            modules.Add(MakeModule("sp_navigation",     LicenseGrade.StudentPilot, "Visual Navigation",
                ExamType.Navigation,        ExamDifficulty.Bronze, new[] { "sp_basic_flight" },
                prereqs: new[] { "sp_basic_flight" }));

            modules.Add(MakeModule("sp_weather",        LicenseGrade.StudentPilot, "Weather Awareness",
                ExamType.WeatherFlight,     ExamDifficulty.Bronze, new[] { "sp_basic_flight" },
                prereqs: new[] { "sp_basic_flight" }));

            modules.Add(MakeModule("sp_emergency",      LicenseGrade.StudentPilot, "Basic Emergency",
                ExamType.EmergencyProcedure, ExamDifficulty.Bronze, new[] { "sp_basic_flight" },
                prereqs: new[] { "sp_basic_flight" }));

            // ── PPL (5 modules) ───────────────────────────────────────────────────
            modules.Add(MakeModule("ppl_precision_landing", LicenseGrade.PPL, "Precision Landing",
                ExamType.Landing,           ExamDifficulty.Silver, new[] { "ppl_precision_landing" }));

            modules.Add(MakeModule("ppl_crosscountry",  LicenseGrade.PPL, "Cross-Country Navigation",
                ExamType.Navigation,        ExamDifficulty.Silver, new[] { "ppl_precision_landing" },
                prereqs: new[] { "ppl_precision_landing" }));

            modules.Add(MakeModule("ppl_night_flight",  LicenseGrade.PPL, "Night Flight Operations",
                ExamType.NightFlight,       ExamDifficulty.Silver, new[] { "ppl_precision_landing" },
                prereqs: new[] { "ppl_precision_landing" }));

            modules.Add(MakeModule("ppl_ifr_basics",    LicenseGrade.PPL, "Instrument Basics",
                ExamType.InstrumentFlight,  ExamDifficulty.Bronze, new[] { "ppl_precision_landing" },
                prereqs: new[] { "ppl_precision_landing" }));

            modules.Add(MakeModule("ppl_emergency_adv", LicenseGrade.PPL, "Advanced Emergency",
                ExamType.EmergencyProcedure, ExamDifficulty.Silver, new[] { "ppl_precision_landing" },
                prereqs: new[] { "ppl_precision_landing" }));

            // ── CPL (5 modules) ───────────────────────────────────────────────────
            modules.Add(MakeModule("cpl_ifr",           LicenseGrade.CPL, "Instrument Flight Rules",
                ExamType.InstrumentFlight,  ExamDifficulty.Silver, new[] { "cpl_ifr" }));

            modules.Add(MakeModule("cpl_formation_basic", LicenseGrade.CPL, "Formation Flying Basics",
                ExamType.FormationFlying,   ExamDifficulty.Bronze, new[] { "cpl_ifr" },
                prereqs: new[] { "cpl_ifr" }));

            modules.Add(MakeModule("cpl_cargo",         LicenseGrade.CPL, "Cargo Operations",
                ExamType.CargoOperations,   ExamDifficulty.Bronze, new[] { "cpl_ifr" },
                prereqs: new[] { "cpl_ifr" }));

            modules.Add(MakeModule("cpl_weather_adv",   LicenseGrade.CPL, "Advanced Weather",
                ExamType.WeatherFlight,     ExamDifficulty.Silver, new[] { "cpl_ifr" },
                prereqs: new[] { "cpl_ifr" }));

            modules.Add(MakeModule("cpl_multi_landing", LicenseGrade.CPL, "Multi-Approach Landing",
                ExamType.Landing,           ExamDifficulty.Gold, new[] { "cpl_ifr" },
                prereqs: new[] { "cpl_ifr" }));

            // ── ATPL (5 modules) ──────────────────────────────────────────────────
            modules.Add(MakeModule("atpl_adv_ifr",      LicenseGrade.ATPL, "Advanced IFR",
                ExamType.InstrumentFlight,  ExamDifficulty.Gold, new[] { "atpl_adv_ifr" }));

            modules.Add(MakeModule("atpl_formation_cmd",LicenseGrade.ATPL, "Formation Command",
                ExamType.FormationFlying,   ExamDifficulty.Silver, new[] { "atpl_adv_ifr" },
                prereqs: new[] { "atpl_adv_ifr" }));

            modules.Add(MakeModule("atpl_heavy_cargo",  LicenseGrade.ATPL, "Heavy Cargo Operations",
                ExamType.CargoOperations,   ExamDifficulty.Silver, new[] { "atpl_adv_ifr" },
                prereqs: new[] { "atpl_adv_ifr" }));

            modules.Add(MakeModule("atpl_storm",        LicenseGrade.ATPL, "Storm Penetration",
                ExamType.WeatherFlight,     ExamDifficulty.Gold, new[] { "atpl_adv_ifr" },
                prereqs: new[] { "atpl_adv_ifr" }));

            modules.Add(MakeModule("atpl_emergency_master", LicenseGrade.ATPL, "Emergency Mastery",
                ExamType.EmergencyProcedure, ExamDifficulty.Gold, new[] { "atpl_adv_ifr" },
                prereqs: new[] { "atpl_adv_ifr" }));

            // ── Instructor Rating (5 modules) ─────────────────────────────────────
            modules.Add(MakeModule("ir_teaching",       LicenseGrade.InstructorRating, "Teaching Methodology",
                ExamType.Navigation,        ExamDifficulty.Gold, new[] { "ir_teaching" }));

            modules.Add(MakeModule("ir_aerobatics",     LicenseGrade.InstructorRating, "Advanced Aerobatics",
                ExamType.Aerobatics,        ExamDifficulty.Gold, new[] { "ir_teaching" },
                prereqs: new[] { "ir_teaching" }));

            modules.Add(MakeModule("ir_formation_prec", LicenseGrade.InstructorRating, "Precision Formation",
                ExamType.FormationFlying,   ExamDifficulty.Gold, new[] { "ir_teaching" },
                prereqs: new[] { "ir_teaching" }));

            modules.Add(MakeModule("ir_allweather",     LicenseGrade.InstructorRating, "All-Weather Operations",
                ExamType.WeatherFlight,     ExamDifficulty.Platinum, new[] { "ir_teaching" },
                prereqs: new[] { "ir_teaching" }));

            modules.Add(MakeModule("ir_emergency_instr",LicenseGrade.InstructorRating, "Emergency Instructor",
                ExamType.EmergencyProcedure, ExamDifficulty.Platinum, new[] { "ir_teaching" },
                prereqs: new[] { "ir_teaching" }));

            // ── Test Pilot (5 modules) ────────────────────────────────────────────
            modules.Add(MakeModule("tp_extreme_alt",    LicenseGrade.TestPilot, "Extreme Altitude",
                ExamType.Navigation,        ExamDifficulty.Platinum, new[] { "tp_extreme_alt" }));

            modules.Add(MakeModule("tp_speed_records",  LicenseGrade.TestPilot, "Speed Records",
                ExamType.TakeOff,           ExamDifficulty.Platinum, new[] { "tp_extreme_alt" },
                prereqs: new[] { "tp_extreme_alt" }));

            modules.Add(MakeModule("tp_experimental",   LicenseGrade.TestPilot, "Experimental Flight",
                ExamType.Aerobatics,        ExamDifficulty.Platinum, new[] { "tp_extreme_alt" },
                prereqs: new[] { "tp_extreme_alt" }));

            modules.Add(MakeModule("tp_full_ifr",       LicenseGrade.TestPilot, "Full Instrument Mastery",
                ExamType.InstrumentFlight,  ExamDifficulty.Platinum, new[] { "tp_extreme_alt" },
                prereqs: new[] { "tp_extreme_alt" }));

            modules.Add(MakeModule("tp_ultimate_landing",LicenseGrade.TestPilot, "Ultimate Landing Challenge",
                ExamType.Landing,           ExamDifficulty.Platinum, new[] { "tp_extreme_alt" },
                prereqs: new[] { "tp_extreme_alt" }));

            return modules.ToArray();
        }

        // ── Validation helpers ────────────────────────────────────────────────────

        /// <summary>Returns the total count of default modules (should be 30).</summary>
        public static int GetDefaultModuleCount() => 30;

        /// <summary>Returns the total count of license grades (should be 6).</summary>
        public static int GetLicenseGradeCount()
            => System.Enum.GetValues(typeof(LicenseGrade)).Length;

        // ── Factory ───────────────────────────────────────────────────────────────
        private static TrainingModule MakeModule(string id, LicenseGrade grade, string title,
                                                  ExamType examType, ExamDifficulty difficulty,
                                                  string[] objectiveKeys,
                                                  string[] prereqs = null)
        {
            var m = ScriptableObject.CreateInstance<TrainingModule>();
            m.moduleId            = id;
            m.licenseGrade        = grade;
            m.titleLocKey         = title;
            m.descriptionLocKey   = $"{id}_desc";
            m.examType            = examType;
            m.examDifficulty      = difficulty;
            m.passingScore        = GetDefaultPassingScore(difficulty);
            m.timeLimit           = 300f; // 5 minutes default
            m.rewardXP            = 100 + (int)grade * 50;
            m.rewardSkillPoints   = 1;

            m.prerequisiteModuleIds = new List<string>();
            if (prereqs != null)
                m.prerequisiteModuleIds.AddRange(prereqs);

            m.objectives = new List<ExamObjective>();
            if (objectiveKeys != null)
            {
                float w = objectiveKeys.Length > 0 ? 1f / objectiveKeys.Length : 1f;
                foreach (var key in objectiveKeys)
                {
                    m.objectives.Add(new ExamObjective
                    {
                        descriptionLocKey = $"{key}_obj",
                        objectiveType     = key,
                        targetValue       = 1f,
                        weight            = w,
                        isBonus           = false
                    });
                }
            }
            return m;
        }
    }
}
