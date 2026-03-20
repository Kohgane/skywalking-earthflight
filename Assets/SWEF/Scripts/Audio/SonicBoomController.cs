using System;
using UnityEngine;
using SWEF.Flight;

namespace SWEF.Audio
{
    /// <summary>
    /// Triggers a sonic boom effect and event when the player's speed crosses Mach 1
    /// (the altitude-accurate speed of sound). Provides a <see cref="CurrentMach"/>
    /// property for HUD display.
    /// </summary>
    public class SonicBoomController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Audio")]
        [SerializeField] private AudioClip boomClip;
        [Range(0f, 1f)]
        [SerializeField] private float boomVolume = 1f;

        [Header("Cooldown")]
        [SerializeField] private float cooldownSeconds = 5f;

        [Header("Refs (auto-found if null)")]
        [SerializeField] private FlightController   flightController;
        [SerializeField] private AltitudeController altitudeController;
        [SerializeField] private SpatialAudioManager spatialAudioManager;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the player crosses Mach 1.</summary>
        public event Action<float> OnSonicBoom;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Current Mach number (speed / speed-of-sound). Read-only.</summary>
        public float CurrentMach { get; private set; }

        // ── Runtime ───────────────────────────────────────────────────────────────
        private float _cooldownTimer;
        private bool  _wasPastMach1;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (flightController   == null) flightController   = FindFirstObjectByType<FlightController>();
            if (altitudeController == null) altitudeController = FindFirstObjectByType<AltitudeController>();
            if (spatialAudioManager == null) spatialAudioManager = FindFirstObjectByType<SpatialAudioManager>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;

            CheckMachTransition();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Evaluates current Mach number and fires <see cref="OnSonicBoom"/> on Mach 1 crossing.</summary>
        public void CheckMachTransition()
        {
            if (flightController == null) return;

            float alt  = altitudeController != null ? altitudeController.CurrentAltitudeMeters : 0f;
            float sos  = DopplerEffectController.GetSpeedOfSound(alt);
            float speed = flightController.Velocity.magnitude;

            CurrentMach = sos > 0f ? speed / sos : 0f;

            bool isPastMach1 = CurrentMach >= 1f;

            if (isPastMach1 && !_wasPastMach1 && _cooldownTimer <= 0f)
            {
                TriggerBoom();
            }

            _wasPastMach1 = isPastMach1;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void TriggerBoom()
        {
            _cooldownTimer = cooldownSeconds;

            if (boomClip != null)
            {
                if (spatialAudioManager != null)
                    spatialAudioManager.PlayAtPosition(boomClip, transform.position, boomVolume, 0f);
                else
                    AudioSource.PlayClipAtPoint(boomClip, transform.position, boomVolume);
            }

            OnSonicBoom?.Invoke(CurrentMach);
            Debug.Log($"[SonicBoom] Mach {CurrentMach:F2} — sonic boom triggered!");
        }
    }
}
