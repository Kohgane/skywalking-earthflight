// SquadronBaseManager.cs — Phase 109: Clan/Squadron System
// Manages the squadron home base: facilities, upgrades, decorations, visitor access.
// Namespace: SWEF.Squadron

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Manages the squadron's home base, including facility construction
    /// and upgrade, area unlocks, and base decoration/customization.
    ///
    /// <para>Attach alongside <see cref="SquadronManager"/> on the persistent scene object.</para>
    /// </summary>
    public sealed class SquadronBaseManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronBaseManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when any facility is successfully upgraded.</summary>
        public event Action<SquadronFacility, int> OnFacilityUpgraded;

        /// <summary>Raised when a new area of the base is unlocked.</summary>
        public event Action<string> OnAreaUnlocked;

        /// <summary>Raised when a decoration is placed on the base.</summary>
        public event Action<string> OnDecorationPlaced;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>The current squadron's base data.</summary>
        public SquadronBase CurrentBase { get; private set; }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBase();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises a default base for a newly created squadron.
        /// All facilities start at level 0 (not built).
        /// </summary>
        public SquadronBase InitialiseBase(string squadronId, string location = "HQ")
        {
            var baseData = new SquadronBase
            {
                baseId     = Guid.NewGuid().ToString(),
                squadronId = squadronId,
                location   = location
            };

            foreach (SquadronFacility facility in Enum.GetValues(typeof(SquadronFacility)))
                baseData.facilityLevels[facility] = 0;

            CurrentBase = baseData;
            SaveBase();
            return baseData;
        }

        /// <summary>
        /// Upgrades a facility by one level. Deducts XP cost from the squadron.
        /// Requires <see cref="SquadronPermission.EditBase"/>.
        /// </summary>
        /// <returns>True if the upgrade was applied.</returns>
        public bool UpgradeFacility(SquadronFacility facility)
        {
            var manager = SquadronManager.Instance;
            if (manager == null || !manager.HasPermission(SquadronPermission.EditBase))
            {
                Debug.LogWarning("[SquadronBaseManager] No permission to edit base.");
                return false;
            }

            if (CurrentBase == null)
            {
                Debug.LogWarning("[SquadronBaseManager] No base found.");
                return false;
            }

            int currentLevel = GetFacilityLevel(facility);
            if (currentLevel >= SquadronConfig.FacilityMaxLevel)
            {
                Debug.LogWarning($"[SquadronBaseManager] {facility} is already at max level.");
                return false;
            }

            int cost = SquadronConfig.FacilityUpgradeCosts[currentLevel];
            var squadron = manager.CurrentSquadron;
            if (squadron == null || squadron.totalXP < cost)
            {
                Debug.LogWarning("[SquadronBaseManager] Not enough squadron XP for upgrade.");
                return false;
            }

            // Deduct cost
            squadron.totalXP -= cost;

            int newLevel = currentLevel + 1;
            CurrentBase.facilityLevels[facility] = newLevel;

            // Check area unlocks triggered by this upgrade
            CheckAreaUnlocks();

            SaveBase();
            OnFacilityUpgraded?.Invoke(facility, newLevel);
            return true;
        }

        /// <summary>
        /// Returns the current level of a specific facility (0 = not built).
        /// </summary>
        public int GetFacilityLevel(SquadronFacility facility)
        {
            if (CurrentBase == null) return 0;
            return CurrentBase.facilityLevels.TryGetValue(facility, out int lvl) ? lvl : 0;
        }

        /// <summary>
        /// Calculates the XP bonus provided by a facility type at its current level.
        /// </summary>
        public float GetFacilityBonus(SquadronFacility facility)
        {
            int level = GetFacilityLevel(facility);
            return facility switch
            {
                SquadronFacility.Hangar         => level * SquadronConfig.HangarSlotsPerLevel,
                SquadronFacility.FuelDepot       => level * SquadronConfig.FuelDepotEfficiencyPerLevel,
                SquadronFacility.RepairBay       => level * SquadronConfig.RepairBaySpeedPerLevel,
                SquadronFacility.BriefingRoom    => level * SquadronConfig.BriefingRoomXPBonusPerLevel,
                _                               => 0f
            };
        }

        /// <summary>
        /// Places a decoration on the base.
        /// Requires <see cref="SquadronPermission.EditBase"/>.
        /// </summary>
        public bool PlaceDecoration(string decorationId)
        {
            var manager = SquadronManager.Instance;
            if (manager == null || !manager.HasPermission(SquadronPermission.EditBase))
                return false;

            if (CurrentBase == null) return false;

            if (!CurrentBase.decorations.Contains(decorationId))
                CurrentBase.decorations.Add(decorationId);

            SaveBase();
            OnDecorationPlaced?.Invoke(decorationId);
            return true;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void CheckAreaUnlocks()
        {
            if (CurrentBase == null) return;

            // Example unlock logic: Control Tower L1 unlocks "airspace" area
            if (GetFacilityLevel(SquadronFacility.ControlTower) >= 1 &&
                !CurrentBase.unlockedAreas.Contains("airspace"))
            {
                CurrentBase.unlockedAreas.Add("airspace");
                OnAreaUnlocked?.Invoke("airspace");
            }

            // TrainingGround L1 unlocks "parade_ground" area
            if (GetFacilityLevel(SquadronFacility.TrainingGround) >= 1 &&
                !CurrentBase.unlockedAreas.Contains("parade_ground"))
            {
                CurrentBase.unlockedAreas.Add("parade_ground");
                OnAreaUnlocked?.Invoke("parade_ground");
            }

            // TrophyRoom L1 unlocks "trophy_wing" area
            if (GetFacilityLevel(SquadronFacility.TrophyRoom) >= 1 &&
                !CurrentBase.unlockedAreas.Contains("trophy_wing"))
            {
                CurrentBase.unlockedAreas.Add("trophy_wing");
                OnAreaUnlocked?.Invoke("trophy_wing");
            }
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveBase()
        {
            try
            {
                if (CurrentBase == null) return;
                File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, SquadronConfig.BaseDataFile),
                    JsonUtility.ToJson(CurrentBase, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronBaseManager] Save error: {ex.Message}");
            }
        }

        private void LoadBase()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SquadronConfig.BaseDataFile);
                if (!File.Exists(path)) return;
                CurrentBase = JsonUtility.FromJson<SquadronBase>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronBaseManager] Load error: {ex.Message}");
            }
        }
    }
}
