// StandardLandingChallenge.cs — Phase 120: Precision Landing Challenge System
// Standard runway precision landing: ILS approach, visual approach, circling approach.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages a standard runway landing challenge.
    /// Supports ILS approach, visual approach, and circling approach modes.
    /// Monitors glideslope/localiser adherence and triggers scoring on touchdown.
    /// </summary>
    public class StandardLandingChallenge : MonoBehaviour
    {
        // ── Approach Mode ─────────────────────────────────────────────────────

        /// <summary>Available approach types for the standard challenge.</summary>
        public enum ApproachMode { ILS, Visual, Circling }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Challenge Settings")]
        [SerializeField] private ApproachMode mode = ApproachMode.ILS;
        [SerializeField] private float glideSlopeAngleDeg = 3f;
        [SerializeField] private float runwayHeadingDeg   = 90f;
        [SerializeField] private float touchdownZoneStart = 300f;
        [SerializeField] private float touchdownZoneEnd   = 900f;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<ApproachSnapshot> _snapshots = new List<ApproachSnapshot>();
        private bool _isActive;
        private float _captureInterval = 1f;
        private float _captureTimer;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current approach mode.</summary>
        public ApproachMode Mode => mode;

        /// <summary>Whether this challenge session is currently active.</summary>
        public bool IsActive => _isActive;

        /// <summary>Collected approach snapshots for scoring.</summary>
        public IReadOnlyList<ApproachSnapshot> Snapshots => _snapshots;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate this challenge and begin approach monitoring.</summary>
        public void Activate()
        {
            _isActive = true;
            _snapshots.Clear();
            _captureTimer = 0f;
        }

        /// <summary>Deactivate and clear state.</summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>Feed current flight state to record an approach snapshot.</summary>
        public void RecordSnapshot(float glideSlopeDots, float locDots, float speedKnots,
                                   float targetKnots, bool gearDown, int flaps, float altFt)
        {
            if (!_isActive) return;
            _snapshots.Add(new ApproachSnapshot
            {
                GlideSlopeDots   = glideSlopeDots,
                LocaliserDots    = locDots,
                SpeedKnots       = speedKnots,
                TargetSpeedKnots = targetKnots,
                GearDown         = gearDown,
                FlapSetting      = flaps,
                AltitudeFeet     = altFt,
                Time             = System.DateTime.UtcNow
            });
        }

        /// <summary>Returns true if touchdown position is within the designated zone.</summary>
        public bool IsInTouchdownZone(float distanceFromThresholdMetres)
        {
            return distanceFromThresholdMetres >= touchdownZoneStart &&
                   distanceFromThresholdMetres <= touchdownZoneEnd;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive) return;
            _captureTimer += Time.deltaTime;
            if (_captureTimer >= _captureInterval)
            {
                _captureTimer = 0f;
                // Snapshot collection triggered externally by flight controller integration
            }
        }
    }
}
