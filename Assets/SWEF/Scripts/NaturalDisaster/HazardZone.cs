// HazardZone.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System;
using UnityEngine;

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — Serializable class that defines a hazard area surrounding an
    /// <see cref="ActiveDisaster"/>.  Hazard zones grow during Onset/Peak and
    /// contract during Declining/Aftermath.
    /// </summary>
    [Serializable]
    public class HazardZone
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        /// <summary>Session-unique identifier (e.g. "volcano_001_ash").</summary>
        public string zoneId;

        /// <summary>Category of hazard this zone represents.</summary>
        public HazardZoneType type;

        // ── Geometry ──────────────────────────────────────────────────────────────

        /// <summary>World-space centre of the hazard zone.</summary>
        public Vector3 center;

        /// <summary>Current radius of the hazard zone in metres.</summary>
        public float radius;

        /// <summary>Maximum radius this zone can reach during peak intensity.</summary>
        public float maxRadius;

        // ── Intensity ─────────────────────────────────────────────────────────────

        /// <summary>Current intensity of the hazard (0 = none, 1 = maximum).</summary>
        [Range(0f, 1f)]
        public float intensity;

        // ── Altitude Bounds ───────────────────────────────────────────────────────

        /// <summary>Minimum altitude (metres) at which this hazard is active.</summary>
        public float altitudeFloor;

        /// <summary>Maximum altitude (metres) at which this hazard is active.</summary>
        public float altitudeCeiling;

        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Whether this zone is currently in effect.</summary>
        public bool isActive = true;

        // ── Spatial Queries ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="playerPos"/> lies within the
        /// horizontal sphere of this zone and <paramref name="playerAlt"/> is
        /// between <see cref="altitudeFloor"/> and <see cref="altitudeCeiling"/>.
        /// </summary>
        public bool IsPlayerInside(Vector3 playerPos, float playerAlt)
        {
            if (!isActive) return false;
            if (playerAlt < altitudeFloor || playerAlt > altitudeCeiling) return false;
            float sqDist = (playerPos - center).sqrMagnitude;
            return sqDist <= radius * radius;
        }

        /// <summary>
        /// Returns the hazard intensity at <paramref name="pos"/> using a linear
        /// falloff from the zone centre.  Returns 0 when outside the zone.
        /// </summary>
        public float GetIntensityAtPosition(Vector3 pos)
        {
            if (!isActive || radius <= 0f) return 0f;
            float dist = Vector3.Distance(pos, center);
            if (dist >= radius) return 0f;
            float t = 1f - dist / radius;   // 1 at centre, 0 at edge
            return intensity * t;
        }

        // ── Dynamic Sizing ────────────────────────────────────────────────────────

        /// <summary>
        /// Expands the zone radius at <paramref name="rate"/> metres-per-second,
        /// clamped to <see cref="maxRadius"/>.
        /// </summary>
        public void Expand(float deltaTime, float rate)
        {
            radius = Mathf.Min(radius + rate * deltaTime, maxRadius);
        }

        /// <summary>
        /// Contracts the zone radius at <paramref name="rate"/> metres-per-second,
        /// clamped to zero.
        /// </summary>
        public void Contract(float deltaTime, float rate)
        {
            radius = Mathf.Max(0f, radius - rate * deltaTime);
        }
    }
}
