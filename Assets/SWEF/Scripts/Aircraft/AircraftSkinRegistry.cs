using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Central registry of all <see cref="AircraftSkinDefinition"/> objects available in the
    /// game. Populated via the Inspector (<see cref="allSkins"/>) or loaded from Resources at
    /// runtime.  Provides O(1) lookups via an internal dictionary built in <c>Awake</c>.
    /// </summary>
    public class AircraftSkinRegistry : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static AircraftSkinRegistry Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("Skin Definitions")]
        [SerializeField] private List<AircraftSkinDefinition> allSkins = new List<AircraftSkinDefinition>();

        // ── Internal state ────────────────────────────────────────────────────────

        private Dictionary<string, AircraftSkinDefinition> _skinById =
            new Dictionary<string, AircraftSkinDefinition>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDictionary();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void BuildDictionary()
        {
            _skinById.Clear();
            foreach (var skin in allSkins)
            {
                if (skin == null) continue;
                if (string.IsNullOrEmpty(skin.skinId))
                {
                    Debug.LogWarning("[AircraftSkinRegistry] Skin with empty skinId skipped.");
                    continue;
                }
                if (_skinById.ContainsKey(skin.skinId))
                {
                    Debug.LogWarning($"[AircraftSkinRegistry] Duplicate skinId '{skin.skinId}' – keeping first.");
                    continue;
                }
                _skinById[skin.skinId] = skin;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="AircraftSkinDefinition"/> with the given ID, or
        /// <c>null</c> if not found.
        /// </summary>
        public AircraftSkinDefinition GetSkin(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return null;
            _skinById.TryGetValue(skinId, out var def);
            return def;
        }

        /// <summary>
        /// Returns all skins that target the specified <paramref name="part"/>.
        /// </summary>
        public List<AircraftSkinDefinition> GetSkinsByPart(AircraftPartType part)
        {
            var result = new List<AircraftSkinDefinition>();
            foreach (var skin in allSkins)
            {
                if (skin != null && skin.partType == part)
                    result.Add(skin);
            }
            return result;
        }

        /// <summary>
        /// Returns all skins of the specified <paramref name="rarity"/> tier.
        /// </summary>
        public List<AircraftSkinDefinition> GetSkinsByRarity(AircraftSkinRarity rarity)
        {
            var result = new List<AircraftSkinDefinition>();
            foreach (var skin in allSkins)
            {
                if (skin != null && skin.rarity == rarity)
                    result.Add(skin);
            }
            return result;
        }

        /// <summary>
        /// Returns all skins where <see cref="AircraftSkinDefinition.isDefault"/> is
        /// <c>true</c> (i.e. the starter set).
        /// </summary>
        public List<AircraftSkinDefinition> GetDefaultSkins()
        {
            var result = new List<AircraftSkinDefinition>();
            foreach (var skin in allSkins)
            {
                if (skin != null && skin.isDefault)
                    result.Add(skin);
            }
            return result;
        }

        /// <summary>
        /// Returns a read-only copy of the full skin list.
        /// </summary>
        public List<AircraftSkinDefinition> GetAllSkins()
        {
            return new List<AircraftSkinDefinition>(allSkins);
        }

        /// <summary>
        /// Returns all skins whose
        /// <see cref="AircraftUnlockCondition.conditionType"/> matches
        /// <paramref name="unlockType"/>.
        /// </summary>
        public List<AircraftSkinDefinition> GetSkinsForUnlockType(AircraftUnlockType unlockType)
        {
            var result = new List<AircraftSkinDefinition>();
            foreach (var skin in allSkins)
            {
                if (skin != null &&
                    skin.unlockCondition != null &&
                    skin.unlockCondition.conditionType == unlockType)
                    result.Add(skin);
            }
            return result;
        }
    }
}
