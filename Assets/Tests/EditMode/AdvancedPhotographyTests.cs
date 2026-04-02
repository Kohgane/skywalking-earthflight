// AdvancedPhotographyTests.cs — NUnit EditMode tests for Phase 89 Advanced Photography & Drone Camera System
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SWEF.AdvancedPhotography;

[TestFixture]
public class AdvancedPhotographyTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // DroneFlightMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DroneFlightMode_HasEightValues()
    {
        Assert.AreEqual(8, Enum.GetValues(typeof(DroneFlightMode)).Length,
            "DroneFlightMode must have exactly 8 values as per the Phase 89 spec.");
    }

    [Test]
    public void DroneFlightMode_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.FreeRoam));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Orbit));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Flyby));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Follow));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Waypoint));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Tracking));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.Cinematic));
        Assert.IsTrue(Enum.IsDefined(typeof(DroneFlightMode), DroneFlightMode.ReturnHome));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CompositionRule enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CompositionRule_HasSevenValues()
    {
        Assert.AreEqual(7, Enum.GetValues(typeof(CompositionRule)).Length);
    }

    [Test]
    public void CompositionRule_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.RuleOfThirds));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.GoldenRatio));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.Symmetry));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.LeadingLines));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.FrameWithinFrame));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.DiagonalMethod));
        Assert.IsTrue(Enum.IsDefined(typeof(CompositionRule), CompositionRule.CenterWeighted));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PhotoSubject enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PhotoSubject_HasTenValues()
    {
        Assert.AreEqual(10, Enum.GetValues(typeof(PhotoSubject)).Length);
    }

    [Test]
    public void PhotoSubject_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Landscape));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Landmark));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Aircraft));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Wildlife));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Weather));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Celestial));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Urban));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Nature));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Action));
        Assert.IsTrue(Enum.IsDefined(typeof(PhotoSubject), PhotoSubject.Abstract));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ChallengeCategory enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ChallengeCategory_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(ChallengeCategory)).Length);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PhotoRating enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PhotoRating_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(PhotoRating)).Length);
    }

    [Test]
    public void PhotoRating_ValuesMatchStarNumbers()
    {
        Assert.AreEqual(1, (int)PhotoRating.OneStar);
        Assert.AreEqual(2, (int)PhotoRating.TwoStar);
        Assert.AreEqual(3, (int)PhotoRating.ThreeStar);
        Assert.AreEqual(4, (int)PhotoRating.FourStar);
        Assert.AreEqual(5, (int)PhotoRating.FiveStar);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PanoramaType enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PanoramaType_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(PanoramaType)).Length);
    }

    [Test]
    public void PanoramaType_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(PanoramaType), PanoramaType.Horizontal));
        Assert.IsTrue(Enum.IsDefined(typeof(PanoramaType), PanoramaType.Vertical));
        Assert.IsTrue(Enum.IsDefined(typeof(PanoramaType), PanoramaType.Full360));
        Assert.IsTrue(Enum.IsDefined(typeof(PanoramaType), PanoramaType.LittlePlanet));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TimelapseMode enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TimelapseMode_HasFiveValues()
    {
        Assert.AreEqual(5, Enum.GetValues(typeof(TimelapseMode)).Length);
    }

    [Test]
    public void TimelapseMode_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(TimelapseMode), TimelapseMode.TimeInterval));
        Assert.IsTrue(Enum.IsDefined(typeof(TimelapseMode), TimelapseMode.DistanceInterval));
        Assert.IsTrue(Enum.IsDefined(typeof(TimelapseMode), TimelapseMode.SunTracking));
        Assert.IsTrue(Enum.IsDefined(typeof(TimelapseMode), TimelapseMode.WeatherChange));
        Assert.IsTrue(Enum.IsDefined(typeof(TimelapseMode), TimelapseMode.DayNightCycle));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AIAssistLevel enum
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AIAssistLevel_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(AIAssistLevel)).Length);
    }

    [Test]
    public void AIAssistLevel_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(AIAssistLevel), AIAssistLevel.Off));
        Assert.IsTrue(Enum.IsDefined(typeof(AIAssistLevel), AIAssistLevel.Suggestions));
        Assert.IsTrue(Enum.IsDefined(typeof(AIAssistLevel), AIAssistLevel.AutoFrame));
        Assert.IsTrue(Enum.IsDefined(typeof(AIAssistLevel), AIAssistLevel.FullAuto));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AdvancedPhotographyConfig constants
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void Config_DroneMaxRangeIsPositive()
    {
        Assert.Greater(AdvancedPhotographyConfig.DroneMaxRange, 0f);
    }

    [Test]
    public void Config_DroneMaxAltitudeIsPositive()
    {
        Assert.Greater(AdvancedPhotographyConfig.DroneMaxAltitude, 0f);
    }

    [Test]
    public void Config_DroneBatteryDurationIsPositive()
    {
        Assert.Greater(AdvancedPhotographyConfig.DroneBatteryDuration, 0f);
    }

    [Test]
    public void Config_DroneLowBatteryThresholdIsValidProbability()
    {
        Assert.GreaterOrEqual(AdvancedPhotographyConfig.DroneLowBatteryThreshold, 0f);
        Assert.LessOrEqual(AdvancedPhotographyConfig.DroneLowBatteryThreshold, 1f);
    }

    [Test]
    public void Config_AICompositionThresholdsAreOrdered()
    {
        Assert.Less(
            AdvancedPhotographyConfig.AICompositionGoodThreshold,
            AdvancedPhotographyConfig.AICompositionExcellentThreshold,
            "Good threshold must be less than Excellent threshold.");
    }

    [Test]
    public void Config_TimelapsIntervalOrderIsValid()
    {
        Assert.Less(
            AdvancedPhotographyConfig.TimelapseMinInterval,
            AdvancedPhotographyConfig.TimelapseMaxInterval,
            "Min timelapse interval must be less than max.");
    }

    [Test]
    public void Config_PanoramaFaceResolutionIsPositive()
    {
        Assert.Greater(AdvancedPhotographyConfig.PanoramaDefaultFaceResolution, 0);
    }

    [Test]
    public void Config_PanoramaOverlapPercentIsValid()
    {
        Assert.GreaterOrEqual(AdvancedPhotographyConfig.PanoramaOverlapPercent, 0f);
        Assert.Less(AdvancedPhotographyConfig.PanoramaOverlapPercent, 100f);
    }

    [Test]
    public void Config_PhotoSpotDiscoveryRadiusIsPositive()
    {
        Assert.Greater(AdvancedPhotographyConfig.PhotoSpotDiscoveryRadius, 0f);
    }

    [Test]
    public void Config_ContestLeaderboardPageSizeIsAtLeastOne()
    {
        Assert.GreaterOrEqual(AdvancedPhotographyConfig.ContestLeaderboardPageSize, 1);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DroneWaypoint / DroneFlightPath data classes
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void DroneWaypoint_DefaultSpeedIsPositive()
    {
        var wp = new DroneWaypoint();
        Assert.Greater(wp.speed, 0f);
    }

    [Test]
    public void DroneWaypoint_DefaultHoldTimeIsNonNegative()
    {
        var wp = new DroneWaypoint();
        Assert.GreaterOrEqual(wp.holdTime, 0f);
    }

    [Test]
    public void DroneFlightPath_DefaultsToEmptyAndNoLoop()
    {
        var path = new DroneFlightPath();
        Assert.IsNotNull(path.waypoints);
        Assert.AreEqual(0, path.waypoints.Count);
        Assert.IsFalse(path.loop);
    }

    [Test]
    public void DroneFlightPath_CanAddWaypoints()
    {
        var path = new DroneFlightPath();
        path.waypoints.Add(new DroneWaypoint { position = Vector3.zero });
        path.waypoints.Add(new DroneWaypoint { position = Vector3.one  });
        Assert.AreEqual(2, path.waypoints.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CompositionAnalysis
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CompositionAnalysis_DefaultScoreIsZero()
    {
        var analysis = new CompositionAnalysis();
        Assert.AreEqual(0f, analysis.score);
    }

    [Test]
    public void CompositionAnalysis_DefaultSuggestionIsNotNull()
    {
        var analysis = new CompositionAnalysis();
        Assert.IsNotNull(analysis.suggestion);
    }

    [Test]
    public void CompositionAnalysis_GuidePointsNotNull()
    {
        var analysis = new CompositionAnalysis();
        Assert.IsNotNull(analysis.guidePoints);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PhotoMetadata
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PhotoMetadata_DefaultSubjectsListNotNull()
    {
        var meta = new PhotoMetadata();
        Assert.IsNotNull(meta.subjects);
    }

    [Test]
    public void PhotoMetadata_CompositionScoreDefaultsToZero()
    {
        var meta = new PhotoMetadata();
        Assert.AreEqual(0f, meta.compositionScore);
    }

    [Test]
    public void PhotoMetadata_FieldOfViewDefaultIsPositive()
    {
        var meta = new PhotoMetadata();
        Assert.Greater(meta.fieldOfView, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PhotoSpot
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void PhotoSpot_DefaultsToNotDiscovered()
    {
        var spot = new PhotoSpot();
        Assert.IsFalse(spot.discovered);
    }

    [Test]
    public void PhotoSpot_RecommendedSubjectsNotNull()
    {
        var spot = new PhotoSpot();
        Assert.IsNotNull(spot.recommendedSubjects);
    }

    [Test]
    public void PhotoSpot_DifficultyDefaultIsWithinRange()
    {
        var spot = new PhotoSpot();
        Assert.GreaterOrEqual(spot.difficulty, 1);
        Assert.LessOrEqual(spot.difficulty, 5);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TimelapseConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TimelapseConfig_DefaultIntervalMatchesConfig()
    {
        var cfg = new TimelapseConfig();
        Assert.AreEqual(AdvancedPhotographyConfig.TimelapseDefaultInterval, cfg.timeInterval);
    }

    [Test]
    public void TimelapseConfig_DefaultDistanceIntervalMatchesConfig()
    {
        var cfg = new TimelapseConfig();
        Assert.AreEqual(AdvancedPhotographyConfig.TimelapseDefaultDistanceInterval, cfg.distanceInterval);
    }

    [Test]
    public void TimelapseConfig_DefaultModeIsTimeInterval()
    {
        var cfg = new TimelapseConfig();
        Assert.AreEqual(TimelapseMode.TimeInterval, cfg.mode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ContestState enum (defined inside PhotoContestManager.cs)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ContestState_HasFourValues()
    {
        Assert.AreEqual(4, Enum.GetValues(typeof(ContestState)).Length);
    }

    [Test]
    public void ContestState_ContainsAllExpectedValues()
    {
        Assert.IsTrue(Enum.IsDefined(typeof(ContestState), ContestState.Upcoming));
        Assert.IsTrue(Enum.IsDefined(typeof(ContestState), ContestState.Active));
        Assert.IsTrue(Enum.IsDefined(typeof(ContestState), ContestState.Judging));
        Assert.IsTrue(Enum.IsDefined(typeof(ContestState), ContestState.Complete));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ContestSubmission
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ContestSubmission_DefaultVoteCountIsZero()
    {
        var sub = new ContestSubmission();
        Assert.AreEqual(0, sub.voteCount);
    }

    [Test]
    public void ContestSubmission_DefaultAIScoreIsZero()
    {
        var sub = new ContestSubmission();
        Assert.AreEqual(0f, sub.aiScore);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ActiveContest
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ActiveContest_DefaultStateIsUpcoming()
    {
        var contest = new ActiveContest();
        Assert.AreEqual(ContestState.Upcoming, contest.state);
    }

    [Test]
    public void ActiveContest_SubmissionsListNotNull()
    {
        var contest = new ActiveContest();
        Assert.IsNotNull(contest.submissions);
    }
}
