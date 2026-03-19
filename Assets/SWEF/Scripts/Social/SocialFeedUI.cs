using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Social
{
    /// <summary>
    /// ScrollRect-based social feed viewer.
    /// Spawns <see cref="SocialPostCard"/> prefab instances for each post.
    /// Supports pull-to-refresh and scroll-to-bottom pagination (20 posts per page).
    /// </summary>
    public class SocialFeedUI : MonoBehaviour
    {
        [Header("Scroll View")]
        [SerializeField] private ScrollRect  feedScrollRect;
        [SerializeField] private GameObject  postPrefab;
        [SerializeField] private Transform   contentParent;

        [Header("Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createPostButton;
        [SerializeField] private Button closeFeedButton;

        [Header("Panel")]
        [SerializeField] private GameObject  feedPanel;
        [SerializeField] private CanvasGroup feedCanvasGroup;
        [SerializeField] private Text        emptyFeedText;

        [Header("Pagination")]
        [SerializeField] private int  pageSize        = 20;
        [SerializeField] private float pullThreshold  = 80f;

        // ── Private state ────────────────────────────────────────────────────
        private readonly List<GameObject> _cards       = new List<GameObject>();
        private int  _loadedCount;
        private bool _isRefreshing;
        private bool _isPanelOpen;
        private Vector2 _dragStartPos;

        // Cached scene references resolved in Awake
        private PostComposerUI                  _postComposer;
        private Screenshot.ScreenshotController _screenshotController;

        // ── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (refreshButton    != null) refreshButton.onClick.AddListener(OnRefreshClicked);
            if (createPostButton != null) createPostButton.onClick.AddListener(OnCreatePostClicked);
            if (closeFeedButton  != null) closeFeedButton.onClick.AddListener(Close);

            if (feedScrollRect   != null) feedScrollRect.onValueChanged.AddListener(OnScrollChanged);

            _postComposer        = FindFirstObjectByType<PostComposerUI>();
            _screenshotController = FindFirstObjectByType<Screenshot.ScreenshotController>();

            SetPanelAlpha(0f);
            if (feedPanel != null) feedPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.OnFeedRefreshed += RebuildCards;
        }

        private void OnDisable()
        {
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.OnFeedRefreshed -= RebuildCards;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Shows the feed panel with a fade-in animation.</summary>
        public void Open()
        {
            if (_isPanelOpen) return;
            _isPanelOpen = true;
            if (feedPanel != null) feedPanel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, 1f, 0.2f));

            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.RefreshFeed();
        }

        /// <summary>Hides the feed panel with a fade-out animation.</summary>
        public void Close()
        {
            if (!_isPanelOpen) return;
            _isPanelOpen = false;
            StopAllCoroutines();
            StartCoroutine(FadePanel(1f, 0f, 0.2f, () =>
            {
                if (feedPanel != null) feedPanel.SetActive(false);
            }));
        }

        // ── Card management ──────────────────────────────────────────────────

        private void RebuildCards()
        {
            ClearCards();
            _loadedCount = 0;
            LoadNextPage();
            UpdateEmptyText();
        }

        private void LoadNextPage()
        {
            if (SocialFeedManager.Instance == null) return;

            var feed = SocialFeedManager.Instance.LocalFeed;
            int end  = Mathf.Min(_loadedCount + pageSize, feed.Count);

            for (int i = _loadedCount; i < end; i++)
                SpawnCard(feed[i]);

            _loadedCount = end;
        }

        private void SpawnCard(SocialPost post)
        {
            if (postPrefab == null || contentParent == null) return;

            GameObject go   = Instantiate(postPrefab, contentParent);
            var        card = go.GetComponent<SocialPostCard>();
            if (card != null) card.Bind(post);
            _cards.Add(go);
        }

        private void ClearCards()
        {
            foreach (var go in _cards)
            {
                if (go != null) Destroy(go);
            }
            _cards.Clear();
        }

        private void UpdateEmptyText()
        {
            bool isEmpty = SocialFeedManager.Instance == null ||
                           SocialFeedManager.Instance.LocalFeed.Count == 0;
            if (emptyFeedText != null)
                emptyFeedText.gameObject.SetActive(isEmpty);
        }

        // ── Scroll-to-bottom pagination ──────────────────────────────────────

        private void OnScrollChanged(Vector2 pos)
        {
            // pos.y == 0 means scrolled to bottom
            if (pos.y <= 0.01f && !_isRefreshing)
            {
                if (SocialFeedManager.Instance != null &&
                    _loadedCount < SocialFeedManager.Instance.LocalFeed.Count)
                {
                    LoadNextPage();
                }
            }

            // Pull-to-refresh: detect drag beyond top (pos.y > 1 + threshold in normalised units)
            if (feedScrollRect != null && feedScrollRect.content != null)
            {
                float contentY = feedScrollRect.content.anchoredPosition.y;
                if (contentY < -pullThreshold && !_isRefreshing)
                {
                    _isRefreshing = true;
                    if (SocialFeedManager.Instance != null)
                        SocialFeedManager.Instance.RefreshFeed();
                    StartCoroutine(ResetRefreshFlag());
                }
            }
        }

        private IEnumerator ResetRefreshFlag()
        {
            yield return new WaitForSeconds(1f);
            _isRefreshing = false;
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnRefreshClicked()
        {
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.RefreshFeed();
        }

        private void OnCreatePostClicked()
        {
            if (_postComposer != null)
            {
                string path = _screenshotController != null
                    ? _screenshotController.LastScreenshotPath
                    : string.Empty;
                _postComposer.Open(path);
            }
            else
            {
                Debug.LogWarning("[SWEF] SocialFeedUI: no PostComposerUI found in scene.");
            }
        }

        // ── Fade helpers ─────────────────────────────────────────────────────

        private void SetPanelAlpha(float alpha)
        {
            if (feedCanvasGroup != null) feedCanvasGroup.alpha = alpha;
        }

        private IEnumerator FadePanel(float from, float to, float duration,
            System.Action onComplete = null)
        {
            float elapsed = 0f;
            SetPanelAlpha(from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetPanelAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            SetPanelAlpha(to);
            onComplete?.Invoke();
        }
    }
}
