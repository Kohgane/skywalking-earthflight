// TLEParser.cs — Phase 114: Satellite & Space Debris Tracking
// Two-Line Element set parser for real satellite data (NORAD format).
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Parses NORAD Two-Line Element (TLE) sets from raw text.
    /// Supports three-line TLE (name + line 1 + line 2) and two-line TLE formats.
    /// </summary>
    public static class TLEParser
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses a multi-satellite TLE file containing consecutive TLE entries.
        /// </summary>
        /// <param name="rawText">Full text content of the TLE file.</param>
        /// <returns>List of parsed <see cref="TLEData"/> objects.</returns>
        public static List<TLEData> ParseMultiple(string rawText)
        {
            var results = new List<TLEData>();
            if (string.IsNullOrEmpty(rawText)) return results;

            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < lines.Length)
            {
                string trimmed = lines[i].Trim();
                // Three-line format: name, line 1, line 2
                if (!trimmed.StartsWith("1 ") && !trimmed.StartsWith("2 ") && i + 2 < lines.Length)
                {
                    string name  = trimmed;
                    string line1 = i + 1 < lines.Length ? lines[i + 1].Trim() : null;
                    string line2 = i + 2 < lines.Length ? lines[i + 2].Trim() : null;

                    if (line1 != null && line2 != null &&
                        line1.StartsWith("1 ") && line2.StartsWith("2 "))
                    {
                        var tle = ParseLines(name, line1, line2);
                        if (tle != null) results.Add(tle);
                        i += 3;
                        continue;
                    }
                }
                // Two-line format
                if (trimmed.StartsWith("1 ") && i + 1 < lines.Length)
                {
                    string line2 = lines[i + 1].Trim();
                    if (line2.StartsWith("2 "))
                    {
                        var tle = ParseLines("UNKNOWN", trimmed, line2);
                        if (tle != null) results.Add(tle);
                        i += 2;
                        continue;
                    }
                }
                i++;
            }
            return results;
        }

        /// <summary>
        /// Parses a single TLE from its three component strings.
        /// </summary>
        public static TLEData ParseLines(string name, string line1, string line2)
        {
            if (string.IsNullOrEmpty(line1) || string.IsNullOrEmpty(line2)) return null;
            if (line1.Length < 69 || line2.Length < 69) return null;

            try
            {
                var tle = new TLEData();
                tle.name = name?.Trim() ?? "UNKNOWN";

                // ── Line 1 ────────────────────────────────────────────────────────
                tle.noradId = int.Parse(line1.Substring(2, 5).Trim());
                tle.internationalDesignator = line1.Substring(9, 8).Trim();

                // Epoch: YY + Day-of-year fraction
                string epochStr = line1.Substring(18, 14).Trim();
                tle.epochJulian = ParseTLEEpoch(epochStr);

                tle.meanMotionDot  = ParseSignedDouble(line1.Substring(33, 10));
                tle.meanMotionDDot = ParseTLEScientific(line1.Substring(44, 8));
                tle.bstar          = ParseTLEScientific(line1.Substring(53, 8));

                // ── Line 2 ────────────────────────────────────────────────────────
                tle.inclinationDeg    = double.Parse(line2.Substring(8,  8).Trim());
                tle.raanDeg           = double.Parse(line2.Substring(17, 8).Trim());
                // Eccentricity: implied decimal point
                tle.eccentricity      = double.Parse("0." + line2.Substring(26, 7).Trim());
                tle.argOfPerigeeDeg   = double.Parse(line2.Substring(34, 8).Trim());
                tle.meanAnomalyDeg    = double.Parse(line2.Substring(43, 8).Trim());
                tle.meanMotionRevPerDay = double.Parse(line2.Substring(52, 11).Trim());
                tle.revNumberAtEpoch  = int.Parse(line2.Substring(63, 5).Trim());

                return tle;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TLEParser] Failed to parse TLE for '{name}': {ex.Message}");
                return null;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static double ParseTLEEpoch(string epochStr)
        {
            // Format: YYDDD.DDDDDDDD
            int year  = int.Parse(epochStr.Substring(0, 2));
            year += year >= 57 ? 1900 : 2000;
            double dayOfYear = double.Parse(epochStr.Substring(2));

            var jan1 = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var epoch = jan1.AddDays(dayOfYear - 1.0);

            // Convert to Julian Date
            int y = epoch.Year; int m = epoch.Month; int d = epoch.Day;
            double h = epoch.Hour + epoch.Minute / 60.0 + epoch.Second / 3600.0;
            if (m <= 2) { y--; m += 12; }
            int A = y / 100; int B = 2 - A + A / 4;
            return (int)(365.25 * (y + 4716)) + (int)(30.6001 * (m + 1)) + d + h / 24.0 + B - 1524.5;
        }

        private static double ParseSignedDouble(string s)
        {
            s = s.Trim();
            // Remove leading '+' or space-padded sign
            if (s.StartsWith("+") || s.StartsWith(" ")) s = s.TrimStart('+', ' ');
            double.TryParse(s, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double val);
            return val;
        }

        private static double ParseTLEScientific(string s)
        {
            // TLE scientific: ±NNNNN±N  →  ±.NNNNN × 10^±N
            s = s.Trim();
            if (string.IsNullOrEmpty(s) || s == "00000+0" || s == "00000-0") return 0.0;

            int signVal = 1;
            if (s[0] == '-') { signVal = -1; s = s.Substring(1); }
            else if (s[0] == '+' || s[0] == ' ') { s = s.Substring(1); }

            int expSign = 1;
            int ePos = s.LastIndexOf('+');
            if (ePos < 0) { ePos = s.LastIndexOf('-'); expSign = -1; }
            if (ePos < 0) return 0.0;

            if (double.TryParse("." + s.Substring(0, ePos),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double mantissa) &&
                int.TryParse(s.Substring(ePos + 1), out int exponent))
            {
                return signVal * mantissa * Math.Pow(10.0, expSign * exponent);
            }
            return 0.0;
        }
    }
}
