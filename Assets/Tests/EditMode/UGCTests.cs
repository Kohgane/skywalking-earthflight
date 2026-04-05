// UGCTests.cs — NUnit EditMode tests for Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SWEF.UGC;

[TestFixture]
public class UGCTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // UGCEnums
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCContentType_AllValuesAreDefined()
    {
        var values = (UGCContentType[])Enum.GetValues(typeof(UGCContentType));
        Assert.GreaterOrEqual(values.Length, 8);
        Assert.Contains(UGCContentType.Tour,         values);
        Assert.Contains(UGCContentType.Mission,      values);
        Assert.Contains(UGCContentType.RaceCourse,   values);
        Assert.Contains(UGCContentType.Scenario,     values);
        Assert.Contains(UGCContentType.Challenge,    values);
        Assert.Contains(UGCContentType.PhotoSpot,    values);
        Assert.Contains(UGCContentType.WaypointPack, values);
        Assert.Contains(UGCContentType.FlightRoute,  values);
    }

    [Test]
    public void UGCStatus_AllValuesAreDefined()
    {
        var values = (UGCStatus[])Enum.GetValues(typeof(UGCStatus));
        Assert.Contains(UGCStatus.Draft,        values);
        Assert.Contains(UGCStatus.UnderReview,  values);
        Assert.Contains(UGCStatus.Published,    values);
        Assert.Contains(UGCStatus.Rejected,     values);
        Assert.Contains(UGCStatus.Archived,     values);
        Assert.Contains(UGCStatus.Featured,     values);
    }

    [Test]
    public void UGCDifficulty_AllValuesAreDefined()
    {
        var values = (UGCDifficulty[])Enum.GetValues(typeof(UGCDifficulty));
        Assert.Contains(UGCDifficulty.Beginner,     values);
        Assert.Contains(UGCDifficulty.Intermediate, values);
        Assert.Contains(UGCDifficulty.Advanced,     values);
        Assert.Contains(UGCDifficulty.Expert,       values);
        Assert.Contains(UGCDifficulty.Extreme,      values);
    }

    [Test]
    public void UGCCategory_AllValuesAreDefined()
    {
        var values = (UGCCategory[])Enum.GetValues(typeof(UGCCategory));
        Assert.Contains(UGCCategory.Sightseeing,  values);
        Assert.Contains(UGCCategory.Adventure,    values);
        Assert.Contains(UGCCategory.Education,    values);
        Assert.Contains(UGCCategory.Competition,  values);
        Assert.Contains(UGCCategory.Relaxation,   values);
        Assert.Contains(UGCCategory.Exploration,  values);
        Assert.Contains(UGCCategory.Historical,   values);
        Assert.Contains(UGCCategory.SciFi,        values);
    }

    [Test]
    public void EditorTool_AllValuesAreDefined()
    {
        var values = (EditorTool[])Enum.GetValues(typeof(EditorTool));
        Assert.GreaterOrEqual(values.Length, 10);
        Assert.Contains(EditorTool.Select,  values);
        Assert.Contains(EditorTool.Place,   values);
        Assert.Contains(EditorTool.Move,    values);
        Assert.Contains(EditorTool.Rotate,  values);
        Assert.Contains(EditorTool.Scale,   values);
        Assert.Contains(EditorTool.Delete,  values);
        Assert.Contains(EditorTool.Path,    values);
        Assert.Contains(EditorTool.Zone,    values);
        Assert.Contains(EditorTool.Trigger, values);
        Assert.Contains(EditorTool.Text,    values);
    }

    [Test]
    public void ValidationSeverity_AllValuesAreDefined()
    {
        var values = (ValidationSeverity[])Enum.GetValues(typeof(ValidationSeverity));
        Assert.Contains(ValidationSeverity.Info,     values);
        Assert.Contains(ValidationSeverity.Warning,  values);
        Assert.Contains(ValidationSeverity.Error,    values);
        Assert.Contains(ValidationSeverity.Critical, values);
    }

    [Test]
    public void UGCRating_StarValuesAreCorrect()
    {
        Assert.AreEqual(1, (int)UGCRating.OneStar);
        Assert.AreEqual(2, (int)UGCRating.TwoStar);
        Assert.AreEqual(3, (int)UGCRating.ThreeStar);
        Assert.AreEqual(4, (int)UGCRating.FourStar);
        Assert.AreEqual(5, (int)UGCRating.FiveStar);
    }

    [Test]
    public void UGCTriggerType_AllValuesAreDefined()
    {
        var values = (UGCTriggerType[])Enum.GetValues(typeof(UGCTriggerType));
        Assert.Contains(UGCTriggerType.EnterZone,       values);
        Assert.Contains(UGCTriggerType.ExitZone,        values);
        Assert.Contains(UGCTriggerType.AltitudeReached, values);
        Assert.Contains(UGCTriggerType.SpeedReached,    values);
        Assert.Contains(UGCTriggerType.Timer,           values);
        Assert.Contains(UGCTriggerType.Checkpoint,      values);
        Assert.Contains(UGCTriggerType.Proximity,       values);
        Assert.Contains(UGCTriggerType.Weather,         values);
    }

    [Test]
    public void AltitudeMode_AllValuesAreDefined()
    {
        var values = (AltitudeMode[])Enum.GetValues(typeof(AltitudeMode));
        Assert.Contains(AltitudeMode.GroundLevel,      values);
        Assert.Contains(AltitudeMode.FixedAltitude,    values);
        Assert.Contains(AltitudeMode.RelativeToGround, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCConfig_MaxWaypoints_Is200()
    {
        Assert.AreEqual(200, UGCConfig.MaxWaypoints);
    }

    [Test]
    public void UGCConfig_MaxTriggers_Is50()
    {
        Assert.AreEqual(50, UGCConfig.MaxTriggers);
    }

    [Test]
    public void UGCConfig_MaxZones_Is30()
    {
        Assert.AreEqual(30, UGCConfig.MaxZones);
    }

    [Test]
    public void UGCConfig_TitleLimits_AreValid()
    {
        Assert.Greater(UGCConfig.MaxTitleLength, UGCConfig.MinTitleLength);
        Assert.Greater(UGCConfig.MinTitleLength, 0);
    }

    [Test]
    public void UGCConfig_ExportExtension_HasLeadingDot()
    {
        Assert.IsTrue(UGCConfig.ExportExtension.StartsWith("."));
    }

    [Test]
    public void UGCConfig_QualityThresholds_AreOrdered()
    {
        Assert.Greater(UGCConfig.AutoPublishQualityThreshold, UGCConfig.ManualReviewQualityThreshold);
        Assert.Greater(UGCConfig.ManualReviewQualityThreshold, UGCConfig.AutoRejectQualityThreshold);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCContent
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCContent_Create_GeneratesUniqueIds()
    {
        var a = UGCContent.Create("author1", "Alice", UGCContentType.Tour);
        var b = UGCContent.Create("author1", "Alice", UGCContentType.Tour);
        Assert.AreNotEqual(a.contentId, b.contentId);
    }

    [Test]
    public void UGCContent_Create_SetsStatus_ToDraft()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Mission);
        Assert.AreEqual(UGCStatus.Draft, content.status);
    }

    [Test]
    public void UGCContent_Create_SetsTimestamps()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Tour);
        Assert.IsFalse(string.IsNullOrEmpty(content.createdAt));
        Assert.IsFalse(string.IsNullOrEmpty(content.updatedAt));
    }

    [Test]
    public void UGCContent_ListsDefaultToEmpty()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Tour);
        Assert.IsNotNull(content.waypoints);
        Assert.IsNotNull(content.triggers);
        Assert.IsNotNull(content.zones);
        Assert.IsNotNull(content.tags);
        Assert.AreEqual(0, content.waypoints.Count);
        Assert.AreEqual(0, content.triggers.Count);
        Assert.AreEqual(0, content.zones.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCWaypoint
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCWaypoint_DefaultValues_AreCorrect()
    {
        var wp = new UGCWaypoint();
        Assert.AreEqual(0.0, wp.latitude);
        Assert.AreEqual(0.0, wp.longitude);
        Assert.AreEqual(0f,  wp.altitude);
        Assert.AreEqual(100f, wp.radius);
        Assert.IsTrue(wp.isRequired);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCReview
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCReview_Create_SetsFields()
    {
        var review = UGCReview.Create("content1", "reviewer1", UGCRating.FiveStar, "Great content!");
        Assert.AreEqual("content1",  review.contentId);
        Assert.AreEqual("reviewer1", review.reviewerId);
        Assert.AreEqual(UGCRating.FiveStar, review.rating);
        Assert.AreEqual("Great content!", review.comment);
        Assert.AreEqual(0, review.helpfulCount);
        Assert.IsFalse(string.IsNullOrEmpty(review.reviewId));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ValidationIssue / ValidationResult
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ValidationIssue_CanBeInstantiated()
    {
        var issue = new ValidationIssue(ValidationSeverity.Warning, "Test warning", "Fix it");
        Assert.AreEqual(ValidationSeverity.Warning, issue.severity);
        Assert.AreEqual("Test warning", issue.message);
        Assert.AreEqual("Fix it", issue.suggestion);
    }

    [Test]
    public void ValidationResult_IsPublishable_FalseWhenErrorPresent()
    {
        var result = new ValidationResult();
        result.Issues.Add(new ValidationIssue(ValidationSeverity.Error, "Some error"));
        Assert.IsFalse(result.IsPublishable);
    }

    [Test]
    public void ValidationResult_IsPublishable_TrueWithOnlyWarnings()
    {
        var result = new ValidationResult();
        result.Issues.Add(new ValidationIssue(ValidationSeverity.Warning, "Some warning"));
        Assert.IsTrue(result.IsPublishable);
    }

    [Test]
    public void ValidationResult_IsPublishable_FalseWhenCriticalPresent()
    {
        var result = new ValidationResult();
        result.Issues.Add(new ValidationIssue(ValidationSeverity.Critical, "Critical issue"));
        Assert.IsFalse(result.IsPublishable);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCValidator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCValidator_ValidateContent_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => UGCValidator.ValidateContent(null));
    }

    [Test]
    public void UGCValidator_EmptyTitle_ProducesError()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Tour);
        content.title = string.Empty;
        var result = UGCValidator.ValidateContent(content);
        bool hasError = result.Issues.Exists(i =>
            i.severity >= ValidationSeverity.Error && i.message.Contains("Title"));
        Assert.IsTrue(hasError, "Expected an Error-level issue about empty title.");
    }

    [Test]
    public void UGCValidator_TooFewWaypoints_ProducesError()
    {
        var content = BuildMinimalContent();
        content.waypoints.Clear();
        var result = UGCValidator.ValidateContent(content);
        bool hasError = result.Issues.Exists(i =>
            i.severity >= ValidationSeverity.Error && i.message.ToLower().Contains("waypoint"));
        Assert.IsTrue(hasError, "Expected an error about too few waypoints.");
    }

    [Test]
    public void UGCValidator_ValidContent_IsPublishable()
    {
        var content = BuildValidContent();
        var result = UGCValidator.ValidateContent(content);
        Assert.IsTrue(result.IsPublishable, "Valid content should be publishable.");
    }

    [Test]
    public void UGCValidator_QualityScore_IsInRange()
    {
        var content = BuildValidContent();
        var result  = UGCValidator.ValidateContent(content);
        Assert.GreaterOrEqual(result.QualityScore, 0);
        Assert.LessOrEqual(result.QualityScore,    100);
    }

    [Test]
    public void UGCValidator_TestedContent_GetsHigherScore()
    {
        var notTested = BuildValidContent();
        notTested.metadata.hasBeenTested = false;
        var resultUntested = UGCValidator.ValidateContent(notTested);

        var tested = BuildValidContent();
        tested.metadata.hasBeenTested = true;
        var resultTested = UGCValidator.ValidateContent(tested);

        Assert.GreaterOrEqual(resultTested.QualityScore, resultUntested.QualityScore,
            "Tested content should have at least as high a quality score.");
    }

    [Test]
    public void UGCValidator_OrphanTriggerChain_ProducesError()
    {
        var content = BuildValidContent();
        content.triggers.Add(new UGCTrigger
        {
            triggerId         = Guid.NewGuid().ToString(),
            chainToTriggerId  = "nonexistent-id",
            radius            = 50f,
            isEnabled         = true,
        });
        var result = UGCValidator.ValidateContent(content);
        bool hasError = result.Issues.Exists(i => i.severity >= ValidationSeverity.Error
                                                   && i.message.Contains("chain"));
        Assert.IsTrue(hasError, "Orphan trigger chain should produce an error.");
    }

    [Test]
    public void UGCValidator_InvalidCoordinates_ProducesCritical()
    {
        var content = BuildValidContent();
        content.waypoints[0].latitude  = 999.0;
        content.waypoints[0].longitude = 999.0;
        var result = UGCValidator.ValidateContent(content);
        bool hasCritical = result.Issues.Exists(i => i.severity == ValidationSeverity.Critical);
        Assert.IsTrue(hasCritical, "Invalid coordinates should produce a Critical issue.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Editor command pattern
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void AddWaypointCommand_Execute_AddsWaypoint()
    {
        var content  = UGCContent.Create("a", "A", UGCContentType.Tour);
        var waypoint = new UGCWaypoint { waypointId = "wp1" };
        var cmd      = new AddWaypointCommand(content, waypoint);
        cmd.Execute();
        Assert.AreEqual(1, content.waypoints.Count);
        Assert.AreSame(waypoint, content.waypoints[0]);
    }

    [Test]
    public void AddWaypointCommand_Undo_RemovesWaypoint()
    {
        var content  = UGCContent.Create("a", "A", UGCContentType.Tour);
        var waypoint = new UGCWaypoint { waypointId = "wp1" };
        var cmd      = new AddWaypointCommand(content, waypoint);
        cmd.Execute();
        cmd.Undo();
        Assert.AreEqual(0, content.waypoints.Count);
    }

    [Test]
    public void RemoveWaypointCommand_Execute_RemovesWaypoint()
    {
        var content  = UGCContent.Create("a", "A", UGCContentType.Tour);
        var waypoint = new UGCWaypoint { waypointId = "wp1" };
        content.waypoints.Add(waypoint);
        var cmd = new RemoveWaypointCommand(content, waypoint);
        cmd.Execute();
        Assert.AreEqual(0, content.waypoints.Count);
    }

    [Test]
    public void RemoveWaypointCommand_Undo_RestoresWaypoint()
    {
        var content  = UGCContent.Create("a", "A", UGCContentType.Tour);
        var waypoint = new UGCWaypoint { waypointId = "wp1" };
        content.waypoints.Add(waypoint);
        var cmd = new RemoveWaypointCommand(content, waypoint);
        cmd.Execute();
        cmd.Undo();
        Assert.AreEqual(1, content.waypoints.Count);
        Assert.AreSame(waypoint, content.waypoints[0]);
    }

    [Test]
    public void AddTriggerCommand_Execute_AddsTrigger()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Mission);
        var trigger = new UGCTrigger { triggerId = "t1", radius = 50f };
        var cmd     = new AddTriggerCommand(content, trigger);
        cmd.Execute();
        Assert.AreEqual(1, content.triggers.Count);
    }

    [Test]
    public void AddTriggerCommand_Undo_RemovesTrigger()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Mission);
        var trigger = new UGCTrigger { triggerId = "t1", radius = 50f };
        var cmd     = new AddTriggerCommand(content, trigger);
        cmd.Execute();
        cmd.Undo();
        Assert.AreEqual(0, content.triggers.Count);
    }

    [Test]
    public void AddZoneCommand_Execute_AddsZone()
    {
        var content = UGCContent.Create("a", "A", UGCContentType.Scenario);
        var zone    = new UGCZone { zoneId = "z1", radius = 200f };
        var cmd     = new AddZoneCommand(content, zone);
        cmd.Execute();
        Assert.AreEqual(1, content.zones.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCEditorManager (MonoBehaviour — instantiated via AddComponent)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCEditorManager_Undo_CanUndo_AfterExecute()
    {
        var go      = new GameObject("EditorManager");
        var manager = go.AddComponent<UGCEditorManager>();

        manager.CreateProject("author", "Alice", UGCContentType.Tour);

        var waypoint = new UGCWaypoint { waypointId = "wp1" };
        var cmd      = new AddWaypointCommand(manager.CurrentProject, waypoint);
        manager.ExecuteCommand(cmd);

        Assert.IsTrue(manager.CanUndo, "Should be able to undo after executing a command.");
        Assert.IsFalse(manager.CanRedo, "Redo stack should be empty after execute.");

        manager.Undo();
        Assert.IsFalse(manager.CanUndo, "After undo, undo stack should be empty.");
        Assert.IsTrue(manager.CanRedo,  "After undo, redo should be available.");

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void UGCEditorManager_CreateProject_RaisesEvent()
    {
        var go      = new GameObject("EditorManager2");
        var manager = go.AddComponent<UGCEditorManager>();

        UGCContent received = null;
        manager.OnProjectCreated += c => received = c;
        manager.CreateProject("auth", "Bob", UGCContentType.RaceCourse);

        Assert.IsNotNull(received);
        Assert.AreEqual(UGCContentType.RaceCourse, received.contentType);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCReviewManager (MonoBehaviour)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCReviewManager_SubmitReview_StoresReview()
    {
        var go      = new GameObject("ReviewManager");
        var manager = go.AddComponent<UGCReviewManager>();

        var review = manager.SubmitReview("content1", "player1", UGCRating.FourStar, "Nice!");
        Assert.IsNotNull(review);
        Assert.AreEqual(1, manager.GetReviewCount("content1"));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void UGCReviewManager_PreventsDuplicateReview()
    {
        var go      = new GameObject("ReviewManager2");
        var manager = go.AddComponent<UGCReviewManager>();

        manager.SubmitReview("content1", "player1", UGCRating.FourStar);
        var duplicate = manager.SubmitReview("content1", "player1", UGCRating.TwoStar);
        Assert.IsNull(duplicate, "Duplicate review should be rejected.");
        Assert.AreEqual(1, manager.GetReviewCount("content1"));

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void UGCReviewManager_AverageRating_IsCorrect()
    {
        var go      = new GameObject("ReviewManager3");
        var manager = go.AddComponent<UGCReviewManager>();

        manager.SubmitReview("c1", "p1", UGCRating.FourStar);
        manager.SubmitReview("c1", "p2", UGCRating.TwoStar);
        float avg = manager.GetAverageRating("c1");
        Assert.AreEqual(3f, avg, 0.01f);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void UGCReviewManager_AverageRating_ZeroForNoReviews()
    {
        var go      = new GameObject("ReviewManager4");
        var manager = go.AddComponent<UGCReviewManager>();
        Assert.AreEqual(0f, manager.GetAverageRating("no_content"));
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void UGCReviewManager_VoteHelpful_IncrementsCount()
    {
        var go      = new GameObject("ReviewManager5");
        var manager = go.AddComponent<UGCReviewManager>();

        var review = manager.SubmitReview("c1", "p1", UGCRating.ThreeStar);
        Assert.IsNotNull(review);
        manager.VoteHelpful(review.reviewId);
        Assert.AreEqual(1, review.helpfulCount);

        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BrowseSortMode
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void BrowseSortMode_AllValuesAreDefined()
    {
        var values = (BrowseSortMode[])Enum.GetValues(typeof(BrowseSortMode));
        Assert.Contains(BrowseSortMode.Newest,        values);
        Assert.Contains(BrowseSortMode.Popular,       values);
        Assert.Contains(BrowseSortMode.HighestRated,  values);
        Assert.Contains(BrowseSortMode.MostDownloaded, values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TestPlayResult
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void TestPlayResult_DefaultsToNotPassed()
    {
        var result = new TestPlayResult();
        Assert.IsFalse(result.passed);
        Assert.AreEqual(0, result.waypointsReached);
        Assert.IsNotNull(result.issues);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UGCShareManager
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UGCShareManager_GenerateDeepLink_HasCorrectPrefix()
    {
        var go      = new GameObject("ShareManager");
        var manager = go.AddComponent<UGCShareManager>();
        string link = manager.GenerateDeepLink("abc123");
        Assert.IsTrue(link.StartsWith("swef://ugc?id="), $"Unexpected link: {link}");
        Assert.IsTrue(link.EndsWith("abc123"), $"Unexpected link: {link}");
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static UGCContent BuildMinimalContent()
    {
        var content = UGCContent.Create("auth", "Author", UGCContentType.Tour);
        content.title       = "Test Tour";
        content.description = "A test tour.";
        return content;
    }

    private static UGCContent BuildValidContent()
    {
        var content = BuildMinimalContent();
        for (int i = 0; i < 3; i++)
        {
            content.waypoints.Add(new UGCWaypoint
            {
                waypointId = Guid.NewGuid().ToString(),
                latitude   = 35.0 + i * 0.1,
                longitude  = 135.0 + i * 0.1,
                altitude   = 1000f,
                radius     = 100f,
                order      = i,
                isRequired = true,
            });
        }
        return content;
    }
}
