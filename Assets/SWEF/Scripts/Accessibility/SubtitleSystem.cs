using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>Subtitle panel position on screen.</summary>
    public enum SubtitlePosition
    {
        /// <summary>Subtitles anchored near the top of the screen.</summary>
        Top,
        /// <summary>Subtitles anchored at vertical centre.</summary>
        Center,
        /// <summary>Subtitles anchored at the bottom (default; WCAG-recommended).</summary>
        Bottom
    }

    /// <summary>Pre-set text-size categories.</summary>
    public enum SubtitleTextSize
    {
        Small      = 18,
        Medium     = 24,
        Large      = 32,
        ExtraLarge = 42
    }

    // ── Data types ────────────────────────────────────────────────────────────────

    /// <summary>A single subtitle entry to be displayed.</summary>
    [Serializable]
    public class SubtitleEntry
    {
        /// <summary>Text to display. May include rich-text tags.</summary>
        public string text;
        /// <summary>Speaker name for colour-coded identification. Empty for narration.</summary>
        public string speakerName;
        /// <summary>Speaker colour override. Default used when Color.clear is passed.</summary>
        public Color  speakerColor;
        /// <summary>Explicit display duration in seconds. 0 = auto-calculate from reading speed.</summary>
        public float  duration;
        /// <summary>Localization key to look up text at display time. Overrides <see cref="text"/> if set.</summary>
        public string localizationKey;
    }

    /// <summary>
    /// Manages subtitle and closed-caption display for all spoken dialogue,
    /// narration, tutorial voice-lines, and ambient sound descriptions.
    /// Integrates with the Phase 30 Localization system.
    /// </summary>
    public class SubtitleSystem : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SubtitleSystem Instance { get; private set; }

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeySubtitlesEnabled     = "SWEF_SubtitlesEnabled";
        private const string KeyCaptionsEnabled      = "SWEF_CaptionsEnabled";
        private const string KeyTextSize             = "SWEF_SubtitleTextSize";
        private const string KeyPosition             = "SWEF_SubtitlePosition";
        private const string KeyBgOpacity            = "SWEF_SubtitleBgOpacity";
        private const string KeyMaxCharsPerLine      = "SWEF_SubtitleMaxChars";
        private const string KeyMinDisplayTime       = "SWEF_SubtitleMinTime";

        // ── Serialised fields ────────────────────────────────────────────────────
        [Header("Enable/Disable")]
        [SerializeField] private bool subtitlesEnabled = false;
        [SerializeField] private bool closedCaptionsEnabled = false;

        [Header("Display Settings")]
        [SerializeField] private SubtitleTextSize textSize   = SubtitleTextSize.Medium;
        [SerializeField] private SubtitlePosition position   = SubtitlePosition.Bottom;
        [SerializeField] [Range(0f, 1f)] private float backgroundOpacity = 0.6f;
        [SerializeField] [Range(20, 80)]  private int  maxCharsPerLine   = 42;
        [SerializeField] [Range(1f, 10f)] private float minDisplayTime   = 2f;
        /// <summary>Reading speed in characters per second for auto duration (≈ 21 cps = WCAG AA).</summary>
        [SerializeField] [Range(5f, 40f)] private float readingSpeedCps  = 21f;

        [Header("Speaker Colours")]
        [SerializeField] private Color defaultSpeakerColor = Color.cyan;

        [Header("UI References")]
        [SerializeField] private GameObject subtitlePanel;
        [SerializeField] private Text       subtitleText;
        [SerializeField] private Text       speakerNameText;
        [SerializeField] private Image      backgroundPanel;
        [SerializeField] private RectTransform panelRect;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly Queue<SubtitleEntry> _queue = new Queue<SubtitleEntry>();
        private Coroutine                     _displayRoutine;
        private SubtitleEntry                 _current;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired when a subtitle entry begins displaying.</summary>
        public event Action<SubtitleEntry> OnSubtitleShown;

        /// <summary>Fired when the currently displayed subtitle is dismissed.</summary>
        public event Action<SubtitleEntry> OnSubtitleDismissed;

        /// <summary>Fired when closed-caption mode is toggled.</summary>
        public event Action<bool> OnCaptionModeChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPreferences();
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a subtitle entry for display. Thread-safe (Unity main thread only).
        /// </summary>
        public void ShowSubtitle(SubtitleEntry entry)
        {
            if (entry == null || !subtitlesEnabled) return;
            _queue.Enqueue(entry);
            if (_displayRoutine == null)
                _displayRoutine = StartCoroutine(ProcessQueue());
        }

        /// <summary>
        /// Convenience overload: queues a subtitle with text, optional speaker, and optional duration.
        /// </summary>
        public void ShowSubtitle(string text, string speakerName = "", Color speakerColor = default, float duration = 0f)
        {
            ShowSubtitle(new SubtitleEntry
            {
                text         = text,
                speakerName  = speakerName,
                speakerColor = speakerColor == default ? defaultSpeakerColor : speakerColor,
                duration     = duration
            });
        }

        /// <summary>
        /// Queues a closed-caption sound description (e.g., "[Wind howling]").
        /// Only displayed when closed-captions mode is on.
        /// </summary>
        public void ShowSoundDescription(string description)
        {
            if (!closedCaptionsEnabled) return;
            ShowSubtitle(new SubtitleEntry
            {
                text         = $"<i>{description}</i>",
                speakerName  = string.Empty,
                speakerColor = Color.gray,
                duration     = minDisplayTime
            });
        }

        /// <summary>Clears the subtitle queue and hides the panel immediately.</summary>
        public void ClearAll()
        {
            _queue.Clear();
            if (_displayRoutine != null)
            {
                StopCoroutine(_displayRoutine);
                _displayRoutine = null;
            }
            HidePanel();
        }

        /// <summary>Enables or disables subtitles at runtime.</summary>
        public void SetSubtitlesEnabled(bool enabled)
        {
            subtitlesEnabled = enabled;
            PlayerPrefs.SetInt(KeySubtitlesEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled) ClearAll();
        }

        /// <summary>Enables or disables closed-captions mode at runtime.</summary>
        public void SetClosedCaptions(bool enabled)
        {
            closedCaptionsEnabled = enabled;
            PlayerPrefs.SetInt(KeyCaptionsEnabled, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnCaptionModeChanged?.Invoke(enabled);
        }

        /// <summary>Sets the text size category.</summary>
        public void SetTextSize(SubtitleTextSize size)
        {
            textSize = size;
            if (subtitleText != null) subtitleText.fontSize = (int)size;
            PlayerPrefs.SetInt(KeyTextSize, (int)size);
            PlayerPrefs.Save();
        }

        /// <summary>Sets the subtitle panel position.</summary>
        public void SetPosition(SubtitlePosition pos)
        {
            position = pos;
            ApplyPosition();
            PlayerPrefs.SetInt(KeyPosition, (int)pos);
            PlayerPrefs.Save();
        }

        /// <summary>Sets the background panel opacity (0–1).</summary>
        public void SetBackgroundOpacity(float opacity)
        {
            backgroundOpacity = Mathf.Clamp01(opacity);
            if (backgroundPanel != null)
            {
                Color c = backgroundPanel.color;
                c.a = backgroundOpacity;
                backgroundPanel.color = c;
            }
            PlayerPrefs.SetFloat(KeyBgOpacity, backgroundOpacity);
            PlayerPrefs.Save();
        }

        // ── Internal display ─────────────────────────────────────────────────────
        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                _current = _queue.Dequeue();
                string displayText = ResolveText(_current);
                displayText = WrapText(displayText, maxCharsPerLine);

                ShowPanel(displayText, _current.speakerName, _current.speakerColor);
                OnSubtitleShown?.Invoke(_current);

                float dur = _current.duration > 0f
                    ? _current.duration
                    : Mathf.Max(minDisplayTime, displayText.Length / Mathf.Max(readingSpeedCps, 1f));

                yield return new WaitForSecondsRealtime(dur);

                OnSubtitleDismissed?.Invoke(_current);
                _current = null;
            }
            HidePanel();
            _displayRoutine = null;
        }

        private string ResolveText(SubtitleEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.localizationKey))
            {
                // Attempt Phase 30 localization lookup
                var locMgr = SWEF.Localization.LocalizationManager.Instance;
                if (locMgr != null)
                {
                    string localised = locMgr.GetText(entry.localizationKey);
                    if (!string.IsNullOrEmpty(localised)) return localised;
                }
            }
            return entry.text ?? string.Empty;
        }

        private static string WrapText(string text, int maxChars)
        {
            if (text.Length <= maxChars) return text;
            var sb = new System.Text.StringBuilder();
            int start = 0;
            while (start < text.Length)
            {
                int end = Mathf.Min(start + maxChars, text.Length);
                // Back up to last space
                if (end < text.Length && text[end] != ' ')
                {
                    int space = text.LastIndexOf(' ', end, end - start);
                    if (space > start) end = space;
                }
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(text, start, end - start);
                start = end + (end < text.Length && text[end] == ' ' ? 1 : 0);
            }
            return sb.ToString();
        }

        private void ShowPanel(string text, string speaker, Color speakerColor)
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(true);
            if (subtitleText  != null) subtitleText.text = text;
            if (speakerNameText != null)
            {
                speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
                speakerNameText.text  = $"<b>{speaker}</b>";
                speakerNameText.color = speakerColor;
            }
            if (backgroundPanel != null)
            {
                Color c = backgroundPanel.color;
                c.a = backgroundOpacity;
                backgroundPanel.color = c;
            }
            ApplyPosition();
        }

        private void HidePanel()
        {
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
        }

        private void ApplyPosition()
        {
            if (panelRect == null) return;
            switch (position)
            {
                case SubtitlePosition.Top:
                    panelRect.anchorMin = new Vector2(0f, 1f);
                    panelRect.anchorMax = new Vector2(1f, 1f);
                    panelRect.pivot     = new Vector2(0.5f, 1f);
                    break;
                case SubtitlePosition.Center:
                    panelRect.anchorMin = new Vector2(0f, 0.5f);
                    panelRect.anchorMax = new Vector2(1f, 0.5f);
                    panelRect.pivot     = new Vector2(0.5f, 0.5f);
                    break;
                default: // Bottom
                    panelRect.anchorMin = new Vector2(0f, 0f);
                    panelRect.anchorMax = new Vector2(1f, 0f);
                    panelRect.pivot     = new Vector2(0.5f, 0f);
                    break;
            }
        }

        private void LoadPreferences()
        {
            subtitlesEnabled      = PlayerPrefs.GetInt(KeySubtitlesEnabled, 0) == 1;
            closedCaptionsEnabled = PlayerPrefs.GetInt(KeyCaptionsEnabled,  0) == 1;
            int textSizeRaw = PlayerPrefs.GetInt(KeyTextSize, (int)SubtitleTextSize.Medium);
            textSize = Enum.IsDefined(typeof(SubtitleTextSize), textSizeRaw)
                ? (SubtitleTextSize)textSizeRaw
                : SubtitleTextSize.Medium;
            int posRaw = PlayerPrefs.GetInt(KeyPosition, (int)SubtitlePosition.Bottom);
            position = Enum.IsDefined(typeof(SubtitlePosition), posRaw)
                ? (SubtitlePosition)posRaw
                : SubtitlePosition.Bottom;
            backgroundOpacity     = PlayerPrefs.GetFloat(KeyBgOpacity, 0.6f);
            maxCharsPerLine       = PlayerPrefs.GetInt(KeyMaxCharsPerLine, 42);
            minDisplayTime        = PlayerPrefs.GetFloat(KeyMinDisplayTime, 2f);
        }
    }
}
