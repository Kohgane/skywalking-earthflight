using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// HUD mini-player overlay (compact bar) for the In-Flight Music Player.
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item>Compact bar at the bottom of the screen showing title, artist, progress, controls.</item>
    ///   <item>Tap to expand to a full-player view.</item>
    ///   <item>Swipe right = next track, swipe left = previous track.</item>
    ///   <item>Slide-in / slide-out animations (skipped when <c>ReducedMotionEnabled</c>).</item>
    ///   <item>Colorblind-safe UI colours via <see cref="AccessibilityController"/>.</item>
    ///   <item>Localised strings via <see cref="SWEF.Localization.LocalizationManager"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class MusicPlayerUI : MonoBehaviour
    {
        // ── Inspector — Mini-player ────────────────────────────────────────────────
        [Header("Mini-Player Panel")]
        [Tooltip("Root RectTransform of the compact mini-player bar.")]
        [SerializeField] private RectTransform miniPlayerPanel;

        [Tooltip("Label showing the current track title.")]
        [SerializeField] private Text trackTitleLabel;

        [Tooltip("Label showing the current artist.")]
        [SerializeField] private Text artistLabel;

        [Tooltip("Image displaying the album art thumbnail.")]
        [SerializeField] private Image albumArtImage;

        [Tooltip("Progress bar slider (value 0–1, read-only display).")]
        [SerializeField] private Slider progressBar;

        [Tooltip("Volume slider (value 0–1).")]
        [SerializeField] private Slider volumeSlider;

        [Tooltip("Play/Pause button.")]
        [SerializeField] private Button playPauseButton;

        [Tooltip("Image used as the play/pause button icon.")]
        [SerializeField] private Image playPauseIcon;

        [Tooltip("Sprite used when the player is paused/stopped (play arrow).")]
        [SerializeField] private Sprite playSprite;

        [Tooltip("Sprite used when the player is playing (pause bars).")]
        [SerializeField] private Sprite pauseSprite;

        [Tooltip("Next track button.")]
        [SerializeField] private Button nextButton;

        [Tooltip("Previous track button.")]
        [SerializeField] private Button prevButton;

        // ── Inspector — Full-Player Panel ─────────────────────────────────────────
        [Header("Full-Player Panel")]
        [Tooltip("Root RectTransform of the expanded full-player view.")]
        [SerializeField] private RectTransform fullPlayerPanel;

        [Tooltip("Close / collapse button on the full-player panel.")]
        [SerializeField] private Button collapseButton;

        // ── Inspector — Animation ─────────────────────────────────────────────────
        [Header("Animation")]
        [Tooltip("Seconds for the slide-in / slide-out animation.")]
        [SerializeField] private float slideAnimationDuration = 0.3f;

        [Tooltip("Off-screen Y offset in pixels for the slide animation.")]
        [SerializeField] private float slideOffsetY = 120f;

        // ── Inspector — Swipe Gesture ─────────────────────────────────────────────
        [Header("Swipe Gesture")]
        [Tooltip("Minimum horizontal swipe distance (pixels) to trigger next/previous.")]
        [SerializeField] private float swipeThreshold = 80f;

        // ── Private state ─────────────────────────────────────────────────────────
        private bool               _isExpanded;
        private bool               _visible;
        private bool               _ignoreSliderCallback;
        private Vector2            _touchStart;
        private bool               _isSwiping;
        private Coroutine          _slideCoroutine;
        private Button             _miniPlayerButton;
        private AccessibilityController _accessibilityController;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _accessibilityController = FindFirstObjectByType<AccessibilityController>();
            if (miniPlayerPanel != null)
                _miniPlayerButton = miniPlayerPanel.GetComponent<Button>();
        }

        private void Start()
        {
            // Wire up button callbacks
            if (playPauseButton != null) playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            if (nextButton      != null) nextButton.onClick.AddListener(OnNextClicked);
            if (prevButton      != null) prevButton.onClick.AddListener(OnPrevClicked);
            if (collapseButton  != null) collapseButton.onClick.AddListener(CollapseFullPlayer);

            if (_miniPlayerButton != null)
                _miniPlayerButton.onClick.AddListener(ExpandFullPlayer);

            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

            // Subscribe to manager events
            if (MusicPlayerManager.Instance != null)
            {
                MusicPlayerManager.Instance.OnTrackChanged         += HandleTrackChanged;
                MusicPlayerManager.Instance.OnPlaybackStateChanged += HandlePlaybackStateChanged;
                MusicPlayerManager.Instance.OnVolumeChanged        += HandleVolumeChanged;

                // Sync initial state
                SyncToCurrentState();
            }

            // Hide full-player initially
            if (fullPlayerPanel != null) fullPlayerPanel.gameObject.SetActive(false);

            Show();
        }

        private void OnDestroy()
        {
            if (MusicPlayerManager.Instance != null)
            {
                MusicPlayerManager.Instance.OnTrackChanged         -= HandleTrackChanged;
                MusicPlayerManager.Instance.OnPlaybackStateChanged -= HandlePlaybackStateChanged;
                MusicPlayerManager.Instance.OnVolumeChanged        -= HandleVolumeChanged;
            }
        }

        private void Update()
        {
            UpdateProgressBar();
            HandleSwipeInput();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Slides the mini-player into view.</summary>
        public void Show()
        {
            if (_visible) return;
            _visible = true;
            if (miniPlayerPanel != null)
            {
                miniPlayerPanel.gameObject.SetActive(true);
                AnimateSlide(true);
            }
        }

        /// <summary>Slides the mini-player out of view.</summary>
        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            AnimateSlide(false);
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleTrackChanged(MusicTrack track)
        {
            if (track == null) return;

            if (trackTitleLabel != null)
                trackTitleLabel.text = track.title;

            if (artistLabel != null)
                artistLabel.text = track.artist;

            // Album art would be loaded asynchronously in a production build
            if (albumArtImage != null)
                albumArtImage.color = GetAccessibleColor(Color.white);
        }

        private void HandlePlaybackStateChanged(PlaybackState state)
        {
            UpdatePlayPauseIcon(state == PlaybackState.Playing);
        }

        private void HandleVolumeChanged(float volume)
        {
            if (volumeSlider == null) return;
            _ignoreSliderCallback = true;
            volumeSlider.value    = volume;
            _ignoreSliderCallback = false;
        }

        // ── Button callbacks ──────────────────────────────────────────────────────

        private void OnPlayPauseClicked()
        {
            if (MusicPlayerManager.Instance == null) return;
            if (MusicPlayerManager.Instance.CurrentPlaybackState == PlaybackState.Playing)
                MusicPlayerManager.Instance.Pause();
            else
                MusicPlayerManager.Instance.Play();
        }

        private void OnNextClicked()
        {
            MusicPlayerManager.Instance?.NextTrack();
        }

        private void OnPrevClicked()
        {
            MusicPlayerManager.Instance?.PreviousTrack();
        }

        private void OnVolumeSliderChanged(float value)
        {
            if (_ignoreSliderCallback) return;
            MusicPlayerManager.Instance?.SetVolume(value);
        }

        // ── Expand / Collapse ─────────────────────────────────────────────────────

        private void ExpandFullPlayer()
        {
            if (_isExpanded) return;
            _isExpanded = true;
            if (fullPlayerPanel != null) fullPlayerPanel.gameObject.SetActive(true);
        }

        private void CollapseFullPlayer()
        {
            if (!_isExpanded) return;
            _isExpanded = false;
            if (fullPlayerPanel != null) fullPlayerPanel.gameObject.SetActive(false);
        }

        // ── Progress ──────────────────────────────────────────────────────────────

        private void UpdateProgressBar()
        {
            if (progressBar == null || MusicPlayerManager.Instance == null) return;
            progressBar.value = MusicPlayerManager.Instance.GetPlaybackProgress();
        }

        // ── Swipe gesture ─────────────────────────────────────────────────────────

        private void HandleSwipeInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _touchStart = touch.position;
                        _isSwiping  = true;
                        break;

                    case TouchPhase.Ended:
                        if (_isSwiping)
                        {
                            float delta = touch.position.x - _touchStart.x;
                            if (Mathf.Abs(delta) >= swipeThreshold)
                            {
                                if (delta > 0)
                                    MusicPlayerManager.Instance?.NextTrack();
                                else
                                    MusicPlayerManager.Instance?.PreviousTrack();
                            }
                        }
                        _isSwiping = false;
                        break;

                    case TouchPhase.Canceled:
                        _isSwiping = false;
                        break;
                }
            }
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        private void AnimateSlide(bool slideIn)
        {
            if (miniPlayerPanel == null) return;

            bool reducedMotion = _accessibilityController != null
                && _accessibilityController.ReducedMotionEnabled;

            if (reducedMotion || slideAnimationDuration <= 0f)
            {
                Vector2 pos = miniPlayerPanel.anchoredPosition;
                pos.y = slideIn ? 0f : -slideOffsetY;
                miniPlayerPanel.anchoredPosition = pos;
                if (!slideIn) miniPlayerPanel.gameObject.SetActive(false);
                return;
            }

            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideCoroutine(slideIn));
        }

        private IEnumerator SlideCoroutine(bool slideIn)
        {
            float elapsed  = 0f;
            float duration = slideAnimationDuration;

            Vector2 startPos = miniPlayerPanel.anchoredPosition;
            float   targetY  = slideIn ? 0f : -slideOffsetY;
            Vector2 endPos   = new Vector2(startPos.x, targetY);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                miniPlayerPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            miniPlayerPanel.anchoredPosition = endPos;

            if (!slideIn)
                miniPlayerPanel.gameObject.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SyncToCurrentState()
        {
            if (MusicPlayerManager.Instance == null) return;

            MusicTrack current = MusicPlayerManager.Instance.GetCurrentTrack();
            if (current != null) HandleTrackChanged(current);

            HandlePlaybackStateChanged(MusicPlayerManager.Instance.CurrentPlaybackState);

            float vol = MusicPlayerManager.Instance.State.volume;
            if (volumeSlider != null)
            {
                _ignoreSliderCallback = true;
                volumeSlider.value    = vol;
                _ignoreSliderCallback = false;
            }
        }

        private void UpdatePlayPauseIcon(bool isPlaying)
        {
            if (playPauseIcon == null) return;
            playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
        }

        private Color GetAccessibleColor(Color original)
        {
            return _accessibilityController != null
                ? _accessibilityController.GetAccessibleColor(original)
                : original;
        }

        private string Localize(string key)
        {
            return SWEF.Localization.LocalizationManager.Instance != null
                ? SWEF.Localization.LocalizationManager.Instance.GetText(key)
                : key;
        }
    }
}
