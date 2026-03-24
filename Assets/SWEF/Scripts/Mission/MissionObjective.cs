// MissionObjective.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Defines a single trackable mission objective.
    ///
    /// <para>Objectives are serialised as part of <see cref="MissionData"/> and driven at
    /// runtime by <see cref="MissionManager"/>. Each objective fires
    /// <see cref="OnObjectiveStatusChanged"/> whenever its <see cref="status"/> changes so
    /// that UI components can react without polling.</para>
    /// </summary>
    [Serializable]
    public class MissionObjective
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>Unique string key for this objective within its parent mission.</summary>
        [Tooltip("Unique string key for this objective within its parent mission.")]
        public string objectiveId;

        /// <summary>Player-facing description, e.g. "Fly through all checkpoint rings".</summary>
        [Tooltip("Player-facing description shown in the tracker HUD.")]
        public string description;

        // ── Status ────────────────────────────────────────────────────────────

        /// <summary>Current lifecycle state of this objective.</summary>
        [Tooltip("Current lifecycle state of this objective.")]
        public ObjectiveStatus status = ObjectiveStatus.Pending;

        // ── Flags ─────────────────────────────────────────────────────────────

        /// <summary>When <c>true</c> completing this objective is not required to finish the mission.</summary>
        [Tooltip("Optional bonus objectives do not block mission completion.")]
        public bool isOptional = false;

        /// <summary>When <c>true</c> the objective is not shown to the player until triggered.</summary>
        [Tooltip("Hidden objectives are revealed only when explicitly activated.")]
        public bool isHidden = false;

        // ── Progress ──────────────────────────────────────────────────────────

        /// <summary>Number of completions required, e.g. collect 5 items.</summary>
        [Tooltip("Total completions required (e.g. 5 rings to fly through).")]
        [Min(1)]
        public int requiredCount = 1;

        /// <summary>Number of completions accumulated so far.</summary>
        [Tooltip("Completions accumulated at runtime (do not set in Inspector).")]
        public int currentCount = 0;

        /// <summary>Normalised completion ratio in the range [0, 1].</summary>
        public float progress => requiredCount > 0 ? (float)currentCount / requiredCount : 0f;

        /// <summary><c>true</c> when <see cref="currentCount"/> has reached <see cref="requiredCount"/>.</summary>
        public bool isCompleted => currentCount >= requiredCount;

        // ── Time ──────────────────────────────────────────────────────────────

        /// <summary>Per-objective time limit in seconds (0 = no limit).</summary>
        [Tooltip("Per-objective time limit in seconds. Set 0 for no limit.")]
        public float timeLimit = 0f;

        /// <summary>Seconds remaining on this objective's timer; only meaningful when <see cref="timeLimit"/> &gt; 0.</summary>
        [Tooltip("Seconds remaining (runtime, do not set in Inspector).")]
        public float remainingTime;

        // ── Scoring ───────────────────────────────────────────────────────────

        /// <summary>Points awarded when the objective is completed.</summary>
        [Tooltip("Score points awarded on completion.")]
        public int scoreValue = 100;

        // ── Location ──────────────────────────────────────────────────────────

        /// <summary>World-space position of the objective target (for location-based objectives).</summary>
        [Tooltip("World-space target position used by the HUD waypoint indicator.")]
        public Vector3 targetPosition;

        /// <summary>Radius in metres around <see cref="targetPosition"/> that counts as "at target".</summary>
        [Tooltip("Trigger radius around the target position in metres.")]
        public float targetRadius = 50f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired whenever <see cref="status"/> changes.</summary>
        public event Action<MissionObjective> OnObjectiveStatusChanged;

        // ── Methods ───────────────────────────────────────────────────────────

        /// <summary>
        /// Increments <see cref="currentCount"/> by <paramref name="amount"/>.
        /// If the objective becomes complete, calls <see cref="Complete"/> automatically.
        /// </summary>
        /// <param name="amount">Number of units to add (default 1).</param>
        public void Advance(int amount = 1)
        {
            if (status == ObjectiveStatus.Completed || status == ObjectiveStatus.Failed)
                return;

            currentCount = Mathf.Min(currentCount + amount, requiredCount);
            if (isCompleted)
                Complete();
        }

        /// <summary>Marks this objective as <see cref="ObjectiveStatus.Completed"/> and fires the changed event.</summary>
        public void Complete()
        {
            if (status == ObjectiveStatus.Completed)
                return;

            currentCount = requiredCount;
            SetStatus(ObjectiveStatus.Completed);
        }

        /// <summary>Marks this objective as <see cref="ObjectiveStatus.Failed"/> and fires the changed event.</summary>
        public void Fail()
        {
            if (status == ObjectiveStatus.Failed)
                return;

            SetStatus(ObjectiveStatus.Failed);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetStatus(ObjectiveStatus newStatus)
        {
            status = newStatus;
            OnObjectiveStatusChanged?.Invoke(this);
        }
    }
}
