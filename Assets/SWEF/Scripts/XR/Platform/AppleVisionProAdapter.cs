// AppleVisionProAdapter.cs — Phase 112: VR/XR Flight Experience
// Apple Vision Pro: spatial computing, eye tracking, spatial gestures.
// Namespace: SWEF.XR

using UnityEngine;

#if SWEF_APPLE_VISION
// PolySpatial / visionOS types would be referenced here in a real project.
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Platform adapter for Apple Vision Pro spatial computing.
    /// Implements eye tracking, spatial pinch gestures, and
    /// visionOS immersive space management.
    /// Compiled only when <c>SWEF_APPLE_VISION</c> is defined.
    /// </summary>
#if SWEF_APPLE_VISION
    public class AppleVisionProAdapter : IXRPlatformAdapter
    {
        /// <inheritdoc/>
        public XRPlatform Platform => XRPlatform.AppleVisionPro;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>Last known eye-gaze direction (world space).</summary>
        public UnityEngine.Vector3 EyeGazeDirection { get; private set; }

        private readonly XRHandState _leftHand  = new XRHandState { Hand = XRHandedness.Left };
        private readonly XRHandState _rightHand = new XRHandState { Hand = XRHandedness.Right };

        /// <inheritdoc/>
        public void Initialise(XRFlightConfig config)
        {
            IsActive = true;
            Debug.Log("[SWEF] AppleVisionProAdapter: Initialised.");
        }

        /// <inheritdoc/>
        public void RecenterView()
        {
            // TODO: visionOS immersive space recenter when PolySpatial SDK is present.
            Debug.Log("[SWEF] AppleVisionProAdapter: RecenterView.");
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // Update eye gaze and hand states from ARKit / visionOS (stub).
            EyeGazeDirection = UnityEngine.Vector3.forward;
            _leftHand.IsTracked  = false;
            _rightHand.IsTracked = false;
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            IsActive = false;
            Debug.Log("[SWEF] AppleVisionProAdapter: Shutdown.");
        }

        /// <inheritdoc/>
        public XRHandState GetLeftHandState()  => _leftHand;

        /// <inheritdoc/>
        public XRHandState GetRightHandState() => _rightHand;
    }
#endif
}
