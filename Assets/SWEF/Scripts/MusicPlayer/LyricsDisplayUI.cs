using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SWEF.UI;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Display mode for the Lyrics UI panel.
    /// </summary>
    public enum LyricsDisplayMode
    {
        /// <summary>Full scrolling list of all lines.</summary>
        Scroll,
        /// <summary>Compact HUD showing only the current and next line.</summary>
        TwoLine,
        /// <summary>Word-by-word karaoke highlight with scroll.</summary>
        Karaoke,
        /// <summary>Lyrics loaded but panel is invisible.</summary>
        Hidden
    }

    /// <summary>
    /// UGUI panel that displays synchronised lyrics from a <see cref="KaraokeController"/>.
    /// <para>
    /// Supports three visible display modes:
    /// <list type="bullet">
    ///   <item><see cref="LyricsDisplayMode.Scroll"/> — scrolling list with current line highlighted.</item>
    ///   <item><see cref="LyricsDisplayMode.TwoLine"/> — current + next line compact overlay.</item>
    ///   <item><see cref="LyricsDisplayMode.Karaoke"/> — word-by-word colour fill, auto-scroll.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Touch gestures: swipe up/down to manually scroll; tap a line to seek to its timestamp.
    /// Font size integrates with <c>AccessibilityController</c> when present.
    /// </para>
    /// </summary>
    public class LyricsDisplayUI : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const float FadeInDuration         = 0.25f;
        private const float FadeOutDuration        = 0.25f;
        private const float ScrollAnimationSeconds = 0.3f;
        private const float SwipeThreshold         = 40f;
        private const float LineTapSlop            = 20f;
        private const string LogTag                = "[SWEF][LyricsDisplayUI]";

        // ── Inspector — Root ──────────────────────────────────────────────────────

        [Header("Root Panel")]
        [Tooltip("Root CanvasGroup for fade-in/out animation.")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        // ── Inspector — Scroll Mode ────────────────────────────────────────────────

        [Header("Scroll Mode")]
        [Tooltip("ScrollRect for the full scrolling lyrics list.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("Content RectTransform inside the ScrollRect.")]
        [SerializeField] private RectTransform scrollContent;

        [Tooltip("Prefab for a single lyric line row (must contain a Text or Text component).")]
        [SerializeField] private GameObject lyricLinePrefab;

        // ── Inspector — Two-Line Mode ──────────────────────────────────────────────

        [Header("Two-Line Mode")]
        [Tooltip("Label showing the current lyric line.")]
        [SerializeField] private Text currentLineLabel;

        [Tooltip("Label showing the upcoming lyric line.")]
        [SerializeField] private Text nextLineLabel;

        // ── Inspector — Colours ────────────────────────────────────────────────────

        [Header("Colours")]
        [Tooltip("Colour for the active/current lyric line.")]
        [SerializeField] private Color activeColor   = new Color(1f, 0.85f, 0.2f);   // amber

        [Tooltip("Colour for upcoming lyric lines.")]
        [SerializeField] private Color upcomingColor = new Color(1f, 1f, 1f, 0.7f);

        [Tooltip("Colour for past lyric lines.")]
        [SerializeField] private Color pastColor     = new Color(1f, 1f, 1f, 0.35f);

        [Tooltip("Karaoke fill colour (word highlight).")]
        [SerializeField] private Color karaokeHighlightColor = new Color(1f, 0.4f, 0.4f);

        // ── Inspector — Font Size ─────────────────────────────────────────────────

        [Header("Font")]
        [Tooltip("Base font size for lyric lines.")]
        [SerializeField] private int baseFontSize = 24;

        [Tooltip("Font size multiplier for the active line (Scroll / Karaoke modes).")]
        [SerializeField] private float activeLineSizeMultiplier = 1.3f;

        // ── Inspector — Animation ─────────────────────────────────────────────────

        [Header("Animation")]
        [Tooltip("Duration in seconds for the auto-scroll animation.")]
        [SerializeField] private float scrollDuration = ScrollAnimationSeconds;

        [Header("Gesture")]
        [Tooltip("Seconds after a manual swipe before auto-scroll resumes.")]
        [SerializeField] private float manualScrollCooldown = 3f;

        // ── Private state ─────────────────────────────────────────────────────────

        private LyricsDisplayMode      _mode = LyricsDisplayMode.Hidden;
        private KaraokeController      _karaoke;
        private AccessibilityController _accessibility;
        private List<Text>             _lineLabels = new List<Text>();
        private int                    _lastLineIndex = -1;
        private Coroutine              _fadeCoroutine;
        private Coroutine              _scrollCoroutine;
        private Vector2                _touchStart;
        private bool                   _isSwiping;
        private bool                   _manualScroll;
        private float                  _manualScrollTimer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _karaoke       = FindFirstObjectByType<KaraokeController>();
            _accessibility = FindFirstObjectByType<AccessibilityController>();
        }

        private void Start()
        {
            if (_karaoke != null)
            {
                _karaoke.OnLineChanged   += HandleLineChanged;
                _karaoke.OnWordProgress  += HandleWordProgress;
                _karaoke.OnLyricsStarted += HandleLyricsStarted;
                _karaoke.OnLyricsEnded   += HandleLyricsEnded;
            }

            // Respect colorblind palette
            ApplyAccessibleColors();

            SetMode(LyricsDisplayMode.Hidden);
        }

        private void OnDestroy()
        {
            if (_karaoke != null)
            {
                _karaoke.OnLineChanged   -= HandleLineChanged;
                _karaoke.OnWordProgress  -= HandleWordProgress;
                _karaoke.OnLyricsStarted -= HandleLyricsStarted;
                _karaoke.OnLyricsEnded   -= HandleLyricsEnded;
            }
        }

        private void Update()
        {
            HandleTouchInput();

            if (_manualScroll)
            {
                _manualScrollTimer -= Time.deltaTime;
                if (_manualScrollTimer <= 0f)
                    _manualScroll = false;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the active display mode and animates the panel accordingly.</summary>
        public void SetMode(LyricsDisplayMode mode)
        {
            _mode = mode;

            bool visible = mode != LyricsDisplayMode.Hidden;

            if (rootCanvasGroup != null)
                AnimateFade(visible);

            RefreshScrollModeVisibility();
            RefreshTwoLineModeVisibility();

            if (visible && _karaoke != null && _karaoke.CurrentLyrics != null)
                BuildScrollLines(_karaoke.CurrentLyrics);
        }

        /// <summary>Returns the current display mode.</summary>
        public LyricsDisplayMode CurrentMode => _mode;

        /// <summary>
        /// Rebuilds the scroll-mode line list from the supplied lyrics data.
        /// Call this whenever new lyrics are loaded.
        /// </summary>
        public void BuildScrollLines(LrcData data)
        {
            if (scrollContent == null || lyricLinePrefab == null) return;

            // Clear old lines
            foreach (var lbl in _lineLabels)
                if (lbl != null) Destroy(lbl.gameObject);
            _lineLabels.Clear();

            if (data == null || !data.HasLyrics) return;

            foreach (LrcLine line in data.lines)
            {
                GameObject go  = Instantiate(lyricLinePrefab, scrollContent);
                Text       txt = go.GetComponentInChildren<Text>();
                if (txt == null)
                {
                    Debug.LogWarning($"{LogTag} lyricLinePrefab has no Text component.");
                    continue;
                }

                txt.text     = line.text;
                txt.fontSize = baseFontSize;
                txt.color    = upcomingColor;

                // Wire tap-to-seek
                int capturedIndex = _lineLabels.Count;
                Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                btn.onClick.AddListener(() => OnLineTapped(capturedIndex));

                _lineLabels.Add(txt);
            }

            _lastLineIndex = -1;
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleLineChanged(int index, LrcLine line)
        {
            switch (_mode)
            {
                case LyricsDisplayMode.Scroll:
                    UpdateScrollHighlight(index);
                    if (!_manualScroll) AutoScrollToLine(index);
                    break;

                case LyricsDisplayMode.TwoLine:
                    UpdateTwoLineDisplay();
                    break;

                case LyricsDisplayMode.Karaoke:
                    UpdateScrollHighlight(index);
                    if (!_manualScroll) AutoScrollToLine(index);
                    UpdateKaraokeLine(index, 0, 0f);
                    break;
            }

            _lastLineIndex = index;
        }

        private void HandleWordProgress(int wordIndex, float progress)
        {
            if (_mode == LyricsDisplayMode.Karaoke)
                UpdateKaraokeLine(_lastLineIndex, wordIndex, progress);
        }

        private void HandleLyricsStarted()
        {
            if (_mode != LyricsDisplayMode.Hidden)
                AnimateFade(true);
        }

        private void HandleLyricsEnded()
        {
            // Optionally fade out after lyrics end — keep visible for user to review
        }

        // ── Line display helpers ──────────────────────────────────────────────────

        private void UpdateScrollHighlight(int activeIndex)
        {
            for (int i = 0; i < _lineLabels.Count; i++)
            {
                Text lbl = _lineLabels[i];
                if (lbl == null) continue;

                if (i == activeIndex)
                {
                    lbl.color    = GetAccessibleColor(activeColor);
                    lbl.fontSize = Mathf.RoundToInt(baseFontSize * GetFontScale() * activeLineSizeMultiplier);
                }
                else if (i < activeIndex)
                {
                    lbl.color    = GetAccessibleColor(pastColor);
                    lbl.fontSize = Mathf.RoundToInt(baseFontSize * GetFontScale());
                }
                else
                {
                    lbl.color    = GetAccessibleColor(upcomingColor);
                    lbl.fontSize = Mathf.RoundToInt(baseFontSize * GetFontScale());
                }
            }
        }

        private void UpdateTwoLineDisplay()
        {
            if (_karaoke == null) return;

            if (currentLineLabel != null)
            {
                LrcLine cur = _karaoke.CurrentLine;
                currentLineLabel.text  = cur != null ? cur.text : string.Empty;
                currentLineLabel.color = GetAccessibleColor(activeColor);
            }

            if (nextLineLabel != null)
            {
                LrcLine nxt = _karaoke.NextLine;
                nextLineLabel.text  = nxt != null ? nxt.text : string.Empty;
                nextLineLabel.color = GetAccessibleColor(upcomingColor);
            }
        }

        private void UpdateKaraokeLine(int lineIndex, int wordIndex, float progress)
        {
            if (lineIndex < 0 || lineIndex >= _lineLabels.Count) return;
            if (_karaoke == null || _karaoke.CurrentLyrics == null) return;

            Text lbl = _lineLabels[lineIndex];
            if (lbl == null) return;

            LrcLine line = _karaoke.CurrentLyrics.lines[lineIndex];

            if (!line.HasWordTiming)
            {
                lbl.color = GetAccessibleColor(activeColor);
                return;
            }

            // Build rich-text string where completed words use highlight colour
            var sb = new StringBuilder();
            string highlightHex = ColorUtility.ToHtmlStringRGBA(
                GetAccessibleColor(karaokeHighlightColor));
            string defaultHex   = ColorUtility.ToHtmlStringRGBA(
                GetAccessibleColor(activeColor));

            for (int i = 0; i < line.words.Count; i++)
            {
                string word = line.words[i].text;
                if (i < wordIndex)
                {
                    sb.Append($"<color=#{highlightHex}>{word}</color>");
                }
                else if (i == wordIndex)
                {
                    // Partial fill — approximate via colour blend
                    Color blended = Color.Lerp(
                        GetAccessibleColor(activeColor),
                        GetAccessibleColor(karaokeHighlightColor),
                        progress);
                    string blendedHex = ColorUtility.ToHtmlStringRGBA(blended);
                    sb.Append($"<color=#{blendedHex}>{word}</color>");
                }
                else
                {
                    sb.Append($"<color=#{defaultHex}>{word}</color>");
                }

                if (i < line.words.Count - 1) sb.Append(' ');
            }

            lbl.text = sb.ToString();
        }

        // ── Scroll ────────────────────────────────────────────────────────────────

        private void AutoScrollToLine(int index)
        {
            if (scrollRect == null || _lineLabels.Count == 0 || index < 0) return;

            float normalizedPos = 1f - (float)index / Mathf.Max(1, _lineLabels.Count - 1);
            normalizedPos = Mathf.Clamp01(normalizedPos);

            if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(SmoothScrollCoroutine(normalizedPos));
        }

        private IEnumerator SmoothScrollCoroutine(float targetNormalized)
        {
            if (scrollRect == null) yield break;

            float start   = scrollRect.verticalNormalizedPosition;
            float elapsed = 0f;

            while (elapsed < scrollDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / scrollDuration);
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, targetNormalized, t);
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = targetNormalized;
        }

        // ── Touch input ───────────────────────────────────────────────────────────

        private void HandleTouchInput()
        {
            if (Input.touchCount != 1) return;

            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStart = touch.position;
                    _isSwiping  = false;
                    break;

                case TouchPhase.Moved:
                    float deltaY = touch.position.y - _touchStart.y;
                    if (Mathf.Abs(deltaY) > SwipeThreshold && scrollRect != null)
                    {
                        _isSwiping    = true;
                        _manualScroll = true;
                        _manualScrollTimer = manualScrollCooldown;
                    }
                    break;

                case TouchPhase.Ended:
                    if (!_isSwiping)
                    {
                        // Tap — try to seek to tapped line
                        TrySeekByTap(touch.position);
                    }
                    _isSwiping = false;
                    break;

                case TouchPhase.Canceled:
                    _isSwiping = false;
                    break;
            }
        }

        private void TrySeekByTap(Vector2 screenPos)
        {
            if (_karaoke == null || _karaoke.CurrentLyrics == null) return;

            for (int i = 0; i < _lineLabels.Count; i++)
            {
                Text lbl = _lineLabels[i];
                if (lbl == null) continue;

                RectTransform rt = lbl.rectTransform;
                if (rt == null) continue;

                // Convert screen pos to local space
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rt, screenPos, null, out Vector2 local))
                {
                    if (rt.rect.Contains(local + Vector2.one * LineTapSlop * 0.5f))
                    {
                        OnLineTapped(i);
                        return;
                    }
                }
            }
        }

        private void OnLineTapped(int lineIndex)
        {
            if (_karaoke == null || _karaoke.CurrentLyrics == null) return;
            if (lineIndex < 0 || lineIndex >= _karaoke.CurrentLyrics.lines.Count) return;

            float ts = _karaoke.CurrentLyrics.lines[lineIndex].timestamp;
            if (MusicPlayerManager.Instance == null) return;

            float duration = MusicPlayerManager.Instance.GetCurrentTrackDuration();
            if (duration > 0f)
                MusicPlayerManager.Instance.Seek(ts / duration);
        }

        // ── Visibility helpers ────────────────────────────────────────────────────

        private void RefreshScrollModeVisibility()
        {
            bool show = _mode == LyricsDisplayMode.Scroll || _mode == LyricsDisplayMode.Karaoke;
            if (scrollRect != null)
                scrollRect.gameObject.SetActive(show);
        }

        private void RefreshTwoLineModeVisibility()
        {
            bool show = _mode == LyricsDisplayMode.TwoLine;
            if (currentLineLabel != null) currentLineLabel.gameObject.SetActive(show);
            if (nextLineLabel     != null) nextLineLabel.gameObject.SetActive(show);
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        private void AnimateFade(bool fadeIn)
        {
            if (rootCanvasGroup == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCoroutine(fadeIn));
        }

        private IEnumerator FadeCoroutine(bool fadeIn)
        {
            float start    = rootCanvasGroup.alpha;
            float target   = fadeIn ? 1f : 0f;
            float duration = fadeIn ? FadeInDuration : FadeOutDuration;
            float elapsed  = 0f;

            rootCanvasGroup.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rootCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            rootCanvasGroup.alpha = target;
            if (!fadeIn)
                rootCanvasGroup.gameObject.SetActive(false);
        }

        // ── Accessibility / colour helpers ────────────────────────────────────────

        private void ApplyAccessibleColors()
        {
            if (_accessibility == null
                || _accessibility.ActiveColorblindMode == ColorblindMode.Normal)
                return;

            // Replace amber/red highlights with colorblind-safe tones
            activeColor            = new Color(0f,   0.6f, 1f,   1f);  // blue
            karaokeHighlightColor  = new Color(0.2f, 0.8f, 0.2f, 1f);  // green
        }

        private Color GetAccessibleColor(Color original)
        {
            return _accessibility != null
                ? _accessibility.GetAccessibleColor(original)
                : original;
        }

        private float GetFontScale()
        {
            return _accessibility != null && _accessibility.TextScaleMultiplier > 0f
                ? _accessibility.TextScaleMultiplier
                : 1f;
        }
    }
}
