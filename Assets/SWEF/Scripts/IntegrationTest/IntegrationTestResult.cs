// IntegrationTestResult.cs — SWEF Phase 96: Integration Test & QA Framework
// Defines the result types returned by every integration test case.
using System;

namespace SWEF.IntegrationTest
{
    /// <summary>Outcome of a single integration test run.</summary>
    public enum TestStatus
    {
        /// <summary>The test passed without errors.</summary>
        Pass,

        /// <summary>The test detected one or more failures.</summary>
        Fail,

        /// <summary>The test was intentionally skipped (e.g. missing dependency).</summary>
        Skip,

        /// <summary>The test did not complete within the allowed time budget.</summary>
        Timeout
    }

    /// <summary>
    /// Immutable record returned by <see cref="IntegrationTestCase.Execute"/>.
    /// Carries the status, human-readable message, timing, and originating module/test names.
    /// </summary>
    public sealed class IntegrationTestResult
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Name of the module under test (e.g. "Flight", "Achievement").</summary>
        public string ModuleName { get; }

        /// <summary>Name of the individual test (e.g. "FlightManagerInitializes").</summary>
        public string TestName { get; }

        // ── Outcome ───────────────────────────────────────────────────────────

        /// <summary>Pass / Fail / Skip / Timeout.</summary>
        public TestStatus Status { get; }

        /// <summary>Human-readable description of the outcome or failure reason.</summary>
        public string Message { get; }

        /// <summary>Wall-clock duration of the Execute phase in seconds.</summary>
        public float Duration { get; }

        /// <summary>UTC timestamp when the result was created.</summary>
        public DateTime Timestamp { get; }

        // ── Construction ──────────────────────────────────────────────────────

        /// <summary>Creates a new <see cref="IntegrationTestResult"/>.</summary>
        /// <param name="moduleName">Name of the module under test.</param>
        /// <param name="testName">Name of the individual test.</param>
        /// <param name="status">Outcome of the test.</param>
        /// <param name="message">Details or failure reason.</param>
        /// <param name="duration">Duration in seconds.</param>
        public IntegrationTestResult(string moduleName, string testName, TestStatus status, string message, float duration = 0f)
        {
            ModuleName = moduleName ?? string.Empty;
            TestName   = testName   ?? string.Empty;
            Status     = status;
            Message    = message    ?? string.Empty;
            Duration   = duration;
            Timestamp  = DateTime.UtcNow;
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>Creates a <see cref="TestStatus.Pass"/> result.</summary>
        public static IntegrationTestResult Pass(string moduleName, string testName, string message = "OK", float duration = 0f)
            => new IntegrationTestResult(moduleName, testName, TestStatus.Pass, message, duration);

        /// <summary>Creates a <see cref="TestStatus.Fail"/> result.</summary>
        public static IntegrationTestResult Fail(string moduleName, string testName, string message, float duration = 0f)
            => new IntegrationTestResult(moduleName, testName, TestStatus.Fail, message, duration);

        /// <summary>Creates a <see cref="TestStatus.Skip"/> result.</summary>
        public static IntegrationTestResult Skip(string moduleName, string testName, string reason)
            => new IntegrationTestResult(moduleName, testName, TestStatus.Skip, reason);

        /// <summary>Creates a <see cref="TestStatus.Timeout"/> result.</summary>
        public static IntegrationTestResult Timeout(string moduleName, string testName, float timeoutSeconds)
            => new IntegrationTestResult(moduleName, testName, TestStatus.Timeout,
                                         $"Test exceeded {timeoutSeconds:F1}s timeout.", timeoutSeconds);

        /// <inheritdoc/>
        public override string ToString()
            => $"[{Status}] {ModuleName}/{TestName} ({Duration:F3}s) — {Message}";
    }
}
