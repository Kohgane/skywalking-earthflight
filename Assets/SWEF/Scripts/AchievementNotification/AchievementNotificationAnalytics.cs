// AchievementNotificationAnalytics.cs — SWEF Achievement Notification & Popup System

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// Snapshot of notification analytics data at a point in time.
    /// Returned by <see cref="AchievementNotificationAnalytics.GetNotificationAnalyticsSummary"/>.
    /// </summary>
    [System.Serializable]
    public struct NotificationAnalyticsSummary
    {
        /// <summary>Total number of notifications shown in this session.</summary>
        public int totalShown;

        /// <summary>Number of notifications dismissed before the auto-dismiss timer expired.</summary>
        public int dismissedEarly;

        /// <summary>Average time (seconds) a notification was visible before dismissal.</summary>
        public float averageViewTime;

        /// <summary>The tier that has been unlocked most frequently this session.</summary>
        public AchievementTier mostCommonTier;
    }

    /// <summary>
    /// Static utility class that tracks in-session achievement notification analytics.
    ///
    /// <para>No MonoBehaviour or persistent state is required; all data lives in
    /// static fields and is reset when the process restarts.</para>
    ///
    /// <para>Call <see cref="RecordNotificationShown"/> when a notification begins
    /// displaying, and <see cref="RecordNotificationDismissed"/> when it is dismissed
    /// (either by the auto-timer or by the user).
    /// Retrieve a snapshot via <see cref="GetNotificationAnalyticsSummary"/>.</para>
    /// </summary>
    public static class AchievementNotificationAnalytics
    {
        #region Tracked data

        private static int   _totalShown;
        private static int   _dismissedEarly;
        private static float _totalViewTime;
        private static int   _dismissedCount;

        private static readonly Dictionary<AchievementTier, int> _tierCounts =
            new Dictionary<AchievementTier, int>();

        #endregion

        #region Public API

        /// <summary>
        /// Records that a notification was shown.
        /// Should be called every time <see cref="NotificationQueueManager"/> begins
        /// displaying a notification.
        /// </summary>
        /// <param name="info">The achievement that was displayed.</param>
        public static void RecordNotificationShown(AchievementDisplayInfo info)
        {
            _totalShown++;

            _tierCounts.TryGetValue(info.tier, out int count);
            _tierCounts[info.tier] = count + 1;
        }

        /// <summary>
        /// Records that a notification was dismissed, along with how long it was visible.
        /// Pass a <paramref name="viewTime"/> less than the configured display duration to
        /// count as an "early dismiss".
        /// </summary>
        /// <param name="viewTime">Seconds the notification was visible before dismissal.</param>
        /// <param name="autoDisplayDuration">
        /// Expected display duration (seconds).  When <paramref name="viewTime"/> is
        /// less than this value the notification is considered dismissed early.
        /// Defaults to <see cref="AchievementNotificationConfig.Default"/> display duration.
        /// </param>
        public static void RecordNotificationDismissed(
            float viewTime,
            float autoDisplayDuration = 0f)
        {
            _dismissedCount++;
            _totalViewTime += viewTime;

            if (autoDisplayDuration <= 0f)
                autoDisplayDuration = AchievementNotificationConfig.Default.displayDuration;

            if (viewTime < autoDisplayDuration - 0.1f)
                _dismissedEarly++;
        }

        /// <summary>Returns a snapshot of the current session's notification analytics.</summary>
        public static NotificationAnalyticsSummary GetNotificationAnalyticsSummary()
        {
            float avg = _dismissedCount > 0
                ? _totalViewTime / _dismissedCount
                : 0f;

            AchievementTier mostCommon = AchievementTier.Bronze;
            if (_tierCounts.Count > 0)
                mostCommon = _tierCounts.OrderByDescending(kv => kv.Value).First().Key;

            return new NotificationAnalyticsSummary
            {
                totalShown      = _totalShown,
                dismissedEarly  = _dismissedEarly,
                averageViewTime = avg,
                mostCommonTier  = mostCommon
            };
        }

        /// <summary>Resets all analytics counters to zero.  Call at session start if needed.</summary>
        public static void Reset()
        {
            _totalShown     = 0;
            _dismissedEarly = 0;
            _totalViewTime  = 0f;
            _dismissedCount = 0;
            _tierCounts.Clear();
        }

        #endregion
    }
}
