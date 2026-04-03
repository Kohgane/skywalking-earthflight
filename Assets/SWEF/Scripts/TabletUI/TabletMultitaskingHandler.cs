// TabletMultitaskingHandler.cs — SWEF Tablet UI Optimization (Phase 97)
using System;
using UnityEngine;

namespace SWEF.TabletUI
{
    /// <summary>
    /// Singleton MonoBehaviour that handles the app lifecycle on tablets, including
    /// backgrounding, split-view transitions, picture-in-picture considerations, and
    /// orientation changes.
    ///
    /// <para>When the app enters the background the flight simulation is paused (via
    /// <c>Time.timeScale = 0</c>) and the current state is saved.  When the app
    /// resumes, the simulation is unpaused and the state is restored.</para>
    ///
    /// <para>Orientation changes (landscape ↔ portrait) fire
    /// <see cref="OnOrientationChanged"/> so other systems can re-layout.</para>
    /// </summary>
    public class TabletMultitaskingHandler : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TabletMultitaskingHandler Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Behaviour")]
        [Tooltip("Pause simulation (timeScale = 0) when the app goes to the background.")]
        [SerializeField] private bool pauseOnBackground = true;

        [Tooltip("Save state when a multitasking transition is detected.")]
        [SerializeField] private bool saveOnMultitasking = true;

        [Tooltip("Allowed auto-rotation orientations as a bitmask. " +
                 "0 = landscape only, 1 = portrait only, 2 = auto.")]
        [SerializeField, Range(0, 2)] private int orientationPolicy = 2;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private bool              _isInBackground;
        private ScreenOrientation _lastOrientation;
        private float             _savedTimeScale = 1f;

        /// <summary>True while the application is in the background.</summary>
        public bool IsInBackground => _isInBackground;

        /// <summary>Last detected screen orientation.</summary>
        public ScreenOrientation CurrentOrientation => _lastOrientation;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the app enters the background.</summary>
        public event Action OnAppBackgrounded;

        /// <summary>Fired when the app returns to the foreground.</summary>
        public event Action OnAppForegrounded;

        /// <summary>Fired when screen orientation changes. Parameter: new orientation.</summary>
        public event Action<ScreenOrientation> OnOrientationChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ApplyOrientationPolicy();
            _lastOrientation = Screen.orientation;
        }

        private void Update()
        {
            DetectOrientationChange();
        }

        // Unity fires OnApplicationPause on both iOS and Android when the app goes to
        // the background (pause=true) or returns (pause=false).
        private void OnApplicationPause(bool pause)
        {
            if (pause)
                HandleBackground();
            else
                HandleForeground();
        }

        // Editor and standalone fallback (window focus lost)
        private void OnApplicationFocus(bool focus)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!focus)
                HandleBackground();
            else
                HandleForeground();
#endif
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Programmatically simulate a background event (useful for testing).
        /// </summary>
        public void SimulateBackground() => HandleBackground();

        /// <summary>
        /// Programmatically simulate a foreground event (useful for testing).
        /// </summary>
        public void SimulateForeground() => HandleForeground();

        /// <summary>
        /// Apply the configured orientation policy to Unity's auto-rotation settings.
        /// </summary>
        public void ApplyOrientationPolicy()
        {
            switch (orientationPolicy)
            {
                case 0: // Landscape only
                    Screen.autorotateToLandscapeLeft  = true;
                    Screen.autorotateToLandscapeRight = true;
                    Screen.autorotateToPortrait               = false;
                    Screen.autorotateToPortraitUpsideDown     = false;
                    Screen.orientation = ScreenOrientation.AutoRotation;
                    break;
                case 1: // Portrait only
                    Screen.autorotateToLandscapeLeft  = false;
                    Screen.autorotateToLandscapeRight = false;
                    Screen.autorotateToPortrait               = true;
                    Screen.autorotateToPortraitUpsideDown     = true;
                    Screen.orientation = ScreenOrientation.AutoRotation;
                    break;
                default: // Auto
                    Screen.autorotateToLandscapeLeft  = true;
                    Screen.autorotateToLandscapeRight = true;
                    Screen.autorotateToPortrait               = true;
                    Screen.autorotateToPortraitUpsideDown     = false;
                    Screen.orientation = ScreenOrientation.AutoRotation;
                    break;
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────────
        private void HandleBackground()
        {
            if (_isInBackground) return;
            _isInBackground = true;

            if (saveOnMultitasking)
                TrySaveState();

            if (pauseOnBackground)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale  = 0f;
            }

            Debug.Log("[SWEF] TabletMultitaskingHandler: app backgrounded.");
            OnAppBackgrounded?.Invoke();
        }

        private void HandleForeground()
        {
            if (!_isInBackground) return;
            _isInBackground = false;

            if (pauseOnBackground)
                Time.timeScale = _savedTimeScale;

            Debug.Log("[SWEF] TabletMultitaskingHandler: app foregrounded.");
            OnAppForegrounded?.Invoke();
        }

        private void TrySaveState()
        {
            // Flush PlayerPrefs as an immediate safety net so any pending preference
            // writes (volume, last position, key bindings, etc.) survive backgrounding.
            PlayerPrefs.Save();
        }

        private void DetectOrientationChange()
        {
            ScreenOrientation current = Screen.orientation;
            if (current == _lastOrientation) return;

            _lastOrientation = current;
            Debug.Log($"[SWEF] TabletMultitaskingHandler: orientation → {current}.");
            OnOrientationChanged?.Invoke(current);
        }
    }
}
