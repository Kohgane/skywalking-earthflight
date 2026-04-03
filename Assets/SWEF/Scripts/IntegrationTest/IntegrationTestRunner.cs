// IntegrationTestRunner.cs — SWEF Phase 96: Integration Test & QA Framework
// MonoBehaviour-based orchestrator that discovers and runs integration tests.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// MonoBehaviour test orchestrator for SWEF's integration test suite.
    ///
    /// <para>Discovers all <see cref="IntegrationTestCase"/> subclasses via
    /// <see cref="IntegrationTestRegistry"/>, runs them sequentially (one per
    /// frame to avoid blocking the main thread), and fires events on completion.</para>
    ///
    /// <para>Can be triggered from the Unity Editor menu
    /// (<c>SWEF → Integration Tests → Run All</c>) or at runtime by calling
    /// <see cref="RunAllTests"/> directly.</para>
    /// </summary>
    public class IntegrationTestRunner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum seconds a single test is allowed to run before being marked Timeout.")]
        [SerializeField] private float testTimeoutSeconds = 30f;

        [Tooltip("When true, auto-discover test cases on Start.")]
        [SerializeField] private bool autoDiscoverOnStart = true;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired once before any tests run. Argument = total test count.</summary>
        public event Action<int> OnTestSuiteStarted;

        /// <summary>Fired after each individual test completes.</summary>
        public event Action<IntegrationTestResult> OnTestCompleted;

        /// <summary>Fired once after all tests have run. Argument = aggregated report.</summary>
        public event Action<IntegrationTestReport> OnTestSuiteFinished;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<IntegrationTestResult> _results = new List<IntegrationTestResult>();
        private bool _isRunning;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            if (autoDiscoverOnStart)
                IntegrationTestRegistry.DiscoverAll();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Runs all registered integration tests asynchronously (one per frame).
        /// Does nothing if a run is already in progress.
        /// </summary>
        public void RunAllTests()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[IntegrationTestRunner] A test run is already in progress.");
                return;
            }

            IntegrationTestRegistry.DiscoverAll();
            StartCoroutine(RunSuiteCoroutine(IntegrationTestRegistry.GetAll()));
        }

        /// <summary>
        /// Runs only the tests belonging to the specified module.
        /// </summary>
        /// <param name="moduleName">Module name filter (case-insensitive).</param>
        public void RunModuleTests(string moduleName)
        {
            if (_isRunning)
            {
                Debug.LogWarning("[IntegrationTestRunner] A test run is already in progress.");
                return;
            }

            IntegrationTestRegistry.DiscoverAll();
            StartCoroutine(RunSuiteCoroutine(IntegrationTestRegistry.GetByModule(moduleName)));
        }

        /// <summary>Returns true while a test suite is executing.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Results from the most recently completed (or ongoing) suite run.</summary>
        public IReadOnlyList<IntegrationTestResult> LastResults => _results;

        // ── Coroutine ─────────────────────────────────────────────────────────

        private IEnumerator RunSuiteCoroutine(IReadOnlyList<IntegrationTestCase> cases)
        {
            _isRunning = true;
            _results.Clear();

            int total = cases.Count;
            OnTestSuiteStarted?.Invoke(total);
            Debug.Log($"[IntegrationTestRunner] Starting suite: {total} test(s).");

            for (int i = 0; i < cases.Count; i++)
            {
                var testCase = cases[i];
                IntegrationTestResult result = RunSingleTest(testCase);

                _results.Add(result);
                OnTestCompleted?.Invoke(result);

                string icon = result.Status == TestStatus.Pass ? "✓" : result.Status == TestStatus.Fail ? "✗" : "○";
                Debug.Log($"[IntegrationTestRunner] {icon} [{i + 1}/{total}] {result}");

                // Yield to keep Unity responsive.
                yield return null;
            }

            var report = new IntegrationTestReport(_results);
            _isRunning = false;

            OnTestSuiteFinished?.Invoke(report);
            Debug.Log($"[IntegrationTestRunner] Suite finished. {report.Summary}");
        }

        // ── Single-test execution ─────────────────────────────────────────────

        private IntegrationTestResult RunSingleTest(IntegrationTestCase testCase)
        {
            IntegrationTestResult setupResult = null;

            try
            {
                setupResult = testCase.Setup();
            }
            catch (Exception ex)
            {
                return IntegrationTestResult.Fail(testCase.ModuleName, testCase.TestName,
                    $"Setup threw exception: {ex.GetType().Name} — {ex.Message}");
            }

            // If Setup returned a result (skip/fail), use it directly.
            if (setupResult != null)
            {
                SafeTeardown(testCase);
                return setupResult;
            }

            IntegrationTestResult executeResult;
            float startTime = Time.realtimeSinceStartup;

            try
            {
                executeResult = testCase.Execute();
                float duration = Time.realtimeSinceStartup - startTime;

                if (executeResult == null)
                    executeResult = IntegrationTestResult.Fail(testCase.ModuleName, testCase.TestName,
                        "Execute() returned null.");

                // NOTE: This post-hoc duration check marks a slow test as Timeout after it
                // returns, but does NOT abort a synchronous Execute() call mid-flight.
                // True cancellation would require async/threading — deferred to a future phase.
                if (duration > testTimeoutSeconds)
                    executeResult = IntegrationTestResult.Timeout(testCase.ModuleName, testCase.TestName, testTimeoutSeconds);
            }
            catch (Exception ex)
            {
                float duration = Time.realtimeSinceStartup - startTime;
                executeResult = IntegrationTestResult.Fail(testCase.ModuleName, testCase.TestName,
                    $"Execute threw exception: {ex.GetType().Name} — {ex.Message}", duration);
            }
            finally
            {
                SafeTeardown(testCase);
            }

            return executeResult;
        }

        private static void SafeTeardown(IntegrationTestCase testCase)
        {
            try { testCase.Teardown(); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IntegrationTestRunner] Teardown error in {testCase.TestName}: {ex.Message}");
            }
        }

#if UNITY_EDITOR
        // ── Editor menu ───────────────────────────────────────────────────────

        [UnityEditor.MenuItem("SWEF/Integration Tests/Run All")]
        private static void EditorRunAll()
        {
            var go = new GameObject("_IntegrationTestRunner");
            var runner = go.AddComponent<IntegrationTestRunner>();
            runner.autoDiscoverOnStart = false;
            runner.RunAllTests();
        }
#endif
    }
}
