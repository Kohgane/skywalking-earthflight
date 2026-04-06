// ReportExporter.cs — Phase 116: Flight Analytics Dashboard
// Export reports: CSV raw data, JSON, image export.
// Namespace: SWEF.FlightAnalytics

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Exports <see cref="FlightReport"/> and <see cref="FlightSessionRecord"/>
    /// data to the device's file system in the format specified by <see cref="ExportFormat"/>.
    /// PDF and PNG export are platform-dependent stubs (require native plugins).
    /// </summary>
    public class ReportExporter : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── Properties ────────────────────────────────────────────────────────────

        private string ExportPath => Path.Combine(
            Application.persistentDataPath,
            config != null ? config.exportSubfolder : "FlightAnalytics/Exports");

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake() => Directory.CreateDirectory(ExportPath);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Export a <see cref="FlightReport"/> in the requested format.</summary>
        /// <returns>Full path of the exported file, or null on failure.</returns>
        public string ExportReport(FlightReport report, ExportFormat format)
        {
            if (report == null) return null;

            try
            {
                string filename = $"report_{report.reportId[..8]}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                string path;

                switch (format)
                {
                    case ExportFormat.JSON:
                        path = Path.Combine(ExportPath, filename + ".json");
                        File.WriteAllText(path, JsonUtility.ToJson(report, prettyPrint: true));
                        break;

                    case ExportFormat.CSV:
                        path = Path.Combine(ExportPath, filename + ".csv");
                        File.WriteAllText(path, BuildCsv(report), Encoding.UTF8);
                        break;

                    case ExportFormat.PDF:
                    case ExportFormat.PNG:
                        Debug.LogWarning("[SWEF] ReportExporter: PDF/PNG export requires a native plugin — not implemented.");
                        return null;

                    default:
                        return null;
                }

                Debug.Log($"[SWEF] ReportExporter: Exported to {path}");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] ReportExporter: Export failed — {e.Message}");
                return null;
            }
        }

        /// <summary>Export raw session data points as CSV.</summary>
        public string ExportSessionCsv(FlightSessionRecord session)
        {
            if (session == null) return null;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("timestamp,altitude,speedKnots,heading,gForce,fuelNormalised");

                foreach (var p in session.dataPoints)
                    sb.AppendLine($"{p.timestamp:F2},{p.altitude:F1},{p.speedKnots:F1}," +
                                  $"{p.heading:F1},{p.gForce:F2},{p.fuelNormalised:F3}");

                string path = Path.Combine(ExportPath, $"session_{session.sessionId[..8]}.csv");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[SWEF] ReportExporter: Session CSV exported to {path}");
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] ReportExporter: Session CSV export failed — {e.Message}");
                return null;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildCsv(FlightReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("metric,value");
            foreach (var kv in report.metrics)
                sb.AppendLine($"{kv.Key},{kv.Value:F2}");
            sb.AppendLine($"title,\"{report.title}\"");
            sb.AppendLine($"summary,\"{report.summary}\"");
            return sb.ToString();
        }
    }
}
