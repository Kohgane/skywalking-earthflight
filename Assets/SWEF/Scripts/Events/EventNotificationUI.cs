using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.Events
{
    /// <summary>
    /// MonoBehaviour that displays toast notifications when world events spawn nearby
    /// and shows a persistent HUD widget with countdown and participation progress for
    /// the event the player is currently inside.
    /// </summary>
    public class EventNotificationUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Toast Notification")]
        [Tooltip("Root RectTransform of the slide-in toast panel.")]
        [SerializeField] private RectTransform toastPanel;

        [Tooltip("Text element that shows the event name.")]
        [SerializeField] private TMP_Text toastEventNameText;

        [Tooltip("Text element that shows the distance to the event.")]
        [SerializeField] private TMP_Text toastDistanceText;

        [Tooltip("Text element that shows how long the event will last.")]
        [SerializeField] private TMP_Text toastCountdownText;

        [Tooltip("Button that navigates the player to the event using WaypointNavigator.")]
        [SerializeField] private Button navigateButton;

        [Tooltip("Seconds the toast is shown before auto-dismissing.")]
        [SerializeField] private float toastDisplayDuration = 8f;

        [Tooltip("Pixels the toast slides in from off-screen.")]
        [SerializeField] private float toastSlideDistance = 300f;

        [Tooltip("Seconds the slide-in / slide-out animation takes.")]
        [SerializeField] private float toastAnimDuration = 0.4f;

        [Header("Active Event HUD Widget")]
        [Tooltip("Root RectTransform of the always-on HUD widget.")]
        [SerializeField] private RectTransform hudWidget;

        [Tooltip("Text element showing the active event name.")]
        [SerializeField] private TMP_Text hudEventNameText;

        [Tooltip("Slider representing the remaining event time.")]
        [SerializeField] private Slider hudCountdownSlider;

        [Tooltip("Text element showing remaining seconds.")]
        [SerializeField] private TMP_Text hudCountdownText;

        [Tooltip("Slider representing the player's participation progress.")]
        [SerializeField] private Slider hudProgressSlider;

        [Header("References")]
        [Tooltip("Player transform used to compute distance. Auto-resolved if null.")]
        [SerializeField] private Transform playerTransform;

        // ── Internal state ────────────────────────────────────────────────────────
        private EventScheduler          _scheduler;
        private EventParticipationTracker _tracker;
        private WorldEventInstance      _trackedHudInstance;
        private Coroutine               _toastCoroutine;
        private Vector2                 _toastShownPos;
        private Vector2                 _toastHiddenPos;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (playerTransform == null)
            {
                var fc = FindFirstObjectByType<SWEF.Flight.FlightController>();
                if (fc != null) playerTransform = fc.transform;
            }

            if (toastPanel != null)
            {
                _toastShownPos  = toastPanel.anchoredPosition;
                _toastHiddenPos = _toastShownPos + Vector2.right * toastSlideDistance;
                toastPanel.anchoredPosition = _toastHiddenPos;
                toastPanel.gameObject.SetActive(false);
            }

            if (hudWidget != null)
                hudWidget.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _scheduler = FindFirstObjectByType<EventScheduler>();
            _tracker   = FindFirstObjectByType<EventParticipationTracker>();

            if (_scheduler != null)
                _scheduler.OnEventSpawned += ShowEventNotification;
        }

        private void OnDisable()
        {
            if (_scheduler != null)
                _scheduler.OnEventSpawned -= ShowEventNotification;
        }

        private void Update()
        {
            UpdateHudWidget();
        }

        // ── Public API ────────────────────────────────────────────────────────────
        /// <summary>
        /// Displays a slide-in toast notification for the given event instance.
        /// </summary>
        /// <param name="instance">The event that just spawned.</param>
        public void ShowEventNotification(WorldEventInstance instance)
        {
            if (instance == null || toastPanel == null) return;

            if (toastEventNameText != null)
                toastEventNameText.text = instance.eventData?.eventName ?? "Event";

            if (toastDistanceText != null && playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, instance.spawnPosition);
                toastDistanceText.text = $"{dist:F0} m";
            }

            if (toastCountdownText != null)
                toastCountdownText.text = FormatSeconds(instance.RemainingTime);

            if (navigateButton != null)
            {
                navigateButton.onClick.RemoveAllListeners();
                navigateButton.onClick.AddListener(() => NavigateToEvent(instance));
            }

            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ShowToastCoroutine(instance));

            // Track the nearest active event for the HUD widget
            _trackedHudInstance = instance;
        }

        /// <summary>
        /// Immediately hides the current toast notification.
        /// </summary>
        public void DismissNotification()
        {
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(SlideOut());
        }

        /// <summary>
        /// Shows the persistent HUD widget tracking the given event's countdown and
        /// the player's participation progress.
        /// </summary>
        /// <param name="instance">The event to track in the HUD.</param>
        public void ShowActiveEventWidget(WorldEventInstance instance)
        {
            if (hudWidget == null || instance == null) return;
            _trackedHudInstance = instance;
            hudWidget.gameObject.SetActive(true);

            if (hudEventNameText != null)
                hudEventNameText.text = instance.eventData?.eventName ?? "Event";
        }

        // ── Internal helpers ──────────────────────────────────────────────────────
        private void UpdateHudWidget()
        {
            if (hudWidget == null || !hudWidget.gameObject.activeSelf) return;
            if (_trackedHudInstance == null || _trackedHudInstance.state == WorldEventState.Ended)
            {
                hudWidget.gameObject.SetActive(false);
                _trackedHudInstance = null;
                return;
            }

            float remaining = _trackedHudInstance.RemainingTime;
            float totalDuration = _trackedHudInstance.endTime - _trackedHudInstance.startTime;

            if (hudCountdownSlider != null)
                hudCountdownSlider.value = totalDuration > 0f ? remaining / totalDuration : 0f;

            if (hudCountdownText != null)
                hudCountdownText.text = FormatSeconds(remaining);

            if (hudProgressSlider != null && _tracker != null)
            {
                bool participating = _tracker.IsParticipatingIn(_trackedHudInstance.instanceId);
                float progress = 0f;
                if (participating)
                {
                    foreach (var rec in _tracker.GetActiveParticipation())
                    {
                        if (rec.instanceId == _trackedHudInstance.instanceId.ToString())
                        {
                            float maxPart = _trackedHudInstance.eventData.maxDurationMinutes * 60f * 0.25f;
                            progress = Mathf.Clamp01(rec.totalParticipationSeconds / maxPart);
                            break;
                        }
                    }
                }
                hudProgressSlider.value = progress;
            }
        }

        private IEnumerator ShowToastCoroutine(WorldEventInstance instance)
        {
            toastPanel.gameObject.SetActive(true);
            yield return SlideIn();

            float elapsed = 0f;
            while (elapsed < toastDisplayDuration)
            {
                elapsed += Time.deltaTime;
                if (toastCountdownText != null)
                    toastCountdownText.text = FormatSeconds(instance.RemainingTime);
                yield return null;
            }

            yield return SlideOut();
        }

        private IEnumerator SlideIn()
        {
            yield return AnimateToastPosition(_toastHiddenPos, _toastShownPos, toastAnimDuration);
        }

        private IEnumerator SlideOut()
        {
            yield return AnimateToastPosition(_toastShownPos, _toastHiddenPos, toastAnimDuration);
            if (toastPanel != null) toastPanel.gameObject.SetActive(false);
        }

        private IEnumerator AnimateToastPosition(Vector2 from, Vector2 to, float duration)
        {
            if (toastPanel == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                toastPanel.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            toastPanel.anchoredPosition = to;
        }

        private void NavigateToEvent(WorldEventInstance instance)
        {
            var navigator = FindFirstObjectByType<SWEF.GuidedTour.WaypointNavigator>();
            if (navigator != null)
            {
                navigator.SetManualTarget(instance.spawnPosition);
                navigator.EnableAutoPilot();
                Debug.Log($"[SWEF] EventNotificationUI: navigating to event '{instance.eventData?.eventId}'.");
            }
            else
            {
                Debug.LogWarning("[SWEF] EventNotificationUI: WaypointNavigator not found in scene.");
            }
            DismissNotification();
        }

        private static string FormatSeconds(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:D2}:{s:D2}";
        }
    }
}
