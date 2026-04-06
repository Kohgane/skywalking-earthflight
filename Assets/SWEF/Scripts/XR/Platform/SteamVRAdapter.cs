// SteamVRAdapter.cs — Phase 112: VR/XR Flight Experience
// SteamVR/OpenXR: controller input, lighthouse tracking.
// Namespace: SWEF.XR

using UnityEngine;

#if SWEF_STEAMVR
// SteamVR Plugin types would be referenced here in a real project.
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Platform adapter for SteamVR and generic OpenXR PC VR headsets.
    /// Wraps Valve SteamVR Plugin actions for controller input and
    /// supports lighthouse positional tracking.
    /// Compiled only when <c>SWEF_STEAMVR</c> is defined.
    /// </summary>
#if SWEF_STEAMVR
    public class SteamVRAdapter : IXRPlatformAdapter
    {
        /// <inheritdoc/>
        public XRPlatform Platform => XRPlatform.SteamVR;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        private readonly XRHandState _leftHand  = new XRHandState { Hand = XRHandedness.Left };
        private readonly XRHandState _rightHand = new XRHandState { Hand = XRHandedness.Right };

        /// <inheritdoc/>
        public void Initialise(XRFlightConfig config)
        {
            IsActive = true;
            Debug.Log("[SWEF] SteamVRAdapter: Initialised.");
        }

        /// <inheritdoc/>
        public void RecenterView()
        {
            // TODO: SteamVR.System.ResetSeatedZeroPose() when SteamVR Plugin is present.
            Debug.Log("[SWEF] SteamVRAdapter: RecenterView.");
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // Update controller states from SteamVR actions (stub).
            _leftHand.IsTracked  = false;
            _rightHand.IsTracked = false;
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            IsActive = false;
            Debug.Log("[SWEF] SteamVRAdapter: Shutdown.");
        }

        /// <inheritdoc/>
        public XRHandState GetLeftHandState()  => _leftHand;

        /// <inheritdoc/>
        public XRHandState GetRightHandState() => _rightHand;
    }
#endif
}
