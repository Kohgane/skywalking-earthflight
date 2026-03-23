// EventNotificationUI.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// MonoBehaviour that displays animated event notification popups in the HUD.
    /// Popups are queued so that they play sequentially even when multiple events
    /// trigger at the same time.  Attach to a Canvas GameObject.
    /// </summary>
    public sealed class EventNotificationUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Popup Settings")]
        [Tooltip("How long each popup remains visible on screen before auto-dismissing.")]
        /// <summary>How long each popup remains visible on screen before auto-dismissing.</summary>
        public float popupDuration = 5f;

        [Tooltip("Animation curve that controls the popup slide-in motion (time 0-1, value 0-1).")]
        /// <summary>Animation curve that controls the popup slide-in motion.</summary>
        public AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("UI References")]
        [Tooltip("Root RectTransform of the notification popup panel.")]
        /// <summary>Root RectTransform of the notification popup panel.</summary>
        [SerializeField] private RectTransform _popupPanel;

        [Tooltip("Text element that shows the event name inside the popup.")]
        /// <summary>Text element that shows the event name inside the popup.</summary>
        [SerializeField] private Text _eventNameText;

        [Tooltip("Text element that shows the event description inside the popup.")]
        /// <summary>Text element that shows the event description inside the popup.</summary>
        [SerializeField] private Text _descriptionText;

        [Tooltip("Image element that shows the event icon inside the popup.")]
        /// <summary>Image element that shows the event icon inside the popup.</summary>
        [SerializeField] private Image _eventIconImage;

        [Tooltip("'Track' or 'Accept' button that the player clicks to start tracking the event.")]
        /// <summary>'Track' or 'Accept' button that the player clicks to start tracking the event.</summary>
        [SerializeField] private Button _acceptButton;

        [Tooltip("Pixel distance the popup travels during the slide-in animation.")]
        /// <summary>Pixel distance the popup travels during the slide-in animation.</summary>
        [SerializeField] private float _slideDistance = 300f;

        [Tooltip("Duration in seconds of the slide-in/slide-out animation.")]
        /// <summary>Duration in seconds of the slide-in/slide-out animation.</summary>
        [SerializeField] private float _animationDuration = 0.4f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the player clicks the Accept button on a notification popup.</summary>
        public event Action<ActiveWorldEvent> OnEventAccepted;

        // ── Runtime State ─────────────────────────────────────────────────────────

        private readonly Queue<(ActiveWorldEvent worldEvent, NotificationType type)> _queue
            = new Queue<(ActiveWorldEvent, NotificationType)>();

        private bool _isShowing;
        private ActiveWorldEvent _currentEvent;

        private enum NotificationType { Spawned, Completed, Failed }

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.OnEventSpawned    += OnEventSpawned;
                WorldEventManager.Instance.OnEventCompleted  += OnEventCompleted;
                WorldEventManager.Instance.OnEventFailed     += OnEventFailed;
            }

            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(HandleAcceptClicked);
        }

        private void OnDisable()
        {
            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.OnEventSpawned    -= OnEventSpawned;
                WorldEventManager.Instance.OnEventCompleted  -= OnEventCompleted;
                WorldEventManager.Instance.OnEventFailed     -= OnEventFailed;
            }

            if (_acceptButton != null)
                _acceptButton.onClick.RemoveListener(HandleAcceptClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues an animated popup for a newly spawned world event.
        /// </summary>
        /// <param name="worldEvent">The event that just spawned.</param>
        public void ShowEventPopup(ActiveWorldEvent worldEvent)
        {
            _queue.Enqueue((worldEvent, NotificationType.Spawned));
            TryShowNext();
        }

        /// <summary>
        /// Enqueues a completion celebration popup.
        /// </summary>
        /// <param name="worldEvent">The event that was just completed.</param>
        public void ShowEventComplete(ActiveWorldEvent worldEvent)
        {
            _queue.Enqueue((worldEvent, NotificationType.Completed));
            TryShowNext();
        }

        /// <summary>
        /// Enqueues a failure notification popup.
        /// </summary>
        /// <param name="worldEvent">The event that was just failed.</param>
        public void ShowEventFailed(ActiveWorldEvent worldEvent)
        {
            _queue.Enqueue((worldEvent, NotificationType.Failed));
            TryShowNext();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void TryShowNext()
        {
            if (_isShowing || _queue.Count == 0) return;
            var (ev, type) = _queue.Dequeue();
            StartCoroutine(ShowPopupRoutine(ev, type));
        }

        private IEnumerator ShowPopupRoutine(ActiveWorldEvent ev, NotificationType type)
        {
            _isShowing = true;
            _currentEvent = ev;

            PopulatePopup(ev, type);

            // Slide in
            yield return StartCoroutine(AnimateSlide(visible: true));

            // Hold
            float held = 0f;
            while (held < popupDuration)
            {
                held += Time.unscaledDeltaTime;
                yield return null;
            }

            // Slide out
            yield return StartCoroutine(AnimateSlide(visible: false));

            HideImmediate();
            _isShowing = false;
            _currentEvent = null;

            TryShowNext();
        }

        private IEnumerator AnimateSlide(bool visible)
        {
            if (_popupPanel == null) yield break;

            float elapsed = 0f;
            Vector2 hiddenPos = new Vector2(-_slideDistance, _popupPanel.anchoredPosition.y);
            Vector2 shownPos  = new Vector2(0f, _popupPanel.anchoredPosition.y);

            Vector2 from = visible ? hiddenPos : shownPos;
            Vector2 to   = visible ? shownPos  : hiddenPos;

            _popupPanel.gameObject.SetActive(true);

            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _animationDuration);
                float curved = slideInCurve.Evaluate(t);
                _popupPanel.anchoredPosition = Vector2.LerpUnclamped(from, to, curved);
                yield return null;
            }

            _popupPanel.anchoredPosition = to;
        }

        private void PopulatePopup(ActiveWorldEvent ev, NotificationType type)
        {
            if (ev?.eventData == null) return;

            if (_eventNameText != null)
                _eventNameText.text = BuildTitle(ev.eventData, type);

            if (_descriptionText != null)
                _descriptionText.text = ev.eventData.description;

            if (_eventIconImage != null && ev.eventData.eventIcon != null)
                _eventIconImage.sprite = ev.eventData.eventIcon;

            bool showAccept = type == NotificationType.Spawned;
            if (_acceptButton != null)
                _acceptButton.gameObject.SetActive(showAccept);
        }

        private static string BuildTitle(WorldEventData data, NotificationType type)
        {
            return type switch
            {
                NotificationType.Completed => $"✓ {data.eventName}",
                NotificationType.Failed    => $"✗ {data.eventName}",
                _                          => data.eventName
            };
        }

        private void HideImmediate()
        {
            if (_popupPanel != null)
                _popupPanel.gameObject.SetActive(false);
        }

        // ── Event Manager Callbacks ────────────────────────────────────────────────

        private void OnEventSpawned(ActiveWorldEvent ev) => ShowEventPopup(ev);

        private void OnEventCompleted(ActiveWorldEvent ev) => ShowEventComplete(ev);

        private void OnEventFailed(ActiveWorldEvent ev) => ShowEventFailed(ev);

        private void HandleAcceptClicked()
        {
            if (_currentEvent == null) return;
            _currentEvent.isTracked = true;
            OnEventAccepted?.Invoke(_currentEvent);
        }
    }
}
