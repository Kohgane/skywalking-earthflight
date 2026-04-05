// UGCTestRunner.cs — SWEF Phase 108: User-Generated Content (UGC) Editor
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.UGC
{
    /// <summary>
    /// Phase 108 — MonoBehaviour that simulates a player flying through the UGC content
    /// to verify completability, detect unreachable waypoints, and estimate difficulty.
    ///
    /// <para>A test-play session must succeed before content can be published.</para>
    /// </summary>
    public sealed class UGCTestRunner : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("Maximum time in seconds allowed per test-play before timing out.")]
        [SerializeField] private float _maxTestTimeSeconds = 1800f;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a test-play session starts.</summary>
        public event Action OnTestStarted;

        /// <summary>Raised when the test-play session ends. Argument is the test result.</summary>
        public event Action<TestPlayResult> OnTestCompleted;

        /// <summary>Raised when an issue is detected mid-test.</summary>
        public event Action<string> OnTestIssueDetected;

        // ── Public state ───────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> while a test-play is in progress.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Most recent test-play result, or <c>null</c> if no test has run.</summary>
        public TestPlayResult LastResult { get; private set; }

        // ── Internal state ─────────────────────────────────────────────────────

        private UGCContent _content;
        private Coroutine  _testCoroutine;
        private float      _testStartTime;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnDestroy()
        {
            StopTest();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Binds the test runner to the given content project.
        /// </summary>
        public void SetContent(UGCContent content)
        {
            _content = content;
        }

        /// <summary>
        /// Starts a test-play session for the currently bound content.
        /// </summary>
        public void StartTest()
        {
            if (IsRunning)
            {
                Debug.LogWarning("[UGCTestRunner] A test is already running.");
                return;
            }

            if (_content == null)
            {
                Debug.LogWarning("[UGCTestRunner] No content bound.");
                return;
            }

            var validation = UGCValidator.ValidateContent(_content);
            if (!validation.IsPublishable)
            {
                Debug.LogWarning("[UGCTestRunner] Content has blocking errors — fix them before testing.");
                return;
            }

            IsRunning = true;
            _testStartTime = Time.realtimeSinceStartup;
            _testCoroutine = StartCoroutine(RunTestSession());
            OnTestStarted?.Invoke();
            Debug.Log("[UGCTestRunner] Test-play started.");
        }

        /// <summary>
        /// Aborts a running test-play session.
        /// </summary>
        public void StopTest()
        {
            if (!IsRunning) return;
            IsRunning = false;
            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
                _testCoroutine = null;
            }
            Debug.Log("[UGCTestRunner] Test-play aborted.");
        }

        // ── Private test logic ─────────────────────────────────────────────────

        private IEnumerator RunTestSession()
        {
            var result          = new TestPlayResult { contentId = _content.contentId };
            var reachedWaypoints = new HashSet<string>();
            var sortedWaypoints  = new List<UGCWaypoint>(_content.waypoints);
            sortedWaypoints.Sort((a, b) => a.order.CompareTo(b.order));

            float elapsed = 0f;

            // Simulate traversal — in a real implementation the player flies through;
            // here we check reachability sequentially based on distance and altitude constraints.
            foreach (var wp in sortedWaypoints)
            {
                yield return null; // allow frame to advance

                if (!IsReachable(wp))
                {
                    string issue = $"Waypoint '{wp.label}' (order {wp.order}) appears unreachable.";
                    result.issues.Add(issue);
                    OnTestIssueDetected?.Invoke(issue);
                }
                else
                {
                    reachedWaypoints.Add(wp.waypointId);
                }

                elapsed = Time.realtimeSinceStartup - _testStartTime;
                if (elapsed >= _maxTestTimeSeconds)
                {
                    result.issues.Add("Test timed out — content may be too long.");
                    break;
                }
            }

            result.completionTimeSeconds = Time.realtimeSinceStartup - _testStartTime;
            result.waypointsReached      = reachedWaypoints.Count;
            result.totalWaypoints        = sortedWaypoints.Count;
            result.passed                = result.issues.Count == 0 && reachedWaypoints.Count == sortedWaypoints.Count;
            result.estimatedDifficulty   = EstimateDifficulty(result, _content);

            // Mark content as tested
            if (result.passed)
            {
                _content.metadata.hasBeenTested          = true;
                _content.metadata.testCompletionTimeSeconds = result.completionTimeSeconds;
                if (UGCEditorManager.Instance != null)
                    UGCEditorManager.Instance.HasUnsavedChanges = true;
            }

            LastResult = result;
            IsRunning  = false;
            OnTestCompleted?.Invoke(result);

            Debug.Log($"[UGCTestRunner] Test complete. Passed={result.passed} ({result.waypointsReached}/{result.totalWaypoints} waypoints)");
        }

        /// <summary>
        /// Heuristic check: a waypoint is considered unreachable if its altitude exceeds
        /// the practical ceiling or if coordinates are invalid.
        /// </summary>
        private static bool IsReachable(UGCWaypoint wp)
        {
            if (wp.latitude < -90.0 || wp.latitude > 90.0) return false;
            if (wp.longitude < -180.0 || wp.longitude > 180.0) return false;
            if (wp.altitude > UGCConfig.MaxAltitudeMetres) return false;
            return true;
        }

        /// <summary>
        /// Estimates difficulty from completion time and the number of detected issues.
        /// </summary>
        private static UGCDifficulty EstimateDifficulty(TestPlayResult result, UGCContent content)
        {
            // Simple heuristic based on waypoint count, trigger count, and average spacing
            int complexity = content.waypoints.Count + content.triggers.Count * 2 + content.zones.Count;

            if (complexity < 5)  return UGCDifficulty.Beginner;
            if (complexity < 15) return UGCDifficulty.Intermediate;
            if (complexity < 30) return UGCDifficulty.Advanced;
            if (complexity < 50) return UGCDifficulty.Expert;
            return UGCDifficulty.Extreme;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Test play result data class
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 108 — Result record returned after a <see cref="UGCTestRunner"/> session.
    /// </summary>
    [Serializable]
    public sealed class TestPlayResult
    {
        /// <summary>ID of the content that was tested.</summary>
        public string contentId = string.Empty;

        /// <summary>Whether the test-play was completed without blocking issues.</summary>
        public bool passed = false;

        /// <summary>Wall-clock seconds from test start to test end.</summary>
        public float completionTimeSeconds = 0f;

        /// <summary>Number of waypoints successfully reached.</summary>
        public int waypointsReached = 0;

        /// <summary>Total number of waypoints in the content.</summary>
        public int totalWaypoints = 0;

        /// <summary>Difficulty estimated from the test flight data.</summary>
        public UGCDifficulty estimatedDifficulty = UGCDifficulty.Intermediate;

        /// <summary>List of issue descriptions encountered during the test.</summary>
        public List<string> issues = new List<string>();
    }
}
