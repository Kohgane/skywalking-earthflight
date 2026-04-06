// ChallengeAirportSelector.cs — Phase 120: Precision Landing Challenge System
// Airport selection: curated list of challenging airports worldwide.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Provides a curated catalogue of challenging airports for
    /// landing challenges.  Includes notoriously difficult real-world destinations
    /// such as St. Maarten, Lukla, Gibraltar, and the Kai Tak curved approach.
    /// </summary>
    public class ChallengeAirportSelector : MonoBehaviour
    {
        // ── Airport Entry ─────────────────────────────────────────────────────

        [System.Serializable]
        public class AirportEntry
        {
            /// <summary>ICAO identifier.</summary>
            public string ICAO;

            /// <summary>Common name.</summary>
            public string Name;

            /// <summary>Country or region.</summary>
            public string Region;

            /// <summary>Elevation in feet MSL.</summary>
            public float ElevationFeet;

            /// <summary>Brief description of why it is challenging.</summary>
            public string ChallengeNote;

            /// <summary>Default challenge type suited to this airport.</summary>
            public ChallengeType RecommendedType;

            /// <summary>Minimum recommended difficulty.</summary>
            public DifficultyLevel MinDifficulty;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Curated Airport Catalogue")]
        [SerializeField] private List<AirportEntry> airports = new List<AirportEntry>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (airports.Count == 0) PopulateDefaults();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns all registered airport entries.</summary>
        public IReadOnlyList<AirportEntry> AllAirports => airports;

        /// <summary>Returns airports filtered by challenge type.</summary>
        public List<AirportEntry> GetByType(ChallengeType type)
        {
            return airports.FindAll(a => a.RecommendedType == type);
        }

        /// <summary>Returns airports filtered by minimum difficulty.</summary>
        public List<AirportEntry> GetByDifficulty(DifficultyLevel minDifficulty)
        {
            return airports.FindAll(a => a.MinDifficulty >= minDifficulty);
        }

        /// <summary>Returns the entry for a given ICAO code, or <c>null</c>.</summary>
        public AirportEntry GetByICAO(string icao)
        {
            return airports.Find(a => a.ICAO == icao);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void PopulateDefaults()
        {
            airports.Add(new AirportEntry { ICAO = "TNCM", Name = "Princess Juliana (St. Maarten)", Region = "Caribbean", ElevationFeet = 13f,   ChallengeNote = "Beach overfly, short runway",                    RecommendedType = ChallengeType.ShortField,       MinDifficulty = DifficultyLevel.Intermediate });
            airports.Add(new AirportEntry { ICAO = "VNLK", Name = "Tenzing-Hillary (Lukla)",        Region = "Nepal",     ElevationFeet = 9383f, ChallengeNote = "11.7° slope, 527m runway, one-way approach",     RecommendedType = ChallengeType.MountainApproach, MinDifficulty = DifficultyLevel.Advanced });
            airports.Add(new AirportEntry { ICAO = "LXGB", Name = "Gibraltar",                      Region = "Gibraltar", ElevationFeet = 15f,   ChallengeNote = "Road crosses runway, severe Levanter crosswind",  RecommendedType = ChallengeType.CrosswindLanding, MinDifficulty = DifficultyLevel.Advanced });
            airports.Add(new AirportEntry { ICAO = "VHHX", Name = "Kai Tak (historic)",              Region = "Hong Kong", ElevationFeet = 15f,   ChallengeNote = "IGS curved approach, checkerboard hill",         RecommendedType = ChallengeType.Standard,         MinDifficulty = DifficultyLevel.Expert });
            airports.Add(new AirportEntry { ICAO = "KJFK", Name = "JFK International",              Region = "USA",       ElevationFeet = 13f,   ChallengeNote = "Complex airspace, multiple intersecting runways", RecommendedType = ChallengeType.Standard,         MinDifficulty = DifficultyLevel.Beginner });
            airports.Add(new AirportEntry { ICAO = "EGPD", Name = "Aberdeen Dyce",                  Region = "Scotland",  ElevationFeet = 215f,  ChallengeNote = "Frequent low cloud and strong winds",             RecommendedType = ChallengeType.CrosswindLanding, MinDifficulty = DifficultyLevel.Intermediate });
            airports.Add(new AirportEntry { ICAO = "NZSP", Name = "McMurdo-South Pole",             Region = "Antarctica",ElevationFeet = 9301f, ChallengeNote = "Ice runway, extreme cold, no go-around terrain",  RecommendedType = ChallengeType.Standard,         MinDifficulty = DifficultyLevel.Legendary });
            airports.Add(new AirportEntry { ICAO = "CARRIER", Name = "CVN-68 Nimitz Class",         Region = "Ocean",     ElevationFeet = 0f,    ChallengeNote = "Pitching deck, arrestor wires, night ops",        RecommendedType = ChallengeType.CarrierLanding,   MinDifficulty = DifficultyLevel.Expert });
        }
    }
}
