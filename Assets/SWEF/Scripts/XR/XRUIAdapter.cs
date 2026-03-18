using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Adapts existing Canvas-based UI for VR world-space rendering.
    /// Converts assigned canvases between Overlay/Camera and WorldSpace modes,
    /// positions them in front of the XR head camera, and optionally follows
    /// the head rotation with smooth interpolation.
    /// </summary>
    public class XRUIAdapter : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Canvases")]
        [SerializeField] private Canvas[] uiCanvases;

        [Header("World Space Layout")]
        [SerializeField] private float worldSpaceDistance = 2f;
        [SerializeField] private float worldSpaceScale    = 0.001f;

        [Header("Head Follow")]
        [SerializeField] private bool  followHead  = true;
        [SerializeField] private float followSpeed = 2f;

        [Header("Future Options")]
        [SerializeField] private bool curvedUI = false; // reserved for future curved UI support

        // ── Public events ─────────────────────────────────────────────────────────
        /// <summary>
        /// Fires whenever the UI mode is converted.
        /// Parameter is <c>true</c> for world-space, <c>false</c> for screen-space.
        /// </summary>
        public event Action<bool> OnUIConverted;

        // ── Private state ─────────────────────────────────────────────────────────
        private bool     _isWorldSpace;
        private Camera   _headCamera;

        // Stored original render modes so we can restore them
        private RenderMode[] _originalRenderModes;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            CacheOriginalRenderModes();
        }

        private void OnEnable()
        {
            if (XRRigManager.Instance != null)
                XRRigManager.Instance.OnRigModeChanged += OnRigModeChanged;
        }

        private void OnDisable()
        {
            if (XRRigManager.Instance != null)
                XRRigManager.Instance.OnRigModeChanged -= OnRigModeChanged;
        }

        private void LateUpdate()
        {
            if (!_isWorldSpace || !followHead) return;

            Camera cam = GetHeadCamera();
            if (cam == null) return;

            Vector3    targetPos = cam.transform.position + cam.transform.forward * worldSpaceDistance;
            Quaternion targetRot = cam.transform.rotation;

            foreach (Canvas canvas in uiCanvases)
            {
                if (canvas == null) continue;
                canvas.transform.position = Vector3.Lerp(
                    canvas.transform.position, targetPos,
                    followSpeed * Time.deltaTime);
                canvas.transform.rotation = Quaternion.Slerp(
                    canvas.transform.rotation, targetRot,
                    followSpeed * Time.deltaTime);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts all assigned canvases to WorldSpace render mode and
        /// positions them in front of the XR camera.
        /// </summary>
        public void ConvertToWorldSpace()
        {
            if (uiCanvases == null || uiCanvases.Length == 0) return;

            Camera cam = GetHeadCamera();

            foreach (Canvas canvas in uiCanvases)
            {
                if (canvas == null) continue;

                canvas.renderMode = RenderMode.WorldSpace;

                // Scale down for world-space legibility
                canvas.transform.localScale = Vector3.one * worldSpaceScale;

                // Position in front of head camera if available
                if (cam != null)
                {
                    canvas.transform.position = cam.transform.position
                                                + cam.transform.forward * worldSpaceDistance;
                    canvas.transform.rotation = cam.transform.rotation;
                }
            }

            _isWorldSpace = true;
            Debug.Log("[SWEF] XRUIAdapter: UI converted to WorldSpace.");
            OnUIConverted?.Invoke(true);
        }

        /// <summary>
        /// Reverts all assigned canvases back to their original render mode
        /// for use in mobile/flat-screen mode.
        /// </summary>
        public void ConvertToScreenSpace()
        {
            if (uiCanvases == null) return;

            for (int i = 0; i < uiCanvases.Length; i++)
            {
                if (uiCanvases[i] == null) continue;

                if (_originalRenderModes != null && i < _originalRenderModes.Length)
                    uiCanvases[i].renderMode = _originalRenderModes[i];
                else
                    uiCanvases[i].renderMode = RenderMode.ScreenSpaceOverlay;

                uiCanvases[i].transform.localScale = Vector3.one;
            }

            _isWorldSpace = false;
            Debug.Log("[SWEF] XRUIAdapter: UI reverted to ScreenSpace.");
            OnUIConverted?.Invoke(false);
        }

        /// <summary>
        /// Adjusts the distance of UI panels from the player's head.
        /// </summary>
        public void SetUIDistance(float distance)
        {
            worldSpaceDistance = Mathf.Clamp(distance, 0.5f, 10f);
        }

        /// <summary>
        /// Enables or disables smooth head-following for world-space UI panels.
        /// </summary>
        public void SetFollowHead(bool follow)
        {
            followHead = follow;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnRigModeChanged(XRRigManager.RigMode mode)
        {
            if (mode == XRRigManager.RigMode.XR)
                ConvertToWorldSpace();
            else
                ConvertToScreenSpace();
        }

        private Camera GetHeadCamera()
        {
            if (_headCamera != null) return _headCamera;

            // Try XRRigManager's XR camera first (via serialized field — not accessible here directly)
            // Fall back to Camera.main
            _headCamera = Camera.main;
            return _headCamera;
        }

        private void CacheOriginalRenderModes()
        {
            if (uiCanvases == null) return;
            _originalRenderModes = new RenderMode[uiCanvases.Length];
            for (int i = 0; i < uiCanvases.Length; i++)
            {
                if (uiCanvases[i] != null)
                    _originalRenderModes[i] = uiCanvases[i].renderMode;
            }
        }
    }
}
