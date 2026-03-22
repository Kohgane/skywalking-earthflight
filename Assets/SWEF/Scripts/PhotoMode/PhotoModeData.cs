using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PhotoMode
{
    // ── DroneMode ─────────────────────────────────────────────────────────────────

    /// <summary>Operating mode of the free-flying drone camera.</summary>
    public enum DroneMode
    {
        /// <summary>Full manual 6-DoF control by the player.</summary>
        Free,
        /// <summary>Auto-circles a target transform at a configurable radius and speed.</summary>
        Orbit,
        /// <summary>Follows the player aircraft from behind at a configurable offset.</summary>
        FollowTarget,
        /// <summary>Follows a spline path (CinematicCameraPath) at a set speed.</summary>
        Dolly,
        /// <summary>Position is locked; only rotation is controllable.</summary>
        Static,
        /// <summary>Front-facing selfie mode with subject-blur background.</summary>
        Selfie
    }

    // ── PhotoFilter ───────────────────────────────────────────────────────────────

    /// <summary>Built-in photo filter presets.</summary>
    public enum PhotoFilter
    {
        None,
        Vintage,
        Noir,
        Warm,
        Cool,
        HDR,
        Cinematic,
        Sunset,
        NightVision,
        Sketch,
        Tiltshift,
        Bokeh
    }

    // ── FrameStyle ────────────────────────────────────────────────────────────────

    /// <summary>Decorative frame styles applied around a captured photo.</summary>
    public enum FrameStyle
    {
        None,
        Polaroid,
        Filmstrip,
        Panoramic,
        Square,
        Postcard,
        Passport,
        Widescreen
    }

    // ── FocusMode ─────────────────────────────────────────────────────────────────

    /// <summary>Focus mode for the virtual camera.</summary>
    public enum FocusMode
    {
        /// <summary>Continuous auto-focus on scene centre.</summary>
        Auto,
        /// <summary>Fixed focus at the manually set <see cref="CameraSettings.focusDistance"/>.</summary>
        Manual,
        /// <summary>Tap/click a screen point to focus on that world position.</summary>
        PointSelect,
        /// <summary>Detect and track the largest face in frame.</summary>
        FaceDetect,
        /// <summary>Focus at infinity — no depth-of-field blur.</summary>
        InfinityFocus
    }

    // ── PhotoResolution ───────────────────────────────────────────────────────────

    /// <summary>Output resolution for photo capture.</summary>
    public enum PhotoResolution
    {
        /// <summary>1280 × 720</summary>
        HD_720,
        /// <summary>1920 × 1080</summary>
        FHD_1080,
        /// <summary>2560 × 1440</summary>
        QHD_1440,
        /// <summary>3840 × 2160</summary>
        UHD_4K,
        /// <summary>7680 × 4320</summary>
        UHD_8K
    }

    // ── PhotoFormat ───────────────────────────────────────────────────────────────

    /// <summary>File format for exported photos.</summary>
    public enum PhotoFormat
    {
        /// <summary>JPEG with configurable quality (lossy).</summary>
        JPEG,
        /// <summary>PNG lossless with alpha.</summary>
        PNG,
        /// <summary>Uncompressed 32-bit raw data.</summary>
        RAW,
        /// <summary>TIFF lossless.</summary>
        TIFF
    }

    // ── DroneConfig ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Inspector-configurable settings for the drone camera.
    /// Serialized to PlayerPrefs as JSON under the <c>SWEF_PhotoMode_DroneConfig</c> key.
    /// </summary>
    [Serializable]
    public class DroneConfig
    {
        /// <summary>Maximum distance (metres) the drone may travel from the player aircraft.</summary>
        public float maxRange = 500f;

        /// <summary>Translational movement speed in m/s.</summary>
        public float moveSpeed = 15f;

        /// <summary>Rotation speed in degrees per second.</summary>
        public float rotateSpeed = 90f;

        /// <summary>
        /// Movement damping factor (0 = instant, 1 = never arrives).
        /// Clamped to [0, 0.99] internally.
        /// </summary>
        [Range(0f, 1f)] public float smoothing = 0.85f;

        /// <summary>When true, the drone raycasts ahead and slides around obstacles.</summary>
        public bool collisionAvoidance = true;

        /// <summary>When true, the drone automatically returns when battery reaches 10 %.</summary>
        public bool autoReturnOnLowBattery = true;

        /// <summary>Total battery life in seconds of drone flight time.</summary>
        public float batteryDuration = 300f;

        /// <summary>Radius in metres for <see cref="DroneMode.Orbit"/> mode.</summary>
        public float orbitRadius = 50f;

        /// <summary>Angular speed in degrees per second for <see cref="DroneMode.Orbit"/> mode.</summary>
        public float orbitSpeed = 15f;

        /// <summary>Distance behind target for <see cref="DroneMode.FollowTarget"/> mode.</summary>
        public float followDistance = 30f;

        /// <summary>Height above target for <see cref="DroneMode.FollowTarget"/> mode.</summary>
        public float followHeight = 10f;
    }

    // ── CameraSettings ────────────────────────────────────────────────────────────

    /// <summary>
    /// Virtual camera settings controlling exposure, depth-of-field, and filters.
    /// Snapshotted into <see cref="PhotoMetadata"/> at capture time.
    /// </summary>
    [Serializable]
    public class CameraSettings
    {
        // ── Optics ────────────────────────────────────────────────────────────────

        /// <summary>Field of view in degrees (10–120).</summary>
        [Range(10f, 120f)] public float fieldOfView = 60f;

        /// <summary>
        /// Aperture f-stop (1.4–22). Smaller values produce shallower depth of field.
        /// </summary>
        [Range(1.4f, 22f)] public float aperture = 5.6f;

        /// <summary>
        /// Shutter speed in seconds (1/8000 → ~0.000125 to 30).
        /// Drives motion-blur intensity.
        /// </summary>
        public float shutterSpeed = 0.004f; // 1/250

        /// <summary>ISO sensitivity (100–12800). Higher values add film grain.</summary>
        [Range(100, 12800)] public int iso = 400;

        /// <summary>Exposure compensation in EV stops (−3 to +3).</summary>
        [Range(-3f, 3f)] public float exposureCompensation = 0f;

        /// <summary>White balance colour temperature in Kelvin (2500–10000).</summary>
        [Range(2500, 10000)] public int whiteBalance = 5500;

        // ── Focus ─────────────────────────────────────────────────────────────────

        /// <summary>Manual focus distance in metres (0.5–1000).</summary>
        [Range(0.5f, 1000f)] public float focusDistance = 10f;

        /// <summary>Active focus mode.</summary>
        public FocusMode focusMode = FocusMode.Auto;

        // ── Artistic ──────────────────────────────────────────────────────────────

        /// <summary>Active photo filter.</summary>
        public PhotoFilter filter = PhotoFilter.None;

        /// <summary>Active frame style.</summary>
        public FrameStyle frame = FrameStyle.None;

        // ── Output ────────────────────────────────────────────────────────────────

        /// <summary>Output resolution.</summary>
        public PhotoResolution resolution = PhotoResolution.FHD_1080;

        /// <summary>Output file format.</summary>
        public PhotoFormat format = PhotoFormat.JPEG;

        /// <summary>JPEG quality (1–100). Only used when <see cref="format"/> is JPEG.</summary>
        [Range(1, 100)] public int jpegQuality = 95;

        // ── Overlay toggles ───────────────────────────────────────────────────────

        /// <summary>Show rule-of-thirds grid overlay in viewfinder.</summary>
        public bool enableGrid = false;

        /// <summary>Show horizon levelling guide in viewfinder.</summary>
        public bool enableLevel = false;

        /// <summary>Show live exposure histogram in viewfinder.</summary>
        public bool enableHistogram = false;

        /// <summary>Show overexposure zebra-stripe overlay in viewfinder.</summary>
        public bool enableZebra = false;
    }

    // ── PhotoMetadata ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Rich metadata record saved alongside each captured photo as a JSON sidecar file.
    /// </summary>
    [Serializable]
    public class PhotoMetadata
    {
        /// <summary>Unique identifier (GUID) for this photo.</summary>
        public string photoId;

        /// <summary>UTC timestamp of capture as ISO-8601 string.</summary>
        public string timestamp;

        // ── Location ──────────────────────────────────────────────────────────────

        /// <summary>Latitude at capture time (WGS-84 degrees).</summary>
        public double latitude;

        /// <summary>Longitude at capture time (WGS-84 degrees).</summary>
        public double longitude;

        /// <summary>Altitude at capture time in metres above sea level.</summary>
        public double altitude;

        // ── Context ───────────────────────────────────────────────────────────────

        /// <summary>Snapshot of camera settings at capture time.</summary>
        public CameraSettings cameraSettings;

        /// <summary>Drone mode active at capture time.</summary>
        public DroneMode droneMode;

        /// <summary>Type/name of the player aircraft at capture time.</summary>
        public string playerAircraftType;

        /// <summary>Current weather condition description at capture time.</summary>
        public string weatherCondition;

        // ── File info ─────────────────────────────────────────────────────────────

        /// <summary>Absolute or relative path to the photo file.</summary>
        public string filePath;

        /// <summary>Absolute or relative path to the generated thumbnail.</summary>
        public string thumbnailPath;

        /// <summary>File size in bytes.</summary>
        public long fileSize;

        /// <summary>Pixel width of the captured image.</summary>
        public int width;

        /// <summary>Pixel height of the captured image.</summary>
        public int height;

        // ── Social ────────────────────────────────────────────────────────────────

        /// <summary>User-defined tags for searching and filtering.</summary>
        public List<string> tags = new List<string>();

        /// <summary>Whether the photo has been starred as a favourite.</summary>
        public bool isFavorite;

        /// <summary>Number of times this photo has been shared.</summary>
        public int shareCount;
    }
}
