using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Core;
using SWEF.Recorder;

namespace SWEF.Social
{
    /// <summary>
    /// Post-creation dialog.
    /// Open with <see cref="Open(string)"/> passing the path of the screenshot to share.
    /// The panel loads the screenshot preview, auto-populates the current GPS location,
    /// and calls <see cref="SocialFeedManager.CreatePost"/> when the player taps Post.
    /// </summary>
    public class PostComposerUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject composerPanel;
        [SerializeField] private CanvasGroup composerCanvasGroup;

        [Header("Content")]
        [SerializeField] private RawImage  previewImage;
        [SerializeField] private InputField captionInput;
        [SerializeField] private Text       locationLabel;
        [SerializeField] private Text       altitudeLabel;
        [SerializeField] private Text       characterCountText;

        [Header("Buttons")]
        [SerializeField] private Button postButton;
        [SerializeField] private Button cancelButton;

        [Header("Options")]
        [SerializeField] private Toggle includeFlightDataToggle;

        // ── Constants ────────────────────────────────────────────────────────
        private const int MaxCaptionLength = 280;

        // ── Private state ────────────────────────────────────────────────────
        private string    _screenshotPath;
        private Texture2D _previewTexture;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (postButton   != null) postButton.onClick.AddListener(OnPost);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);

            if (captionInput != null)
                captionInput.onValueChanged.AddListener(OnCaptionChanged);

            if (composerPanel != null) composerPanel.SetActive(false);
            SetAlpha(0f);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Opens the composer, loads <paramref name="screenshotPath"/> into the preview,
        /// and populates the current GPS location from <see cref="SWEFSession"/>.
        /// </summary>
        public void Open(string screenshotPath)
        {
            _screenshotPath = screenshotPath;

            if (composerPanel != null) composerPanel.SetActive(true);

            // Populate location labels
            if (locationLabel != null)
                locationLabel.text = SWEFSession.HasFix
                    ? $"{SWEFSession.Lat:F4}°, {SWEFSession.Lon:F4}°"
                    : "Location unavailable";

            if (altitudeLabel != null)
                altitudeLabel.text = SWEFSession.HasFix
                    ? $"{SWEFSession.Alt:F0} m"
                    : string.Empty;

            // Reset caption
            if (captionInput != null)
            {
                captionInput.text = string.Empty;
                captionInput.characterLimit = MaxCaptionLength;
            }

            UpdateCharacterCount(string.Empty);

            // Fade in
            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, 1f, 0.2f));

            // Load preview
            if (!string.IsNullOrEmpty(screenshotPath))
                StartCoroutine(LoadPreview(screenshotPath));
        }

        /// <summary>Closes the composer without creating a post.</summary>
        public void ClosePanel()
        {
            StopAllCoroutines();
            StartCoroutine(FadePanel(1f, 0f, 0.2f, () =>
            {
                if (composerPanel != null) composerPanel.SetActive(false);
                CleanupPreviewTexture();
            }));
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void OnPost()
        {
            if (SocialFeedManager.Instance == null)
            {
                Debug.LogWarning("[SWEF] PostComposerUI: SocialFeedManager not found.");
                ClosePanel();
                return;
            }

            string caption = captionInput != null ? captionInput.text : string.Empty;
            var post = SocialFeedManager.Instance.CreatePost(_screenshotPath, caption);

            // Optionally attach flight recorder ID
            if (includeFlightDataToggle != null && includeFlightDataToggle.isOn)
            {
                var recorder = FindFirstObjectByType<FlightRecorder>();
                if (recorder != null)
                {
                    // Use the recorder's GameObject name as a stable session tag
                    // (a real integration would use a persistent recording file ID)
                    post.flightDataId = recorder.gameObject.name + "_" +
                                        System.DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    Debug.Log($"[SWEF] PostComposerUI: attached flight data id {post.flightDataId}");
                }
            }

            // Track screenshot stat
            if (CommunityProfileManager.Instance != null)
                CommunityProfileManager.Instance.IncrementStat("totalScreenshots");

            Debug.Log($"[SWEF] PostComposerUI: post created — {post.postId}");
            ClosePanel();
        }

        private void OnCancel() => ClosePanel();

        private void OnCaptionChanged(string value)
        {
            // Clamp to max length
            if (value.Length > MaxCaptionLength)
            {
                captionInput.text = value.Substring(0, MaxCaptionLength);
                return;
            }
            UpdateCharacterCount(value);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void UpdateCharacterCount(string value)
        {
            if (characterCountText != null)
                characterCountText.text = $"{value.Length}/{MaxCaptionLength}";
        }

        private IEnumerator LoadPreview(string path)
        {
            if (previewImage == null) yield break;
            if (!System.IO.File.Exists(path)) yield break;

            byte[] bytes;
            try { bytes = System.IO.File.ReadAllBytes(path); }
            catch { yield break; }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(bytes))
            {
                CleanupPreviewTexture();
                _previewTexture = tex;
                previewImage.texture = tex;
            }
            else
            {
                Destroy(tex);
            }

            yield return null;
        }

        private void CleanupPreviewTexture()
        {
            if (_previewTexture != null)
            {
                Destroy(_previewTexture);
                _previewTexture = null;
            }
            if (previewImage != null) previewImage.texture = null;
        }

        private void SetAlpha(float alpha)
        {
            if (composerCanvasGroup != null) composerCanvasGroup.alpha = alpha;
        }

        private IEnumerator FadePanel(float from, float to, float duration,
            System.Action onComplete = null)
        {
            float elapsed = 0f;
            SetAlpha(from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            SetAlpha(to);
            onComplete?.Invoke();
        }

        private void OnDestroy() => CleanupPreviewTexture();
    }
}
