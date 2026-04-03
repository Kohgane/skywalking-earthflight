// SplitViewSupport.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that detects iPad Split View and Android
    /// multi-window / freeform mode by monitoring resolution changes at runtime.
    ///
    /// <para>When the app window width drops below
    /// <see cref="SplitViewWidthThreshold"/> of the full screen width, the manager
    /// considers the app to be in split-view and fires
    /// <see cref="OnSplitViewChanged"/>.</para>
    ///
    /// <para>A minimum usable width (<see cref="MinUsableWidthPx"/>) is enforced;
    /// UI consumers should hide non-essential elements when
    /// <see cref="IsBelowMinimumSize"/> is true.</para>
    /// </summary>
    public class SplitViewSupport : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SplitViewSupport Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Thresholds")]
        [Tooltip("Width ratio (0–1) below which the app is considered to be in split view.")]
        [SerializeField, Range(0.1f, 0.95f)]
        private float splitViewWidthThreshold = 0.75f;

        [Tooltip("Minimum usable window width in pixels. " +
                 "Below this value IsBelowMinimumSize is set to true.")]
        [SerializeField] private int minUsableWidthPx = 320;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private int   _lastWidth;
        private int   _lastHeight;
        private bool  _isSplitView;
        private float _widthRatio = 1f;

        /// <summary>Expose the split-view threshold for external systems.</summary>
        public float SplitViewWidthThreshold => splitViewWidthThreshold;

        /// <summary>Minimum usable width in pixels.</summary>
        public int MinUsableWidthPx => minUsableWidthPx;

        /// <summary>Whether the app is currently in split-view / multi-window mode.</summary>
        public bool IsSplitView => _isSplitView;

        /// <summary>
        /// Ratio of the current window width to the full screen width (0–1).
        /// 1.0 means full screen.
        /// </summary>
        public float WidthRatio => _widthRatio;

        /// <summary>
        /// True when the current window width is below the minimum usable threshold.
        /// UI systems should collapse non-essential panels when this is true.
        /// </summary>
        public bool IsBelowMinimumSize => Screen.width < minUsableWidthPx;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when split-view mode changes.
        /// Parameters: <c>isSplitView</c> — new split-view state;
        ///             <c>widthRatio</c> — new window-to-screen width ratio.
        /// </summary>
        public event Action<bool, float> OnSplitViewChanged;

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
            Evaluate(force: true);
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                _lastWidth  = Screen.width;
                _lastHeight = Screen.height;
                Evaluate(force: false);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Force an immediate re-evaluation of the split-view state.</summary>
        public void ForceRefresh() => Evaluate(force: true);

        // ── Internal ──────────────────────────────────────────────────────────────
        private void Evaluate(bool force)
        {
            float fullWidth = GetNativeScreenWidth();
            float ratio     = fullWidth > 0f ? Screen.width / fullWidth : 1f;
            ratio = Mathf.Clamp01(ratio);

            bool splitView = ratio < splitViewWidthThreshold;

            if (!force && splitView == _isSplitView && Mathf.Approximately(ratio, _widthRatio))
                return;

            _isSplitView = splitView;
            _widthRatio  = ratio;

            Debug.Log($"[SWEF] SplitViewSupport: isSplitView={_isSplitView}, widthRatio={_widthRatio:F2}");
            OnSplitViewChanged?.Invoke(_isSplitView, _widthRatio);
        }

        /// <summary>
        /// Returns the native full-screen width.  On platforms where the window can be
        /// resized we fall back to the current display resolution.
        /// </summary>
        private static float GetNativeScreenWidth()
        {
#if UNITY_IOS || UNITY_ANDROID
            // On mobile the display resolution IS the native resolution
            return Display.main.systemWidth > 0
                ? Display.main.systemWidth
                : Screen.currentResolution.width;
#else
            return Screen.currentResolution.width;
#endif
        }
    }
}
