// HandGestureRecognizer.cs — Phase 112: VR/XR Flight Experience
// Gesture recognition: pinch, grab, point, open palm, thumbs up.
// Namespace: SWEF.XR

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Recognises discrete hand gestures from continuous <see cref="XRHandState"/> data.
    /// Supported gestures: Pinch, Grab, Point, OpenPalm, ThumbsUp.
    /// Fires events when gestures start, hold, and end.
    /// </summary>
    public class HandGestureRecognizer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Thresholds")]
        [SerializeField] private float pinchThreshold    = 0.7f;
        [SerializeField] private float grabThreshold     = 0.8f;
        [SerializeField] private float confirmDuration   = 0.1f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently active gesture on the left hand.</summary>
        public XRGestureType LeftGesture  { get; private set; } = XRGestureType.None;

        /// <summary>Currently active gesture on the right hand.</summary>
        public XRGestureType RightGesture { get; private set; } = XRGestureType.None;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a gesture is first confirmed. Args: hand, gesture.</summary>
        public event Action<XRHandedness, XRGestureType> OnGestureStarted;

        /// <summary>Fired when a gesture ends. Args: hand, gesture.</summary>
        public event Action<XRHandedness, XRGestureType> OnGestureEnded;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly Dictionary<XRHandedness, float>         _candidateTimers = new Dictionary<XRHandedness, float>
        {
            { XRHandedness.Left,  0f },
            { XRHandedness.Right, 0f }
        };
        private readonly Dictionary<XRHandedness, XRGestureType> _candidateGesture = new Dictionary<XRHandedness, XRGestureType>
        {
            { XRHandedness.Left,  XRGestureType.None },
            { XRHandedness.Right, XRGestureType.None }
        };

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            var htc = HandTrackingController.Instance;
            if (htc != null)
                htc.OnHandStatesUpdated += ProcessHandStates;
        }

        private void OnDestroy()
        {
            var htc = HandTrackingController.Instance;
            if (htc != null)
                htc.OnHandStatesUpdated -= ProcessHandStates;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Classifies a raw hand state into a gesture type.
        /// Can be called directly for testing without an update loop.
        /// </summary>
        public XRGestureType ClassifyGesture(XRHandState state)
        {
            if (!state.IsTracked) return XRGestureType.None;

            if (state.GrabStrength >= grabThreshold)
                return XRGestureType.Grab;

            if (state.PinchStrength >= pinchThreshold)
                return XRGestureType.Pinch;

            // Finger-pose heuristics use the platform-provided gesture field
            return state.ActiveGesture;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void ProcessHandStates(XRHandState left, XRHandState right)
        {
            ProcessSingleHand(left,  XRHandedness.Left,  ref _candidateTimers[XRHandedness.Left],
                              ref _candidateGesture[XRHandedness.Left],  ref _leftGestureRef);
            ProcessSingleHand(right, XRHandedness.Right, ref _candidateTimers[XRHandedness.Right],
                              ref _candidateGesture[XRHandedness.Right], ref _rightGestureRef);
        }

        private XRGestureType _leftGestureRef  = XRGestureType.None;
        private XRGestureType _rightGestureRef = XRGestureType.None;

        private void ProcessSingleHand(XRHandState state, XRHandedness hand,
                                       ref float timer, ref XRGestureType candidate,
                                       ref XRGestureType active)
        {
            XRGestureType detected = ClassifyGesture(state);

            if (detected == candidate)
            {
                timer += Time.deltaTime;
                if (timer >= confirmDuration && active != detected)
                {
                    XRGestureType prev = active;
                    active = detected;

                    if (prev != XRGestureType.None)
                        OnGestureEnded?.Invoke(hand, prev);
                    if (detected != XRGestureType.None)
                        OnGestureStarted?.Invoke(hand, detected);

                    if (hand == XRHandedness.Left)  LeftGesture  = active;
                    else                             RightGesture = active;
                }
            }
            else
            {
                candidate = detected;
                timer     = 0f;
            }
        }
    }
}
