// SpaceStationData.cs — SWEF Space Station & Orbital Docking System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SpaceStation
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Identifies a specific orbital body / station type.</summary>
    public enum OrbitalBody
    {
        ISS,
        CustomStation,
        SpaceHotel,
        ResearchLab,
        FuelDepot,
        Observatory
    }

    /// <summary>Operational state of a docking port.</summary>
    public enum DockingPortState
    {
        Available,
        Occupied,
        Damaged,
        Locked
    }

    /// <summary>Segment type for a station module.</summary>
    public enum StationSegmentType
    {
        Habitat,
        Laboratory,
        Solar,
        Docking,
        Storage,
        Observation,
        Medical,
        Command
    }

    /// <summary>Phase of the 6-step docking approach sequence.</summary>
    public enum DockingApproachPhase
    {
        FreeApproach,
        InitialAlignment,
        FinalApproach,
        SoftCapture,
        HardDock,
        Docked
    }

    // ── Structs ───────────────────────────────────────────────────────────────────

    /// <summary>Keplerian orbital parameters for a station.</summary>
    [Serializable]
    public struct OrbitalParameters
    {
        /// <summary>Orbit altitude above Earth surface in metres.</summary>
        public double altitude;

        /// <summary>Orbital inclination in degrees.</summary>
        public double inclination;

        /// <summary>Orbit eccentricity (0 = circular).</summary>
        public double eccentricity;

        /// <summary>Orbital period in seconds.</summary>
        public double period;

        /// <summary>True anomaly in degrees at epoch.</summary>
        public double trueAnomaly;

        /// <summary>Returns parameters for a circular equatorial orbit at the given altitude.</summary>
        public static OrbitalParameters Circular(double altitudeMetres)
        {
            const double mu = 3.986004418e14; // Earth GM (m^3/s^2)
            const double earthRadius = 6_371_000.0;
            double r = earthRadius + altitudeMetres;
            double period = 2.0 * Math.PI * Math.Sqrt(r * r * r / mu);
            return new OrbitalParameters
            {
                altitude     = altitudeMetres,
                inclination  = 0.0,
                eccentricity = 0.0,
                period       = period,
                trueAnomaly  = 0.0
            };
        }
    }

    // ── Classes ───────────────────────────────────────────────────────────────────

    /// <summary>Definition of a single docking port on a station.</summary>
    [Serializable]
    public class DockingPortDefinition
    {
        [Tooltip("Unique port identifier within the station (e.g. 'port_fwd').")]
        public string portId = string.Empty;

        [Tooltip("Port position relative to the station root transform.")]
        public Vector3 localPosition = Vector3.zero;

        [Tooltip("Port orientation relative to the station root transform.")]
        public Quaternion localRotation = Quaternion.identity;

        [Tooltip("Ship size tags accepted by this port (e.g. 'Small', 'Medium').")]
        public string[] acceptedShipSizes = Array.Empty<string>();

        [Tooltip("Current operational state of this port.")]
        public DockingPortState state = DockingPortState.Available;
    }

    /// <summary>Full definition of one space station instance.</summary>
    [Serializable]
    public class StationDefinition
    {
        [Tooltip("Unique station identifier (e.g. 'iss_primary').")]
        public string stationId = string.Empty;

        [Tooltip("Localization key for the display name.")]
        public string displayNameLocKey = string.Empty;

        [Tooltip("Orbital parameters for this station.")]
        public OrbitalParameters orbitalParams;

        [Tooltip("Ordered list of station segment types in this definition.")]
        public StationSegmentType[] segments = Array.Empty<StationSegmentType>();

        [Tooltip("Docking ports available on this station.")]
        public DockingPortDefinition[] dockingPorts = Array.Empty<DockingPortDefinition>();

        [Tooltip("Resource path to the station prefab (relative to Resources/).")]
        public string modelPrefabPath = string.Empty;
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// Project-wide configuration for the Space Station system.
    /// Create via <c>Assets → Create → SWEF → Space Station Config</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Space Station Config", fileName = "SpaceStationConfig")]
    public class SpaceStationConfig : ScriptableObject
    {
        [Header("Docking")]
        [Tooltip("Radius in metres within which a soft-capture lock can be triggered.")]
        [Range(0.1f, 20f)]
        public float dockingCaptureRadius = 5f;

        [Tooltip("Maximum angular deviation (degrees) for alignment to count as valid.")]
        [Range(0.5f, 30f)]
        public float alignmentToleranceDeg = 5f;

        [Tooltip("Maximum closing speed (m/s) allowed during the FinalApproach phase.")]
        [Range(0.1f, 20f)]
        public float approachSpeedLimit = 5f;

        [Tooltip("Force (N) applied by a single RCS thruster burst.")]
        [Range(1f, 500f)]
        public float rcsForce = 100f;

        [Header("Stations")]
        [Tooltip("Default altitude (m) at which new stations are spawned.")]
        [Range(100_000f, 2_000_000f)]
        public float stationSpawnAltitude = 400_000f;

        [Tooltip("Maximum number of simultaneously active station instances.")]
        [Range(1, 10)]
        public int maxActiveStations = 3;
    }
}
