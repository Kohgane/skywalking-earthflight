// DebugOverlayController.cs — SWEF Performance Profiler & Debug Overlay
using System;
using UnityEngine;

namespace SWEF.DebugOverlay
{
    /// <summary>
    /// Singleton MonoBehaviour that acts as the master controller for the debug
    /// overlay.  Aggregates data from <see cref="FPSCounter"/>,
    /// <see cref="MemoryProfiler"/>, and <see cref="DrawCallMonitor"/> and exposes
    /// a unified API for toggling visibility, cycling display modes, and obtaining
    /// a combined snapshot.
    /// </summary>
    public class DebugOverlayController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static DebugOverlayController Instance { get; private set; }

        #endregion

        #region Inspector Fields

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        [Header("Profile")]
        [Tooltip("Optional ScriptableObject profile.  If null, built-in defaults are used.")]
        [SerializeField] private DebugOverlayProfile profile;

        [Header("Toggle Hotkey")]
        [Tooltip("Keyboard key that toggles the overlay on/off.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("References")]
        [Tooltip("FPSCounter component (auto-found on same GameObject if null).")]
        [SerializeField] private FPSCounter fpsCounter;

        [Tooltip("MemoryProfiler component (auto-found on same GameObject if null).")]
        [SerializeField] private MemoryProfiler memoryProfiler;

        [Tooltip("DrawCallMonitor component (auto-found on same GameObject if null).")]
        [SerializeField] private DrawCallMonitor drawCallMonitor;

        [Header("State")]
        [Tooltip("Whether the overlay starts visible.")]
        [SerializeField] private bool startVisible = true;

        [Tooltip("Initial display mode.")]
        [SerializeField] private OverlayDisplayMode initialMode = OverlayDisplayMode.Standard;
#endif

        #endregion

        #region Events

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Fired whenever the overlay is toggled.  Parameter: new visible state.</summary>
        public event Action<bool> OnOverlayToggled;

        /// <summary>Fired whenever the display mode changes.</summary>
        public event Action<OverlayDisplayMode> OnDisplayModeChanged;
#endif

        #endregion

        #region Public Properties

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Whether the overlay is currently visible.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>Current display mode.</summary>
        public OverlayDisplayMode DisplayMode { get; private set; }

        /// <summary>Current overlay position.</summary>
        public OverlayPosition Position { get; private set; }
#endif

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (fpsCounter     == null) fpsCounter     = GetComponent<FPSCounter>();
            if (memoryProfiler == null) memoryProfiler = GetComponent<MemoryProfiler>();
            if (drawCallMonitor == null) drawCallMonitor = GetComponent<DrawCallMonitor>();

            // Apply profile defaults
            if (profile != null)
            {
                initialMode = profile.defaultDisplayMode;
                Position    = profile.overlayPosition;
            }

            DisplayMode = initialMode;
            IsVisible   = startVisible;
#endif
        }

        private void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Input.GetKeyDown(toggleKey))
                ToggleOverlay();
#endif
        }

        #endregion

        #region Public API

        /// <summary>Toggles overlay visibility on/off.</summary>
        public void ToggleOverlay()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            IsVisible = !IsVisible;
            OnOverlayToggled?.Invoke(IsVisible);
#endif
        }

        /// <summary>Sets the display mode explicitly.</summary>
        /// <param name="mode">Target display mode.</param>
        public void SetDisplayMode(OverlayDisplayMode mode)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (DisplayMode == mode) return;
            DisplayMode = mode;
            OnDisplayModeChanged?.Invoke(mode);
#endif
        }

        /// <summary>Cycles to the next display mode in the enum order.</summary>
        public void CycleDisplayMode()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int next = ((int)DisplayMode + 1) % Enum.GetValues(typeof(OverlayDisplayMode)).Length;
            SetDisplayMode((OverlayDisplayMode)next);
#endif
        }

        /// <summary>Sets the screen-corner anchor for the overlay panel.</summary>
        /// <param name="position">Target overlay position.</param>
        public void SetOverlayPosition(OverlayPosition position)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Position = position;
#endif
        }

        /// <summary>
        /// Returns a <see cref="DebugOverlaySnapshot"/> combining data from all
        /// active monitors.
        /// </summary>
        public DebugOverlaySnapshot GetFullSnapshot()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            MemorySnapshot mem     = memoryProfiler  != null ? memoryProfiler.GetCurrentSnapshot()   : default;
            RenderingStats render  = drawCallMonitor != null ? drawCallMonitor.GetCurrentStats()      : default;

            float curFPS  = fpsCounter != null ? fpsCounter.GetCurrentFPS()  : 0f;
            float avgFPS  = fpsCounter != null ? fpsCounter.GetAverageFPS()  : 0f;
            float minFPS  = fpsCounter != null ? fpsCounter.GetMinFPS()      : 0f;
            float maxFPS  = fpsCounter != null ? fpsCounter.GetMaxFPS()      : 0f;
            float ftMs    = fpsCounter != null ? fpsCounter.GetFrameTimeMs() : 0f;
            PerformanceThreshold state = fpsCounter != null
                ? fpsCounter.GetCurrentState()
                : PerformanceThreshold.Good;

            return new DebugOverlaySnapshot
            {
                currentFPS       = curFPS,
                averageFPS       = avgFPS,
                minFPS           = minFPS,
                maxFPS           = maxFPS,
                frameTimeMs      = ftMs,
                memory           = mem,
                rendering        = render,
                performanceState = state,
                timestamp        = DateTime.UtcNow
            };
#else
            return default;
#endif
        }

        #endregion
    }
}
