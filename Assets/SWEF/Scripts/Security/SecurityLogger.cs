// SecurityLogger.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Singleton that persists all security events to a rolling JSON log file
    /// (<c>security_log.json</c>) and exposes query / export helpers.
    ///
    /// <para>The log is capped at <see cref="MaxLogEntries"/> entries; oldest events
    /// are evicted first when the cap is reached.</para>
    /// </summary>
    public class SecurityLogger : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance.</summary>
        public static SecurityLogger Instance { get; private set; }
        #endregion

        #region Constants
        private const string FileName      = "security_log.json";
        private const int    MaxLogEntries = 1000;
        #endregion

        #region Inspector
        [SerializeField, Tooltip("Minimum severity to write to the log. Events below this level are discarded.")]
        private SecuritySeverity _minimumSeverity = SecuritySeverity.Low;
        #endregion

        #region Private state
        private readonly List<SecurityEventData> _events = new List<SecurityEventData>();
        private string _persistencePath;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _persistencePath = Path.Combine(Application.persistentDataPath, FileName);
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Appends a <see cref="SecurityEventData"/> to the log and persists immediately.
        /// Events whose severity is below <see cref="_minimumSeverity"/> are silently discarded.
        /// </summary>
        /// <param name="evt">Event to log.</param>
        public void LogEvent(SecurityEventData evt)
        {
            if (evt == null) return;
            if (evt.severity < _minimumSeverity) return;

            _events.Add(evt);

            // Evict oldest entries when cap is reached
            while (_events.Count > MaxLogEntries)
                _events.RemoveAt(0);

            Save();
        }

        /// <summary>Returns the most recent <paramref name="count"/> events.</summary>
        /// <param name="count">Number of events to return.</param>
        public List<SecurityEventData> GetRecentEvents(int count)
        {
            int skip = Mathf.Max(0, _events.Count - count);
            return _events.GetRange(skip, _events.Count - skip);
        }

        /// <summary>Returns all events matching the given <paramref name="type"/>.</summary>
        /// <param name="type">Event type filter.</param>
        public List<SecurityEventData> GetEventsByType(SecurityEventType type)
        {
            return _events.Where(e => e.eventType == type).ToList();
        }

        /// <summary>
        /// Exports a JSON summary of the current log suitable for debugging or support.
        /// </summary>
        /// <returns>Formatted JSON string.</returns>
        public string ExportSecurityReport()
        {
            var report = new SecurityReport
            {
                generatedAt   = DateTime.UtcNow.ToString("o"),
                totalEvents   = _events.Count,
                criticalCount = _events.Count(e => e.severity == SecuritySeverity.Critical),
                highCount     = _events.Count(e => e.severity == SecuritySeverity.High),
                mediumCount   = _events.Count(e => e.severity == SecuritySeverity.Medium),
                lowCount      = _events.Count(e => e.severity == SecuritySeverity.Low),
                recentEvents  = GetRecentEvents(50)
            };
            return JsonUtility.ToJson(report, prettyPrint: true);
        }
        #endregion

        #region Persistence
        private void Save()
        {
            try
            {
                var wrapper = new LogWrapper { events = _events };
                File.WriteAllText(_persistencePath,
                    JsonUtility.ToJson(wrapper, prettyPrint: false),
                    System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: SecurityLogger save failed — {ex.Message}");
            }
        }

        private void Load()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json    = File.ReadAllText(_persistencePath, System.Text.Encoding.UTF8);
                var    wrapper = JsonUtility.FromJson<LogWrapper>(json);
                if (wrapper?.events != null)
                    _events.AddRange(wrapper.events);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Security: SecurityLogger load failed — {ex.Message}");
            }
        }
        #endregion

        #region Serialisation helpers
        [Serializable]
        private class LogWrapper
        {
            public List<SecurityEventData> events;
        }

        [Serializable]
        private class SecurityReport
        {
            public string                  generatedAt;
            public int                     totalEvents;
            public int                     criticalCount;
            public int                     highCount;
            public int                     mediumCount;
            public int                     lowCount;
            public List<SecurityEventData> recentEvents;
        }
        #endregion
    }
}
