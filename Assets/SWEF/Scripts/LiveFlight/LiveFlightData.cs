// LiveFlightData.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System;
using UnityEngine;

namespace SWEF.LiveFlight
{
    // ── Data source enum ──────────────────────────────────────────────────────────

    /// <summary>Selects the live flight data provider or mock mode.</summary>
    public enum LiveFlightDataSource
    {
        /// <summary>OpenSky Network REST API (free, registration required for higher rate limits).</summary>
        OpenSky,

        /// <summary>ADS-B Exchange API (commercial, requires API key).</summary>
        ADS_B_Exchange,

        /// <summary>Locally-generated mock aircraft — no network or API key required.</summary>
        Mock
    }

    // ── LiveAircraftInfo ──────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of a single aircraft's state at a given moment, as reported by
    /// the live data source.
    /// </summary>
    [Serializable]
    public struct LiveAircraftInfo
    {
        /// <summary>24-bit ICAO transponder address in hex (e.g. "abc123").</summary>
        public string icao24;

        /// <summary>Flight callsign as broadcast by the transponder (may be empty).</summary>
        public string callsign;

        /// <summary>WGS-84 latitude in decimal degrees.</summary>
        public double latitude;

        /// <summary>WGS-84 longitude in decimal degrees.</summary>
        public double longitude;

        /// <summary>Barometric altitude in metres above mean sea level.</summary>
        public float altitude;

        /// <summary>Ground speed in metres per second.</summary>
        public float velocity;

        /// <summary>True track in degrees clockwise from north (0–360).</summary>
        public float heading;

        /// <summary>Vertical rate in metres per second; positive = climbing.</summary>
        public float verticalRate;

        /// <summary><c>true</c> when the aircraft is on the ground.</summary>
        public bool onGround;

        /// <summary>Unix timestamp (seconds) of the last position report.</summary>
        public long lastUpdate;

        /// <summary>Country of origin as reported by the registry.</summary>
        public string originCountry;

        /// <summary>ICAO aircraft type designator (e.g. "B737", "A320").</summary>
        public string aircraftType;
    }

    // ── FlightRoute ───────────────────────────────────────────────────────────────

    /// <summary>Planned or inferred route for a single flight.</summary>
    [Serializable]
    public struct FlightRoute
    {
        /// <summary>ICAO airport code of the departure airport.</summary>
        public string departureICAO;

        /// <summary>ICAO airport code of the arrival airport.</summary>
        public string arrivalICAO;

        /// <summary>
        /// Sampled world-space waypoints along the route (Unity units / Cesium coordinates).
        /// </summary>
        public Vector3[] waypoints;

        /// <summary>Estimated time of arrival (UTC).</summary>
        public DateTime estimatedArrival;
    }

    // ── LiveFlightConfig ──────────────────────────────────────────────────────────

    /// <summary>
    /// ScriptableObject that holds all user-facing configuration for the Live Flight
    /// Tracking system.  Create via <em>Assets → Create → SWEF/LiveFlight/Config</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/LiveFlight/Config", fileName = "LiveFlightConfig")]
    public class LiveFlightConfig : ScriptableObject
    {
        [Header("Data Source")]
        [Tooltip("Which live data API to use, or Mock for Editor testing.")]
        public LiveFlightDataSource apiProvider = LiveFlightDataSource.Mock;

        [Tooltip("Base URL of the selected REST API.")]
        public string apiUrl = "https://opensky-network.org/api";

        [Tooltip("API key / bearer token (leave empty for OpenSky anonymous access).")]
        public string apiKey = "";

        [Header("Polling")]
        [Tooltip("Seconds between successive data fetches.  Minimum 5 s to respect API quotas.")]
        [Min(5f)]
        public float pollIntervalSeconds = 10f;

        [Header("Display")]
        [Tooltip("Maximum number of aircraft to render simultaneously.")]
        [Min(1)]
        public int maxAircraftDisplayed = 100;

        [Tooltip("Radius (km) around the player camera within which aircraft are displayed.")]
        [Min(1f)]
        public float displayRadiusKm = 500f;

        [Tooltip("Draw a line from each aircraft's current position to its planned route.")]
        public bool showRouteLines = true;

        [Tooltip("Show callsign / altitude / speed labels above each marker.")]
        public bool showLabels = true;

        [Header("Visuals")]
        [Tooltip("Gradient used to colour-code aircraft by altitude (left = low, right = high).")]
        public Gradient altitudeColorGradient = new Gradient();

        [Tooltip("World-space scale factor applied to aircraft icon GameObjects.")]
        [Min(0.01f)]
        public float iconScale = 1f;
    }
}
