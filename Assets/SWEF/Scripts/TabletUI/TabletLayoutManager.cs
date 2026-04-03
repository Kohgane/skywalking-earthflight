// TabletLayoutManager.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>Screen-size category used for layout switching.</summary>
    public enum LayoutMode
    {
        /// <summary>Phone screens smaller than 7 inches.</summary>
        Compact,
        /// <summary>Tablet screens between 7 and 13 inches.</summary>
        Standard,
        /// <summary>Desktop/large-screen displays above 13 inches.</summary>
        Expanded
    }

    /// <summary>
    /// Singleton MonoBehaviour that detects the physical screen size at runtime and
    /// manages layout switching between Compact / Standard / Expanded modes.
    ///
    /// <para>Detection uses <c>Screen.dpi</c> together with <c>Screen.width</c> and
    /// <c>Screen.height</c> to compute the diagonal in inches.  Breakpoints:</para>
    /// <list type="bullet">
    ///   <item><description>Phone  — diagonal &lt; 7″  → <see cref="LayoutMode.Compact"/></description></item>
    ///   <item><description>Tablet — 7″ ≤ diagonal ≤ 13″ → <see cref="LayoutMode.Standard"/></description></item>
    ///   <item><description>Desktop — diagonal &gt; 13″ → <see cref="LayoutMode.Expanded"/></description></item>
    /// </list>
    /// <para>Re-evaluates whenever the screen resolution changes (e.g. orientation flip
    /// or split-view resize).</para>
    /// </summary>
    public class TabletLayoutManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletLayoutManager Instance { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        private const float BreakpointPhoneMaxInches   = 7f;
        private const float BreakpointTabletMaxInches  = 13f;
        /// <summary>Fallback DPI used when the platform reports 0 or an obviously wrong value.</summary>
        private const float FallbackDpi = 96f;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Override")]
        [Tooltip("Force a specific layout mode regardless of detected screen size. " +
                 "Set to a negative value (-1) to use auto-detection.")]
        [SerializeField] private int forceLayoutMode = -1;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private LayoutMode _currentMode;
        private int        _lastWidth;
        private int        _lastHeight;

        /// <summary>Currently active layout mode.</summary>
        public LayoutMode CurrentMode => _currentMode;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the layout mode changes, passing the new <see cref="LayoutMode"/>.</summary>
        public event Action<LayoutMode> OnLayoutModeChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _lastWidth  = Screen.width;
            _lastHeight = Screen.height;
            _currentMode = DetectLayoutMode();
            Debug.Log($"[SWEF] TabletLayoutManager: initial mode '{_currentMode}' " +
                      $"(screen {Screen.width}×{Screen.height}, dpi {Screen.dpi:F1}).");
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                _lastWidth  = Screen.width;
                _lastHeight = Screen.height;
                EvaluateLayoutMode();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the physical diagonal of the screen in inches.
        /// Falls back to <c>96 dpi</c> when <c>Screen.dpi</c> reports an invalid value.
        /// </summary>
        public static float GetScreenDiagonalInches()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : FallbackDpi;
            float widthInches  = Screen.width  / dpi;
            float heightInches = Screen.height / dpi;
            return Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);
        }

        /// <summary>
        /// Forces a re-evaluation of the current layout mode and fires
        /// <see cref="OnLayoutModeChanged"/> if the mode has changed.
        /// Call this after manually resizing the window in the Unity Editor.
        /// </summary>
        public void ForceRefresh() => EvaluateLayoutMode();

        // ── Internal ──────────────────────────────────────────────────────────────
        private void EvaluateLayoutMode()
        {
            LayoutMode newMode = DetectLayoutMode();
            if (newMode == _currentMode) return;

            _currentMode = newMode;
            Debug.Log($"[SWEF] TabletLayoutManager: mode changed to '{_currentMode}'.");
            OnLayoutModeChanged?.Invoke(_currentMode);
        }

        private LayoutMode DetectLayoutMode()
        {
            // Manual override (editor/debug use)
            if (forceLayoutMode >= 0 && System.Enum.IsDefined(typeof(LayoutMode), forceLayoutMode))
                return (LayoutMode)forceLayoutMode;

            float diagonal = GetScreenDiagonalInches();

            if (diagonal < BreakpointPhoneMaxInches)   return LayoutMode.Compact;
            if (diagonal <= BreakpointTabletMaxInches) return LayoutMode.Standard;
            return LayoutMode.Expanded;
        }
    }
}
