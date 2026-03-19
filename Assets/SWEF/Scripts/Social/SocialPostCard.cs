using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Social
{
    /// <summary>
    /// UI component for a single post card in the social feed.
    /// Call <see cref="Bind"/> to populate all fields from a <see cref="SocialPost"/>.
    /// </summary>
    public class SocialPostCard : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private RawImage screenshotImage;
        [SerializeField] private Text     authorText;
        [SerializeField] private Text     captionText;
        [SerializeField] private Text     timestampText;
        [SerializeField] private Text     locationText;
        [SerializeField] private Text     likeCountText;

        [Header("Buttons")]
        [SerializeField] private Button likeButton;
        [SerializeField] private Button shareButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button viewOnMapButton;

        [Header("Like icon")]
        [SerializeField] private Image likeIcon;

        [Header("Delete visibility")]
        [SerializeField] private GameObject deleteButtonObj;

        // ── Private state ────────────────────────────────────────────────────
        private SocialPost _post;
        private Texture2D  _loadedTexture;
        private Teleport.TeleportController _teleportController;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Populates all UI fields from <paramref name="post"/> and loads the thumbnail
        /// from disk asynchronously.
        /// </summary>
        public void Bind(SocialPost post)
        {
            _post = post;

            if (_teleportController == null)
                _teleportController = FindFirstObjectByType<Teleport.TeleportController>();

            if (authorText    != null) authorText.text    = post.authorName;
            if (captionText   != null) captionText.text   = post.caption;
            if (timestampText != null) timestampText.text = post.GetFormattedTimestamp();
            if (locationText  != null) locationText.text  = post.GetLocationString();

            RefreshLikeUI();

            // Show delete button only for posts made by the current player.
            // MVP: ownership is determined by display name equality.
            // A future backend integration should compare a persistent player UUID instead.
            string myName = CommunityProfileManager.Instance != null
                ? CommunityProfileManager.Instance.GetDisplayName()
                : "Pilot";
            bool isOwn = string.Equals(post.authorName, myName,
                System.StringComparison.OrdinalIgnoreCase);
            if (deleteButtonObj != null) deleteButtonObj.SetActive(isOwn);

            // Wire buttons
            if (likeButton      != null) likeButton.onClick.RemoveAllListeners();
            if (shareButton     != null) shareButton.onClick.RemoveAllListeners();
            if (deleteButton    != null) deleteButton.onClick.RemoveAllListeners();
            if (viewOnMapButton != null) viewOnMapButton.onClick.RemoveAllListeners();

            if (likeButton      != null) likeButton.onClick.AddListener(OnLike);
            if (shareButton     != null) shareButton.onClick.AddListener(OnShare);
            if (deleteButton    != null) deleteButton.onClick.AddListener(OnDelete);
            if (viewOnMapButton != null) viewOnMapButton.onClick.AddListener(OnViewOnMap);

            // Load thumbnail
            StartCoroutine(LoadThumbnail(post.thumbnailPath));
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnLike()
        {
            if (_post == null || SocialFeedManager.Instance == null) return;
            SocialFeedManager.Instance.ToggleLike(_post.postId);
            RefreshLikeUI();
            StartCoroutine(PunchScale(likeButton != null ? likeButton.transform : transform));
        }

        private void OnShare()
        {
            if (_post == null) return;
            SocialShareController.ShareImage(_post.imagePath, _post.caption);
        }

        private void OnDelete()
        {
            if (_post == null) return;
            // Simple inline confirmation via a Debug dialog (no Dialog system required)
            // In a real project this would show a modal; here we delete immediately.
            Debug.Log($"[SWEF] SocialPostCard: deleting post {_post.postId}");
            if (SocialFeedManager.Instance != null)
                SocialFeedManager.Instance.DeletePost(_post.postId);
            Destroy(gameObject);
        }

        private void OnViewOnMap()
        {
            if (_post == null) return;
            if (_teleportController != null)
                _teleportController.TeleportTo(_post.latitude, _post.longitude);
            else
                Debug.LogWarning("[SWEF] SocialPostCard: TeleportController not found.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void RefreshLikeUI()
        {
            if (_post == null) return;

            if (likeCountText != null)
                likeCountText.text = _post.likeCount.ToString();

            if (likeIcon != null)
                likeIcon.color = _post.isLikedByMe ? Color.red : Color.white;
        }

        private IEnumerator LoadThumbnail(string path)
        {
            if (screenshotImage == null || string.IsNullOrEmpty(path)) yield break;
            if (!System.IO.File.Exists(path)) yield break;

            byte[] bytes;
            try { bytes = System.IO.File.ReadAllBytes(path); }
            catch { yield break; }

            if (bytes == null || bytes.Length == 0) yield break;

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(bytes))
            {
                if (_loadedTexture != null) Destroy(_loadedTexture);
                _loadedTexture = tex;
                screenshotImage.texture = tex;
            }
            else
            {
                Destroy(tex);
            }

            yield return null;
        }

        private IEnumerator PunchScale(Transform target)
        {
            Vector3 original = target.localScale;
            Vector3 big      = original * 1.3f;
            float   duration = 0.15f;
            float   elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(original, big, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            target.localScale = original;
        }

        private void OnDestroy()
        {
            if (_loadedTexture != null) Destroy(_loadedTexture);
        }
    }
}
