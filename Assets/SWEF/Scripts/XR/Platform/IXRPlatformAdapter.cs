// IXRPlatformAdapter.cs — Phase 112: VR/XR Flight Experience
// Platform abstraction interface for XR adapters.
// Namespace: SWEF.XR

namespace SWEF.XR
{
    /// <summary>
    /// Platform-agnostic interface that all XR platform adapters must implement.
    /// Provides lifecycle, input, and platform-specific feature hooks.
    /// </summary>
    public interface IXRPlatformAdapter
    {
        /// <summary>The platform this adapter targets.</summary>
        XRPlatform Platform { get; }

        /// <summary>Whether this adapter is currently active and tracking.</summary>
        bool IsActive { get; }

        /// <summary>
        /// Initialise the adapter with the provided configuration.
        /// Called once after instantiation.
        /// </summary>
        void Initialise(XRFlightConfig config);

        /// <summary>Recenter the headset origin to the current head position.</summary>
        void RecenterView();

        /// <summary>Update per-frame adapter state. Called from MonoBehaviour.Update.</summary>
        void Tick(float deltaTime);

        /// <summary>Clean up platform resources. Called on session stop.</summary>
        void Shutdown();

        /// <summary>Get the current left-hand tracking state.</summary>
        XRHandState GetLeftHandState();

        /// <summary>Get the current right-hand tracking state.</summary>
        XRHandState GetRightHandState();
    }
}
