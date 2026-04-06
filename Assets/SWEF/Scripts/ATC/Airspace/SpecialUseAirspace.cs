// SpecialUseAirspace.cs — Phase 119: Advanced AI Traffic Control
// Special use airspace: military operating areas, restricted zones,
// prohibited areas, TFRs.
// Namespace: SWEF.ATC

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages special use airspace (SUA): MOAs, restricted areas,
    /// prohibited areas and temporary flight restrictions (TFRs).
    /// </summary>
    public class SpecialUseAirspace : MonoBehaviour
    {
        // ── SUA Type ──────────────────────────────────────────────────────────────

        /// <summary>Type of special use airspace.</summary>
        public enum SUAType
        {
            /// <summary>Military Operating Area — military training activities.</summary>
            MOA,
            /// <summary>Restricted area — potentially hazardous operations, requires ATC clearance.</summary>
            Restricted,
            /// <summary>Prohibited area — flight not permitted (e.g. White House).</summary>
            Prohibited,
            /// <summary>Warning area — potentially hazardous, international waters.</summary>
            Warning,
            /// <summary>Temporary Flight Restriction — event/VIP/disaster.</summary>
            TFR,
            /// <summary>Air Defense Identification Zone.</summary>
            ADIZ
        }

        // ── SUA Zone ──────────────────────────────────────────────────────────────

        /// <summary>A special use airspace zone.</summary>
        [Serializable]
        public class SUAZone
        {
            public string zoneId;
            public string name;
            public SUAType suaType;
            public Vector3 center;
            public float radiusNM;
            public int lowerLimitFt;
            public int upperLimitFt;
            public bool isActive;
            public float activeTo;   // 0 = permanent

            /// <summary>Returns true if the given position/altitude is within the active SUA.</summary>
            public bool Contains(Vector3 pos, float altFt)
            {
                if (!isActive) return false;
                if (activeTo > 0f && Time.time > activeTo) return false;

                float distNM = Vector3.Distance(
                    new Vector3(pos.x, 0, pos.z),
                    new Vector3(center.x, 0, center.z)) / 1852f;
                return distNM <= radiusNM && altFt >= lowerLimitFt && altFt <= upperLimitFt;
            }
        }

        private readonly List<SUAZone> _zones = new List<SUAZone>();
        private int _nextTFRId = 1;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a special use airspace zone.</summary>
        public void RegisterZone(SUAZone zone)
        {
            if (zone != null) _zones.Add(zone);
        }

        /// <summary>Issues a TFR (temporary flight restriction).</summary>
        public SUAZone IssueTFR(Vector3 center, float radiusNM, int lowerFt, int upperFt,
            float durationSeconds = 3600f)
        {
            var tfr = new SUAZone
            {
                zoneId     = $"TFR{_nextTFRId}",
                name       = $"TFR {_nextTFRId}",
                suaType    = SUAType.TFR,
                center     = center,
                radiusNM   = radiusNM,
                lowerLimitFt = lowerFt,
                upperLimitFt = upperFt,
                isActive   = true,
                activeTo   = Time.time + durationSeconds
            };
            _nextTFRId++;
            _zones.Add(tfr);
            return tfr;
        }

        /// <summary>Returns all active SUA zones at a given position and altitude.</summary>
        public List<SUAZone> GetActiveSUAAt(Vector3 pos, float altFt)
        {
            var result = new List<SUAZone>();
            foreach (var z in _zones)
                if (z.Contains(pos, altFt)) result.Add(z);
            return result;
        }

        /// <summary>Returns whether a given position/altitude is inside a prohibited area.</summary>
        public bool IsProhibited(Vector3 pos, float altFt)
        {
            foreach (var z in _zones)
                if (z.suaType == SUAType.Prohibited && z.Contains(pos, altFt)) return true;
            return false;
        }

        /// <summary>Returns whether a given position/altitude is inside an active TFR.</summary>
        public bool IsInTFR(Vector3 pos, float altFt)
        {
            foreach (var z in _zones)
                if (z.suaType == SUAType.TFR && z.Contains(pos, altFt)) return true;
            return false;
        }

        /// <summary>Number of registered SUA zones.</summary>
        public int ZoneCount => _zones.Count;
    }
}
