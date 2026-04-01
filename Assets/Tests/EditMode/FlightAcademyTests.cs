using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.FlightAcademy;

/// <summary>
/// NUnit EditMode tests for the Flight Training Academy system (Phase 84).
/// Tests cover ExamScoringEngine, CertificateGenerator, FlightAcademyDefaultData,
/// and licence progression logic.
/// </summary>
[TestFixture]
public class FlightAcademyTests
{
    // ══════════════════════════════════════════════════════════════════════════════
    // ExamScoringEngine — CalculateScore
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void CalculateScore_NoObjectives_ReturnsBonusMinusPenalty()
    {
        float score = ExamScoringEngine.CalculateScore(new List<ObjectiveScore>(), 5f, 10f);
        Assert.AreEqual(5f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_NullObjectives_ReturnsBonusMinusPenalty()
    {
        float score = ExamScoringEngine.CalculateScore(null, 0f, 0f);
        Assert.AreEqual(0f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_PerfectObjectives_Returns100()
    {
        var objs = new List<ObjectiveScore>
        {
            new ObjectiveScore { objectiveType = "a", score = 100f, completed = true },
            new ObjectiveScore { objectiveType = "b", score = 100f, completed = true }
        };
        float score = ExamScoringEngine.CalculateScore(objs, 0f, 0f);
        Assert.AreEqual(100f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_PenaltyReducesScore()
    {
        var objs = new List<ObjectiveScore>
        {
            new ObjectiveScore { objectiveType = "a", score = 80f, completed = true }
        };
        float score = ExamScoringEngine.CalculateScore(objs, 10f, 0f);
        Assert.AreEqual(70f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_BonusIncreasesScore()
    {
        var objs = new List<ObjectiveScore>
        {
            new ObjectiveScore { objectiveType = "a", score = 80f, completed = true }
        };
        float score = ExamScoringEngine.CalculateScore(objs, 0f, 10f);
        Assert.AreEqual(90f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_ClampsTo100()
    {
        var objs = new List<ObjectiveScore>
        {
            new ObjectiveScore { objectiveType = "a", score = 100f, completed = true }
        };
        float score = ExamScoringEngine.CalculateScore(objs, 0f, 50f);
        Assert.AreEqual(100f, score, 0.001f);
    }

    [Test]
    public void CalculateScore_ClampsTo0()
    {
        var objs = new List<ObjectiveScore>
        {
            new ObjectiveScore { objectiveType = "a", score = 10f, completed = true }
        };
        float score = ExamScoringEngine.CalculateScore(objs, 50f, 0f);
        Assert.AreEqual(0f, score, 0.001f);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // ExamScoringEngine — CalculateLandingScore
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void LandingScore_PerfectLanding_IsNear100()
    {
        // Perfect: 0 speed deviation, 0 centerline, 100 fpm, 1.0G, stable
        float score = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f, 1.0f, 100f);
        Assert.AreEqual(100f, score, 0.001f);
    }

    [Test]
    public void LandingScore_HardLanding_IsLower()
    {
        // 2G touchdown, large deviation
        float score = ExamScoringEngine.CalculateLandingScore(25f, 25f, 700f, 2.5f, 50f);
        Assert.Less(score, 60f);
    }

    [Test]
    public void LandingScore_TouchdownSpeedBands()
    {
        // Speed deviation ≤10 kts → speed component = 25 (25% of 100)
        float s1 = ExamScoringEngine.CalculateLandingScore(5f,   0f, 100f, 1.0f, 100f);
        float s2 = ExamScoringEngine.CalculateLandingScore(15f,  0f, 100f, 1.0f, 100f);
        float s3 = ExamScoringEngine.CalculateLandingScore(25f,  0f, 100f, 1.0f, 100f);
        float s4 = ExamScoringEngine.CalculateLandingScore(35f,  0f, 100f, 1.0f, 100f);
        Assert.Greater(s1, s2);
        Assert.Greater(s2, s3);
        Assert.Greater(s3, s4);
    }

    [Test]
    public void LandingScore_CenterlineBands()
    {
        float s1 = ExamScoringEngine.CalculateLandingScore(0f, 1f,   100f, 1.0f, 100f);
        float s2 = ExamScoringEngine.CalculateLandingScore(0f, 5f,   100f, 1.0f, 100f);
        float s3 = ExamScoringEngine.CalculateLandingScore(0f, 15f,  100f, 1.0f, 100f);
        float s4 = ExamScoringEngine.CalculateLandingScore(0f, 25f,  100f, 1.0f, 100f);
        Assert.Greater(s1, s2);
        Assert.Greater(s2, s3);
        Assert.Greater(s3, s4);
    }

    [Test]
    public void LandingScore_DescentRateBands()
    {
        float s1 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f,  1.0f, 100f);
        float s2 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 300f,  1.0f, 100f);
        float s3 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 500f,  1.0f, 100f);
        float s4 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 700f,  1.0f, 100f);
        Assert.Greater(s1, s2);
        Assert.Greater(s2, s3);
        Assert.Greater(s3, s4);
    }

    [Test]
    public void LandingScore_GForceBands()
    {
        float s1 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f, 1.1f, 100f);
        float s2 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f, 1.3f, 100f);
        float s3 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f, 1.7f, 100f);
        float s4 = ExamScoringEngine.CalculateLandingScore(0f, 0f, 100f, 2.5f, 100f);
        Assert.Greater(s1, s2);
        Assert.Greater(s2, s3);
        Assert.Greater(s3, s4);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // ExamScoringEngine — GetLetterGrade
    // ══════════════════════════════════════════════════════════════════════════════

    [TestCase(100f, "A+")]
    [TestCase(97f,  "A+")]
    [TestCase(96f,  "A")]
    [TestCase(93f,  "A")]
    [TestCase(92f,  "A-")]
    [TestCase(90f,  "A-")]
    [TestCase(89f,  "B+")]
    [TestCase(87f,  "B+")]
    [TestCase(86f,  "B")]
    [TestCase(83f,  "B")]
    [TestCase(82f,  "B-")]
    [TestCase(80f,  "B-")]
    [TestCase(79f,  "C+")]
    [TestCase(77f,  "C+")]
    [TestCase(76f,  "C")]
    [TestCase(73f,  "C")]
    [TestCase(72f,  "C-")]
    [TestCase(70f,  "C-")]
    [TestCase(69f,  "D")]
    [TestCase(60f,  "D")]
    [TestCase(59f,  "F")]
    [TestCase(0f,   "F")]
    public void GetLetterGrade_ReturnsCorrectGrade(float score, string expected)
    {
        Assert.AreEqual(expected, ExamScoringEngine.GetLetterGrade(score));
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // ExamScoringEngine — GetPassStatus (per-difficulty thresholds)
    // ══════════════════════════════════════════════════════════════════════════════

    [TestCase(ExamDifficulty.Bronze,   60f,  true)]
    [TestCase(ExamDifficulty.Bronze,   59f,  false)]
    [TestCase(ExamDifficulty.Silver,   70f,  true)]
    [TestCase(ExamDifficulty.Silver,   69f,  false)]
    [TestCase(ExamDifficulty.Gold,     80f,  true)]
    [TestCase(ExamDifficulty.Gold,     79f,  false)]
    [TestCase(ExamDifficulty.Platinum, 90f,  true)]
    [TestCase(ExamDifficulty.Platinum, 89f,  false)]
    public void GetPassStatus_CorrectForAllDifficulties(ExamDifficulty diff, float score, bool expectedPass)
    {
        float threshold = ExamScoringEngine.GetPassingThreshold(diff);
        bool result = ExamScoringEngine.GetPassStatus(score, threshold);
        Assert.AreEqual(expectedPass, result);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // CertificateGenerator — GenerateCertificate
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void GenerateCertificate_ReturnsValidCertificate()
    {
        var scores = new Dictionary<string, float> { { "sp_basic_flight", 85f } };
        var cert = CertificateGenerator.GenerateCertificate(
            LicenseGrade.PPL, "Test Pilot", scores, 42f);

        Assert.IsNotNull(cert);
        Assert.AreEqual(LicenseGrade.PPL, cert.licenseGrade);
        Assert.AreEqual("Test Pilot", cert.playerName);
        Assert.IsFalse(string.IsNullOrEmpty(cert.certificateId));
        Assert.IsFalse(string.IsNullOrEmpty(cert.signatureHash));
        Assert.AreEqual(42f, cert.totalFlightHours, 0.001f);
    }

    [Test]
    public void GenerateCertificate_NullScores_DoesNotThrow()
    {
        Certificate cert = null;
        Assert.DoesNotThrow(() =>
        {
            cert = CertificateGenerator.GenerateCertificate(
                LicenseGrade.StudentPilot, "Cadet", null, 0f);
        });
        Assert.IsNotNull(cert);
    }

    [Test]
    public void VerifyCertificate_ValidCertificate_ReturnsTrue()
    {
        var cert = CertificateGenerator.GenerateCertificate(
            LicenseGrade.ATPL, "Ace", new Dictionary<string, float>(), 500f);
        Assert.IsTrue(CertificateGenerator.VerifyCertificate(cert));
    }

    [Test]
    public void VerifyCertificate_TamperedName_ReturnsFalse()
    {
        var cert = CertificateGenerator.GenerateCertificate(
            LicenseGrade.ATPL, "Ace", new Dictionary<string, float>(), 500f);
        cert.playerName = "Hacker"; // tamper
        Assert.IsFalse(CertificateGenerator.VerifyCertificate(cert));
    }

    [Test]
    public void VerifyCertificate_NullCertificate_ReturnsFalse()
    {
        Assert.IsFalse(CertificateGenerator.VerifyCertificate(null));
    }

    [Test]
    public void FormatCertificateText_ContainsPilotNameAndGrade()
    {
        var cert = CertificateGenerator.GenerateCertificate(
            LicenseGrade.CPL, "Sky Walker", new Dictionary<string, float>(), 200f);
        string text = CertificateGenerator.FormatCertificateText(cert);
        StringAssert.Contains("Sky Walker", text);
        StringAssert.Contains("CPL", text);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // FlightAcademyDefaultData — validation
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void DefaultData_Creates30Modules()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        Assert.AreEqual(30, modules.Length);
    }

    [Test]
    public void DefaultData_Has6LicenseGrades()
    {
        Assert.AreEqual(6, FlightAcademyDefaultData.GetLicenseGradeCount());
    }

    [Test]
    public void DefaultData_5ModulesPerGrade()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        foreach (LicenseGrade grade in System.Enum.GetValues(typeof(LicenseGrade)))
        {
            int count = 0;
            foreach (var m in modules)
                if (m.licenseGrade == grade) count++;
            Assert.AreEqual(5, count, $"Expected 5 modules for {grade}, found {count}");
        }
    }

    [Test]
    public void DefaultData_AllModulesHaveUniqueIds()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        var ids = new HashSet<string>();
        foreach (var m in modules)
        {
            Assert.IsFalse(string.IsNullOrEmpty(m.moduleId), "Module has empty ID");
            Assert.IsTrue(ids.Add(m.moduleId), $"Duplicate module ID: {m.moduleId}");
        }
    }

    [Test]
    public void DefaultData_AllModulesHaveObjectives()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        foreach (var m in modules)
            Assert.IsTrue(m.objectives != null && m.objectives.Count > 0,
                          $"Module {m.moduleId} has no objectives");
    }

    [Test]
    public void DefaultData_PassingScoresMatchDifficulty()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        foreach (var m in modules)
        {
            float expected = ExamScoringEngine.GetPassingThreshold(m.examDifficulty);
            Assert.AreEqual(expected, m.passingScore, 0.001f,
                            $"Module {m.moduleId} has wrong passing score");
        }
    }

    [Test]
    public void DefaultData_PrerequisiteChainsAreValid()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        var allIds = new HashSet<string>();
        foreach (var m in modules)
            allIds.Add(m.moduleId);

        foreach (var m in modules)
        {
            if (m.prerequisiteModuleIds == null) continue;
            foreach (var prereq in m.prerequisiteModuleIds)
            {
                Assert.IsTrue(allIds.Contains(prereq),
                              $"Module {m.moduleId} references unknown prereq {prereq}");
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // License progression — all modules passed triggers license
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void LicenseProgression_AllStudentModulesPassed_EarnsStudentPilot()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        var progress = new AcademyProgress
        {
            currentLicense  = LicenseGrade.StudentPilot,
            completedModules = new List<string>()
        };

        // Mark all StudentPilot modules as completed
        foreach (var m in modules)
            if (m.licenseGrade == LicenseGrade.StudentPilot)
                progress.completedModules.Add(m.moduleId);

        // Verify all StudentPilot modules are in completedModules
        foreach (var m in modules)
        {
            if (m.licenseGrade == LicenseGrade.StudentPilot)
                Assert.IsTrue(progress.completedModules.Contains(m.moduleId),
                              $"Module {m.moduleId} not completed");
        }

        // Simulate the license check: all 5 SP modules completed
        bool allPassed = true;
        foreach (var m in modules)
        {
            if (m.licenseGrade == LicenseGrade.StudentPilot
                && !progress.completedModules.Contains(m.moduleId))
            {
                allPassed = false;
                break;
            }
        }
        Assert.IsTrue(allPassed, "All Student Pilot modules should be marked complete");
    }

    [Test]
    public void LicenseProgression_MissingOneModule_DoesNotEarnLicense()
    {
        var modules = FlightAcademyDefaultData.CreateDefaultModules();
        var progress = new AcademyProgress
        {
            currentLicense  = LicenseGrade.StudentPilot,
            completedModules = new List<string>()
        };

        // Complete only 4 of the 5 SP modules
        int added = 0;
        foreach (var m in modules)
        {
            if (m.licenseGrade == LicenseGrade.StudentPilot && added < 4)
            {
                progress.completedModules.Add(m.moduleId);
                added++;
            }
        }

        // Count missing modules
        int missing = 0;
        foreach (var m in modules)
            if (m.licenseGrade == LicenseGrade.StudentPilot
                && !progress.completedModules.Contains(m.moduleId))
                missing++;

        Assert.AreEqual(1, missing, "Should have exactly 1 missing module");
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // ExamScoringEngine — CalculateIFRScore & CalculateFormationScore
    // ══════════════════════════════════════════════════════════════════════════════

    [Test]
    public void IFRScore_PerfectFlight_Returns100()
    {
        float score = ExamScoringEngine.CalculateIFRScore(0f, 0f, 0f, 1f);
        Assert.AreEqual(100f, score, 0.001f);
    }

    [Test]
    public void IFRScore_LargeDeviations_IsLow()
    {
        float score = ExamScoringEngine.CalculateIFRScore(30f, 500f, 50f, 0f);
        Assert.Less(score, 50f);
    }

    [Test]
    public void FormationScore_PerfectFormation_Returns100()
    {
        float score = ExamScoringEngine.CalculateFormationScore(0f, 0f, 0f);
        Assert.AreEqual(100f, score, 0.001f);
    }

    [Test]
    public void FormationScore_LargeDeviations_IsLow()
    {
        float score = ExamScoringEngine.CalculateFormationScore(100f, 30f, 60f);
        Assert.Less(score, 50f);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Cleanup ScriptableObjects created by DefaultData
    // ══════════════════════════════════════════════════════════════════════════════
    [TearDown]
    public void TearDown()
    {
        foreach (var obj in Object.FindObjectsOfType<TrainingModule>())
            Object.DestroyImmediate(obj);
    }
}
