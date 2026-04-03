// SampleIntegrationTests.cs — SWEF Phase 96: Integration Test & QA Framework
// Example integration tests demonstrating how to use the SWEF integration test framework.
// These tests serve as both documentation and baseline smoke tests.
using System;
using NUnit.Framework;
using SWEF.IntegrationTest;

// ─────────────────────────────────────────────────────────────────────────────
// FlightSystemHealthTest
// Checks that the FlightManager type is resolvable (compile-time wiring check).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Validates that the core SWEF integration test infrastructure itself is functional.
/// </summary>
[TestFixture]
public class IntegrationTestInfrastructureTests
{
    [Test]
    public void IntegrationTestResult_PassFactory_ReturnsPassStatus()
    {
        var result = IntegrationTestResult.Pass("Flight", "SmokeTest", "Looks good", 0.5f);
        Assert.AreEqual(TestStatus.Pass, result.Status);
        Assert.AreEqual("Flight", result.ModuleName);
        Assert.AreEqual("SmokeTest", result.TestName);
        Assert.AreEqual(0.5f, result.Duration, 0.001f);
    }

    [Test]
    public void IntegrationTestResult_FailFactory_ReturnsFailStatus()
    {
        var result = IntegrationTestResult.Fail("Achievement", "WiringTest", "Event not found");
        Assert.AreEqual(TestStatus.Fail, result.Status);
        Assert.AreEqual("Achievement", result.ModuleName);
    }

    [Test]
    public void IntegrationTestResult_SkipFactory_ReturnsSkipStatus()
    {
        var result = IntegrationTestResult.Skip("Mission", "JournalBridge", "Journal module not available");
        Assert.AreEqual(TestStatus.Skip, result.Status);
    }

    [Test]
    public void IntegrationTestResult_TimeoutFactory_ReturnsTimeoutStatus()
    {
        var result = IntegrationTestResult.Timeout("Weather", "StormEvent", 30f);
        Assert.AreEqual(TestStatus.Timeout, result.Status);
        Assert.AreEqual(30f, result.Duration, 0.001f);
    }

    [Test]
    public void IntegrationTestResult_ToString_ContainsKeyInfo()
    {
        var result = IntegrationTestResult.Pass("Core", "Smoke", "OK", 1.23f);
        string str = result.ToString();
        Assert.IsTrue(str.Contains("Pass"), $"Expected 'Pass' in: {str}");
        Assert.IsTrue(str.Contains("Core"), $"Expected 'Core' in: {str}");
        Assert.IsTrue(str.Contains("Smoke"), $"Expected 'Smoke' in: {str}");
    }
}

/// <summary>
/// Tests for <see cref="IntegrationTestRegistry"/> auto-discovery and querying.
/// </summary>
[TestFixture]
public class IntegrationTestRegistryTests
{
    [SetUp]
    public void SetUp() => IntegrationTestRegistry.Clear();

    [TearDown]
    public void TearDown() => IntegrationTestRegistry.Clear();

    [Test]
    public void Register_AddsTestCase()
    {
        var tc = new StubTestCase("StubTest", "StubModule", 50);
        IntegrationTestRegistry.Register(tc);
        Assert.AreEqual(1, IntegrationTestRegistry.Count);
    }

    [Test]
    public void Register_DuplicateNotAdded()
    {
        var tc = new StubTestCase("StubTest", "StubModule", 50);
        IntegrationTestRegistry.Register(tc);
        IntegrationTestRegistry.Register(tc); // second time — should be ignored
        Assert.AreEqual(1, IntegrationTestRegistry.Count);
    }

    [Test]
    public void GetAll_ReturnsSortedByPriority()
    {
        IntegrationTestRegistry.Register(new StubTestCase("High", "Mod", 100));
        IntegrationTestRegistry.Register(new StubTestCase("Low", "Mod", 10));
        IntegrationTestRegistry.Register(new StubTestCase("Mid", "Mod", 50));

        var all = IntegrationTestRegistry.GetAll();
        Assert.AreEqual("Low", all[0].TestName);
        Assert.AreEqual("Mid", all[1].TestName);
        Assert.AreEqual("High", all[2].TestName);
    }

    [Test]
    public void GetByModule_FiltersCorrectly()
    {
        IntegrationTestRegistry.Register(new StubTestCase("T1", "Flight", 10));
        IntegrationTestRegistry.Register(new StubTestCase("T2", "Achievement", 10));
        IntegrationTestRegistry.Register(new StubTestCase("T3", "Flight", 20));

        var flightTests = IntegrationTestRegistry.GetByModule("Flight");
        Assert.AreEqual(2, flightTests.Count);
        Assert.IsTrue(flightTests.All(t => t.ModuleName == "Flight"));
    }

    [Test]
    public void GetByPriority_ReturnsOnlyLowerPriorityTests()
    {
        IntegrationTestRegistry.Register(new StubTestCase("Critical", "Core", 5));
        IntegrationTestRegistry.Register(new StubTestCase("Normal", "Core", 100));
        IntegrationTestRegistry.Register(new StubTestCase("Optional", "Core", 200));

        var p100 = IntegrationTestRegistry.GetByPriority(100);
        Assert.AreEqual(2, p100.Count);
    }

