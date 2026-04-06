// LandingHeatmap.cs — Phase 116: Flight Analytics Dashboard
// Landing zone heatmap: touchdown distribution, centreline accuracy, approach analysis.
// Namespace: SWEF.FlightAnalytics

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Generates a localised heatmap of touchdown points for a specific
    /// runway, used to visualise centreline accuracy and landing zone spread.
    /// </summary>
    public class LandingHeatmap : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── Nested type ───────────────────────────────────────────────────────────

        /// <summary>A single touchdown record with its runway-relative offset.</summary>
        [System.Serializable]
        public class TouchdownRecord
        {
            /// <summary>World-space touchdown position.</summary>
            public Vector3 worldPosition;
            /// <summary>Distance from the runway threshold in metres.</summary>
            public float distanceFromThreshold;
            /// <summary>Lateral offset from the runway centreline in metres.</summary>
            public float centrelineOffset;
            /// <summary>Vertical speed at touchdown (m/s, negative = descent).</summary>
            public float verticalSpeedMs;
            /// <summary>Landing quality score (0–100).</summary>
            public float score;
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<TouchdownRecord> _records = new List<TouchdownRecord>();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Record a touchdown event.</summary>
        public void RecordTouchdown(Vector3 worldPos, float distFromThreshold,
                                    float centrelineOffset, float verticalSpeedMs, float score)
        {
            _records.Add(new TouchdownRecord
            {
                worldPosition        = worldPos,
                distanceFromThreshold = distFromThreshold,
                centrelineOffset     = centrelineOffset,
                verticalSpeedMs      = verticalSpeedMs,
                score                = score
            });
        }

        /// <summary>
        /// Generate a heatmap of touchdown centreline offsets.
        /// Resolution and range determined by config.
        /// </summary>
        public HeatmapData GenerateCentrelineHeatmap()
        {
            int res = config != null ? config.landingHeatmapResolution : 64;
            float range = 50f; // ±50 m from centreline

            var cells = new float[res, res];

            foreach (var r in _records)
            {
                float normX = Mathf.Clamp01((r.centrelineOffset + range) / (range * 2f));
                float normY = Mathf.Clamp01(r.distanceFromThreshold / 3000f); // up to 3 km
                int cx = Mathf.Clamp(Mathf.FloorToInt(normX * res), 0, res - 1);
                int cy = Mathf.Clamp(Mathf.FloorToInt(normY * res), 0, res - 1);
                cells[cx, cy] += 1f;
            }

            float max = 0f;
            for (int x = 0; x < res; x++)
                for (int y = 0; y < res; y++)
                    if (cells[x, y] > max) max = cells[x, y];

            var data = new HeatmapData { width = res, height = res, maxValue = max };
            for (int x = 0; x < res; x++)
                for (int y = 0; y < res; y++)
                    if (cells[x, y] > 0f)
                        data.cells.Add(new HeatmapCell
                        {
                            x = x, y = y,
                            value = cells[x, y],
                            normalised = max > 0f ? cells[x, y] / max : 0f
                        });

            return data;
        }

        /// <summary>Average centreline offset across all recorded touchdowns.</summary>
        public float AverageCentrelineOffset()
        {
            if (_records.Count == 0) return 0f;
            float sum = 0f;
            foreach (var r in _records) sum += Mathf.Abs(r.centrelineOffset);
            return sum / _records.Count;
        }

        /// <summary>All recorded touchdown records.</summary>
        public IReadOnlyList<TouchdownRecord> Records => _records;
    }
}
