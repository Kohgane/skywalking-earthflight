using UnityEngine;

namespace SWEF.ReplayTheater
{
    /// <summary>
    /// ScriptableObject that stores global defaults and quality presets for the
    /// Replay Theater mode.  Create one via
    /// <c>Assets → Create → SWEF → Replay Theater Settings</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "ReplayTheaterSettings", menuName = "SWEF/Replay Theater Settings")]
    public class ReplayTheaterSettings : ScriptableObject
    {
        #region Playback

        [Header("Playback")]
        /// <summary>Default playback speed when the theater is entered.</summary>
        [SerializeField] private float defaultPlaybackSpeed = 1f;

        /// <summary>Available playback speed steps.</summary>
        [SerializeField] private float[] playbackSpeedSteps = { 0.25f, 0.5f, 1f, 2f, 4f };

        /// <summary>Default camera mode on theater entry.</summary>
        [SerializeField] private CameraMode defaultCameraMode = CameraMode.Follow;

        #endregion

        #region Export

        [Header("Export")]
        /// <summary>Default export video width in pixels.</summary>
        [SerializeField] private int defaultExportWidth = 1920;

        /// <summary>Default export video height in pixels.</summary>
        [SerializeField] private int defaultExportHeight = 1080;

        /// <summary>Default export frame rate.</summary>
        [SerializeField] private int defaultExportFps = 30;

        /// <summary>Root folder for all theater exports (relative to <c>Application.persistentDataPath</c>).</summary>
        [SerializeField] private string exportOutputDirectory = "SWEF_Exports";

        #endregion

        #region Thumbnails

        [Header("Thumbnails")]
        /// <summary>Default thumbnail width in pixels.</summary>
        [SerializeField] private int thumbnailWidth = 512;

        /// <summary>Default thumbnail height in pixels.</summary>
        [SerializeField] private int thumbnailHeight = 288;

        /// <summary>Sub-folder inside <c>Application.persistentDataPath</c> where thumbnails are cached.</summary>
        [SerializeField] private string thumbnailCacheDirectory = "SWEF_ThumbnailCache";

        #endregion

        #region UI Theme

        [Header("UI Theme")]
        /// <summary>Primary colour used for the timeline scrub bar.</summary>
        [SerializeField] private Color timelineColor = new Color(0.2f, 0.6f, 1f, 1f);

        /// <summary>Colour used for camera keyframe markers on the timeline.</summary>
        [SerializeField] private Color keyframeMarkerColor = new Color(1f, 0.8f, 0f, 1f);

        /// <summary>Colour used for A/B loop region overlay.</summary>
        [SerializeField] private Color loopRegionColor = new Color(0.4f, 1f, 0.4f, 0.3f);

        /// <summary>Playhead indicator colour.</summary>
        [SerializeField] private Color playheadColor = Color.white;

        #endregion

        #region Public Properties

        /// <summary>Default playback speed when entering the theater.</summary>
        public float DefaultPlaybackSpeed => defaultPlaybackSpeed;

        /// <summary>Available speed steps for the speed selector.</summary>
        public float[] PlaybackSpeedSteps => playbackSpeedSteps;

        /// <summary>Default camera mode.</summary>
        public CameraMode DefaultCameraMode => defaultCameraMode;

        /// <summary>Default export width in pixels.</summary>
        public int DefaultExportWidth => defaultExportWidth;

        /// <summary>Default export height in pixels.</summary>
        public int DefaultExportHeight => defaultExportHeight;

        /// <summary>Default export FPS.</summary>
        public int DefaultExportFps => defaultExportFps;

        /// <summary>Full path to the export output directory.</summary>
        public string ExportOutputPath =>
            System.IO.Path.Combine(Application.persistentDataPath, exportOutputDirectory);

        /// <summary>Default thumbnail width.</summary>
        public int ThumbnailWidth => thumbnailWidth;

        /// <summary>Default thumbnail height.</summary>
        public int ThumbnailHeight => thumbnailHeight;

        /// <summary>Full path to the thumbnail cache directory.</summary>
        public string ThumbnailCachePath =>
            System.IO.Path.Combine(Application.persistentDataPath, thumbnailCacheDirectory);

        /// <summary>Primary timeline colour.</summary>
        public Color TimelineColor => timelineColor;

        /// <summary>Keyframe marker colour.</summary>
        public Color KeyframeMarkerColor => keyframeMarkerColor;

        /// <summary>A/B loop region overlay colour.</summary>
        public Color LoopRegionColor => loopRegionColor;

        /// <summary>Playhead colour.</summary>
        public Color PlayheadColor => playheadColor;

        #endregion
    }
}
