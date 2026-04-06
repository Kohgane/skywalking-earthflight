// OceanSystemData.cs — Phase 117: Advanced Ocean & Maritime System
// Enums and data models for the ocean and maritime simulation.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    // ── Ocean Region ──────────────────────────────────────────────────────────────

    /// <summary>Geographic classification of a water region.</summary>
    public enum OceanRegion
    {
        /// <summary>Deep open ocean far from land.</summary>
        OpenOcean,
        /// <summary>Shallow waters near shorelines.</summary>
        CoastalWater,
        /// <summary>Protected harbour basin.</summary>
        Harbor,
        /// <summary>Inland flowing water.</summary>
        River,
        /// <summary>Enclosed freshwater body.</summary>
        Lake,
        /// <summary>Polar sea with ice coverage.</summary>
        Arctic
    }

    // ── Sea State ─────────────────────────────────────────────────────────────────

    /// <summary>Beaufort-based sea state classification.</summary>
    public enum SeaState
    {
        /// <summary>Sea surface glassy or rippled (Beaufort 0–1).</summary>
        Calm,
        /// <summary>Small wavelets, no breaking crests (Beaufort 2–3).</summary>
        Slight,
        /// <summary>Moderate waves, some whitecaps (Beaufort 4–5).</summary>
        Moderate,
        /// <summary>Large waves, extensive whitecaps (Beaufort 6–7).</summary>
        Rough,
        /// <summary>Very high waves, heavy sea (Beaufort 8–9).</summary>
        VeryRough,
        /// <summary>Phenomenal seas, extreme conditions (Beaufort 10+).</summary>
        HighSeas
    }

    // ── Vessel Type ───────────────────────────────────────────────────────────────

    /// <summary>Classification of maritime vessel types.</summary>
    public enum VesselType
    {
        /// <summary>Large bulk cargo ship.</summary>
        CargoShip,
        /// <summary>Oil or liquid cargo tanker.</summary>
        Tanker,
        /// <summary>Naval aircraft carrier.</summary>
        AircraftCarrier,
        /// <summary>Naval destroyer or frigate.</summary>
        Destroyer,
        /// <summary>Wind-powered sailing vessel.</summary>
        Sailboat,
        /// <summary>Small commercial fishing vessel.</summary>
        FishingBoat,
        /// <summary>High-speed motor boat.</summary>
        Speedboat
    }

    // ── Water Landing Type ────────────────────────────────────────────────────────

    /// <summary>Mode of water landing operation.</summary>
    public enum WaterLandingType
    {
        /// <summary>Dedicated seaplane or float plane.</summary>
        Seaplane,
        /// <summary>Helicopter water landing or hover.</summary>
        Helicopter,
        /// <summary>Unplanned ditching due to emergency.</summary>
        Emergency
    }

    // ── Maritime Mission Type ─────────────────────────────────────────────────────

    /// <summary>Category of maritime mission.</summary>
    public enum MaritimeMissionType
    {
        /// <summary>Search and rescue of survivors at sea.</summary>
        SearchAndRescue,
        /// <summary>Cargo delivery to ships or platforms.</summary>
        CargoDelivery,
        /// <summary>Area surveillance and vessel identification.</summary>
        Patrol,
        /// <summary>Medical evacuation from vessel or offshore platform.</summary>
        Medevac,
        /// <summary>Aerial firefighting on ship or offshore platform.</summary>
        FireFighting,
        /// <summary>Military carrier-based operations.</summary>
        CarrierOperation
    }

    // ── Catapult Type ─────────────────────────────────────────────────────────────

    /// <summary>Carrier catapult launch system type.</summary>
    public enum CatapultType
    {
        /// <summary>Traditional steam-powered catapult (CATOBAR).</summary>
        Steam,
        /// <summary>Electromagnetic launch system (EMALS).</summary>
        Electromagnetic
    }

    // ── Landing Signal State ──────────────────────────────────────────────────────

    /// <summary>LSO meatball / glidepath indicator state.</summary>
    public enum GlidepathState
    {
        /// <summary>Aircraft on correct glidepath.</summary>
        OnGlidepath,
        /// <summary>Aircraft slightly high.</summary>
        SlightlyHigh,
        /// <summary>Aircraft significantly high — wave-off risk.</summary>
        High,
        /// <summary>Aircraft slightly low.</summary>
        SlightlyLow,
        /// <summary>Aircraft dangerously low — immediate wave-off.</summary>
        Low
    }

    // ── Search Pattern ────────────────────────────────────────────────────────────

    /// <summary>SAR search pattern type.</summary>
    public enum SearchPattern
    {
        /// <summary>Concentric expanding squares from datum.</summary>
        ExpandingSquare,
        /// <summary>Sector search sweeping around a datum.</summary>
        Sector,
        /// <summary>Parallel track creeping line pattern.</summary>
        ParallelTrack
    }

    // ── Debris Type ───────────────────────────────────────────────────────────────

    /// <summary>Type of floating ocean debris object.</summary>
    public enum DebrisType
    {
        /// <summary>Large ice formation.</summary>
        Iceberg,
        /// <summary>Drifting shipping container.</summary>
        Container,
        /// <summary>Surface oil contamination patch.</summary>
        OilSlick,
        /// <summary>Dense seaweed or kelp patch.</summary>
        SeaweedPatch,
        /// <summary>Wreckage or misc floating debris.</summary>
        Wreckage
    }

    // ── Data Classes ──────────────────────────────────────────────────────────────

    /// <summary>Snapshot of wave conditions at a given ocean location.</summary>
    [Serializable]
    public class WaveConditions
    {
        /// <summary>Significant wave height in metres.</summary>
        public float significantWaveHeight;
        /// <summary>Dominant wave period in seconds.</summary>
        public float dominantPeriod;
        /// <summary>Mean wave direction in degrees (0 = North).</summary>
        public float waveDirection;
        /// <summary>Current sea state classification.</summary>
        public SeaState seaState;
        /// <summary>Wind speed in m/s driving the sea state.</summary>
        public float windSpeed;
        /// <summary>Wind direction in degrees.</summary>
        public float windDirection;
    }

    /// <summary>Runtime data for a maritime vessel.</summary>
    [Serializable]
    public class VesselData
    {
        /// <summary>Unique vessel identifier.</summary>
        public string vesselId;
        /// <summary>Human-readable vessel name.</summary>
        public string vesselName;
        /// <summary>Classification of this vessel.</summary>
        public VesselType vesselType;
        /// <summary>Current world-space position.</summary>
        public Vector3 position;
        /// <summary>Current heading in degrees.</summary>
        public float heading;
        /// <summary>Current speed in knots.</summary>
        public float speedKnots;
        /// <summary>Destination port or waypoint name.</summary>
        public string destination;
        /// <summary>Whether this vessel is active in the simulation.</summary>
        public bool isActive;
    }

    /// <summary>Carrier deck slot state used by <see cref="CarrierDeckManager"/>.</summary>
    [Serializable]
    public class DeckSlotState
    {
        /// <summary>Slot index on the carrier deck.</summary>
        public int slotIndex;
        /// <summary>Whether this slot is currently occupied.</summary>
        public bool isOccupied;
        /// <summary>Identifier of aircraft occupying this slot, or empty.</summary>
        public string aircraftId;
        /// <summary>World position of the slot.</summary>
        public Vector3 worldPosition;
    }

    /// <summary>Data for a maritime SAR mission.</summary>
    [Serializable]
    public class SARMissionData
    {
        /// <summary>Unique mission identifier.</summary>
        public string missionId;
        /// <summary>Datum (last known position) of the distress.</summary>
        public Vector3 datumPosition;
        /// <summary>Number of survivors to locate.</summary>
        public int survivorCount;
        /// <summary>Number of survivors already rescued.</summary>
        public int rescuedCount;
        /// <summary>Search pattern to use.</summary>
        public SearchPattern searchPattern;
        /// <summary>Mission time limit in seconds (0 = unlimited).</summary>
        public float timeLimitSeconds;
        /// <summary>Whether the mission is currently active.</summary>
        public bool isActive;
    }

    /// <summary>Analytics record for a single water landing event.</summary>
    [Serializable]
    public class WaterLandingRecord
    {
        /// <summary>UTC timestamp of landing event.</summary>
        public DateTime timestamp;
        /// <summary>Type of water landing.</summary>
        public WaterLandingType landingType;
        /// <summary>Vertical speed at touchdown in m/s.</summary>
        public float touchdownVerticalSpeed;
        /// <summary>Horizontal speed at touchdown in m/s.</summary>
        public float touchdownHorizontalSpeed;
        /// <summary>Sea state at time of landing.</summary>
        public SeaState seaState;
        /// <summary>Whether the landing was successful.</summary>
        public bool success;
    }

    /// <summary>Analytics record for a carrier trap or bolter event.</summary>
    [Serializable]
    public class CarrierTrapRecord
    {
        /// <summary>UTC timestamp.</summary>
        public DateTime timestamp;
        /// <summary>Arrestor wire number engaged (1–4), or 0 for bolter.</summary>
        public int wireNumber;
        /// <summary>Whether the aircraft boltered (missed all wires).</summary>
        public bool wasBolter;
        /// <summary>Approach speed in knots.</summary>
        public float approachSpeedKnots;
        /// <summary>Glidepath state at touchdown.</summary>
        public GlidepathState glidepathState;
    }
}
