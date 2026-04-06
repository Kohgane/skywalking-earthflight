// CarrierNavigationSystem.cs — Phase 117: Advanced Ocean & Maritime System
// Carrier approach: ACLS, ICLS, meatball (Fresnel lens), LSO calls.
// Namespace: SWEF.OceanSystem

#if SWEF_CARRIER_AVAILABLE || !UNITY_EDITOR
using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Provides carrier approach guidance.
    /// Computes meatball glidepath state (Fresnel lens), lineup deviation, and
    /// angle-of-attack indexer data. Emits LSO call strings.
    /// </summary>
    public class CarrierNavigationSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Glidepath")]
        [Tooltip("Desired glideslope angle in degrees.")]
        [SerializeField] private float glideslopeDeg = 3.5f;
        [Tooltip("Approach axis azimuth offset from carrier centreline (angled deck, degrees).")]
        [SerializeField] private float angledDeckOffset = -9f;

        [Header("Approach Gate")]
        [Tooltip("Distance from carrier at which approach monitoring begins (metres).")]
        [SerializeField] private float approachGateDistance = 5000f;

        [Header("LSO")]
        [SerializeField] private float waveOffGlidepathDeviation = 1f; // degrees

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised with an LSO call string for the approaching aircraft.</summary>
        public event Action<string> OnLSOCall;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes approach guidance data for an aircraft at <paramref name="aircraftPos"/>
        /// with <paramref name="approachSpeedKnots"/>.
        /// </summary>
        public ApproachGuidance GetGuidance(Vector3 aircraftPos, float approachSpeedKnots)
        {
            var touchdownPoint = transform.position;

            // Glidepath deviation
            var toAircraft     = aircraftPos - touchdownPoint;
            float distance     = toAircraft.magnitude;
            float idealHeight  = Mathf.Tan(glideslopeDeg * Mathf.Deg2Rad) * distance;
            float heightError  = aircraftPos.y - touchdownPoint.y - idealHeight;
            float glideslopeDev = Mathf.Atan2(heightError, distance) * Mathf.Rad2Deg;

            // Lineup deviation
            var flatToAircraft  = new Vector2(toAircraft.x, toAircraft.z);
            var approachAxis    = new Vector2(
                Mathf.Sin((transform.eulerAngles.y + angledDeckOffset) * Mathf.Deg2Rad),
                Mathf.Cos((transform.eulerAngles.y + angledDeckOffset) * Mathf.Deg2Rad));
            float lineupDev = Vector2.SignedAngle(approachAxis, flatToAircraft.normalized);

            var glidepathState = ClassifyGlidepath(glideslopeDev);

            // Emit LSO call if on approach gate
            if (distance <= approachGateDistance)
                EmitLSOCall(glidepathState, lineupDev, approachSpeedKnots);

            return new ApproachGuidance
            {
                glidepathState      = glidepathState,
                glideslopeDeviation = glideslopeDev,
                lineupDeviation     = lineupDev,
                rangeMetres         = distance
            };
        }

        private GlidepathState ClassifyGlidepath(float devDeg)
        {
            if (devDeg >  2f) return GlidepathState.High;
            if (devDeg >  0.5f) return GlidepathState.SlightlyHigh;
            if (devDeg < -2f) return GlidepathState.Low;
            if (devDeg < -0.5f) return GlidepathState.SlightlyLow;
            return GlidepathState.OnGlidepath;
        }

        private void EmitLSOCall(GlidepathState gs, float lineupDev, float speedKnots)
        {
            string call = gs switch
            {
                GlidepathState.High        => "You're high — come down",
                GlidepathState.SlightlyHigh => "Slightly high — easy with it",
                GlidepathState.Low         => "WAVE OFF WAVE OFF — you're low",
                GlidepathState.SlightlyLow  => "Little power — come up",
                _                          => "Roger ball"
            };

            if (Mathf.Abs(lineupDev) > 5f)
                call = "Come left / come right — lineup";

            OnLSOCall?.Invoke(call);
        }

        // ── Guidance Data ─────────────────────────────────────────────────────────

        /// <summary>Computed approach guidance data returned to the caller.</summary>
        public struct ApproachGuidance
        {
            /// <summary>Meatball glidepath state.</summary>
            public GlidepathState glidepathState;
            /// <summary>Glideslope deviation in degrees (positive = high).</summary>
            public float glideslopeDeviation;
            /// <summary>Lineup deviation in degrees (positive = right of centreline).</summary>
            public float lineupDeviation;
            /// <summary>Distance to touchdown in metres.</summary>
            public float rangeMetres;
        }
    }
}
#endif
