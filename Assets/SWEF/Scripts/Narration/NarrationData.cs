using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Narration
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Broad category of a landmark.</summary>
    public enum LandmarkCategory
    {
        Natural,
        Historical,
        Cultural,
        Architectural,
        Religious,
        Modern,
        Geological,
        Archaeological,
        Industrial,
        Artistic
    }

    /// <summary>How a narration is triggered for a landmark.</summary>
    public enum NarrationTriggerType
    {
        Proximity,
        LookAt,
        FlyOver,
        FlyThrough,
        Manual,
        TimeOfDay
    }

    /// <summary>Priority used when multiple landmarks are nearby and the queue must be ordered.</summary>
    public enum NarrationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>Lifecycle state of a single narration instance.</summary>
    public enum NarrationState
    {
        Idle,
        Queued,
        Playing,
        Paused,
        Completed,
        Skipped
    }

    // ── LandmarkData ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Design-time definition for a single real-world landmark that can trigger narration.
    /// </summary>
    [Serializable]
    public class LandmarkData
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        /// <summary>Unique identifier, e.g. "lm_eiffel_tower".</summary>
        public string landmarkId;

        /// <summary>Display name shown in the HUD (English fallback).</summary>
        public string name;

        /// <summary>Localization key used to look up the translated name.</summary>
        public string localizedNameKey;

        // ── Location ─────────────────────────────────────────────────────────────
        /// <summary>WGS-84 latitude in decimal degrees.</summary>
        public double latitude;

        /// <summary>WGS-84 longitude in decimal degrees.</summary>
        public double longitude;

        /// <summary>Ground-level altitude of the landmark in metres above sea level.</summary>
        public float altitude;

        // ── Classification ────────────────────────────────────────────────────────
        /// <summary>Broad category of the landmark.</summary>
        public LandmarkCategory category;

        /// <summary>Free-form sub-category string, e.g. "Iron lattice tower".</summary>
        public string subcategory;

        // ── Trigger ───────────────────────────────────────────────────────────────
        /// <summary>Distance in metres at which the narration auto-triggers.</summary>
        public float triggerRadius;

        /// <summary>How the narration is activated.</summary>
        public NarrationTriggerType triggerType;

        /// <summary>Priority when multiple landmarks compete for playback.</summary>
        public NarrationPriority priority;

        // ── Geography ─────────────────────────────────────────────────────────────
        public string country;
        public string region;
        public string city;

        // ── Meta ──────────────────────────────────────────────────────────────────
        /// <summary>Year of construction/formation. Use negative values for BCE (e.g. -3000).</summary>
        public int yearBuilt;

        /// <summary>Name of the architect or builder, if known.</summary>
        public string architect;

        /// <summary>Whether this site is a UNESCO World Heritage Site.</summary>
        public bool unescoWorldHeritage;

        /// <summary>Searchable keyword tags.</summary>
        public List<string> tags = new List<string>();

        /// <summary>Minimap icon identifier, e.g. "landmark_cultural".</summary>
        public string iconType;

        /// <summary>IDs of related landmarks in the same complex or region.</summary>
        public List<string> relatedLandmarkIds = new List<string>();
    }

    // ── NarrationScript ───────────────────────────────────────────────────────────

    /// <summary>
    /// A narration script for one landmark in one language, broken into timed segments.
    /// </summary>
    [Serializable]
    public class NarrationScript
    {
        /// <summary>Unique script identifier.</summary>
        public string scriptId;

        /// <summary>Landmark this script describes.</summary>
        public string landmarkId;

        /// <summary>BCP-47 language code, e.g. "en", "ko", "ja".</summary>
        public string languageCode;

        /// <summary>Short title shown in the HUD panel header.</summary>
        public string title;

        /// <summary>One-line subtitle / teaser.</summary>
        public string subtitle;

        /// <summary>Ordered list of timed narration segments.</summary>
        public List<NarrationSegment> segments = new List<NarrationSegment>();

        /// <summary>Total duration in seconds (sum of all segment end-times or audio clip length).</summary>
        public float totalDuration;

        /// <summary>
        /// Resource path to the pre-recorded AudioClip, relative to a Resources folder.
        /// Empty if no audio is available.
        /// </summary>
        public string audioClipPath;

        /// <summary>False = text-only mode; TTS or subtitle-only fallback is used instead.</summary>
        public bool hasAudio;

        /// <summary>Short fun facts shown as toast notifications during playback.</summary>
        public List<string> funFacts = new List<string>();

        /// <summary>Attribution or citation URLs for the content.</summary>
        public List<string> sources = new List<string>();
    }

    // ── NarrationSegment ──────────────────────────────────────────────────────────

    /// <summary>
    /// A single timed segment within a <see cref="NarrationScript"/>.
    /// Used to sync subtitle text to the audio track.
    /// </summary>
    [Serializable]
    public class NarrationSegment
    {
        /// <summary>Zero-based index of this segment within its script.</summary>
        public int segmentIndex;

        /// <summary>Full text of the narration for this segment.</summary>
        public string text;

        /// <summary>Audio start time in seconds.</summary>
        public float startTime;

        /// <summary>Audio end time in seconds.</summary>
        public float endTime;

        /// <summary>Words/phrases to emphasise (bold/highlight) in the subtitle UI.</summary>
        public List<string> highlightKeywords = new List<string>();

        /// <summary>Suggested camera look direction hint (world-space Euler angles).</summary>
        public Vector3 suggestedCameraAngle;

        /// <summary>Optional resource path to an illustration image for this segment.</summary>
        public string relatedImagePath;
    }

    // ── NarrationConfig ───────────────────────────────────────────────────────────

    /// <summary>
    /// Player-adjustable configuration for the narration system.
    /// Serialized to/from JSON in <see cref="NarrationManager"/>.
    /// </summary>
    [Serializable]
    public class NarrationConfig
    {
        public bool   enabled                    = true;
        public bool   autoPlayOnProximity        = true;
        public float  narrationVolume            = 0.8f;
        public bool   duckMusicDuringNarration   = true;
        public float  duckAmount                 = 0.5f;
        public bool   preferAudioNarration       = true;
        public bool   showSubtitles              = true;
        public float  subtitleFontSize           = 18f;
        public bool   autoAdvanceSegments        = true;
        public float  segmentPauseDuration       = 1.5f;
        public int    maxSimultaneousNarrations  = 1;
        public float  cooldownBetweenNarrations  = 30f;
        public bool   showMinimapIcons           = true;
        public bool   showProximityIndicator     = true;
        public bool   enableFunFacts             = true;
        public float  narrationSpeed             = 1.0f;
        public List<LandmarkCategory> preferredCategories = new List<LandmarkCategory>();
        public bool   discoveryMode              = false;
    }

    // ── NarrationQueueEntry ───────────────────────────────────────────────────────

    /// <summary>Internal queue entry used by <see cref="NarrationManager"/>.</summary>
    [Serializable]
    public class NarrationQueueEntry
    {
        public LandmarkData    landmark;
        public NarrationScript script;
        public NarrationState  state = NarrationState.Queued;
        public float           queuedAt;
    }
}
