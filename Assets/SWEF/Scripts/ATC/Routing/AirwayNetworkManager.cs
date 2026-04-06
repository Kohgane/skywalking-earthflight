// AirwayNetworkManager.cs — Phase 119: Advanced AI Traffic Control
// Airway network: waypoints, airways (Victor, Jet), SIDs, STARs, approach procedures.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Singleton managing the airway network: named airways,
    /// waypoints, SIDs, STARs and approach procedures.
    /// </summary>
    public class AirwayNetworkManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="AirwayNetworkManager"/>.</summary>
        public static AirwayNetworkManager Instance { get; private set; }

        // ── Airway ────────────────────────────────────────────────────────────────

        /// <summary>A named airway consisting of ordered waypoints.</summary>
        public class Airway
        {
            public string name;
            public AirwayType type;
            public List<Waypoint> waypoints = new List<Waypoint>();
        }

        // ── Airway Type ───────────────────────────────────────────────────────────

        /// <summary>Classification of airway.</summary>
        public enum AirwayType
        {
            /// <summary>Victor airway — low altitude VOR-based.</summary>
            Victor,
            /// <summary>Jet airway — high altitude IFR.</summary>
            Jet,
            /// <summary>Standard Instrument Departure.</summary>
            SID,
            /// <summary>Standard Terminal Arrival Route.</summary>
            STAR,
            /// <summary>Instrument approach procedure.</summary>
            Approach
        }

        // ── Fields ────────────────────────────────────────────────────────────────

        private readonly Dictionary<string, Waypoint> _waypoints = new Dictionary<string, Waypoint>();
        private readonly Dictionary<string, Airway> _airways = new Dictionary<string, Airway>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SeedNetwork();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ── Seeding ───────────────────────────────────────────────────────────────

        private void SeedNetwork()
        {
            AddWaypoint(new Waypoint("KLAX",  Vector3.zero,                   WaypointType.Airport));
            AddWaypoint(new Waypoint("SLI",   new Vector3( 50000, 0,  10000), WaypointType.VOR));
            AddWaypoint(new Waypoint("BAYJAY",new Vector3(100000, 0,  30000), WaypointType.Intersection));
            AddWaypoint(new Waypoint("DARTS", new Vector3(150000, 0,  60000), WaypointType.Intersection));
            AddWaypoint(new Waypoint("KJFK",  new Vector3(400000, 0, 300000), WaypointType.Airport));

            var j80 = new Airway { name = "J80", type = AirwayType.Jet };
            j80.waypoints.Add(GetWaypoint("SLI"));
            j80.waypoints.Add(GetWaypoint("BAYJAY"));
            j80.waypoints.Add(GetWaypoint("DARTS"));
            _airways["J80"] = j80;
        }

        // ── Waypoints ─────────────────────────────────────────────────────────────

        /// <summary>Registers a waypoint in the network.</summary>
        public void AddWaypoint(Waypoint wp) { if (wp != null) _waypoints[wp.identifier] = wp; }

        /// <summary>Returns a waypoint by identifier, or null.</summary>
        public Waypoint GetWaypoint(string id)
        {
            _waypoints.TryGetValue(id, out var wp);
            return wp;
        }

        /// <summary>Number of registered waypoints.</summary>
        public int WaypointCount => _waypoints.Count;

        // ── Airways ───────────────────────────────────────────────────────────────

        /// <summary>Registers an airway.</summary>
        public void AddAirway(Airway airway) { if (airway != null) _airways[airway.name] = airway; }

        /// <summary>Returns an airway by name, or null.</summary>
        public Airway GetAirway(string name)
        {
            _airways.TryGetValue(name, out var a);
            return a;
        }

        /// <summary>Number of registered airways.</summary>
        public int AirwayCount => _airways.Count;

        /// <summary>Returns all airways of the given type.</summary>
        public List<Airway> GetAirwaysByType(AirwayType type)
        {
            var result = new List<Airway>();
            foreach (var a in _airways.Values)
                if (a.type == type) result.Add(a);
            return result;
        }
    }
}
