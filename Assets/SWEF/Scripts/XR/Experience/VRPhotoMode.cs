// VRPhotoMode.cs — Phase 112: VR/XR Flight Experience
// VR photo/panorama capture mode with hand-held virtual camera.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>Photo capture format options.</summary>
    public enum VRPhotoFormat
    {
        /// <summary>Standard flat screenshot.</summary>
        Flat,
        /// <summary>360° equirectangular panorama.</summary>
        Panorama360,
        /// <summary>Stereo side-by-side for VR viewers.</summary>
        StereoSideBySide
    }

    /// <summary>
    /// Provides a virtual hand-held camera for VR photo and panorama capture.
    /// The virtual camera follows the dominant hand transform and can capture
    /// screenshots on pinch gesture or controller trigger.
    /// </summary>
    public class VRPhotoMode : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Virtual Camera")]
        [SerializeField] private GameObject virtualCameraModel;
        [SerializeField] private Camera     virtualCameraLens;
        [SerializeField] private float      virtualCameraFov = 60f;

        [Header("Capture")]
        [SerializeField] private VRPhotoFormat defaultFormat = VRPhotoFormat.Flat;
        [SerializeField] private int           captureWidth  = 3840;
        [SerializeField] private int           captureHeight = 2160;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether photo mode is currently active.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Number of photos captured this session.</summary>
        public int CaptureCount { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after a photo is captured. Arg: capture format.</summary>
        public event Action<VRPhotoFormat> OnPhotoCaptured;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            SetPhotoModeActive(false);
        }

        private void Update()
        {
            if (!IsActive) return;
            SyncCameraToHand();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enters or exits VR photo mode.</summary>
        public void SetPhotoModeActive(bool active)
        {
            IsActive = active;
            if (virtualCameraModel != null) virtualCameraModel.SetActive(active);
            if (virtualCameraLens  != null) virtualCameraLens.enabled = active;
            Debug.Log($"[SWEF] VRPhotoMode: Photo mode {(active ? "activated" : "deactivated")}.");
        }

        /// <summary>Captures a photo using the current virtual camera view.</summary>
        public void CapturePhoto(VRPhotoFormat? format = null)
        {
            if (!IsActive)
            {
                Debug.LogWarning("[SWEF] VRPhotoMode: Cannot capture — photo mode not active.");
                return;
            }

            VRPhotoFormat fmt = format ?? defaultFormat;
            CaptureCount++;

            // Delegate to ScreenCapture or a RenderTexture blit in production.
            Debug.Log($"[SWEF] VRPhotoMode: Captured photo #{CaptureCount} ({fmt}).");
            OnPhotoCaptured?.Invoke(fmt);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SyncCameraToHand()
        {
            var htc = HandTrackingController.Instance;
            if (htc == null || virtualCameraModel == null) return;

            XRHandState hand = htc.GetHandState(
                XRFlightManager.Instance?.Config?.defaultDominantHand ?? XRHandedness.Right);

            if (hand.IsTracked)
                virtualCameraModel.transform.position = hand.PalmPosition;
        }
    }
}
