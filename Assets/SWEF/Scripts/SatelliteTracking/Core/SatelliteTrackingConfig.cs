// SatelliteTrackingConfig.cs — Phase 114: Satellite & Space Debris Tracking
// ScriptableObject holding all runtime-configurable tracking parameters.
// Namespace: SWEF.SatelliteTracking

using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// ScriptableObject configuration asset for the Satellite Tracking system.
    /// Create via <em>Assets → Create → SWEF → SatelliteTracking → Tracking Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/SatelliteTracking/Tracking Config", fileName = "SatelliteTrackingConfig")]
    public class SatelliteTrackingConfig : ScriptableObject
    {
        // ── Update Frequency ──────────────────────────────────────────────────────

        [Header("Update Frequency")]
        /// <summary>How often (seconds) orbital positions are recalculated.</summary>
        [Range(0.1f, 60f)]
        [Tooltip("Position update interval in seconds.")]
        public float positionUpdateInterval = 1f;

        /// <summary>How often (seconds) TLE data is refreshed from the data source.</summary>
        [Range(60f, 86400f)]
        [Tooltip("TLE data refresh interval in seconds.")]
        public float tleRefreshInterval = 3600f;

        /// <summary>How often (seconds) conjunction analysis is run.</summary>
        [Range(1f, 300f)]
        [Tooltip("Conjunction analysis interval in seconds.")]
        public float conjunctionCheckInterval = 30f;

        // ── Visibility ────────────────────────────────────────────────────────────

        [Header("Visibility")]
        /// <summary>Maximum number of satellites rendered simultaneously.</summary>
        [Range(10, 2000)]
        [Tooltip("Cap on simultaneously visible/rendered satellites.")]
        public int maxVisibleSatellites = 200;

        /// <summary>Altitude band (km) around the player displayed in the radar HUD.</summary>
        [Range(10f, 1000f)]
        [Tooltip("Altitude range in km for the debris radar display.")]
        public float debrisRadarRangeKm = 50f;

        /// <summary>Minimum visual magnitude for pass prediction (higher = dimmer).</summary>
        [Range(-10f, 10f)]
        [Tooltip("Satellites dimmer than this magnitude are excluded from pass predictions.")]
        public float passPredictionMinMagnitude = 4f;

        // ── Orbit Prediction ──────────────────────────────────────────────────────

        [Header("Orbit Prediction")]
        /// <summary>Number of orbit prediction points per visualised pass.</summary>
        [Range(20, 2000)]
        [Tooltip("Resolution of the orbit path line renderer.")]
        public int orbitPathPoints = 360;

        /// <summary>How many hours ahead the SGP4 propagator predicts positions.</summary>
        [Range(0.5f, 168f)]
        [Tooltip("Orbit prediction horizon in hours.")]
        public float predictionHorizonHours = 24f;

        /// <summary>Whether to include perturbation corrections in SGP4 output.</summary>
        [Tooltip("Enable full SGP4 perturbation terms (atmospheric drag, J2, J3, J4).")]
        public bool enablePerturbations = true;

        // ── Debris ────────────────────────────────────────────────────────────────

        [Header("Space Debris")]
        /// <summary>Global density multiplier for procedural debris generation.</summary>
        [Range(0f, 5f)]
        [Tooltip("Scales the number of procedural debris objects generated.")]
        public float debrisDensityMultiplier = 1f;

        /// <summary>Maximum number of procedural debris objects per shell.</summary>
        [Range(0, 10000)]
        [Tooltip("Cap on procedural debris objects per altitude shell.")]
        public int maxDebrisPerShell = 500;

        /// <summary>Collision probability threshold above which a warning is issued.</summary>
        [Range(1e-6f, 1e-2f)]
        [Tooltip("Collision probability threshold for Red-level conjunction warnings.")]
        public float collisionWarningThreshold = 1e-4f;

        // ── Data Source ───────────────────────────────────────────────────────────

        [Header("TLE Data Source")]
        /// <summary>Base URL for TLE data retrieval (CelesTrak default).</summary>
        [Tooltip("URL used to fetch TLE files.")]
        public string tleDataUrl = "https://celestrak.org/SOCRATES/";

        /// <summary>Whether to use the mock/offline TLE dataset when no network is available.</summary>
        [Tooltip("Fall back to built-in mock TLE data when network is unavailable.")]
        public bool useMockDataOffline = true;

        // ── Scale ─────────────────────────────────────────────────────────────────

        [Header("Scene Scale")]
        /// <summary>Kilometres per Unity world unit for orbital visualisation.</summary>
        [Range(1f, 1000f)]
        [Tooltip("Scene scale: how many km one Unity world unit represents.")]
        public float kmPerWorldUnit = 10f;

        /// <summary>Earth radius represented in Unity world units.</summary>
        public float EarthRadiusWorldUnits => 6371f / kmPerWorldUnit;
    }
}
