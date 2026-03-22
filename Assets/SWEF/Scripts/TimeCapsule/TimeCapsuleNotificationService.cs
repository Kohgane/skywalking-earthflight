using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.TimeCapsule
{
    /// <summary>
    /// Background service that periodically checks for capsules that are ready to open
    /// or approaching their expiry date, and fires notification events so that the UI
    /// layer can display toast messages and badge updates.
    /// </summary>
    public class TimeCapsuleNotificationService : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Polling")]
        [Tooltip("How often (in seconds) the service checks for ready or expiring capsules.")]
        [SerializeField] private float checkInterval = 30f;

        [Header("Expiry Warning")]
        [Tooltip("A capsule is considered 'expiring soon' when fewer than this many seconds remain until its openAfter date.")]
        [SerializeField] private float expiryWarnLeadSeconds = 3600f; // 1 hour

        [Header("UI Integration")]
        [Tooltip("Optional reference to the TimeCapsuleUI used to update the notification badge.")]
        [SerializeField] private TimeCapsuleUI capsuleUI;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a sealed capsule becomes ready to open.</summary>
        public event Action<TimeCapsule> OnCapsuleReady;

        /// <summary>
        /// Fired when a sealed capsule is nearing its <c>openAfter</c> expiry window
        /// (within <see cref="expiryWarnLeadSeconds"/> of becoming openable).
        /// </summary>
        public event Action<TimeCapsule> OnCapsuleExpiring;

        // ── Internal state ────────────────────────────────────────────────────────
        /// <summary>IDs of capsules already announced as ready (to avoid duplicate events).</summary>
        private readonly HashSet<string> _notifiedReady    = new HashSet<string>();
        /// <summary>IDs of capsules already announced as expiring.</summary>
        private readonly HashSet<string> _notifiedExpiring = new HashSet<string>();

        private Coroutine _pollCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            _pollCoroutine = StartCoroutine(PollLoop());
        }

        private void OnDisable()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }
        }

        // ── Core check ────────────────────────────────────────────────────────────

        /// <summary>
        /// Queries <see cref="TimeCapsuleManager"/> for sealed capsules that have become
        /// ready to open or that are approaching their expiry window, and fires the
        /// appropriate notification events.
        /// </summary>
        public void CheckForReadyCapsules()
        {
            if (TimeCapsuleManager.Instance == null) return;

            var now = DateTime.UtcNow;

            foreach (var capsule in TimeCapsuleManager.Instance.Capsules)
            {
                if (capsule.status != TimeCapsuleStatus.Sealed) continue;

                // ── Ready check ───────────────────────────────────────────────────
                if (capsule.IsReadyToOpen() && !_notifiedReady.Contains(capsule.capsuleId))
                {
                    _notifiedReady.Add(capsule.capsuleId);
                    OnCapsuleReady?.Invoke(capsule);
                    capsuleUI?.UpdateNotificationBadge();
                    Debug.Log($"[TimeCapsuleNotificationService] Capsule ready to open: \"{capsule.title}\"");
                    continue;
                }

                // ── Expiring check ────────────────────────────────────────────────
                if (!string.IsNullOrEmpty(capsule.openAfter)
                    && !_notifiedExpiring.Contains(capsule.capsuleId)
                    && DateTime.TryParse(capsule.openAfter, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out DateTime openAfterDt))
                {
                    double secondsUntilOpen = (openAfterDt - now).TotalSeconds;
                    if (secondsUntilOpen > 0 && secondsUntilOpen <= expiryWarnLeadSeconds)
                    {
                        _notifiedExpiring.Add(capsule.capsuleId);
                        OnCapsuleExpiring?.Invoke(capsule);
                        Debug.Log($"[TimeCapsuleNotificationService] Capsule expiring soon: \"{capsule.title}\" " +
                                  $"({secondsUntilOpen:F0}s remaining)");
                    }
                }
            }
        }

        // ── Scheduled reminders ───────────────────────────────────────────────────

        /// <summary>
        /// Schedules a one-off check after <paramref name="delaySeconds"/> seconds.
        /// Useful for triggering a precise notification at the capsule's unlock moment.
        /// </summary>
        /// <param name="capsuleId">GUID of the capsule to check.</param>
        /// <param name="delaySeconds">Seconds to wait before checking.</param>
        public void ScheduleReminder(string capsuleId, float delaySeconds)
        {
            if (delaySeconds <= 0f)
            {
                CheckForReadyCapsules();
                return;
            }
            StartCoroutine(DelayedCheck(capsuleId, delaySeconds));
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private IEnumerator PollLoop()
        {
            var wait = new WaitForSeconds(checkInterval);
            while (true)
            {
                CheckForReadyCapsules();
                yield return wait;
            }
        }

        private IEnumerator DelayedCheck(string capsuleId, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (TimeCapsuleManager.Instance == null) yield break;

            foreach (var capsule in TimeCapsuleManager.Instance.Capsules)
            {
                if (capsule.capsuleId != capsuleId) continue;
                if (capsule.status != TimeCapsuleStatus.Sealed) yield break;

                if (capsule.IsReadyToOpen() && !_notifiedReady.Contains(capsule.capsuleId))
                {
                    _notifiedReady.Add(capsule.capsuleId);
                    OnCapsuleReady?.Invoke(capsule);
                    capsuleUI?.UpdateNotificationBadge();
                    Debug.Log($"[TimeCapsuleNotificationService] Scheduled reminder fired: \"{capsule.title}\"");
                }
                yield break;
            }
        }
    }
}
