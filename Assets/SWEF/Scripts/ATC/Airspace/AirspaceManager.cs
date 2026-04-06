// AirspaceManager.cs — Phase 119: Advanced AI Traffic Control
// Airspace zones: Class A/B/C/D/E/G, TFRs, restricted areas, MOAs, ADIZ.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages airspace zones of all ICAO classes plus special-use
    /// areas.  Provides entry-permission queries used by route planning.
    /// </summary>
    public class AirspaceManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="AirspaceManager"/>.</summary>
        public static AirspaceManager Instance { get; private set; }

        // ── Airspace Zone ─────────────────────────────────────────────────────────

        /// <summary>A defined airspace zone.</summary>
        [Serializable]
        public class AirspaceZone
        {
            public string zoneId;
            public string name;
            public AirspaceClass airspaceClass;
            public Vector3 center;
            public float radiusNM;
            public int lowerLimitFt;
            public int upperLimitFt;
            public bool isActive;
            public bool requiresIFR;
            public bool requiresClearance;

            /// <summary>Returns true if the given position/altitude falls within the zone.</summary>
            public bool Contains(Vector3 pos, float altFt)
            {
                float distNM = Vector3.Distance(
                    new Vector3(pos.x, 0, pos.z),
                    new Vector3(center.x, 0, center.z)) / 1852f;
                return isActive
                    && distNM <= radiusNM
                    && altFt >= lowerLimitFt
                    && altFt <= upperLimitFt;
            }
        }

        private readonly List<AirspaceZone> _zones = new List<AirspaceZone>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SeedZones();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void SeedZones()
        {
            AddZone(new AirspaceZone { zoneId = "KLAX_B",   name = "Los Angeles Class B", airspaceClass = AirspaceClass.B, center = Vector3.zero, radiusNM = 20f, lowerLimitFt = 0,     upperLimitFt = 10000, isActive = true, requiresClearance = true });
            AddZone(new AirspaceZone { zoneId = "CLASS_A",  name = "Class A Airspace",    airspaceClass = AirspaceClass.A, center = Vector3.zero, radiusNM = 999f, lowerLimitFt = 18000, upperLimitFt = 60000, isActive = true, requiresIFR = true });
        }

        // ── Zone Management ───────────────────────────────────────────────────────

        /// <summary>Adds an airspace zone.</summary>
        public void AddZone(AirspaceZone zone) { if (zone != null) _zones.Add(zone); }

        /// <summary>Returns all zones that contain the given position and altitude.</summary>
        public List<AirspaceZone> GetZonesAt(Vector3 position, float altFt)
        {
            var result = new List<AirspaceZone>();
            foreach (var z in _zones)
                if (z.Contains(position, altFt)) result.Add(z);
            return result;
        }

        /// <summary>Returns whether a position/altitude requires an ATC clearance.</summary>
        public bool RequiresClearance(Vector3 position, float altFt)
        {
            foreach (var z in _zones)
                if (z.Contains(position, altFt) && z.requiresClearance) return true;
            return false;
        }

        /// <summary>Returns whether a position/altitude is within Class A (IFR required).</summary>
        public bool IsClassA(float altFt) => altFt >= 18000;

        /// <summary>Number of registered airspace zones.</summary>
        public int ZoneCount => _zones.Count;

        /// <summary>Activates or deactivates a zone by ID.</summary>
        public bool SetZoneActive(string zoneId, bool active)
        {
            var z = _zones.Find(x => x.zoneId == zoneId);
            if (z == null) return false;
            z.isActive = active;
            return true;
        }
    }
}
