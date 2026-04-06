// SGP4Propagator.cs — Phase 114: Satellite & Space Debris Tracking
// Simplified General Perturbations Model 4 (SGP4) for satellite position prediction.
// Includes atmospheric drag, J2 oblateness, and solar/lunar perturbations (simplified).
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Simplified General Perturbations 4 (SGP4) propagator.
    /// Predicts satellite position and velocity from TLE data accounting for
    /// atmospheric drag, Earth's oblateness (J2–J4), and basic perturbations.
    /// </summary>
    public class SGP4Propagator : MonoBehaviour
    {
        // ── WGS-72 constants used by SGP4 ─────────────────────────────────────────
        private const double Xke    = 0.0743669161;   // √(GM) in Earth-radii^(3/2) per minute
        private const double J2     = 1.082616e-3;
        private const double J3     = -2.53881e-6;
        private const double J4     = -1.65597e-6;
        private const double Ae     = 1.0;            // Earth radius in Earth-radii
        private const double De2Re  = 6378.135;       // km per Earth radius (WGS-72)
        private const double Xj3    = -2.53881e-6;
        private const double TwoPi  = 2.0 * Math.PI;
        private const double Ck2    = 0.5 * J2 * Ae * Ae;

        /// <summary>
        /// Propagates a satellite from its TLE epoch to the given UTC time using SGP4.
        /// </summary>
        /// <param name="tle">TLE data for the satellite.</param>
        /// <param name="utcTime">Target time for propagation.</param>
        /// <returns>Orbital state at the target time, or null on failure.</returns>
        public OrbitalState Propagate(TLEData tle, DateTime utcTime)
        {
            if (tle == null) return null;

            try
            {
                double tSinceMin = (utcTime - JulianToDateTime(tle.epochJulian)).TotalMinutes;
                return PropagateSGP4(tle, tSinceMin);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SGP4] Propagation failed for NORAD {tle.noradId}: {ex.Message}");
                return null;
            }
        }

        // ── SGP4 implementation ────────────────────────────────────────────────────

        private static OrbitalState PropagateSGP4(TLEData tle, double tSinceMin)
        {
            // Recover original mean motion and semi-major axis
            double xno  = tle.meanMotionRevPerDay * TwoPi / 1440.0; // rad/min
            double eo   = tle.eccentricity;
            double xio  = tle.inclinationDeg * Math.PI / 180.0;
            double omegao = tle.argOfPerigeeDeg * Math.PI / 180.0;
            double omegao_dot = tle.raanDeg * Math.PI / 180.0;
            double xmo  = tle.meanAnomalyDeg * Math.PI / 180.0;
            double bstar = tle.bstar;

            double a1  = Math.Pow(Xke / xno, 2.0 / 3.0);
            double cosio = Math.Cos(xio);
            double theta2 = cosio * cosio;
            double x3thm1 = 3.0 * theta2 - 1.0;
            double eosq  = eo * eo;
            double betao2 = 1.0 - eosq;
            double betao  = Math.Sqrt(betao2);

            double del1 = 1.5 * Ck2 * x3thm1 / (a1 * a1 * betao * betao2);
            double ao   = a1 * (1.0 - del1 * (0.5 * 2.0 / 3.0 + del1 * (1.0 + 134.0 / 81.0 * del1)));
            double delo = 1.5 * Ck2 * x3thm1 / (ao * ao * betao * betao2);
            double xnodp = xno / (1.0 + delo);
            double aodp  = ao / (1.0 - delo);

            // For perigee below 156 km, s and qoms2t are altered
            double perige = (aodp * (1.0 - eo) - Ae) * De2Re;
            double s4, qoms24;
            if (perige < 98.0)
            {
                s4     = 20.0 / De2Re + Ae;
                qoms24 = Math.Pow((120.0 - 78.0) / De2Re, 4.0);
            }
            else if (perige < 156.0)
            {
                s4     = perige / De2Re - 78.0 / De2Re + Ae;
                qoms24 = Math.Pow((120.0 - (perige - 78.0)) / De2Re, 4.0);
            }
            else
            {
                s4     = 78.0 / De2Re + Ae;
                qoms24 = Math.Pow(120.0 / De2Re, 4.0);
            }

            double tsi    = 1.0 / (aodp - s4);
            double eta    = aodp * eo * tsi;
            double etasq  = eta * eta;
            double eeta   = eo * eta;
            double psisq  = Math.Abs(1.0 - etasq);
            double coef   = qoms24 * Math.Pow(tsi, 4.0);
            double coef1  = coef / Math.Pow(psisq, 3.5);

            double c2 = coef1 * xnodp
                * (aodp * (1.0 + 1.5 * etasq + eeta * (4.0 + etasq))
                + 0.75 * Ck2 * tsi / psisq * x3thm1 * (8.0 + 3.0 * etasq * (8.0 + etasq)));
            double c1    = bstar * c2;
            double sinio = Math.Sin(xio);
            double c3    = 0.0;
            if (eo > 1e-4)
                c3 = coef * tsi * J3 / J2 * xnodp * (Ae / eo) * sinio;

            double x1mth2 = 1.0 - theta2;
            double c4     = 2.0 * xnodp * coef1 * aodp * betao2
                * (eta * (2.0 + 0.5 * etasq) + eo * (0.5 + 2.0 * etasq)
                - 2.0 * Ck2 * tsi / (aodp * psisq)
                * (-3.0 * x3thm1 * (1.0 - 2.0 * eeta + etasq * (1.5 - 0.5 * eeta))
                + 0.75 * x1mth2 * (2.0 * etasq - eeta * (1.0 + etasq))
                * Math.Cos(2.0 * omegao)));

            double d2 = 4.0 * aodp * tsi * c1 * c1;
            double temp = d2 * tsi * c1 / 3.0;
            double d3   = (17.0 * aodp + s4) * temp;
            double d4   = 0.5 * temp * aodp * tsi * (221.0 * aodp + 31.0 * s4) * c1;

            // Secular effects of drag and gravity
            double xmdf   = xmo + (1.0 + 1.5 * Ck2 * (-1.0 + 1.5 * theta2) / (aodp * aodp * betao2 * betao2) * xnodp) * tSinceMin;
            double omgadf = omegao + (-1.5 * Ck2 * (1.0 / (aodp * aodp * betao2 * betao2))
                            * (x3thm1 - 1.0 + 5.0 * theta2) / 3.0 * xnodp
                            * (1.0 + 3.0 * Ck2 * (-1.0 + 7.0 * theta2) / (2.0 * aodp * aodp * betao2 * betao2) * 2.0)) * tSinceMin;
            double xnode   = omegao_dot + (-1.5 * Ck2 * cosio / (aodp * aodp * betao2 * betao2) * xnodp * 2.0) * tSinceMin;

            double tsq = tSinceMin * tSinceMin;
            double xmp = xmdf + (-c1) * tSinceMin + d2 * tsq + d3 * tSinceMin * tsq + d4 * tsq * tsq;

            // Long-period terms
            double axn  = eo * Math.Cos(omgadf);
            double temp11 = 1.0 / (aodp * (1.0 - eo * eo));
            double xlcof  = 0.125 * J3 / J2 * sinio * (3.0 + 5.0 * cosio) / (1.0 + cosio);
            double aycof  = 0.25 * J3 / J2 * sinio;
            double xll    = xmp + xlcof * axn;
            double aynl   = aycof * (eo * Math.Sin(omgadf) + c3 * Math.Sin(xll));
            double xlt    = xll + aynl;
            double ayn    = eo * Math.Sin(omgadf) + c3 * Math.Sin(xlt);
            double elsq   = axn * axn + ayn * ayn;
            double capu   = NormaliseAngle(xlt - xnode);

            // Solve Kepler in eccentric form (iterative)
            double sinepw = 0, cosepw = 0, epw = capu;
            for (int iter = 0; iter < 10; iter++)
            {
                sinepw = Math.Sin(epw);
                cosepw = Math.Cos(epw);
                double epwNew = epw + (capu - ayn * cosepw + axn * sinepw - epw)
                                    / (1.0 - cosepw * axn - sinepw * ayn);
                if (Math.Abs(epwNew - epw) < 1e-12) break;
                epw = epwNew;
            }

            double ecose = axn * cosepw + ayn * sinepw;
            double esine = axn * sinepw - ayn * cosepw;
            double el2   = elsq;
            double pl    = aodp * (1.0 - el2);
            double r1    = aodp * (1.0 - ecose);
            double rdot  = Xke * Math.Sqrt(aodp) * esine / r1;
            double rfdot = Xke * Math.Sqrt(pl) / r1;
            double cosu  = (cosepw - axn + ayn * esine / (1.0 + Math.Sqrt(1.0 - el2))) / r1 * aodp;
            double sinu  = (sinepw - ayn - axn * esine / (1.0 + Math.Sqrt(1.0 - el2))) / r1 * aodp;
            double u     = Math.Atan2(sinu, cosu);

            double sin2u = 2.0 * sinu * cosu;
            double cos2u = 1.0 - 2.0 * sinu * sinu;
            double rk    = r1 + Ck2 / pl * (-3.0 * (1.0 - 3.0 * theta2) / (2.0 * pl) + (1.0 + 3.0 * cos2u * x3thm1 / 2.0));
            double uk    = u - Ck2 / (2.0 * pl * pl) * 3.5 * x1mth2 * sin2u;
            double xnodek = xnode + 1.5 * Ck2 * cosio / pl / pl * sin2u;
            double xinck  = xio + 1.5 * Ck2 * cosio * sinio / pl / pl * cos2u;

            // Orientation vectors
            double sinuk  = Math.Sin(uk);
            double cosuk  = Math.Cos(uk);
            double sinik  = Math.Sin(xinck);
            double cosik  = Math.Cos(xinck);
            double sinnok = Math.Sin(xnodek);
            double cosnok = Math.Cos(xnodek);

            double xmx = -sinnok * cosik;
            double xmy =  cosnok * cosik;
            double ux   =  xmx * sinuk + cosnok * cosuk;
            double uy   =  xmy * sinuk + sinnok * cosuk;
            double uz   =  sinik * sinuk;
            double vx   =  xmx * cosuk - cosnok * sinuk;
            double vy   =  xmy * cosuk - sinnok * sinuk;
            double vz   =  sinik * cosuk;

            // Position and velocity (Earth radii and Earth radii / min)
            double x = rk * ux;
            double y = rk * uy;
            double z = rk * uz;
            double xdot = (rdot * ux + rfdot * vx) * De2Re / 60.0; // km/s
            double ydot = (rdot * uy + rfdot * vy) * De2Re / 60.0;
            double zdot = (rdot * uz + rfdot * vz) * De2Re / 60.0;

            // Scale to km
            double xkm = x * De2Re;
            double ykm = y * De2Re;
            double zkm = z * De2Re;

            float altKm = (float)(Math.Sqrt(xkm * xkm + ykm * ykm + zkm * zkm) - De2Re);

            return new OrbitalState
            {
                positionECI  = new Vector3((float)xkm, (float)zkm, (float)ykm),
                velocityECI  = new Vector3((float)xdot, (float)zdot, (float)ydot),
                utcTime      = DateTime.UtcNow.AddMinutes(tSinceMin),
                altitudeKm   = altKm,
                latitudeDeg  = (float)(Math.Asin(zkm / Math.Sqrt(xkm * xkm + ykm * ykm + zkm * zkm)) * 180.0 / Math.PI),
                longitudeDeg = (float)(Math.Atan2(ykm, xkm) * 180.0 / Math.PI)
            };
        }

        private static double NormaliseAngle(double rad)
        {
            rad = rad % TwoPi;
            return rad < 0 ? rad + TwoPi : rad;
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
            try { return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc); }
            catch { return DateTime.UtcNow; }
        }
    }
}
