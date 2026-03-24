// RadarContact.cs — SWEF Radar & Threat Detection System (Phase 67)
using System;
using UnityEngine;

namespace SWEF.Radar
{
    /// <summary>
    /// Represents a single detected contact on the radar scope.
    /// <para>
    /// Instances are created and updated by <see cref="RadarSystem"/> during each
    /// scan cycle.  All positional data is expressed in world-space unless otherwise
    /// noted.
    /// </para>
    /// </summary>
    [Serializable]
    public class RadarContact
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique identifier assigned when the contact is first detected.</summary>
        public string contactId;

        /// <summary>Reference to the detected object's transform (may become null if destroyed).</summary>
        public Transform trackedTransform;

        /// <summary>IFF classification of this contact.</summary>
        public ContactClassification classification;

        /// <summary>Current threat level evaluated by <see cref="ThreatDetector"/>.</summary>
        public ThreatLevel threat;

        /// <summary>Radar cross-section category reported by the contact's <see cref="IFFTransponder"/>.</summary>
        public BlipSize size;

        // ── Spatial Data ──────────────────────────────────────────────────────

        /// <summary>Last known world-space position.</summary>
        public Vector3 position;

        /// <summary>Estimated velocity vector (m/s) derived from successive position samples.</summary>
        public Vector3 velocity;

        /// <summary>Distance from the player/radar origin in metres.</summary>
        public float distance;

        /// <summary>
        /// Bearing from the player's current heading, expressed as a value in the
        /// range [0, 360) degrees.  0° is straight ahead.
        /// </summary>
        public float bearing;

        /// <summary>Elevation angle from the radar origin in degrees (positive = above).</summary>
        public float elevation;

        // ── Signal Quality ────────────────────────────────────────────────────

        /// <summary>
        /// Normalised signal strength in [0, 1].  Degrades beyond
        /// <see cref="RadarConfig.SignalFalloffStart"/> × <see cref="RadarConfig.DefaultRadarRange"/>.
        /// </summary>
        public float signalStrength;

        // ── Timing ────────────────────────────────────────────────────────────

        /// <summary><see cref="Time.time"/> value when this contact was first detected.</summary>
        public float firstDetectedTime;

        /// <summary><see cref="Time.time"/> value of the most recent data refresh.</summary>
        public float lastUpdateTime;

        // ── Tracking ──────────────────────────────────────────────────────────

        /// <summary>Whether the radar is currently hard-locked onto this contact.</summary>
        public bool isLocked;

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable label shown on the radar display
        /// (e.g., "Eagle-2", "Unknown-07", "Event-Beacon").
        /// </summary>
        public string displayName;

        /// <summary>Icon sprite used to represent this contact on the radar display.</summary>
        public Sprite contactIcon;
    }
}
