// VRHandVisualizer.cs — Phase 112: VR/XR Flight Experience
// Visual hand model rendering with ghost hands and haptic feedback triggers.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Renders VR hand models and drives haptic feedback in response to gestures
    /// and cockpit interactions.  A "ghost" hand appears when the real hand
    /// approaches an interactive object but has not yet grabbed it.
    /// </summary>
    public class VRHandVisualizer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Hand Models")]
        [SerializeField] private GameObject leftHandModel;
        [SerializeField] private GameObject rightHandModel;
        [SerializeField] private Material   ghostHandMaterial;
        [SerializeField] private float      ghostAlpha = 0.35f;

        [Header("Haptics")]
        [SerializeField] private float hapticAmplitude = 0.3f;
        [SerializeField] private float hapticDuration  = 0.05f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether ghost-hand overlay is currently shown.</summary>
        public bool IsGhostActive { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when haptic feedback is triggered. Args: hand, amplitude, duration.</summary>
        public event Action<XRHandedness, float, float> OnHapticTriggered;

        // ── Private state ─────────────────────────────────────────────────────────
        private Renderer _leftRenderer;
        private Renderer _rightRenderer;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (leftHandModel  != null) _leftRenderer  = leftHandModel.GetComponent<Renderer>();
            if (rightHandModel != null) _rightRenderer = rightHandModel.GetComponent<Renderer>();
        }

        private void Update()
        {
            SyncHandModels();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the ghost hand overlay on the specified hand.</summary>
        public void SetGhostHandVisible(XRHandedness hand, bool visible)
        {
            IsGhostActive = visible;
            Renderer renderer = hand == XRHandedness.Left ? _leftRenderer : _rightRenderer;
            if (renderer == null) return;

            if (visible && ghostHandMaterial != null)
            {
                renderer.material = ghostHandMaterial;
                Color c = renderer.material.color;
                c.a = ghostAlpha;
                renderer.material.color = c;
            }
        }

        /// <summary>Triggers haptic feedback on the specified hand.</summary>
        public void TriggerHaptic(XRHandedness hand, float amplitude = -1f, float duration = -1f)
        {
            float amp  = amplitude < 0f ? hapticAmplitude : amplitude;
            float dur  = duration  < 0f ? hapticDuration  : duration;
            OnHapticTriggered?.Invoke(hand, amp, dur);
            // TODO: call XR haptic API (OVRInput.SetControllerVibration / Unity XR Input) when packages are present.
        }

        /// <summary>Shows or hides the hand models.</summary>
        public void SetHandModelsVisible(bool visible)
        {
            if (leftHandModel  != null) leftHandModel.SetActive(visible);
            if (rightHandModel != null) rightHandModel.SetActive(visible);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SyncHandModels()
        {
            var htc = HandTrackingController.Instance;
            if (htc == null) return;

            SyncModel(leftHandModel,  htc.LeftHand);
            SyncModel(rightHandModel, htc.RightHand);
        }

        private static void SyncModel(GameObject model, XRHandState state)
        {
            if (model == null) return;
            model.SetActive(state.IsTracked);
            if (state.IsTracked)
            {
                model.transform.position = state.PalmPosition;
            }
        }
    }
}
