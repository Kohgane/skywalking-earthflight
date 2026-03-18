using UnityEngine;

namespace SWEF.Notification
{
    /// <summary>
    /// Data container for notification channel configuration (Android/iOS).
    /// Attach as a component or reference from <see cref="NotificationManager"/>.
    /// </summary>
    public class NotificationSettings : MonoBehaviour
    {
        [Header("Channel Identity")]
        [Tooltip("Unique channel ID used on Android O+.")]
        public string channelId          = "swef_default";

        [Tooltip("Human-readable channel name shown in system settings.")]
        public string channelName        = "SWEF Notifications";

        [Tooltip("Channel description shown in system settings.")]
        public string channelDescription = "Flight reminders and challenges";

        [Header("Android Icons")]
        [Tooltip("Small notification icon resource name (must exist in the Android drawable folder).")]
        public string smallIconId        = "swef_icon_small";

        [Tooltip("Large notification icon resource name (optional).")]
        public string largeIconId        = "swef_icon_large";
    }
}
