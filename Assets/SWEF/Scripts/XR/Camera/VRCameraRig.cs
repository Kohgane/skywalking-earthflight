// VRCameraRig.cs — Phase 112: VR/XR Flight Experience
// VR camera rig with head tracking, IPD adjustment, FOV configuration.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Manages the VR camera rig including head tracking synchronisation,
    /// interpupillary distance (IPD) adjustment, and field-of-view configuration.
    /// Works alongside Unity's XR camera rig and XRFlightManager.
    /// </summary>
    public class VRCameraRig : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Camera References")]
        [SerializeField] private Camera leftEyeCamera;
        [SerializeField] private Camera rightEyeCamera;
        [SerializeField] private Camera centerCamera;

        [Header("IPD")]
        [SerializeField] private float ipdMetres = 0.064f;

        [Header("FOV")]
        [SerializeField] private float fieldOfView = 90f;

        [Header("Clipping")]
        [SerializeField] private float nearClipPlane = 0.05f;
        [SerializeField] private float farClipPlane  = 100000f;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current IPD in metres.</summary>
        public float IPD => ipdMetres;

        /// <summary>Current camera field of view.</summary>
        public float FOV => fieldOfView;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when IPD changes.</summary>
        public event Action<float> OnIPDChanged;

        /// <summary>Fired when FOV changes.</summary>
        public event Action<float> OnFOVChanged;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            var config = XRFlightManager.Instance?.Config;
            if (config != null)
            {
                SetIPD(config.defaultIpd);
                SetNearClipPlane(config.nearClipPlane);
            }
            ApplyCameraSettings();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adjusts the IPD (interpupillary distance) in metres.</summary>
        public void SetIPD(float metres)
        {
            ipdMetres = Mathf.Clamp(metres, 0.05f, 0.08f);
            PositionEyeCameras();
            OnIPDChanged?.Invoke(ipdMetres);
        }

        /// <summary>Sets the field of view for all cameras.</summary>
        public void SetFOV(float fov)
        {
            fieldOfView = Mathf.Clamp(fov, 60f, 120f);
            ApplyCameraSettings();
            OnFOVChanged?.Invoke(fieldOfView);
        }

        /// <summary>Sets the near clip plane distance.</summary>
        public void SetNearClipPlane(float distance)
        {
            nearClipPlane = Mathf.Clamp(distance, 0.01f, 0.5f);
            ApplyCameraSettings();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void PositionEyeCameras()
        {
            float halfIPD = ipdMetres * 0.5f;
            if (leftEyeCamera  != null) leftEyeCamera.transform.localPosition  = new Vector3(-halfIPD, 0f, 0f);
            if (rightEyeCamera != null) rightEyeCamera.transform.localPosition = new Vector3( halfIPD, 0f, 0f);
        }

        private void ApplyCameraSettings()
        {
            Camera[] cameras = { leftEyeCamera, rightEyeCamera, centerCamera };
            foreach (var cam in cameras)
            {
                if (cam == null) continue;
                cam.fieldOfView = fieldOfView;
                cam.nearClipPlane = nearClipPlane;
                cam.farClipPlane  = farClipPlane;
            }
        }
    }
}
