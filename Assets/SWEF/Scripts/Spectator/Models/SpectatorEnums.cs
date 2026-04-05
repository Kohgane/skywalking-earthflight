// SpectatorEnums.cs — SWEF Phase 107: Live Streaming & Spectator Mode
using System;

namespace SWEF.Spectator
{
    // ────────────────────────────────────────────────────────────────────────────
    // Camera modes available in Spectator Mode
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines the active camera mode used by <see cref="SpectatorCameraController"/>.
    /// </summary>
    public enum SpectatorCameraMode
    {
        /// <summary>Free-fly camera controlled by WASD + mouse input.</summary>
        FreeCam,
        /// <summary>Follows the target aircraft with configurable offset and smoothing.</summary>
        FollowCam,
        /// <summary>Orbits around the target aircraft at a configurable radius.</summary>
        OrbitCam,
        /// <summary>Auto-pilot cinematic angles — chase, flyby, and dramatic shots.</summary>
        CinematicCam,
        /// <summary>Renders the scene from the target aircraft's cockpit perspective.</summary>
        PilotView,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Cinematic sub-angles
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sub-modes used by <see cref="SpectatorCameraMode.CinematicCam"/> for
    /// automated shot selection.
    /// </summary>
    public enum CinematicShotType
    {
        /// <summary>Chase camera slightly above and behind the aircraft.</summary>
        Chase,
        /// <summary>Camera positioned ahead; aircraft flies towards and past it.</summary>
        Flyby,
        /// <summary>Low-angle dramatic shot emphasising sky and speed.</summary>
        Dramatic,
        /// <summary>Slow overhead orbit giving a strategic overview.</summary>
        TopDown,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Streaming platforms
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Live-streaming platforms supported by <see cref="StreamingIntegrationManager"/>.
    /// </summary>
    public enum StreamingPlatform
    {
        /// <summary>Twitch live streaming.</summary>
        Twitch,
        /// <summary>YouTube Live streaming.</summary>
        YouTube,
        /// <summary>Custom / user-defined RTMP endpoint.</summary>
        Custom,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Camera-cut transition effects
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transition effect applied when <see cref="CameraSwitchDirector"/> cuts between angles.
    /// </summary>
    public enum CameraTransitionEffect
    {
        /// <summary>Instantaneous cut with no interpolation.</summary>
        Cut,
        /// <summary>Smooth blend between outgoing and incoming camera positions.</summary>
        Crossfade,
        /// <summary>Fast swipe/pan across the screen before snapping to the new angle.</summary>
        WhipPan,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Notable in-flight events (triggers for auto-director and caster overlays)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Key in-flight moments that the <see cref="CameraSwitchDirector"/> and
    /// <see cref="CommentatorController"/> treat as highlight events.
    /// </summary>
    public enum FlightEventType
    {
        /// <summary>Aircraft set a new speed record.</summary>
        SpeedRecord,
        /// <summary>Aircraft reached a notable altitude milestone.</summary>
        AltitudeMilestone,
        /// <summary>Two or more aircraft passed very close to each other.</summary>
        NearMiss,
        /// <summary>One aircraft overtook another.</summary>
        Overtake,
        /// <summary>Aircraft are flying in close formation.</summary>
        FormationFlying,
        /// <summary>Aircraft performed a sharp, high-G manoeuvre.</summary>
        SharpManeuver,
        /// <summary>Aircraft crossed a significant geographic boundary.</summary>
        GeoBoundary,
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Chat command types
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Chat commands that viewers can send during a live stream to interact with
    /// the <see cref="LiveChatController"/>.
    /// </summary>
    public enum ChatCommandType
    {
        /// <summary>Switch to a specific camera mode: <c>!camera &lt;mode&gt;</c>.</summary>
        Camera,
        /// <summary>Ask the director to follow a specific pilot: <c>!follow &lt;name&gt;</c>.</summary>
        Follow,
        /// <summary>Request current flight statistics: <c>!stats</c>.</summary>
        Stats,
        /// <summary>Unknown or unrecognised command.</summary>
        Unknown,
    }
}
