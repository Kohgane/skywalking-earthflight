using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Stub MonoBehaviour for future hand tracking support.
    /// All gesture detection is stubbed — always returns
    /// <see cref="HandGesture.None"/> until the XR Hands package is integrated.
    /// </summary>
    public class XRHandTracker : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────

        /// <summary>Recognised hand gestures (planned mappings).</summary>
        public enum HandGesture
        {
            /// <summary>No recognised gesture.</summary>
            None,
            /// <summary>Open palm — mapped to hover/brake.</summary>
            OpenPalm,
            /// <summary>Closed fist — mapped to boost throttle.</summary>
            Fist,
            /// <summary>Index finger extended — mapped to direction control.</summary>
            Point,
            /// <summary>Thumb and index touching — mapped to fine altitude adjustment.</summary>
            Pinch,
            /// <summary>Thumb up — mapped to screenshot.</summary>
            ThumbsUp
        }

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Hand Tracking")]
        [SerializeField] private bool enableHandTracking = false;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current gesture detected on the left hand.</summary>
        public HandGesture LeftHandGesture  { get; private set; } = HandGesture.None;

        /// <summary>Current gesture detected on the right hand.</summary>
        public HandGesture RightHandGesture { get; private set; } = HandGesture.None;

        /// <summary>Whether the left hand is currently being tracked.</summary>
        public bool IsLeftHandTracked  { get; private set; }

        /// <summary>Whether the right hand is currently being tracked.</summary>
        public bool IsRightHandTracked { get; private set; }

        /// <summary>Fired when the left-hand gesture changes.</summary>
        public event Action<HandGesture> OnLeftGestureChanged;

        /// <summary>Fired when the right-hand gesture changes.</summary>
        public event Action<HandGesture> OnRightGestureChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (!enableHandTracking) return;
            if (!XRPlatformDetector.IsHandTrackingAvailable) return;

            // TODO: Implement with XR Hands package
            // The stubs below keep the gestures at their default None values.
            UpdateLeftHand();
            UpdateRightHand();
        }

        // ── Private stubs ─────────────────────────────────────────────────────────

        private void UpdateLeftHand()
        {
            // TODO: Implement with XR Hands package
            HandGesture newGesture = HandGesture.None; // stub
            bool tracked = false;                       // stub

            if (tracked != IsLeftHandTracked || newGesture != LeftHandGesture)
            {
                IsLeftHandTracked = tracked;
                HandGesture prev  = LeftHandGesture;
                LeftHandGesture   = newGesture;

                if (prev != newGesture)
                {
                    Debug.Log($"[SWEF] Hand tracking: Left={LeftHandGesture}, Right={RightHandGesture}");
                    OnLeftGestureChanged?.Invoke(LeftHandGesture);
                }
            }
        }

        private void UpdateRightHand()
        {
            // TODO: Implement with XR Hands package
            HandGesture newGesture = HandGesture.None; // stub
            bool tracked = false;                       // stub

            if (tracked != IsRightHandTracked || newGesture != RightHandGesture)
            {
                IsRightHandTracked = tracked;
                HandGesture prev   = RightHandGesture;
                RightHandGesture   = newGesture;

                if (prev != newGesture)
                {
                    Debug.Log($"[SWEF] Hand tracking: Left={LeftHandGesture}, Right={RightHandGesture}");
                    OnRightGestureChanged?.Invoke(RightHandGesture);
                }
            }
        }
    }
}
