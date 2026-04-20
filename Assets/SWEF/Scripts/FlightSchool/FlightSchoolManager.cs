using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Core singleton manager for the Training &amp; Flight School system.
    /// Maintains the lesson curriculum, tracks player progress, awards XP and
    /// certifications, and persists all data to
    /// <c>Application.persistentDataPath/flightschool.json</c>.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class FlightSchoolManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static FlightSchoolManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when a lesson transitions to <see cref="LessonStatus.InProgress"/>.</summary>
        public event Action<FlightLesson> OnLessonStarted;

        /// <summary>Raised when a lesson is marked <see cref="LessonStatus.Completed"/>.</summary>
        public event Action<FlightLesson> OnLessonCompleted;

        /// <summary>Raised when a <see cref="PilotCertification"/> is newly earned.</summary>
        public event Action<PilotCertification> OnCertificationEarned;

        /// <summary>Raised when the player earns XP, carrying the amount awarded.</summary>
        public event Action<int> OnXpEarned;

        /// <summary>Raised when a <see cref="SkillNode"/> becomes unlocked (Phase 84).</summary>
        public event Action<SkillNode> OnSkillNodeUnlocked;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>All lessons in the curriculum.</summary>
        public List<FlightLesson> allLessons = new List<FlightLesson>();

        /// <summary>All pilot certifications (earned and unearned).</summary>
        public List<PilotCertification> certifications = new List<PilotCertification>();

        /// <summary>Cumulative XP the player has earned through flight school.</summary>
        public int totalXpEarned;

        /// <summary>Persisted skill-tree graph (Phase 84). Populated by <see cref="SkillTreeController"/>.</summary>
        public SkillTreeData skillTree = new SkillTreeData();

        /// <summary>
        /// Practical-test definitions for certifications (Phase 84).
        /// Registered by <see cref="CertificationExamController"/> at runtime.
        /// </summary>
        public List<CertificationExam> examDefinitions = new List<CertificationExam>();

        // ── Persistence ──────────────────────────────────────────────────────────

        private static readonly string SaveFileName = "flightschool.json";
        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [Serializable]
        private class SaveData
        {
            public List<FlightLesson>       lessons        = new List<FlightLesson>();
            public List<PilotCertification> certifications = new List<PilotCertification>();
            public int                      totalXpEarned;
            public SkillTreeData            skillTree      = new SkillTreeData();
            public List<CertificationExam>  examDefinitions = new List<CertificationExam>();
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions the lesson identified by <paramref name="lessonId"/> to
        /// <see cref="LessonStatus.InProgress"/>.
        /// Prerequisites must be satisfied; otherwise the call is silently ignored.
        /// </summary>
        /// <param name="lessonId">ID of the lesson to start.</param>
        public void StartLesson(string lessonId)
        {
            var lesson = FindLesson(lessonId);
            if (lesson == null) return;

            var completed = GetCompletedLessonIds();
            if (!lesson.ArePrerequisitesMet(completed))
            {
                Debug.LogWarning($"[FlightSchool] Prerequisites not met for lesson '{lessonId}'.");
                return;
            }

            lesson.status = LessonStatus.InProgress;
            OnLessonStarted?.Invoke(lesson);
        }

        /// <summary>
        /// Updates <paramref name="objectiveId"/> inside <paramref name="lessonId"/> with
        /// the latest measured <paramref name="value"/>.
        /// Marks the objective complete when <c>value &gt;= targetValue</c>.
        /// </summary>
        public void CompleteObjective(string lessonId, string objectiveId, float value)
        {
            var lesson = FindLesson(lessonId);
            if (lesson == null) return;

            foreach (var obj in lesson.objectives)
            {
                if (obj.objectiveId != objectiveId) continue;
                obj.currentValue = value;
                if (value >= obj.targetValue)
                    obj.isCompleted = true;
                break;
            }
        }

        /// <summary>
        /// Marks a lesson completed with a given <paramref name="score"/> (0–100),
        /// awards XP on first completion, and checks all certifications.
        /// </summary>
        /// <param name="lessonId">ID of the lesson that was finished.</param>
        /// <param name="score">Normalised score in the range 0–100.</param>
        public void CompleteLesson(string lessonId, float score)
        {
            var lesson = FindLesson(lessonId);
            if (lesson == null) return;

            bool isFirstCompletion = lesson.completionCount == 0;

            lesson.completionCount++;
            lesson.status = score >= 90f ? LessonStatus.Mastered : LessonStatus.Completed;

            if (score > lesson.bestScore)
                lesson.bestScore = score;

            if (isFirstCompletion)
            {
                totalXpEarned += lesson.xpReward;
                OnXpEarned?.Invoke(lesson.xpReward);
            }

            OnLessonCompleted?.Invoke(lesson);
            CheckCertifications();

            // Unlock lessons whose prerequisites are now met
            var completed = GetCompletedLessonIds();
            foreach (var l in allLessons)
            {
                if (l.status == LessonStatus.Locked && l.ArePrerequisitesMet(completed))
                    l.status = LessonStatus.Available;
            }

            SaveProgress();
        }

        /// <summary>
        /// Returns all lessons whose prerequisites are satisfied and that have not
        /// been locked (i.e. status is <see cref="LessonStatus.Available"/>,
        /// <see cref="LessonStatus.InProgress"/>, <see cref="LessonStatus.Completed"/>,
        /// or <see cref="LessonStatus.Mastered"/>).
        /// </summary>
        public List<FlightLesson> GetAvailableLessons()
        {
            var completed = GetCompletedLessonIds();
            var result = new List<FlightLesson>();
            foreach (var l in allLessons)
            {
                if (l.status != LessonStatus.Locked && l.ArePrerequisitesMet(completed))
                    result.Add(l);
            }
            return result;
        }

        /// <summary>Returns all lessons belonging to <paramref name="category"/>.</summary>
        public List<FlightLesson> GetLessonsByCategory(LessonCategory category)
        {
            var result = new List<FlightLesson>();
            foreach (var l in allLessons)
                if (l.category == category) result.Add(l);
            return result;
        }

        /// <summary>Returns all lessons at the specified <paramref name="difficulty"/>.</summary>
        public List<FlightLesson> GetLessonsByDifficulty(LessonDifficulty difficulty)
        {
            var result = new List<FlightLesson>();
            foreach (var l in allLessons)
                if (l.difficulty == difficulty) result.Add(l);
            return result;
        }

        /// <summary>
        /// Returns the registered <see cref="CertificationExam"/> for
        /// <paramref name="certType"/>, or <c>null</c> when none is defined.
        /// </summary>
        public CertificationExam GetExamForCertification(CertificationType certType)
        {
            if (examDefinitions == null) return null;
            foreach (var e in examDefinitions)
                if (e != null && e.certType == certType) return e;
            return null;
        }

        /// <summary>
        /// Notifies listeners that <paramref name="node"/> was unlocked.
        /// Intended to be called by <see cref="SkillTreeController"/>.
        /// </summary>
        public void NotifySkillNodeUnlocked(SkillNode node)
        {
            if (node == null) return;
            OnSkillNodeUnlocked?.Invoke(node);
        }

        /// <summary>
        /// Evaluates every certification against the set of completed lessons and
        /// raises <see cref="OnCertificationEarned"/> for any newly satisfied ones.
        /// </summary>
        public void CheckCertifications()
        {
            var completed = GetCompletedLessonIds();
            foreach (var cert in certifications)
            {
                if (cert.isEarned) continue;
                if (cert.Progress(completed) >= 1f)
                {
                    cert.isEarned  = true;
                    cert.earnedDate = DateTime.UtcNow.ToString("o");
                    OnCertificationEarned?.Invoke(cert);
                }
            }
        }

        /// <summary>
        /// Resets a lesson back to <see cref="LessonStatus.Available"/> so the player
        /// can replay it. Objective progress is cleared but best score is preserved.
        /// </summary>
        /// <param name="lessonId">ID of the lesson to reset.</param>
        public void ResetLesson(string lessonId)
        {
            var lesson = FindLesson(lessonId);
            if (lesson == null) return;

            lesson.status = LessonStatus.Available;
            foreach (var obj in lesson.objectives)
            {
                obj.currentValue = 0f;
                obj.isCompleted  = false;
            }
        }

        /// <summary>Persists current lesson and certification state to disk.</summary>
        public void SaveProgress()
        {
            try
            {
                var data = new SaveData
                {
                    lessons         = allLessons,
                    certifications  = certifications,
                    totalXpEarned   = totalXpEarned,
                    skillTree       = skillTree,
                    examDefinitions = examDefinitions
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlightSchool] SaveProgress failed — {ex.Message}");
            }
        }

        /// <summary>
        /// Loads persisted progress from disk.
        /// If no save file exists, <see cref="InitializeDefaultLessons"/> is called.
        /// </summary>
        public void LoadProgress()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (data != null)
                    {
                        allLessons      = data.lessons         ?? new List<FlightLesson>();
                        certifications  = data.certifications  ?? new List<PilotCertification>();
                        totalXpEarned   = data.totalXpEarned;
                        skillTree       = data.skillTree       ?? new SkillTreeData();
                        examDefinitions = data.examDefinitions ?? new List<CertificationExam>();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlightSchool] LoadProgress failed — {ex.Message}");
            }

            InitializeDefaultLessons();
        }

        // ── Default curriculum ───────────────────────────────────────────────────

        /// <summary>
        /// Populates <see cref="allLessons"/> and <see cref="certifications"/> with a
        /// starter curriculum covering every <see cref="LessonCategory"/>.
        /// Called automatically when no save data is found.
        /// </summary>
        public void InitializeDefaultLessons()
        {
            allLessons     = BuildDefaultLessons();
            certifications = BuildDefaultCertifications();
            SaveProgress();
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private FlightLesson FindLesson(string lessonId)
        {
            foreach (var l in allLessons)
                if (l.lessonId == lessonId) return l;
            Debug.LogWarning($"[FlightSchool] Lesson '{lessonId}' not found.");
            return null;
        }

        private List<string> GetCompletedLessonIds()
        {
            var ids = new List<string>();
            foreach (var l in allLessons)
                if (l.status == LessonStatus.Completed || l.status == LessonStatus.Mastered)
                    ids.Add(l.lessonId);
            return ids;
        }

        // ── Default data builders ────────────────────────────────────────────────

        private static List<FlightLesson> BuildDefaultLessons()
        {
            return new List<FlightLesson>
            {
                // ── Basic Controls ───────────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "basic_takeoff",
                    title            = "First Takeoff",
                    description      = "Learn to lift off the runway safely.",
                    category         = LessonCategory.BasicControls,
                    difficulty       = LessonDifficulty.Beginner,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 5,
                    xpReward         = 100,
                    briefingText     = "Apply full throttle, maintain runway heading, and rotate at 60 kts.",
                    debriefingText   = "Well done on your first takeoff!",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "reach_50m",  description = "Climb to 50 m AGL",    targetValue = 50f  },
                        new LessonObjective { objectiveId = "speed_60",   description = "Reach 60 kts airspeed", targetValue = 60f  }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "basic_level_flight",
                    title            = "Straight & Level Flight",
                    description      = "Maintain altitude and heading without assistance.",
                    category         = LessonCategory.BasicControls,
                    difficulty       = LessonDifficulty.Beginner,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "basic_takeoff" },
                    estimatedMinutes = 8,
                    xpReward         = 120,
                    briefingText     = "Hold altitude ±50 m and heading ±10° for 60 seconds.",
                    debriefingText   = "Excellent control!",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "hold_altitude", description = "Maintain altitude for 60 s", targetValue = 60f },
                        new LessonObjective { objectiveId = "hold_heading",  description = "Keep heading within ±10°",   targetValue = 60f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "basic_landing",
                    title            = "First Landing",
                    description      = "Align with the runway and touch down smoothly.",
                    category         = LessonCategory.BasicControls,
                    difficulty       = LessonDifficulty.Beginner,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "basic_level_flight" },
                    estimatedMinutes = 10,
                    xpReward         = 150,
                    briefingText     = "Reduce to approach speed, maintain glide slope, and grease the touchdown.",
                    debriefingText   = "Smooth landing!",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "on_glideslope", description = "Intercept the glideslope",      targetValue = 1f  },
                        new LessonObjective { objectiveId = "soft_touchdown", description = "Touch down at < 200 fpm",       targetValue = 1f  }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "basic_turns",
                    title            = "Coordinated Turns",
                    description      = "Execute banked turns without slipping or skidding.",
                    category         = LessonCategory.BasicControls,
                    difficulty       = LessonDifficulty.Beginner,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "basic_level_flight" },
                    estimatedMinutes = 8,
                    xpReward         = 100,
                    briefingText     = "Complete 30° and 45° banked turns while keeping the ball centred.",
                    debriefingText   = "Great coordination!",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "turn_30",  description = "Complete a 30° bank turn",  targetValue = 1f },
                        new LessonObjective { objectiveId = "turn_45",  description = "Complete a 45° bank turn",  targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "basic_stall_recovery",
                    title            = "Stall Recognition & Recovery",
                    description      = "Recognise the onset of a stall and recover promptly.",
                    category         = LessonCategory.BasicControls,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "basic_turns" },
                    estimatedMinutes = 12,
                    xpReward         = 180,
                    briefingText     = "Identify stall buffet and recover with forward pressure and full power.",
                    debriefingText   = "You handled the stall confidently.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "stall_identify",  description = "Identify stall onset",       targetValue = 1f },
                        new LessonObjective { objectiveId = "stall_recover",   description = "Recover within 200 m",       targetValue = 1f }
                    }
                },

                // ── Navigation ───────────────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "nav_dead_reckoning",
                    title            = "Dead Reckoning",
                    description      = "Navigate to a waypoint using heading, speed and time.",
                    category         = LessonCategory.Navigation,
                    difficulty       = LessonDifficulty.Beginner,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 10,
                    xpReward         = 130,
                    briefingText     = "Plot a course, maintain heading, and arrive within 1 km of target.",
                    debriefingText   = "Solid positional awareness.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "nav_waypoint",   description = "Reach the waypoint",         targetValue = 1f },
                        new LessonObjective { objectiveId = "nav_accuracy",   description = "Arrive within 1 km",         targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "nav_vor_tracking",
                    title            = "VOR Navigation",
                    description      = "Track a VOR radial from station to waypoint.",
                    category         = LessonCategory.Navigation,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "nav_dead_reckoning" },
                    estimatedMinutes = 15,
                    xpReward         = 160,
                    briefingText     = "Tune the VOR, identify the radial, and fly inbound.",
                    debriefingText   = "Precise navigation!",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "tune_vor",        description = "Tune and identify VOR",      targetValue = 1f },
                        new LessonObjective { objectiveId = "track_radial",    description = "Track radial within ±5°",    targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "nav_instrument_approach",
                    title            = "Instrument Approach",
                    description      = "Fly an ILS approach to minimums.",
                    category         = LessonCategory.Navigation,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "nav_vor_tracking" },
                    estimatedMinutes = 20,
                    xpReward         = 250,
                    briefingText     = "Intercept the localiser and glideslope, land or go-around at DH.",
                    debriefingText   = "Instrument-rated proficiency demonstrated.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "intercept_loc",   description = "Intercept the localiser",    targetValue = 1f },
                        new LessonObjective { objectiveId = "follow_gs",       description = "Follow glideslope to DH",    targetValue = 1f },
                        new LessonObjective { objectiveId = "land_or_ga",      description = "Execute landing/go-around",  targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "nav_cross_country",
                    title            = "Cross-Country Flight",
                    description      = "Plan and fly a multi-leg cross-country route.",
                    category         = LessonCategory.Navigation,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "nav_dead_reckoning" },
                    estimatedMinutes = 30,
                    xpReward         = 200,
                    briefingText     = "File a flight plan, navigate three checkpoints, and land at destination.",
                    debriefingText   = "Cross-country experience logged.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "checkpoint_1",    description = "Reach checkpoint 1",         targetValue = 1f },
                        new LessonObjective { objectiveId = "checkpoint_2",    description = "Reach checkpoint 2",         targetValue = 1f },
                        new LessonObjective { objectiveId = "destination",     description = "Land at destination",        targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "nav_night_flight",
                    title            = "Night Navigation",
                    description      = "Navigate and land under night VFR conditions.",
                    category         = LessonCategory.Navigation,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "nav_cross_country" },
                    estimatedMinutes = 25,
                    xpReward         = 220,
                    briefingText     = "Use airport lighting systems and night VFR techniques.",
                    debriefingText   = "Night currency maintained.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "depart_night",    description = "Depart at night",            targetValue = 1f },
                        new LessonObjective { objectiveId = "night_land",      description = "Land at lit runway",         targetValue = 1f }
                    }
                },

                // ── Weather Flying ────────────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "wx_turbulence",
                    title            = "Flying in Turbulence",
                    description      = "Maintain controlled flight through moderate turbulence.",
                    category         = LessonCategory.WeatherFlying,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 12,
                    xpReward         = 170,
                    briefingText     = "Reduce to turbulence-penetration speed and maintain attitude.",
                    debriefingText   = "Smooth handling in rough air.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "enter_turb",      description = "Enter turbulence zone",      targetValue = 1f },
                        new LessonObjective { objectiveId = "survive_60s",     description = "Maintain flight for 60 s",   targetValue = 60f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "wx_crosswind_landing",
                    title            = "Crosswind Landing",
                    description      = "Land safely in a 15-knot crosswind.",
                    category         = LessonCategory.WeatherFlying,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "basic_landing", "wx_turbulence" },
                    estimatedMinutes = 15,
                    xpReward         = 200,
                    briefingText     = "Use crab or sideslip technique to correct for crosswind.",
                    debriefingText   = "Crosswind technique confirmed.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "crab_approach",   description = "Fly crab approach",          targetValue = 1f },
                        new LessonObjective { objectiveId = "xwind_land",      description = "Touch down on centreline",   targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "wx_mountain_wave",
                    title            = "Mountain Wave Soaring",
                    description      = "Exploit mountain wave lift safely.",
                    category         = LessonCategory.WeatherFlying,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "wx_turbulence" },
                    estimatedMinutes = 20,
                    xpReward         = 230,
                    briefingText     = "Locate the rotor zone, enter the wave, and climb to 8,000 m.",
                    debriefingText   = "Mountain wave experience gained.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "find_wave",       description = "Enter mountain wave lift",   targetValue = 1f },
                        new LessonObjective { objectiveId = "climb_8000",      description = "Climb to 8,000 m",           targetValue = 8000f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "wx_icing",
                    title            = "Icing Avoidance",
                    description      = "Recognise and escape from structural icing conditions.",
                    category         = LessonCategory.WeatherFlying,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "nav_instrument_approach" },
                    estimatedMinutes = 18,
                    xpReward         = 240,
                    briefingText     = "Identify icing conditions and divert to ice-free altitude.",
                    debriefingText   = "Icing awareness demonstrated.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "detect_ice",      description = "Detect icing accumulation",  targetValue = 1f },
                        new LessonObjective { objectiveId = "exit_ice",        description = "Exit icing layer",           targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "wx_storm_deviation",
                    title            = "Storm Cell Deviation",
                    description      = "Navigate safely around embedded thunderstorms.",
                    category         = LessonCategory.WeatherFlying,
                    difficulty       = LessonDifficulty.Expert,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "wx_icing", "wx_mountain_wave" },
                    estimatedMinutes = 25,
                    xpReward         = 300,
                    briefingText     = "Use onboard radar to deviate around cells by at least 20 NM.",
                    debriefingText   = "Storm avoidance completed safely.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "radar_on",        description = "Activate weather radar",     targetValue = 1f },
                        new LessonObjective { objectiveId = "deviate_20nm",    description = "Deviate 20 NM from cell",    targetValue = 1f }
                    }
                },

                // ── Aerobatics ───────────────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "aero_loop",
                    title            = "Inside Loop",
                    description      = "Execute a clean inside loop at entry speed.",
                    category         = LessonCategory.Aerobatics,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 10,
                    xpReward         = 150,
                    briefingText     = "Dive to entry speed, pull to 3–4 G, maintain back pressure over the top.",
                    debriefingText   = "Clean loop recorded.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "entry_speed",     description = "Reach loop entry speed",     targetValue = 1f },
                        new LessonObjective { objectiveId = "complete_loop",   description = "Complete a full loop",       targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "aero_roll",
                    title            = "Aileron Roll",
                    description      = "Perform a precise aileron roll maintaining altitude.",
                    category         = LessonCategory.Aerobatics,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 8,
                    xpReward         = 140,
                    briefingText     = "Apply full aileron and coordinate rudder to minimise nose wander.",
                    debriefingText   = "Roll completed within altitude tolerances.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "full_roll",       description = "Complete 360° aileron roll",  targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "aero_cuban8",
                    title            = "Cuban Eight",
                    description      = "Link two half-loops with half-rolls.",
                    category         = LessonCategory.Aerobatics,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "aero_loop", "aero_roll" },
                    estimatedMinutes = 12,
                    xpReward         = 200,
                    briefingText     = "Perform a half-loop, roll upright at the top, repeat for the figure-8.",
                    debriefingText   = "Cuban Eight completed.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "first_halfloop",  description = "Complete first half-loop",   targetValue = 1f },
                        new LessonObjective { objectiveId = "second_halfloop", description = "Complete second half-loop",  targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "aero_snap_roll",
                    title            = "Snap Roll",
                    description      = "Perform an accelerated stall-spin roll.",
                    category         = LessonCategory.Aerobatics,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "aero_roll", "basic_stall_recovery" },
                    estimatedMinutes = 12,
                    xpReward         = 210,
                    briefingText     = "Apply abrupt back pressure and full rudder to induce snap roll.",
                    debriefingText   = "Snap roll executed cleanly.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "snap_roll",       description = "Execute snap roll",          targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "aero_sequence",
                    title            = "Aerobatic Sequence",
                    description      = "Fly a full IAC-style aerobatic sequence.",
                    category         = LessonCategory.Aerobatics,
                    difficulty       = LessonDifficulty.Expert,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "aero_cuban8", "aero_snap_roll" },
                    estimatedMinutes = 20,
                    xpReward         = 350,
                    briefingText     = "String together 8 figures within the aerobatic box.",
                    debriefingText   = "Full aerobatic sequence flown.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "four_figures",    description = "Complete 4 figures",         targetValue = 4f  },
                        new LessonObjective { objectiveId = "eight_figures",   description = "Complete all 8 figures",     targetValue = 8f  }
                    }
                },

                // ── Emergency Procedures ─────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "emg_engine_failure",
                    title            = "Engine Failure at Altitude",
                    description      = "Execute a forced landing after engine failure.",
                    category         = LessonCategory.EmergencyProcedures,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 15,
                    xpReward         = 200,
                    briefingText     = "Restart checklist, select field, glide to a safe landing.",
                    debriefingText   = "Emergency landing handled calmly.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "restart_check",   description = "Run restart checklist",      targetValue = 1f },
                        new LessonObjective { objectiveId = "field_landing",   description = "Land in selected field",     targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "emg_fire",
                    title            = "Engine Fire",
                    description      = "Respond to an in-flight engine fire.",
                    category         = LessonCategory.EmergencyProcedures,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "emg_engine_failure" },
                    estimatedMinutes = 12,
                    xpReward         = 240,
                    briefingText     = "Fuel off, mixture off, extinguisher, dive to extinguish flame.",
                    debriefingText   = "Fire checklist completed correctly.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "fire_checklist",  description = "Complete fire checklist",    targetValue = 1f },
                        new LessonObjective { objectiveId = "extinguish",      description = "Extinguish the fire",        targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "emg_hydraulic",
                    title            = "Hydraulic Failure",
                    description      = "Land without hydraulic brakes or flaps.",
                    category         = LessonCategory.EmergencyProcedures,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "emg_engine_failure" },
                    estimatedMinutes = 15,
                    xpReward         = 220,
                    briefingText     = "Extend gear via alternate, plan longer landing roll.",
                    debriefingText   = "Hydraulic-off landing completed safely.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "alt_gear",        description = "Extend alternate gear",      targetValue = 1f },
                        new LessonObjective { objectiveId = "no_flap_land",    description = "Complete no-flap landing",   targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "emg_loss_comm",
                    title            = "Loss of Communications",
                    description      = "Navigate and land under radio failure (NORDO) rules.",
                    category         = LessonCategory.EmergencyProcedures,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 18,
                    xpReward         = 180,
                    briefingText     = "Squawk 7600, follow NORDO arrival procedure, land on light signals.",
                    debriefingText   = "NORDO procedure executed correctly.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "squawk_7600",     description = "Set transponder 7600",       targetValue = 1f },
                        new LessonObjective { objectiveId = "light_signals",   description = "Respond to light signals",   targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "emg_bird_strike",
                    title            = "Bird Strike Response",
                    description      = "Assess and handle damage after a bird strike.",
                    category         = LessonCategory.EmergencyProcedures,
                    difficulty       = LessonDifficulty.Expert,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "emg_fire", "emg_hydraulic" },
                    estimatedMinutes = 20,
                    xpReward         = 280,
                    briefingText     = "Check systems post-strike, declare emergency, divert to nearest airport.",
                    debriefingText   = "Bird strike scenario handled professionally.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "assess_damage",   description = "Complete systems check",     targetValue = 1f },
                        new LessonObjective { objectiveId = "declare_emg",     description = "Declare emergency",          targetValue = 1f },
                        new LessonObjective { objectiveId = "divert_land",     description = "Land at divert airport",     targetValue = 1f }
                    }
                },

                // ── Formation ─────────────────────────────────────────────────────
                new FlightLesson
                {
                    lessonId         = "form_trail",
                    title            = "Trail Formation",
                    description      = "Fly 50 m directly behind the lead aircraft.",
                    category         = LessonCategory.Formation,
                    difficulty       = LessonDifficulty.Intermediate,
                    status           = LessonStatus.Available,
                    estimatedMinutes = 10,
                    xpReward         = 160,
                    briefingText     = "Match lead's speed and altitude, maintain 50 m separation.",
                    debriefingText   = "Formation maintained successfully.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "join_trail",      description = "Join trail formation",       targetValue = 1f },
                        new LessonObjective { objectiveId = "hold_trail_60s",  description = "Hold position for 60 s",     targetValue = 60f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "form_echelon",
                    title            = "Echelon Formation",
                    description      = "Fly wing-tip to wing-tip in echelon right.",
                    category         = LessonCategory.Formation,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "form_trail" },
                    estimatedMinutes = 12,
                    xpReward         = 190,
                    briefingText     = "Position 3 m from lead's wingtip, match all control inputs.",
                    debriefingText   = "Echelon position held precisely.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "join_echelon",    description = "Join echelon position",      targetValue = 1f },
                        new LessonObjective { objectiveId = "hold_echelon_90s","description" = "Hold for 90 s",           targetValue = 90f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "form_crossover",
                    title            = "Formation Crossover",
                    description      = "Swap sides with the lead during a turn.",
                    category         = LessonCategory.Formation,
                    difficulty       = LessonDifficulty.Advanced,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "form_echelon" },
                    estimatedMinutes = 14,
                    xpReward         = 210,
                    briefingText     = "On lead's signal, cross behind and join opposite echelon.",
                    debriefingText   = "Crossover transition completed cleanly.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "start_crossover", description = "Initiate crossover",         targetValue = 1f },
                        new LessonObjective { objectiveId = "end_crossover",   description = "Establish on new side",      targetValue = 1f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "form_diamond",
                    title            = "Diamond Four-Ship",
                    description      = "Fly #4 position in a diamond formation.",
                    category         = LessonCategory.Formation,
                    difficulty       = LessonDifficulty.Expert,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "form_crossover" },
                    estimatedMinutes = 20,
                    xpReward         = 300,
                    briefingText     = "Slot into the #4 trail position, maintain through turns and vertical manoeuvres.",
                    debriefingText   = "Diamond four-ship completed.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "join_diamond",    description = "Join diamond slot",          targetValue = 1f },
                        new LessonObjective { objectiveId = "diamond_3min",    description = "Hold diamond for 3 min",     targetValue = 180f }
                    }
                },
                new FlightLesson
                {
                    lessonId         = "form_break",
                    title            = "Formation Break & Rejoin",
                    description      = "Execute a combat break and rejoin the formation.",
                    category         = LessonCategory.Formation,
                    difficulty       = LessonDifficulty.Expert,
                    status           = LessonStatus.Locked,
                    prerequisites    = new List<string> { "form_diamond" },
                    estimatedMinutes = 18,
                    xpReward         = 320,
                    briefingText     = "Break on cue with 3 G turn, extend, and rejoin within 2 minutes.",
                    debriefingText   = "Break and rejoin executed to standard.",
                    objectives       = new List<LessonObjective>
                    {
                        new LessonObjective { objectiveId = "break_turn",      description = "Execute 3 G break turn",     targetValue = 1f },
                        new LessonObjective { objectiveId = "rejoin_2min",     description = "Rejoin within 2 minutes",    targetValue = 1f }
                    }
                }
            };
        }

        private static List<PilotCertification> BuildDefaultCertifications()
        {
            return new List<PilotCertification>
            {
                new PilotCertification
                {
                    certType       = CertificationType.StudentPilot,
                    displayName    = "Student Pilot Certificate",
                    requiredLessons = new List<string>
                        { "basic_takeoff", "basic_level_flight", "basic_landing", "basic_turns" }
                },
                new PilotCertification
                {
                    certType       = CertificationType.PrivatePilot,
                    displayName    = "Private Pilot Certificate",
                    requiredLessons = new List<string>
                    {
                        "basic_takeoff", "basic_level_flight", "basic_landing", "basic_turns",
                        "basic_stall_recovery", "nav_dead_reckoning", "nav_cross_country",
                        "emg_engine_failure", "emg_loss_comm"
                    }
                },
                new PilotCertification
                {
                    certType       = CertificationType.CommercialPilot,
                    displayName    = "Commercial Pilot Certificate",
                    requiredLessons = new List<string>
                    {
                        "nav_vor_tracking", "nav_instrument_approach", "nav_night_flight",
                        "wx_turbulence", "wx_crosswind_landing", "wx_icing",
                        "emg_fire", "emg_hydraulic"
                    }
                },
                new PilotCertification
                {
                    certType       = CertificationType.AcrobaticPilot,
                    displayName    = "Acrobatic Pilot Certificate",
                    requiredLessons = new List<string>
                        { "aero_loop", "aero_roll", "aero_cuban8", "aero_snap_roll", "aero_sequence" }
                },
                new PilotCertification
                {
                    certType       = CertificationType.MasterAviator,
                    displayName    = "Master Aviator Certificate",
                    requiredLessons = new List<string>
                    {
                        "aero_sequence", "wx_storm_deviation", "emg_bird_strike",
                        "form_break", "nav_instrument_approach"
                    }
                }
            };
        }
    }
}
