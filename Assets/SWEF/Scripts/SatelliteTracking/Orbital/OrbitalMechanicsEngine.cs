// OrbitalMechanicsEngine.cs — Phase 114: Satellite & Space Debris Tracking
// Keplerian orbit calculation and coordinate conversions.
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Performs Keplerian orbital mechanics calculations: propagation from TLE data,
    /// ECI ↔ ECEF ↔ geodetic coordinate conversions, and ground-track computation.
    /// </summary>
    public class OrbitalMechanicsEngine : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Earth's gravitational parameter μ (km³/s²).</summary>
        public const double MuEarth = 398600.4418;

        /// <summary>Earth mean radius (km).</summary>
        public const double EarthRadiusKm = 6371.0;

        /// <summary>Earth's angular rotation rate (rad/s).</summary>
        public const double EarthOmegaRad = 7.2921150e-5;

        /// <summary>Seconds per day.</summary>
        public const double SecondsPerDay = 86400.0;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Propagates the satellite from its TLE epoch to the given UTC time and
        /// returns the resulting orbital state using a two-body Keplerian model.
        /// </summary>
        public OrbitalState Propagate(TLEData tle, DateTime utcTime)
        {
            if (tle == null) return null;

            double tSinceEpochMin = (utcTime - JulianToDateTime(tle.epochJulian)).TotalMinutes;

            // Keplerian elements (convert to radians)
            double incRad  = tle.inclinationDeg   * Mathf.Deg2Rad;
            double raanRad = tle.raanDeg           * Mathf.Deg2Rad;
            double argPRad = tle.argOfPerigeeDeg   * Mathf.Deg2Rad;
            double ecc     = tle.eccentricity;

            // Semi-major axis from mean motion (revolutions/day → rad/s)
            double n = tle.meanMotionRevPerDay * 2.0 * Math.PI / SecondsPerDay;
            double sma = Math.Pow(MuEarth / (n * n), 1.0 / 3.0);

            // Propagate mean anomaly
            double m0  = tle.meanAnomalyDeg * Mathf.Deg2Rad;
            double m   = m0 + n * tSinceEpochMin * 60.0;
            m = NormaliseAngle(m);

            // Solve Kepler's equation: M = E - e*sin(E)
            double eccAnom = SolveKepler(m, ecc);

            // True anomaly
            double sinE = Math.Sin(eccAnom);
            double cosE = Math.Cos(eccAnom);
            double nu = Math.Atan2(Math.Sqrt(1.0 - ecc * ecc) * sinE, cosE - ecc);

            // Distance
            double r = sma * (1.0 - ecc * cosE);

            // Position in perifocal frame
            double xPerif = r * Math.Cos(nu);
            double yPerif = r * Math.Sin(nu);

            // Rotation matrices: perifocal → ECI
            double cosRaan = Math.Cos(raanRad);
            double sinRaan = Math.Sin(raanRad);
            double cosInc  = Math.Cos(incRad);
            double sinInc  = Math.Sin(incRad);
            double cosArgP = Math.Cos(argPRad);
            double sinArgP = Math.Sin(argPRad);

            double x = (cosRaan * cosArgP - sinRaan * sinArgP * cosInc) * xPerif
                     + (-cosRaan * sinArgP - sinRaan * cosArgP * cosInc) * yPerif;
            double y = (sinRaan * cosArgP + cosRaan * sinArgP * cosInc) * xPerif
                     + (-sinRaan * sinArgP + cosRaan * cosArgP * cosInc) * yPerif;
            double z = (sinArgP * sinInc) * xPerif + (cosArgP * sinInc) * yPerif;

            // Velocity in perifocal frame
            double p = sma * (1.0 - ecc * ecc);
            double sqrtMuP = Math.Sqrt(MuEarth / p);
            double vxPerif = -sqrtMuP * Math.Sin(nu);
            double vyPerif =  sqrtMuP * (ecc + Math.Cos(nu));

            double vx = (cosRaan * cosArgP - sinRaan * sinArgP * cosInc) * vxPerif
                      + (-cosRaan * sinArgP - sinRaan * cosArgP * cosInc) * vyPerif;
            double vy = (sinRaan * cosArgP + cosRaan * sinArgP * cosInc) * vxPerif
                      + (-sinRaan * sinArgP + cosRaan * cosArgP * cosInc) * vyPerif;
            double vz = sinArgP * sinInc * vxPerif + cosArgP * sinInc * vyPerif;

            // Convert ECI to geodetic
            float alt = (float)(r - EarthRadiusKm);
            ECIToGeodetic(x, y, z, utcTime, out float lat, out float lon);

            return new OrbitalState
            {
                positionECI = new Vector3((float)x, (float)z, (float)y),
                velocityECI = new Vector3((float)vx, (float)vz, (float)vy),
                utcTime     = utcTime,
                altitudeKm  = alt,
                latitudeDeg = lat,
                longitudeDeg = lon
            };
        }

        /// <summary>Computes the orbital period in minutes for a given semi-major axis (km).</summary>
        public static double OrbitalPeriodMin(double smaKm)
        {
            return 2.0 * Math.PI * Math.Sqrt((smaKm * smaKm * smaKm) / MuEarth) / 60.0;
        }

        /// <summary>Returns the orbit type classification based on altitude (km).</summary>
        public static OrbitType ClassifyOrbit(float altitudeKm)
        {
            if (altitudeKm < 2000f)  return OrbitType.LEO;
            if (altitudeKm < 35000f) return OrbitType.MEO;
            if (altitudeKm < 36500f) return OrbitType.GEO;
            return OrbitType.HEO;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static double SolveKepler(double m, double ecc, int maxIter = 50)
        {
            double e = m;
            for (int i = 0; i < maxIter; i++)
            {
                double delta = (m - e + ecc * Math.Sin(e)) / (1.0 - ecc * Math.Cos(e));
                e += delta;
                if (Math.Abs(delta) < 1e-12) break;
            }
            return e;
        }

        private static double NormaliseAngle(double rad)
        {
            rad = rad % (2.0 * Math.PI);
            return rad < 0 ? rad + 2.0 * Math.PI : rad;
        }

        private static void ECIToGeodetic(double x, double y, double z, DateTime utc,
                                          out float lat, out float lon)
        {
            // Greenwich Mean Sidereal Time (approximate)
            double jd = DateTimeToJulian(utc);
            double T  = (jd - 2451545.0) / 36525.0;
            double gmst = (280.46061837 + 360.98564736629 * (jd - 2451545.0)
                          + T * T * 0.000387933) % 360.0;
            double gmstRad = gmst * Mathf.Deg2Rad;

            // Rotate ECI → ECEF
            double xe = x * Math.Cos(gmstRad) + y * Math.Sin(gmstRad);
            double ye = -x * Math.Sin(gmstRad) + y * Math.Cos(gmstRad);
            double ze = z;

            lon = (float)(Math.Atan2(ye, xe) * Mathf.Rad2Deg);
            double r  = Math.Sqrt(xe * xe + ye * ye + ze * ze);
            lat = (float)(Math.Asin(ze / r) * Mathf.Rad2Deg);
        }

        private static double DateTimeToJulian(DateTime dt)
        {
            int y = dt.Year; int m = dt.Month; int d = dt.Day;
            double h = dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0;
            if (m <= 2) { y--; m += 12; }
            int A = y / 100; int B = 2 - A + A / 4;
            return (int)(365.25 * (y + 4716)) + (int)(30.6001 * (m + 1)) + d + h / 24.0 + B - 1524.5;
        }

        private static DateTime JulianToDateTime(double jd)
        {
            double z = Math.Floor(jd + 0.5);
            double f = jd + 0.5 - z;
            double a = z < 2299161 ? z : z + 1 + (int)((z - 1867216.25) / 36524.25)
                       - (int)((z - 1867216.25) / 36524.25) / 4;
            double b = a + 1524;
            int c = (int)((b - 122.1) / 365.25);
            int dd = (int)(365.25 * c);
            int e = (int)((b - dd) / 30.6001);
            int day   = (int)(b - dd - (int)(30.6001 * e));
            int month = e < 14 ? e - 1 : e - 13;
            int year  = month > 2 ? c - 4716 : c - 4715;
            double hourF = f * 24.0;
            int hour = (int)hourF; int min = (int)((hourF - hour) * 60);
            int sec  = (int)(((hourF - hour) * 60 - min) * 60);
            return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
        }
    }
}
