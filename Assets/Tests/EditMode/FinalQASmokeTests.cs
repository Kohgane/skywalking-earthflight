// FinalQASmokeTests.cs — SWEF Phase 102: Final QA & Release Candidate Prep
// NUnit EditMode tests for the Phase 102 QA data model:
//   FinalQAChecklist, SmokeTestConfig, PerformanceBenchmarkConfig, StoreSubmissionChecklist
// and the ReleaseCandidateConfig constants.
using System.Linq;
using NUnit.Framework;
using SWEF.BuildPipeline;
using SWEF.QA;

// ═══════════════════════════════════════════════════════════════════════════════
// FinalQAChecklist tests
// ═══════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class FinalQAChecklistTests
{
    private FinalQAChecklist _checklist;

    [SetUp]
    public void SetUp()
    {
        _checklist = FinalQAChecklist.Build();
    }

    // ── Build ──────────────────────────────────────────────────────────────────

    [Test]
    public void Build_ReturnsNonNullChecklist()
    {
        Assert.IsNotNull(_checklist);
    }

    [Test]
    public void Build_ContainsAtLeast40Items()
    {
        Assert.GreaterOrEqual(_checklist.Items.Count, 40,
            "Checklist should cover at least 40 QA items across all major systems.");
    }

    [Test]
    public void Build_AllItemsHaveNonEmptyId()
    {
        foreach (var item in _checklist.Items)
            Assert.IsFalse(string.IsNullOrEmpty(item.Id), $"Item '{item.Title}' has an empty Id.");
    }

    [Test]
    public void Build_AllItemsHaveNonEmptyTitle()
    {
        foreach (var item in _checklist.Items)
            Assert.IsFalse(string.IsNullOrEmpty(item.Title), $"Item '{item.Id}' has an empty Title.");
    }

    [Test]
    public void Build_AllItemsHavePassCriteria()
    {
        foreach (var item in _checklist.Items)
            Assert.IsFalse(string.IsNullOrEmpty(item.PassCriteria),
                $"Item '{item.Id}' is missing a PassCriteria.");
    }

    [Test]
    public void Build_AllItemsStartPending()
    {
        foreach (var item in _checklist.Items)
            Assert.AreEqual(QAResult.Pending, item.Result,
                $"Item '{item.Id}' should start as Pending.");
    }

    [Test]
    public void Build_AllIdsAreUnique()
    {
        var ids = _checklist.Items.Select(i => i.Id).ToList();
        var distinct = ids.Distinct().Count();
        Assert.AreEqual(ids.Count, distinct, "Duplicate IDs found in the checklist.");
    }

    // ── System coverage ────────────────────────────────────────────────────────

    [Test]
    public void Build_CoversFlightPhysicsSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.FlightPhysics));
    }

    [Test]
    public void Build_CoversControlsSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Controls));
    }

    [Test]
    public void Build_CoversCesiumTilesSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.CesiumTiles));
    }

    [Test]
    public void Build_CoversGPSSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.GPS));
    }

    [Test]
    public void Build_CoversWeatherSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Weather));
    }

    [Test]
    public void Build_CoversDayNightSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.DayNight));
    }

    [Test]
    public void Build_CoversHUDSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.HUD));
    }

    [Test]
    public void Build_CoversMinimapSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Minimap));
    }

    [Test]
    public void Build_CoversAchievementSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Achievement));
    }

    [Test]
    public void Build_CoversJournalSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Journal));
    }

    [Test]
    public void Build_CoversMultiplayerSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Multiplayer));
    }

    [Test]
    public void Build_CoversAICoPilotSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.AICoPilot));
    }

    [Test]
    public void Build_CoversAudioSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Audio));
    }

    [Test]
    public void Build_CoversCameraSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Camera));
    }

    [Test]
    public void Build_CoversATCSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.ATC));
    }

    [Test]
    public void Build_CoversEmergencySystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Emergency));
    }

    [Test]
    public void Build_CoversBattlePassSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.BattlePass));
    }

    [Test]
    public void Build_CoversSeasonalEventsSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.SeasonalEvents));
    }

    [Test]
    public void Build_CoversPerformanceSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Performance));
    }

    [Test]
    public void Build_CoversPlatformSystem()
    {
        Assert.IsNotEmpty(_checklist.GetBySystem(QASystem.Platform));
    }

    // ── State transitions ──────────────────────────────────────────────────────

    [Test]
    public void PendingChecklistIsNotReleasable()
    {
        Assert.IsFalse(_checklist.IsReleasable,
            "A checklist with all-Pending items must not be releasable.");
    }

    [Test]
    public void AllPassedChecklistIsReleasable()
    {
        foreach (var item in _checklist.Items)
            item.Mark(QAResult.Pass);

        Assert.IsTrue(_checklist.IsReleasable);
        Assert.AreEqual(0, _checklist.FailCount);
        Assert.AreEqual(0, _checklist.PendingCount);
    }

    [Test]
    public void SingleFailMakesChecklistNotReleasable()
    {
        foreach (var item in _checklist.Items)
            item.Mark(QAResult.Pass);

        _checklist.Items[0].Mark(QAResult.Fail, "Deliberate test failure");

        Assert.IsFalse(_checklist.IsReleasable);
        Assert.AreEqual(1, _checklist.FailCount);
    }

    [Test]
    public void GetById_KnownId_ReturnsItem()
    {
        var item = _checklist.GetById("FP-001");
        Assert.IsNotNull(item);
        Assert.AreEqual("FP-001", item.Id);
    }

    [Test]
    public void GetById_UnknownId_ReturnsNull()
    {
        Assert.IsNull(_checklist.GetById("DOES-NOT-EXIST-999"));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SmokeTestConfig tests
// ═══════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class SmokeTestConfigTests
{
    [Test]
    public void All_Returns6Configs()
    {
        Assert.AreEqual(6, SmokeTestConfig.All.Count,
            "There should be exactly 6 platform smoke configs.");
    }

    [Test]
    public void All_CoversWindowsPC()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.WindowsPC));
    }

    [Test]
    public void All_CoversMacOS()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.macOS));
    }

    [Test]
    public void All_CoversiOS()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.iOS));
    }

    [Test]
    public void All_CoversAndroid()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.Android));
    }

    [Test]
    public void All_CoversiPad()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.iPad));
    }

    [Test]
    public void All_CoversAndroidTablet()
    {
        Assert.IsNotNull(SmokeTestConfig.Get(SmokePlatform.AndroidTablet));
    }

    [Test]
    public void WindowsPC_TargetFps_Is60()
    {
        Assert.AreEqual(60, SmokeTestConfig.Get(SmokePlatform.WindowsPC).MinTargetFps);
    }

    [Test]
    public void iOS_TargetFps_Is30()
    {
        Assert.AreEqual(30, SmokeTestConfig.Get(SmokePlatform.iOS).MinTargetFps);
    }

    [Test]
    public void Android_TargetFps_Is30()
    {
        Assert.AreEqual(30, SmokeTestConfig.Get(SmokePlatform.Android).MinTargetFps);
    }

    [Test]
    public void iPad_TargetFps_Is60()
    {
        // iPads with ProMotion target 60 fps (capped from 120 for battery)
        Assert.AreEqual(60, SmokeTestConfig.Get(SmokePlatform.iPad).MinTargetFps);
    }

    [Test]
    public void All_RequiredItemIds_NotEmpty()
    {
        foreach (var cfg in SmokeTestConfig.All)
            Assert.IsNotEmpty(cfg.RequiredItemIds,
                $"{cfg.DisplayName} smoke config has no required item IDs.");
    }

    [Test]
    public void All_RequiredIds_ExistInFullChecklist()
    {
        var checklist = FinalQAChecklist.Build();
        foreach (var cfg in SmokeTestConfig.All)
        {
            foreach (var id in cfg.RequiredItemIds)
            {
                var item = checklist.GetById(id);
                Assert.IsNotNull(item,
                    $"Smoke config '{cfg.DisplayName}' references unknown item ID '{id}'.");
            }
        }
    }

    [Test]
    public void Evaluate_AllPass_ReturnsTrue()
    {
        var checklist = FinalQAChecklist.Build();
        foreach (var item in checklist.Items)
            item.Mark(QAResult.Pass);

        foreach (var cfg in SmokeTestConfig.All)
            Assert.IsTrue(cfg.Evaluate(checklist),
                $"Smoke gate for '{cfg.DisplayName}' should pass when all items pass.");
    }

    [Test]
    public void Evaluate_RequiredFailed_ReturnsFalse()
    {
        var checklist = FinalQAChecklist.Build();
        foreach (var item in checklist.Items)
            item.Mark(QAResult.Pass);

        // Fail a required item for Windows PC
        var cfg    = SmokeTestConfig.Get(SmokePlatform.WindowsPC);
        var failId = cfg.RequiredItemIds[0];
        checklist.GetById(failId).Mark(QAResult.Fail, "Deliberate failure");

        Assert.IsFalse(cfg.Evaluate(checklist),
            "Smoke gate should fail when a required item has failed.");
    }

    [Test]
    public void Evaluate_NullChecklist_ThrowsArgumentNullException()
    {
        var cfg = SmokeTestConfig.Get(SmokePlatform.WindowsPC);
        Assert.Throws<System.ArgumentNullException>(() => cfg.Evaluate(null));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PerformanceBenchmarkConfig tests
// ═══════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class PerformanceBenchmarkConfigTests
{
    [Test]
    public void All_Returns6Configs()
    {
        Assert.AreEqual(6, PerformanceBenchmarkConfig.All.Count);
    }

    [Test]
    public void WindowsPC_TargetFps_Is60()
    {
        Assert.AreEqual(60, PerformanceBenchmarkConfig.WindowsPC.TargetFps);
    }

    [Test]
    public void macOS_TargetFps_Is60()
    {
        Assert.AreEqual(60, PerformanceBenchmarkConfig.macOS.TargetFps);
    }

    [Test]
    public void iOS_TargetFps_Is30()
    {
        Assert.AreEqual(30, PerformanceBenchmarkConfig.iOS.TargetFps);
    }

    [Test]
    public void Android_TargetFps_Is30()
    {
        Assert.AreEqual(30, PerformanceBenchmarkConfig.Android.TargetFps);
    }

    [Test]
    public void iPad_TargetFps_Is60()
    {
        Assert.AreEqual(60, PerformanceBenchmarkConfig.iPad.TargetFps);
    }

    [Test]
    public void AndroidTablet_TargetFps_Is30()
    {
        Assert.AreEqual(30, PerformanceBenchmarkConfig.AndroidTablet.TargetFps);
    }

    [Test]
    public void All_MinAcceptableFps_LessThanOrEqualToTargetFps()
    {
        foreach (var cfg in PerformanceBenchmarkConfig.All)
            Assert.LessOrEqual(cfg.MinAcceptableFps, cfg.TargetFps,
                $"{cfg.PlatformName}: MinAcceptableFps must be ≤ TargetFps.");
    }

    [Test]
    public void All_MemoryBudgets_TotalRamMB_GreaterThanZero()
    {
        foreach (var cfg in PerformanceBenchmarkConfig.All)
            Assert.Greater(cfg.Memory.TotalRamMB, 0,
                $"{cfg.PlatformName}: TotalRamMB must be > 0.");
    }

    [Test]
    public void All_TileBudgets_MaxActiveTiles_GreaterThanZero()
    {
        foreach (var cfg in PerformanceBenchmarkConfig.All)
            Assert.Greater(cfg.Tiles.MaxActiveTiles, 0,
                $"{cfg.PlatformName}: MaxActiveTiles must be > 0.");
    }

    [Test]
    public void All_NetworkBudgets_MinBandwidth_LessThanRecommended()
    {
        foreach (var cfg in PerformanceBenchmarkConfig.All)
            Assert.LessOrEqual(cfg.Network.MinimumBandwidthMbps, cfg.Network.RecommendedBandwidthMbps,
                $"{cfg.PlatformName}: MinimumBandwidthMbps must be ≤ RecommendedBandwidthMbps.");
    }

    [Test]
    public void MobilePlatforms_TilesBudget_LessThanPC()
    {
        Assert.Less(PerformanceBenchmarkConfig.iOS.Tiles.MaxActiveTiles,
                    PerformanceBenchmarkConfig.WindowsPC.Tiles.MaxActiveTiles,
            "iOS tile budget must be smaller than Windows PC tile budget.");
    }

    [Test]
    public void Get_WindowsPrefix_ReturnsWindowsConfig()
    {
        var cfg = PerformanceBenchmarkConfig.Get("Windows");
        Assert.IsNotNull(cfg);
        Assert.AreEqual(60, cfg.TargetFps);
    }

    [Test]
    public void Get_UnknownPrefix_ReturnsNull()
    {
        Assert.IsNull(PerformanceBenchmarkConfig.Get("NonExistentPlatform"));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// StoreSubmissionChecklist tests
// ═══════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class StoreSubmissionChecklistTests
{
    private StoreSubmissionChecklist _checklist;

    [SetUp]
    public void SetUp()
    {
        _checklist = StoreSubmissionChecklist.Build();
    }

    [Test]
    public void Build_ReturnsNonNullChecklist()
    {
        Assert.IsNotNull(_checklist);
    }

    [Test]
    public void Build_ContainsAppStoreItems()
    {
        Assert.IsNotEmpty(_checklist.GetByStore(StoreTarget.AppStore));
    }

    [Test]
    public void Build_ContainsGooglePlayItems()
    {
        Assert.IsNotEmpty(_checklist.GetByStore(StoreTarget.GooglePlay));
    }

    [Test]
    public void Build_ContainsSteamItems()
    {
        Assert.IsNotEmpty(_checklist.GetByStore(StoreTarget.Steam));
    }

    [Test]
    public void Build_AllItemsHaveNonEmptyId()
    {
        foreach (var item in _checklist.Items)
            Assert.IsFalse(string.IsNullOrEmpty(item.Id));
    }

    [Test]
    public void Build_AllItemsHaveNonEmptyTitle()
    {
        foreach (var item in _checklist.Items)
            Assert.IsFalse(string.IsNullOrEmpty(item.Title));
    }

    [Test]
    public void Build_AllItemsStartNotStarted()
    {
        foreach (var item in _checklist.Items)
            Assert.AreEqual(SubmissionStatus.NotStarted, item.Status);
    }

    [Test]
    public void Build_AllIdsAreUnique()
    {
        var ids      = _checklist.Items.Select(i => i.Id).ToList();
        var distinct = ids.Distinct().Count();
        Assert.AreEqual(ids.Count, distinct, "Duplicate IDs found in store submission checklist.");
    }

    [Test]
    public void NewChecklist_IsNotReadyForSubmission()
    {
        Assert.IsFalse(_checklist.IsReadyForSubmission,
            "A fresh checklist with NotStarted items must not be ready for submission.");
    }

    [Test]
    public void AllComplete_IsReadyForSubmission()
    {
        foreach (var item in _checklist.Items)
            item.Status = SubmissionStatus.Complete;

        Assert.IsTrue(_checklist.IsReadyForSubmission);
    }

    [Test]
    public void GetBlockers_NothingComplete_ReturnsOnlyMandatory()
    {
        var blockers = _checklist.GetBlockers();
        // All mandatory + NotStarted items are blockers
        int mandatoryCount = _checklist.Items.Count(i => i.IsMandatory);
        Assert.AreEqual(mandatoryCount, blockers.Count);
    }

    [Test]
    public void AppStore_IncludesPrivacyItems()
    {
        var appStoreItems = _checklist.GetByStore(StoreTarget.AppStore);
        bool hasPrivacy   = appStoreItems.Any(i => i.Category == "Privacy");
        Assert.IsTrue(hasPrivacy, "App Store checklist must include Privacy items.");
    }

    [Test]
    public void GooglePlay_IncludesPermissionAudit()
    {
        var gpItems    = _checklist.GetByStore(StoreTarget.GooglePlay);
        bool hasPerms  = gpItems.Any(i => i.Category == "Permissions");
        Assert.IsTrue(hasPerms, "Google Play checklist must include Permissions audit items.");
    }

    [Test]
    public void Steam_IncludesSystemRequirements()
    {
        var steamItems = _checklist.GetByStore(StoreTarget.Steam);
        bool hasSysReq = steamItems.Any(i => i.Category == "System Requirements");
        Assert.IsTrue(hasSysReq, "Steam checklist must include System Requirements items.");
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ReleaseCandidateConfig tests
// ═══════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class ReleaseCandidateConfigTests
{
    [Test]
    public void Version_Is_1_0_0_rc1()
    {
        Assert.AreEqual("1.0.0-rc1", ReleaseCandidateVersion.Version);
    }

    [Test]
    public void BuildNumber_IsPositive()
    {
        Assert.Greater(ReleaseCandidateVersion.BuildNumber, 0);
    }

    [Test]
    public void BundleIdentifier_IsCorrect()
    {
        Assert.AreEqual("com.kohgane.swef", ReleaseCandidateVersion.BundleIdentifier);
    }

    [Test]
    public void IsDevelopment_IsFalse()
    {
        // RC builds submitted to stores must never be development builds.
        Assert.IsFalse(ReleaseCandidateVersion.IsDevelopment);
    }

    [Test]
    public void WindowsPC_ScriptingBackend_IsIL2CPP()
    {
        Assert.AreEqual("IL2CPP", RCPlatformSettings.WindowsPC.ScriptingBackend);
    }

    [Test]
    public void macOS_Architecture_IsUniversal()
    {
        Assert.AreEqual("Universal", RCPlatformSettings.macOS.Architecture);
    }

    [Test]
    public void iOS_Architecture_IsARM64()
    {
        Assert.AreEqual("ARM64", RCPlatformSettings.iOS.Architecture);
    }

    [Test]
    public void Android_Architecture_ContainsARM64()
    {
        StringAssert.Contains("ARM64", RCPlatformSettings.Android.Architecture);
    }

    [Test]
    public void AllProfiles_DefinesContainSWEF_RELEASE()
    {
        foreach (var profile in new[]
        {
            RCPlatformSettings.WindowsPC,
            RCPlatformSettings.macOS,
            RCPlatformSettings.iOS,
            RCPlatformSettings.Android
        })
        {
            CollectionAssert.Contains(profile.Defines, "SWEF_RELEASE",
                $"{profile.PlatformName} profile must define SWEF_RELEASE.");
        }
    }

    [Test]
    public void WindowsPC_TargetFps_Is60()
    {
        Assert.AreEqual(60, RCPlatformSettings.WindowsPC.TargetFps);
    }

    [Test]
    public void iOS_TargetFps_Is60()
    {
        // iOS RC targets 60 fps (ProMotion capped from 120 for battery); mobile benchmark floor is 30.
        Assert.AreEqual(60, RCPlatformSettings.iOS.TargetFps);
    }

    [Test]
    public void Android_TargetFps_Is30()
    {
        Assert.AreEqual(30, RCPlatformSettings.Android.TargetFps);
    }

    [Test]
    public void iOS_MinOSVersion_ContainsiOS()
    {
        StringAssert.Contains("iOS", RCPlatformSettings.iOS.MinOSVersion);
    }

    [Test]
    public void Android_MinOSVersion_ContainsAPI()
    {
        StringAssert.Contains("API", RCPlatformSettings.Android.MinOSVersion);
    }
}
