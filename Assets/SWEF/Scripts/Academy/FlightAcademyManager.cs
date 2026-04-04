// FlightAcademyManager.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// Central singleton that owns the full lifecycle of the Flight Academy:
    /// configuration loading, session orchestration, progress persistence,
    /// and certificate issuance.
    /// </summary>
    public class FlightAcademyManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static FlightAcademyManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private AcademyConfig config;

        // ── Sub-systems ───────────────────────────────────────────────────────
        private AcademyProgressTracker _progressTracker;
        private TrainingSessionManager _sessionManager;
        private CertificationManager   _certificationManager;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Raised when the academy has finished initialising.</summary>
        public event Action OnAcademyReady;

        /// <summary>Raised when a lesson session concludes.</summary>
        public event Action<SessionResult> OnSessionCompleted;

        /// <summary>Raised when a new certificate is issued.</summary>
        public event Action<CertificateData> OnCertificateIssued;

        // ── Properties ─────────────────────────────────────────────────────────
        public AcademyConfig           Config               => config;
        public AcademyProgressTracker  ProgressTracker      => _progressTracker;
        public TrainingSessionManager  SessionManager       => _sessionManager;
        public CertificationManager    CertificationManager => _certificationManager;

        // ── Unity lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialise();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveProgress();
                Instance = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        // ── Initialisation ─────────────────────────────────────────────────────

        private void Initialise()
        {
            if (config == null)
            {
                Debug.LogWarning("[FlightAcademy] AcademyConfig not assigned — using defaults.");
                config = ScriptableObject.CreateInstance<AcademyConfig>();
            }

            var savedData = LoadProgress();
            _progressTracker      = new AcademyProgressTracker(savedData);
            _sessionManager       = new TrainingSessionManager();
            _certificationManager = new CertificationManager(_progressTracker);

            // Wire up session events
            _sessionManager.OnSessionCompleted  += HandleSessionCompleted;
            _certificationManager.OnCertificateIssued += cert => OnCertificateIssued?.Invoke(cert);

            // Unlock first lessons if curricula are configured
            UnlockInitialLessons();

            OnAcademyReady?.Invoke();
        }

        // ── Curriculum helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Returns all curricula the player is eligible to enroll in.
        /// </summary>
        public IEnumerable<FlightCurriculum> GetAvailableCurricula()
        {
            if (config?.curricula == null) yield break;
            foreach (var c in config.curricula)
            {
                if (_progressTracker.HighestTier >= c.prerequisiteTier)
                    yield return c;
            }
        }

        /// <summary>
        /// Enrolls the player in the curriculum and unlocks its first lesson.
        /// </summary>
        public void EnrollInCurriculum(string curriculumId)
        {
            _progressTracker.EnrollInCurriculum(curriculumId);
            var curriculum = FindCurriculum(curriculumId);
            if (curriculum?.lessons?.Count > 0)
                _progressTracker.UnlockLesson(curriculumId, curriculum.lessons[0].lessonId);
            SaveProgress();
        }

        // ── Session control ─────────────────────────────────────────────────────

        /// <summary>Starts a training session for the specified lesson.</summary>
        public void StartLesson(string curriculumId, string lessonId)
        {
            var curriculum = FindCurriculum(curriculumId);
            if (curriculum == null)
            {
                Debug.LogError($"[FlightAcademy] Curriculum not found: {curriculumId}");
                return;
            }
            var lesson = curriculum.GetLesson(lessonId);
            if (lesson == null)
            {
                Debug.LogError($"[FlightAcademy] Lesson not found: {lessonId} in {curriculumId}");
                return;
            }

            var status = _progressTracker.GetLessonStatus(curriculumId, lessonId);
            if (status == LessonStatus.Locked)
            {
                Debug.LogWarning($"[FlightAcademy] Lesson {lessonId} is locked.");
                return;
            }

            _progressTracker.StartLesson(curriculumId, lessonId);
            _sessionManager.StartSession(curriculumId, lesson);
        }

        /// <summary>
        /// Submits theory answers for the active session.
        /// </summary>
        public void SubmitTheoryAnswers(IList<int> answers)
        {
            _sessionManager.SubmitTheoryAnswers(answers);
        }

        /// <summary>
        /// Records objective points during the practical phase.
        /// </summary>
        public void RecordObjectivePoints(string objectiveTag, int points)
        {
            _sessionManager.RecordObjectivePoints(objectiveTag, points);
        }

        /// <summary>Signals that the practical portion has ended.</summary>
        public void FinishPractical() => _sessionManager.FinishPractical();

        /// <summary>Aborts the current session.</summary>
        public void AbortSession() => _sessionManager.AbortSession();

        // ── Exam handling ───────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates a completed exam and, if passing, issues a certificate.
        /// </summary>
        public ExamResult EvaluateExam(string examId, float theoryScore, float practicalScore)
        {
            var exam = FindExam(examId);
            if (exam == null)
            {
                Debug.LogError($"[FlightAcademy] Exam not found: {examId}");
                return null;
            }

            int attempts     = _progressTracker.GetExamAttemptCount(examId) + 1;
            var result       = exam.BuildResult(theoryScore, practicalScore, attempts);
            _progressTracker.RecordExamResult(result);

            if (result.passed)
            {
                string pilotName      = _progressTracker.Data.pilotName;
                string curriculumName = GetCurriculumNameForTier(exam.targetTier);
                _certificationManager.IssueCertificate(pilotName, exam.targetTier, result, curriculumName);
            }

            SaveProgress();
            return result;
        }

        // ── Persistence ─────────────────────────────────────────────────────────

        /// <summary>Manually saves progress to disk.</summary>
        public void SaveProgress()
        {
            try
            {
                string path = GetSavePath();
                string json = JsonUtility.ToJson(_progressTracker.Data, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FlightAcademy] Failed to save progress: {ex.Message}");
            }
        }

        private AcademyProgressData LoadProgress()
        {
            try
            {
                string path = GetSavePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonUtility.FromJson<AcademyProgressData>(json) ?? new AcademyProgressData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FlightAcademy] Could not load progress: {ex.Message}");
            }
            return new AcademyProgressData();
        }

        private string GetSavePath() =>
            Path.Combine(Application.persistentDataPath, config?.saveFileName ?? "academy_progress.json");

        // ── Event handlers ──────────────────────────────────────────────────────

        private void HandleSessionCompleted(SessionResult result)
        {
            if (result.lessonCompleted)
            {
                int xp = result.xpAwarded;
                if (config != null) xp = Mathf.RoundToInt(xp * config.xpMultiplier);

                _progressTracker.RecordTheoryScore(
                    result.curriculumId, result.lessonId, result.theoryScore);
                _progressTracker.RecordPracticalScore(
                    result.curriculumId, result.lessonId, result.practicalScore);
                _progressTracker.CompleteLesson(result.curriculumId, result.lessonId, xp);

                UnlockNextLesson(result.curriculumId, result.lessonId);
            }

            SaveProgress();
            OnSessionCompleted?.Invoke(result);
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        private void UnlockInitialLessons()
        {
            if (config?.curricula == null) return;
            foreach (var curriculum in config.curricula)
            {
                var cp = _progressTracker.GetCurriculumProgress(curriculum.curriculumId);
                if (cp == null || !cp.isEnrolled) continue;
                if (curriculum.lessons?.Count > 0)
                    _progressTracker.UnlockLesson(curriculum.curriculumId, curriculum.lessons[0].lessonId);
            }
        }

        private void UnlockNextLesson(string curriculumId, string completedLessonId)
        {
            var curriculum = FindCurriculum(curriculumId);
            if (curriculum?.lessons == null) return;

            bool found = false;
            foreach (var lesson in curriculum.lessons)
            {
                if (found)
                {
                    _progressTracker.UnlockLesson(curriculumId, lesson.lessonId);
                    break;
                }
                if (lesson.lessonId == completedLessonId)
                    found = true;
            }

            // Check if all lessons completed → mark curriculum done
            CheckCurriculumCompletion(curriculumId, curriculum);
        }

        private void CheckCurriculumCompletion(string curriculumId, FlightCurriculum curriculum)
        {
            if (curriculum?.lessons == null) return;
            foreach (var lesson in curriculum.lessons)
            {
                if (_progressTracker.GetLessonStatus(curriculumId, lesson.lessonId) != LessonStatus.Completed)
                    return;
            }
            _progressTracker.CompleteCurriculum(curriculumId);
        }

        private FlightCurriculum FindCurriculum(string curriculumId)
        {
            if (config?.curricula == null) return null;
            foreach (var c in config.curricula)
                if (c != null && c.curriculumId == curriculumId) return c;
            return null;
        }

        private FlightExam FindExam(string examId)
        {
            if (config?.exams == null) return null;
            foreach (var e in config.exams)
                if (e != null && e.examId == examId) return e;
            return null;
        }

        private string GetCurriculumNameForTier(LicenseTier tier)
        {
            if (config?.curricula == null) return tier.ToString();
            foreach (var c in config.curricula)
                if (c != null && c.targetTier == tier) return c.curriculumName;
            return tier.ToString();
        }
    }
}
