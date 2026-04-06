// HistoricalDataStore.cs — Phase 116: Flight Analytics Dashboard
// Persistent local storage for flight session records (JSON-based).
// Namespace: SWEF.FlightAnalytics

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.FlightAnalytics
{
    /// <summary>
    /// Phase 116 — Persists <see cref="FlightSessionRecord"/> objects to the device's
    /// persistent data path as JSON files. Supports auto-cleanup of data older than
    /// the configured retention period.
    /// </summary>
    public class HistoricalDataStore : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [SerializeField] private FlightAnalyticsConfig config;

        // ── Constants ─────────────────────────────────────────────────────────────

        private const string StorageSubfolder = "FlightAnalytics/Sessions";

        // ── Properties ────────────────────────────────────────────────────────────

        private string StoragePath => Path.Combine(Application.persistentDataPath, StorageSubfolder);

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            Directory.CreateDirectory(StoragePath);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Persist a session record to disk.</summary>
        public void SaveSession(FlightSessionRecord session)
        {
            if (session == null) return;

            try
            {
                string json = JsonUtility.ToJson(session, prettyPrint: false);
                string path = Path.Combine(StoragePath, $"{session.sessionId}.json");
                File.WriteAllText(path, json);
                Debug.Log($"[SWEF] HistoricalDataStore: Saved session {session.sessionId}.");
                PurgeOldSessions();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] HistoricalDataStore: Failed to save session — {e.Message}");
            }
        }

        /// <summary>Load all session records filtered by the given time range.</summary>
        public List<FlightSessionRecord> GetSessions(TimeRange range)
        {
            var result = new List<FlightSessionRecord>();
            long cutoff = GetCutoffTimestamp(range);

            try
            {
                foreach (string file in Directory.GetFiles(StoragePath, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    var session = JsonUtility.FromJson<FlightSessionRecord>(json);
                    if (session != null && session.startTimeUtc >= cutoff)
                        result.Add(session);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF] HistoricalDataStore: Failed to load sessions — {e.Message}");
            }

            result.Sort((a, b) => b.startTimeUtc.CompareTo(a.startTimeUtc));
            return result;
        }

        /// <summary>Delete a specific session record by ID.</summary>
        public bool DeleteSession(string sessionId)
        {
            string path = Path.Combine(StoragePath, $"{sessionId}.json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        /// <summary>Remove sessions older than the configured retention period.</summary>
        public void PurgeOldSessions()
        {
            int retentionDays = config != null ? config.dataRetentionDays : 90;
            int maxSessions   = config != null ? config.maxStoredSessions : 500;

            if (retentionDays > 0)
            {
                long cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays).ToUnixTimeSeconds();
                foreach (string file in Directory.GetFiles(StoragePath, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var session = JsonUtility.FromJson<FlightSessionRecord>(json);
                        if (session != null && session.startTimeUtc < cutoff)
                            File.Delete(file);
                    }
                    catch { /* skip corrupt files */ }
                }
            }

            if (maxSessions > 0)
            {
                var files = new List<string>(Directory.GetFiles(StoragePath, "*.json"));
                if (files.Count > maxSessions)
                {
                    files.Sort((a, b) => File.GetLastWriteTimeUtc(a).CompareTo(File.GetLastWriteTimeUtc(b)));
                    for (int i = 0; i < files.Count - maxSessions; i++)
                        File.Delete(files[i]);
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static long GetCutoffTimestamp(TimeRange range)
        {
            var now = DateTimeOffset.UtcNow;
            return range switch
            {
                TimeRange.LastFlight => 0L,      // caller filters by taking the latest
                TimeRange.Today      => new DateTimeOffset(now.Date, TimeSpan.Zero).ToUnixTimeSeconds(),
                TimeRange.Week       => now.AddDays(-7).ToUnixTimeSeconds(),
                TimeRange.Month      => now.AddDays(-30).ToUnixTimeSeconds(),
                TimeRange.AllTime    => 0L,
                _                    => 0L
            };
        }
    }
}
