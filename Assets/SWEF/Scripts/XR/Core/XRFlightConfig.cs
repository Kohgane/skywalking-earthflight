// XRFlightConfig.cs — Phase 112: VR/XR Flight Experience
// ScriptableObject holding all runtime-configurable XR parameters.
// Namespace: SWEF.XR

using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// ScriptableObject configuration asset for the VR/XR flight system.
    /// Create via <em>Assets → Create → SWEF → XR → XR Flight Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/XR/XR Flight Config", fileName = "XRFlightConfig")]
    public class XRFlightConfig : ScriptableObject
    {
        // ── Comfort ───────────────────────────────────────────────────────────────

        [Header("Comfort Settings")]
        /// <summary>Default comfort level applied on first launch.</summary>
        [Tooltip("Default comfort level applied on first launch.")]
        public XRComfortLevel defaultComfortLevel = XRComfortLevel.Medium;

        /// <summary>Vignette intensity at maximum rotation speed (0..1).</summary>
        [Range(0f, 1f)]
        public float maxVignetteIntensity = 0.7f;

        /// <summary>Angle per snap-turn step in degrees.</summary>
        [Range(15f, 90f)]
        public float snapTurnAngle = 30f;

        /// <summary>Duration of snap-turn animation in seconds.</summary>
        [Range(0f, 0.5f)]
        public float snapTurnDuration = 0.1f;

        /// <summary>Whether the ground reference frame overlay is enabled by default.</summary>
        public bool groundReferenceDefault = true;

        // ── Rendering Quality ─────────────────────────────────────────────────────

        [Header("Rendering Quality")]
        /// <summary>Target eye render scale for VR (lower = better performance).</summary>
        [Range(0.5f, 2f)]
        public float renderScale = 1f;

        /// <summary>Fixed foveated rendering level (0 = disabled, higher = stronger).</summary>
        [Range(0, 4)]
        public int fixedFoveatedRenderingLevel = 2;

        /// <summary>Enable MSAA for VR rendering.</summary>
        public bool enableMsaa = true;

        // ── Hand Tracking ─────────────────────────────────────────────────────────

        [Header("Hand Tracking")]
        /// <summary>Default dominant hand.</summary>
        public XRHandedness defaultDominantHand = XRHandedness.Right;

        /// <summary>Pinch gesture recognition threshold [0..1].</summary>
        [Range(0f, 1f)]
        public float pinchThreshold = 0.7f;

        /// <summary>Grab gesture recognition threshold [0..1].</summary>
        [Range(0f, 1f)]
        public float grabThreshold = 0.8f;

        /// <summary>Minimum hold duration (seconds) before a gesture is confirmed.</summary>
        [Range(0f, 0.5f)]
        public float gestureConfirmDuration = 0.1f;

        // ── Locomotion ────────────────────────────────────────────────────────────

        [Header("Locomotion")]
        /// <summary>Default locomotion type.</summary>
        public XRLocomotionType defaultLocomotionType = XRLocomotionType.Seated;

        /// <summary>Teleport arc gravity (controls arc curve).</summary>
        [Range(1f, 20f)]
        public float teleportArcGravity = 9.81f;

        /// <summary>Maximum teleport distance in metres.</summary>
        [Range(1f, 20f)]
        public float maxTeleportDistance = 5f;

        // ── Camera / IPD ──────────────────────────────────────────────────────────

        [Header("Camera & IPD")]
        /// <summary>Default interpupillary distance in metres.</summary>
        [Range(0.05f, 0.08f)]
        public float defaultIpd = 0.064f;

        /// <summary>Near clip plane for VR camera.</summary>
        [Range(0.01f, 0.1f)]
        public float nearClipPlane = 0.05f;

        /// <summary>HUD projection distance in metres.</summary>
        [Range(1f, 10f)]
        public float hudProjectionDistance = 3f;

        // ── Platform ──────────────────────────────────────────────────────────────

        [Header("Platform")]
        /// <summary>Preferred platform; overrides auto-detection when set.</summary>
        public XRPlatform preferredPlatform = XRPlatform.Generic;

        /// <summary>Enable passthrough (mixed-reality) on Meta Quest when available.</summary>
        public bool enablePassthroughOnQuest = false;
    }
}
