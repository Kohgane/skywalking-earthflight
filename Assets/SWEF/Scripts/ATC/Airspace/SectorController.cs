// SectorController.cs — Phase 119: Advanced AI Traffic Control
// ATC sector management: sector boundaries, handoff procedures, sector load
// balancing.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages ATC sector boundaries, load balancing between sectors
    /// and inter-sector handoff procedures.
    /// </summary>
    public class SectorController : MonoBehaviour
    {
        // ── Sector ────────────────────────────────────────────────────────────────

        /// <summary>An ATC sector definition.</summary>
        public class ATCSector
        {
            public string sectorId;
            public string name;
            public string facilityId;
            public Bounds bounds;
            public int lowerLimitFt;
            public int upperLimitFt;
            public List<string> activeCallsigns = new List<string>();

            /// <summary>Current aircraft load in this sector.</summary>
            public int Load => activeCallsigns.Count;

            /// <summary>Returns true if the position and altitude fall within this sector.</summary>
            public bool Contains(Vector3 pos, float altFt)
                => bounds.Contains(pos) && altFt >= lowerLimitFt && altFt <= upperLimitFt;
        }

        private readonly Dictionary<string, ATCSector> _sectors = new Dictionary<string, ATCSector>();

        private void Awake()
        {
            SeedSectors();
        }

        private void SeedSectors()
        {
            RegisterSector(new ATCSector
            {
                sectorId   = "ZLA_01",
                name       = "LA Sector 01 (Low)",
                facilityId = "ZLA",
                bounds     = new Bounds(Vector3.zero, new Vector3(200000, 6000, 200000)),
                lowerLimitFt = 0,
                upperLimitFt = 18000
            });
            RegisterSector(new ATCSector
            {
                sectorId   = "ZLA_02",
                name       = "LA Sector 02 (High)",
                facilityId = "ZLA",
                bounds     = new Bounds(Vector3.zero, new Vector3(200000, 14000, 200000)),
                lowerLimitFt = 18000,
                upperLimitFt = 60000
            });
        }

        // ── Registration ──────────────────────────────────────────────────────────

        /// <summary>Registers a sector.</summary>
        public void RegisterSector(ATCSector sector)
        {
            if (sector != null) _sectors[sector.sectorId] = sector;
        }

        /// <summary>Returns the sector containing a given position and altitude, or null.</summary>
        public ATCSector GetSectorAt(Vector3 pos, float altFt)
        {
            foreach (var s in _sectors.Values)
                if (s.Contains(pos, altFt)) return s;
            return null;
        }

        // ── Load Management ───────────────────────────────────────────────────────

        /// <summary>Adds a callsign to the specified sector's load.</summary>
        public bool EnterSector(string sectorId, string callsign)
        {
            if (!_sectors.TryGetValue(sectorId, out var s)) return false;
            if (!s.activeCallsigns.Contains(callsign)) s.activeCallsigns.Add(callsign);
            return true;
        }

        /// <summary>Removes a callsign from the specified sector's load.</summary>
        public bool ExitSector(string sectorId, string callsign)
        {
            if (!_sectors.TryGetValue(sectorId, out var s)) return false;
            return s.activeCallsigns.Remove(callsign);
        }

        /// <summary>Returns the least-loaded sector in the given facility.</summary>
        public ATCSector GetLeastLoadedSector(string facilityId)
        {
            ATCSector best = null;
            foreach (var s in _sectors.Values)
            {
                if (s.facilityId != facilityId) continue;
                if (best == null || s.Load < best.Load) best = s;
            }
            return best;
        }

        /// <summary>Number of registered sectors.</summary>
        public int SectorCount => _sectors.Count;
    }
}
