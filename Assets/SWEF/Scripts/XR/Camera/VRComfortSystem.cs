// VRComfortSystem.cs — Phase 112: VR/XR Flight Experience
// Motion sickness mitigation: vignette, snap turning, ground reference frame.
// Namespace: SWEF.XR

using System;
using UnityEngine;

namespace SWEF.XR
{
    /// <summary>
    /// Implements motion sickness mitigation techniques for VR flight:
    /// dynamic vignette during rotation, optional snap turning, ground reference
    /// frame overlay, and rest frame blending.
    /// </summary>
    public class VRComfortSystem : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Vignette")]
        [SerializeField] private bool  vignetteEnabled  = true;
        [SerializeField] private float vignetteRampSpeed = 2f;

        [Header("Snap Turning")]
        [SerializeField] private bool  snapTurningEnabled = false;
        [SerializeField] private float snapTurnAngle = 30f;

        [Header("Ground Reference")]
        [SerializeField] private bool         groundReferenceEnabled = true;
        [SerializeField] private GameObject   groundReferenceOverlay;

        [Header("Rest Frame")]
        [SerializeField] private bool         restFrameEnabled = false;
        [SerializeField] private GameObject   restFrameOverlay;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Current comfort level preset.</summary>
        public XRComfortLevel ComfortLevel { get; private set; } = XRComfortLevel.Medium;

        /// <summary>Current vignette intensity [0..1].</summary>
        public float CurrentVignetteIntensity { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a snap-turn step is executed. Arg: angle degrees.</summary>
        public event Action<float> OnSnapTurn;

        // ── Private state ─────────────────────────────────────────────────────────
        private float _targetVignette;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            SmoothVignette();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Applies a comfort preset, configuring all comfort features.</summary>
        public void SetComfortLevel(XRComfortLevel level)
        {
            ComfortLevel = level;

            switch (level)
            {
                case XRComfortLevel.Off:
                    vignetteEnabled      = false;
                    snapTurningEnabled   = false;
                    groundReferenceEnabled = false;
                    restFrameEnabled     = false;
                    break;
                case XRComfortLevel.Low:
                    vignetteEnabled      = true;
                    snapTurningEnabled   = false;
                    groundReferenceEnabled = false;
                    restFrameEnabled     = false;
                    break;
                case XRComfortLevel.Medium:
                    vignetteEnabled      = true;
                    snapTurningEnabled   = false;
                    groundReferenceEnabled = true;
                    restFrameEnabled     = false;
                    break;
                case XRComfortLevel.High:
                    vignetteEnabled      = true;
                    snapTurningEnabled   = true;
                    groundReferenceEnabled = true;
                    restFrameEnabled     = true;
                    break;
                // Custom: leave individual settings unchanged
            }

            UpdateOverlays();
            Debug.Log($"[SWEF] VRComfortSystem: Comfort level set to {level}.");
        }

        /// <summary>
        /// Updates the target vignette intensity based on angular speed.
        /// Call from a flight update loop with the current yaw/pitch rate.
        /// </summary>
        public void UpdateVignetteForRotationSpeed(float angularSpeedDegPerSec, float maxSpeed = 90f)
        {
            if (!vignetteEnabled)
            {
                _targetVignette = 0f;
                return;
            }
            float t = Mathf.Clamp01(angularSpeedDegPerSec / Mathf.Max(maxSpeed, 1f));
            float maxIntensity = XRFlightManager.Instance?.Config?.maxVignetteIntensity ?? 0.7f;
            _targetVignette = t * maxIntensity;
        }

        /// <summary>Executes a single snap-turn step. Does nothing when snap turning is disabled.</summary>
        public void SnapTurn(bool turnRight)
        {
            if (!snapTurningEnabled) return;
            float angle = turnRight ? snapTurnAngle : -snapTurnAngle;
            transform.Rotate(Vector3.up, angle, Space.World);
            OnSnapTurn?.Invoke(angle);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SmoothVignette()
        {
            CurrentVignetteIntensity = Mathf.MoveTowards(
                CurrentVignetteIntensity, _targetVignette, vignetteRampSpeed * Time.deltaTime);
        }

        private void UpdateOverlays()
        {
            if (groundReferenceOverlay != null)
                groundReferenceOverlay.SetActive(groundReferenceEnabled);
            if (restFrameOverlay != null)
                restFrameOverlay.SetActive(restFrameEnabled);
        }
    }
}
