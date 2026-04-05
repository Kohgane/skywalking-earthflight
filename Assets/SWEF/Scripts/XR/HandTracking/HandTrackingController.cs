// HandTrackingController.cs — Phase 112: VR/XR Flight Experience
// Hand tracking input processor supporting Meta Quest and Apple Vision Pro.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Processes hand tracking input from the active XR platform adapter
    /// and dispatches normalised <see cref="XRHandState"/> data to listeners.
    /// Works with Meta Quest hand tracking and Apple Vision Pro gestures.
    /// </summary>
    public class HandTrackingController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static HandTrackingController Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Settings")]
        [SerializeField] private bool enableHandTracking = true;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current left-hand state.</summary>
        public XRHandState LeftHand  { get; private set; } = new XRHandState { Hand = XRHandedness.Left };

        /// <summary>Current right-hand state.</summary>
        public XRHandState RightHand { get; private set; } = new XRHandState { Hand = XRHandedness.Right };

        /// <summary>Whether hand tracking is currently active and both hands visible.</summary>
        public bool IsTracking => enableHandTracking && (LeftHand.IsTracked || RightHand.IsTracked);

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired each frame with updated left and right hand states.</summary>
        public event Action<XRHandState, XRHandState> OnHandStatesUpdated;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!enableHandTracking) return;
            PollHandStates();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enable or disable hand tracking input processing.</summary>
        public void SetHandTrackingEnabled(bool enabled)
        {
            enableHandTracking = enabled;
            Debug.Log($"[SWEF] HandTrackingController: Hand tracking {(enabled ? "enabled" : "disabled")}.");
        }

        /// <summary>Returns the hand state for the specified handedness.</summary>
        public XRHandState GetHandState(XRHandedness hand) =>
            hand == XRHandedness.Left ? LeftHand : RightHand;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void PollHandStates()
        {
            var manager = XRFlightManager.Instance;
            if (manager?.PlatformAdapter == null) return;

            LeftHand  = manager.PlatformAdapter.GetLeftHandState();
            RightHand = manager.PlatformAdapter.GetRightHandState();
            OnHandStatesUpdated?.Invoke(LeftHand, RightHand);
        }
    }
}
