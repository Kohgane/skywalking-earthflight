// IntegrationTestReport.cs — SWEF Phase 96: Integration Test & QA Framework
// Generates summary and per-module reports from integration test results.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SWEF.IntegrationTest
{
    /// <summary>
    /// Aggregates a collection of <see cref="IntegrationTestResult"/> objects into
    /// human-readable and JSON reports.
    ///
    /// <para>Call <see cref="ToText"/> for a formatted console summary or
    /// <see cref="ToJson"/> for a machine-readable JSON string.
    /// Use <see cref="WriteToFile"/> to persist the report to disk.</para>
    /// </summary>
    public sealed class IntegrationTestReport
    {
        // ── Source data ───────────────────────────────────────────────────────

        private readonly IReadOnlyList<IntegrationTestResult> _results;

        // ── Aggregate counts ──────────────────────────────────────────────────

        /// <summary>Total number of tests in this report.</summary>
        public int Total => _results.Count;

        /// <summary>Number of tests that passed.</summary>
        public int Passed => _results.Count(r => r.Status == TestStatus.Pass);

        /// <summary>Number of tests that failed.</summary>
        public int Failed => _results.Count(r => r.Status == TestStatus.Fail);

        /// <summary>Number of tests that were skipped.</summary>
        public int Skipped => _results.Count(r => r.Status == TestStatus.Skip);

        /// <summary>Number of tests that timed out.</summary>
        public int TimedOut => _results.Count(r => r.Status == TestStatus.Timeout);

        /// <summary>Total wall-clock duration of all tests (seconds).</summary>
        public float TotalDuration => _results.Sum(r => r.Duration);

        /// <summary>Short one-line summary string suitable for log output.</summary>
        public string Summary =>
            $"Total={Total} Passed={Passed} Failed={Failed} Skipped={Skipped} Timeout={TimedOut} ({TotalDuration:F2}s)";

        // ── Construction ──────────────────────────────────────────────────────

        /// <summary>Creates a new report from a list of results.</summary>
        /// <param name="results">Results to aggregate. Must not be null.</param>
        public IntegrationTestReport(IEnumerable<IntegrationTestResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            _results = results.ToList();
        }

        // ── Formatting ────────────────────────────────────────────────────────

        /// <summary>Returns a full human-readable test report.</summary>
        public string ToText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("  SWEF Integration Test Report");
            sb.AppendLine($"  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine($"  {Summary}");
            sb.AppendLine();

            // Per-module breakdown
            var byModule = _results
                .GroupBy(r => r.ModuleName)
                .OrderBy(g => g.Key);

            foreach (var group in byModule)
            {
                int pass = group.Count(r => r.Status == TestStatus.Pass);
                int fail = group.Count(r => r.Status == TestStatus.Fail);
                int skip = group.Count(r => r.Status == TestStatus.Skip);
                sb.AppendLine($"  [{group.Key}]  pass={pass} fail={fail} skip={skip}");

                foreach (var r in group)
                {
                    string icon = r.Status == TestStatus.Pass ? "✓"
                                : r.Status == TestStatus.Fail ? "✗"
                                : r.Status == TestStatus.Timeout ? "⏱"
                                : "○";
                    sb.AppendLine($"    {icon} {r.TestName} ({r.Duration:F3}s) — {r.Message}");
                }
            }

            sb.AppendLine("═══════════════════════════════════════════════════════");
            return sb.ToString();
        }

        /// <summary>Returns a minimal JSON representation of the report.</summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"generatedAt\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
            sb.AppendLine($"  \"total\": {Total},");
            sb.AppendLine($"  \"passed\": {Passed},");
            sb.AppendLine($"  \"failed\": {Failed},");
            sb.AppendLine($"  \"skipped\": {Skipped},");
            sb.AppendLine($"  \"timedOut\": {TimedOut},");
            sb.AppendLine($"  \"totalDuration\": {TotalDuration:F3},");
            sb.AppendLine("  \"results\": [");

            for (int i = 0; i < _results.Count; i++)
            {
                var r = _results[i];
                string comma = i < _results.Count - 1 ? "," : string.Empty;
                sb.AppendLine("    {");
                sb.AppendLine($"      \"module\": \"{Escape(r.ModuleName)}\",");
                sb.AppendLine($"      \"test\": \"{Escape(r.TestName)}\",");
                sb.AppendLine($"      \"status\": \"{r.Status}\",");
                sb.AppendLine($"      \"message\": \"{Escape(r.Message)}\",");
                sb.AppendLine($"      \"duration\": {r.Duration:F3}");
                sb.AppendLine($"    }}{comma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Writes the report to a file in the specified format.
        /// Directory is created if it does not exist.
        /// </summary>
        /// <param name="path">Absolute or relative file path.</param>
        /// <param name="json">When true, writes JSON; otherwise writes human-readable text.</param>
        public void WriteToFile(string path, bool json = false)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json ? ToJson() : ToText(), Encoding.UTF8);
            Debug.Log($"[IntegrationTestReport] Report written to: {path}");
        }

        /// <summary>Logs the full human-readable report to the Unity console.</summary>
        public void LogToConsole() => Debug.Log(ToText());

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Escape(string s) =>
            s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") ?? string.Empty;
    }
}
