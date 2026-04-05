// VRUI.cs — Phase 112: VR/XR Flight Experience
// World-space VR UI panels (settings, HUD, menus) that follow gaze or cockpit.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>VR UI follow behaviour.</summary>
    public enum VRUIFollowMode
    {
        /// <summary>Panel stays fixed in world space.</summary>
        Fixed,
        /// <summary>Panel smoothly follows the player's gaze direction.</summary>
        GazeFollow,
        /// <summary>Panel is attached to a cockpit anchor point.</summary>
        CockpitAttached
    }

    /// <summary>
    /// Manages a world-space UI panel in VR. Switches between fixed, gaze-follow,
    /// and cockpit-attached placement modes. All Unity UI Canvas objects must be
    /// set to WorldSpace render mode before attaching to this component.
    /// </summary>
    public class VRUI : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Canvas")]
        [SerializeField] private Canvas      canvas;

        [Header("Placement")]
        [SerializeField] private VRUIFollowMode followMode = VRUIFollowMode.GazeFollow;
        [SerializeField] private float          followDistance    = 1.5f;
        [SerializeField] private float          followSmoothSpeed = 3f;
        [SerializeField] private Transform      cockpitAnchor;

        [Header("Head Camera")]
        [SerializeField] private Camera         headCamera;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether this UI panel is currently visible.</summary>
        public bool IsVisible { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.renderMode = RenderMode.WorldSpace;
            if (headCamera == null) headCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsVisible) return;
            FollowPanel();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides this VR UI panel.</summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (canvas != null) canvas.gameObject.SetActive(visible);
        }

        /// <summary>Sets the follow mode for this panel.</summary>
        public void SetFollowMode(VRUIFollowMode mode)
        {
            followMode = mode;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void FollowPanel()
        {
            switch (followMode)
            {
                case VRUIFollowMode.GazeFollow when headCamera != null:
                {
                    Vector3 target = headCamera.transform.position
                                   + headCamera.transform.forward * followDistance;
                    transform.position = Vector3.Lerp(transform.position, target,
                                                      followSmoothSpeed * Time.deltaTime);
                    transform.LookAt(headCamera.transform);
                    transform.Rotate(0f, 180f, 0f);
                    break;
                }
                case VRUIFollowMode.CockpitAttached when cockpitAnchor != null:
                    transform.position = cockpitAnchor.position;
                    transform.rotation = cockpitAnchor.rotation;
                    break;
            }
        }
    }
}
