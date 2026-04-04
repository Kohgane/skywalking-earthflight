// AcademyProgressTracker.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// Per-lesson progress record stored in the player's save.
    /// </summary>
    [Serializable]
    public class LessonProgress
    {
        public string       lessonId;
        public LessonStatus status             = LessonStatus.Locked;
        public float        theoryScore        = 0f;
        public float        practicalScore     = 0f;
        public int          attemptsTheory     = 0;
        public int          attemptsPractical  = 0;
        public string       completedAtUtc;
    }

    /// <summary>
    /// Per-curriculum progress record.
    /// </summary>
    [Serializable]
    public class CurriculumProgress
    {
        public string                  curriculumId;
        public bool                    isEnrolled;
        public bool                    isCompleted;
        public string                  startedAtUtc;
        public string                  completedAtUtc;
        public List<LessonProgress>    lessonProgress = new List<LessonProgress>();
    }

    /// <summary>
    /// Full academy progress payload persisted to disk.
    /// </summary>
    [Serializable]
    public class AcademyProgressData
    {
        public string                     pilotName          = "Pilot";
        public LicenseTier                highestTier        = LicenseTier.None;
        public int                        totalXpEarned      = 0;
        public List<CurriculumProgress>   curricula          = new List<CurriculumProgress>();
        public List<CertificateData>      certificates       = new List<CertificateData>();
        public List<ExamResult>           examHistory        = new List<ExamResult>();
    }

    /// <summary>
    /// Tracks, queries, and persists per-player progress across all Academy courses.
    /// Works against an in-memory <see cref="AcademyProgressData"/> payload that is
    /// serialised to JSON by the <see cref="FlightAcademyManager"/>.
    /// </summary>
    public class AcademyProgressTracker
    {
        // ── State ──────────────────────────────────────────────────────────────
        private AcademyProgressData _data;

        // ── Constructor ────────────────────────────────────────────────────────
        public AcademyProgressTracker(AcademyProgressData existingData = null)
        {
            _data = existingData ?? new AcademyProgressData();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Reference to the live progress data (do not mutate externally).</summary>
        public AcademyProgressData Data => _data;

        /// <summary>The pilot's current highest license tier.</summary>
        public LicenseTier HighestTier => _data.highestTier;

        // ── Curriculum ─────────────────────────────────────────────────────────

        /// <summary>Enrolls the player in the curriculum with the given ID.</summary>
        public void EnrollInCurriculum(string curriculumId)
        {
            var cp = GetOrCreateCurriculumProgress(curriculumId);
            if (cp.isEnrolled) return;
            cp.isEnrolled   = true;
            cp.startedAtUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Returns the <see cref="CurriculumProgress"/> for the given ID, or <c>null</c>.</summary>
        public CurriculumProgress GetCurriculumProgress(string curriculumId)
        {
            foreach (var cp in _data.curricula)
                if (cp.curriculumId == curriculumId) return cp;
            return null;
        }

        /// <summary>Marks a curriculum as fully completed.</summary>
        public void CompleteCurriculum(string curriculumId)
        {
            var cp = GetOrCreateCurriculumProgress(curriculumId);
            cp.isCompleted     = true;
            cp.completedAtUtc  = DateTime.UtcNow.ToString("o");
        }

        // ── Lessons ────────────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="LessonStatus"/> for the given lesson inside a curriculum.</summary>
        public LessonStatus GetLessonStatus(string curriculumId, string lessonId)
        {
            var lp = GetLessonProgress(curriculumId, lessonId);
            return lp?.status ?? LessonStatus.Locked;
        }

        /// <summary>Unlocks the lesson so the player can start it.</summary>
        public void UnlockLesson(string curriculumId, string lessonId)
        {
            var lp = GetOrCreateLessonProgress(curriculumId, lessonId);
            if (lp.status == LessonStatus.Locked)
                lp.status = LessonStatus.Available;
        }

        /// <summary>Marks the lesson as in-progress.</summary>
        public void StartLesson(string curriculumId, string lessonId)
        {
            var lp = GetOrCreateLessonProgress(curriculumId, lessonId);
            lp.status = LessonStatus.InProgress;
        }

        /// <summary>Records theory score and increments attempt counter.</summary>
        public void RecordTheoryScore(string curriculumId, string lessonId, float score)
        {
            var lp = GetOrCreateLessonProgress(curriculumId, lessonId);
            lp.theoryScore = score;
            lp.attemptsTheory++;
        }

        /// <summary>Records practical score and increments attempt counter.</summary>
        public void RecordPracticalScore(string curriculumId, string lessonId, float score)
        {
            var lp = GetOrCreateLessonProgress(curriculumId, lessonId);
            lp.practicalScore = score;
            lp.attemptsPractical++;
        }

        /// <summary>Marks the lesson as fully completed and awards XP.</summary>
        public void CompleteLesson(string curriculumId, string lessonId, int xpReward)
        {
            var lp = GetOrCreateLessonProgress(curriculumId, lessonId);
            lp.status          = LessonStatus.Completed;
            lp.completedAtUtc  = DateTime.UtcNow.ToString("o");
            _data.totalXpEarned += xpReward;
        }

        // ── Certificates ───────────────────────────────────────────────────────

        /// <summary>Adds a certificate to the player's gallery.</summary>
        public void AddCertificate(CertificateData cert)
        {
            _data.certificates.Add(cert);
            if (cert.tier > _data.highestTier)
                _data.highestTier = cert.tier;
        }

        /// <summary>Returns all certificates for the given tier.</summary>
        public List<CertificateData> GetCertificates(LicenseTier tier)
        {
            var result = new List<CertificateData>();
            foreach (var c in _data.certificates)
                if (c.tier == tier) result.Add(c);
            return result;
        }

        // ── Exams ──────────────────────────────────────────────────────────────

        /// <summary>Stores an exam result in the history list.</summary>
        public void RecordExamResult(ExamResult result)
        {
            _data.examHistory.Add(result);
        }

        /// <summary>Returns the number of previous attempts for the given exam ID.</summary>
        public int GetExamAttemptCount(string examId)
        {
            int count = 0;
            foreach (var r in _data.examHistory)
                if (r.examId == examId) count++;
            return count;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private CurriculumProgress GetOrCreateCurriculumProgress(string curriculumId)
        {
            foreach (var cp in _data.curricula)
                if (cp.curriculumId == curriculumId) return cp;
            var newCp = new CurriculumProgress { curriculumId = curriculumId };
            _data.curricula.Add(newCp);
            return newCp;
        }

        private LessonProgress GetLessonProgress(string curriculumId, string lessonId)
        {
            var cp = GetCurriculumProgress(curriculumId);
            if (cp == null) return null;
            foreach (var lp in cp.lessonProgress)
                if (lp.lessonId == lessonId) return lp;
            return null;
        }

        private LessonProgress GetOrCreateLessonProgress(string curriculumId, string lessonId)
        {
            var cp = GetOrCreateCurriculumProgress(curriculumId);
            foreach (var lp in cp.lessonProgress)
                if (lp.lessonId == lessonId) return lp;
            var newLp = new LessonProgress { lessonId = lessonId };
            cp.lessonProgress.Add(newLp);
            return newLp;
        }
    }
}
