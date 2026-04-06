// CollisionWarningSystem.cs — Phase 114: Satellite & Space Debris Tracking
// Conjunction assessment: closest approach calculation, collision probability, avoidance maneuver suggestion.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Performs conjunction (close-approach) analysis across all tracked satellites and
    /// debris objects, issuing collision warnings and suggesting avoidance maneuvers.
    /// </summary>
    public class CollisionWarningSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Thresholds")]
        [Tooltip("Miss-distance below which a conjunction alert is raised (km).")]
        [Range(0.1f, 100f)]
        [SerializeField] private float conjunctionThresholdKm = 5f;

        [Tooltip("Collision probability threshold for Red alert level.")]
        [Range(1e-6f, 1e-2f)]
        [SerializeField] private float redAlertThreshold = 1e-4f;

        [Tooltip("Collision probability threshold for Yellow alert level.")]
        [Range(1e-6f, 1e-2f)]
        [SerializeField] private float yellowAlertThreshold = 1e-5f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when a new conjunction is detected.</summary>
        public event Action<ConjunctionData> OnConjunctionDetected;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<ConjunctionData> _activeConjunctions = new List<ConjunctionData>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the list of currently active conjunctions.</summary>
        public IReadOnlyList<ConjunctionData> ActiveConjunctions => _activeConjunctions.AsReadOnly();

        /// <summary>
        /// Runs a full conjunction screening pass across all tracked satellites and debris.
        /// </summary>
        public void RunCheck(IReadOnlyList<SatelliteRecord> satellites,
                             IReadOnlyList<DebrisObject> debris)
        {
            _activeConjunctions.Clear();

            // Satellite–satellite conjunctions
            for (int i = 0; i < satellites.Count; i++)
            {
                var satA = satellites[i];
                if (satA.currentState == null) continue;

                for (int j = i + 1; j < satellites.Count; j++)
                {
                    var satB = satellites[j];
                    if (satB.currentState == null) continue;

                    float dist = Vector3.Distance(satA.currentState.positionECI,
                                                  satB.currentState.positionECI);
                    if (dist < conjunctionThresholdKm)
                    {
                        var conj = BuildConjunction(satA.noradId, satB.noradId,
                                                     dist, satA.currentState);
                        _activeConjunctions.Add(conj);
                        OnConjunctionDetected?.Invoke(conj);
                    }
                }

                // Satellite–debris conjunctions
                foreach (var d in debris)
                {
                    float dist = Vector3.Distance(satA.currentState.positionECI, d.positionECI);
                    if (dist < conjunctionThresholdKm)
                    {
                        var conj = BuildConjunction(satA.noradId, d.noradId,
                                                     dist, satA.currentState);
                        _activeConjunctions.Add(conj);
                        OnConjunctionDetected?.Invoke(conj);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the required avoidance maneuver delta-V (m/s) to increase
        /// miss distance to the safe separation (km).
        /// </summary>
        public static float CalculateAvoidanceDeltaV(float currentMissDistKm,
                                                      float targetMissDistKm,
                                                      float timeToTCAmin)
        {
            if (timeToTCAmin <= 0f) return 0f;
            float additionalDistKm = Mathf.Max(0f, targetMissDistKm - currentMissDistKm);
            // Approximate: Δv ≈ ΔR / Δt  (km → m/s)
            return (additionalDistKm * 1000f) / (timeToTCAmin * 60f);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private ConjunctionData BuildConjunction(int primaryId, int secondaryId,
                                                  float missDistKm, OrbitalState primaryState)
        {
            // Approximate collision probability using hard-sphere model
            float combinedRadius = 0.005f; // km (typical 5 m combined radius)
            float sigma = Mathf.Max(0.1f, missDistKm * 0.1f);
            float pCollide = (float)(Math.Exp(-0.5 * (missDistKm / sigma) * (missDistKm / sigma))
                           * (combinedRadius * combinedRadius) / (sigma * sigma));
            pCollide = Mathf.Clamp(pCollide, 0f, 1f);

            int urgency = pCollide >= redAlertThreshold    ? 3
                        : pCollide >= yellowAlertThreshold ? 2
                        : missDistKm < 1f                  ? 1
                        : 0;

            float avoidDV = CalculateAvoidanceDeltaV(missDistKm, conjunctionThresholdKm * 2f, 60f);

            return new ConjunctionData
            {
                primaryNoradId       = primaryId,
                secondaryNoradId     = secondaryId,
                tcaUtc               = primaryState?.utcTime.AddMinutes(10) ?? DateTime.UtcNow,
                missDistanceKm       = missDistKm,
                collisionProbability = pCollide,
                avoidanceDeltaVms    = avoidDV,
                urgencyLevel         = urgency
            };
        }
    }
}
