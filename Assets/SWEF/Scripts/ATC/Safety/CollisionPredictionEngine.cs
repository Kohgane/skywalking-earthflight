// CollisionPredictionEngine.cs — Phase 119: Advanced AI Traffic Control
// Collision prediction: closest point of approach (CPA), time to CPA,
// probability of conflict.
// Namespace: SWEF.ATC

using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Geometric collision prediction engine computing closest
    /// point of approach (CPA) and time-to-CPA between two aircraft.
    /// </summary>
    public class CollisionPredictionEngine : MonoBehaviour
    {
        // ── CPA Result ────────────────────────────────────────────────────────────

        /// <summary>Result of a closest-point-of-approach calculation.</summary>
        public class CPAResult
        {
            /// <summary>Time to CPA in seconds (negative means past).</summary>
            public float timeToCPA;
            /// <summary>Horizontal distance at CPA in nautical miles.</summary>
            public float horizontalDistNM;
            /// <summary>Vertical distance at CPA in feet.</summary>
            public float verticalDistFt;
            /// <summary>Whether this constitutes a predicted loss of separation.</summary>
            public bool isConflict;
            /// <summary>Estimated probability of conflict (0–1).</summary>
            public float conflictProbability;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the closest point of approach between two aircraft.
        /// </summary>
        public CPAResult ComputeCPA(
            Vector3 posA, Vector3 velA, float altA, float vertRateA,
            Vector3 posB, Vector3 velB, float altB, float vertRateB,
            float sepNM = 3f, float vertSepFt = 1000f)
        {
            // Relative position and velocity (horizontal plane)
            Vector3 relPos = new Vector3(posA.x - posB.x, 0, posA.z - posB.z);
            Vector3 relVel = new Vector3(velA.x - velB.x, 0, velA.z - velB.z);

            float relSpeedSq = relVel.sqrMagnitude;

            float timeToCPA = relSpeedSq > 0.0001f
                ? -Vector3.Dot(relPos, relVel) / relSpeedSq
                : 0f;

            Vector3 closestPosA = posA + new Vector3(velA.x, 0, velA.z) * timeToCPA;
            Vector3 closestPosB = posB + new Vector3(velB.x, 0, velB.z) * timeToCPA;
            float horizM = Vector3.Distance(closestPosA, closestPosB);
            float horizNM = horizM / 1852f;

            float altAtCPA_A = altA + vertRateA * (timeToCPA / 60f);
            float altAtCPA_B = altB + vertRateB * (timeToCPA / 60f);
            float vertFt = Mathf.Abs(altAtCPA_A - altAtCPA_B);

            bool isConflict = horizNM < sepNM && vertFt < vertSepFt && timeToCPA > 0f;

            // Simple probability: inverse of separation ratio
            float prob = isConflict
                ? Mathf.Clamp01(1f - (horizNM / sepNM) * 0.5f - (vertFt / vertSepFt) * 0.5f)
                : Mathf.Clamp01(Mathf.Max(0f, 1f - horizNM / (sepNM * 2f)));

            return new CPAResult
            {
                timeToCPA         = timeToCPA,
                horizontalDistNM  = horizNM,
                verticalDistFt    = vertFt,
                isConflict        = isConflict,
                conflictProbability = prob
            };
        }

        /// <summary>
        /// Returns whether two aircraft currently violate separation.
        /// </summary>
        public bool IsSeparationLost(
            Vector3 posA, float altA, Vector3 posB, float altB,
            float sepNM = 3f, float vertSepFt = 1000f)
        {
            float horizNM = Vector3.Distance(
                new Vector3(posA.x, 0, posA.z),
                new Vector3(posB.x, 0, posB.z)) / 1852f;
            return horizNM < sepNM && Mathf.Abs(altA - altB) < vertSepFt;
        }
    }
}
