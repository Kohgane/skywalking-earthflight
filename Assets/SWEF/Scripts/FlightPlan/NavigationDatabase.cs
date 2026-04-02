// NavigationDatabase.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightPlan
{
    // ── Supporting data classes ───────────────────────────────────────────────────

    /// <summary>A single navaid or intersection entry in the navigation database.</summary>
    [Serializable]
    public class NavaidEntry
    {
        [Tooltip("Unique ICAO/internal identifier, e.g. 'VOR-BKK', 'HELEN'.")]
        public string id = string.Empty;

        [Tooltip("Human-readable name.")]
        public string displayName = string.Empty;

        [Tooltip("Fix category (VOR, NDB, Intersection, Airport, GPS).")]
        public WaypointCategory category = WaypointCategory.VOR;

        [Tooltip("Latitude in decimal degrees (WGS-84).")]
        public double latitude;

        [Tooltip("Longitude in decimal degrees (WGS-84).")]
        public double longitude;

        [Tooltip("Navaid frequency in MHz (0 if not applicable).")]
        public float frequencyMHz;

        [Tooltip("ICAO airport code this navaid belongs to (if any).")]
        public string airportId = string.Empty;
    }

    /// <summary>A published instrument procedure (SID, STAR, approach) entry.</summary>
    [Serializable]
    public class ProcedureEntry
    {
        [Tooltip("Name of the procedure, e.g. 'NIKEL2B' or 'ILS RWY 28L'.")]
        public string procedureName = string.Empty;

        [Tooltip("ICAO code of the associated airport.")]
        public string airportId = string.Empty;

        [Tooltip("Runway designator this procedure serves, e.g. '28L'.")]
        public string runwayId = string.Empty;

        [Tooltip("Type of procedure (SID, STAR, Approach, etc.).")]
        public ProcedureType type = ProcedureType.SID;

        [Tooltip("Ordered list of waypoints that make up this procedure.")]
        public List<FlightPlanWaypoint> waypoints = new List<FlightPlanWaypoint>();
    }

    // ── NavigationDatabase MonoBehaviour ──────────────────────────────────────────

    /// <summary>
    /// Phase 87 — Singleton database of all navigation aids, intersections, and
    /// published instrument procedures (SID/STAR/Approach) available in the world.
    ///
    /// <para>Data is loaded from <c>Resources/FlightPlan/Navaids</c> and
    /// <c>Resources/FlightPlan/Procedures</c> JSON text assets or ScriptableObject
    /// lists on Awake.  Queries are available to any system via
    /// <see cref="Instance"/>.</para>
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public class NavigationDatabase : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static NavigationDatabase Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Navaid Database")]
        [Tooltip("All VOR/NDB/intersection entries. Populated from Resources/FlightPlan/Navaids or manually.")]
        public List<NavaidEntry> navaids = new List<NavaidEntry>();

        [Header("Procedure Database")]
        [Tooltip("All SID/STAR/approach procedure entries.")]
        public List<ProcedureEntry> procedures = new List<ProcedureEntry>();

        [Header("Resource Loading")]
        [Tooltip("If true, loads additional entries from Resources/FlightPlan/Navaids*.json on Awake.")]
        public bool loadFromResources = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadFromResources)
                LoadFromResources();

            Debug.Log($"[SWEF] NavigationDatabase: ready — {navaids.Count} navaids, {procedures.Count} procedures.");
        }

        #endregion

        #region Resource Loading

        private void LoadFromResources()
        {
            // Navaids JSON bundles
            var navaidAssets = Resources.LoadAll<TextAsset>("FlightPlan/Navaids");
            foreach (var asset in navaidAssets)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<NavaidListWrapper>(asset.text);
                    if (wrapper?.entries != null)
                        navaids.AddRange(wrapper.entries);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] NavigationDatabase: failed to parse {asset.name}: {ex.Message}");
                }
            }

            // Procedure JSON bundles
            var procedureAssets = Resources.LoadAll<TextAsset>("FlightPlan/Procedures");
            foreach (var asset in procedureAssets)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<ProcedureListWrapper>(asset.text);
                    if (wrapper?.entries != null)
                        procedures.AddRange(wrapper.entries);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] NavigationDatabase: failed to parse {asset.name}: {ex.Message}");
                }
            }
        }

        [Serializable] private class NavaidListWrapper    { public List<NavaidEntry>    entries; }
        [Serializable] private class ProcedureListWrapper { public List<ProcedureEntry> entries; }

        #endregion

        #region Public Query API

        /// <summary>
        /// Finds the nearest navaid to <paramref name="lat"/>/<paramref name="lon"/>
        /// of the specified <paramref name="category"/>.  Returns <c>null</c> if the
        /// database is empty or no match exists for that category.
        /// </summary>
        public NavaidEntry FindNearest(double lat, double lon, WaypointCategory category)
        {
            NavaidEntry nearest = null;
            double bestDist = double.MaxValue;

            foreach (var nav in navaids)
            {
                if (nav.category != category) continue;
                double d = HaversineNm(lat, lon, nav.latitude, nav.longitude);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest  = nav;
                }
            }
            return nearest;
        }

        /// <summary>Looks up a navaid by its unique <paramref name="id"/>.</summary>
        public NavaidEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var nav in navaids)
                if (string.Equals(nav.id, id, StringComparison.OrdinalIgnoreCase))
                    return nav;
            return null;
        }

        /// <summary>
        /// Returns all procedures for <paramref name="airportId"/> of the given
        /// <paramref name="type"/>.
        /// </summary>
        public List<ProcedureEntry> GetProceduresForAirport(string airportId, ProcedureType type)
        {
            var result = new List<ProcedureEntry>();
            if (string.IsNullOrEmpty(airportId)) return result;

            foreach (var proc in procedures)
                if (string.Equals(proc.airportId, airportId, StringComparison.OrdinalIgnoreCase)
                    && proc.type == type)
                    result.Add(proc);

            return result;
        }

        /// <summary>
        /// Returns all navaids whose position falls within a corridor of width
        /// <paramref name="corridorWidthNm"/> nautical miles on either side of the
        /// great-circle track from (<paramref name="lat1"/>, <paramref name="lon1"/>)
        /// to (<paramref name="lat2"/>, <paramref name="lon2"/>).
        /// </summary>
        public List<NavaidEntry> FindAlongRoute(double lat1, double lon1,
                                                double lat2, double lon2,
                                                float  corridorWidthNm)
        {
            var result = new List<NavaidEntry>();
            foreach (var nav in navaids)
            {
                double xtk = CrossTrackDistanceNm(lat1, lon1, lat2, lon2,
                                                  nav.latitude, nav.longitude);
                if (Math.Abs(xtk) <= corridorWidthNm)
                    result.Add(nav);
            }
            return result;
        }

        #endregion

        #region Geodesic Helpers

        /// <summary>Haversine great-circle distance in nautical miles.</summary>
        public static double HaversineNm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R  = 3440.065; // Earth radius in nautical miles
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a    = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                        * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }

        /// <summary>
        /// Signed cross-track distance in nautical miles of <paramref name="lat3"/>/<paramref name="lon3"/>
        /// from the great-circle track defined by the two route endpoints.
        /// </summary>
        private static double CrossTrackDistanceNm(double lat1, double lon1,
                                                   double lat2, double lon2,
                                                   double lat3, double lon3)
        {
            const double R = 3440.065;
            double d13  = HaversineNm(lat1, lon1, lat3, lon3) / R;
            double brg12 = BearingRad(lat1, lon1, lat2, lon2);
            double brg13 = BearingRad(lat1, lon1, lat3, lon3);
            return Math.Asin(Math.Sin(d13) * Math.Sin(brg13 - brg12)) * R;
        }

        private static double BearingRad(double lat1, double lon1, double lat2, double lon2)
        {
            double la1 = lat1 * Math.PI / 180.0;
            double la2 = lat2 * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double y    = Math.Sin(dLon) * Math.Cos(la2);
            double x    = Math.Cos(la1) * Math.Sin(la2) - Math.Sin(la1) * Math.Cos(la2) * Math.Cos(dLon);
            return Math.Atan2(y, x);
        }

        #endregion
    }
}
