// SubtitleController.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Accessibility
{
    // SubtitlePosition enum is defined in SubtitleSystem.cs (Top / Center / Bottom) — same SWEF.Accessibility namespace.
    // SubtitleSize enum is defined in AccessibilityProfile.cs (Small / Medium / Large / ExtraLarge) — same SWEF.Accessibility namespace.

    /// <summary>
    /// Singleton MonoBehaviour that renders subtitles for voice commands, ATC audio,
    /// the cockpit voice assistant, and multiplayer chat.
    ///
    /// <para>Supports configurable font size (four tiers), optional background panel,
    /// speaker identification with per-speaker colour coding, auto-fade, and a
    /// FIFO queue for multiple simultaneous speakers.</para>
    /// </summary>
    public class SubtitleController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SubtitleController Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("UI References")]
        [SerializeField] private Canvas       subtitleCanvas;
        [SerializeField] private Text         subtitleText;
        [SerializeField] private Text         speakerText;
        [SerializeField] private Image        backgroundPanel;
        [SerializeField] private RectTransform subtitleRoot;

        [Header("Display Settings")]
        [SerializeField] private SubtitlePosition position        = SubtitlePosition.Bottom;
        [SerializeField] private SubtitleSize     textSize        = SubtitleSize.Medium;
        [SerializeField] private bool             showBackground  = true;
        [SerializeField] private float            bgOpacity       = 0.65f;
        [SerializeField] private float            fadeOutDuration = 0.4f;
        [SerializeField] private float            charsPerSecond  = 20f;

        [Header("Speaker Colours")]
        [SerializeField] private Color defaultSpeakerColor = Color.yellow;
        [SerializeField] private Color atcSpeakerColor     = new Color(0.4f, 0.9f, 1f);
        [SerializeField] private Color assistantColor      = new Color(0.6f, 1f, 0.6f);

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Queue<(string speaker, string text, float duration, Color color)> _queue
            = new Queue<(string, string, float, Color)>();

        private Coroutine _displayCoroutine;
        private bool      _isShowing;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetVisible(false);
        }

        private void Start()
        {
            // Sync with AccessibilityManager if present
            if (AccessibilityManager.Instance != null)
            {
                ApplyProfile(AccessibilityManager.Instance.Profile);
                AccessibilityManager.Instance.OnProfileChanged += () =>
                    ApplyProfile(AccessibilityManager.Instance.Profile);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Queues a subtitle entry.  When no subtitle is active the entry shows immediately.
        /// </summary>
        /// <param name="speaker">Speaker name (empty = narration only).</param>
        /// <param name="text">Text to display.</param>
        /// <param name="duration">Display duration in seconds; 0 = auto-calculate from reading speed.</param>
        /// <param name="speakerColor">Override speaker label colour; <see cref="Color.clear"/> = use default.</param>
        public void ShowSubtitle(string speaker, string text, float duration = 0f, Color speakerColor = default)
        {
            if (AccessibilityManager.Instance != null && !AccessibilityManager.Instance.Profile.subtitleEnabled)
                return;

            if (speakerColor == default || speakerColor == Color.clear)
                speakerColor = ResolveSpeakerColor(speaker);

            if (duration <= 0f)
                duration = Mathf.Max(1.5f, text.Length / charsPerSecond);

            _queue.Enqueue((speaker, text, duration, speakerColor));

            if (!_isShowing)
                _displayCoroutine = StartCoroutine(DisplayQueue());
        }

        /// <summary>Immediately clears the current subtitle and drains the queue.</summary>
        public void ClearAll()
        {
            _queue.Clear();
            if (_displayCoroutine != null) StopCoroutine(_displayCoroutine);
            _isShowing = false;
            SetVisible(false);
        }

        // ── Settings sync ─────────────────────────────────────────────────────────

        /// <summary>Applies settings from the given <see cref="AccessibilityProfile"/>.</summary>
        public void ApplyProfile(AccessibilityProfile p)
        {
            textSize       = p.subtitleSize;
            showBackground = p.subtitleBackground;
            bgOpacity      = p.subtitleBackgroundOpacity;

            if (subtitleText != null)
                subtitleText.fontSize = (int)textSize;

            if (backgroundPanel != null)
            {
                var c = backgroundPanel.color;
                c.a = showBackground ? bgOpacity : 0f;
                backgroundPanel.color = c;
            }
        }

        // ── Coroutine ─────────────────────────────────────────────────────────────

        private IEnumerator DisplayQueue()
        {
            _isShowing = true;

            while (_queue.Count > 0)
            {
                var (speaker, text, duration, color) = _queue.Dequeue();

                // Show
                SetContent(speaker, text, color);
                SetVisible(true);

                // Wait
                yield return new WaitForSeconds(duration);

                // Fade
                yield return StartCoroutine(FadeOut());

                SetVisible(false);
            }

            _isShowing = false;
        }

        private IEnumerator FadeOut()
        {
            if (subtitleCanvas == null) yield break;

            var group = subtitleCanvas.GetComponent<CanvasGroup>();
            if (group == null) { yield break; }

            float t = 0f;
            while (t < fadeOutDuration)
            {
                group.alpha = 1f - t / fadeOutDuration;
                t += Time.deltaTime;
                yield return null;
            }
            group.alpha = 1f; // reset for next entry
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void SetContent(string speaker, string text, Color color)
        {
            if (speakerText != null)
            {
                speakerText.text    = string.IsNullOrEmpty(speaker) ? string.Empty : $"[{speaker}]";
                speakerText.color   = color;
                speakerText.enabled = !string.IsNullOrEmpty(speaker);
            }

            if (subtitleText != null)
                subtitleText.text = text;

            if (backgroundPanel != null)
            {
                var c = backgroundPanel.color;
                c.a = showBackground ? bgOpacity : 0f;
                backgroundPanel.color = c;
            }
        }

        private void SetVisible(bool visible)
        {
            if (subtitleRoot != null)
                subtitleRoot.gameObject.SetActive(visible);
        }

        private Color ResolveSpeakerColor(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return defaultSpeakerColor;
            string lower = speaker.ToLowerInvariant();
            if (lower.Contains("atc"))       return atcSpeakerColor;
            if (lower.Contains("assistant")) return assistantColor;
            return defaultSpeakerColor;
        }
    }
}
