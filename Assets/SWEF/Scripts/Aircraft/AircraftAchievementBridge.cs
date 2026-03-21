using UnityEngine;
using SWEF.Achievement;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Listens to <see cref="AircraftCustomizationManager"/> events and reports
    /// customisation milestones to <see cref="AchievementManager"/>.
    ///
    /// Tracks:
    /// <list type="bullet">
    ///   <item>Total unique skins unlocked → <c>aircraft_skins_unlocked</c></item>
    ///   <item>First skin ever unlocked → <c>aircraft_first_skin</c></item>
    ///   <item>First Legendary skin unlocked → <c>aircraft_legendary_first</c></item>
    ///   <item>Full eight-slot set completed from same rarity → <c>aircraft_full_set</c></item>
    ///   <item>Number of loadouts created → <c>aircraft_loadouts_created</c></item>
    ///   <item>Skins per rarity tier (e.g. <c>aircraft_rare_skins</c>)</item>
    /// </list>
    /// </summary>
    public class AircraftAchievementBridge : MonoBehaviour
    {
        // ── Internal state ────────────────────────────────────────────────────────

        private AircraftCustomizationManager _customManager;
        private AircraftSkinRegistry _registry;
        private AchievementManager _achievements;

        private int _loadoutsCreated = 0;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            _customManager = AircraftCustomizationManager.Instance;
            _registry      = AircraftSkinRegistry.Instance;
            _achievements  = AchievementManager.Instance;

            if (_customManager == null)
            {
                Debug.LogWarning("[AircraftAchievementBridge] AircraftCustomizationManager not found.");
                return;
            }

            _customManager.OnSkinUnlocked   += HandleSkinUnlocked;
            _customManager.OnLoadoutChanged += HandleLoadoutChanged;
        }

        private void OnDestroy()
        {
            if (_customManager != null)
            {
                _customManager.OnSkinUnlocked   -= HandleSkinUnlocked;
                _customManager.OnLoadoutChanged -= HandleLoadoutChanged;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandleSkinUnlocked(string skinId)
        {
            if (_achievements == null || _registry == null) return;

            // Total skins unlocked
            int totalUnlocked = _customManager.UnlockedSkinIds.Count;
            _achievements.SetProgress("aircraft_skins_unlocked", totalUnlocked);

            // First skin
            if (totalUnlocked == 1)
                _achievements.ReportProgress("aircraft_first_skin", 1f);

            // Rarity-specific
            var skin = _registry.GetSkin(skinId);
            if (skin == null) return;

            string rarityKey = $"aircraft_{skin.rarity.ToString().ToLowerInvariant()}_skins";
            _achievements.ReportProgress(rarityKey, 1f);

            // First Legendary
            if (skin.rarity == AircraftSkinRarity.Legendary)
                _achievements.ReportProgress("aircraft_legendary_first", 1f);

            // Full set check — all 8 slots from same rarity in active loadout
            CheckFullSetAchievement(skin.rarity);
        }

        private void HandleLoadoutChanged(AircraftLoadout loadout)
        {
            if (_achievements == null || loadout == null) return;

            _loadoutsCreated++;
            _achievements.SetProgress("aircraft_loadouts_created", _loadoutsCreated);
        }

        private void CheckFullSetAchievement(AircraftSkinRarity rarity)
        {
            if (_customManager == null || _registry == null || _achievements == null) return;

            var loadout = _customManager.ActiveLoadout;
            if (loadout == null) return;

            foreach (var part in System.Enum.GetValues(typeof(AircraftPartType)))
            {
                var partType = (AircraftPartType)part;
                string equippedId = loadout.GetSkinForPart(partType);
                if (string.IsNullOrEmpty(equippedId)) return;

                var equippedSkin = _registry.GetSkin(equippedId);
                if (equippedSkin == null || equippedSkin.rarity != rarity) return;
            }

            // All 8 slots filled with the same rarity
            _achievements.ReportProgress("aircraft_full_set", 1f);
        }
    }
}
