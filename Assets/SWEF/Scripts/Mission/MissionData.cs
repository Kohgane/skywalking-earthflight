// MissionData.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — ScriptableObject template that fully describes a mission.
    ///
    /// <para>Create via <em>Assets → Create → SWEF/Mission/Mission Data</em>.
    /// Load a MissionData asset into <see cref="MissionManager.LoadMission"/> to enter
    /// the briefing state and begin the mission lifecycle.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Mission/Mission Data", fileName = "NewMissionData")]
    public class MissionData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Unique string identifier for this mission used in save data and prerequisites.")]
        /// <summary>Unique string identifier for this mission.</summary>
        public string missionId;

        [Tooltip("Human-readable display name shown in the mission list and briefing screen.")]
        /// <summary>Human-readable display name shown in the UI.</summary>
        public string missionName;

        [Tooltip("One-sentence summary shown in mission-selection lists.")]
        [TextArea(2, 4)]
        /// <summary>Short one-sentence description shown in the mission selection list.</summary>
        public string shortDescription;

        [Tooltip("Full mission briefing text displayed on the briefing screen.")]
        [TextArea(3, 8)]
        /// <summary>Detailed briefing text revealed with a typewriter effect on the briefing screen.</summary>
        public string briefingText;

        // ── Classification ────────────────────────────────────────────────────

        [Header("Classification")]

        [Tooltip("Gameplay category of this mission.")]
        /// <summary>Gameplay category of this mission.</summary>
        public MissionType type = MissionType.Custom;

        [Tooltip("Difficulty tier that determines HUD hints, time pressure, and scoring context.")]
        /// <summary>Difficulty tier of the mission.</summary>
        public MissionDifficulty difficulty = MissionDifficulty.Normal;

        [Tooltip("Recommended player level for this mission.")]
        [Min(1)]
        /// <summary>Recommended player level to attempt this mission.</summary>
        public int recommendedLevel = 1;

        // ── Visuals ───────────────────────────────────────────────────────────

        [Header("Visuals")]

        [Tooltip("Small icon used in mission lists and the tracker HUD.")]
        /// <summary>Small icon sprite used in mission lists and the HUD tracker.</summary>
        public Sprite missionIcon;

        [Tooltip("Large banner image displayed on the full-screen briefing panel.")]
        /// <summary>Large banner image displayed on the full-screen briefing panel.</summary>
        public Sprite missionBanner;

        // ── Timing ────────────────────────────────────────────────────────────

        [Header("Timing")]

        [Tooltip("Overall time limit in seconds. Set 0 for no time limit.")]
        [Min(0f)]
        /// <summary>Overall time limit in seconds (0 = no limit).</summary>
        public float timeLimit = 0f;

        // ── World Placement ───────────────────────────────────────────────────

        [Header("World Placement")]

        [Tooltip("World-space position where the player is placed when the mission starts.")]
        /// <summary>World-space spawn position for the start of the mission.</summary>
        public Vector3 startPosition;

        [Tooltip("Initial heading in degrees at the mission start position.")]
        [Range(0f, 360f)]
        /// <summary>Initial aircraft heading in degrees.</summary>
        public float startHeading;

        [Tooltip("Geographic region name shown in the mission details panel.")]
        /// <summary>Geographic region name displayed in the mission details panel.</summary>
        public string regionName;

        // ── Objectives & Checkpoints ──────────────────────────────────────────

        [Header("Objectives & Checkpoints")]

        [Tooltip("Ordered list of mission objectives. Required objectives must all be completed to finish the mission.")]
        /// <summary>Ordered list of mission objectives.</summary>
        public List<MissionObjective> objectives = new List<MissionObjective>();

        [Tooltip("Flight checkpoints the player must pass in sequence.")]
        /// <summary>Ordered sequence of flight checkpoints.</summary>
        public List<MissionCheckpoint> checkpoints = new List<MissionCheckpoint>();

        // ── Rewards ───────────────────────────────────────────────────────────

        [Header("Rewards")]

        [Tooltip("XP, currency, and unlock rewards granted on mission completion.")]
        /// <summary>Reward bundle granted on completion.</summary>
        public MissionReward reward = new MissionReward();

        // ── Prerequisites ─────────────────────────────────────────────────────

        [Header("Prerequisites")]

        [Tooltip("Mission IDs that must be completed before this mission becomes available.")]
        /// <summary>Mission IDs that must be in the completed history before this mission unlocks.</summary>
        public string[] prerequisiteMissionIds = System.Array.Empty<string>();

        [Tooltip("When true the player may replay this mission after completing it.")]
        /// <summary>Whether the player may replay this mission after completing it.</summary>
        public bool isReplayable = true;

        // ── Audio ─────────────────────────────────────────────────────────────

        [Header("Audio")]

        [Tooltip("Optional voice-over clip played during the briefing screen.")]
        /// <summary>Optional narration audio clip played during the briefing screen.</summary>
        public AudioClip briefingNarration;
    }
}
