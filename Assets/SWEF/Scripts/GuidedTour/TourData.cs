using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.GuidedTour
{
    /// <summary>
    /// Difficulty level for a guided tour.
    /// </summary>
    public enum TourDifficulty { Easy, Medium, Hard }

    /// <summary>
    /// ScriptableObject that defines a complete guided tour including all waypoints.
    /// Create via <c>Assets → Create → SWEF → Guided Tour → Tour Data</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Guided Tour/Tour Data", fileName = "NewTourData")]
    public class TourData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────
        [Header("Identity")]
        /// <summary>Unique identifier used for progress tracking and achievements.</summary>
        [SerializeField] public string tourId;

        /// <summary>Display name shown in the tour catalog UI.</summary>
        [SerializeField] public string tourName;

        /// <summary>Short description shown on the catalog entry card.</summary>
        [SerializeField, TextArea(2, 5)] public string description;

        /// <summary>Localization key for the tour name/description (SWEF.Localization).</summary>
        [SerializeField] public string localizationKey;

        // ── Classification ────────────────────────────────────────────────────────
        [Header("Classification")]
        /// <summary>Difficulty rating shown as a badge in the catalog.</summary>
        [SerializeField] public TourDifficulty difficulty = TourDifficulty.Easy;

        /// <summary>Estimated completion time shown in the catalog.</summary>
        [SerializeField] public float estimatedDurationMinutes = 10f;

        /// <summary>Region tag used by the catalog filter (e.g. "Europe", "Asia").</summary>
        [SerializeField] public string region;

        // ── Waypoints ─────────────────────────────────────────────────────────────
        [Header("Waypoints")]
        /// <summary>Ordered list of waypoints the player visits during this tour.</summary>
        [SerializeField] public List<WaypointData> waypoints = new List<WaypointData>();

        // ── Inner types ───────────────────────────────────────────────────────────
        /// <summary>
        /// Data for a single waypoint in a guided tour.
        /// </summary>
        [Serializable]
        public struct WaypointData
        {
            /// <summary>World-space position of this waypoint.</summary>
            public Vector3 position;

            /// <summary>World-space point the camera/player looks toward on arrival.</summary>
            public Vector3 lookAtTarget;

            /// <summary>Human-readable name shown in the HUD.</summary>
            public string waypointName;

            /// <summary>Localization key for the narration subtitle text.</summary>
            public string narrationKey;

            /// <summary>
            /// Seconds the player must remain at the waypoint before auto-advance.
            /// A value of 0 means advance immediately on trigger entry.
            /// </summary>
            public float stayDurationSeconds;

            /// <summary>Radius in metres within which the player triggers this waypoint.</summary>
            public float triggerRadius;

            /// <summary>Whether the optional camera angle override is active.</summary>
            public bool useCameraAngleOverride;

            /// <summary>Optional camera angle override applied when the waypoint is reached.</summary>
            public Quaternion cameraAngleOverride;
        }
    }
}
