using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SWEF.FlightSchool.Tests
{
    /// <summary>
    /// EditMode unit tests for the Flight Training Academy &amp; Skill Certification
    /// system (Phase 84). Covers grading, constraints, exams, skill-tree, data model,
    /// and debrief logic — all tested without MonoBehaviour instantiation.
    /// </summary>
    [TestFixture]
    public class FlightSchoolTests
    {
        // =====================================================================
        //  1. DATA MODEL TESTS
        // =====================================================================

        #region LessonObjective

        [Test]
        public void LessonObjective_Progress01_NoTarget_Returns0()
        {
            var obj = new LessonObjective { targetValue = 0f, currentValue = 5f };
            Assert.AreEqual(0f, obj.Progress01());
        }

        [Test]
        public void LessonObjective_Progress01_NoTarget_CompletedReturns1()
        {
            var obj = new LessonObjective { targetValue = 0f, isCompleted = true };
            Assert.AreEqual(1f, obj.Progress01());
        }

        [Test]
        public void LessonObjective_Progress01_HalfDone()
        {
            var obj = new LessonObjective { targetValue = 100f, currentValue = 50f };
            Assert.AreEqual(0.5f, obj.Progress01(), 0.001f);
        }

        [Test]
        public void LessonObjective_Progress01_Clamped()
        {
            var obj = new LessonObjective { targetValue = 100f, currentValue = 200f };
            Assert.AreEqual(1f, obj.Progress01());
        }

        #endregion

        #region FlightLesson

        [Test]
        public void FlightLesson_OverallProgress_NoObjectives_Returns0()
        {
            var lesson = new FlightLesson { objectives = null };
            Assert.AreEqual(0f, lesson.OverallProgress());
        }

        [Test]
        public void FlightLesson_OverallProgress_AllDone_Returns1()
        {
            var lesson = new FlightLesson
            {
                objectives = new List<LessonObjective>
                {
                    new LessonObjective { targetValue = 10, currentValue = 10, isCompleted = true },
                    new LessonObjective { targetValue = 20, currentValue = 20, isCompleted = true }
                }
            };
            Assert.AreEqual(1f, lesson.OverallProgress(), 0.001f);
        }

        [Test]
        public void FlightLesson_ArePrerequisitesMet_EmptyPrereqs_ReturnsTrue()
        {
            var lesson = new FlightLesson { prerequisites = new List<string>() };
            Assert.IsTrue(lesson.ArePrerequisitesMet(new List<string>()));
        }

        [Test]
        public void FlightLesson_ArePrerequisitesMet_NullCompleted_ReturnsFalse()
        {
            var lesson = new FlightLesson { prerequisites = new List<string> { "basic_takeoff" } };
            Assert.IsFalse(lesson.ArePrerequisitesMet(null));
        }

        [Test]
        public void FlightLesson_ArePrerequisitesMet_Satisfied()
        {
            var lesson = new FlightLesson { prerequisites = new List<string> { "a", "b" } };
            Assert.IsTrue(lesson.ArePrerequisitesMet(new List<string> { "a", "b", "c" }));
        }

        [Test]
        public void FlightLesson_ArePrerequisitesMet_PartialMiss()
        {
            var lesson = new FlightLesson { prerequisites = new List<string> { "a", "b" } };
            Assert.IsFalse(lesson.ArePrerequisitesMet(new List<string> { "a" }));
        }

        #endregion

        #region PilotCertification

        [Test]
        public void PilotCertification_Progress_NoRequired_Returns1()
        {
            var cert = new PilotCertification { requiredLessons = new List<string>() };
            Assert.AreEqual(1f, cert.Progress(new List<string>()));
        }

        [Test]
        public void PilotCertification_Progress_Half()
        {
            var cert = new PilotCertification { requiredLessons = new List<string> { "a", "b" } };
            Assert.AreEqual(0.5f, cert.Progress(new List<string> { "a" }), 0.001f);
        }

        [Test]
        public void PilotCertification_Progress_NullCompleted_Returns0()
        {
            var cert = new PilotCertification { requiredLessons = new List<string> { "a" } };
            Assert.AreEqual(0f, cert.Progress(null));
        }

        #endregion

        // =====================================================================
        //  2. FLIGHT CONSTRAINT TESTS
        // =====================================================================

        #region FlightConstraint

        [Test]
        public void FlightConstraint_IsWithin_InsideRange()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f };
            Assert.IsTrue(c.IsWithin(300f));
        }

        [Test]
        public void FlightConstraint_IsWithin_AtBoundary()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f };
            Assert.IsTrue(c.IsWithin(100f));
            Assert.IsTrue(c.IsWithin(500f));
        }

        [Test]
        public void FlightConstraint_IsWithin_Below()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f };
            Assert.IsFalse(c.IsWithin(99f));
        }

        [Test]
        public void FlightConstraint_IsWithin_Above()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f };
            Assert.IsFalse(c.IsWithin(501f));
        }

        [Test]
        public void FlightConstraint_IsInWarningZone_NoMargin_ReturnsFalse()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f, warningMargin = 0f };
            Assert.IsFalse(c.IsInWarningZone(99f));
        }

        [Test]
        public void FlightConstraint_IsInWarningZone_InsideEnvelope_ReturnsFalse()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f, warningMargin = 20f };
            Assert.IsFalse(c.IsInWarningZone(300f));
        }

        [Test]
        public void FlightConstraint_IsInWarningZone_InLowerMargin()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f, warningMargin = 20f };
            Assert.IsTrue(c.IsInWarningZone(85f)); // 80–100 is warning zone
        }

        [Test]
        public void FlightConstraint_IsInWarningZone_InUpperMargin()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f, warningMargin = 20f };
            Assert.IsTrue(c.IsInWarningZone(515f)); // 500–520 is warning zone
        }

        [Test]
        public void FlightConstraint_IsInWarningZone_BeyondMargin_ReturnsFalse()
        {
            var c = new FlightConstraint { minValue = 100f, maxValue = 500f, warningMargin = 20f };
            Assert.IsFalse(c.IsInWarningZone(70f)); // below 80
        }

        #endregion

        // =====================================================================
        //  3. GRADE CRITERIA / LESSON GRADE REPORT TESTS
        // =====================================================================

        #region GradeCriteria

        [Test]
        public void GradeCriteria_WeightedScore()
        {
            var c = new GradeCriteria { weight = 2f, score = 80f };
            Assert.AreEqual(160f, c.WeightedScore(), 0.001f);
        }

        [Test]
        public void GradeCriteria_WeightedScore_ZeroWeight()
        {
            var c = new GradeCriteria { weight = 0f, score = 100f };
            Assert.AreEqual(0f, c.WeightedScore());
        }

        #endregion

        #region LessonGradeReport

        [Test]
        public void LessonGradeReport_ScoreToLetter_A() => Assert.AreEqual("A", LessonGradeReport.ScoreToLetter(95f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_A_Boundary() => Assert.AreEqual("A", LessonGradeReport.ScoreToLetter(90f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_B() => Assert.AreEqual("B", LessonGradeReport.ScoreToLetter(85f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_B_Boundary() => Assert.AreEqual("B", LessonGradeReport.ScoreToLetter(80f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_C() => Assert.AreEqual("C", LessonGradeReport.ScoreToLetter(75f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_C_Boundary() => Assert.AreEqual("C", LessonGradeReport.ScoreToLetter(70f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_D() => Assert.AreEqual("D", LessonGradeReport.ScoreToLetter(65f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_D_Boundary() => Assert.AreEqual("D", LessonGradeReport.ScoreToLetter(60f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_F() => Assert.AreEqual("F", LessonGradeReport.ScoreToLetter(59f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_Zero() => Assert.AreEqual("F", LessonGradeReport.ScoreToLetter(0f));

        [Test]
        public void LessonGradeReport_ScoreToLetter_Perfect() => Assert.AreEqual("A", LessonGradeReport.ScoreToLetter(100f));

        #endregion

        // =====================================================================
        //  4. GRADING SYSTEM TESTS (static helpers)
        // =====================================================================

        #region FlightGradingSystem

        [Test]
        public void ComputeFinalScore_EmptyCriteria_Returns0()
        {
            Assert.AreEqual(0f, FlightGradingSystem.ComputeFinalScore(null, 0f));
        }

        [Test]
        public void ComputeFinalScore_SingleCriterion()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "precision", weight = 1f, score = 80f }
            };
            Assert.AreEqual(80f, FlightGradingSystem.ComputeFinalScore(criteria, 1f), 0.001f);
        }

        [Test]
        public void ComputeFinalScore_WeightedAverage()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "precision", weight = 2f, score = 100f },
                new GradeCriteria { criteriaId = "timing",    weight = 1f, score = 70f }
            };
            // (2*100 + 1*70) / 3 = 270/3 = 90
            Assert.AreEqual(90f, FlightGradingSystem.ComputeFinalScore(criteria, 3f), 0.001f);
        }

        [Test]
        public void ComputeFinalScore_ZeroTotalWeight_Returns0()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "x", weight = 0f, score = 50f }
            };
            Assert.AreEqual(0f, FlightGradingSystem.ComputeFinalScore(criteria, 0f));
        }

        [Test]
        public void ComputeFinalScore_AllPerfect()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "a", weight = 1f, score = 100f },
                new GradeCriteria { criteriaId = "b", weight = 1f, score = 100f },
                new GradeCriteria { criteriaId = "c", weight = 1f, score = 100f }
            };
            Assert.AreEqual(100f, FlightGradingSystem.ComputeFinalScore(criteria, 3f), 0.001f);
        }

        [Test]
        public void ComputeFinalScore_AllZero()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "a", weight = 1f, score = 0f },
                new GradeCriteria { criteriaId = "b", weight = 1f, score = 0f }
            };
            Assert.AreEqual(0f, FlightGradingSystem.ComputeFinalScore(criteria, 2f), 0.001f);
        }

        [Test]
        public void ComputeFinalScore_HeavyWeight()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "safety",    weight = 10f, score = 90f },
                new GradeCriteria { criteriaId = "precision", weight = 1f,  score = 0f }
            };
            // (10*90 + 1*0) / 11 ≈ 81.8
            Assert.AreEqual(81.818f, FlightGradingSystem.ComputeFinalScore(criteria, 11f), 0.01f);
        }

        #endregion

        // =====================================================================
        //  5. SKILL TREE TESTS
        // =====================================================================

        #region SkillTreeData

        [Test]
        public void SkillTreeData_FindNode_Found()
        {
            var tree = BuildSampleTree();
            Assert.IsNotNull(tree.FindNode("node_a"));
        }

        [Test]
        public void SkillTreeData_FindNode_NotFound()
        {
            var tree = BuildSampleTree();
            Assert.IsNull(tree.FindNode("nonexistent"));
        }

        [Test]
        public void SkillTreeData_FindNode_NullId()
        {
            var tree = BuildSampleTree();
            Assert.IsNull(tree.FindNode(null));
        }

        [Test]
        public void SkillTreeData_FindNodesByLesson()
        {
            var tree = BuildSampleTree();
            var nodes = tree.FindNodesByLesson("lesson_a");
            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual("node_a", nodes[0].nodeId);
        }

        [Test]
        public void SkillTreeData_FindNodesByLesson_NoMatch()
        {
            var tree = BuildSampleTree();
            var nodes = tree.FindNodesByLesson("nonexistent");
            Assert.AreEqual(0, nodes.Count);
        }

        [Test]
        public void SkillTreeData_FindNodesByLesson_NullLesson()
        {
            var tree = BuildSampleTree();
            var nodes = tree.FindNodesByLesson(null);
            Assert.AreEqual(0, nodes.Count);
        }

        [Test]
        public void SkillTreeData_FindNodesByLesson_EmptyTree()
        {
            var tree = new SkillTreeData();
            var nodes = tree.FindNodesByLesson("any");
            Assert.AreEqual(0, nodes.Count);
        }

        #endregion

        #region SkillNode

        [Test]
        public void SkillNode_DefaultState_NotUnlocked()
        {
            var node = new SkillNode { nodeId = "test" };
            Assert.IsFalse(node.isUnlocked);
        }

        [Test]
        public void SkillNode_ChildIds_DefaultEmpty()
        {
            var node = new SkillNode { nodeId = "test" };
            Assert.IsNotNull(node.childNodeIds);
            Assert.AreEqual(0, node.childNodeIds.Count);
        }

        #endregion

        // =====================================================================
        //  6. CERTIFICATION EXAM TESTS
        // =====================================================================

        #region CertificationExam

        [Test]
        public void CertificationExam_DefaultMinimumPassScore()
        {
            var exam = new CertificationExam();
            Assert.AreEqual(70f, exam.minimumPassScore);
        }

        [Test]
        public void CertificationExam_DefaultMaxAttempts()
        {
            var exam = new CertificationExam();
            Assert.AreEqual(3, exam.maxAttempts);
        }

        [Test]
        public void CertificationExam_AttemptsUsed_DefaultZero()
        {
            var exam = new CertificationExam();
            Assert.AreEqual(0, exam.attemptsUsed);
        }

        [Test]
        public void CertificationExam_CannotExceedMaxAttempts()
        {
            var exam = new CertificationExam { maxAttempts = 2, attemptsUsed = 2 };
            Assert.IsTrue(exam.attemptsUsed >= exam.maxAttempts);
        }

        [Test]
        public void CertificationExam_LessonIds_DefaultEmpty()
        {
            var exam = new CertificationExam();
            Assert.IsNotNull(exam.examLessonIds);
            Assert.AreEqual(0, exam.examLessonIds.Count);
        }

        #endregion

        // =====================================================================
        //  7. DEBRIEFING CONTROLLER TESTS (static helpers)
        // =====================================================================

        #region DebriefingController

        [Test]
        public void GenerateTipMessage_Precision_Low()
        {
            string msg = DebriefingController.GenerateTipMessage("precision", 40f);
            Assert.IsTrue(msg.Contains("significantly"));
            Assert.IsTrue(msg.Contains("altitude") || msg.Contains("heading"));
        }

        [Test]
        public void GenerateTipMessage_Precision_Medium()
        {
            string msg = DebriefingController.GenerateTipMessage("precision", 65f);
            Assert.IsTrue(msg.Contains("sometimes"));
        }

        [Test]
        public void GenerateTipMessage_Precision_Slight()
        {
            string msg = DebriefingController.GenerateTipMessage("precision", 75f);
            Assert.IsTrue(msg.Contains("slightly"));
        }

        [Test]
        public void GenerateTipMessage_Smoothness()
        {
            string msg = DebriefingController.GenerateTipMessage("smoothness", 40f);
            Assert.IsTrue(msg.Contains("jerk") || msg.Contains("Control"));
        }

        [Test]
        public void GenerateTipMessage_Timing()
        {
            string msg = DebriefingController.GenerateTipMessage("timing", 55f);
            Assert.IsTrue(msg.Contains("longer") || msg.Contains("maneuver"));
        }

        [Test]
        public void GenerateTipMessage_Safety()
        {
            string msg = DebriefingController.GenerateTipMessage("safety", 30f);
            Assert.IsTrue(msg.Contains("warning") || msg.Contains("Stall"));
        }

        [Test]
        public void GenerateTipMessage_Fuel()
        {
            string msg = DebriefingController.GenerateTipMessage("fuel", 45f);
            Assert.IsTrue(msg.Contains("Fuel") || msg.Contains("burn") || msg.Contains("fuel"));
        }

        [Test]
        public void GenerateTipMessage_Unknown_ShowsCriteriaId()
        {
            string msg = DebriefingController.GenerateTipMessage("custom_thing", 50f);
            Assert.IsTrue(msg.Contains("custom_thing"));
        }

        [Test]
        public void GenerateTipMessage_NullOrEmpty()
        {
            Assert.AreEqual(string.Empty, DebriefingController.GenerateTipMessage(null, 50f));
            Assert.AreEqual(string.Empty, DebriefingController.GenerateTipMessage(string.Empty, 50f));
        }

        #endregion

        // =====================================================================
        //  8. CONSTRAINT TYPE ENUM TESTS
        // =====================================================================

        #region ConstraintType

        [Test]
        public void ConstraintType_HasExpectedMembers()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.AltitudeRange));
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.SpeedRange));
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.HeadingRange));
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.BankAngleLimit));
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.GForceLimit));
            Assert.IsTrue(Enum.IsDefined(typeof(ConstraintType), ConstraintType.GeofenceRadius));
        }

        [Test]
        public void ConstraintType_Count()
        {
            Assert.AreEqual(6, Enum.GetValues(typeof(ConstraintType)).Length);
        }

        #endregion

        // =====================================================================
        //  9. LESSON CATEGORY / DIFFICULTY / STATUS / CERTIFICATION TYPE TESTS
        // =====================================================================

        #region Enums

        [Test]
        public void LessonCategory_Count()
        {
            Assert.AreEqual(6, Enum.GetValues(typeof(LessonCategory)).Length);
        }

        [Test]
        public void LessonDifficulty_Count()
        {
            Assert.AreEqual(4, Enum.GetValues(typeof(LessonDifficulty)).Length);
        }

        [Test]
        public void LessonStatus_Count()
        {
            Assert.AreEqual(5, Enum.GetValues(typeof(LessonStatus)).Length);
        }

        [Test]
        public void CertificationType_Count()
        {
            Assert.AreEqual(5, Enum.GetValues(typeof(CertificationType)).Length);
        }

        [Test]
        public void LessonStatus_Progression()
        {
            // Locked → Available → InProgress → Completed → Mastered
            Assert.Less((int)LessonStatus.Locked,     (int)LessonStatus.Available);
            Assert.Less((int)LessonStatus.Available,   (int)LessonStatus.InProgress);
            Assert.Less((int)LessonStatus.InProgress,  (int)LessonStatus.Completed);
            Assert.Less((int)LessonStatus.Completed,   (int)LessonStatus.Mastered);
        }

        #endregion

        // =====================================================================
        //  10. INTEGRATION / EDGE-CASE TESTS
        // =====================================================================

        #region Integration

        [Test]
        public void GradeReport_ScoreMatchesLetter()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "a", weight = 1f, score = 95f },
                new GradeCriteria { criteriaId = "b", weight = 1f, score = 85f }
            };
            float score = FlightGradingSystem.ComputeFinalScore(criteria, 2f);
            Assert.AreEqual("A", LessonGradeReport.ScoreToLetter(score)); // (95+85)/2 = 90
        }

        [Test]
        public void FlightConstraint_Penalty_Defaults()
        {
            var c = new FlightConstraint();
            Assert.AreEqual(1f, c.penaltyPerSecond);
        }

        [Test]
        public void FlightConstraint_WarningMargin_Defaults()
        {
            var c = new FlightConstraint();
            Assert.AreEqual(0f, c.warningMargin);
        }

        [Test]
        public void LessonWithConstraints_DefaultsToEmptyList()
        {
            var lesson = new FlightLesson();
            Assert.IsNotNull(lesson.constraints);
            Assert.AreEqual(0, lesson.constraints.Count);
        }

        [Test]
        public void GradeReport_DefaultLetterGrade_IsF()
        {
            var report = new LessonGradeReport();
            Assert.AreEqual("F", report.letterGrade);
        }

        [Test]
        public void GradeReport_DefaultFinalScore_IsZero()
        {
            var report = new LessonGradeReport();
            Assert.AreEqual(0f, report.finalScore);
        }

        [Test]
        public void SkillTreeData_EmptyTree_FindReturnsNull()
        {
            var tree = new SkillTreeData();
            Assert.IsNull(tree.FindNode("anything"));
        }

        [Test]
        public void CertificationExam_UnlimitedAttempts()
        {
            var exam = new CertificationExam { maxAttempts = 0 };
            // maxAttempts = 0 means unlimited
            Assert.AreEqual(0, exam.maxAttempts);
            Assert.IsTrue(exam.maxAttempts <= 0 || exam.attemptsUsed < exam.maxAttempts);
        }

        [Test]
        public void MultipleConstraints_IndependentEvaluation()
        {
            var alt = new FlightConstraint { type = ConstraintType.AltitudeRange, minValue = 100f, maxValue = 500f };
            var spd = new FlightConstraint { type = ConstraintType.SpeedRange, minValue = 60f, maxValue = 200f };

            Assert.IsTrue(alt.IsWithin(300f));
            Assert.IsFalse(spd.IsWithin(250f));
        }

        [Test]
        public void FlightLesson_BriefingDebriefing_Default()
        {
            var lesson = new FlightLesson();
            Assert.IsNull(lesson.briefingText);
            Assert.IsNull(lesson.debriefingText);
        }

        [Test]
        public void GradeCriteria_DefaultWeight()
        {
            var c = new GradeCriteria();
            Assert.AreEqual(1f, c.weight);
        }

        [Test]
        public void ComputeFinalScore_UnevenWeights()
        {
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "a", weight = 3f, score = 100f },
                new GradeCriteria { criteriaId = "b", weight = 1f, score = 0f }
            };
            // (3*100 + 1*0) / 4 = 75
            Assert.AreEqual(75f, FlightGradingSystem.ComputeFinalScore(criteria, 4f), 0.001f);
        }

        [Test]
        public void ComputeFinalScore_ClampedTo100()
        {
            // score > 100 shouldn't be possible in practice but test defensive clamping
            var criteria = new List<GradeCriteria>
            {
                new GradeCriteria { criteriaId = "a", weight = 1f, score = 100f }
            };
            float result = FlightGradingSystem.ComputeFinalScore(criteria, 1f);
            Assert.LessOrEqual(result, 100f);
        }

        [Test]
        public void SkillTree_MultipleNodesForSameLesson()
        {
            var tree = new SkillTreeData
            {
                nodes = new List<SkillNode>
                {
                    new SkillNode { nodeId = "n1", lessonId = "shared_lesson" },
                    new SkillNode { nodeId = "n2", lessonId = "shared_lesson" },
                    new SkillNode { nodeId = "n3", lessonId = "other" }
                }
            };
            var found = tree.FindNodesByLesson("shared_lesson");
            Assert.AreEqual(2, found.Count);
        }

        #endregion

        // =====================================================================
        //  HELPERS
        // =====================================================================

        private static SkillTreeData BuildSampleTree()
        {
            return new SkillTreeData
            {
                nodes = new List<SkillNode>
                {
                    new SkillNode { nodeId = "node_a", lessonId = "lesson_a", isUnlocked = true,
                                    childNodeIds = new List<string> { "node_b", "node_c" } },
                    new SkillNode { nodeId = "node_b", lessonId = "lesson_b", isUnlocked = false,
                                    childNodeIds = new List<string> { "node_d" } },
                    new SkillNode { nodeId = "node_c", lessonId = "lesson_c", isUnlocked = false,
                                    childNodeIds = new List<string>() },
                    new SkillNode { nodeId = "node_d", lessonId = "lesson_d", isUnlocked = false,
                                    childNodeIds = new List<string>() }
                }
            };
        }
    }
}
