// ProximityWarningSystem.cs — Phase 119: Advanced AI Traffic Control
// Multi-level proximity warnings: informational, caution, warning, critical
// with escalation.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Multi-level proximity warning system that escalates alerts
    /// as aircraft converge, from informational through to critical.
    /// </summary>
    public class ProximityWarningSystem : MonoBehaviour
    {
        // ── Warning Level ─────────────────────────────────────────────────────────

        /// <summary>Proximity warning level.</summary>
        public enum WarningLevel
        {
            /// <summary>No active warning.</summary>
            None,
            /// <summary>Informational — traffic in vicinity.</summary>
            Informational,
            /// <summary>Caution — traffic within advisory range.</summary>
            Caution,
            /// <summary>Warning — imminent conflict.</summary>
            Warning,
            /// <summary>Critical — immediate action required.</summary>
            Critical
        }

        // ── Warning Entry ─────────────────────────────────────────────────────────

        private class WarningEntry
        {
            public string ownCallsign;
            public string targetCallsign;
            public WarningLevel level;
            public float lastUpdateTime;
        }

        private readonly List<WarningEntry> _warnings = new List<WarningEntry>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the warning level changes for a pair of aircraft.</summary>
        public event Action<string, string, WarningLevel> OnWarningLevelChanged;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates and updates the proximity warning level between an ownship
        /// and a target aircraft.
        /// </summary>
        public WarningLevel Evaluate(
            string own, Vector3 ownPos, float ownAlt,
            string target, Vector3 targetPos, float targetAlt)
        {
            float horizNM = Vector3.Distance(
                new Vector3(ownPos.x, 0, ownPos.z),
                new Vector3(targetPos.x, 0, targetPos.z)) / 1852f;
            float vertFt = Mathf.Abs(ownAlt - targetAlt);

            WarningLevel level;
            if      (horizNM < 1f  && vertFt < 300f)  level = WarningLevel.Critical;
            else if (horizNM < 2f  && vertFt < 500f)  level = WarningLevel.Warning;
            else if (horizNM < 5f  && vertFt < 800f)  level = WarningLevel.Caution;
            else if (horizNM < 10f && vertFt < 1200f) level = WarningLevel.Informational;
            else                                        level = WarningLevel.None;

            UpdateWarning(own, target, level);
            return level;
        }

        private void UpdateWarning(string own, string target, WarningLevel newLevel)
        {
            string key = own + "|" + target;
            var entry = _warnings.Find(w => w.ownCallsign == own && w.targetCallsign == target);

            if (entry == null)
            {
                if (newLevel == WarningLevel.None) return;
                entry = new WarningEntry { ownCallsign = own, targetCallsign = target, level = WarningLevel.None };
                _warnings.Add(entry);
            }

            if (entry.level != newLevel)
            {
                entry.level = newLevel;
                OnWarningLevelChanged?.Invoke(own, target, newLevel);
            }
            entry.lastUpdateTime = Time.time;

            if (newLevel == WarningLevel.None)
                _warnings.Remove(entry);
        }

        /// <summary>Returns the current warning level for a specific pair, or None.</summary>
        public WarningLevel GetWarningLevel(string own, string target)
        {
            var e = _warnings.Find(w => w.ownCallsign == own && w.targetCallsign == target);
            return e?.level ?? WarningLevel.None;
        }

        /// <summary>Number of active proximity warnings.</summary>
        public int ActiveWarningCount => _warnings.Count;

        /// <summary>Clears all stale warnings older than the timeout.</summary>
        public void ClearStaleWarnings(float timeoutSeconds = 10f)
        {
            _warnings.RemoveAll(w => Time.time - w.lastUpdateTime > timeoutSeconds);
        }
    }
}
