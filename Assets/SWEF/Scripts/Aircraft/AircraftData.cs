using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Aircraft
{
    /// <summary>Rarity tier of an aircraft skin.</summary>
    public enum AircraftSkinRarity
    {
        /// <summary>Available to all players by default.</summary>
        Common,
        /// <summary>Slightly rarer, unlockable through normal play.</summary>
        Uncommon,
        /// <summary>Requires dedicated effort to unlock.</summary>
        Rare,
        /// <summary>Prestigious skins for dedicated pilots.</summary>
        Epic,
        /// <summary>The rarest, highest-prestige skins.</summary>
        Legendary
    }

    /// <summary>Which part of the aircraft a skin applies to.</summary>
    public enum AircraftPartType
    {
        /// <summary>Main fuselage / body material.</summary>
        Body,
        /// <summary>Wing surfaces.</summary>
        Wings,
        /// <summary>Engine nacelles.</summary>
        Engine,
        /// <summary>Cockpit interior / canopy tint.</summary>
        Cockpit,
        /// <summary>Contrail / wake trail.</summary>
        Trail,
        /// <summary>Hull decal / livery overlay.</summary>
        Decal,
        /// <summary>Exhaust / thrust particle effect.</summary>
        Particle,
        /// <summary>Ambient glow / aura effect around the aircraft.</summary>
        Aura
    }

    /// <summary>Condition type that gates a skin's unlock.</summary>
    public enum AircraftUnlockType
    {
        /// <summary>Unlocked for every player with no requirement.</summary>
        Free,
        /// <summary>Requires reaching a specific pilot rank.</summary>
        PilotRank,
        /// <summary>Requires completing a specific achievement.</summary>
        Achievement,
        /// <summary>Available via in-app purchase.</summary>
        Purchase,
        /// <summary>Requires a season pass tier.</summary>
        SeasonPass,
        /// <summary>Awarded after discovering a specific hidden gem.</summary>
        HiddenGem,
        /// <summary>Requires participation in a world event.</summary>
        Event
    }

    /// <summary>
    /// Describes the condition that must be satisfied to unlock a skin.
    /// </summary>
    [Serializable]
    public class AircraftUnlockCondition
    {
        /// <summary>Type of the unlock gate.</summary>
        public AircraftUnlockType conditionType = AircraftUnlockType.Free;

        /// <summary>
        /// The target identifier:
        /// achievement ID, season-pass tier, hidden-gem ID, or event ID.
        /// Empty for Free / PilotRank conditions.
        /// </summary>
        public string targetId = string.Empty;

        /// <summary>
        /// Numeric threshold, e.g. the required pilot-rank level or progress value.
        /// </summary>
        public float targetValue = 0f;
    }

    /// <summary>
    /// Full definition of a single aircraft skin / cosmetic item.
    /// Populated in the Inspector or loaded from Resources.
    /// </summary>
    [Serializable]
    public class AircraftSkinDefinition
    {
        /// <summary>Unique identifier for this skin.</summary>
        public string skinId = string.Empty;

        /// <summary>Localised display name shown in the Hangar UI.</summary>
        public string displayName = string.Empty;

        /// <summary>Short description shown in the detail panel.</summary>
        public string description = string.Empty;

        /// <summary>Rarity tier that drives badge colour and sort order.</summary>
        public AircraftSkinRarity rarity = AircraftSkinRarity.Common;

        /// <summary>Which part slot this skin occupies.</summary>
        public AircraftPartType partType = AircraftPartType.Body;

        /// <summary>
        /// Sprite resource key used to load the preview icon for the gallery card.
        /// </summary>
        public string previewIconId = string.Empty;

        /// <summary>
        /// Material resource name / path applied to the relevant renderer at runtime.
        /// Used for Body, Wings, Engine, and Cockpit parts.
        /// </summary>
        public string materialId = string.Empty;

        /// <summary>Primary colour of the contrail gradient (Trail type only).</summary>
        public Color trailColorPrimary = Color.white;

        /// <summary>Secondary colour of the contrail gradient (Trail type only).</summary>
        public Color trailColorSecondary = new Color(0.7f, 0.9f, 1f, 0f);

        /// <summary>
        /// Particle-prefab resource key instantiated at the particle attach point
        /// (Particle and Aura types).
        /// </summary>
        public string particleSystemId = string.Empty;

        /// <summary>Condition that must be met before this skin can be unlocked.</summary>
        public AircraftUnlockCondition unlockCondition = new AircraftUnlockCondition();

        /// <summary>
        /// IAP product identifier. Empty string if the skin is not purchasable.
        /// References <see cref="SWEF.IAP.IAPManager"/> product catalogue.
        /// </summary>
        public string iapProductId = string.Empty;

        /// <summary>
        /// Minimum pilot rank required to use this skin (0 = no requirement).
        /// </summary>
        public int requiredPilotRank = 0;

        /// <summary>True if this skin is part of the default starter set.</summary>
        public bool isDefault = false;

        /// <summary>Arbitrary extra configuration data.</summary>
        public SerializableStringDictionary metadata = new SerializableStringDictionary();
    }

    /// <summary>
    /// Serializable wrapper for a <c>Dictionary&lt;string, string&gt;</c>
    /// so it can be persisted by <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class SerializableStringDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _keys = new List<string>();
        [SerializeField] private List<string> _values = new List<string>();

        private Dictionary<string, string> _dict = new Dictionary<string, string>();

        /// <summary>Gets or sets a value by key.</summary>
        public string this[string key]
        {
            get => _dict.TryGetValue(key, out var v) ? v : null;
            set => _dict[key] = value;
        }

        /// <summary>Returns true if the dictionary contains <paramref name="key"/>.</summary>
        public bool ContainsKey(string key) => _dict.ContainsKey(key);

        /// <summary>Tries to get a value, returning false if the key is absent.</summary>
        public bool TryGetValue(string key, out string value) => _dict.TryGetValue(key, out value);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kv in _dict)
            {
                _keys.Add(kv.Key);
                _values.Add(kv.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _dict = new Dictionary<string, string>();
            int count = Mathf.Min(_keys.Count, _values.Count);
            for (int i = 0; i < count; i++)
                _dict[_keys[i]] = _values[i];
        }
    }

    /// <summary>
    /// A named set of skins covering each of the eight aircraft part slots.
    /// Players can save multiple loadouts and switch between them in the Hangar.
    /// </summary>
    [Serializable]
    public class AircraftLoadout
    {
        /// <summary>Unique identifier for this loadout.</summary>
        public string loadoutId = string.Empty;

        /// <summary>Player-chosen display name for this loadout.</summary>
        public string loadoutName = "Custom";

        /// <summary>Skin ID equipped in the Body slot.</summary>
        public string bodySkinId = string.Empty;

        /// <summary>Skin ID equipped in the Wings slot.</summary>
        public string wingsSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Engine slot.</summary>
        public string engineSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Cockpit slot.</summary>
        public string cockpitSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Trail slot.</summary>
        public string trailSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Decal slot.</summary>
        public string decalSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Particle slot.</summary>
        public string particleSkinId = string.Empty;

        /// <summary>Skin ID equipped in the Aura slot.</summary>
        public string auraSkinId = string.Empty;

        /// <summary>Returns the skin ID for the given part slot.</summary>
        public string GetSkinForPart(AircraftPartType part)
        {
            switch (part)
            {
                case AircraftPartType.Body:    return bodySkinId;
                case AircraftPartType.Wings:   return wingsSkinId;
                case AircraftPartType.Engine:  return engineSkinId;
                case AircraftPartType.Cockpit: return cockpitSkinId;
                case AircraftPartType.Trail:   return trailSkinId;
                case AircraftPartType.Decal:   return decalSkinId;
                case AircraftPartType.Particle: return particleSkinId;
                case AircraftPartType.Aura:    return auraSkinId;
                default: return string.Empty;
            }
        }

        /// <summary>Sets the skin ID for the given part slot.</summary>
        public void SetSkinForPart(AircraftPartType part, string skinId)
        {
            switch (part)
            {
                case AircraftPartType.Body:    bodySkinId    = skinId; break;
                case AircraftPartType.Wings:   wingsSkinId   = skinId; break;
                case AircraftPartType.Engine:  engineSkinId  = skinId; break;
                case AircraftPartType.Cockpit: cockpitSkinId = skinId; break;
                case AircraftPartType.Trail:   trailSkinId   = skinId; break;
                case AircraftPartType.Decal:   decalSkinId   = skinId; break;
                case AircraftPartType.Particle: particleSkinId = skinId; break;
                case AircraftPartType.Aura:    auraSkinId    = skinId; break;
            }
        }
    }

    /// <summary>
    /// All aircraft customization data that is persisted to disk.
    /// Serialised as JSON to <c>aircraft_customization.json</c>.
    /// </summary>
    [Serializable]
    public class AircraftCustomizationSaveData
    {
        /// <summary>Skin IDs that the player has unlocked.</summary>
        public List<string> unlockedSkinIds = new List<string>();

        /// <summary>ID of the currently active loadout.</summary>
        public string activeLoadoutId = string.Empty;

        /// <summary>All saved loadouts, including the active one.</summary>
        public List<AircraftLoadout> loadouts = new List<AircraftLoadout>();

        /// <summary>Skin IDs that the player has marked as favourites.</summary>
        public List<string> favoriteSkins = new List<string>();
    }
}
