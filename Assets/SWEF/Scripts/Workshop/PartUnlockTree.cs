// PartUnlockTree.cs — SWEF Aircraft Workshop & Part Customization (Phase 90)
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWEF.Workshop
{
    /// <summary>
    /// Describes the conditions that must be met before a part can be unlocked.
    /// </summary>
    [Serializable]
    public class UnlockRequirementData
    {
        /// <summary>Minimum player level required.</summary>
        [Tooltip("Minimum player level required to unlock this part.")]
        public int requiredLevel = 1;

        /// <summary>Amount of in-game currency required to purchase the unlock.</summary>
        [Tooltip("Currency cost to unlock this part.")]
        public int currencyCost = 0;

        /// <summary>
        /// Achievement keys that must all be completed before the part becomes
        /// available (empty list = no achievement requirement).
        /// </summary>
        [Tooltip("Achievement keys that must be completed before this part unlocks.")]
        public List<string> requiredAchievements = new List<string>();

        /// <summary>Number of missions the player must have completed.</summary>
        [Tooltip("Number of completed missions required.")]
        public int requiredMissionsCompleted = 0;

        /// <summary>
        /// Part IDs that must already be unlocked before this part becomes
        /// available (prerequisite parts in the tree).
        /// </summary>
        [Tooltip("Part IDs that must be unlocked before this part can be unlocked.")]
        public List<string> prerequisitePartIds = new List<string>();
    }

    /// <summary>
    /// A node in the part-unlock tech tree, binding an
    /// <see cref="AircraftPartData"/> definition to its
    /// <see cref="UnlockRequirementData"/>.
    /// </summary>
    [Serializable]
    public class UnlockTreeNode
    {
        /// <summary>The part this node represents.</summary>
        [Tooltip("Part definition for this tree node.")]
        public AircraftPartData part = new AircraftPartData();

        /// <summary>Conditions required to unlock the part.</summary>
        [Tooltip("Unlock requirement data for this node.")]
        public UnlockRequirementData requirements = new UnlockRequirementData();
    }

    /// <summary>
    /// Singleton MonoBehaviour managing the tech / unlock tree for aircraft parts.
    /// Parts are unlocked by meeting player-level, achievement, mission-count, and
    /// currency requirements.
    /// </summary>
    public class PartUnlockTree : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static PartUnlockTree Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Tree Data")]
        [Tooltip("All nodes in the unlock tree.  Populate via the Inspector or at runtime.")]
        [SerializeField] private List<UnlockTreeNode> _nodes = new List<UnlockTreeNode>();

        // ── Unity Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Determines whether the player currently meets all requirements to
        /// unlock the part with the given ID.
        /// </summary>
        /// <param name="partId">ID of the part to check.</param>
        /// <param name="playerLevel">Current player level.</param>
        /// <param name="currency">Current player currency balance.</param>
        /// <param name="completedMissions">Number of missions the player has completed.</param>
        /// <param name="completedAchievements">Set of achievement keys the player has completed.</param>
        /// <param name="inventory">Inventory used to verify prerequisite parts are owned.</param>
        /// <returns><c>true</c> if the player can unlock the part right now.</returns>
        public bool CanUnlock(
            string partId,
            int playerLevel,
            int currency,
            int completedMissions,
            HashSet<string> completedAchievements,
            PartInventoryController inventory)
        {
            var node = FindNode(partId);
            if (node == null)
            {
                Debug.LogWarning($"[SWEF] Workshop: CanUnlock — no tree node found for partId '{partId}'.");
                return false;
            }
            if (node.part.isUnlocked) return false; // already unlocked

            var req = node.requirements;
            if (playerLevel < req.requiredLevel)         return false;
            if (currency    < req.currencyCost)          return false;
            if (completedMissions < req.requiredMissionsCompleted) return false;

            if (req.requiredAchievements != null)
            {
                foreach (var ach in req.requiredAchievements)
                {
                    if (completedAchievements == null || !completedAchievements.Contains(ach))
                        return false;
                }
            }

            if (req.prerequisitePartIds != null && inventory != null)
            {
                foreach (var prereqId in req.prerequisitePartIds)
                {
                    if (!inventory.HasPart(prereqId)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the specified part as unlocked and adds it to the player's
        /// inventory.  Call only after validating with <see cref="CanUnlock"/>.
        /// </summary>
        /// <param name="partId">ID of the part to unlock.</param>
        /// <param name="inventory">Inventory to add the part to.</param>
        /// <returns><c>true</c> if the part was successfully unlocked.</returns>
        public bool UnlockPart(string partId, PartInventoryController inventory)
        {
            var node = FindNode(partId);
            if (node == null)
            {
                Debug.LogWarning($"[SWEF] Workshop: UnlockPart — no tree node found for partId '{partId}'.");
                return false;
            }
            if (node.part.isUnlocked)
            {
                Debug.LogWarning($"[SWEF] Workshop: UnlockPart — part '{partId}' is already unlocked.");
                return false;
            }

            node.part.isUnlocked = true;

            if (inventory != null)
                inventory.AddPart(node.part);

            return true;
        }

        /// <summary>
        /// Returns a progress fraction [0, 1] indicating how many of the unlock
        /// conditions for the given part have been satisfied.
        /// Useful for partial-progress UI bars.
        /// </summary>
        /// <param name="partId">ID of the part to check.</param>
        /// <param name="playerLevel">Current player level.</param>
        /// <param name="currency">Current player currency balance.</param>
        /// <param name="completedMissions">Number of completed missions.</param>
        /// <param name="completedAchievements">Set of completed achievement keys.</param>
        /// <param name="inventory">Inventory for prerequisite checks.</param>
        /// <returns>Progress fraction in [0, 1].</returns>
        public float GetUnlockProgress(
            string partId,
            int playerLevel,
            int currency,
            int completedMissions,
            HashSet<string> completedAchievements,
            PartInventoryController inventory)
        {
            var node = FindNode(partId);
            if (node == null) return 0f;
            if (node.part.isUnlocked) return 1f;

            var req = node.requirements;
            int total = 0, met = 0;

            // Level
            total++;
            if (playerLevel >= req.requiredLevel) met++;

            // Currency
            total++;
            if (currency >= req.currencyCost) met++;

            // Missions
            total++;
            if (completedMissions >= req.requiredMissionsCompleted) met++;

            // Achievements
            if (req.requiredAchievements != null)
            {
                foreach (var ach in req.requiredAchievements)
                {
                    total++;
                    if (completedAchievements != null && completedAchievements.Contains(ach)) met++;
                }
            }

            // Prerequisites
            if (req.prerequisitePartIds != null)
            {
                foreach (var prereqId in req.prerequisitePartIds)
                {
                    total++;
                    if (inventory != null && inventory.HasPart(prereqId)) met++;
                }
            }

            return total > 0 ? (float)met / total : 1f;
        }

        /// <summary>
        /// Returns the list of parts that the player can unlock next — i.e. parts
        /// whose prerequisites are all met but that are not yet unlocked.
        /// </summary>
        /// <param name="playerLevel">Current player level.</param>
        /// <param name="currency">Current player currency balance.</param>
        /// <param name="completedMissions">Number of completed missions.</param>
        /// <param name="completedAchievements">Set of completed achievement keys.</param>
        /// <param name="inventory">Part inventory for prerequisite checks.</param>
        /// <returns>List of parts available to unlock now.</returns>
        public List<AircraftPartData> GetNextUnlockable(
            int playerLevel,
            int currency,
            int completedMissions,
            HashSet<string> completedAchievements,
            PartInventoryController inventory)
        {
            return _nodes
                .Where(n => !n.part.isUnlocked &&
                            CanUnlock(n.part.partId, playerLevel, currency,
                                      completedMissions, completedAchievements, inventory))
                .Select(n => n.part)
                .ToList();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private UnlockTreeNode FindNode(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            return _nodes.FirstOrDefault(n => n.part != null &&
                                              string.Equals(n.part.partId, partId, StringComparison.Ordinal));
        }
    }
}
