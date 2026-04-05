// SpectatorConfig.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using UnityEngine;

namespace SWEF.Spectator
{
    /// <summary>
    /// ScriptableObject that centralises all tunable parameters for the
    /// Phase 107 Spectator / Streaming system.
    ///
    /// <para>Create via <b>Assets → Create → SWEF → Spectator → Config</b>.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Spectator/Config", fileName = "SpectatorConfig")]
    public sealed class SpectatorConfig : ScriptableObject
    {
        // ── Camera — FreeCam ──────────────────────────────────────────────────
        [Header("FreeCam")]
        [Tooltip("Base movement speed in metres/second.")]
        [SerializeField] public float freeCamSpeed = 50f;

        [Tooltip("Multiplier applied while the boost key is held.")]
        [SerializeField] public float freeCamBoostMultiplier = 5f;

        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] public float freeCamMouseSensitivity = 2f;

        // ── Camera — FollowCam ────────────────────────────────────────────────
        [Header("FollowCam")]
        [Tooltip("Default offset from the target aircraft (local space).")]
        [SerializeField] public Vector3 followOffset = new Vector3(0f, 10f, -30f);

        [Tooltip("Position smoothing factor (0 = snap, 1 = never catch up).")]
        [Range(0f, 1f)]
        [SerializeField] public float followPositionSmoothing = 0.12f;

        [Tooltip("Rotation smoothing factor.")]
        [Range(0f, 1f)]
        [SerializeField] public float followRotationSmoothing = 0.10f;

        // ── Camera — OrbitCam ─────────────────────────────────────────────────
        [Header("OrbitCam")]
        [Tooltip("Default orbit radius in metres.")]
        [SerializeField] public float orbitRadius = 60f;

        [Tooltip("Orbit angular speed in degrees/second.")]
        [SerializeField] public float orbitSpeed = 20f;

        [Tooltip("Vertical angle of the orbit in degrees (0 = horizontal, 90 = directly above).")]
        [Range(0f, 89f)]
        [SerializeField] public float orbitElevation = 20f;

        // ── Camera — CinematicCam ─────────────────────────────────────────────
        [Header("CinematicCam")]
        [Tooltip("Seconds between automated cinematic angle changes.")]
        [SerializeField] public float cinematicShotDuration = 8f;

        [Tooltip("Chase offset for cinematic chase shots.")]
        [SerializeField] public Vector3 cinematicChaseOffset = new Vector3(0f, 5f, -20f);

        // ── Camera — FOV ──────────────────────────────────────────────────────
        [Header("Field of View")]
        [Tooltip("Default vertical FOV in degrees.")]
        [SerializeField] public float defaultFov = 60f;

        [Tooltip("FOV added at maximum aircraft speed.")]
        [SerializeField] public float maxFovBoost = 20f;

        [Tooltip("Reference maximum speed (kph) at which maxFovBoost is fully applied.")]
        [SerializeField] public float fovMaxSpeedKph = 1000f;

        // ── Camera Shake ──────────────────────────────────────────────────────
        [Header("Camera Shake")]
        [Tooltip("Amplitude of camera shake applied on exciting events.")]
        [SerializeField] public float shakeAmplitude = 0.15f;

        [Tooltip("Duration of a single camera shake burst in seconds.")]
        [SerializeField] public float shakeDuration = 0.4f;

        // ── Auto-Director ─────────────────────────────────────────────────────
        [Header("Auto-Director")]
        [Tooltip("Minimum time between automated camera cuts in seconds.")]
        [SerializeField] public float directorMinCutInterval = 5f;

        [Tooltip("Maximum time between automated camera cuts in seconds.")]
        [SerializeField] public float directorMaxCutInterval = 20f;

        // ── Chat ──────────────────────────────────────────────────────────────
        [Header("Chat")]
        [Tooltip("Maximum number of visible chat messages in the overlay.")]
        [SerializeField] public int chatMaxMessages = 40;

        [Tooltip("Minimum seconds between messages from the same viewer (rate limiting).")]
        [SerializeField] public float chatRateLimitSeconds = 2f;

        [Tooltip("Seconds a chat message remains visible before fading.")]
        [SerializeField] public float chatMessageLifetime = 12f;

        // ── Streaming ─────────────────────────────────────────────────────────
        [Header("Streaming")]
        [Tooltip("Viewer count milestones that trigger the OnViewerMilestone event.")]
        [SerializeField] public int[] viewerMilestones = { 100, 500, 1000, 5000, 10000 };
    }
}
