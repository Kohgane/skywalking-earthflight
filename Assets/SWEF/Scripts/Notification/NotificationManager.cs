using System;
using UnityEngine;

namespace SWEF.Notification
{
    /// <summary>
    /// Singleton that schedules and cancels local mobile notifications.
    /// Compiles without the Unity Mobile Notifications package by using
    /// preprocessor guards — platform-specific calls are isolated inside
    /// <c>#if UNITY_ANDROID</c> / <c>#if UNITY_IOS</c> blocks with a
    /// no-op fallback for the editor and other platforms.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static NotificationManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] bool notificationsEnabled = true;

        [Tooltip("Optional channel settings. Uses built-in defaults when null.")]
        [SerializeField] private NotificationSettings settings;

        // ── PlayerPrefs key ──────────────────────────────────────────────────
        private const string KeyEnabled = "SWEF_NotificationsEnabled";

        // ── Notification IDs ─────────────────────────────────────────────────
        private const int IdReturnReminder      = 1001;
        private const int IdDailyChallenge      = 1002;
        private const int IdAchievementReminder = 1003;

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

            // Prefer SettingsManager as the authoritative source; fall back to PlayerPrefs
            var sm = FindFirstObjectByType<Settings.SettingsManager>();
            notificationsEnabled = sm != null
                ? sm.NotificationsEnabled
                : PlayerPrefs.GetInt(KeyEnabled, 1) == 1;

            if (sm != null)
                sm.OnNotificationSettingChanged += OnSettingChanged;
        }

        private void OnDestroy()
        {
            var sm = FindFirstObjectByType<Settings.SettingsManager>();
            if (sm != null)
                sm.OnNotificationSettingChanged -= OnSettingChanged;
        }

        private void OnSettingChanged(bool enabled)
        {
            notificationsEnabled = enabled;
            if (!enabled) CancelAll();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // App going to background — schedule a return reminder for 24 h
                ScheduleReturnReminder(24f);
            }
            else
            {
                // App returning to foreground — clear pending notifications
                CancelAll();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Requests notification permission from the OS.
        /// On Android this is handled automatically by the manifest / runtime;
        /// on iOS a system permission dialog is shown.
        /// </summary>
        public void RequestPermission()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Unity.Notifications.iOS.iOSNotificationCenter.RequestAuthorizationAsync(
                Unity.Notifications.iOS.AuthorizationOption.Alert |
                Unity.Notifications.iOS.AuthorizationOption.Badge |
                Unity.Notifications.iOS.AuthorizationOption.Sound);
#else
            Debug.Log("[SWEF] NotificationManager: RequestPermission — no-op on this platform.");
#endif
        }

        /// <summary>
        /// Schedules a "return to the app" reminder notification.
        /// </summary>
        /// <param name="hoursFromNow">Delay in hours before the notification fires.</param>
        public void ScheduleReturnReminder(float hoursFromNow)
        {
            if (!notificationsEnabled) return;
            ScheduleNotification(
                id: IdReturnReminder,
                title: "Skywalking: Earth Flight",
                body: "It's been a while! Your flight awaits ✈️",
                fireAt: DateTime.Now.AddHours(hoursFromNow));
        }

        /// <summary>
        /// Schedules a daily challenge notification at a specific local time.
        /// </summary>
        /// <param name="hour">Hour (0–23) in local time.</param>
        /// <param name="minute">Minute (0–59) in local time.</param>
        public void ScheduleDailyChallenge(int hour, int minute)
        {
            if (!notificationsEnabled) return;

            // Calculate the next occurrence of the requested local time
            DateTime now  = DateTime.Now;
            DateTime fire = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (fire <= now)
                fire = fire.AddDays(1);

            ScheduleNotification(
                id: IdDailyChallenge,
                title: "SWEF — Daily Challenge",
                body: "New daily challenge available! 🌍",
                fireAt: fire);
        }

        /// <summary>
        /// Schedules an achievement-proximity reminder notification.
        /// </summary>
        /// <param name="hoursFromNow">Delay in hours before the notification fires.</param>
        public void ScheduleAchievementReminder(float hoursFromNow)
        {
            if (!notificationsEnabled) return;
            ScheduleNotification(
                id: IdAchievementReminder,
                title: "SWEF — Achievement Alert",
                body: "You're close to unlocking a new achievement! 🏆",
                fireAt: DateTime.Now.AddHours(hoursFromNow));
        }

        /// <summary>
        /// Cancels all pending notifications scheduled by this manager.
        /// </summary>
        public void CancelAll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Unity.Notifications.Android.AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
            Unity.Notifications.iOS.iOSNotificationCenter.RemoveAllScheduledNotifications();
            Unity.Notifications.iOS.iOSNotificationCenter.RemoveAllDeliveredNotifications();
#else
            Debug.Log("[SWEF] NotificationManager: CancelAll — no-op on this platform.");
#endif
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private void ScheduleNotification(int id, string title, string body, DateTime fireAt)
        {
            string channelId = settings != null ? settings.channelId : "swef_default";
            string smallIcon = settings != null ? settings.smallIconId : "swef_icon_small";
            string largeIcon = settings != null ? settings.largeIconId : "swef_icon_large";

#if UNITY_ANDROID && !UNITY_EDITOR
            var notification = new Unity.Notifications.Android.AndroidNotification
            {
                Title        = title,
                Text         = body,
                FireTime     = fireAt,
                SmallIcon    = smallIcon,
                LargeIcon    = largeIcon,
            };
            Unity.Notifications.Android.AndroidNotificationCenter.CancelNotification(id);
            Unity.Notifications.Android.AndroidNotificationCenter.SendNotificationWithExplicitID(
                notification, channelId, id);
            Debug.Log($"[SWEF] NotificationManager: Android notification #{id} scheduled for {fireAt}");
#elif UNITY_IOS && !UNITY_EDITOR
            var trigger = new Unity.Notifications.iOS.iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = fireAt > DateTime.Now ? fireAt - DateTime.Now : TimeSpan.FromSeconds(1),
                Repeats      = false,
            };
            var notification = new Unity.Notifications.iOS.iOSNotification
            {
                Identifier  = id.ToString(),
                Title       = title,
                Body        = body,
                Trigger     = trigger,
                ShowInForeground = false,
            };
            Unity.Notifications.iOS.iOSNotificationCenter.RemoveScheduledNotification(id.ToString());
            Unity.Notifications.iOS.iOSNotificationCenter.ScheduleNotification(notification);
            Debug.Log($"[SWEF] NotificationManager: iOS notification #{id} scheduled for {fireAt}");
#else
            Debug.Log($"[SWEF] NotificationManager: stub — would schedule '{title}' at {fireAt}");
#endif
        }
    }
}
