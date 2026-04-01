using System;
using System.Collections.Generic;

namespace SWEF.ReplayTheater
{
    // ─── Enums ───────────────────────────────────────────────────────────────────

    /// <summary>Editing operations available in the clip editor.</summary>
    public enum EditMode
    {
        /// <summary>Cut a segment out of a clip.</summary>
        Cut,
        /// <summary>Trim the start or end of a clip.</summary>
        Trim,
        /// <summary>Merge two adjacent clips into one.</summary>
        Merge,
        /// <summary>Split a clip at a given timestamp.</summary>
        Split,
        /// <summary>Apply a transition between clips.</summary>
        Transition,
        /// <summary>Apply a visual effect to a clip.</summary>
        Effect
    }

    /// <summary>Visual transition styles between clips.</summary>
    public enum TransitionType
    {
        /// <summary>Fade to black and back.</summary>
        Fade,
        /// <summary>Blend both clips simultaneously.</summary>
        CrossDissolve,
        /// <summary>Wipe from one side to the other.</summary>
        Wipe,
        /// <summary>Zoom into the cut point.</summary>
        Zoom,
        /// <summary>Slide the next clip in from an edge.</summary>
        Slide,
        /// <summary>Hard cut with no transition.</summary>
        None
    }

    /// <summary>Output container formats for replay export.</summary>
    public enum ExportFormat
    {
        /// <summary>H.264/MP4 video file.</summary>
        MP4,
        /// <summary>Animated GIF.</summary>
        GIF,
        /// <summary>VP8/VP9 WebM video file.</summary>
        WebM,
        /// <summary>Native SWEF replay file (.swefr).</summary>
        ReplayFile
    }

    /// <summary>Target resolution / quality tier for exported video.</summary>
    public enum ExportQuality
    {
        /// <summary>480 p (SD).</summary>
        Low_480p,
        /// <summary>720 p (HD).</summary>
        Medium_720p,
        /// <summary>1080 p (Full HD).</summary>
        High_1080p,
        /// <summary>2160 p (4 K UHD).</summary>
        Ultra_4K
    }

    /// <summary>Destination platforms for sharing a replay.</summary>
    public enum SharingPlatform
    {
        /// <summary>Generate a direct shareable link.</summary>
        DirectLink,
        /// <summary>Post to a social-media integration.</summary>
        SocialMedia,
        /// <summary>Share within the game's community hub.</summary>
        InGame,
        /// <summary>Upload to cloud save storage.</summary>
        CloudSave
    }

    /// <summary>Built-in colour-grading look presets.</summary>
    public enum ColorGradingPreset
    {
        /// <summary>No grading applied.</summary>
        None,
        /// <summary>Filmic, slightly desaturated look.</summary>
        Cinematic,
        /// <summary>Warm, faded retro style.</summary>
        Vintage,
        /// <summary>High-contrast, punchy look.</summary>
        Dramatic,
        /// <summary>Boosted saturation and vibrance.</summary>
        Vivid,
        /// <summary>Black-and-white conversion.</summary>
        Monochrome
    }

    /// <summary>Audience visibility settings for shared replays.</summary>
    public enum PrivacySetting
    {
        /// <summary>Visible to everyone.</summary>
        Public,
        /// <summary>Visible to friends only.</summary>
        FriendsOnly,
        /// <summary>Visible to the owner only.</summary>
        Private
    }

    // ─── Data classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single, editable segment of replay footage.
    /// </summary>
    [System.Serializable]
    public class ReplayClip
    {
        /// <summary>Unique identifier for this clip.</summary>
        public string clipId;

        /// <summary>Start time within the source replay (seconds).</summary>
        public float startTime;

        /// <summary>End time within the source replay (seconds).</summary>
        public float endTime;

        /// <summary>Named camera angle used for this clip.</summary>
        public string cameraAngle;

        /// <summary>Transition type applied at the beginning of this clip.</summary>
        public TransitionType transitionIn;

        /// <summary>Transition type applied at the end of this clip.</summary>
        public TransitionType transitionOut;

        /// <summary>Ordered list of effect identifiers applied to this clip.</summary>
        public List<string> effects = new List<string>();

        /// <summary>Playback speed multiplier for this clip (default: 1).</summary>
        public float playbackSpeed = 1f;

        /// <summary>Human-readable label for the clip.</summary>
        public string label;
    }

    /// <summary>
    /// A complete editing project comprising an ordered sequence of <see cref="ReplayClip"/>s,
    /// optional music, and metadata.
    /// </summary>
    [System.Serializable]
    public class ReplayProject
    {
        /// <summary>Unique identifier for this project.</summary>
        public string projectId;

        /// <summary>Human-readable title.</summary>
        public string title;

        /// <summary>Optional description shown in the gallery.</summary>
        public string description;

        /// <summary>Ordered list of clips in the project.</summary>
        public List<ReplayClip> clips = new List<ReplayClip>();

        /// <summary>Identifier or path of the background music track.</summary>
        public string musicTrack;

        /// <summary>Total rendered duration of the project in seconds.</summary>
        public float totalDuration;

        /// <summary>UTC timestamp when the project was first created.</summary>
        public DateTime createdAt;

        /// <summary>UTC timestamp of the most recent save.</summary>
        public DateTime lastModifiedAt;
    }

    /// <summary>
    /// Parameters that control how a <see cref="ReplayProject"/> is exported.
    /// </summary>
    [System.Serializable]
    public class ExportSettings
    {
        /// <summary>Target container format.</summary>
        public ExportFormat format;

        /// <summary>Output resolution / quality tier.</summary>
        public ExportQuality quality;

        /// <summary>Output frame rate (frames per second, default: 30).</summary>
        public int framerate = 30;

        /// <summary>Whether to overlay the SWEF watermark on exported video.</summary>
        public bool includeWatermark;

        /// <summary>Whether to bake the HUD into the exported video.</summary>
        public bool includeHUD;

        /// <summary>Filesystem path where the exported file will be written.</summary>
        public string outputPath;
    }

    /// <summary>
    /// Result record returned after a replay has been shared.
    /// </summary>
    [System.Serializable]
    public class ShareResult
    {
        /// <summary>Full URL to the shared replay.</summary>
        public string url;

        /// <summary>Platform the replay was shared to.</summary>
        public SharingPlatform platform;

        /// <summary>UTC timestamp when the replay was shared.</summary>
        public DateTime sharedAt;

        /// <summary>UTC timestamp when the share link expires (if applicable).</summary>
        public DateTime expiresAt;

        /// <summary>Short alphanumeric code that can be used to retrieve the replay.</summary>
        public string shareCode;

        /// <summary>Whether the replay is visible to the public.</summary>
        public bool isPublic;
    }

    /// <summary>
    /// Abstract base class for undoable editor commands (Command Pattern).
    /// </summary>
    [System.Serializable]
    public abstract class EditCommand
    {
        /// <summary>Executes the command, applying its change to the project.</summary>
        public abstract void Execute();

        /// <summary>Reverts the change made by <see cref="Execute"/>.</summary>
        public abstract void Undo();
    }
}
