using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HiddenGems
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Broad category of a hidden gem location.</summary>
    public enum GemCategory
    {
        NaturalWonder,
        AncientRuin,
        SecretBeach,
        HiddenWaterfall,
        UndergroundCave,
        AbandonedStructure,
        SacredSite,
        GeologicalFormation,
        HiddenVillage,
        MysteriousLandmark,
        UnexploredIsland,
        ForgottenTemple,
        NaturalArch,
        VolcanicFormation,
        IceFormation
    }

    /// <summary>Rarity tier of a hidden gem — influences discovery radius and XP reward.</summary>
    public enum GemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>Continent on which the gem is located.</summary>
    public enum GemContinent
    {
        Asia,
        Europe,
        NorthAmerica,
        SouthAmerica,
        Africa,
        Oceania,
        Antarctica
    }

    // ── HiddenGemDefinition ───────────────────────────────────────────────────────

    /// <summary>
    /// Immutable design-time definition for a single hidden gem location.
    /// Loaded from <see cref="HiddenGemDatabase"/> at runtime.
    /// </summary>
    [Serializable]
    public class HiddenGemDefinition
    {
        /// <summary>Unique identifier, e.g. "gem_trolltunga_norway".</summary>
        public string gemId;

        /// <summary>Localization key for the display name.</summary>
        public string nameKey;

        /// <summary>Localization key for the lore/description text.</summary>
        public string descriptionKey;

        /// <summary>Localization key for the fun fact.</summary>
        public string factKey;

        /// <summary>Broad category of this location.</summary>
        public GemCategory category;

        /// <summary>Rarity tier — affects radius and XP.</summary>
        public GemRarity rarity;

        /// <summary>Continent on which this gem sits.</summary>
        public GemContinent continent;

        /// <summary>Country name string, e.g. "Norway".</summary>
        public string country;

        /// <summary>WGS-84 latitude in decimal degrees.</summary>
        public double latitude;

        /// <summary>WGS-84 longitude in decimal degrees.</summary>
        public double longitude;

        /// <summary>Suggested flyover altitude in metres above sea level.</summary>
        public float altitudeHint;

        /// <summary>
        /// Distance in metres at which the player triggers discovery.
        /// Defaults per rarity: Common=600, Uncommon=500, Rare=400, Epic=300, Legendary=200.
        /// </summary>
        public float discoveryRadiusMeters;

        /// <summary>
        /// XP awarded on first discovery.
        /// Defaults per rarity: Common=50, Uncommon=100, Rare=200, Epic=500, Legendary=1000.
        /// </summary>
        public int xpReward;

        /// <summary>Optional custom minimap icon identifier. Empty = use default PoI icon.</summary>
        public string iconOverride;

        /// <summary>If true a vague hint blip is shown on the minimap before discovery.</summary>
        public bool isHintVisible;

        /// <summary>
        /// Optional unlock requirement key, e.g. "discover_5_asia".
        /// Empty = no requirement.
        /// </summary>
        public string unlockRequirement;

        // ── Derived helpers ───────────────────────────────────────────────────────

        /// <summary>Default discovery radius for the given rarity.</summary>
        public static float DefaultRadius(GemRarity r) => r switch
        {
            GemRarity.Common    => 600f,
            GemRarity.Uncommon  => 500f,
            GemRarity.Rare      => 400f,
            GemRarity.Epic      => 300f,
            GemRarity.Legendary => 200f,
            _                   => 500f
        };

        /// <summary>Default XP reward for the given rarity.</summary>
        public static int DefaultXP(GemRarity r) => r switch
        {
            GemRarity.Common    => 50,
            GemRarity.Uncommon  => 100,
            GemRarity.Rare      => 200,
            GemRarity.Epic      => 500,
            GemRarity.Legendary => 1000,
            _                   => 50
        };

        /// <summary>Hex colour string representing this rarity.</summary>
        public static string RarityColor(GemRarity r) => r switch
        {
            GemRarity.Common    => "#AAAAAA",
            GemRarity.Uncommon  => "#1EFF00",
            GemRarity.Rare      => "#0070FF",
            GemRarity.Epic      => "#A335EE",
            GemRarity.Legendary => "#FF8000",
            _                   => "#FFFFFF"
        };
    }

    // ── HiddenGemState ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mutable per-player state for a single hidden gem.
    /// Persisted to <c>Application.persistentDataPath/hidden_gems.json</c>.
    /// </summary>
    [Serializable]
    public class HiddenGemState
    {
        /// <summary>Matches <see cref="HiddenGemDefinition.gemId"/>.</summary>
        public string gemId;

        /// <summary>Whether the player has discovered this gem at least once.</summary>
        public bool isDiscovered;

        /// <summary>ISO 8601 timestamp of first discovery, empty if not yet discovered.</summary>
        public string discoveredDate;

        /// <summary>Player altitude (m) at the moment of first discovery.</summary>
        public float discoveryAltitude;

        /// <summary>Player speed (m/s) at the moment of first discovery.</summary>
        public float discoverySpeed;

        /// <summary>Whether the player has marked this gem as a favourite.</summary>
        public bool isFavorited;

        /// <summary>Number of times the player has flown within the discovery radius.</summary>
        public int timesVisited;

        /// <summary>Whether the player has taken a screenshot at this location.</summary>
        public bool photoTaken;
    }

    // ── GemDiscoveryEvent ─────────────────────────────────────────────────────────

    /// <summary>
    /// Payload fired by <see cref="HiddenGemManager.OnGemDiscovered"/>.
    /// </summary>
    public class GemDiscoveryEvent
    {
        /// <summary>Definition of the gem that was discovered.</summary>
        public HiddenGemDefinition gem;

        /// <summary>Updated state after discovery.</summary>
        public HiddenGemState state;

        /// <summary>UTC timestamp of the discovery moment.</summary>
        public DateTime timestamp;
    }
}
