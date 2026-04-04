// SpecialMissionData.cs — SWEF Phase 106: Historical & Sci-Fi Flight Mode
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.HistoricalSciFi
{
    /// <summary>Completion status of a special mission.</summary>
    public enum MissionStatus
    {
        /// <summary>Mission is available but has not been started.</summary>
        Available,
        /// <summary>Mission is currently in progress.</summary>
        InProgress,
        /// <summary>Mission was completed successfully.</summary>
        Completed,
        /// <summary>Mission was failed — may be retried.</summary>
        Failed,
        /// <summary>Mission is locked pending prerequisites.</summary>
        Locked,
    }

    /// <summary>Category of special mission.</summary>
    public enum MissionCategory
    {
        /// <summary>Mission tied to a historical aviation scenario.</summary>
        Historical,
        /// <summary>Mission set in a Sci-Fi / off-world environment.</summary>
        SciFi,
    }

    /// <summary>A single objective within a <see cref="SpecialMissionData"/>.</summary>
    [Serializable]
    public sealed class MissionObjective
    {
        /// <summary>Short human-readable label (e.g. "Reach 120 km/h").</summary>
        public string label;

        /// <summary>Whether this objective has been completed.</summary>
        public bool isCompleted;

        /// <summary>Creates a new mission objective.</summary>
        public static MissionObjective Create(string label) =>
            new MissionObjective { label = label, isCompleted = false };
    }

    /// <summary>Reward granted upon successful mission completion.</summary>
    [Serializable]
    public sealed class MissionReward
    {
        /// <summary>Bonus score points awarded.</summary>
        public int bonusPoints;

        /// <summary>ID of an aircraft unlocked by completing this mission, if any.</summary>
        public string unlockedAircraftId;

        /// <summary>Human-readable flavour text for the reward.</summary>
        public string rewardDescription;
    }

    /// <summary>
    /// Immutable data record describing a special historical or Sci-Fi mission.
    /// Includes objectives, required aircraft/environment, and completion rewards.
    /// </summary>
    [Serializable]
    public sealed class SpecialMissionData
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Unique machine-readable identifier (e.g. "first_flight").</summary>
        public string id;

        /// <summary>Human-readable title shown in the mission list UI.</summary>
        public string title;

        /// <summary>Full mission briefing text.</summary>
        [TextArea(3, 6)]
        public string description;

        /// <summary>Historical or Sci-Fi category.</summary>
        public MissionCategory category;

        // ── Requirements ─────────────────────────────────────────────────────

        /// <summary>ID of the aircraft required to start this mission.</summary>
        public string requiredAircraftId;

        /// <summary>ID of the environment (<see cref="SciFiEnvironmentData"/>) in which the mission takes place.</summary>
        public string requiredEnvironmentId;

        // ── Objectives ───────────────────────────────────────────────────────

        /// <summary>Ordered list of objectives that must all be completed.</summary>
        public List<MissionObjective> objectives = new List<MissionObjective>();

        // ── Reward ───────────────────────────────────────────────────────────

        /// <summary>Reward granted on successful completion.</summary>
        public MissionReward reward;

        // ── Runtime State ────────────────────────────────────────────────────

        /// <summary>Current completion status of this mission.</summary>
        public MissionStatus status = MissionStatus.Locked;

        // ── Factory ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a fully-populated <see cref="SpecialMissionData"/> instance.
        /// </summary>
        public static SpecialMissionData Create(
            string id,
            string title,
            string description,
            MissionCategory category,
            string requiredAircraftId,
            string requiredEnvironmentId,
            List<MissionObjective> objectives,
            MissionReward reward)
        {
            return new SpecialMissionData
            {
                id                    = id,
                title                 = title,
                description           = description,
                category              = category,
                requiredAircraftId    = requiredAircraftId,
                requiredEnvironmentId = requiredEnvironmentId,
                objectives            = objectives ?? new List<MissionObjective>(),
                reward                = reward,
                status                = MissionStatus.Locked,
            };
        }

        /// <summary>Returns <c>true</c> when every objective is marked complete.</summary>
        public bool AllObjectivesComplete()
        {
            if (objectives == null || objectives.Count == 0) return false;
            foreach (var obj in objectives)
                if (!obj.isCompleted) return false;
            return true;
        }
    }
}
