using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    #region Enumerations

    /// <summary>Category of ATC facility providing air traffic services.</summary>
    public enum ATCFacilityType
    {
        Tower,
        Approach,
        Center,
        Ground,
        Departure,
        ATIS
    }

    /// <summary>Current phase of flight for the player aircraft.</summary>
    public enum FlightPhase
    {
        Parked,
        Taxi,
        Takeoff,
        Departure,
        Cruise,
        Approach,
        Landing,
        GoAround,
        Emergency
    }

    /// <summary>Type of ATC clearance instruction.</summary>
    public enum Clearance
    {
        Taxi,
        Takeoff,
        Landing,
        Approach,
        Altitude,
        Speed,
        Heading,
        Hold,
        GoAround
    }

    /// <summary>Operational status of a runway.</summary>
    public enum RunwayStatus
    {
        Active,
        Closed,
        Maintenance
    }

    #endregion

    #region Data Classes

    /// <summary>VHF radio frequency in the aviation band (118–136.975 MHz).</summary>
    [Serializable]
    public class RadioFrequency
    {
        [Tooltip("Frequency value in MHz, e.g. 118.1.")]
        public float valueMHz = 118.0f;

        [Tooltip("Human-readable name of the facility on this frequency.")]
        public string name = string.Empty;

        [Tooltip("ATC facility type that monitors this frequency.")]
        public ATCFacilityType facilityType = ATCFacilityType.Tower;

        /// <summary>Returns a formatted frequency string, e.g. \"118.100\".</summary>
        public override string ToString() => valueMHz.ToString("000.000");
    }

    /// <summary>
    /// A single ATC clearance instruction issued to the player aircraft.
    /// </summary>
    [Serializable]
    public class ATCInstruction
    {
        [Tooltip("Type of clearance being issued.")]
        public Clearance clearanceType = Clearance.Taxi;

        [Tooltip("Runway designator assigned, e.g. \"27L\". Empty if not applicable.")]
        public string assignedRunway = string.Empty;

        [Tooltip("Assigned altitude in feet MSL. Zero means no altitude assignment.")]
        public float assignedAltitude = 0f;

        [Tooltip("Assigned magnetic heading in degrees. Negative means no heading assignment.")]
        public float assignedHeading = -1f;

        [Tooltip("Assigned indicated airspeed in knots. Zero means no speed assignment.")]
        public float assignedSpeed = 0f;

        [Tooltip("Whether the aircraft must enter a published holding pattern.")]
        public bool holdingPattern = false;

        [Tooltip("World time (Time.time) at which this clearance expires. Zero means it does not expire.")]
        public float expirationTime = 0f;

        /// <summary>Returns true if the clearance has expired relative to current <see cref="Time.time"/>.</summary>
        public bool IsExpired => expirationTime > 0f && Time.time >= expirationTime;
    }

    /// <summary>Definition of a controlled or advisory airspace zone.</summary>
    [Serializable]
    public class AirspaceZone
    {
        [Tooltip("World-space center of the airspace cylinder.")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Horizontal radius of the zone in metres.")]
        public float radius = 5000f;

        [Tooltip("Lower altitude boundary of the zone in feet MSL.")]
        public float floorAltitude = 0f;

        [Tooltip("Upper altitude boundary of the zone in feet MSL.")]
        public float ceilingAltitude = 18000f;

        [Tooltip("Type of ATC facility managing this zone.")]
        public ATCFacilityType facilityType = ATCFacilityType.Tower;

        [Tooltip("Primary communications frequency for this zone.")]
        public RadioFrequency frequency = new RadioFrequency();

        [Tooltip("Human-readable name of this airspace zone.")]
        public string name = string.Empty;
    }

    /// <summary>Simulated AI traffic contact visible on the traffic radar scope.</summary>
    [Serializable]
    public class TrafficContact
    {
        [Tooltip("Aircraft callsign, e.g. \"SWR 123\".")]
        public string callsign = string.Empty;

        [Tooltip("World-space position of this contact.")]
        public Vector3 position = Vector3.zero;

        [Tooltip("Altitude in feet MSL.")]
        public float altitude = 5000f;

        [Tooltip("Groundspeed in knots.")]
        public float speed = 250f;

        [Tooltip("Magnetic heading in degrees.")]
        public float heading = 0f;

        [Tooltip("Current flight phase of this simulated aircraft.")]
        public FlightPhase flightPhase = FlightPhase.Cruise;

        [Tooltip("Runway designator assigned to this contact. Empty if not on approach/departure.")]
        public string assignedRunway = string.Empty;

        [Tooltip("Conflict threat level (0 = no threat, 1 = proximity advisory, 2 = resolution advisory).")]
        [Range(0, 2)]
        public int threatLevel = 0;
    }

    /// <summary>Physical and operational data for a single runway.</summary>
    [Serializable]
    public class RunwayInfo
    {
        [Tooltip("Runway designator, e.g. \"09L\".")]
        public string name = string.Empty;

        [Tooltip("World-space threshold position.")]
        public Vector3 position = Vector3.zero;

        [Tooltip("Runway magnetic heading in degrees.")]
        public float heading = 0f;

        [Tooltip("Runway length in metres.")]
        public float length = 3000f;

        [Tooltip("Runway width in metres.")]
        public float width = 45f;

        [Tooltip("Whether an ILS approach is available on this runway.")]
        public bool ILSAvailable = false;

        [Tooltip("Current operational status.")]
        public RunwayStatus status = RunwayStatus.Active;
    }

    /// <summary>Runtime configuration for the ATC subsystem.</summary>
    [Serializable]
    public class ATCSettings
    {
        [Tooltip("Maximum number of simultaneously simulated AI traffic contacts.")]
        [Range(0, 50)]
        public int maxSimulatedTraffic = 10;

        [Tooltip("Maximum range in metres over which radio communications are received.")]
        public float communicationRange = 370400f; // ~200 nm

        [Tooltip("Volume of radio audio (0–1).")]
        [Range(0f, 1f)]
        public float radioVolume = 0.8f;

        [Tooltip("When true, full ICAO standard phraseology is used; when false, simplified phrases are used.")]
        public bool realisticPhraseology = true;

        [Tooltip("When true, the radio automatically tunes to the appropriate ATC frequency when the player enters a new zone.")]
        public bool autoTuneFrequency = true;

        [Tooltip("Enable ATIS broadcast generation and display.")]
        public bool enableATIS = true;
    }

    #endregion
}
