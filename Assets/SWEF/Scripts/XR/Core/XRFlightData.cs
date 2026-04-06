// XRFlightData.cs — Phase 112: VR/XR Flight Experience
// Enums and data models for the XR flight system.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    // ── Platform ─────────────────────────────────────────────────────────────────

    /// <summary>Supported XR hardware platforms.</summary>
    public enum XRPlatform
    {
        /// <summary>Generic/unknown XR device.</summary>
        Generic,
        /// <summary>Meta Quest 2/3/Pro headsets.</summary>
        MetaQuest,
        /// <summary>Apple Vision Pro spatial computing device.</summary>
        AppleVisionPro,
        /// <summary>SteamVR-compatible PC VR headsets.</summary>
        SteamVR
    }

    // ── Comfort ──────────────────────────────────────────────────────────────────

    /// <summary>Motion comfort presets for VR.</summary>
    public enum XRComfortLevel
    {
        /// <summary>Minimal comfort aids — for experienced VR users.</summary>
        Off,
        /// <summary>Mild vignette during rotation only.</summary>
        Low,
        /// <summary>Moderate vignette and gentle snap-turning.</summary>
        Medium,
        /// <summary>Full comfort aids: snap-turning, vignette, rest frame overlay.</summary>
        High,
        /// <summary>Custom user-configured comfort settings.</summary>
        Custom
    }

    // ── Handedness ───────────────────────────────────────────────────────────────

    /// <summary>Dominant-hand preference for controller/gesture mapping.</summary>
    public enum XRHandedness
    {
        /// <summary>Right hand is primary.</summary>
        Right,
        /// <summary>Left hand is primary.</summary>
        Left
    }

    // ── Locomotion ───────────────────────────────────────────────────────────────

    /// <summary>VR locomotion / movement scheme.</summary>
    public enum XRLocomotionType
    {
        /// <summary>Continuous thumbstick-driven movement.</summary>
        Continuous,
        /// <summary>Blink/instant teleport to aimed position.</summary>
        Teleport,
        /// <summary>Snap teleport with arc visualiser.</summary>
        SnapTeleport,
        /// <summary>Seated mode — no body locomotion.</summary>
        Seated
    }

    // ── Session state ─────────────────────────────────────────────────────────────

    /// <summary>Lifecycle states of an XR flight session.</summary>
    public enum XRSessionState
    {
        /// <summary>XR session has not been initialised.</summary>
        Uninitialized,
        /// <summary>XR subsystems are initialising.</summary>
        Initializing,
        /// <summary>Session is active and tracking.</summary>
        Running,
        /// <summary>Session is suspended (app backgrounded).</summary>
        Suspended,
        /// <summary>Session has ended cleanly.</summary>
        Stopped,
        /// <summary>A fatal XR error has occurred.</summary>
        Error
    }

    // ── Flight phase ──────────────────────────────────────────────────────────────

    /// <summary>Current VR flight experience phase.</summary>
    public enum VRFlightPhase
    {
        /// <summary>Pre-flight cockpit setup and briefing.</summary>
        Preflight,
        /// <summary>Takeoff roll and rotation.</summary>
        Takeoff,
        /// <summary>En-route cruise phase.</summary>
        Cruise,
        /// <summary>Approach and landing.</summary>
        Landing,
        /// <summary>Post-flight debrief.</summary>
        Debrief
    }

    // ── Gesture ───────────────────────────────────────────────────────────────────

    /// <summary>Recognised hand gestures for interaction.</summary>
    public enum XRGestureType
    {
        /// <summary>No gesture detected.</summary>
        None,
        /// <summary>Thumb and index touching — primary select / UI confirm.</summary>
        Pinch,
        /// <summary>All fingers closed — grip cockpit controls.</summary>
        Grab,
        /// <summary>Index finger extended — UI pointer / direction.</summary>
        Point,
        /// <summary>All fingers open — open palm gesture (pause menu).</summary>
        OpenPalm,
        /// <summary>Thumb extended up — confirm / approve.</summary>
        ThumbsUp
    }

    // ── Cockpit interaction type ──────────────────────────────────────────────────

    /// <summary>Physical interaction type for cockpit controls.</summary>
    public enum CockpitInteractionType
    {
        /// <summary>Grab and hold (throttle, yoke).</summary>
        Grab,
        /// <summary>Push button or toggle switch.</summary>
        Push,
        /// <summary>Pull lever or handle.</summary>
        Pull,
        /// <summary>Rotary knob twist.</summary>
        Twist
    }

    // ── Data classes ──────────────────────────────────────────────────────────────

    /// <summary>Snapshot of hand tracking input for one frame.</summary>
    [Serializable]
    public class XRHandState
    {
        /// <summary>Which hand this state represents.</summary>
        public XRHandedness Hand;
        /// <summary>World-space palm centre position.</summary>
        public Vector3 PalmPosition;
        /// <summary>Palm-facing direction.</summary>
        public Vector3 PalmNormal;
        /// <summary>Tip position of each finger (thumb→pinky).</summary>
        public Vector3[] FingerTips = new Vector3[5];
        /// <summary>Currently recognised gesture on this hand.</summary>
        public XRGestureType ActiveGesture;
        /// <summary>Pinch strength [0..1].</summary>
        public float PinchStrength;
        /// <summary>Grab/grip strength [0..1].</summary>
        public float GrabStrength;
        /// <summary>True when this hand is being tracked.</summary>
        public bool IsTracked;
    }

    /// <summary>Per-user hand calibration data.</summary>
    [Serializable]
    public class XRHandCalibrationData
    {
        /// <summary>Dominant hand preference.</summary>
        public XRHandedness DominantHand = XRHandedness.Right;
        /// <summary>Measured palm width in metres.</summary>
        public float PalmWidthMetres = 0.085f;
        /// <summary>Measured finger length (index) in metres.</summary>
        public float FingerLengthMetres = 0.075f;
        /// <summary>Gesture recognition sensitivity multiplier [0.5..2].</summary>
        public float GestureSensitivity = 1f;
        /// <summary>Whether calibration has been performed.</summary>
        public bool IsCalibrated;
    }

    /// <summary>Summarised XR analytics event.</summary>
    [Serializable]
    public class XRAnalyticsEvent
    {
        /// <summary>Session identifier (GUID).</summary>
        public string SessionId;
        /// <summary>Active platform at the time of the event.</summary>
        public XRPlatform Platform;
        /// <summary>Comfort level in use.</summary>
        public XRComfortLevel ComfortLevel;
        /// <summary>Session duration in seconds.</summary>
        public float SessionDurationSeconds;
        /// <summary>Number of gestures recognised during the session.</summary>
        public int GesturesRecognised;
        /// <summary>UTC timestamp of the event.</summary>
        public string Timestamp;
    }
}
