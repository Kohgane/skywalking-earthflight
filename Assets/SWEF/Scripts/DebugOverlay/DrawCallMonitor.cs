// DrawCallMonitor.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// MonoBehaviour that captures per-frame rendering statistics (draw calls,
    /// batches, triangles, vertices) and fires a budget-exceeded event when the
    /// draw-call count surpasses a configurable threshold.
    /// </summary>
    public class DrawCallMonitor : MonoBehaviour
    {
        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("Draw Call Monitor Configuration")]
        [Tooltip("Draw call count above which OnDrawCallBudgetExceeded is fired.")]
        [SerializeField] private int budgetThreshold = 300;
#endif

        #endregion

        #region Events

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// Fired when the draw-call count exceeds <see cref="budgetThreshold"/>.
        /// Parameters: current draw-call count, threshold.
        /// </summary>
        public event Action<int, int> OnDrawCallBudgetExceeded;
#endif

        #endregion

        #region Private State

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private RenderingStats _latest;
        private bool _budgetExceededLastFrame;
#endif

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // Unity exposes rendering stats via UnityStats, which requires
            // UnityEditor at runtime — we read via reflection in editor mode
            // and fall back to safe zeroes in standalone dev builds.
            CaptureStats();

            bool over = _latest.drawCalls > budgetThreshold;
            if (over && !_budgetExceededLastFrame)
                OnDrawCallBudgetExceeded?.Invoke(_latest.drawCalls, budgetThreshold);
            _budgetExceededLastFrame = over;
#endif
        }

        #endregion

        #region Public API

        /// <summary>Returns the rendering stats snapshot from the most recent frame.</summary>
        public RenderingStats GetCurrentStats() =>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _latest;
#else
            default;
#endif

        /// <summary>Gets or sets the draw-call budget threshold.</summary>
        public int BudgetThreshold
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            get => budgetThreshold;
            set => budgetThreshold = value;
#else
            get => 0;
            set { }
#endif
        }

        #endregion

        #region Private Helpers

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void CaptureStats()
        {
            // UnityStats is only available in the editor.
            // In development-build standalone we read what we can from
            // UnityEngine.Profiling.Profiler which doesn't expose draw calls
            // directly, so we leave those at 0 and only record what's available.
#if UNITY_EDITOR
            _latest = new RenderingStats
            {
                drawCalls    = UnityEditor.UnityStats.drawCalls,
                batches      = UnityEditor.UnityStats.batches,
                triangles    = UnityEditor.UnityStats.triangles,
                vertices     = UnityEditor.UnityStats.vertices,
                setPassCalls = UnityEditor.UnityStats.setPassCalls,
                shadowCasters = UnityEditor.UnityStats.shadowCasters
            };
#else
            // Development-build standalone: UnityStats is unavailable.
            // Populate with zeroes; external integrations may push values in.
            _latest = new RenderingStats();
#endif
        }
#endif

        #endregion
    }
}
