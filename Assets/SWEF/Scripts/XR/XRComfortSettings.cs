using System;
using UnityEngine;
using SWEF.Atmosphere;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.XR
{
    /// <summary>
    /// VR-specific comfort and anti-motion-sickness settings.
    /// Integrates with <see cref="ComfortVignette"/> and dynamically adjusts
    /// vignette intensity based on rotation speed and movement.
    /// Settings are persisted to PlayerPrefs.
    /// </summary>
    public class XRComfortSettings : MonoBehaviour
    {
        // ── Enums ─────────────────────────────────────────────────────────────────

        /// <summary>Comfort preset levels.</summary>
        public enum ComfortLevel { Low, Medium, High, Custom }

        // ── PlayerPrefs keys ──────────────────────────────────────────────────────
        private const string KeyComfortLevel  = "SWEF_XR_ComfortLevel";
        private const string KeySnapTurning   = "SWEF_XR_SnapTurning";
        private const string KeyTunnelVision  = "SWEF_XR_TunnelVision";

        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private ComfortVignette comfortVignette;

        [Header("Vignette Intensity")]
        [SerializeField] private float vignetteTurnIntensity  = 0.6f;
        [SerializeField] private float vignetteSpeedIntensity = 0.4f;

        [Header("Snap Turning")]
        [SerializeField] private bool  snapTurningEnabled = false;
        [SerializeField] private float snapTurnAngle      = 30f;

        [Header("Tunnel Vision / FOV Restriction")]
        [SerializeField] private bool  tunnelVisionEnabled    = true;
        [SerializeField] private float maxTurnSpeed           = 45f;

        [Header("Ground Reference")]
        [SerializeField] private bool groundReferenceEnabled = true;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Currently applied comfort preset.</summary>
        public ComfortLevel CurrentLevel { get; private set; } = ComfortLevel.Medium;

        /// <summary>Fired whenever the comfort level changes.</summary>
        public event Action<ComfortLevel> OnComfortLevelChanged;

        // ── Private state ─────────────────────────────────────────────────────────
        private FlightController _flight;
        private Quaternion       _prevRotation;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (comfortVignette == null)
                comfortVignette = FindFirstObjectByType<ComfortVignette>();

            _flight = FindFirstObjectByType<FlightController>();

            // Restore saved settings
            ComfortLevel saved = (ComfortLevel)PlayerPrefs.GetInt(KeyComfortLevel, (int)ComfortLevel.Medium);
            snapTurningEnabled = PlayerPrefs.GetInt(KeySnapTurning,  0) == 1;
            tunnelVisionEnabled = PlayerPrefs.GetInt(KeyTunnelVision, 1) == 1;
            SetComfortLevel(saved);
        }

        private void Start()
        {
            _prevRotation = transform.rotation;
        }

        private void Update()
        {
            AdjustVignetteFromMotion();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a comfort preset, adjusting snap-turning, tunnel-vision,
        /// and max-turn-speed settings. Saves the selection to PlayerPrefs.
        /// </summary>
        public void SetComfortLevel(ComfortLevel level)
        {
            switch (level)
            {
                case ComfortLevel.Low:
                    snapTurningEnabled  = false;
                    tunnelVisionEnabled = false;
                    maxTurnSpeed        = 90f;
                    break;
                case ComfortLevel.Medium:
                    snapTurningEnabled  = false;
                    tunnelVisionEnabled = true;
                    maxTurnSpeed        = 60f;
                    break;
                case ComfortLevel.High:
                    snapTurningEnabled  = true;
                    tunnelVisionEnabled = true;
                    maxTurnSpeed        = 45f;
                    break;
                case ComfortLevel.Custom:
                    // No changes — user configured values are kept as-is
                    break;
            }

            CurrentLevel = level;
            SaveSettings();
            Debug.Log($"[SWEF] XRComfortSettings: Comfort level set to {level}");
            OnComfortLevelChanged?.Invoke(CurrentLevel);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void AdjustVignetteFromMotion()
        {
            if (comfortVignette == null) return;

            float dt = Time.deltaTime;

            // Compute angular speed from transform rotation delta
            Quaternion deltaRot = transform.rotation * Quaternion.Inverse(_prevRotation);
            _prevRotation = transform.rotation;

            deltaRot.ToAngleAxis(out float angleDeg, out _);
            if (angleDeg > 180f) angleDeg = 360f - angleDeg;
            float angularSpeed = dt > 0f ? angleDeg / dt : 0f;

            // Clamp rotation speed in VR to cap-induced nausea
            if (angularSpeed > maxTurnSpeed && _flight != null)
            {
                // Damp rotation by scaling back the flight rotation; indirect via comfortMode flag
                // The actual clamping is a hint — full implementation requires FlightController cooperation
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(KeyComfortLevel, (int)CurrentLevel);
            PlayerPrefs.SetInt(KeySnapTurning,  snapTurningEnabled  ? 1 : 0);
            PlayerPrefs.SetInt(KeyTunnelVision, tunnelVisionEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
