using System;
using UnityEngine;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Pure-math static utility for astronomical calculations.
    /// Based on Jean Meeus' <em>Astronomical Algorithms</em> and the NOAA Solar Calculator.
    /// No MonoBehaviour, no Unity lifecycle — safe to call from any thread.
    /// </summary>
    public static class SolarCalculator
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const double Deg2Rad = Math.PI / 180.0;
        private const double Rad2Deg = 180.0 / Math.PI;

        // Obliquity coefficient used in mean obliquity calculation (arcseconds/Julian century)
        private const double ObliquityCoeff = 46.8150;

        // Synodic month length in days
        private const double SynodicMonth = 29.53058867;

        // Known new moon reference epoch (Jan 6, 2000 18:14 UTC)
        private static readonly DateTime KnownNewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the sun's altitude and azimuth for a given UTC time and geographic location.
        /// </summary>
        /// <param name="utcTime">UTC date and time.</param>
        /// <param name="latitude">Degrees, −90 (south) to +90 (north).</param>
        /// <param name="longitude">Degrees, −180 (west) to +180 (east).</param>
        /// <returns>Tuple of (altitude, azimuth) in degrees.</returns>
        public static (float altitude, float azimuth) CalculateSunPosition(
            DateTime utcTime, float latitude, float longitude)
        {
            double jd = ToJulianDay(utcTime);

            GetSunEquatorial(jd, out double declRad, out double raDeg);

            double lst = GetLocalSiderealTime(jd, longitude);
            double haRad = (lst - raDeg) * Deg2Rad;
            double latRad = latitude * Deg2Rad;

            double sinAlt = Math.Sin(latRad) * Math.Sin(declRad) +
                            Math.Cos(latRad) * Math.Cos(declRad) * Math.Cos(haRad);
            double altRad = Math.Asin(Clamp(sinAlt, -1.0, 1.0));

            double cosAz = (Math.Sin(declRad) - Math.Sin(latRad) * sinAlt) /
                           (Math.Cos(latRad) * Math.Cos(altRad));
            double azRad = Math.Acos(Clamp(cosAz, -1.0, 1.0));

            double az = Math.Sin(haRad) < 0 ? azRad * Rad2Deg : 360.0 - azRad * Rad2Deg;

            // Atmospheric refraction correction near horizon
            double altDeg = altRad * Rad2Deg;
            if (altDeg > -0.575 && altDeg < 5.0)
            {
                altDeg += RefractionCorrection(altDeg);
            }

            return ((float)altDeg, (float)az);
        }

        /// <summary>
        /// Calculates today's sunrise time as a fractional local hour (0–24).
        /// Returns <c>-1</c> for polar night and <c>25</c> for midnight sun.
        /// </summary>
        public static float CalculateSunrise(DateTime date, float lat, float lon)
        {
            return GetSunriseSunset(date, lat, lon, rising: true);
        }

        /// <summary>
        /// Calculates today's sunset time as a fractional local hour (0–24).
        /// Returns <c>-1</c> for polar night and <c>25</c> for midnight sun.
        /// </summary>
        public static float CalculateSunset(DateTime date, float lat, float lon)
        {
            return GetSunriseSunset(date, lat, lon, rising: false);
        }

        /// <summary>
        /// Calculates total daylight hours for the given date and location.
        /// Returns <c>0</c> for polar night and <c>24</c> for midnight sun.
        /// </summary>
        public static float CalculateDayLength(DateTime date, float lat, float lon)
        {
            float rise = CalculateSunrise(date, lat, lon);
            float set  = CalculateSunset(date, lat, lon);
            if (rise < 0f)  return 0f;
            if (rise > 24f) return 24f;
            return Mathf.Max(0f, set - rise);
        }

        /// <summary>
        /// Maps a sun altitude angle to the corresponding <see cref="DayPhase"/>.
        /// </summary>
        /// <param name="sunAltitude">Sun altitude in degrees (may be negative).</param>
        public static DayPhase GetDayPhase(float sunAltitude)
        {
            if (sunAltitude >= -1f && sunAltitude <= 1f)
            {
                // Within 1° of horizon — still check for golden hour
                return sunAltitude >= 0f ? DayPhase.GoldenHour : DayPhase.CivilTwilight;
            }
            if (sunAltitude > 1f  && sunAltitude <= 6f)  return DayPhase.GoldenHour;
            if (sunAltitude > 6f)                         return DayPhase.Day;
            if (sunAltitude > -6f)                        return DayPhase.CivilTwilight;
            if (sunAltitude > -12f)                       return DayPhase.NauticalTwilight;
            if (sunAltitude > -18f)                       return DayPhase.AstronomicalTwilight;
            return DayPhase.Night;
        }

        /// <summary>
        /// Computes the moon's altitude and azimuth for a given UTC time and location.
        /// Uses a simplified lunar position algorithm (accuracy ≈ 0.3°).
        /// </summary>
        public static (float altitude, float azimuth) CalculateMoonPosition(
            DateTime utcTime, float lat, float lon)
        {
            double jd = ToJulianDay(utcTime);

            // Days since J2000.0
            double d = jd - 2451545.0;

            // Moon's mean longitude and anomaly
            double L = Mod360(218.316 + 13.176396 * d);
            double M = Mod360(134.963 + 13.064993 * d);
            double F = Mod360(93.272  + 13.229350 * d);

            // Geocentric ecliptic longitude/latitude
            double lon_moon = L + 6.289 * Math.Sin(M * Deg2Rad);
            double lat_moon = 5.128 * Math.Sin(F * Deg2Rad);

            // Convert ecliptic → equatorial
            double eps = 23.439 - 0.000036 * d; // obliquity
            double epsRad = eps * Deg2Rad;
            double lonRad = lon_moon * Deg2Rad;

            double raDeg = Math.Atan2(
                Math.Sin(lonRad) * Math.Cos(epsRad) - Math.Tan(lat_moon * Deg2Rad) * Math.Sin(epsRad),
                Math.Cos(lonRad)) * Rad2Deg;
            raDeg = Mod360(raDeg);

            double decRad = Math.Asin(
                Math.Sin(lat_moon * Deg2Rad) * Math.Cos(epsRad) +
                Math.Cos(lat_moon * Deg2Rad) * Math.Sin(epsRad) * Math.Sin(lonRad));

            double lst = GetLocalSiderealTime(jd, lon);
            double haRad = (lst - raDeg) * Deg2Rad;
            double latRad = lat * Deg2Rad;

            double sinAlt = Math.Sin(latRad) * Math.Sin(decRad) +
                            Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(haRad);
            double altRad = Math.Asin(Clamp(sinAlt, -1.0, 1.0));

            double cosAz = (Math.Sin(decRad) - Math.Sin(latRad) * sinAlt) /
                           (Math.Cos(latRad) * Math.Cos(altRad));
            double azRad = Math.Acos(Clamp(cosAz, -1.0, 1.0));
            double az = Math.Sin(haRad) < 0 ? azRad * Rad2Deg : 360.0 - azRad * Rad2Deg;

            return ((float)(altRad * Rad2Deg), (float)az);
        }

        /// <summary>
        /// Determines the current <see cref="MoonPhase"/> for the given UTC time.
        /// </summary>
        public static MoonPhase GetMoonPhase(DateTime utcTime)
        {
            double age = GetMoonAge(utcTime);
            // Divide synodic month into 8 segments
            double seg = age / SynodicMonth * 8.0;
            int idx = (int)Math.Floor(seg) % 8;
            return (MoonPhase)idx;
        }

        /// <summary>
        /// Returns the fraction of the moon's visible disk that is illuminated (0–1).
        /// </summary>
        public static float GetMoonIllumination(DateTime utcTime)
        {
            double age = GetMoonAge(utcTime);
            // Illumination follows a cosine curve over the synodic month
            double phase = age / SynodicMonth * 2.0 * Math.PI;
            return (float)((1.0 - Math.Cos(phase)) / 2.0);
        }

        /// <summary>
        /// Determines the meteorological <see cref="Season"/> for the given date and hemisphere.
        /// Southern hemisphere seasons are inverted relative to the northern hemisphere.
        /// </summary>
        /// <param name="date">Calendar date.</param>
        /// <param name="latitude">Decimal degrees latitude. Negative = southern hemisphere.</param>
        public static Season GetSeason(DateTime date, float latitude)
        {
            // Northern hemisphere meteorological seasons by month
            Season northern = date.Month switch
            {
                3 or 4 or 5   => Season.Spring,
                6 or 7 or 8   => Season.Summer,
                9 or 10 or 11 => Season.Autumn,
                _             => Season.Winter
            };

            // Flip for southern hemisphere
            if (latitude < 0f)
            {
                return northern switch
                {
                    Season.Spring => Season.Autumn,
                    Season.Autumn => Season.Spring,
                    Season.Summer => Season.Winter,
                    Season.Winter => Season.Summer,
                    _             => northern
                };
            }
            return northern;
        }

        /// <summary>
        /// Calculates the time of solar noon (maximum sun altitude) as a fractional UTC hour.
        /// </summary>
        public static float SolarNoonTime(DateTime date, float longitude)
        {
            double jd = ToJulianDay(new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc));
            double jc = (jd - 2451545.0) / 36525.0;

            double eqTime = EquationOfTime(jc); // minutes
            double solarNoon = 720.0 - 4.0 * longitude - eqTime; // minutes from midnight UTC
            return (float)(solarNoon / 60.0);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private static double ToJulianDay(DateTime utc)
        {
            // Julian Day Number calculation
            int    y = utc.Year,  m = utc.Month;
            double d = utc.Day + (utc.Hour + utc.Minute / 60.0 + utc.Second / 3600.0) / 24.0;
            if (m <= 2) { y--; m += 12; }
            int A = y / 100;
            int B = 2 - A + A / 4;
            return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + B - 1524.5;
        }

        private static void GetSunEquatorial(double jd, out double declRad, out double raDeg)
        {
            double jc = (jd - 2451545.0) / 36525.0;

            double geomMeanLon  = Mod360(280.46646 + jc * (36000.76983 + jc * 0.0003032));
            double geomMeanAnom = Mod360(357.52911 + jc * (35999.05029 - jc * 0.0001537));
            double eccOrbit     = 0.016708634 - jc * (0.000042037 + jc * 0.0000001267);

            double eqCenter = (1.914602 - jc * (0.004817 + 0.000014 * jc)) * Math.Sin(geomMeanAnom * Deg2Rad)
                            + (0.019993 - 0.000101 * jc) * Math.Sin(2.0 * geomMeanAnom * Deg2Rad)
                            + 0.000289 * Math.Sin(3.0 * geomMeanAnom * Deg2Rad);

            double sunTrueLon = geomMeanLon + eqCenter;
            double appLon = sunTrueLon - 0.00569 - 0.00478 * Math.Sin((125.04 - 1934.136 * jc) * Deg2Rad);

            double meanObliq = 23.0 + (26.0 + (21.448 - jc * (ObliquityCoeff + jc * (0.00059 - jc * 0.001813))) / 60.0) / 60.0;
            double obliqCorr = meanObliq + 0.00256 * Math.Cos((125.04 - 1934.136 * jc) * Deg2Rad);

            raDeg   = Math.Atan2(Math.Cos(obliqCorr * Deg2Rad) * Math.Sin(appLon * Deg2Rad),
                                 Math.Cos(appLon * Deg2Rad)) * Rad2Deg;
            raDeg   = Mod360(raDeg);
            declRad = Math.Asin(Math.Sin(obliqCorr * Deg2Rad) * Math.Sin(appLon * Deg2Rad));
        }

        private static double GetLocalSiderealTime(double jd, double longitude)
        {
            double jc = (jd - 2451545.0) / 36525.0;
            double gmst = 280.46061837 + 360.98564736629 * (jd - 2451545.0) +
                          jc * jc * (0.000387933 - jc / 38710000.0);
            return Mod360(gmst + longitude);
        }

        private static float GetSunriseSunset(DateTime date, float lat, float lon, bool rising)
        {
            double jd = ToJulianDay(new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc));
            double jc = (jd - 2451545.0) / 36525.0;

            double eqTime    = EquationOfTime(jc);
            double sunDeclin = GetSunDeclination(jc);

            double latRad  = lat * Deg2Rad;
            double decRad  = sunDeclin * Deg2Rad;

            // Hour angle at sunrise/sunset (sun centre at horizon = −0.833°)
            double cosOmega = (Math.Cos(90.833 * Deg2Rad) - Math.Sin(latRad) * Math.Sin(decRad)) /
                              (Math.Cos(latRad) * Math.Cos(decRad));

            // Polar night
            if (cosOmega > 1.0)  return -1f;
            // Midnight sun
            if (cosOmega < -1.0) return 25f;

            double omega = Math.Acos(cosOmega) * Rad2Deg;
            double transit = 720.0 - 4.0 * lon - eqTime; // minutes from midnight UTC

            double minutesFromNoon = rising ? -omega * 4.0 : omega * 4.0;
            double result = (transit + minutesFromNoon) / 60.0;
            return (float)result;
        }

        private static double GetSunDeclination(double jc)
        {
            double geomMeanLon  = Mod360(280.46646 + jc * (36000.76983 + jc * 0.0003032));
            double geomMeanAnom = Mod360(357.52911 + jc * (35999.05029 - jc * 0.0001537));
            double eqCenter = (1.914602 - jc * (0.004817 + 0.000014 * jc)) * Math.Sin(geomMeanAnom * Deg2Rad)
                            + (0.019993 - 0.000101 * jc) * Math.Sin(2.0 * geomMeanAnom * Deg2Rad)
                            + 0.000289 * Math.Sin(3.0 * geomMeanAnom * Deg2Rad);
            double sunTrueLon = geomMeanLon + eqCenter;
            double appLon     = sunTrueLon  - 0.00569 - 0.00478 * Math.Sin((125.04 - 1934.136 * jc) * Deg2Rad);
            double meanObliq  = 23.0 + (26.0 + (21.448 - jc * (ObliquityCoeff + jc * (0.00059 - jc * 0.001813))) / 60.0) / 60.0;
            double obliqCorr  = meanObliq + 0.00256 * Math.Cos((125.04 - 1934.136 * jc) * Deg2Rad);
            return Math.Asin(Math.Sin(obliqCorr * Deg2Rad) * Math.Sin(appLon * Deg2Rad)) * Rad2Deg;
        }

        private static double EquationOfTime(double jc)
        {
            double geomMeanLon  = Mod360(280.46646 + jc * (36000.76983 + jc * 0.0003032));
            double geomMeanAnom = Mod360(357.52911 + jc * (35999.05029 - jc * 0.0001537));
            double eccOrbit     = 0.016708634 - jc * (0.000042037 + jc * 0.0000001267);
            double meanObliq    = 23.0 + (26.0 + (21.448 - jc * (ObliquityCoeff + jc * (0.00059 - jc * 0.001813))) / 60.0) / 60.0;
            double obliqCorr    = meanObliq + 0.00256 * Math.Cos((125.04 - 1934.136 * jc) * Deg2Rad);
            double y            = Math.Tan(obliqCorr / 2.0 * Deg2Rad);
            y *= y;
            double lonRad  = geomMeanLon  * Deg2Rad;
            double anomRad = geomMeanAnom * Deg2Rad;
            return 4.0 * Rad2Deg * (y * Math.Sin(2.0 * lonRad)
                   - 2.0 * eccOrbit * Math.Sin(anomRad)
                   + 4.0 * eccOrbit * y * Math.Sin(anomRad) * Math.Cos(2.0 * lonRad)
                   - 0.5 * y * y * Math.Sin(4.0 * lonRad)
                   - 1.25 * eccOrbit * eccOrbit * Math.Sin(2.0 * anomRad));
        }

        private static double GetMoonAge(DateTime utcTime)
        {
            double elapsedDays = (utcTime - KnownNewMoon).TotalDays;
            return elapsedDays % SynodicMonth;
        }

        private static double RefractionCorrection(double altDeg)
        {
            if (altDeg > 85.0) return 0.0;
            double tanAlt = Math.Tan(altDeg * Deg2Rad);
            if (altDeg > 5.0)
                return (58.1 / tanAlt - 0.07 / (tanAlt * tanAlt * tanAlt) + 0.000086 / (tanAlt * tanAlt * tanAlt * tanAlt * tanAlt)) / 3600.0;
            if (altDeg > -0.575)
                return (1735.0 + altDeg * (-518.2 + altDeg * (103.4 + altDeg * (-12.79 + altDeg * 0.711)))) / 3600.0;
            return (-20.774 / tanAlt) / 3600.0;
        }

        private static double Mod360(double deg) => deg - Math.Floor(deg / 360.0) * 360.0;
        private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
    }
}
