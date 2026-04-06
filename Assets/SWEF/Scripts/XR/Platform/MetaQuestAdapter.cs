// MetaQuestAdapter.cs — Phase 112: VR/XR Flight Experience
// Meta Quest specific: passthrough mode, guardian boundary, hand tracking API.
// Namespace: SWEF.XR

using UnityEngine;

#if SWEF_META_QUEST
// Meta XR Core SDK types would be referenced here in a real project.
// Stub implementations keep the file compilable without the package.
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Platform adapter for Meta Quest 2/3/Pro headsets.
    /// Handles passthrough mode, Guardian boundary queries, and
    /// Meta-specific hand tracking via the Meta XR Core SDK.
    /// Compiled only when <c>SWEF_META_QUEST</c> is defined.
    /// </summary>
#if SWEF_META_QUEST
    public class MetaQuestAdapter : IXRPlatformAdapter
    {
        /// <inheritdoc/>
        public XRPlatform Platform => XRPlatform.MetaQuest;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }

        /// <summary>Whether passthrough (mixed-reality) is currently active.</summary>
        public bool PassthroughActive { get; private set; }

        private readonly XRHandState _leftHand  = new XRHandState { Hand = XRHandedness.Left };
        private readonly XRHandState _rightHand = new XRHandState { Hand = XRHandedness.Right };
        private XRFlightConfig _config;

        /// <inheritdoc/>
        public void Initialise(XRFlightConfig config)
        {
            _config  = config;
            IsActive = true;

            if (config != null && config.enablePassthroughOnQuest)
                EnablePassthrough();

            Debug.Log("[SWEF] MetaQuestAdapter: Initialised.");
        }

        /// <summary>Toggle Meta Quest passthrough (mixed-reality) mode.</summary>
        public void EnablePassthrough()
        {
            PassthroughActive = true;
            // TODO: call OVRPassthroughLayer.enabled = true when Meta XR SDK is present.
            Debug.Log("[SWEF] MetaQuestAdapter: Passthrough enabled.");
        }

        /// <summary>Disable passthrough and return to full VR.</summary>
        public void DisablePassthrough()
        {
            PassthroughActive = false;
            Debug.Log("[SWEF] MetaQuestAdapter: Passthrough disabled.");
        }

        /// <inheritdoc/>
        public void RecenterView()
        {
            // TODO: OVRManager.display.RecenterPose() when Meta XR SDK is present.
            Debug.Log("[SWEF] MetaQuestAdapter: RecenterView.");
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // Update hand states from OVR Hand API (stub).
            _leftHand.IsTracked  = false;
            _rightHand.IsTracked = false;
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (PassthroughActive) DisablePassthrough();
            IsActive = false;
            Debug.Log("[SWEF] MetaQuestAdapter: Shutdown.");
        }

        /// <inheritdoc/>
        public XRHandState GetLeftHandState()  => _leftHand;

        /// <inheritdoc/>
        public XRHandState GetRightHandState() => _rightHand;
    }
#endif
}
