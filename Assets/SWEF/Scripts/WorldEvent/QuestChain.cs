// QuestChain.cs — SWEF Dynamic Event & World Quest System (Phase 64)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.WorldEvent
{
    /// <summary>
    /// ScriptableObject that defines an ordered chain of world events forming a
    /// multi-step quest line.  Each step must be completed before the next is made
    /// available.  Create via
    /// <c>Assets → Create → SWEF → WorldEvent → Quest Chain</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/WorldEvent/Quest Chain", fileName = "NewQuestChain")]
    public class QuestChain : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique identifier for this quest chain.")]
        /// <summary>Unique identifier for this quest chain.</summary>
        public string chainId;

        [Tooltip("Human-readable name shown in the quest log.")]
        /// <summary>Human-readable name shown in the quest log.</summary>
        public string chainName;

        [Tooltip("Description of the overall quest line shown in the quest log.")]
        /// <summary>Description of the overall quest line shown in the quest log.</summary>
        public string chainDescription;

        // ── Steps ────────────────────────────────────────────────────────────────

        [Header("Steps")]
        [Tooltip("Ordered list of events that form the quest chain. Completed left-to-right.")]
        /// <summary>Ordered list of events that form this quest chain, completed left-to-right.</summary>
        public List<WorldEventData> events = new List<WorldEventData>();

        // ── Rewards ───────────────────────────────────────────────────────────────

        [Header("Chain Completion Reward")]
        [Tooltip("Bonus reward granted when every step in the chain is completed.")]
        /// <summary>Bonus reward granted when every step in the chain is completed.</summary>
        public RewardData chainCompletionReward = new RewardData();

        // ── Runtime Progress ─────────────────────────────────────────────────────

        /// <summary>Index of the event the player is currently working on.</summary>
        public int currentStep = 0;

        /// <summary>Returns <c>true</c> when all steps have been completed.</summary>
        public bool isCompleted => currentStep >= (events != null ? events.Count : 0);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="WorldEventData"/> for the current step, or
        /// <c>null</c> when the chain is already completed.
        /// </summary>
        public WorldEventData GetCurrentEvent()
        {
            if (isCompleted || events == null || currentStep < 0 || currentStep >= events.Count)
                return null;
            return events[currentStep];
        }

        /// <summary>
        /// Advances to the next step.  Has no effect when the chain is already complete.
        /// </summary>
        public void AdvanceStep()
        {
            if (!isCompleted)
                currentStep++;
        }

        /// <summary>Resets progress back to the first step.</summary>
        public void ResetChain()
        {
            currentStep = 0;
        }
    }
}
