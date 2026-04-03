// IntegrationTestCase.cs — SWEF Phase 96: Integration Test & QA Framework
// Abstract base class that every integration test case must extend.
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Abstract base class for all SWEF integration test cases.
    ///
    /// <para>Subclasses override <see cref="Setup"/>, <see cref="Execute"/>, and
    /// <see cref="Teardown"/>.  The <see cref="IntegrationTestRunner"/> calls these
    /// methods in order and records the returned <see cref="IntegrationTestResult"/>.</para>
    ///
    /// <para>Tests must be safe to run on ALL platforms (PC, Mobile, Tablet, XR).
    /// Use defensive null checks — never assume any singleton or scene object exists.</para>
    /// </summary>
    public abstract class IntegrationTestCase
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Human-readable name of this individual test.</summary>
        public abstract string TestName { get; }

        /// <summary>Name of the SWEF module being exercised (e.g. "Flight", "Achievement").</summary>
        public abstract string ModuleName { get; }

        /// <summary>
        /// Execution priority.  Lower values run first.
        /// Use 0 for infrastructure/smoke tests, 100+ for feature-level tests.
        /// </summary>
        public virtual int Priority => 100;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Called once before <see cref="Execute"/>.
        /// Allocate resources, create GameObjects, or skip the test here.
        ///
        /// <para>Return <c>null</c> to indicate setup succeeded; return a
        /// <see cref="IntegrationTestResult"/> with <see cref="TestStatus.Skip"/>
        /// to skip execution.</para>
        /// </summary>
        /// <returns><c>null</c> on success, or a skip/fail result.</returns>
        public abstract IntegrationTestResult Setup();

        /// <summary>
        /// The actual test logic.  Called only when <see cref="Setup"/> returned <c>null</c>.
        /// Return a <see cref="IntegrationTestResult"/> indicating Pass, Fail, or Skip.
        /// </summary>
        public abstract IntegrationTestResult Execute();

        /// <summary>
        /// Called after <see cref="Execute"/> (even if it failed) to clean up
        /// any resources allocated in <see cref="Setup"/>.
        /// </summary>
        public abstract void Teardown();

        // ── Convenience helpers ───────────────────────────────────────────────

        /// <summary>Creates a Pass result for this test.</summary>
        protected IntegrationTestResult Pass(string message = "OK", float duration = 0f)
            => IntegrationTestResult.Pass(ModuleName, TestName, message, duration);

        /// <summary>Creates a Fail result for this test.</summary>
        protected IntegrationTestResult Fail(string message, float duration = 0f)
            => IntegrationTestResult.Fail(ModuleName, TestName, message, duration);

        /// <summary>Creates a Skip result for this test.</summary>
        protected IntegrationTestResult Skip(string reason)
            => IntegrationTestResult.Skip(ModuleName, TestName, reason);

        /// <summary>
        /// Safely destroys a GameObject, suppressing null-reference exceptions.
        /// Call in <see cref="Teardown"/> to clean up temporary objects.
        /// </summary>
        protected static void SafeDestroy(Object obj)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }
    }
}
