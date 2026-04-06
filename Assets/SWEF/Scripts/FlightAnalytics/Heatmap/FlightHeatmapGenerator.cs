// FlightHeatmapGenerator.cs — Phase 116: Flight Analytics Dashboard
// Geographic heatmap: flight path density, favourite areas, most visited airports.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Generates a geographic density heatmap from historical
    /// <see cref="FlightDataPoint"/> positions, suitable for world-map overlay rendering.
    /// </summary>
    public class FlightHeatmapGenerator : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a <see cref="HeatmapData"/> from a collection of flight sessions.
        /// Each recorded position is mapped to a grid cell and its density incremented.
        /// </summary>
        public HeatmapData GenerateFlightDensityMap(IList<FlightSessionRecord> sessions)
        {
            int res        = config != null ? config.heatmapResolution : 128;
            float worldSize = config != null ? config.heatmapWorldSize  : 200000f;

            var cells = new float[res, res];

            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    if (session.dataPoints == null) continue;
                    foreach (var pt in session.dataPoints)
                        Accumulate(cells, pt.position, res, worldSize);
                }
            }

            return BuildHeatmapData(cells, res);
        }

        /// <summary>
        /// Build a heatmap that shows the most visited airports, mapped by world position.
        /// </summary>
        public HeatmapData GenerateAirportVisitMap(IList<FlightSessionRecord> sessions,
                                                    Dictionary<string, Vector3> airportPositions)
        {
            int res        = config != null ? config.heatmapResolution : 128;
            float worldSize = config != null ? config.heatmapWorldSize  : 200000f;

            var cells = new float[res, res];

            if (sessions != null && airportPositions != null)
            {
                foreach (var session in sessions)
                {
                    foreach (string icao in session.airportsVisited)
                    {
                        if (airportPositions.TryGetValue(icao, out Vector3 pos))
                            Accumulate(cells, pos, res, worldSize);
                    }
                }
            }

            return BuildHeatmapData(cells, res);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void Accumulate(float[,] cells, Vector3 worldPos, int res, float worldSize)
        {
            float halfSize = worldSize * 0.5f;
            int cx = Mathf.Clamp(Mathf.FloorToInt((worldPos.x + halfSize) / worldSize * res), 0, res - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt((worldPos.z + halfSize) / worldSize * res), 0, res - 1);
            cells[cx, cy] += 1f;
        }

        private static HeatmapData BuildHeatmapData(float[,] cells, int res)
        {
            var data = new HeatmapData { width = res, height = res };
            float max = 0f;

            for (int x = 0; x < res; x++)
                for (int y = 0; y < res; y++)
                    if (cells[x, y] > max) max = cells[x, y];

            data.maxValue = max;

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    if (cells[x, y] <= 0f) continue;
                    data.cells.Add(new HeatmapCell
                    {
                        x          = x,
                        y          = y,
                        value      = cells[x, y],
                        normalised = max > 0f ? cells[x, y] / max : 0f
                    });
                }
            }

            return data;
        }
    }
}
