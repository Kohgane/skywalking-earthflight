// ToastNotificationController.cs — SWEF Achievement Notification & Popup System

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.AchievementNotification
{
    /// <summary>
    /// MonoBehaviour that manages slide-in/slide-out toast animations and stacking
    /// for achievement notifications.
    ///
    /// <para>Each toast slides onto the screen from a configurable edge
    /// (<see cref="SlideDirection"/>), plays a scale-punch effect on arrival,
    /// and shows a progress bar counting down the remaining display time.
    /// Multiple toasts are stacked vertically with even spacing.</para>
    ///
    /// <para>Call <see cref="ShowToast"/> and <see cref="HideToast"/> from
    /// <see cref="NotificationQueueManager"/>.</para>
    /// </summary>
    public class ToastNotificationController : MonoBehaviour
    {
        #region Inspector

        [Header("Slide Animation")]
        [Tooltip("Direction from which toast cards slide onto the screen.")]
        [SerializeField] private SlideDirection slideDirection = SlideDirection.Top;

        [Tooltip("Speed (units/second) of the slide-in and slide-out movement.")]
        [SerializeField] [Range(1f, 30f)] private float slideSpeed = 8f;

        [Tooltip("Off-screen offset distance used as the start position for the slide.")]
        [SerializeField] private float offscreenOffset = 300f;

        [Header("Punch Effect")]
        [Tooltip("Whether to play a scale-punch effect when the toast appears.")]
        [SerializeField] private bool enablePunch = true;

        [Tooltip("Maximum scale overshoot during the punch (e.g. 1.15 = 15 % larger).")]
        [SerializeField] [Range(1f, 1.5f)] private float punchScalePeak = 1.15f;

        [Tooltip("Duration (seconds) of the punch animation.")]
        [SerializeField] [Range(0.05f, 0.5f)] private float punchDuration = 0.18f;

        [Header("Stacking")]
        [Tooltip("Vertical gap (pixels) between stacked toast cards.")]
        [SerializeField] private float stackSpacing = 10f;

        [Tooltip("Toast card prefab. Must contain an AchievementPopupUI component.")]
        [SerializeField] private GameObject toastPrefab;

        [Tooltip("Parent RectTransform used to anchor toast instances.")]
        [SerializeField] private RectTransform toastContainer;

        [Header("Progress Bar")]
        [Tooltip("Reference to the Image used as a fill-amount progress bar on the current toast.")]
        [SerializeField] private Image progressBarImage;

        [Tooltip("Display duration fed from AchievementNotificationConfig when available.")]
        [SerializeField] private float displayDuration = 4f;

        #endregion

        #region Private state

        private readonly List<ToastEntry> _activeToasts = new List<ToastEntry>();
        private Coroutine _progressBarCoroutine;
        private float     _cachedCardHeight = -1f;

        private struct ToastEntry
        {
            public RectTransform  rectTransform;
            public CanvasGroup    canvasGroup;
            public AchievementPopupUI popupUI;
            public Vector2        targetPosition;
            public Coroutine      slideCoroutine;
        }

        #endregion

        #region Public API

        /// <summary>Creates and animates a new toast notification card onto the screen.</summary>
        /// <param name="info">Achievement data to display on the toast.</param>
        public void ShowToast(AchievementDisplayInfo info)
        {
            if (toastPrefab == null || toastContainer == null)
            {
                Debug.LogWarning("[ToastNotificationController] toastPrefab or toastContainer is not assigned.");
                return;
            }

            var go = Instantiate(toastPrefab, toastContainer);
            var rt = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            var popupUI = go.GetComponent<AchievementPopupUI>();
            if (popupUI != null)
            {
                popupUI.SetupCard(info);
                popupUI.SetDisplayMode(DisplayMode.Mini);
            }

            // Calculate target (on-screen) position.
            Vector2 targetPos = CalculateStackPosition(_activeToasts.Count);

            // Start off-screen.
            rt.anchoredPosition = GetOffscreenPosition(targetPos);
            cg.alpha = 1f;

            var entry = new ToastEntry
            {
                rectTransform  = rt,
                canvasGroup    = cg,
                popupUI        = popupUI,
                targetPosition = targetPos
            };
            _activeToasts.Add(entry);
            int idx = _activeToasts.Count - 1;

            var slideCoroutine = StartCoroutine(SlideInRoutine(idx));
            var updatedEntry = _activeToasts[idx];
            updatedEntry.slideCoroutine = slideCoroutine;
            _activeToasts[idx] = updatedEntry;

            // Re-arrange older toasts.
            RefreshStackPositions();

            // Progress bar.
            if (progressBarImage != null)
            {
                if (_progressBarCoroutine != null) StopCoroutine(_progressBarCoroutine);
                _progressBarCoroutine = StartCoroutine(ProgressBarRoutine());
            }
        }

        /// <summary>Slides out and destroys the oldest toast after an optional delay.</summary>
        /// <param name="delay">Seconds to wait before starting the slide-out.</param>
        public void HideToast(float delay)
        {
            if (_activeToasts.Count == 0) return;
            StartCoroutine(HideRoutine(0, delay));
        }

        #endregion

        #region Animation coroutines

        private IEnumerator SlideInRoutine(int index)
        {
            if (index >= _activeToasts.Count) yield break;

            var entry = _activeToasts[index];
            var rt    = entry.rectTransform;

            // Slide in.
            Vector2 start  = rt.anchoredPosition;
            Vector2 target = entry.targetPosition;
            float   t      = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                rt.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            rt.anchoredPosition = target;

            // Scale punch.
            if (enablePunch)
                yield return StartCoroutine(PunchRoutine(rt));
        }

        private IEnumerator PunchRoutine(RectTransform rt)
        {
            float half = punchDuration * 0.5f;
            float t    = 0f;

            while (t < half)
            {
                t  += Time.deltaTime;
                float s = Mathf.Lerp(1f, punchScalePeak, t / half);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t  += Time.deltaTime;
                float s = Mathf.Lerp(punchScalePeak, 1f, t / half);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private IEnumerator HideRoutine(int index, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (index >= _activeToasts.Count) yield break;

            var entry = _activeToasts[index];
            var rt    = entry.rectTransform;
            var cg    = entry.canvasGroup;

            Vector2 start  = rt.anchoredPosition;
            Vector2 target = GetOffscreenPosition(start);
            float   t      = 0f;

            while (t < 1f)
            {
                t     += Time.deltaTime * slideSpeed;
                float eased = Mathf.SmoothStep(0f, 1f, t);
                rt.anchoredPosition = Vector2.Lerp(start, target, eased);
                cg.alpha            = Mathf.Lerp(1f, 0f, eased);
                yield return null;
            }

            _activeToasts.RemoveAt(index);
            Destroy(rt.gameObject);
            RefreshStackPositions();
        }

        private IEnumerator ProgressBarRoutine()
        {
            float t = 0f;
            while (t < displayDuration)
            {
                t += Time.deltaTime;
                progressBarImage.fillAmount = 1f - (t / displayDuration);
                yield return null;
            }
            progressBarImage.fillAmount = 0f;
        }

        #endregion

        #region Helpers

        private Vector2 CalculateStackPosition(int stackIndex)
        {
            // Cache the card height on first access to avoid repeated GetComponent calls.
            if (_cachedCardHeight < 0f)
            {
                var prefabRt = toastPrefab != null ? toastPrefab.GetComponent<RectTransform>() : null;
                _cachedCardHeight = prefabRt != null ? prefabRt.rect.height : 80f;
            }

            float yOffset = stackIndex * -(_cachedCardHeight + stackSpacing);
            return new Vector2(0f, yOffset);
        }

        private void RefreshStackPositions()
        {
            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var entry = _activeToasts[i];
                entry.targetPosition = CalculateStackPosition(i);
                _activeToasts[i]     = entry;
                StartCoroutine(SlideTo(entry.rectTransform, entry.targetPosition));
            }
        }

        private IEnumerator SlideTo(RectTransform rt, Vector2 target)
        {
            float t = 0f;
            Vector2 start = rt.anchoredPosition;
            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                rt.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            rt.anchoredPosition = target;
        }

        private Vector2 GetOffscreenPosition(Vector2 onScreenPos)
        {
            switch (slideDirection)
            {
                case SlideDirection.Top:    return onScreenPos + Vector2.up    * offscreenOffset;
                case SlideDirection.Bottom: return onScreenPos + Vector2.down  * offscreenOffset;
                case SlideDirection.Left:   return onScreenPos + Vector2.left  * offscreenOffset;
                case SlideDirection.Right:  return onScreenPos + Vector2.right * offscreenOffset;
                default:                    return onScreenPos + Vector2.up    * offscreenOffset;
            }
        }

        #endregion
    }
}
