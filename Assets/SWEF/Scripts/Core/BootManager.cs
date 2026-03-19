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
            Debug.Log("[SWEF] Boot sequence started — Phase 13: Notifications, Rate Prompt & App Lifecycle");

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