    [Test]
    public void DiscoverAll_FindsConcreteSubclasses()
    {
        // DiscoverAll should pick up at least the concrete test cases defined
        // in the SWEF.IntegrationTest assembly (ModuleHealthCheck, etc.).
        IntegrationTestRegistry.DiscoverAll();
        Assert.Greater(IntegrationTestRegistry.Count, 0,
            "DiscoverAll should find at least one concrete IntegrationTestCase.");
    }
}

/// <summary>
/// Tests for <see cref="IntegrationTestReport"/> formatting.
/// </summary>
[TestFixture]
public class IntegrationTestReportTests
{
    [Test]
    public void Report_Summary_ReflectsResultCounts()
    {
        var results = new[]
        {
            IntegrationTestResult.Pass("A", "T1", "ok", 0.1f),
            IntegrationTestResult.Fail("A", "T2", "bad", 0.2f),
            IntegrationTestResult.Skip("B", "T3", "skip"),
        };

        var report = new IntegrationTestReport(results);
        Assert.AreEqual(3, report.Total);
        Assert.AreEqual(1, report.Passed);
        Assert.AreEqual(1, report.Failed);
        Assert.AreEqual(1, report.Skipped);
        Assert.AreEqual(0, report.TimedOut);
        Assert.AreEqual(0.3f, report.TotalDuration, 0.01f);
    }

    [Test]
    public void Report_ToText_ContainsModuleName()
    {
        var report = new IntegrationTestReport(new[]
        {
            IntegrationTestResult.Pass("Flight", "InitTest", "ok")
        });
        Assert.IsTrue(report.ToText().Contains("Flight"));
    }

    [Test]
    public void Report_ToJson_IsValidJsonFragment()
    {
        var report = new IntegrationTestReport(new[]
        {
            IntegrationTestResult.Pass("Core", "Smoke", "ok", 0.5f)
        });
        string json = report.ToJson();
        Assert.IsTrue(json.Contains("\"passed\""));
        Assert.IsTrue(json.Contains("\"results\""));
        Assert.IsTrue(json.Contains("\"Core\""));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// InputSystemCrossplatformTest
// Validates that at least one input backend is available at compile time.
// ─────────────────────────────────────────────────────────────────────────────

[TestFixture]
public class InputSystemCrossplatformTest
{
    [Test]
    public void AtLeastOneInputBackendIsEnabled()
    {
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        Assert.Pass("At least one input backend is enabled.");
#else
        Assert.Fail("Neither ENABLE_INPUT_SYSTEM nor ENABLE_LEGACY_INPUT_MANAGER is defined. " +
                    "The app will have no input on any platform.");
#endif
    }

    [Test]
    public void PlatformCompatibilityTest_CanInstantiate()
    {
        var test = new PlatformCompatibilityTest();
        Assert.IsNotNull(test);
        Assert.AreEqual("Platform", test.ModuleName);
        Assert.AreEqual("PlatformCompatibility", test.TestName);
        Assert.AreEqual(5, test.Priority);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// AchievementEventWiringTest
// Validates that the achievement event wiring check runs without crashing.
// ─────────────────────────────────────────────────────────────────────────────

[TestFixture]
public class AchievementEventWiringTest
{
    [Test]
    public void CrossModuleEventTest_ExecutesWithoutException()
    {
        var test = new CrossModuleEventTest();
        Assert.IsNull(test.Setup(), "Setup should return null (no skip).");

        IntegrationTestResult result = null;
        Assert.DoesNotThrow(() => result = test.Execute());
        Assert.IsNotNull(result);

        // Result can be Pass or Fail (depending on assembly availability),
        // but must never be null or throw.
        Assert.IsTrue(result.Status == TestStatus.Pass || result.Status == TestStatus.Fail,
            $"Unexpected status: {result.Status} — {result.Message}");

        test.Teardown();
    }

    [Test]
    public void AssemblyReferenceValidator_ExecutesWithoutException()
    {
        var test = new AssemblyReferenceValidator();
        Assert.IsNull(test.Setup());

        IntegrationTestResult result = null;
        Assert.DoesNotThrow(() => result = test.Execute());
        Assert.IsNotNull(result);
        test.Teardown();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StubTestCase — local helper used by registry tests only.
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class StubTestCase : IntegrationTestCase
{
    private readonly string _testName;
    private readonly string _moduleName;
    private readonly int    _priority;

    public StubTestCase(string testName, string moduleName, int priority)
    {
        _testName   = testName;
        _moduleName = moduleName;
        _priority   = priority;
    }

    public override string TestName   => _testName;
    public override string ModuleName => _moduleName;
    public override int    Priority   => _priority;

    public override IntegrationTestResult Setup()   => null;
    public override IntegrationTestResult Execute() => Pass("stub");
    public override void Teardown() { }
}

// Extension to make LINQ .All() work on IReadOnlyList<IntegrationTestCase>
internal static class TestCaseEnumerableExtensions
{
    internal static bool All(this System.Collections.Generic.IReadOnlyList<IntegrationTestCase> list,
                             Func<IntegrationTestCase, bool> predicate)
    {
        foreach (var item in list)
            if (!predicate(item)) return false;
        return true;
    }
}
