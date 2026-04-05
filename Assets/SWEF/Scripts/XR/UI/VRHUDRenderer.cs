// VRHUDRenderer.cs — Phase 112: VR/XR Flight Experience
// Head-up display overlay: speed, altitude, heading at comfortable viewing distance.
// Namespace: SWEF.XR

using System;
using UnityEngine;
using UnityEngine.UI;

#if SWEF_TMP_AVAILABLE
using TMPro;
#endif

namespace SWEF.XR
{
    /// <summary>
    /// Renders a VR heads-up display projected at a comfortable viewing distance.
    /// Displays: airspeed (knots), altitude (ft), magnetic heading (°), and
    /// current flight phase. The HUD follows the player's head with a slight lag.
    /// </summary>
    public class VRHUDRenderer : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("HUD Placement")]
        [SerializeField] private Camera  headCamera;
        [SerializeField] private float   projectionDistance = 3f;
        [SerializeField] private float   followSmoothSpeed  = 5f;

        [Header("Labels")]
#if SWEF_TMP_AVAILABLE
        [SerializeField] private TMP_Text labelAirspeed;
        [SerializeField] private TMP_Text labelAltitude;
        [SerializeField] private TMP_Text labelHeading;
        [SerializeField] private TMP_Text labelPhase;
#else
        [SerializeField] private Text labelAirspeed;
        [SerializeField] private Text labelAltitude;
        [SerializeField] private Text labelHeading;
        [SerializeField] private Text labelPhase;
#endif

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether the HUD is currently visible.</summary>
        public bool IsVisible { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (headCamera == null) headCamera = Camera.main;
        }

        private void Update()
        {
            if (!IsVisible || headCamera == null) return;
            FollowHead();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the VR HUD.</summary>
        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>Refreshes displayed flight data.</summary>
        public void UpdateHUD(float airspeedKnots, float altitudeFt,
                              float headingDeg, VRFlightPhase phase)
        {
            if (labelAirspeed != null) labelAirspeed.text = $"{airspeedKnots:F0} kt";
            if (labelAltitude != null) labelAltitude.text = $"{altitudeFt:F0} ft";
            if (labelHeading  != null) labelHeading.text  = $"{headingDeg:F0}°";
            if (labelPhase    != null) labelPhase.text    = phase.ToString();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void FollowHead()
        {
            Vector3 target = headCamera.transform.position
                           + headCamera.transform.forward * projectionDistance;
            transform.position = Vector3.Lerp(transform.position, target,
                                              followSmoothSpeed * Time.deltaTime);
            transform.LookAt(headCamera.transform);
            transform.Rotate(0f, 180f, 0f);
        }
    }
}
