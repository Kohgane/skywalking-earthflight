// DisasterFlightModifier.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — MonoBehaviour that applies natural-disaster hazard effects to the
    /// player's flight every frame.  Subscribes to <see cref="DisasterManager"/> events
    /// and queries all active hazard zones for turbulence, visibility, thermal forces,
    /// speed reductions, and screen-shake triggers.
    ///
    /// <para>Attach to the player aircraft or a persistent manager object.</para>
    /// </summary>
    public class DisasterFlightModifier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Player Reference")]
        [Tooltip("Player aircraft transform. Auto-found from FlightController if null.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Turbulence")]
        [Tooltip("Base turbulence shake magnitude applied per unit of turbulence intensity.")]
        [SerializeField] [Min(0f)] private float _turbulenceShakeMagnitude = 0.1f;

        [Header("Thermal")]
        [Tooltip("Force scale applied to vertical thermal force.")]
        [SerializeField] [Min(0f)] private float _thermalForceScale = 1f;

        [Header("Visual Feedback")]
        [Tooltip("CanvasGroup used for hazard zone entry flash.  Leave null to disable.")]
        [SerializeField] private CanvasGroup _warningFlashOverlay;

        [Tooltip("Flash duration in seconds when entering a hazard zone.")]
        [SerializeField] [Min(0.1f)] private float _warningFlashDuration = 0.5f;

        // ── Cached State ──────────────────────────────────────────────────────────

        private float _currentTurbulence;
        private float _currentVisibility;   // 0 = full visibility, 1 = zero
        private Vector3 _currentThermal;
        private float _currentSpeedMultiplier = 1f;

        private bool  _wasInHazard;
        private float _flashTimer;
        private Vector3 _lastShakeOffset;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (_playerTransform == null)
            {
                var fc = FindFirstObjectByType<Flight.FlightController>();
                if (fc != null) _playerTransform = fc.transform;
            }
        }

        private void Update()
        {
            RefreshHazardEffects();
            UpdateWarningFlash();
        }

        // ── Hazard Query ──────────────────────────────────────────────────────────

        private void RefreshHazardEffects()
        {
            if (DisasterManager.Instance == null || _playerTransform == null)
            {
                _currentTurbulence    = 0f;
                _currentVisibility    = 0f;
                _currentThermal       = Vector3.zero;
                _currentSpeedMultiplier = 1f;
                return;
            }

            Vector3 pos = _playerTransform.position;
            float   alt = pos.y;

            List<HazardZone> zones = DisasterManager.Instance.GetHazardsAtPosition(pos, alt);

            _currentTurbulence    = 0f;
            _currentVisibility    = 0f;
            _currentThermal       = Vector3.zero;
            _currentSpeedMultiplier = 1f;

            bool inHazard = zones.Count > 0;

            foreach (HazardZone z in zones)
            {
                float localIntensity = z.GetIntensityAtPosition(pos);

                switch (z.type)
                {
                    case HazardZoneType.Turbulence:
                        _currentTurbulence += localIntensity;
                        break;

                    case HazardZoneType.ReducedVisibility:
                    case HazardZoneType.AshCloud:
                        _currentVisibility += localIntensity;
                        _currentSpeedMultiplier = Mathf.Min(
                            _currentSpeedMultiplier,
                            1f - localIntensity * DisasterConfig.MaxSpeedReductionAsh);
                        break;

                    case HazardZoneType.ThermalUpDraft:
                        _currentThermal += Vector3.up * (localIntensity * _thermalForceScale);
                        break;

                    case HazardZoneType.DebrisField:
                        _currentSpeedMultiplier = Mathf.Min(
                            _currentSpeedMultiplier,
                            1f - localIntensity * DisasterConfig.MaxSpeedReductionDebris);
                        _currentTurbulence += localIntensity * 0.5f;
                        break;
                }
            }

            _currentTurbulence    = Mathf.Clamp(_currentTurbulence, 0f, DisasterConfig.MaxTurbulenceMultiplier);
            _currentVisibility    = Mathf.Clamp01(_currentVisibility);
            _currentSpeedMultiplier = Mathf.Clamp01(_currentSpeedMultiplier);

            // Apply shake if turbulence is significant
            if (_currentTurbulence > 0.1f)
            {
                ApplyTurbulenceShake();
            }
            else if (_lastShakeOffset != Vector3.zero)
            {
                // Restore any residual shake offset when turbulence stops
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    mainCam.transform.localPosition -= _lastShakeOffset;
                _lastShakeOffset = Vector3.zero;
            }

            // Trigger warning flash on zone entry
            if (inHazard && !_wasInHazard)
                TriggerWarningFlash();
            _wasInHazard = inHazard;
        }

        private void ApplyTurbulenceShake()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            float shake = _currentTurbulence * _turbulenceShakeMagnitude * Time.deltaTime;
            Vector3 basePos = mainCam.transform.localPosition;
            // Remove any previous frame's offset before adding the new one.
            // We store the shake offset in a dedicated field so we can subtract it next frame.
            mainCam.transform.localPosition = basePos - _lastShakeOffset;
            _lastShakeOffset = new Vector3(
                UnityEngine.Random.Range(-shake, shake),
                UnityEngine.Random.Range(-shake, shake),
                0f);
            mainCam.transform.localPosition += _lastShakeOffset;
        }

        private void TriggerWarningFlash()
        {
            _flashTimer = _warningFlashDuration;
        }

        private void UpdateWarningFlash()
        {
            if (_warningFlashOverlay == null) return;
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                _warningFlashOverlay.alpha = _flashTimer / _warningFlashDuration;
            }
            else
            {
                _warningFlashOverlay.alpha = 0f;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Current aggregate turbulence intensity (0 = none).</summary>
        public float GetCurrentTurbulence() => _currentTurbulence;

        /// <summary>Current aggregate visibility reduction factor (0 = full, 1 = zero).</summary>
        public float GetCurrentVisibility() => _currentVisibility;

        /// <summary>Current aggregate thermal force vector to apply to the flight controller.</summary>
        public Vector3 GetThermalForce() => _currentThermal;

        /// <summary>Current speed multiplier due to ash clouds or debris (0–1).</summary>
        public float GetSpeedMultiplier() => _currentSpeedMultiplier;
    }
}
