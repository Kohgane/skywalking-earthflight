using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.DailyChallenge
{
    /// <summary>
    /// Toast notification manager for challenge-related events.
    /// Queues slide-in toasts for: challenge completions, streak milestones,
    /// season tier-ups, and weekly challenge availability.
    /// Maximum 3 toasts visible simultaneously; each auto-dismisses after 4 seconds.
    /// </summary>
    public class ChallengeNotificationUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Toast Setup")]
        [Tooltip("Prefab for a single toast notification.")]
        [SerializeField] private GameObject _toastPrefab;

        [Tooltip("Parent container where toasts are spawned.")]
        [SerializeField] private Transform _toastContainer;

        [Header("Settings")]
        [Tooltip("Maximum number of simultaneously visible toasts.")]
        [SerializeField] private int _maxVisible = 3;

        [Tooltip("Seconds before a toast auto-dismisses.")]
        [SerializeField] private float _autoDismissSeconds = 4f;

        [Tooltip("Slide-in duration in seconds.")]
        [SerializeField] private float _slideInSeconds = 0.25f;

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly Queue<string> _queue = new Queue<string>();
        private readonly List<GameObject> _active = new List<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start() => SubscribeEvents();
        private void OnDestroy() => UnsubscribeEvents();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enqueues a toast with the given message string.</summary>
        /// <param name="message">Display text for the toast.</param>
        public void Enqueue(string message)
        {
            _queue.Enqueue(message);
            TryShowNext();
        }

        // ── Event subscriptions ───────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            var daily = DailyChallengeManager.Instance;
            if (daily != null)
            {
                daily.OnChallengeCompleted += ac =>
                {
                    if (ac.definition != null)
                        Enqueue($"Challenge Complete! +{ac.definition.baseXPReward} XP");
                };
                daily.OnStreakUpdated += streak =>
                {
                    if (streak % 5 == 0 || streak == 1)
                        Enqueue($"🔥 {streak}-Day Streak! +{streak * 10}% XP Bonus");
                };
                daily.OnDailyReset += () => Enqueue("New Daily Challenges Available!");
            }

            var spm = SeasonPassManager.Instance;
            if (spm != null)
                spm.OnTierAdvanced += tier => Enqueue($"Season Tier {tier} Reached! New rewards available.");

            var weekly = WeeklyChallengeManager.Instance;
            if (weekly != null)
                weekly.OnWeeklyReset += () => Enqueue("New Weekly Challenges Available!");
        }

        private void UnsubscribeEvents()
        {
            // Delegates are captured lambdas — they will be GC'd with this object.
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void TryShowNext()
        {
            // Remove destroyed/null actives.
            _active.RemoveAll(g => g == null);

            if (_active.Count >= _maxVisible) return;
            if (_queue.Count == 0) return;

            string msg = _queue.Dequeue();
            ShowToast(msg);
        }

        private void ShowToast(string message)
        {
            if (_toastContainer == null) return;

            GameObject go;
            if (_toastPrefab != null)
            {
                go = Instantiate(_toastPrefab, _toastContainer);
            }
            else
            {
                // Fallback: create a minimal UI element.
                go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Text));
                go.transform.SetParent(_toastContainer, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.8f);
            }

            // Set text.
            var text = go.GetComponentInChildren<Text>();
            if (text == null) text = go.GetComponent<Text>();
            if (text != null) text.text = message;

            _active.Add(go);
            StartCoroutine(ToastRoutine(go));
        }

        private IEnumerator ToastRoutine(GameObject toast)
        {
            if (toast == null) yield break;

            var rect = toast.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Slide in from off-screen top.
                Vector2 hiddenPos = new Vector2(0f, 100f);
                Vector2 shownPos  = Vector2.zero;
                float t = 0f;
                while (t < _slideInSeconds)
                {
                    t += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, t / _slideInSeconds);
                    yield return null;
                }
                rect.anchoredPosition = shownPos;
            }

            yield return new WaitForSeconds(_autoDismissSeconds);

            // Slide out.
            if (toast != null && rect != null)
            {
                float t2 = 0f;
                Vector2 start = rect.anchoredPosition;
                Vector2 end   = new Vector2(0f, 100f);
                while (t2 < _slideInSeconds && toast != null)
                {
                    t2 += Time.deltaTime;
                    rect.anchoredPosition = Vector2.Lerp(start, end, t2 / _slideInSeconds);
                    yield return null;
                }
            }

            if (toast != null)
            {
                _active.Remove(toast);
                Destroy(toast);
            }

            TryShowNext();
        }
    }
}
