// NPCTrafficData.cs — Phase 110: Dynamic NPC & Air Traffic Ecosystem
// Enumerations, data classes, and configuration ScriptableObject for the NPC Traffic module.
// Namespace: SWEF.NPCTraffic

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.NPCTraffic
{
    // ════════════════════════════════════════════════════════════════════════════
    // NPC Aircraft categories
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>High-level category of an NPC aircraft.</summary>
    public enum NPCAircraftCategory
    {
        /// <summary>Large commercial passenger jet on a scheduled airline route.</summary>
        CommercialAirline = 0,
        /// <summary>Small- to medium-sized private/corporate jet with irregular routes.</summary>
        PrivateJet        = 1,
        /// <summary>Freighter or cargo-configured aircraft on cargo routes.</summary>
        CargoPlane        = 2,
        /// <summary>Military fixed-wing aircraft patrolling restricted zones.</summary>
        MilitaryAircraft  = 3,
        /// <summary>Rotary-wing aircraft operating locally or regionally.</summary>
        Helicopter        = 4,
        /// <summary>Small piston aircraft performing circuits near airports.</summary>
        TrainingAircraft  = 5
    }

    // ════════════════════════════════════════════════════════════════════════════
    // NPC Behavior states
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Current behavior state of an NPC aircraft in the AI state machine.</summary>
    public enum NPCBehaviorState
    {
        /// <summary>Aircraft is moving on the ground.</summary>
        Taxiing   = 0,
        /// <summary>Aircraft is executing departure roll and rotation.</summary>
        Takeoff   = 1,
        /// <summary>Aircraft is climbing to cruise altitude.</summary>
        Climbing  = 2,
        /// <summary>Aircraft is in level cruise flight.</summary>
        Cruising  = 3,
        /// <summary>Aircraft is descending for approach.</summary>
        Descending = 4,
        /// <summary>Aircraft is on final approach to runway.</summary>
        Approach  = 5,
        /// <summary>Aircraft is touching down and rolling out.</summary>
        Landing   = 6,
        /// <summary>Aircraft is in a holding pattern awaiting clearance.</summary>
        Holding   = 7,
        /// <summary>Aircraft is executing an emergency diversion.</summary>
        Emergency = 8,
        /// <summary>Aircraft is parked / inactive at a gate.</summary>
        Parked    = 9
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Route types
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Type of flight route assigned to an NPC.</summary>
    public enum NPCRouteType
    {
        /// <summary>Scheduled airport-to-airport service with SID/STAR procedures.</summary>
        AirportToAirport = 0,
        /// <summary>Repeating patrol loop for military aircraft.</summary>
        PatrolLoop       = 1,
        /// <summary>Circuit/touch-and-go pattern flown by training aircraft.</summary>
        TrainingCircuit  = 2,
        /// <summary>Semi-random general aviation route with ad-hoc waypoints.</summary>
        RandomGA         = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // LOD levels for visual representation
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Level-of-detail tier for an NPC aircraft visual.</summary>
    public enum NPCVisualLOD
    {
        /// <summary>Simple blip icon shown at long range.</summary>
        Icon     = 0,
        /// <summary>Low-polygon stand-in mesh used at medium range.</summary>
        LowPoly  = 1,
        /// <summary>Full model with livery and lights shown at close range.</summary>
        FullModel = 2
    }

    // ════════════════════════════════════════════════════════════════════════════
    // NPC Traffic density level
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Overall traffic density preset.</summary>
    public enum NPCTrafficDensity
    {
        /// <summary>No NPC aircraft spawned.</summary>
        None   = 0,
        /// <summary>Minimal traffic for performance-constrained devices.</summary>
        Sparse = 1,
        /// <summary>Moderate, balanced traffic (default).</summary>
        Normal = 2,
        /// <summary>Dense traffic for immersive environments.</summary>
        Dense  = 3
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Core data classes
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Runtime snapshot of a single NPC aircraft.</summary>
    [Serializable]
    public class NPCAircraftData
    {
        /// <summary>Unique identifier for this NPC instance.</summary>
        public string Id;

        /// <summary>Radio callsign displayed to the player (e.g. "UAL123").</summary>
        public string Callsign;

        /// <summary>ICAO aircraft type designator (e.g. "B738").</summary>
        public string AircraftType;

        /// <summary>Broad category controlling behavior and spawn rules.</summary>
        public NPCAircraftCategory Category;

        /// <summary>Identifier of the currently assigned route.</summary>
        public string CurrentRouteId;

        /// <summary>Current altitude in metres above sea level.</summary>
        public float AltitudeMetres;

        /// <summary>Current true airspeed in knots.</summary>
        public float SpeedKnots;

        /// <summary>Current magnetic heading in degrees (0–359).</summary>
        public float HeadingDeg;

        /// <summary>Active state in the behaviour state machine.</summary>
        public NPCBehaviorState BehaviorState;

        /// <summary>World-space position of the aircraft.</summary>
        public Vector3 WorldPosition;

        /// <summary>ICAO code of the origin airport.</summary>
        public string OriginICAO;

        /// <summary>ICAO code of the destination airport.</summary>
        public string DestinationICAO;

        /// <summary>Airline/operator name used for callsign generation.</summary>
        public string OperatorName;

        /// <summary>Whether this NPC is currently visible to the player.</summary>
        public bool IsVisible;

        /// <summary>Timestamp (game time) when this NPC was spawned.</summary>
        public float SpawnTime;
    }

    /// <summary>
    /// Per-category flight characteristics used by the spawn and AI controllers.
    /// </summary>
    [Serializable]
    public class NPCFlightProfile
    {
        /// <summary>Aircraft category this profile applies to.</summary>
        public NPCAircraftCategory Category;

        /// <summary>Typical cruise speed in knots.</summary>
        public float CruiseSpeedKnots;

        /// <summary>Minimum airspeed (approach/stall buffer) in knots.</summary>
        public float MinSpeedKnots;

        /// <summary>Maximum operating speed in knots.</summary>
        public float MaxSpeedKnots;

        /// <summary>Nominal cruise altitude in metres MSL.</summary>
        public float CruiseAltitudeMetres;

        /// <summary>Maximum service ceiling in metres MSL.</summary>
        public float MaxAltitudeMetres;

        /// <summary>Average climb rate in metres per second.</summary>
        public float ClimbRateMs;

        /// <summary>Average descent rate in metres per second.</summary>
        public float DescentRateMs;

        /// <summary>Maximum bank angle in degrees for turns.</summary>
        public float MaxBankDeg;

        /// <summary>Maximum heading change per second (deg/s) for navigation.</summary>
        public float TurnRateDegPerSec;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Configuration ScriptableObject
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 110 — Editor-configurable settings for the NPC Traffic system.
    /// Create an asset instance via <c>Assets → Create → SWEF → NPC Traffic Config</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/NPC Traffic Config", fileName = "NPCTrafficConfig")]
    public class NPCTrafficConfig : ScriptableObject
    {
        [Header("Density")]
        [Tooltip("Global traffic density preset.")]
        public NPCTrafficDensity Density = NPCTrafficDensity.Normal;

        [Tooltip("Maximum number of simultaneously active NPC aircraft (budget cap).")]
        [Range(0, 200)]
        public int MaxActiveNPCs = 50;

        [Tooltip("Radius in metres around the player within which NPCs are spawned.")]
        [Range(500f, 200000f)]
        public float SpawnRadiusMetres = 80000f;

        [Tooltip("Radius beyond which NPCs are despawned.")]
        [Range(500f, 250000f)]
        public float DespawnRadiusMetres = 100000f;

        [Header("LOD Distances")]
        [Tooltip("Distance in metres beyond which aircraft are rendered as icons.")]
        public float IconLODDistanceMetres = 50000f;

        [Tooltip("Distance in metres beyond which low-poly meshes are used.")]
        public float LowPolyLODDistanceMetres = 10000f;

        [Header("Time of Day")]
        [Tooltip("Traffic multiplier during rush hours (07-09 and 17-20).")]
        [Range(0.5f, 3f)]
        public float RushHourMultiplier = 1.8f;

        [Tooltip("Traffic multiplier during night hours (23-05).")]
        [Range(0.1f, 1f)]
        public float NightMultiplier = 0.3f;

        [Header("Spawn Timing")]
        [Tooltip("Seconds between spawn attempts.")]
        [Range(1f, 60f)]
        public float SpawnIntervalSeconds = 5f;

        [Tooltip("Seconds between NPC position update ticks.")]
        [Range(0.05f, 1f)]
        public float UpdateIntervalSeconds = 0.1f;

        [Header("Per-Category Flight Profiles")]
        [Tooltip("Flight performance data for each aircraft category.")]
        public List<NPCFlightProfile> FlightProfiles = new List<NPCFlightProfile>
        {
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.CommercialAirline,
                CruiseSpeedKnots      = 460f,
                MinSpeedKnots         = 130f,
                MaxSpeedKnots         = 520f,
                CruiseAltitudeMetres  = 10500f,
                MaxAltitudeMetres     = 12500f,
                ClimbRateMs           = 10f,
                DescentRateMs         = 6f,
                MaxBankDeg            = 25f,
                TurnRateDegPerSec     = 3f
            },
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.PrivateJet,
                CruiseSpeedKnots      = 400f,
                MinSpeedKnots         = 110f,
                MaxSpeedKnots         = 470f,
                CruiseAltitudeMetres  = 9000f,
                MaxAltitudeMetres     = 13700f,
                ClimbRateMs           = 12f,
                DescentRateMs         = 7f,
                MaxBankDeg            = 30f,
                TurnRateDegPerSec     = 4f
            },
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.CargoPlane,
                CruiseSpeedKnots      = 440f,
                MinSpeedKnots         = 140f,
                MaxSpeedKnots         = 490f,
                CruiseAltitudeMetres  = 9500f,
                MaxAltitudeMetres     = 11000f,
                ClimbRateMs           = 8f,
                DescentRateMs         = 5f,
                MaxBankDeg            = 20f,
                TurnRateDegPerSec     = 2f
            },
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.MilitaryAircraft,
                CruiseSpeedKnots      = 500f,
                MinSpeedKnots         = 150f,
                MaxSpeedKnots         = 600f,
                CruiseAltitudeMetres  = 8000f,
                MaxAltitudeMetres     = 15000f,
                ClimbRateMs           = 20f,
                DescentRateMs         = 15f,
                MaxBankDeg            = 60f,
                TurnRateDegPerSec     = 10f
            },
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.Helicopter,
                CruiseSpeedKnots      = 120f,
                MinSpeedKnots         = 0f,
                MaxSpeedKnots         = 160f,
                CruiseAltitudeMetres  = 600f,
                MaxAltitudeMetres     = 3000f,
                ClimbRateMs           = 5f,
                DescentRateMs         = 4f,
                MaxBankDeg            = 15f,
                TurnRateDegPerSec     = 15f
            },
            new NPCFlightProfile
            {
                Category              = NPCAircraftCategory.TrainingAircraft,
                CruiseSpeedKnots      = 100f,
                MinSpeedKnots         = 60f,
                MaxSpeedKnots         = 150f,
                CruiseAltitudeMetres  = 1000f,
                MaxAltitudeMetres     = 3000f,
                ClimbRateMs           = 3f,
                DescentRateMs         = 2f,
                MaxBankDeg            = 20f,
                TurnRateDegPerSec     = 5f
            }
        };
    }
}
