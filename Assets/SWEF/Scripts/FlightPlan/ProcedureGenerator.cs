// ProcedureGenerator.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — Procedurally generates SID, STAR, approach, missed-approach, and
    /// holding-pattern waypoint sequences from runway and airport data.
    ///
    /// <para>When a full <see cref="NavigationDatabase"/> is present, published procedure
    /// entries are returned directly.  Otherwise, standard-pattern waypoints are computed
    /// from the runway heading and airport position.</para>
    ///
    /// <para>Integrates with <see cref="SWEF.Landing.AirportRegistry"/> and
    /// <c>SWEF.ATC.RunwayManager</c> where available.</para>
    /// </summary>
    public class ProcedureGenerator : MonoBehaviour
    {
        #region Inspector

        [Tooltip("Default SID/STAR waypoint altitude (ft) when no altitude constraint is published.")]
        public float defaultProcedureAltitudeFt = 3000f;

        [Tooltip("Horizontal separation between auto-generated STAR waypoints (nm).")]
        public float starWaypointSpacingNm = 10f;

        [Tooltip("Horizontal separation between auto-generated SID waypoints (nm).")]
        public float sidWaypointSpacingNm = 8f;

        [Tooltip("Approach glideslope distance (nm from threshold) for FAF/IF generation.")]
        public float approachFinalApproachFixNm = 5f;

        [Tooltip("Initial approach fix distance (nm) from the runway threshold.")]
        public float approachIAFNm = 20f;

        #endregion

        #region SID

        /// <summary>
        /// Returns the waypoint list for the named <paramref name="sidName"/> SID at
        /// <paramref name="airportId"/> runway <paramref name="runwayId"/>.
        ///
        /// <para>If a matching entry exists in <see cref="NavigationDatabase"/> it is returned
        /// directly.  Otherwise a generic procedural SID is generated.</para>
        /// </summary>
        public List<FlightPlanWaypoint> GenerateSID(string airportId, string runwayId, string sidName)
        {
            // Try database first
            if (NavigationDatabase.Instance != null)
            {
                var entries = NavigationDatabase.Instance.GetProceduresForAirport(airportId, ProcedureType.SID);
                foreach (var e in entries)
                    if (e.procedureName == sidName && (string.IsNullOrEmpty(e.runwayId) || e.runwayId == runwayId))
                        return new List<FlightPlanWaypoint>(e.waypoints);
            }

            // Procedural fallback
            var rwy = GetRunwayData(airportId, runwayId);
            double apLat = rwy.lat, apLon = rwy.lon;
            float  hdg   = rwy.heading;

            var wps = new List<FlightPlanWaypoint>();
            string prefix = $"{airportId}{runwayId}SID";

            float alt = defaultProcedureAltitudeFt;
            for (int i = 1; i <= 4; i++)
            {
                double lat, lon;
                ProjectPoint(apLat, apLon, hdg, sidWaypointSpacingNm * i, out lat, out lon);
                alt += FlightPlanConfig.SIDClimbRateFpm
                    * (sidWaypointSpacingNm / (FlightPlanConfig.DefaultCruiseSpeedKts * 0.5f / 60f)); // use ~half cruise speed for low-altitude SID climb
                wps.Add(new FlightPlanWaypoint
                {
                    waypointId    = $"{prefix}{i:00}",
                    name          = $"{prefix}{i:00}",
                    category      = WaypointCategory.SID,
                    latitude      = lat,
                    longitude     = lon,
                    altitude      = alt,
                    legType       = LegType.TrackToFix,
                    course        = hdg,
                    procedureName = sidName
                });
            }
            return wps;
        }

        #endregion

        #region STAR

        /// <summary>
        /// Returns the waypoint list for the named <paramref name="starName"/> STAR at
        /// <paramref name="airportId"/> runway <paramref name="runwayId"/>.
        /// </summary>
        public List<FlightPlanWaypoint> GenerateSTAR(string airportId, string runwayId, string starName)
        {
            if (NavigationDatabase.Instance != null)
            {
                var entries = NavigationDatabase.Instance.GetProceduresForAirport(airportId, ProcedureType.STAR);
                foreach (var e in entries)
                    if (e.procedureName == starName && (string.IsNullOrEmpty(e.runwayId) || e.runwayId == runwayId))
                        return new List<FlightPlanWaypoint>(e.waypoints);
            }

            var rwy  = GetRunwayData(airportId, runwayId);
            double apLat = rwy.lat, apLon = rwy.lon;
            float  hdg   = rwy.heading;
            float  inboundHdg = (hdg + 180f) % 360f; // inbound to runway

            var wps    = new List<FlightPlanWaypoint>();
            string prefix = $"{airportId}{runwayId}STAR";

            for (int i = 4; i >= 1; i--)
            {
                double lat, lon;
                float  dist = starWaypointSpacingNm * i;
                ProjectPoint(apLat, apLon, inboundHdg, dist, out lat, out lon);
                float alt = Mathf.Max(defaultProcedureAltitudeFt,
                                      dist / (FlightPlanConfig.DefaultCruiseSpeedKts / 60f)
                                      * FlightPlanConfig.DefaultDescentRateFpm);
                wps.Add(new FlightPlanWaypoint
                {
                    waypointId    = $"{prefix}{i:00}",
                    name          = $"{prefix}{i:00}",
                    category      = WaypointCategory.STAR,
                    latitude      = lat,
                    longitude     = lon,
                    altitude      = alt,
                    legType       = LegType.TrackToFix,
                    course        = hdg,
                    procedureName = starName
                });
            }
            return wps;
        }

        #endregion

        #region Approach

        /// <summary>
        /// Returns the waypoint list for the specified approach procedure at
        /// <paramref name="airportId"/> runway <paramref name="runwayId"/>.
        /// </summary>
        public List<FlightPlanWaypoint> GenerateApproach(string airportId, string runwayId, ProcedureType type)
        {
            if (NavigationDatabase.Instance != null)
            {
                var entries = NavigationDatabase.Instance.GetProceduresForAirport(airportId, type);
                foreach (var e in entries)
                    if (string.IsNullOrEmpty(e.runwayId) || e.runwayId == runwayId)
                        return new List<FlightPlanWaypoint>(e.waypoints);
            }

            // Generic RNAV approach
            var rwy  = GetRunwayData(airportId, runwayId);
            double apLat = rwy.lat, apLon = rwy.lon;
            float  hdg   = rwy.heading;
            float  inHdg = (hdg + 180f) % 360f;
            string prefix = $"{airportId}{runwayId}APPR";

            var wps = new List<FlightPlanWaypoint>();

            // IAF — 20 nm out
            {
                double lat, lon;
                ProjectPoint(apLat, apLon, inHdg, approachIAFNm, out lat, out lon);
                wps.Add(new FlightPlanWaypoint
                {
                    waypointId = $"{prefix}IAF", name = $"{prefix}IAF",
                    category   = WaypointCategory.Approach,
                    latitude   = lat, longitude = lon,
                    altitude   = 3000f, legType = LegType.TrackToFix, course = hdg,
                    procedureName = "RNAV"
                });
            }

            // FAF — 5 nm out
            {
                double lat, lon;
                ProjectPoint(apLat, apLon, inHdg, approachFinalApproachFixNm, out lat, out lon);
                wps.Add(new FlightPlanWaypoint
                {
                    waypointId = $"{prefix}FAF", name = $"{prefix}FAF",
                    category   = WaypointCategory.Approach,
                    latitude   = lat, longitude = lon,
                    altitude   = 1500f, legType = LegType.TrackToFix, course = hdg,
                    procedureName = "RNAV"
                });
            }

            // MAP — runway threshold
            wps.Add(new FlightPlanWaypoint
            {
                waypointId = $"{prefix}MAP", name = $"{prefix}MAP",
                category   = WaypointCategory.Approach,
                latitude   = apLat, longitude = apLon,
                altitude   = 0f, legType = LegType.TrackToFix, course = hdg,
                isFlyover  = true,
                procedureName = "RNAV"
            });

            return wps;
        }

        #endregion

        #region Holding Pattern

        /// <summary>
        /// Generates a four-waypoint racetrack holding pattern centred on
        /// (<paramref name="lat"/>, <paramref name="lon"/>) with the given inbound
        /// course and leg time.
        /// </summary>
        public List<FlightPlanWaypoint> GenerateHoldingPattern(double lat, double lon,
                                                               float inboundCourse,
                                                               float legTimeMin)
        {
            if (legTimeMin <= 0f) legTimeMin = FlightPlanConfig.DefaultHoldLegTimeMin;

            // Standard holding speed (kts) — use cruise or 200 kts, whichever is lower
            float speedKts = FlightPlanConfig.DefaultCruiseSpeedKts > 200f
                ? 200f
                : FlightPlanConfig.DefaultCruiseSpeedKts;
            float legNm    = speedKts * legTimeMin / 60f;

            float outbound = (inboundCourse + 180f) % 360f;
            float rightTurn90 = (inboundCourse + 90f) % 360f;

            double lat1, lon1, lat2, lon2, lat3, lon3;
            ProjectPoint(lat, lon, outbound,   legNm,    out lat1, out lon1); // abeam fix
            ProjectPoint(lat, lon, rightTurn90, legNm * 0.5f, out lat2, out lon2); // outbound-leg midpoint
            ProjectPoint(lat1, lon1, inboundCourse, legNm, out lat3, out lon3); // outbound fix

            var wps = new List<FlightPlanWaypoint>
            {
                BuildHoldWp("HOLD01", lat,  lon,  inboundCourse, legTimeMin, LegType.HoldingPattern),
                BuildHoldWp("HOLD02", lat3, lon3, outbound,      0f,         LegType.TrackToFix),
                BuildHoldWp("HOLD03", lat2, lon2, inboundCourse, 0f,         LegType.TrackToFix),
                BuildHoldWp("HOLD04", lat,  lon,  inboundCourse, 0f,         LegType.DirectTo)
            };
            return wps;
        }

        #endregion

        #region Missed Approach

        /// <summary>
        /// Generates a missed-approach procedure starting at the MAP for
        /// <paramref name="runwayId"/> at <paramref name="airportId"/>.
        /// </summary>
        public List<FlightPlanWaypoint> GenerateMissedApproach(string airportId, string runwayId)
        {
            if (NavigationDatabase.Instance != null)
            {
                var entries = NavigationDatabase.Instance.GetProceduresForAirport(
                    airportId, ProcedureType.MissedApproach);
                foreach (var e in entries)
                    if (string.IsNullOrEmpty(e.runwayId) || e.runwayId == runwayId)
                        return new List<FlightPlanWaypoint>(e.waypoints);
            }

            var rwy   = GetRunwayData(airportId, runwayId);
            double apLat = rwy.lat, apLon = rwy.lon;
            float  hdg  = rwy.heading;

            var wps = new List<FlightPlanWaypoint>();
            string prefix = $"{airportId}{runwayId}MA";

            // Climb straight ahead for 3 nm, then turn to holding fix
            double lat1, lon1;
            ProjectPoint(apLat, apLon, hdg, 3f, out lat1, out lon1);
            wps.Add(new FlightPlanWaypoint
            {
                waypointId    = $"{prefix}01", name = $"{prefix}01",
                category      = WaypointCategory.Missed,
                latitude      = lat1, longitude = lon1,
                altitude      = 3000f, legType = LegType.TrackToFix, course = hdg,
                procedureName = "MA"
            });

            // Holding fix at 6 nm
            double lat2, lon2;
            ProjectPoint(apLat, apLon, hdg, 6f, out lat2, out lon2);
            wps.Add(new FlightPlanWaypoint
            {
                waypointId    = $"{prefix}HOLD", name = $"{prefix}HOLD",
                category      = WaypointCategory.Missed,
                latitude      = lat2, longitude = lon2,
                altitude      = 4000f, legType = LegType.HoldingPattern,
                course        = (hdg + 180f) % 360f,
                holdingTime   = FlightPlanConfig.DefaultHoldLegTimeMin,
                procedureName = "MA"
            });

            return wps;
        }

        #endregion

        #region Private Helpers

        private struct RunwayInfo
        {
            public double lat, lon;
            public float  heading;
        }

        private RunwayInfo GetRunwayData(string airportId, string runwayId)
        {
            var info = new RunwayInfo();

            // Try AirportRegistry (Phase 68)
#if SWEF_LANDING_AVAILABLE
            var reg = FindFirstObjectByType<Landing.AirportRegistry>();
            if (reg != null)
            {
                var airport = reg.FindAirport(airportId);
                if (airport != null)
                {
                    info.lat = airport.latitude;
                    info.lon = airport.longitude;
                    foreach (var rwy in airport.runways)
                    {
                        if (rwy.designator == runwayId)
                        {
                            info.heading = rwy.heading;
                            break;
                        }
                    }
                    return info;
                }
            }
#endif

            // Try NavigationDatabase
            if (NavigationDatabase.Instance != null)
            {
                var entry = NavigationDatabase.Instance.FindById(airportId);
                if (entry != null)
                {
                    info.lat = entry.latitude;
                    info.lon = entry.longitude;
                }
            }

            // Parse runway heading from designator (e.g. "28L" → 280°)
            if (!string.IsNullOrEmpty(runwayId) && runwayId.Length >= 2)
            {
                string digits = string.Empty;
                foreach (char c in runwayId)
                    if (char.IsDigit(c)) digits += c;
                if (int.TryParse(digits, out int hdgTens))
                    info.heading = hdgTens * 10f;
            }
            return info;
        }

        /// <summary>
        /// Projects a point from (<paramref name="lat"/>, <paramref name="lon"/>)
        /// along <paramref name="bearing"/> degrees for <paramref name="distNm"/>
        /// nautical miles.
        /// </summary>
        private static void ProjectPoint(double lat, double lon, float bearing, float distNm,
                                         out double outLat, out double outLon)
        {
            const double R  = 3440.065; // nm
            double bRad     = bearing * System.Math.PI / 180.0;
            double latRad   = lat * System.Math.PI / 180.0;
            double lonRad   = lon * System.Math.PI / 180.0;
            double d        = distNm / R;

            double outLatRad = System.Math.Asin(
                System.Math.Sin(latRad) * System.Math.Cos(d)
                + System.Math.Cos(latRad) * System.Math.Sin(d) * System.Math.Cos(bRad));

            double outLonRad = lonRad + System.Math.Atan2(
                System.Math.Sin(bRad) * System.Math.Sin(d) * System.Math.Cos(latRad),
                System.Math.Cos(d) - System.Math.Sin(latRad) * System.Math.Sin(outLatRad));

            outLat = outLatRad * 180.0 / System.Math.PI;
            outLon = outLonRad * 180.0 / System.Math.PI;
        }

        private static FlightPlanWaypoint BuildHoldWp(string id, double lat, double lon,
                                                       float course, float holdTime, LegType leg)
        {
            return new FlightPlanWaypoint
            {
                waypointId   = id,
                name         = id,
                category     = WaypointCategory.GPS,
                latitude     = lat,
                longitude    = lon,
                legType      = leg,
                course       = course,
                holdingTime  = holdTime,
                isFlyover    = true
            };
        }

        #endregion
    }
}
