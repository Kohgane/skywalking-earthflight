// MissionCheckpoint.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Defines a single flight checkpoint (waypoint) within a mission.
    ///
    /// <para>Checkpoints are ordered by <see cref="checkpointIndex"/> and are evaluated by
    /// <see cref="MissionManager"/> each physics tick. When the player's aircraft enters
    /// <see cref="radius"/> the checkpoint is stamped as passed.</para>
    /// </summary>
    [Serializable]
    public class MissionCheckpoint
    {
        // ── Ordering ──────────────────────────────────────────────────────────

        /// <summary>Zero-based sequence index; lower indices are visited first.</summary>
        [Tooltip("Zero-based order in which this checkpoint must be passed.")]
        public int checkpointIndex;

        /// <summary>Display label shown in the HUD (e.g. "WP-01", "Turn Point Alpha").</summary>
        [Tooltip("Short label shown in the HUD waypoint display.")]
        public string label;

        // ── Geometry ──────────────────────────────────────────────────────────

        /// <summary>World-space centre of the checkpoint trigger volume.</summary>
        [Tooltip("World-space centre of the checkpoint trigger sphere.")]
        public Vector3 position;

        /// <summary>Trigger radius in metres; the player must enter this sphere to pass.</summary>
        [Tooltip("Trigger radius in metres.")]
        [Min(1f)]
        public float radius = MissionConfig.CheckpointDefaultRadius;

        // ── Constraints ───────────────────────────────────────────────────────

        /// <summary>Required magnetic heading in degrees (−1 = any heading accepted).</summary>
        [Tooltip("Required heading in degrees. Set -1 to accept any heading.")]
        public float heading = -1f;

        /// <summary>Minimum AGL altitude in metres required to pass (−1 = unconstrained).</summary>
        [Tooltip("Minimum altitude in metres AGL. Set -1 for no minimum.")]
        public float minAltitude = -1f;

        /// <summary>Maximum AGL altitude in metres allowed when passing (−1 = unconstrained).</summary>
        [Tooltip("Maximum altitude in metres AGL. Set -1 for no maximum.")]
        public float maxAltitude = -1f;

        /// <summary>Maximum airspeed in m/s allowed at this checkpoint (−1 = unconstrained).</summary>
        [Tooltip("Maximum speed in m/s at pass. Set -1 for no speed limit.")]
        public float maxSpeed = -1f;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary><c>true</c> once the player has successfully passed through this checkpoint.</summary>
        [Tooltip("Set to true at runtime once passed (do not set in Inspector).")]
        public bool isPassed = false;

        /// <summary>Game-time (seconds since startup) at which the player passed this checkpoint.</summary>
        [Tooltip("Timestamp recorded when the checkpoint is passed (runtime only).")]
        public float passedTime;

        // ── Visuals ───────────────────────────────────────────────────────────

        /// <summary>Colour of the ring marker rendered in the world.</summary>
        [Tooltip("Colour used for the fly-through ring and HUD indicator.")]
        public Color markerColor = Color.cyan;

        /// <summary>When <c>true</c> a fly-through ring is rendered at this checkpoint's position.</summary>
        [Tooltip("Show a fly-through ring in the world for this checkpoint.")]
        public bool showRing = true;

        /// <summary>Uniform scale multiplier applied to the ring mesh (1 = default size).</summary>
        [Tooltip("Scale multiplier for the ring visual. 1 = default size.")]
        [Min(0.1f)]
        public float ringScale = 1f;
    }
}
