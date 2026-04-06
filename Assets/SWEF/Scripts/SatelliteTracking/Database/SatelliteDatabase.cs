// SatelliteDatabase.cs — Phase 114: Satellite & Space Debris Tracking
// Catalog of known satellites: name, NORAD ID, orbit parameters, type, country, launch date.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// In-memory catalogue of known satellite records. Provides lookup, filtering, and
    /// built-in seed data for well-known satellites (ISS, GPS, GLONASS, etc.).
    /// </summary>
    public class SatelliteDatabase : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static SatelliteDatabase Instance { get; private set; }

        // ── Private catalogue ─────────────────────────────────────────────────────
        private readonly Dictionary<int, SatelliteRecord> _catalogue
            = new Dictionary<int, SatelliteRecord>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SeedBuiltInSatellites();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Adds or updates a satellite record in the catalogue.</summary>
        public void Upsert(SatelliteRecord record)
        {
            if (record == null) return;
            _catalogue[record.noradId] = record;
        }

        /// <summary>Returns all records in the catalogue.</summary>
        public IReadOnlyList<SatelliteRecord> GetAll()
            => new List<SatelliteRecord>(_catalogue.Values);

        /// <summary>Looks up a record by NORAD ID. Returns null if not found.</summary>
        public SatelliteRecord FindByNoradId(int noradId)
        {
            _catalogue.TryGetValue(noradId, out var rec);
            return rec;
        }

        /// <summary>Returns all satellites of the specified type.</summary>
        public IEnumerable<SatelliteRecord> GetByType(SatelliteType type)
            => _catalogue.Values.Where(r => r.satelliteType == type);

        /// <summary>Returns all satellites in the specified orbital regime.</summary>
        public IEnumerable<SatelliteRecord> GetByOrbit(OrbitType orbit)
            => _catalogue.Values.Where(r => r.orbitType == orbit);

        /// <summary>Returns all satellites from the specified country (ISO or name).</summary>
        public IEnumerable<SatelliteRecord> GetByCountry(string country)
            => _catalogue.Values.Where(r =>
                string.Equals(r.country, country, StringComparison.OrdinalIgnoreCase));

        /// <summary>Returns all active satellites.</summary>
        public IEnumerable<SatelliteRecord> GetActive()
            => _catalogue.Values.Where(r => r.status == SatelliteStatus.Active);

        /// <summary>Returns the total number of records.</summary>
        public int Count => _catalogue.Count;

        /// <summary>Removes all records from the catalogue.</summary>
        public void Clear() => _catalogue.Clear();

        // ── Seed data ─────────────────────────────────────────────────────────────

        private void SeedBuiltInSatellites()
        {
            // International Space Station
            Upsert(new SatelliteRecord
            {
                name           = "ISS (ZARYA)",
                noradId        = 25544,
                satelliteType  = SatelliteType.SpaceStation,
                orbitType      = OrbitType.LEO,
                status         = SatelliteStatus.Active,
                country        = "International",
                launchDate     = new DateTime(1998, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                visualMagnitude = -4.0f
            });

            // Hubble Space Telescope
            Upsert(new SatelliteRecord
            {
                name           = "HST",
                noradId        = 20580,
                satelliteType  = SatelliteType.Science,
                orbitType      = OrbitType.LEO,
                status         = SatelliteStatus.Active,
                country        = "USA",
                launchDate     = new DateTime(1990, 4, 24, 0, 0, 0, DateTimeKind.Utc),
                visualMagnitude = 2.0f
            });

            // GPS Block IIR (representative)
            Upsert(new SatelliteRecord
            {
                name           = "GPS BIIR-2  (PRN 13)",
                noradId        = 24876,
                satelliteType  = SatelliteType.Navigation,
                orbitType      = OrbitType.MEO,
                status         = SatelliteStatus.Active,
                country        = "USA",
                launchDate     = new DateTime(1997, 7, 23, 0, 0, 0, DateTimeKind.Utc),
                visualMagnitude = 7.0f
            });

            // NOAA-18 (weather)
            Upsert(new SatelliteRecord
            {
                name           = "NOAA 18",
                noradId        = 28654,
                satelliteType  = SatelliteType.Weather,
                orbitType      = OrbitType.SSO,
                status         = SatelliteStatus.Active,
                country        = "USA",
                launchDate     = new DateTime(2005, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                visualMagnitude = 3.5f
            });

            // Fengyun-1C debris (representative)
            Upsert(new SatelliteRecord
            {
                name           = "FENGYUN 1C DEB",
                noradId        = 29228,
                satelliteType  = SatelliteType.Debris,
                orbitType      = OrbitType.SSO,
                status         = SatelliteStatus.Failed,
                country        = "China",
                launchDate     = new DateTime(2007, 1, 11, 0, 0, 0, DateTimeKind.Utc),
                visualMagnitude = 9.0f
            });
        }
    }
}
