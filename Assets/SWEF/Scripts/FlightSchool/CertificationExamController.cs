using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightSchool
{
    /// <summary>
    /// Coordinates the practical-test flow for a <see cref="CertificationExam"/>
    /// (Phase 84). An exam is a sequence of <see cref="FlightLesson"/>s that must
    /// be passed in order, each scoring at least <see cref="CertificationExam.minimumPassScore"/>.
    /// Failing any lesson ends the session and increments the attempt counter.
    /// </summary>
    public class CertificationExamController : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired when an exam attempt begins.</summary>
        public event Action<CertificationExam> OnExamStarted;

        /// <summary>Fired when the player passes every lesson in the exam.</summary>
        public event Action<CertificationExam> OnExamPassed;

        /// <summary>Fired when an exam attempt fails. Second arg is the reason string.</summary>
        public event Action<CertificationExam, string> OnExamFailed;

        /// <summary>Fired when the exam advances to its next lesson.</summary>
        public event Action<CertificationExam, string> OnExamLessonAdvanced;

        // ── Inspector ────────────────────────────────────────────────────────────

        [SerializeField] private FlightSchoolManager schoolManager;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>The exam currently being taken, or <c>null</c> when idle.</summary>
        public CertificationExam ActiveExam { get; private set; }

        /// <summary>Index of the lesson currently being attempted within the active exam.</summary>
        public int CurrentLessonIndex { get; private set; }

        // ── Internal state ───────────────────────────────────────────────────────

        private readonly List<CertificationExam> _examRegistry = new List<CertificationExam>();

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (schoolManager == null) schoolManager = FlightSchoolManager.Instance;
        }

        private void OnEnable()
        {
            if (schoolManager != null)
                schoolManager.OnLessonCompleted += HandleLessonCompleted;
        }

        private void OnDisable()
        {
            if (schoolManager != null)
                schoolManager.OnLessonCompleted -= HandleLessonCompleted;
        }

        // ── Registry ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers <paramref name="exam"/> so it can be looked up by
        /// <see cref="GetExamFor"/>. Ignores duplicates.
        /// </summary>
        public void RegisterExam(CertificationExam exam)
        {
            if (exam == null) return;
            foreach (var e in _examRegistry)
                if (e.certType == exam.certType) return;
            _examRegistry.Add(exam);
        }

        /// <summary>Returns the registered exam for <paramref name="certType"/>, or <c>null</c>.</summary>
        public CertificationExam GetExamFor(CertificationType certType)
        {
            foreach (var e in _examRegistry)
                if (e.certType == certType) return e;
            return null;
        }

        /// <summary>Returns a copy of all registered exams.</summary>
        public List<CertificationExam> GetAllExams() => new List<CertificationExam>(_examRegistry);

        // ── Session control ──────────────────────────────────────────────────────

        /// <summary>
        /// Begins an attempt on the exam for <paramref name="certType"/>.
        /// Returns <c>false</c> if the exam is not registered, the player is
        /// already in an exam, or the attempt limit has been reached.
        /// </summary>
        public bool StartExam(CertificationType certType)
        {
            if (ActiveExam != null) return false;

            var exam = GetExamFor(certType);
            if (exam == null) return false;
            if (exam.examLessonIds == null || exam.examLessonIds.Count == 0) return false;
            if (exam.maxAttempts > 0 && exam.attemptsUsed >= exam.maxAttempts) return false;

            exam.attemptsUsed++;
            ActiveExam         = exam;
            CurrentLessonIndex = 0;

            OnExamStarted?.Invoke(exam);
            StartCurrentLesson();
            return true;
        }

        /// <summary>
        /// Aborts the current exam attempt without awarding a certification.
        /// Raises <see cref="OnExamFailed"/> with reason "aborted".
        /// </summary>
        public void AbortExam()
        {
            if (ActiveExam == null) return;
            var exam = ActiveExam;
            ActiveExam = null;
            OnExamFailed?.Invoke(exam, "aborted");
        }

        // ── Event handlers ───────────────────────────────────────────────────────

        private void HandleLessonCompleted(FlightLesson lesson)
        {
            if (ActiveExam == null || lesson == null) return;
            if (CurrentLessonIndex >= ActiveExam.examLessonIds.Count) return;
            if (ActiveExam.examLessonIds[CurrentLessonIndex] != lesson.lessonId) return;

            if (lesson.bestScore < ActiveExam.minimumPassScore)
            {
                var failed = ActiveExam;
                ActiveExam = null;
                OnExamFailed?.Invoke(failed, $"score {lesson.bestScore:F0} below minimum {failed.minimumPassScore:F0}");
                return;
            }

            CurrentLessonIndex++;

            if (CurrentLessonIndex >= ActiveExam.examLessonIds.Count)
            {
                var passed = ActiveExam;
                ActiveExam = null;
                AwardCertification(passed.certType);
                OnExamPassed?.Invoke(passed);
            }
            else
            {
                OnExamLessonAdvanced?.Invoke(ActiveExam, ActiveExam.examLessonIds[CurrentLessonIndex]);
                StartCurrentLesson();
            }
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void StartCurrentLesson()
        {
            if (ActiveExam == null) return;
            if (schoolManager == null) return;
            if (CurrentLessonIndex < 0 || CurrentLessonIndex >= ActiveExam.examLessonIds.Count) return;

            schoolManager.StartLesson(ActiveExam.examLessonIds[CurrentLessonIndex]);
        }

        private void AwardCertification(CertificationType certType)
        {
            if (schoolManager == null) return;

            foreach (var cert in schoolManager.certifications)
            {
                if (cert.certType != certType || cert.isEarned) continue;
                cert.isEarned  = true;
                cert.earnedDate = DateTime.UtcNow.ToString("o");
                break;
            }

            schoolManager.SaveProgress();
        }
    }
}
