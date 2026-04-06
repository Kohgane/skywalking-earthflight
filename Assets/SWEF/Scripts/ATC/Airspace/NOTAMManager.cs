// NOTAMManager.cs — Phase 119: Advanced AI Traffic Control
// NOTAM system: active NOTAMs affecting routing, temporary restrictions,
// runway closures.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages NOTAMs (Notices to Air Missions): runway closures,
    /// temporary restrictions, navaid outages, airspace reservations.
    /// </summary>
    public class NOTAMManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Shared instance of <see cref="NOTAMManager"/>.</summary>
        public static NOTAMManager Instance { get; private set; }

        // ── NOTAM ─────────────────────────────────────────────────────────────────

        /// <summary>A single NOTAM record.</summary>
        [Serializable]
        public class NOTAM
        {
            public string notamId;
            public string icao;
            public NOTAMType type;
            public string description;
            public float startTime;
            public float endTime;      // 0 = no expiry
            public bool isActive;

            /// <summary>Returns whether this NOTAM is currently in effect.</summary>
            public bool IsInEffect(float now)
                => isActive && now >= startTime && (endTime <= 0f || now <= endTime);
        }

        /// <summary>Classification of NOTAM.</summary>
        public enum NOTAMType
        {
            RunwayClosure,
            TaxiwayClosure,
            NavaidiOutage,
            AirspaceReservation,
            ObstructionLight,
            ConstructionHazard,
            Other
        }

        private readonly List<NOTAM> _notams = new List<NOTAM>();
        private int _nextId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Issues a new NOTAM.</summary>
        public NOTAM IssueNOTAM(string icao, NOTAMType type, string description,
            float durationSeconds = 0f)
        {
            var n = new NOTAM
            {
                notamId     = $"N{_nextId++:0000}",
                icao        = icao,
                type        = type,
                description = description,
                startTime   = Time.time,
                endTime     = durationSeconds > 0f ? Time.time + durationSeconds : 0f,
                isActive    = true
            };
            _notams.Add(n);
            return n;
        }

        /// <summary>Cancels a NOTAM by ID.</summary>
        public bool CancelNOTAM(string notamId)
        {
            var n = _notams.Find(x => x.notamId == notamId);
            if (n == null) return false;
            n.isActive = false;
            return true;
        }

        /// <summary>Returns all currently active NOTAMs for an airport.</summary>
        public List<NOTAM> GetActiveNOTAMs(string icao)
        {
            float now = Time.time;
            var result = new List<NOTAM>();
            foreach (var n in _notams)
                if (n.icao == icao && n.IsInEffect(now)) result.Add(n);
            return result;
        }

        /// <summary>Returns whether a specific runway is closed at an airport.</summary>
        public bool IsRunwayClosed(string icao, string runway)
        {
            float now = Time.time;
            foreach (var n in _notams)
                if (n.icao == icao && n.type == NOTAMType.RunwayClosure
                    && n.IsInEffect(now)
                    && n.description.Contains(runway)) return true;
            return false;
        }

        /// <summary>Total number of NOTAMs in the system.</summary>
        public int TotalCount => _notams.Count;
    }
}
