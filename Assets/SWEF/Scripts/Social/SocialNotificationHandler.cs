using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Social
{
    /// <summary>
    /// Handles social notifications and in-app toast alerts.
    /// Shows an animated slide-in toast when a new post is created.
    /// Multiple notifications are queued and shown sequentially.
    /// </summary>
    public class SocialNotificationHandler : MonoBehaviour
    {
        [Header("Toast")]
        [SerializeField] private GameObject  toastPanel;
        [SerializeField] private Text        toastText;
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private float       displayDuration = 3f;

        [Header("Animation")]
        [SerializeField] private float  slideDistance = 80f;
        [SerializeField] private float  animDuration  = 0.3f;

        // ── Private state ─────────────────────────────────────────────────────
        private readonly Queue<string> _queue   = new Queue<string>();
        private bool                   _showing;
        private RectTransform          _toastRect;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
                _toastRect = toastPanel.GetComponent<RectTransform>();
            }

            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.OnPostCreated += HandlePostCreated;
        }

        private void OnDisable()
        {
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.OnPostCreated -= HandlePostCreated;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a toast notification for a new post and shows it when the
        /// previous one (if any) finishes.
        /// </summary>
        public void ShowNewPostToast(SocialPost post)
        {
            if (post == null) return;
            string message = $"✈ {post.authorName} shared a flight!";
            EnqueueToast(message);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void HandlePostCreated(SocialPost post) => ShowNewPostToast(post);

        private void EnqueueToast(string message)
        {
            _queue.Enqueue(message);
            if (!_showing)
                StartCoroutine(ShowNextToast());
        }

        private IEnumerator ShowNextToast()
        {
            while (_queue.Count > 0)
            {
                _showing = true;
                string msg = _queue.Dequeue();
                yield return StartCoroutine(AnimateToast(msg));
            }
            _showing = false;
        }

        private IEnumerator AnimateToast(string message)
        {
            if (toastPanel == null) yield break;

            if (toastText != null) toastText.text = message;

            toastPanel.SetActive(true);

            // Slide in from top: start above screen, slide down to anchor position
            Vector2 origin = _toastRect != null ? _toastRect.anchoredPosition : Vector2.zero;
            Vector2 hiddenPos = origin + new Vector2(0f, slideDistance);

            if (_toastRect != null) _toastRect.anchoredPosition = hiddenPos;
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;

            // Slide in + fade in
            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
                if (_toastRect     != null) _toastRect.anchoredPosition = Vector2.Lerp(hiddenPos, origin, t);
                if (toastCanvasGroup != null) toastCanvasGroup.alpha    = t;
                yield return null;
            }

            if (_toastRect     != null) _toastRect.anchoredPosition = origin;
            if (toastCanvasGroup != null) toastCanvasGroup.alpha    = 1f;

            // Display for the configured duration
            yield return new WaitForSecondsRealtime(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animDuration;
                if (toastCanvasGroup != null) toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
            toastPanel.SetActive(false);
            if (_toastRect != null) _toastRect.anchoredPosition = origin;
        }
    }
}
