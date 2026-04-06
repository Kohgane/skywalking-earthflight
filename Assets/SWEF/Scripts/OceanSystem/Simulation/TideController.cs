// TideController.cs — Phase 117: Advanced Ocean & Maritime System
// Tide simulation: configurable tidal cycle, spring/neap tides, water level.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates tidal water level changes over a configurable cycle.
    /// Supports spring and neap tide multipliers. Other systems query
    /// <see cref="CurrentWaterLevel"/> to offset shoreline and buoyancy calculations.
    /// </summary>
    public class TideController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Debug")]
        [SerializeField] [Range(0f, 1f)] private float debugTidePhase = 0f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _tidePhase;        // 0..1 normalised phase within one cycle
        private float _springNeapPhase;  // slower lunar cycle for spring/neap modulation
        private bool  _isSpringTide;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the tide transitions between high and low.</summary>
        public event Action<bool> OnTideStateChanged; // true = high tide

        // ── Public Properties ─────────────────────────────────────────────────────

        /// <summary>Current water level offset from mean sea level in metres.</summary>
        public float CurrentWaterLevel { get; private set; }

        /// <summary>Normalised tide phase (0 = low water, 0.5 = high water).</summary>
        public float TidePhase => _tidePhase;

        /// <summary>Whether current conditions are spring tide.</summary>
        public bool IsSpringTide => _isSpringTide;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (config == null || !config.enableTides) return;
            TickTide();
        }

        // ── Tide Calculation ──────────────────────────────────────────────────────

        private void TickTide()
        {
            float cycleDuration = config.tidalCycleDuration;
            _tidePhase       = (_tidePhase + Time.deltaTime / cycleDuration) % 1f;
            _springNeapPhase = (_springNeapPhase + Time.deltaTime / (cycleDuration * 14.765f)) % 1f; // lunar fortnightly

            float springFactor = Mathf.Lerp(1f, config.springTideMultiplier, (Mathf.Sin(_springNeapPhase * Mathf.PI * 2f) + 1f) * 0.5f);
            _isSpringTide      = springFactor > (1f + config.springTideMultiplier) * 0.5f;

            float tidalHeight  = Mathf.Sin(_tidePhase * Mathf.PI * 2f); // −1..+1
            CurrentWaterLevel  = tidalHeight * config.tidalRange * 0.5f * springFactor;
        }

        /// <summary>
        /// Overrides the tide phase directly (editor or scripting use).
        /// </summary>
        public void SetTidePhase(float phase)
        {
            _tidePhase = Mathf.Clamp01(phase);
        }
    }
}
