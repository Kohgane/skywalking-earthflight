// GenericXRAdapter.cs — Phase 112: VR/XR Flight Experience
// Fallback OpenXR adapter for unknown/generic XR devices.
// Namespace: SWEF.XR

using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Fallback XR adapter used when no platform-specific adapter is available.
    /// Provides stub hand states and basic recenter via Unity's XR subsystem API.
    /// </summary>
    public class GenericXRAdapter : IXRPlatformAdapter
    {
        /// <inheritdoc/>
        public XRPlatform Platform => XRPlatform.Generic;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        private readonly XRHandState _leftHand  = new XRHandState { Hand = XRHandedness.Left };
        private readonly XRHandState _rightHand = new XRHandState { Hand = XRHandedness.Right };

        /// <inheritdoc/>
        public void Initialise(XRFlightConfig config)
        {
            IsActive = true;
            Debug.Log("[SWEF] GenericXRAdapter: Initialised.");
        }

        /// <inheritdoc/>
        public void RecenterView()
        {
            Debug.Log("[SWEF] GenericXRAdapter: RecenterView (stub).");
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime) { /* No per-frame work for generic adapter */ }

        /// <inheritdoc/>
        public void Shutdown()
        {
            IsActive = false;
            Debug.Log("[SWEF] GenericXRAdapter: Shutdown.");
        }

        /// <inheritdoc/>
        public XRHandState GetLeftHandState()  => _leftHand;

        /// <inheritdoc/>
        public XRHandState GetRightHandState() => _rightHand;
    }
}
