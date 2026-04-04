// HistoricalAircraftData.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>Era classification for historical aircraft.</summary>
    public enum AircraftEra
    {
        /// <summary>Pioneer aviation era (pre-WWI, 1900–1913).</summary>
        Pioneer,
        /// <summary>Inter-war golden age of aviation (1919–1938).</summary>
        GoldenAge,
        /// <summary>World War II era aircraft (1939–1945).</summary>
        WorldWarII,
        /// <summary>Cold War jet age (1946–1991).</summary>
        ColdWar,
        /// <summary>Supersonic commercial aviation era.</summary>
        Supersonic,
        /// <summary>Space-capable vehicles and spacecraft.</summary>
        SpaceAge,
    }

    /// <summary>Special ability that an aircraft may possess.</summary>
    public enum AircraftSpecialAbility
    {
        /// <summary>No special ability.</summary>
        None,
        /// <summary>Aircraft can sustain supersonic flight without damage.</summary>
        SupersonicCruise,
        /// <summary>High-altitude reconnaissance capability above 25 km.</summary>
        HighAltitudeRecon,
        /// <summary>Atmospheric re-entry and glide capability.</summary>
        OrbitalReentry,
        /// <summary>Historical significance grants bonus mission rewards.</summary>
        HistoricalIconStatus,
        /// <summary>Can operate in low/no-atmosphere environments.</summary>
        SpaceCapable,
        /// <summary>Exceptional roll and pitch agility.</summary>
        SuperiorManeuverability,
    }

    /// <summary>
    /// Immutable data record for a historical aircraft entry in the registry.
    /// Stores all era-appropriate flight characteristics and metadata.
    /// </summary>
    [Serializable]
    public sealed class HistoricalAircraftData
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Unique machine-readable identifier (e.g. "wright_flyer").</summary>
        public string id;

        /// <summary>Human-readable display name.</summary>
        public string displayName;

        /// <summary>Year of first flight or introduction.</summary>
        public int year;

        /// <summary>Historical era classification.</summary>
        public AircraftEra era;

        /// <summary>Short description shown in the selection UI.</summary>
        [TextArea(2, 4)]
        public string description;

        // ── Flight Characteristics ────────────────────────────────────────────

        /// <summary>Maximum sustained airspeed in km/h.</summary>
        public float maxSpeedKph;

        /// <summary>Service ceiling in metres above sea level.</summary>
        public float maxAltitudeMetres;

        /// <summary>
        /// Maneuverability rating on a 0–10 scale.
        /// Higher values indicate sharper, more responsive handling.
        /// </summary>
        [Range(0f, 10f)]
        public float maneuverabilityRating;

        /// <summary>
        /// Fuel efficiency rating on a 0–10 scale.
        /// Higher values indicate longer range per unit of fuel.
        /// </summary>
        [Range(0f, 10f)]
        public float fuelEfficiency;

        // ── Unlock / Progression ─────────────────────────────────────────────

        /// <summary>Whether the aircraft is unlocked by default without any prerequisites.</summary>
        public bool unlockedByDefault;

        /// <summary>
        /// Optional prerequisite aircraft IDs that must be flown before this
        /// aircraft becomes available for selection.
        /// </summary>
        public List<string> prerequisiteAircraftIds = new List<string>();

        // ── Special Abilities ────────────────────────────────────────────────

        /// <summary>List of special abilities granted by this aircraft.</summary>
        public List<AircraftSpecialAbility> specialAbilities = new List<AircraftSpecialAbility>();

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fully-populated <see cref="HistoricalAircraftData"/> instance.
        /// </summary>
        public static HistoricalAircraftData Create(
            string id,
            string displayName,
            int year,
            AircraftEra era,
            string description,
            float maxSpeedKph,
            float maxAltitudeMetres,
            float maneuverabilityRating,
            float fuelEfficiency,
            bool unlockedByDefault = false,
            List<AircraftSpecialAbility> specialAbilities = null)
        {
            return new HistoricalAircraftData
            {
                id                    = id,
                displayName           = displayName,
                year                  = year,
                era                   = era,
                description           = description,
                maxSpeedKph           = maxSpeedKph,
                maxAltitudeMetres     = maxAltitudeMetres,
                maneuverabilityRating = Mathf.Clamp(maneuverabilityRating, 0f, 10f),
                fuelEfficiency        = Mathf.Clamp(fuelEfficiency, 0f, 10f),
                unlockedByDefault     = unlockedByDefault,
                specialAbilities      = specialAbilities ?? new List<AircraftSpecialAbility>(),
            };
        }

        /// <summary>
        /// Returns <c>true</c> when this aircraft possesses the given special ability.
        /// </summary>
        public bool HasAbility(AircraftSpecialAbility ability) =>
            specialAbilities != null && specialAbilities.Contains(ability);
    }
}
