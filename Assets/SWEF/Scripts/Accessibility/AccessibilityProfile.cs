// AccessibilityProfile.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>Color-vision deficiency type for daltonization correction.</summary>
    public enum ColorBlindMode
    {
        /// <summary>No colorblind processing.</summary>
        None,
        /// <summary>Red-weak / red-blind.</summary>
        Protanopia,
        /// <summary>Green-weak / green-blind (most common red-green colorblindness).</summary>
        Deuteranopia,
        /// <summary>Blue-yellow colorblindness.</summary>
        Tritanopia,
        /// <summary>Complete absence of colour (monochromacy).</summary>
        Achromatopsia
    }

    /// <summary>Pre-set subtitle text-size categories.</summary>
    public enum SubtitleSize
    {
        Small      = 18,
        Medium     = 24,
        Large      = 32,
        ExtraLarge = 42
    }

    /// <summary>
    /// Serializable container for all user accessibility preferences.
    /// Persisted to <c>accessibility_settings.json</c> by <see cref="AccessibilityManager"/>.
    /// </summary>
    [Serializable]
    public class AccessibilityProfile
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        [Tooltip("Human-readable profile name shown in settings UI.")]
        public string profileName = "Default";

        /// <summary>Currently active preset (used for display and serialisation).</summary>
        public AccessibilityPreset activePreset = AccessibilityPreset.Default;

        // ── Vision / Color-blind ──────────────────────────────────────────────
        [Tooltip("Active color-blind correction mode.")]
        public ColorBlindMode colorBlindMode = ColorBlindMode.None;

        [Tooltip("Daltonization correction strength (0 = off, 1 = full).")]
        [Range(0f, 1f)]
        public float colorBlindIntensity = 1f;

        // ── Subtitles ─────────────────────────────────────────────────────────
        [Tooltip("Show subtitles for voice commands, ATC, assistant, and multiplayer chat.")]
        public bool subtitleEnabled;

        [Tooltip("Subtitle font size tier.")]
        public SubtitleSize subtitleSize = SubtitleSize.Medium;

        [Tooltip("Draw a semi-transparent background panel behind subtitles.")]
        public bool subtitleBackground = true;

        [Tooltip("Background panel opacity (0 = transparent, 1 = opaque).")]
        [Range(0f, 1f)]
        public float subtitleBackgroundOpacity = 0.65f;

        // ── Screen reader ─────────────────────────────────────────────────────
        [Tooltip("Enable TTS screen-reader announcements.")]
        public bool screenReaderEnabled;

        // ── Motion ────────────────────────────────────────────────────────────
        [Tooltip("Reduce or eliminate non-essential UI animations.")]
        public bool reducedMotion;

        // ── Visual UI ─────────────────────────────────────────────────────────
        [Tooltip("Enable high-contrast UI skin for low-vision players.")]
        public bool highContrastUI;

        [Tooltip("Global HUD scale multiplier (0.5 = half, 2.0 = double).")]
        [Range(0.5f, 2f)]
        public float hudScale = 1f;

        [Tooltip("Global text scale multiplier (0.75 – 2.0).")]
        [Range(0.75f, 2f)]
        public float textScale = 1f;

        // ── Motor / Input ─────────────────────────────────────────────────────
        [Tooltip("Enable auto-hover so the aircraft holds altitude without constant input.")]
        public bool autoHoverAssist;

        [Tooltip("Restrict all controls to a one-handed input scheme.")]
        public bool oneHandedMode;

        [Tooltip("Enable haptic / rumble feedback.")]
        public bool hapticFeedback = true;

        // ── Safety ────────────────────────────────────────────────────────────
        [Tooltip("Show a warning icon instead of playing strobing visual effects.")]
        public bool flashWarning;

        // ── Audio ─────────────────────────────────────────────────────────────
        [Tooltip("Enable audio description narration for important visual events.")]
        public bool audioDescriptions;

        // ── Feature flags (backward-compat with older AccessibilityManager) ───────
        /// <summary>Feature-flag keys currently enabled (serialised as a list).</summary>
        public System.Collections.Generic.List<string> enabledFeatureKeys
            = new System.Collections.Generic.List<string>();
    }
}
