// RunwayData.cs — SWEF Landing & Airport System (Phase 68)
using System;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — Serializable data for a single runway strip.
    ///
    /// <para>Instances are embedded in <see cref="AirportData"/> runway lists and
    /// consumed by <see cref="ApproachGuidance"/>, <see cref="LandingDetector"/>,
    /// and <see cref="AirportRegistry"/>.</para>
    /// </summary>
    [Serializable]
    public class RunwayData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Runway designator, e.g. "09L/27R".</summary>
        [Tooltip("Runway designator, e.g. \"09L/27R\".")]
        public string runwayId = "09/27";

        /// <summary>Magnetic heading of the primary runway end in degrees (0–360).</summary>
        [Tooltip("Magnetic heading of the primary runway end in degrees.")]
        public float heading = 90f;

        // ── Dimensions ────────────────────────────────────────────────────────

        /// <summary>Runway length in meters.</summary>
        [Tooltip("Runway length in meters.")]
        public float length = 2000f;

        /// <summary>Runway width in meters.</summary>
        [Tooltip("Runway width in meters.")]
        public float width = 45f;

        // ── World Positions ───────────────────────────────────────────────────

        /// <summary>World-space position of the runway threshold (landing end).</summary>
        [Tooltip("World-space position of the runway threshold.")]
        public Vector3 thresholdPosition;

        /// <summary>World-space position of the far runway end.</summary>
        [Tooltip("World-space position of the far runway end.")]
        public Vector3 endPosition;

        // ── Surface ───────────────────────────────────────────────────────────

        /// <summary>Current surface condition affecting braking performance.</summary>
        [Tooltip("Current surface condition.")]
        public RunwayCondition condition = RunwayCondition.Dry;

        // ── Approach Parameters ───────────────────────────────────────────────

        /// <summary>ILS glide slope angle in degrees (typically 3°).</summary>
        [Tooltip("ILS glide slope angle in degrees.")]
        public float glideSlopeAngle = LandingConfig.DefaultGlideSlopeAngle;

        /// <summary>Decision altitude in meters AGL; pilot must see the runway by this height.</summary>
        [Tooltip("Decision altitude in meters AGL.")]
        public float decisionAltitude = LandingConfig.DefaultDecisionAltitude;

        // ── Capabilities ──────────────────────────────────────────────────────

        /// <summary><c>true</c> when this runway has an Instrument Landing System.</summary>
        [Tooltip("Whether an ILS transmitter is installed on this runway.")]
        public bool hasILS = false;

        /// <summary><c>true</c> when edge lighting is available for night operations.</summary>
        [Tooltip("Whether runway edge lights are installed.")]
        public bool hasLighting = true;

        // ── Computed Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the normalised direction vector from threshold to end.
        /// </summary>
        /// <returns>A unit vector pointing from <see cref="thresholdPosition"/> toward <see cref="endPosition"/>.</returns>
        public Vector3 GetRunwayDirection()
        {
            Vector3 dir = endPosition - thresholdPosition;
            return dir == Vector3.zero ? Vector3.forward : dir.normalized;
        }

        /// <summary>
        /// Returns the Y (vertical) position of the runway centerline mid-point.
        /// </summary>
        /// <returns>Average Y of threshold and end positions.</returns>
        public float GetRunwayCenter()
        {
            return (thresholdPosition.y + endPosition.y) * 0.5f;
        }
    }
}
