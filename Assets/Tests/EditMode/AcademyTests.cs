// AcademyTests.cs — NUnit EditMode tests for Phase 104 Flight Academy & Certification System
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.Academy;

[TestFixture]
public class AcademyTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // LicenseTier enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LicenseTier_ValuesAreOrdered()
    {
        Assert.Less((int)LicenseTier.None,                 (int)LicenseTier.StudentPilot,          "None < StudentPilot");
        Assert.Less((int)LicenseTier.StudentPilot,         (int)LicenseTier.PrivatePilot,          "StudentPilot < PrivatePilot");
        Assert.Less((int)LicenseTier.PrivatePilot,         (int)LicenseTier.CommercialPilot,       "PrivatePilot < CommercialPilot");
        Assert.Less((int)LicenseTier.CommercialPilot,      (int)LicenseTier.AirlineTransportPilot, "CommercialPilot < ATP");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CertificateData
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CertificateData_Create_SetsFields()
    {
        var cert = CertificateData.Create(
            "Private Pilot License (PPL)",
            LicenseTier.PrivatePilot,
            "Jane Doe",
            88.5f,
            "PPL Course",
            "Academy/Badges/badge_private_pilot");

        Assert.AreEqual("Private Pilot License (PPL)", cert.certificateName);
        Assert.AreEqual(LicenseTier.PrivatePilot,       cert.tier);
        Assert.AreEqual("Jane Doe",                     cert.pilotName);
        Assert.AreEqual(88.5f,                          cert.examScore, 1e-4f);
        Assert.AreEqual("PPL Course",                   cert.curriculumName);
        Assert.IsFalse(string.IsNullOrEmpty(cert.certificateId),   "certificateId should be set");
        Assert.IsFalse(string.IsNullOrEmpty(cert.awardedDateUtc),  "awardedDateUtc should be set");
    }

    [Test]
    public void CertificateData_Create_GeneratesUniqueIds()
    {
        var c1 = CertificateData.Create("A", LicenseTier.StudentPilot, "X", 70f, "C");
        var c2 = CertificateData.Create("A", LicenseTier.StudentPilot, "X", 70f, "C");
        Assert.AreNotEqual(c1.certificateId, c2.certificateId, "Each certificate should have a unique ID");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TheoryModule — CalculateScore
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TheoryModule_CalculateScore_AllCorrect()
    {
        var module = new TheoryModule
        {
            questions = new List<TheoryQuestion>
            {
                new TheoryQuestion { correctAnswerIndex = 0 },
                new TheoryQuestion { correctAnswerIndex = 2 },
                new TheoryQuestion { correctAnswerIndex = 1 }
            },
            passingScore = 70f
        };

        float score = module.CalculateScore(new[] { 0, 2, 1 });
        Assert.AreEqual(100f, score, 1e-4f);
        Assert.IsTrue(module.IsPassing(score));
    }

    [Test]
    public void TheoryModule_CalculateScore_AllWrong()
    {
        var module = new TheoryModule
        {
            questions = new List<TheoryQuestion>
            {
                new TheoryQuestion { correctAnswerIndex = 0 },
                new TheoryQuestion { correctAnswerIndex = 0 }
            },
            passingScore = 70f
        };

        float score = module.CalculateScore(new[] { 1, 1 });
        Assert.AreEqual(0f, score, 1e-4f);
        Assert.IsFalse(module.IsPassing(score));
    }

    [Test]
    public void TheoryModule_CalculateScore_PartialCorrect()
    {
        var module = new TheoryModule
        {
            questions = new List<TheoryQuestion>
            {
                new TheoryQuestion { correctAnswerIndex = 0 },
                new TheoryQuestion { correctAnswerIndex = 1 },
                new TheoryQuestion { correctAnswerIndex = 2 },
                new TheoryQuestion { correctAnswerIndex = 3 }
            },
            passingScore = 50f
        };

        float score = module.CalculateScore(new[] { 0, 1, 0, 0 }); // 2 / 4 = 50%
        Assert.AreEqual(50f, score, 1e-4f);
        Assert.IsTrue(module.IsPassing(score));
    }

    [Test]
    public void TheoryModule_CalculateScore_EmptyQuestions_ReturnsZero()
    {
        var module = new TheoryModule { questions = new List<TheoryQuestion>() };
        float score = module.CalculateScore(new[] { 0 });
        Assert.AreEqual(0f, score, 1e-4f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PracticalExercise — CalculateScore
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PracticalExercise_CalculateScore_FullPoints()
    {
        var exercise = new PracticalExercise
        {
            objectives = new List<ExerciseObjective>
            {
                new ExerciseObjective { objectiveTag = "takeoff", maxPoints = 10 },
                new ExerciseObjective { objectiveTag = "heading", maxPoints = 10 }
            },
            passingScorePercent = 60f
        };

        var earned = new Dictionary<string, int> { { "takeoff", 10 }, { "heading", 10 } };
        float score = exercise.CalculateScore(earned);
        Assert.AreEqual(100f, score, 1e-4f);
        Assert.IsTrue(exercise.IsPassing(score));
    }

    [Test]
    public void PracticalExercise_CalculateScore_ZeroPoints()
    {
        var exercise = new PracticalExercise
        {
            objectives = new List<ExerciseObjective>
            {
                new ExerciseObjective { objectiveTag = "obj1", maxPoints = 5 }
            },
            passingScorePercent = 60f
        };

        float score = exercise.CalculateScore(new Dictionary<string, int>());
        Assert.AreEqual(0f, score, 1e-4f);
        Assert.IsFalse(exercise.IsPassing(score));
    }

    [Test]
    public void PracticalExercise_CalculateScore_ClampsAboveMax()
    {
        var exercise = new PracticalExercise
        {
            objectives = new List<ExerciseObjective>
            {
                new ExerciseObjective { objectiveTag = "obj", maxPoints = 10 }
            }
        };
        var earned = new Dictionary<string, int> { { "obj", 999 } };
        float score = exercise.CalculateScore(earned);
        Assert.AreEqual(100f, score, 1e-4f, "Clamped to 100% even when earned > max");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightExam — ComputeOverallScore / BuildResult
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightExam_ComputeOverallScore_WeightedAverage()
    {
        var exam = new FlightExam
        {
            examId          = "test_ppl",
            theoryWeight    = 0.4f,
            passingScore    = 70f,
            theoryComponent    = new TheoryModule(),
            practicalComponent = new PracticalExercise()
        };

        float overall = exam.ComputeOverallScore(80f, 60f);
        // 80*0.4 + 60*0.6 = 32 + 36 = 68
        Assert.AreEqual(68f, overall, 0.01f);
    }

    [Test]
    public void FlightExam_BuildResult_PassWhenScoreAboveThreshold()
    {
        var exam = new FlightExam
        {
            examId          = "test_sp",
            theoryWeight    = 0.5f,
            passingScore    = 70f,
            theoryComponent    = new TheoryModule(),
            practicalComponent = new PracticalExercise()
        };

        var result = exam.BuildResult(80f, 80f, 1);
        Assert.IsTrue(result.passed);
        Assert.AreEqual(80f, result.overallScore, 0.01f);
        Assert.AreEqual(1, result.attemptNumber);
    }

    [Test]
    public void FlightExam_BuildResult_FailWhenScoreBelowThreshold()
    {
        var exam = new FlightExam
        {
            examId          = "test_sp",
            theoryWeight    = 0.5f,
            passingScore    = 70f,
            theoryComponent    = new TheoryModule(),
            practicalComponent = new PracticalExercise()
        };

        var result = exam.BuildResult(50f, 50f, 2);
        Assert.IsFalse(result.passed);
        Assert.AreEqual(2, result.attemptNumber);
    }

    [Test]
    public void FlightExam_ComputeOverallScore_TheoryOnly()
    {
        var exam = new FlightExam
        {
            examId        = "theory_only",
            theoryWeight  = 0.4f,
            theoryComponent = new TheoryModule()
            // no practicalComponent
        };

        float overall = exam.ComputeOverallScore(75f, 0f);
        Assert.AreEqual(75f, overall, 0.01f, "Theory-only exam should return the theory score directly");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AcademyProgressTracker
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AcademyProgressTracker_Enroll_SetsEnrolledFlag()
    {
        var tracker = new AcademyProgressTracker();
        tracker.EnrollInCurriculum("ppl_course");
        var cp = tracker.GetCurriculumProgress("ppl_course");
        Assert.IsNotNull(cp);
        Assert.IsTrue(cp.isEnrolled);
    }

    [Test]
    public void AcademyProgressTracker_LessonLifecycle()
    {
        var tracker = new AcademyProgressTracker();
        tracker.EnrollInCurriculum("sp_course");
        tracker.UnlockLesson("sp_course", "lesson_01");

        Assert.AreEqual(LessonStatus.Available,  tracker.GetLessonStatus("sp_course", "lesson_01"));

        tracker.StartLesson("sp_course", "lesson_01");
        Assert.AreEqual(LessonStatus.InProgress, tracker.GetLessonStatus("sp_course", "lesson_01"));

        tracker.CompleteLesson("sp_course", "lesson_01", 100);
        Assert.AreEqual(LessonStatus.Completed,  tracker.GetLessonStatus("sp_course", "lesson_01"));
        Assert.AreEqual(100, tracker.Data.totalXpEarned);
    }

    [Test]
    public void AcademyProgressTracker_AddCertificate_UpdatesHighestTier()
    {
        var tracker = new AcademyProgressTracker();
        Assert.AreEqual(LicenseTier.None, tracker.HighestTier);

        var cert = CertificateData.Create("PPL", LicenseTier.PrivatePilot, "Pilot", 75f, "PPL Course");
        tracker.AddCertificate(cert);

        Assert.AreEqual(LicenseTier.PrivatePilot, tracker.HighestTier);
    }

    [Test]
    public void AcademyProgressTracker_RecordExamResult_TracksAttemptCount()
    {
        var tracker = new AcademyProgressTracker();
        Assert.AreEqual(0, tracker.GetExamAttemptCount("exam_ppl"));

        tracker.RecordExamResult(new ExamResult { examId = "exam_ppl", passed = false, attemptNumber = 1 });
        Assert.AreEqual(1, tracker.GetExamAttemptCount("exam_ppl"));

        tracker.RecordExamResult(new ExamResult { examId = "exam_ppl", passed = true, attemptNumber = 2 });
        Assert.AreEqual(2, tracker.GetExamAttemptCount("exam_ppl"));
    }

    [Test]
    public void AcademyProgressTracker_UnknownLesson_IsLocked()
    {
        var tracker = new AcademyProgressTracker();
        Assert.AreEqual(LessonStatus.Locked, tracker.GetLessonStatus("none", "none"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CertificationManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CertificationManager_IssueCertificate_OnPassedExam()
    {
        var tracker = new AcademyProgressTracker();
        var certMgr = new CertificationManager(tracker);

        CertificateData issued = null;
        certMgr.OnCertificateIssued += c => issued = c;

        var result = new ExamResult
        {
            examId       = "exam_ppl",
            passed       = true,
            overallScore = 82f
        };

        var cert = certMgr.IssueCertificate("Pilot Joe", LicenseTier.PrivatePilot, result, "PPL Course");

        Assert.IsNotNull(cert);
        Assert.IsNotNull(issued);
        Assert.AreEqual("Pilot Joe",           cert.pilotName);
        Assert.AreEqual(LicenseTier.PrivatePilot, cert.tier);
        Assert.IsTrue(certMgr.HasCertificate(LicenseTier.PrivatePilot));
    }

    [Test]
    public void CertificationManager_IssueCertificate_ThrowsOnFailedExam()
    {
        var tracker = new AcademyProgressTracker();
        var certMgr = new CertificationManager(tracker);

        var result = new ExamResult { examId = "exam", passed = false, overallScore = 40f };

        Assert.Throws<InvalidOperationException>(
            () => certMgr.IssueCertificate("Pilot", LicenseTier.StudentPilot, result, "Course"));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TrainingSessionManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TrainingSessionManager_TheoryOnlyLesson_CompletesCorrectly()
    {
        var session = new TrainingSessionManager();

        var lesson = new FlightLesson
        {
            lessonId        = "theory_lesson",
            xpReward        = 50,
            theoryModule    = new TheoryModule
            {
                questions    = new List<TheoryQuestion>
                {
                    new TheoryQuestion { correctAnswerIndex = 0 },
                    new TheoryQuestion { correctAnswerIndex = 1 }
                },
                passingScore = 50f
            }
            // no practicalExercise
        };

        SessionResult capturedResult = null;
        session.OnSessionCompleted += r => capturedResult = r;

        session.StartSession("sp_course", lesson);
        Assert.AreEqual(SessionPhase.Briefing, session.Phase);

        session.BeginLesson();
        Assert.AreEqual(SessionPhase.TheoryQuiz, session.Phase);

        session.SubmitTheoryAnswers(new[] { 0, 1 }); // all correct → 100%
        Assert.IsNotNull(capturedResult);
        Assert.IsTrue(capturedResult.lessonCompleted);
        Assert.AreEqual(100f, capturedResult.theoryScore, 1e-4f);
        Assert.AreEqual(50,   capturedResult.xpAwarded);
    }

    [Test]
    public void TrainingSessionManager_AbortSession_SetsAbortedPhase()
    {
        var session = new TrainingSessionManager();

        var lesson = new FlightLesson
        {
            lessonId     = "abort_lesson",
            theoryModule = new TheoryModule { questions = new List<TheoryQuestion>() }
        };

        session.StartSession("c1", lesson);
        session.AbortSession();

        Assert.AreEqual(SessionPhase.Aborted, session.Phase);
        Assert.IsNull(session.ActiveLesson);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FlightCurriculum ScriptableObject
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FlightCurriculum_GetLesson_ReturnsCorrectLesson()
    {
        var curriculum = ScriptableObject.CreateInstance<FlightCurriculum>();
        try
        {
            curriculum.curriculumId = "test_curr";
            curriculum.lessons = new List<FlightLesson>
            {
                new FlightLesson { lessonId = "l1", title = "Lesson 1" },
                new FlightLesson { lessonId = "l2", title = "Lesson 2" }
            };

            var found = curriculum.GetLesson("l2");
            Assert.IsNotNull(found);
            Assert.AreEqual("Lesson 2", found.title);

            Assert.IsNull(curriculum.GetLesson("l99"), "Should return null for unknown lessonId");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(curriculum);
        }
    }

    [Test]
    public void FlightCurriculum_LessonCount_ReturnsCorrectValue()
    {
        var curriculum = ScriptableObject.CreateInstance<FlightCurriculum>();
        try
        {
            curriculum.lessons = new List<FlightLesson>
            {
                new FlightLesson { lessonId = "l1" },
                new FlightLesson { lessonId = "l2" },
                new FlightLesson { lessonId = "l3" }
            };
            Assert.AreEqual(3, curriculum.LessonCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(curriculum);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AcademyConfig ScriptableObject
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AcademyConfig_DefaultValues_AreCorrect()
    {
        var cfg = ScriptableObject.CreateInstance<AcademyConfig>();
        try
        {
            Assert.AreEqual("SkyWalking Flight Academy", cfg.academyName);
            Assert.AreEqual("academy_progress.json",    cfg.saveFileName);
            Assert.AreEqual(500,                        cfg.certificateXpBonus);
            Assert.AreEqual(1f,                         cfg.xpMultiplier, 1e-4f);
            Assert.IsTrue(cfg.autoResumeSession);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cfg);
        }
    }
}
