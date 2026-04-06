// RadioFrequencyManager.cs — Phase 119: Advanced AI Traffic Control
// Frequency management: ATIS, ground, tower, approach, departure, center
// frequencies per airport.
// Namespace: SWEF.ATC

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ATC
{
    /// <summary>
    /// Phase 119 — Manages radio frequency assignments for airports and
    /// en-route sectors, providing lookup and tuning helpers.
    /// </summary>
    public class RadioFrequencyManager : MonoBehaviour
    {
        // ── Frequency Record ──────────────────────────────────────────────────────

        private class FrequencyRecord
        {
            public string icao;
            public ATCFacilityType type;
            public float frequencyMHz;
            public string facilityName;
        }

        private readonly List<FrequencyRecord> _records = new List<FrequencyRecord>();
        private float _activeFrequencyMHz = 122.8f;

        private void Awake()
        {
            SeedFrequencies();
        }

        private void SeedFrequencies()
        {
            Add("KLAX", ATCFacilityType.ATIS,      135.65f, "LAX ATIS");
            Add("KLAX", ATCFacilityType.Ground,    121.65f, "LA Ground");
            Add("KLAX", ATCFacilityType.Tower,     133.9f,  "LA Tower");
            Add("KLAX", ATCFacilityType.Approach,  124.5f,  "SoCal Approach");
            Add("KLAX", ATCFacilityType.Departure, 125.2f,  "SoCal Departure");
            Add("KLAX", ATCFacilityType.Center,    134.5f,  "LA Center");

            Add("KJFK", ATCFacilityType.ATIS,      128.725f,"JFK ATIS");
            Add("KJFK", ATCFacilityType.Ground,    121.9f,  "JFK Ground");
            Add("KJFK", ATCFacilityType.Tower,     119.1f,  "JFK Tower");
            Add("KJFK", ATCFacilityType.Approach,  127.4f,  "NY Approach");

            Add("EGLL", ATCFacilityType.ATIS,      113.75f, "Heathrow ATIS");
            Add("EGLL", ATCFacilityType.Tower,     118.5f,  "Heathrow Tower");
            Add("EGLL", ATCFacilityType.Approach,  119.725f,"Heathrow Approach");

            Add("ZZZZ", ATCFacilityType.Emergency, 121.5f,  "Guard");
        }

        private void Add(string icao, ATCFacilityType type, float freq, string name)
        {
            _records.Add(new FrequencyRecord { icao = icao, type = type, frequencyMHz = freq, facilityName = name });
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the frequency for a specific airport and facility type.</summary>
        public float GetFrequency(string icao, ATCFacilityType type)
        {
            var r = _records.Find(x => x.icao == icao && x.type == type);
            return r?.frequencyMHz ?? 0f;
        }

        /// <summary>Tunes the radio to the specified frequency.</summary>
        public void TuneFrequency(float frequencyMHz)
        {
            _activeFrequencyMHz = frequencyMHz;
        }

        /// <summary>Currently tuned frequency.</summary>
        public float ActiveFrequency => _activeFrequencyMHz;

        /// <summary>Number of registered frequency records.</summary>
        public int RecordCount => _records.Count;

        /// <summary>Returns all frequencies registered for a given ICAO airport.</summary>
        public List<(ATCFacilityType, float)> GetAllFrequencies(string icao)
        {
            var result = new List<(ATCFacilityType, float)>();
            foreach (var r in _records)
                if (r.icao == icao) result.Add((r.type, r.frequencyMHz));
            return result;
        }
    }
}
