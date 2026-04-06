using System;
using System.Globalization;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton that evaluates conditions for showing a native app-store
    /// review prompt and provides helpers for opening the store listing.
    /// Conditions required: ≥5 sessions, ≥30 min total flight time,
    /// ≥3 days since first launch, and the user has not yet rated the app.
    /// </summary>
    public class RatePromptManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static RatePromptManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Store IDs")]
        [Tooltip("Numeric App Store app ID (numbers only, e.g. 1234567890).")]
        [SerializeField] private string iosAppId = "";

        [Tooltip("Play Store package name.")]
        [SerializeField] private string androidPackageName = "com.kohgane.swef.earthflight";

        // ── PlayerPrefs keys ─────────────────────────────────────────────────
        private const string KeySessionCount    = "SWEF_SessionCount";
        private const string KeyFlightMinutes   = "SWEF_TotalFlightMinutes";
        private const string KeyHasRated        = "SWEF_HasRated";
        private const string KeyFirstLaunchDate = "SWEF_FirstLaunchDate";

        // ── Thresholds ───────────────────────────────────────────────────────
        private const int   MinSessions    = 5;
        private const float MinFlightMin   = 30f;
        private const int   MinDaysSince   = 3;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all rate-prompt conditions and, when all are met,
        /// attempts a native review request (falling back to opening the store URL).
        /// </summary>
        public void CheckAndPrompt()
        {
            if (!AllConditionsMet()) return;

            Debug.Log("[SWEF] RatePromptManager: all conditions met — requesting review.");

#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Google Play In-App Review API requires the Google Play Core library.
            // Fall back to opening the Play Store URL.
            OpenStoreReview();
#else
            OpenStoreReview();
#endif
        }

        /// <summary>
        /// Opens the platform-appropriate store listing in the device browser.
        /// </summary>
        public void OpenStoreReview()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(iosAppId))
                Application.OpenURL($"itms-apps://itunes.apple.com/app/id{iosAppId}?action=write-review");
            else
                Debug.LogWarning("[SWEF] RatePromptManager: iosAppId is not set.");
#elif UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL($"market://details?id={androidPackageName}");
#else
            Debug.Log($"[SWEF] RatePromptManager: stub — would open store for '{androidPackageName}'.");
#endif
        }

        /// <summary>
        /// Records that the user has rated the app, suppressing future prompts.
        /// </summary>
        public void MarkAsRated()
        {
            PlayerPrefs.SetInt(KeyHasRated, 1);
            PlayerPrefs.Save();
            Debug.Log("[SWEF] RatePromptManager: marked as rated.");
        }

        /// <summary>
        /// Clears all rate-prompt PlayerPrefs keys (for testing).
        /// </summary>
        public void ResetRateData()
        {
            PlayerPrefs.DeleteKey(KeySessionCount);
            PlayerPrefs.DeleteKey(KeyFlightMinutes);
            PlayerPrefs.DeleteKey(KeyHasRated);
            PlayerPrefs.DeleteKey(KeyFirstLaunchDate);
            PlayerPrefs.Save();
            Debug.Log("[SWEF] RatePromptManager: rate data reset.");
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private bool AllConditionsMet()
        {
            if (PlayerPrefs.GetInt(KeyHasRated, 0) != 0)
                return false;

            int sessionCount = PlayerPrefs.GetInt(KeySessionCount, 0);
            if (sessionCount < MinSessions)
                return false;

            float flightMinutes = PlayerPrefs.GetFloat(KeyFlightMinutes, 0f);
            if (flightMinutes < MinFlightMin)
                return false;

            string firstLaunchStr = PlayerPrefs.GetString(KeyFirstLaunchDate, "");
            if (string.IsNullOrEmpty(firstLaunchStr))
                return false;

            if (!DateTime.TryParse(firstLaunchStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime firstLaunch))
                return false;

            double daysSince = (DateTime.UtcNow - firstLaunch).TotalDays;
            if (daysSince < MinDaysSince)
                return false;

            return true;
        }
    }
}
