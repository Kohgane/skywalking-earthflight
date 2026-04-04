// HistoricalAircraftRegistry.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>
    /// Phase 106 — Registry/database of all historical aircraft available in SWEF.
    /// Provides built-in definitions for iconic aircraft spanning the Pioneer era through
    /// the Space Age, and exposes query/unlock APIs used by
    /// <see cref="HistoricalSciFiModeManager"/>.
    ///
    /// <para>The registry is self-contained (no external data files required) and is
    /// populated lazily on first access via <see cref="Instance"/>.</para>
    /// </summary>
    public sealed class HistoricalAircraftRegistry
    {
        #region Singleton

        private static HistoricalAircraftRegistry _instance;

        /// <summary>Global singleton instance. Initialised on first access.</summary>
        public static HistoricalAircraftRegistry Instance =>
            _instance ?? (_instance = new HistoricalAircraftRegistry());

        #endregion

        #region Registry Data

        private readonly Dictionary<string, HistoricalAircraftData> _registry =
            new Dictionary<string, HistoricalAircraftData>(StringComparer.Ordinal);

        private readonly HashSet<string> _unlockedIds =
            new HashSet<string>(StringComparer.Ordinal);

        #endregion

        #region Well-Known IDs

        /// <summary>Aircraft ID: Wright Flyer (1903).</summary>
        public const string IdWrightFlyer      = "wright_flyer";
        /// <summary>Aircraft ID: Spirit of St. Louis (1927).</summary>
        public const string IdSpiritOfStLouis  = "spirit_of_st_louis";
        /// <summary>Aircraft ID: Spitfire (1938–WWII).</summary>
        public const string IdSpitfire         = "spitfire";
        /// <summary>Aircraft ID: SR-71 Blackbird (1966).</summary>
        public const string IdSR71Blackbird    = "sr71_blackbird";
        /// <summary>Aircraft ID: Concorde (1969).</summary>
        public const string IdConcorde         = "concorde";
        /// <summary>Aircraft ID: Space Shuttle (1981).</summary>
        public const string IdSpaceShuttle     = "space_shuttle";

        #endregion

        #region Constructor

        private HistoricalAircraftRegistry()
        {
            PopulateBuiltInAircraft();
        }

        #endregion

        #region Public API

        /// <summary>Returns the total number of aircraft in the registry.</summary>
        public int Count => _registry.Count;

        /// <summary>Returns all registered aircraft data records.</summary>
        public IReadOnlyCollection<HistoricalAircraftData> All => _registry.Values;

        /// <summary>
        /// Returns all aircraft data records for a given <paramref name="era"/>.
        /// </summary>
        public IEnumerable<HistoricalAircraftData> GetByEra(AircraftEra era) =>
            _registry.Values.Where(a => a.era == era);

        /// <summary>
        /// Looks up an aircraft by its unique ID.
        /// Returns <c>null</c> if no match is found.
        /// </summary>
        public HistoricalAircraftData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _registry.TryGetValue(id, out var data);
            return data;
        }

        /// <summary>
        /// Returns all aircraft that are currently unlocked for the player.
        /// </summary>
        public IEnumerable<HistoricalAircraftData> GetUnlocked() =>
            _registry.Values.Where(a => _unlockedIds.Contains(a.id));

        /// <summary>Returns <c>true</c> when the aircraft with the given ID is unlocked.</summary>
        public bool IsUnlocked(string id) =>
            !string.IsNullOrEmpty(id) && _unlockedIds.Contains(id);

        /// <summary>
        /// Unlocks the aircraft with the given ID.
        /// </summary>
        /// <param name="id">Aircraft ID to unlock.</param>
        /// <returns><c>true</c> if the aircraft was found and unlocked; <c>false</c> otherwise.</returns>
        public bool Unlock(string id)
        {
            if (!_registry.ContainsKey(id)) return false;
            _unlockedIds.Add(id);
            return true;
        }

        /// <summary>
        /// Registers a custom aircraft data record.  Overwrites any existing record
        /// with the same ID.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="data"/> or its ID is null/empty.
        /// </exception>
        public void Register(HistoricalAircraftData data)
        {
            if (data == null)           throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(data.id))
                throw new ArgumentNullException(nameof(data), "Aircraft ID must not be null or empty.");

            _registry[data.id] = data;
            if (data.unlockedByDefault) _unlockedIds.Add(data.id);
        }

        #endregion

        #region Built-In Aircraft

        private void PopulateBuiltInAircraft()
        {
            var aircraft = new[]
            {
                // ── Wright Flyer — Pioneer Era ────────────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdWrightFlyer,
                    displayName:           "Wright Flyer",
                    year:                  1903,
                    era:                   AircraftEra.Pioneer,
                    description:           "The world's first successful powered aeroplane, flown by the Wright Brothers at Kitty Hawk on 17 December 1903.",
                    maxSpeedKph:           48f,
                    maxAltitudeMetres:     9f,
                    maneuverabilityRating: 3f,
                    fuelEfficiency:        4f,
                    unlockedByDefault:     true,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.HistoricalIconStatus }),

                // ── Spirit of St. Louis — Golden Age ─────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdSpiritOfStLouis,
                    displayName:           "Spirit of St. Louis",
                    year:                  1927,
                    era:                   AircraftEra.GoldenAge,
                    description:           "Charles Lindbergh's Ryan NYP monoplane — the first aircraft to complete a non-stop solo transatlantic flight.",
                    maxSpeedKph:           209f,
                    maxAltitudeMetres:     4570f,
                    maneuverabilityRating: 4f,
                    fuelEfficiency:        6f,
                    unlockedByDefault:     false,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.HistoricalIconStatus }),

                // ── Supermarine Spitfire — WWII ───────────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdSpitfire,
                    displayName:           "Supermarine Spitfire",
                    year:                  1938,
                    era:                   AircraftEra.WorldWarII,
                    description:           "Iconic British single-seat fighter renowned for its elliptical wing and pivotal role in the Battle of Britain.",
                    maxSpeedKph:           594f,
                    maxAltitudeMetres:     11_125f,
                    maneuverabilityRating: 9f,
                    fuelEfficiency:        5f,
                    unlockedByDefault:     false,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.SuperiorManeuverability, AircraftSpecialAbility.HistoricalIconStatus }),

                // ── SR-71 Blackbird — Cold War ────────────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdSR71Blackbird,
                    displayName:           "SR-71 Blackbird",
                    year:                  1966,
                    era:                   AircraftEra.ColdWar,
                    description:           "Lockheed's legendary Mach 3+ strategic reconnaissance aircraft — the fastest manned airbreathing jet ever built.",
                    maxSpeedKph:           3_540f,
                    maxAltitudeMetres:     25_908f,
                    maneuverabilityRating: 5f,
                    fuelEfficiency:        3f,
                    unlockedByDefault:     false,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.SupersonicCruise, AircraftSpecialAbility.HighAltitudeRecon }),

                // ── Concorde — Supersonic ─────────────────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdConcorde,
                    displayName:           "Concorde",
                    year:                  1969,
                    era:                   AircraftEra.Supersonic,
                    description:           "Anglo-French supersonic airliner that carried passengers across the Atlantic at Mach 2, cutting journey times in half.",
                    maxSpeedKph:           2_179f,
                    maxAltitudeMetres:     18_300f,
                    maneuverabilityRating: 5f,
                    fuelEfficiency:        2f,
                    unlockedByDefault:     false,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.SupersonicCruise }),

                // ── Space Shuttle — Space Age ─────────────────────────────────
                HistoricalAircraftData.Create(
                    id:                    IdSpaceShuttle,
                    displayName:           "Space Shuttle",
                    year:                  1981,
                    era:                   AircraftEra.SpaceAge,
                    description:           "NASA's reusable orbital spacecraft that operated from 1981 to 2011, deploying satellites and building the ISS.",
                    maxSpeedKph:           28_000f,
                    maxAltitudeMetres:     400_000f,
                    maneuverabilityRating: 4f,
                    fuelEfficiency:        1f,
                    unlockedByDefault:     false,
                    specialAbilities:      new List<AircraftSpecialAbility> { AircraftSpecialAbility.OrbitalReentry, AircraftSpecialAbility.SpaceCapable }),
            };

            foreach (var a in aircraft)
                Register(a);
        }

        #endregion
    }
}
