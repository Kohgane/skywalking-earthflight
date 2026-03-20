using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using SWEF.UI;

namespace SWEF.Core
{
    /// <summary>
    /// Boot scene entry point.
    /// Acquires GPS fix, stores it in SWEFSession, then loads the World scene.
    /// Integrates with LoadingScreen and ErrorHandler for user feedback.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string worldSceneName = "World";
        [SerializeField] private float locationInitTimeoutSec = 15f;

        [Header("UI")]
        [SerializeField] private LoadingScreen loadingScreen;

        [Header("Optional — Phase 6")]
        /// <summary>
        /// Optional reference to a <see cref="SplashScreen"/> in the same scene.
        /// When null the Boot flow proceeds exactly as before.
        /// </summary>
        [SerializeField] private SplashScreen splashScreen;

        [Header("Optional — Phase 8")]
        /// <summary>
        /// Optional reference to a <see cref="CrashReporter"/> for detecting previous session crashes.
        /// Auto-found if null.
        /// </summary>
        [SerializeField] private CrashReporter crashReporter;

        private IEnumerator Start()
        {
            Debug.Log("[SWEF] Boot sequence started — Phase 35: Save System & Cloud Sync");

            loadingScreen?.Show();

            SWEFSession.Clear();

            // Phase 13 — initialise app lifecycle and request notification permission
            var lifecycle = FindFirstObjectByType<AppLifecycleManager>();
            if (lifecycle != null)
                lifecycle.InitSession();

            SWEF.Notification.NotificationManager.Instance?.RequestPermission();

            // Phase 8 — check for previous crash
            if (crashReporter == null)
                crashReporter = FindFirstObjectByType<CrashReporter>();

            if (crashReporter != null)
            {
                string[] logs = crashReporter.GetCrashLogPaths();
                if (logs.Length > 0)
                    Debug.Log($"[SWEF] Previous crash detected: {logs[0]}");
            }

            loadingScreen?.SetStatus("Checking location services...");

            if (!Input.location.isEnabledByUser)
            {
                Debug.LogError("[SWEF] Location service disabled by user.");
                ErrorHandler.ShowGPSError();
                loadingScreen?.Hide();
                yield break;
            }

            Input.location.Start(desiredAccuracyInMeters: 10f, updateDistanceInMeters: 5f);

            loadingScreen?.SetProgress(0.3f);
            loadingScreen?.SetStatus("Acquiring GPS fix...");

            float t = locationInitTimeoutSec;
            while (Input.location.status == LocationServiceStatus.Initializing && t > 0f)
            {
                t -= Time.deltaTime;
                loadingScreen?.SetProgress(0.3f + 0.4f * (1f - t / locationInitTimeoutSec));
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogError($"[SWEF] Location service failed: {Input.location.status}");
                ErrorHandler.ShowGPSTimeoutError();
                loadingScreen?.Hide();
                yield break;
            }

            var d = Input.location.lastData;
            double lat = d.latitude;
            double lon = d.longitude;
            double alt = (double.IsNaN(d.altitude) || d.altitude <= 0) ? 30.0 : d.altitude;

            SWEFSession.Set(lat, lon, alt);
            Debug.Log($"[SWEF] GPS fix: lat={lat}, lon={lon}, alt={alt}");

            loadingScreen?.SetProgress(0.9f);
            loadingScreen?.SetStatus("Loading world...");

            yield return null; // one frame so progress update renders

            loadingScreen?.SetProgress(1f);

            // Phase 10 — warn if SaveManager is not present in the scene
            if (FindFirstObjectByType<SaveManager>() == null)
                Debug.Log("[SWEF] BootManager: SaveManager not found in scene — consider attaching SaveManager to a persistent GameObject.");

            // Phase 15 — XR detection
            if (SWEF.XR.XRPlatformDetector.IsXRActive)
            {
                Debug.Log($"[SWEF] XR device detected: {SWEF.XR.XRPlatformDetector.DeviceName}");
            }

            // Phase 16 — Accessibility initialization
            var accessibility = FindFirstObjectByType<SWEF.UI.AccessibilityController>();
            if (accessibility != null)
            {
                accessibility.ApplySavedSettings();
                Debug.Log("[SWEF] Accessibility settings applied");
            }

            // Phase 25 — Player profile initialization
            var playerProfile = FindFirstObjectByType<SWEF.Social.PlayerProfileManager>();
            if (playerProfile == null)
                Debug.Log("[SWEF] BootManager: PlayerProfileManager not found — add it to a persistent GameObject for global leaderboard support.");

            // Phase 26 — Performance Profiling initialization
            var profiler = FindFirstObjectByType<SWEF.Performance.PerformanceProfiler>();
            if (profiler != null)
                Debug.Log("[SWEF] Performance Profiler initialized");

            var adaptiveQuality = FindFirstObjectByType<SWEF.Performance.AdaptiveQualityController>();
            if (adaptiveQuality != null)
            {
                adaptiveQuality.AutoAdjustEnabled = PlayerPrefs.GetInt("SWEF_AdaptiveQuality", 1) == 1;
                Debug.Log($"[SWEF] Adaptive quality: {(adaptiveQuality.AutoAdjustEnabled ? "ON" : "OFF")}");
            }

            // Phase 27 — Procedural Terrain & LOD
            var terrainGen = FindFirstObjectByType<SWEF.Terrain.ProceduralTerrainGenerator>();
            if (terrainGen != null)
                Debug.Log("[SWEF] ProceduralTerrainGenerator found — terrain system active");

            // Phase 28 — Spatial Audio Engine
            var spatialAudio = FindFirstObjectByType<SWEF.Audio.SpatialAudioManager>();
            if (spatialAudio != null)
                Debug.Log("[SWEF] SpatialAudioManager found — spatial audio engine active");

            // Phase 29 — Cloud rendering initialization
            var cloudRendering = FindFirstObjectByType<SWEF.CloudRendering.CloudRenderingManager>();
            if (cloudRendering != null)
            {
                cloudRendering.Initialize();
                Debug.Log("[SWEF] Cloud rendering system initialized");
            }

            // Phase 30 — Localization initialization
            var localization = FindFirstObjectByType<SWEF.Localization.LocalizationManager>();
            if (localization != null)
            {
                localization.Initialize();
                Debug.Log($"[SWEF] Localization initialized — language: {localization.CurrentLanguage}");
            }

            // Phase 31 — Achievement system initialization
            var achievementMgr = FindFirstObjectByType<SWEF.Achievement.AchievementManager>();
            if (achievementMgr != null)
                Debug.Log($"[SWEF] Achievement system loaded: {achievementMgr.GetAllStates().Count} achievements tracked");

            // Phase 32 — Weather system initialization
            var weatherManager = FindFirstObjectByType<SWEF.Weather.WeatherManager>();
            if (weatherManager != null)
                Debug.Log("[SWEF] WeatherManager found — Phase 32 weather system active");

            var weatherAPIClient = FindFirstObjectByType<SWEF.Weather.WeatherAPIClient>();
            if (weatherAPIClient != null)
                Debug.Log("[SWEF] WeatherAPIClient found — live weather fetch enabled");
            else
                Debug.Log("[SWEF] BootManager: WeatherAPIClient not found — add it for live weather data.");

            // Phase 34 — Accessibility system initialization
            var accessibilityMgr = FindFirstObjectByType<SWEF.Accessibility.AccessibilityManager>();
            if (accessibilityMgr != null)
            {
                accessibilityMgr.LoadProfile();
                Debug.Log($"[SWEF] AccessibilityManager loaded — preset: {accessibilityMgr.Profile.activePreset}");
            }

            var screenReader = FindFirstObjectByType<SWEF.Accessibility.ScreenReaderBridge>();
            if (screenReader != null)
                Debug.Log("[SWEF] ScreenReaderBridge found — TTS system active");

            var colorblindFilter = FindFirstObjectByType<SWEF.Accessibility.ColorblindFilter>();
            if (colorblindFilter != null)
                Debug.Log("[SWEF] ColorblindFilter found — colorblind assistance active");

            var subtitleSystem = FindFirstObjectByType<SWEF.Accessibility.SubtitleSystem>();
            if (subtitleSystem != null)
                Debug.Log("[SWEF] SubtitleSystem found — subtitles/captions active");

            var uiScaling = FindFirstObjectByType<SWEF.Accessibility.UIScalingSystem>();
            if (uiScaling != null)
                Debug.Log($"[SWEF] UIScalingSystem found — scale: {uiScaling.GlobalScale:F2}×");

            var hapticA11y = FindFirstObjectByType<SWEF.Accessibility.HapticAccessibility>();
            if (hapticA11y != null)
                Debug.Log("[SWEF] HapticAccessibility found — extended haptic patterns active");

            var adaptiveInput = FindFirstObjectByType<SWEF.Accessibility.AdaptiveInputManager>();
            if (adaptiveInput != null)
                Debug.Log($"[SWEF] AdaptiveInputManager found — input mode: {adaptiveInput.CurrentMode}");

            var cogAssist = FindFirstObjectByType<SWEF.Accessibility.CognitiveAssistSystem>();
            if (cogAssist != null)
                Debug.Log($"[SWEF] CognitiveAssistSystem found — game speed: {cogAssist.GameSpeed:F2}×");

            // Phase 35 — Save system initialization
            var saveSystemMgr = FindFirstObjectByType<SWEF.SaveSystem.SaveManager>();
            if (saveSystemMgr != null)
            {
                saveSystemMgr.DiscoverSaveables();
                Debug.Log("[SWEF] SaveSystem.SaveManager found — multi-slot save system active");
            }
            else
            {
                Debug.Log("[SWEF] BootManager: SaveSystem.SaveManager not found — add it to a persistent GameObject for full save system support.");
            }

            var saveMigration = FindFirstObjectByType<SWEF.SaveSystem.SaveMigrationSystem>();
            if (saveMigration != null)
                Debug.Log("[SWEF] SaveMigrationSystem found — save format migration active");

            var saveIntegrity = FindFirstObjectByType<SWEF.SaveSystem.SaveIntegrityChecker>();
            if (saveIntegrity != null)
            {
                bool hasCorruption = saveIntegrity.ScanAllSlots();
                if (hasCorruption)
                    Debug.LogWarning("[SWEF] SaveIntegrityChecker: one or more save slots are corrupted.");
            }

            var cloudSync = FindFirstObjectByType<SWEF.SaveSystem.CloudSyncManager>();
            if (cloudSync != null)
                Debug.Log($"[SWEF] CloudSyncManager found — cloud sync {(cloudSync.IsConfigured ? "configured" : "not configured")}");

            var conflictResolver = FindFirstObjectByType<SWEF.SaveSystem.SaveConflictResolver>();
            if (conflictResolver != null)
                Debug.Log("[SWEF] SaveConflictResolver found — conflict resolution active");

            var exportImport = FindFirstObjectByType<SWEF.SaveSystem.SaveExportImport>();
            if (exportImport != null)
                Debug.Log("[SWEF] SaveExportImport found — export/import feature active");

            SceneManager.LoadScene(worldSceneName);
            Debug.Log($"[SWEF] Scene load requested: {worldSceneName}");

            // Phase 22 — Offline mode initialization
            var offlineManager = FindFirstObjectByType<SWEF.Offline.OfflineManager>();
            if (offlineManager != null)
            {
                Debug.Log($"[SWEF] Offline mode: {(offlineManager.IsOffline ? "OFFLINE" : "ONLINE")}");
            }
        }
    }
}
