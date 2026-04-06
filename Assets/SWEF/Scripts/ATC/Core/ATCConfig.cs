// ATCConfig.cs — Phase 119: Advanced AI Traffic Control
// ScriptableObject configuration for ATC system parameters.
// Namespace: SWEF.ATC

using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — ScriptableObject that holds all tunable parameters for the
    /// Advanced AI Traffic Control system.  Create one instance via
    /// Assets → Create → SWEF → ATC → ATC Config.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/ATC/ATC Config", fileName = "ATCConfig")]
    public class ATCConfig : ScriptableObject
    {
        // ── Separation Minimums ───────────────────────────────────────────────────

        [Header("Separation Minimums")]
        [Tooltip("Radar lateral separation minimum (nautical miles).")]
        [Range(1f, 10f)] public float radarSeparationNM = 3f;

        [Tooltip("Procedural lateral separation minimum (nautical miles).")]
        [Range(3f, 30f)] public float proceduralSeparationNM = 5f;

        [Tooltip("Standard vertical separation below FL290 (feet).")]
        [Range(500f, 2000f)] public float standardVerticalSeparationFt = 1000f;

        [Tooltip("RVSM vertical separation above FL290 (feet).")]
        [Range(500f, 2000f)] public float rvsmVerticalSeparationFt = 1000f;

        [Tooltip("Wake turbulence separation — heavy following heavy (NM).")]
        [Range(3f, 10f)] public float wakeTurbulenceHeavyNM = 4f;

        [Tooltip("Wake turbulence separation — heavy leading medium (NM).")]
        [Range(3f, 10f)] public float wakeTurbulenceMediumNM = 5f;

        // ── Communication ─────────────────────────────────────────────────────────

        [Header("Communication")]
        [Tooltip("Simulated radio communication delay in seconds.")]
        [Range(0f, 5f)] public float communicationDelaySeconds = 0.5f;

        [Tooltip("AI ATC response time after receiving a pilot transmission (seconds).")]
        [Range(0.5f, 10f)] public float aiResponseTimeSeconds = 2f;

        [Tooltip("Maximum number of transmissions queued before blocking.")]
        [Range(4, 32)] public int maxCommunicationQueueDepth = 16;

        [Tooltip("Duration to hold a frequency before timing out (seconds).")]
        [Range(10f, 300f)] public float frequencyTimeoutSeconds = 60f;

        // ── Traffic Density ───────────────────────────────────────────────────────

        [Header("Traffic Density")]
        [Tooltip("Maximum number of aircraft per ATC sector.")]
        [Range(5, 50)] public int maxAircraftPerSector = 20;

        [Tooltip("Maximum simultaneous landing traffic at an airport.")]
        [Range(1, 10)] public int maxSimultaneousLandings = 3;

        [Tooltip("Runway occupancy time limit (seconds) before runway incursion alert.")]
        [Range(30f, 180f)] public float runwayOccupancyLimitSeconds = 90f;

        // ── TCAS ──────────────────────────────────────────────────────────────────

        [Header("TCAS")]
        [Tooltip("Traffic Advisory range (nautical miles).")]
        [Range(5f, 30f)] public float tcasTARange = 20f;

        [Tooltip("Resolution Advisory range (nautical miles).")]
        [Range(1f, 10f)] public float tcasRARange = 5f;

        [Tooltip("TCAS TA trigger altitude difference (feet).")]
        [Range(500f, 2000f)] public float tcasTAAltDiffFt = 1200f;

        [Tooltip("TCAS RA trigger altitude difference (feet).")]
        [Range(100f, 1000f)] public float tcasRAAltDiffFt = 600f;

        // ── Route Optimization ────────────────────────────────────────────────────

        [Header("Route Optimization")]
        [Tooltip("Weight given to wind component when optimizing altitude.")]
        [Range(0f, 1f)] public float windOptimizationWeight = 0.4f;

        [Tooltip("Weight given to fuel burn when selecting route.")]
        [Range(0f, 1f)] public float fuelOptimizationWeight = 0.6f;

        [Tooltip("Maximum route deviation for weather avoidance (nautical miles).")]
        [Range(10f, 200f)] public float maxWeatherDeviationNM = 50f;

        // ── Conflict Detection ────────────────────────────────────────────────────

        [Header("Conflict Detection")]
        [Tooltip("Look-ahead time for conflict prediction (minutes).")]
        [Range(5f, 30f)] public float conflictLookaheadMinutes = 15f;

        [Tooltip("Safety buffer added on top of separation minimum for early warning (NM).")]
        [Range(0.5f, 5f)] public float conflictSafetyBufferNM = 1f;

        // ── AI Difficulty ─────────────────────────────────────────────────────────

        [Header("AI Difficulty")]
        [Tooltip("Overall AI ATC difficulty/realism level (0 = arcade, 1 = realistic).")]
        [Range(0f, 1f)] public float realismLevel = 0.8f;

        [Tooltip("Whether the AI uses standard phraseology.")]
        public bool useStandardPhraseology = true;

        [Tooltip("Whether to simulate communication errors and misunderstandings.")]
        public bool simulateCommunicationErrors = false;
    }
}
