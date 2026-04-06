// AltitudeHeatmap.cs — Phase 116: Flight Analytics Dashboard
// Altitude distribution heatmap: time at altitude bands, preferred cruising altitudes.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Computes the distribution of time (seconds) spent in each altitude
    /// band across one or more flight sessions.
    /// </summary>
    public class AltitudeHeatmap : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>Height of each altitude band in metres.</summary>
        public const float BandHeightM = 1000f;

        /// <summary>Maximum altitude tracked (metres).</summary>
        public const float MaxAltitudeM = 15000f;

        /// <summary>Number of altitude bands.</summary>
        public static int BandCount => Mathf.CeilToInt(MaxAltitudeM / BandHeightM);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Compute time-in-band distribution from a list of sessions.
        /// Returns an array indexed by band (index 0 = 0–1000 m, etc.).
        /// </summary>
        public float[] ComputeTimeInBands(IList<FlightSessionRecord> sessions)
        {
            var bands = new float[BandCount];
            if (sessions == null) return bands;

            foreach (var session in sessions)
            {
                if (session.dataPoints == null || session.dataPoints.Count < 2) continue;

                for (int i = 1; i < session.dataPoints.Count; i++)
                {
                    float dt  = session.dataPoints[i].timestamp - session.dataPoints[i - 1].timestamp;
                    float alt = session.dataPoints[i].altitude;
                    int band  = Mathf.Clamp(Mathf.FloorToInt(alt / BandHeightM), 0, BandCount - 1);
                    bands[band] += dt;
                }
            }

            return bands;
        }

        /// <summary>
        /// Return the altitude band index with the most accumulated time (preferred cruising altitude).
        /// </summary>
        public int PreferredCruisingBandIndex(float[] bands)
        {
            if (bands == null || bands.Length == 0) return 0;
            int best = 0;
            for (int i = 1; i < bands.Length; i++)
                if (bands[i] > bands[best]) best = i;
            return best;
        }

        /// <summary>
        /// Convert a band distribution into a list of <see cref="ChartSeries"/> for bar-chart rendering.
        /// </summary>
        public ChartSeries ToChartSeries(float[] bands, string label = "Time in Band (s)")
        {
            var series = new ChartSeries { label = label, color = Color.cyan };
            if (bands == null) return series;

            for (int i = 0; i < bands.Length; i++)
            {
                series.values.Add(bands[i]);
                series.xLabels.Add($"{i * (int)BandHeightM / 1000}–{(i + 1) * (int)BandHeightM / 1000} km");
            }
            return series;
        }
    }
}
