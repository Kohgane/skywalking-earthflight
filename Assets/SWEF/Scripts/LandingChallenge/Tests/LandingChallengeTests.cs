// LandingChallengeTests.cs — Phase 120: Precision Landing Challenge System
// Comprehensive NUnit EditMode tests (45+ tests).
// Tests cover: enums, config, scoring engine, touchdown analysis, approach analysis,
// grade calculation, progression, leaderboard, challenge scenarios.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.LandingChallenge;

[TestFixture]
public class LandingChallengeTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChallengeType_AllValuesAreDefined()
    {
        var values = (ChallengeType[])Enum.GetValues(typeof(ChallengeType));
        Assert.GreaterOrEqual(values.Length, 9, "At least 9 ChallengeType values required");
        Assert.Contains(ChallengeType.Standard,         values);
        Assert.Contains(ChallengeType.CarrierLanding,   values);
        Assert.Contains(ChallengeType.MountainApproach, values);
        Assert.Contains(ChallengeType.CrosswindLanding, values);
        Assert.Contains(ChallengeType.ShortField,       values);
        Assert.Contains(ChallengeType.WaterLanding,     values);
        Assert.Contains(ChallengeType.NightLanding,     values);
        Assert.Contains(ChallengeType.EmergencyLanding, values);
        Assert.Contains(ChallengeType.FormationLanding, values);
    }

    [Test]
    public void ChallengeType_HasDistinctIntValues()
    {
        var values = (ChallengeType[])Enum.GetValues(typeof(ChallengeType));
        var seen = new HashSet<int>();
        foreach (var v in values)
            Assert.IsTrue(seen.Add((int)v), $"Duplicate int value for {v}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DifficultyLevel enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DifficultyLevel_AllValuesAreDefined()
    {
        var values = (DifficultyLevel[])Enum.GetValues(typeof(DifficultyLevel));
        Assert.GreaterOrEqual(values.Length, 5);
        Assert.Contains(DifficultyLevel.Beginner,     values);
        Assert.Contains(DifficultyLevel.Intermediate, values);
        Assert.Contains(DifficultyLevel.Advanced,     values);
        Assert.Contains(DifficultyLevel.Expert,       values);
        Assert.Contains(DifficultyLevel.Legendary,    values);
    }

    [Test]
    public void DifficultyLevel_OrderIsAscending()
    {
        Assert.Less((int)DifficultyLevel.Beginner,     (int)DifficultyLevel.Intermediate);
        Assert.Less((int)DifficultyLevel.Intermediate, (int)DifficultyLevel.Advanced);
        Assert.Less((int)DifficultyLevel.Advanced,     (int)DifficultyLevel.Expert);
        Assert.Less((int)DifficultyLevel.Expert,       (int)DifficultyLevel.Legendary);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ScoringCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ScoringCategory_AllValuesAreDefined()
    {
        var values = (ScoringCategory[])Enum.GetValues(typeof(ScoringCategory));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(ScoringCategory.CenterlineAccuracy,  values);
        Assert.Contains(ScoringCategory.TouchdownZone,       values);
        Assert.Contains(ScoringCategory.GlideSlopeAdherence, values);
        Assert.Contains(ScoringCategory.SpeedControl,        values);
        Assert.Contains(ScoringCategory.SinkRate,            values);
        Assert.Contains(ScoringCategory.Smoothness,          values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingGrade enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LandingGrade_AllValuesAreDefined()
    {
        var values = (LandingGrade[])Enum.GetValues(typeof(LandingGrade));
        Assert.GreaterOrEqual(values.Length, 6);
        Assert.Contains(LandingGrade.Perfect,   values);
        Assert.Contains(LandingGrade.Excellent, values);
        Assert.Contains(LandingGrade.Good,      values);
        Assert.Contains(LandingGrade.Fair,      values);
        Assert.Contains(LandingGrade.Poor,      values);
        Assert.Contains(LandingGrade.Crash,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeStatus enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChallengeStatus_AllValuesAreDefined()
    {
        var values = (ChallengeStatus[])Enum.GetValues(typeof(ChallengeStatus));
        Assert.GreaterOrEqual(values.Length, 5);
        Assert.Contains(ChallengeStatus.Locked,     values);
        Assert.Contains(ChallengeStatus.Available,  values);
        Assert.Contains(ChallengeStatus.InProgress, values);
        Assert.Contains(ChallengeStatus.Completed,  values);
        Assert.Contains(ChallengeStatus.Failed,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LSOGrade enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LSOGrade_AllValuesAreDefined()
    {
        var values = (LSOGrade[])Enum.GetValues(typeof(LSOGrade));
        Assert.GreaterOrEqual(values.Length, 5);
        Assert.Contains(LSOGrade.OK,       values);
        Assert.Contains(LSOGrade.Fair,     values);
        Assert.Contains(LSOGrade.NoGrade,  values);
        Assert.Contains(LSOGrade.CutPass,  values);
        Assert.Contains(LSOGrade.WaveOff,  values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // WeatherPreset enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void WeatherPreset_AllValuesAreDefined()
    {
        var values = (WeatherPreset[])Enum.GetValues(typeof(WeatherPreset));
        Assert.GreaterOrEqual(values.Length, 10);
        Assert.Contains(WeatherPreset.Clear,        values);
        Assert.Contains(WeatherPreset.Fog,          values);
        Assert.Contains(WeatherPreset.Thunderstorm, values);
        Assert.Contains(WeatherPreset.Crosswind,    values);
        Assert.Contains(WeatherPreset.Blizzard,     values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingGradeCalculator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void GradeCalculator_PerfectScoreReturnsPerfectGrade()
    {
        var grade = LandingGradeCalculator.ComputeGrade(975f, null);
        Assert.AreEqual(LandingGrade.Perfect, grade);
    }

    [Test]
    public void GradeCalculator_ZeroScoreReturnsCrash()
    {
        var grade = LandingGradeCalculator.ComputeGrade(0f, null);
        Assert.AreEqual(LandingGrade.Crash, grade);
    }

    [Test]
    public void GradeCalculator_MidScoreReturnsGoodGrade()
    {
        var grade = LandingGradeCalculator.ComputeGrade(730f, null);
        Assert.AreEqual(LandingGrade.Good, grade);
    }

    [Test]
    public void GradeCalculator_GradeLabelNeverEmpty()
    {
        foreach (LandingGrade g in Enum.GetValues(typeof(LandingGrade)))
        {
            string label = LandingGradeCalculator.GradeLabel(g);
            Assert.IsNotNull(label);
            Assert.IsNotEmpty(label);
        }
    }

    [Test]
    public void GradeCalculator_ApplyDeductions_ReducesScore()
    {
        float reduced = LandingGradeCalculator.ApplyDeductions(800f, 2, false, null);
        Assert.Less(reduced, 800f);
    }

    [Test]
    public void GradeCalculator_ApplyDeductions_NeverBelowZero()
    {
        float reduced = LandingGradeCalculator.ApplyDeductions(0f, 100, true, null);
        Assert.GreaterOrEqual(reduced, 0f);
    }

    [Test]
    public void GradeCalculator_ApplyBonuses_IncreasesScore()
    {
        float bonus = LandingGradeCalculator.ApplyBonuses(700f, true, true, false, 0.5f, null);
        Assert.Greater(bonus, 700f);
    }

    [Test]
    public void GradeCalculator_ApplyBonuses_NeverExceeds1000()
    {
        float bonus = LandingGradeCalculator.ApplyBonuses(999f, true, true, true, 1f, null);
        Assert.LessOrEqual(bonus, 1000f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeDefinition data model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChallengeDefinition_CanBeCreated()
    {
        var def = new ChallengeDefinition
        {
            ChallengeId = "test_01",
            DisplayName = "Test Challenge",
            Type        = ChallengeType.Standard,
            Difficulty  = DifficultyLevel.Beginner
        };
        Assert.AreEqual("test_01",  def.ChallengeId);
        Assert.AreEqual(ChallengeType.Standard, def.Type);
    }

    [Test]
    public void ChallengeDefinition_StarThresholds_DefaultLength()
    {
        var def = new ChallengeDefinition();
        Assert.IsNotNull(def.StarThresholds);
        Assert.AreEqual(3, def.StarThresholds.Length);
    }

    [Test]
    public void ChallengeDefinition_Prerequisites_InitialisedEmpty()
    {
        var def = new ChallengeDefinition();
        Assert.IsNotNull(def.Prerequisites);
        Assert.AreEqual(0, def.Prerequisites.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TouchdownData model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TouchdownData_DefaultValues_AreSensible()
    {
        var td = new TouchdownData();
        Assert.AreEqual(0f, td.SpeedKnots);
        Assert.AreEqual(0f, td.VerticalSpeedFPM);
        Assert.AreEqual(0f, td.GForce);
    }

    [Test]
    public void TouchdownData_CanSetFields()
    {
        var td = new TouchdownData
        {
            SpeedKnots           = 137f,
            VerticalSpeedFPM     = -220f,
            GForce               = 1.1f,
            BankAngleDeg         = 0.5f,
            CrabAngleDeg         = 2f,
            CentrelineOffsetMetres = 1.2f,
            ThresholdDistanceMetres = 450f
        };
        Assert.AreEqual(137f, td.SpeedKnots);
        Assert.AreEqual(-220f, td.VerticalSpeedFPM);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ApproachSnapshot model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ApproachSnapshot_CanBeCreated()
    {
        var snap = new ApproachSnapshot
        {
            GlideSlopeDots   = 0.2f,
            LocaliserDots    = -0.1f,
            SpeedKnots       = 140f,
            TargetSpeedKnots = 137f,
            GearDown         = true,
            FlapSetting      = 3,
            AltitudeFeet     = 1500f
        };
        Assert.AreEqual(0.2f, snap.GlideSlopeDots);
        Assert.IsTrue(snap.GearDown);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingResult model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LandingResult_CategoryScores_InitialisedNotNull()
    {
        var result = new LandingResult();
        Assert.IsNotNull(result.CategoryScores);
    }

    [Test]
    public void LandingResult_StarsAreWithinRange()
    {
        var result = new LandingResult { Stars = 3 };
        Assert.GreaterOrEqual(result.Stars, 0);
        Assert.LessOrEqual(result.Stars, 3);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeProgress model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChallengeProgress_CanBeCreated()
    {
        var p = new ChallengeProgress
        {
            ChallengeId  = "test_01",
            Status       = ChallengeStatus.Completed,
            BestScore    = 875f,
            StarsEarned  = 2,
            AttemptCount = 3
        };
        Assert.AreEqual("test_01",             p.ChallengeId);
        Assert.AreEqual(ChallengeStatus.Completed, p.Status);
        Assert.AreEqual(2, p.StarsEarned);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LeaderboardEntry model
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LeaderboardEntry_FromResult_CreatesValidEntry()
    {
        var result = new LandingResult
        {
            TotalScore  = 920f,
            Grade       = LandingGrade.Excellent,
            Stars       = 3,
            Timestamp   = DateTime.UtcNow
        };
        var entry = LeaderboardEntry.FromResult("player1", "Ace Pilot", "std_jfk_22r",
                                                 result, "B737", WeatherPreset.Clear);
        Assert.IsNotNull(entry);
        Assert.AreEqual("player1",           entry.PlayerId);
        Assert.AreEqual(920f,                entry.Score);
        Assert.AreEqual(LandingGrade.Excellent, entry.Grade);
        Assert.IsNotNull(entry.EntryId);
        Assert.IsNotEmpty(entry.EntryId);
    }

    [Test]
    public void LeaderboardEntry_FromResult_HasNonEmptyEntryId()
    {
        var result = new LandingResult { TotalScore = 700f, Timestamp = DateTime.UtcNow };
        var e1 = LeaderboardEntry.FromResult("p1", "P1", "c1", result, "B737", WeatherPreset.Clear);
        var e2 = LeaderboardEntry.FromResult("p2", "P2", "c1", result, "B737", WeatherPreset.Clear);
        Assert.AreNotEqual(e1.EntryId, e2.EntryId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ApproachAnalyzer
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ApproachAnalyzer_EmptySnapshots_ReturnsDefaultReport()
    {
        var go       = new GameObject("Analyzer");
        var analyzer = go.AddComponent<ApproachAnalyzer>();
        var report   = analyzer.Analyse(new List<ApproachSnapshot>());
        Assert.AreEqual(0f, report.AverageGlideSlopeDevDots);
        Assert.AreEqual(0f, report.AverageSpeedDeviationKnots);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ApproachAnalyzer_OnGlideslopeSnapshots_ReturnsHighQuality()
    {
        var go       = new GameObject("Analyzer");
        var analyzer = go.AddComponent<ApproachAnalyzer>();
        var snaps    = new List<ApproachSnapshot>();
        for (int i = 0; i < 10; i++)
            snaps.Add(new ApproachSnapshot { GlideSlopeDots = 0.1f, LocaliserDots = 0.05f, SpeedKnots = 137f, TargetSpeedKnots = 137f, GearDown = true, FlapSetting = 3 });
        var report = analyzer.Analyse(snaps);
        Assert.Greater(report.OverallQuality, 0.7f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ApproachAnalyzer_GearAlwaysDown_GearFractionIsOne()
    {
        var go       = new GameObject("Analyzer");
        var analyzer = go.AddComponent<ApproachAnalyzer>();
        var snaps    = new List<ApproachSnapshot>
        {
            new ApproachSnapshot { GearDown = true, TargetSpeedKnots = 137f },
            new ApproachSnapshot { GearDown = true, TargetSpeedKnots = 137f }
        };
        var report = analyzer.Analyse(snaps);
        Assert.AreEqual(1f, report.GearDownFraction);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingProgressionSystem
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProgressionSystem_RecordStars_UpdatesTotalStars()
    {
        var go  = new GameObject("Progression");
        var sys = go.AddComponent<LandingProgressionSystem>();
        sys.RecordStars("c1", 3);
        Assert.AreEqual(3, sys.TotalStars);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ProgressionSystem_RecordLowerStars_DoesNotDecrement()
    {
        var go  = new GameObject("Progression");
        var sys = go.AddComponent<LandingProgressionSystem>();
        sys.RecordStars("c1", 3);
        sys.RecordStars("c1", 1); // lower score — should not change
        Assert.AreEqual(3, sys.GetStars("c1"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ProgressionSystem_MasteryBadge_AwardedOn3Stars()
    {
        var go  = new GameObject("Progression");
        var sys = go.AddComponent<LandingProgressionSystem>();
        bool badgeEarned = false;
        sys.OnMasteryBadgeEarned += _ => badgeEarned = true;
        sys.RecordStars("c1", 3);
        Assert.IsTrue(badgeEarned);
        Assert.IsTrue(sys.HasMasteryBadge("c1"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ProgressionSystem_MasteryBadge_NotAwardedOn2Stars()
    {
        var go  = new GameObject("Progression");
        var sys = go.AddComponent<LandingProgressionSystem>();
        sys.RecordStars("c1", 2);
        Assert.IsFalse(sys.HasMasteryBadge("c1"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingLeaderboardManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LeaderboardManager_SubmitEntry_AppearsInChallengeBoard()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        var entry   = MakeSampleEntry("p1", "c1", 900f);
        manager.SubmitEntry(entry, "KJFK");
        var board = manager.GetChallengeBoard("c1", 10);
        Assert.AreEqual(1, board.Count);
        Assert.AreEqual("p1", board[0].PlayerId);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_HigherScoreReplacesPrevious()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 700f), "KJFK");
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 900f), "KJFK");
        var board = manager.GetChallengeBoard("c1", 10);
        Assert.AreEqual(1, board.Count);
        Assert.AreEqual(900f, board[0].Score);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_LowerScoreDoesNotReplace()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 900f), "KJFK");
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 700f), "KJFK");
        var board = manager.GetChallengeBoard("c1", 10);
        Assert.AreEqual(900f, board[0].Score);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_MultiplePlayersRankedCorrectly()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 700f), "KJFK");
        manager.SubmitEntry(MakeSampleEntry("p2", "c1", 900f), "KJFK");
        manager.SubmitEntry(MakeSampleEntry("p3", "c1", 850f), "KJFK");
        var board = manager.GetChallengeBoard("c1", 10);
        Assert.AreEqual("p2", board[0].PlayerId);
        Assert.AreEqual("p3", board[1].PlayerId);
        Assert.AreEqual("p1", board[2].PlayerId);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_GetPlayerRank_ReturnsCorrectRank()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        manager.SubmitEntry(MakeSampleEntry("p1", "c1", 900f), "KJFK");
        manager.SubmitEntry(MakeSampleEntry("p2", "c1", 800f), "KJFK");
        Assert.AreEqual(1, manager.GetPlayerRank("c1", "p1"));
        Assert.AreEqual(2, manager.GetPlayerRank("c1", "p2"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LeaderboardManager_GetPlayerRank_ReturnsMinusOneForUnknown()
    {
        var go      = new GameObject("Leaderboard");
        var manager = go.AddComponent<LandingLeaderboardManager>();
        Assert.AreEqual(-1, manager.GetPlayerRank("c1", "nobody"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SeasonalLeaderboard
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SeasonalLeaderboard_RecordScore_UpdatesEntries()
    {
        var go  = new GameObject("Seasonal");
        var sl  = go.AddComponent<SeasonalLeaderboard>();
        sl.RecordScore("p1", "Player1", 500f);
        var top = sl.GetTopEntries(10);
        Assert.AreEqual(1, top.Count);
        Assert.AreEqual("p1", top[0].PlayerId);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SeasonalLeaderboard_TierPromotion_Fired()
    {
        var go      = new GameObject("Seasonal");
        var sl      = go.AddComponent<SeasonalLeaderboard>();
        bool promoted = false;
        sl.OnTierPromotion += (_, __) => promoted = true;
        sl.RecordScore("p1", "P1", 500f);   // Bronze
        sl.RecordScore("p1", "P1", 500f);   // → Silver (1000 total > 500 threshold)
        Assert.IsTrue(promoted);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CrosswindLandingChallenge
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CrosswindChallenge_Activate_SetsIsActiveTrue()
    {
        var go   = new GameObject("Crosswind");
        var chg  = go.AddComponent<CrosswindLandingChallenge>();
        chg.Activate();
        Assert.IsTrue(chg.IsActive);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CrosswindChallenge_Deactivate_SetsIsActiveFalse()
    {
        var go  = new GameObject("Crosswind");
        var chg = go.AddComponent<CrosswindLandingChallenge>();
        chg.Activate();
        chg.Deactivate();
        Assert.IsFalse(chg.IsActive);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MountainLandingChallenge
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void MountainChallenge_DensityAltitude_IsAboveAirportElevation()
    {
        var go  = new GameObject("Mountain");
        var chg = go.AddComponent<MountainLandingChallenge>();
        chg.Activate();
        Assert.Greater(chg.DensityAltitudeFeet, 0f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void MountainChallenge_PerformancePenalty_IsBetweenZeroAndOne()
    {
        var go  = new GameObject("Mountain");
        var chg = go.AddComponent<MountainLandingChallenge>();
        chg.Activate();
        float penalty = chg.GetPerformancePenalty();
        Assert.GreaterOrEqual(penalty, 0f);
        Assert.LessOrEqual(penalty, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ShortFieldChallenge
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ShortFieldChallenge_Activate_ResetsState()
    {
        var go  = new GameObject("ShortField");
        var chg = go.AddComponent<ShortFieldChallenge>();
        chg.Activate();
        Assert.IsTrue(chg.IsActive);
        Assert.IsFalse(chg.ObstacleCleared);
        Assert.IsFalse(chg.RunwayExceeded);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ShortFieldChallenge_PerformanceScore_IsBetweenZeroAndOne()
    {
        var go  = new GameObject("ShortField");
        var chg = go.AddComponent<ShortFieldChallenge>();
        chg.Activate();
        chg.RecordStop(60f, 250f);
        float score = chg.CalculatePerformanceScore(60f);
        Assert.GreaterOrEqual(score, 0f);
        Assert.LessOrEqual(score, 1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExtremeLandingChallenge
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ExtremeChallenge_IceRunway_ReducesBrakingFriction()
    {
        var go  = new GameObject("Extreme");
        var chg = go.AddComponent<ExtremeLandingChallenge>();
        chg.Activate(ExtremeLandingChallenge.HazardFlags.IceRunway);
        Assert.Less(chg.BrakingFriction, 0.1f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ExtremeChallenge_MoreHazards_HigherDifficultyMultiplier()
    {
        var go    = new GameObject("Extreme");
        var chg1  = go.AddComponent<ExtremeLandingChallenge>();
        chg1.Activate(ExtremeLandingChallenge.HazardFlags.IceRunway);
        float mult1 = chg1.GetDifficultyMultiplier();

        var go2   = new GameObject("Extreme2");
        var chg2  = go2.AddComponent<ExtremeLandingChallenge>();
        chg2.Activate(ExtremeLandingChallenge.HazardFlags.IceRunway |
                      ExtremeLandingChallenge.HazardFlags.SevereTurbulence |
                      ExtremeLandingChallenge.HazardFlags.NoInstruments);
        float mult2 = chg2.GetDifficultyMultiplier();

        Assert.Greater(mult2, mult1);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(go2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingChallengeAnalytics
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Analytics_TrackAttempts_IncrementsTotalAttempts()
    {
        var go  = new GameObject("Analytics");
        var an  = go.AddComponent<LandingChallengeAnalytics>();
        an.TrackChallengeStarted("c1", ChallengeType.Standard, DifficultyLevel.Beginner);
        an.TrackChallengeStarted("c1", ChallengeType.Standard, DifficultyLevel.Beginner);
        Assert.AreEqual(2, an.TotalAttempts);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Analytics_CompletionRate_ZeroWithNoAttempts()
    {
        var go  = new GameObject("Analytics");
        var an  = go.AddComponent<LandingChallengeAnalytics>();
        Assert.AreEqual(0f, an.CompletionRate);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void Analytics_GetMostPopularChallenge_ReturnsHighestAttempt()
    {
        var go  = new GameObject("Analytics");
        var an  = go.AddComponent<LandingChallengeAnalytics>();
        an.TrackChallengeStarted("c1", ChallengeType.Standard, DifficultyLevel.Beginner);
        an.TrackChallengeStarted("c2", ChallengeType.CarrierLanding, DifficultyLevel.Expert);
        an.TrackChallengeStarted("c2", ChallengeType.CarrierLanding, DifficultyLevel.Expert);
        Assert.AreEqual("c2", an.GetMostPopularChallenge());
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingHUD
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LandingHUD_OnGlideslope_PAPIIndicatesOnGlideslope()
    {
        var go  = new GameObject("HUD");
        var hud = go.AddComponent<LandingHUD>();
        hud.Show();
        hud.UpdateData(0f, 0f, 137f, 1000f, 5f);
        Assert.AreEqual(LandingHUD.PAPIIndication.OnGlideslope, hud.PAPI);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void LandingHUD_AboveGlideslope_PAPIIndicatesTooHigh()
    {
        var go  = new GameObject("HUD");
        var hud = go.AddComponent<LandingHUD>();
        hud.Show();
        hud.UpdateData(2f, 0f, 137f, 1000f, 5f);
        Assert.AreEqual(LandingHUD.PAPIIndication.TooHigh, hud.PAPI);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingTutorialController
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TutorialController_Activate_SetsIsActive()
    {
        var go   = new GameObject("Tutorial");
        var ctrl = go.AddComponent<LandingTutorialController>();
        ctrl.Activate();
        Assert.IsTrue(ctrl.IsActive);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void TutorialController_FirstStep_IsNotNull()
    {
        var go   = new GameObject("Tutorial");
        var ctrl = go.AddComponent<LandingTutorialController>();
        ctrl.Activate();
        Assert.IsNotNull(ctrl.CurrentStep);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void TutorialController_SpeedHint_ReturnedWhenFarOffTarget()
    {
        var go   = new GameObject("Tutorial");
        var ctrl = go.AddComponent<LandingTutorialController>();
        ctrl.Activate();
        string hint = ctrl.GetSpeedHint(200f);
        Assert.IsNotNull(hint);
        Assert.IsNotEmpty(hint);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ReplayRecorder
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ReplayRecorder_StartRecording_SetsIsRecordingTrue()
    {
        var go  = new GameObject("Recorder");
        var rec = go.AddComponent<ReplayRecorder>();
        rec.StartRecording();
        Assert.IsTrue(rec.IsRecording);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void ReplayRecorder_StopAndSave_InitiallySavedFrameCountZero()
    {
        var go  = new GameObject("Recorder");
        var rec = go.AddComponent<ReplayRecorder>();
        rec.StartRecording();
        rec.StopAndSave();
        Assert.IsFalse(rec.IsRecording);
        Assert.AreEqual(0, rec.SavedFrameCount); // no frames captured without Update
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DailyLandingChallenge
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DailyChallenge_Initialise_SetsTodaysChallenge()
    {
        var go   = new GameObject("Daily");
        var daily = go.AddComponent<DailyLandingChallenge>();
        var pool  = new List<ChallengeDefinition>
        {
            new ChallengeDefinition { ChallengeId = "d1", DisplayName = "D1" },
            new ChallengeDefinition { ChallengeId = "d2", DisplayName = "D2" }
        };
        daily.Initialise(pool);
        Assert.IsNotNull(daily.TodaysChallenge);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void DailyChallenge_NextResetUTC_IsInFuture()
    {
        var go   = new GameObject("Daily");
        var daily = go.AddComponent<DailyLandingChallenge>();
        Assert.GreaterOrEqual(daily.NextResetUTC, DateTime.UtcNow);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeAircraftRestrictor
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AircraftRestrictor_NoRule_AllowsAll()
    {
        var go   = new GameObject("Restrictor");
        var rest = go.AddComponent<ChallengeAircraftRestrictor>();
        bool allowed = rest.IsAllowed("any_challenge", "B737", ChallengeAircraftRestrictor.AircraftCategory.Narrowbody);
        Assert.IsTrue(allowed);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void AircraftRestrictor_DefaultHandicap_GreaterForHeavier()
    {
        var go   = new GameObject("Restrictor");
        var rest = go.AddComponent<ChallengeAircraftRestrictor>();
        float ga     = rest.GetHandicapMultiplier("any", ChallengeAircraftRestrictor.AircraftCategory.GA_Single);
        float carrier = rest.GetHandicapMultiplier("any", ChallengeAircraftRestrictor.AircraftCategory.Carrier);
        Assert.Greater(carrier, ga);
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LandingCoachSystem
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CoachSystem_Activate_SetsIsActiveTrue()
    {
        var go    = new GameObject("Coach");
        var coach = go.AddComponent<LandingCoachSystem>();
        coach.Activate();
        Assert.IsTrue(coach.IsActive);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CoachSystem_PostLandingTips_PerfectGradeIncludesPositiveTip()
    {
        var go    = new GameObject("Coach");
        var coach = go.AddComponent<LandingCoachSystem>();
        coach.Activate();
        var result = new LandingResult { Grade = LandingGrade.Perfect, CenterlineDeviationMetres = 0.5f, SinkRateFPM = -100f, BounceCount = 0 };
        var tips = coach.GeneratePostLandingTips(result);
        Assert.IsTrue(tips.Exists(t => t.Contains("Outstanding")));
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════════════════

    private static LeaderboardEntry MakeSampleEntry(string playerId, string challengeId, float score)
    {
        var result = new LandingResult { TotalScore = score, Grade = LandingGrade.Good, Stars = 2, Timestamp = DateTime.UtcNow };
        return LeaderboardEntry.FromResult(playerId, playerId, challengeId, result, "B737", WeatherPreset.Clear);
    }
}
