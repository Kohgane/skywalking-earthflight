using System;
using System.Globalization;
using System.Collections;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton app-lifecycle manager. Survives scene loads via DontDestroyOnLoad.
    /// Tracks Active / Paused / Background / Quitting states, manages session
    /// counting, periodic auto-save, and coordinates with
    /// <see cref="NotificationManager"/> and <see cref="RatePromptManager"/>.
    /// </summary>
    public class AppLifecycleManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static AppLifecycleManager Instance { get; private set; }

        // ── App state ────────────────────────────────────────────────────────
        /// <summary>Represents the current lifecycle state of the application.</summary>
        public enum AppState { Active, Paused, Background, Quitting }

        /// <summary>Fired whenever the application transitions between lifecycle states.</summary>
        public static event Action<AppState> OnAppStateChanged;

        /// <summary>The current lifecycle state.</summary>
        public AppState CurrentState { get; private set; } = AppState.Active;

        // ── Session timing ───────────────────────────────────────────────────
        /// <summary>Time (realtimeSinceStartup) when the active session began.</summary>
        public float SessionStartTime { get; private set; }

        /// <summary>Seconds elapsed since the session started.</summary>
        public float CurrentSessionDuration => Time.realtimeSinceStartup - SessionStartTime;

        // ── Phase 14 — convenience property ─────────────────────────────────
        /// <summary>The application version string as reported by <see cref="Application.version"/>.</summary>
        public static string AppVersion => Application.version;

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Auto-Save")]
        [Tooltip("Seconds between periodic auto-saves. Set to 0 to disable.")]
        [SerializeField] private float autoSaveIntervalSeconds = 60f;

        [Header("Rate Prompt")]
        [Tooltip("Minimum active session seconds before CheckAndPrompt is called on resume.")]
        [SerializeField] private float minSessionSecForRatePrompt = 60f;

        // ── PlayerPrefs keys ─────────────────────────────────────────────────
        private const string KeySessionCount    = "SWEF_SessionCount";
        private const string KeyFirstLaunchDate = "SWEF_FirstLaunchDate";

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSession();
        }

        private void Start()
        {
            if (autoSaveIntervalSeconds > 0f)
                StartCoroutine(AutoSaveRoutine());
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CurrentState = AppState.Paused;
                OnAppStateChanged?.Invoke(AppState.Paused);

                // Capture duration before resetting timer
                float sessionDuration = CurrentSessionDuration;

                SessionTracker.Instance?.EndSession();
                SaveAll();
                SWEF.Notification.NotificationManager.Instance?.ScheduleReturnReminder(24f);
                Debug.Log("[SWEF] AppLifecycleManager: app paused.");
            }
            else
            {
                // Store how long the previous session ran before resetting the timer
                float prevDuration = CurrentSessionDuration;
                SessionStartTime = Time.realtimeSinceStartup;
                CurrentState = AppState.Active;

                SWEF.Notification.NotificationManager.Instance?.CancelAll();
                OnAppStateChanged?.Invoke(AppState.Active);

                // Only prompt after a meaningful prior session
                if (prevDuration >= minSessionSecForRatePrompt)
                    RatePromptManager.Instance?.CheckAndPrompt();

                Debug.Log("[SWEF] AppLifecycleManager: app resumed.");
            }
        }

        private void OnApplicationQuit()
        {
            CurrentState = AppState.Quitting;
            OnAppStateChanged?.Invoke(AppState.Quitting);

            SessionTracker.Instance?.EndSession();
            SaveAll();
            SWEF.Core.AnalyticsLogger.LogEvent("app_quit", CurrentSessionDuration.ToString("F1"));

#if UNITY_EDITOR
            // Phase 14 — save any dirty BuildConfig asset before quitting in the Editor
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<SWEF.Build.BuildConfig>(
                "Assets/SWEF/Config/SWEFBuildConfig.asset");
            if (config != null && UnityEditor.EditorUtility.IsDirty(config))
                UnityEditor.AssetDatabase.SaveAssets();
#endif

            Debug.Log("[SWEF] AppLifecycleManager: app quitting.");
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Initialises this session: increments session count, records first-launch
        /// date when absent, and resets the session timer.
        /// Called automatically by <see cref="BootManager"/> and internally on Awake.
        /// </summary>
        public void InitSession()
        {
            SessionStartTime = Time.realtimeSinceStartup;

            int count = PlayerPrefs.GetInt(KeySessionCount, 0) + 1;
            PlayerPrefs.SetInt(KeySessionCount, count);

            if (string.IsNullOrEmpty(PlayerPrefs.GetString(KeyFirstLaunchDate, "")))
                PlayerPrefs.SetString(KeyFirstLaunchDate,
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            PlayerPrefs.Save();
            Debug.Log($"[SWEF] AppLifecycleManager: session #{count} started.");
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void SaveAll()
        {
            SaveManager.Instance?.Save();
        }

        private IEnumerator AutoSaveRoutine()
        {
            var wait = new WaitForSecondsRealtime(autoSaveIntervalSeconds);
            while (true)
            {
                yield return wait;
                SaveAll();
                Debug.Log("[SWEF] AppLifecycleManager: auto-save triggered.");
            }
        }
    }
}
