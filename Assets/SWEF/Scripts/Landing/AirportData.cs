// AirportData.cs — SWEF Landing & Airport System (Phase 68)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Landing
{
    /// <summary>
    /// Phase 68 — ScriptableObject defining a single airport or landing location.
    ///
    /// <para>Create instances via <c>Assets → Create → SWEF → Landing → Airport Data</c>.
    /// Register them with <see cref="AirportRegistry"/> at runtime.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Landing/Airport Data", fileName = "NewAirportData")]
    public class AirportData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique ICAO-style code, e.g. \"SWEF01\".")]
        public string airportId = "SWEF01";

        [Tooltip("Full display name of the airport.")]
        public string airportName = "New Airport";

        [Tooltip("City in which the airport is located.")]
        public string city = "";

        [Tooltip("Country in which the airport is located.")]
        public string country = "";

        // ── Classification ────────────────────────────────────────────────────

        [Header("Classification")]
        [Tooltip("Size category of the airport.")]
        public AirportSize size = AirportSize.Small;

        // ── Geography ─────────────────────────────────────────────────────────

        [Header("Geography")]
        [Tooltip("WGS-84 latitude in decimal degrees.")]
        public double latitude;

        [Tooltip("WGS-84 longitude in decimal degrees.")]
        public double longitude;

        [Tooltip("Airport elevation in meters above sea level.")]
        public float elevation;

        // ── Runways ───────────────────────────────────────────────────────────

        [Header("Runways")]
        [Tooltip("All runways at this airport.")]
        public List<RunwayData> runways = new List<RunwayData>();

        // ── Services ──────────────────────────────────────────────────────────

        [Header("Services")]
        [Tooltip("Whether a repair facility is available (links to RepairSystem — Phase 66).")]
        public bool hasRepairFacility = false;

        [Tooltip("Whether aircraft refuelling is available.")]
        public bool hasFuelStation = false;

        [Tooltip("Free-form list of service tags, e.g. \"hangar\", \"customs\", \"charter\".")]
        public List<string> services = new List<string>();

        // ── Radio ─────────────────────────────────────────────────────────────

        [Header("Radio")]
        [Tooltip("ILS approach frequency in MHz, e.g. 110.30.")]
        public float approachFrequency = 110.30f;

        // ── Visuals ───────────────────────────────────────────────────────────

        [Header("Visuals")]
        [Tooltip("Icon shown for this airport on the minimap or world map.")]
        public Sprite airportIcon;
    }
}
