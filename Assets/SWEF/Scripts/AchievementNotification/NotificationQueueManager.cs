// NotificationQueueManager.cs — SWEF Achievement Notification & Popup System

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// Singleton MonoBehaviour that maintains a priority queue of achievement
    /// notifications and drives the display pipeline.
    ///
    /// <para>Other systems enqueue notifications via <see cref="Enqueue"/>.  The manager
    /// shows one notification at a time (or up to
    /// <see cref="AchievementNotificationConfig.maxQueueSize"/> simultaneously when
    /// configured), auto-dismisses them after the configured duration, and fires events
    /// so the UI layer can react.</para>
    ///
    /// <para>Survives scene transitions via <c>DontDestroyOnLoad</c>.</para>
    /// </summary>
    public class NotificationQueueManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NotificationQueueManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Profile")]
        [Tooltip("Notification profile containing timing and queue parameters. Falls back to defaults when null.")]
        [SerializeField] private AchievementNotificationProfile profile;

        [Header("Display")]
        [Tooltip("Maximum number of notifications shown on screen at the same time.")]
        [SerializeField] [Range(1, 5)] private int maxSimultaneous = 1;

        [Header("References")]
        [Tooltip("Toast controller used to render queued notifications. Resolved at runtime if null.")]
        [SerializeField] private ToastNotificationController toastController;

        #endregion

        #region Events

        /// <summary>Fired when a notification begins displaying.</summary>
        public event Action<AchievementDisplayInfo> OnNotificationShown;

        /// <summary>Fired when a notification is dismissed (by timer or explicit call).</summary>
        public event Action<AchievementDisplayInfo> OnNotificationDismissed;

        /// <summary>Fired when the queue is fully drained and nothing is on screen.</summary>
        public event Action OnQueueEmpty;

        #endregion

        #region Private state

        // Internal priority-queue entry.
        private struct QueueEntry
        {
            public AchievementDisplayInfo info;
            public NotificationPriority   priority;
            public float                  enqueueTime;
        }

        private readonly List<QueueEntry>         _queue   = new List<QueueEntry>();
        private readonly List<AchievementDisplayInfo> _active = new List<AchievementDisplayInfo>();
        private Coroutine                          _processorCoroutine;
        private AchievementNotificationConfig      _config;
        // Signals the processor coroutine to skip the current wait immediately.
        private bool _dismissCurrentRequested;

        #endregion

        #region Unity lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _config = profile != null ? profile.notificationConfig : AchievementNotificationConfig.Default;
        }

        private void Start()
        {
            if (toastController == null)
                toastController = FindObjectOfType<ToastNotificationController>();

            _processorCoroutine = StartCoroutine(ProcessQueueRoutine());
        }

        private void OnDestroy()
        {
            if (_processorCoroutine != null)
                StopCoroutine(_processorCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Adds an achievement notification to the priority queue.
        /// Critical-priority notifications are placed at the front of the queue.
        /// </summary>
        /// <param name="info">Display data for the achievement.</param>
        /// <param name="priority">Queue priority; Critical notifications are shown first.</param>
        public void Enqueue(AchievementDisplayInfo info, NotificationPriority priority)
        {
            if (_queue.Count >= _config.maxQueueSize)
            {
                Debug.LogWarning("[NotificationQueueManager] Queue is full; dropping notification: " + info.title);
                return;
            }

            var entry = new QueueEntry
            {
                info        = info,
                priority    = priority,
                enqueueTime = Time.realtimeSinceStartup
            };

            // Insert in priority order (Critical → High → Normal → Low).
            int insertIndex = _queue.Count;
            for (int i = 0; i < _queue.Count; i++)
            {
                if (priority > _queue[i].priority)
                {
                    insertIndex = i;
                    break;
                }
            }
            _queue.Insert(insertIndex, entry);

            AchievementNotificationAnalytics.RecordNotificationShown(info);
        }

        /// <summary>Immediately dismisses the oldest active notification.</summary>
        public void DismissCurrent()
        {
            if (_active.Count == 0) return;

            _dismissCurrentRequested = true;

            var info = _active[0];
            _active.RemoveAt(0);

            if (toastController != null)
                toastController.HideToast(0f);

            OnNotificationDismissed?.Invoke(info);
            AchievementNotificationAnalytics.RecordNotificationDismissed(0f);

            if (_active.Count == 0 && _queue.Count == 0)
                OnQueueEmpty?.Invoke();
        }

        /// <summary>Clears the queue and dismisses all active notifications.</summary>
        public void DismissAll()
        {
            _queue.Clear();
            while (_active.Count > 0)
                DismissCurrent();
        }

        #endregion

        #region Processing coroutine

        private IEnumerator ProcessQueueRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                if (_queue.Count == 0 || _active.Count >= maxSimultaneous)
                    continue;

                var entry = _queue[0];
                _queue.RemoveAt(0);

                _active.Add(entry.info);

                if (toastController != null)
                    toastController.ShowToast(entry.info);

                OnNotificationShown?.Invoke(entry.info);

                float shownAt = Time.realtimeSinceStartup;
                _dismissCurrentRequested = false;

                // Wait for the display duration, but break early if dismissed.
                float elapsed = 0f;
                while (elapsed < _config.displayDuration && !_dismissCurrentRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (_active.Contains(entry.info))
                {
                    _active.Remove(entry.info);

                    if (toastController != null)
                        toastController.HideToast(_config.fadeDuration);

                    OnNotificationDismissed?.Invoke(entry.info);
                    AchievementNotificationAnalytics.RecordNotificationDismissed(
                        Time.realtimeSinceStartup - shownAt, _config.displayDuration);
                }

                _dismissCurrentRequested = false;

                if (_active.Count == 0 && _queue.Count == 0)
                    OnQueueEmpty?.Invoke();
            }
        }

        #endregion
    }
}
