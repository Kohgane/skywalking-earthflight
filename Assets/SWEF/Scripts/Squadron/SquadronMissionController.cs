// SquadronMissionController.cs — Phase 109: Clan/Squadron System
// Squadron cooperative mission management — creation, assignment, tracking, completion.
// Namespace: SWEF.Squadron

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Manages the lifecycle of squadron cooperative missions.
    /// Creates missions from templates, assigns members, tracks collective and
    /// per-member progress, and distributes rewards on completion.
    ///
    /// <para>Attach alongside <see cref="SquadronManager"/> on the persistent scene object.</para>
    /// </summary>
    public sealed class SquadronMissionController : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronMissionController Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a mission starts. Argument is the mission record.</summary>
        public event Action<SquadronMission> OnMissionStarted;

        /// <summary>Raised when a mission is successfully completed.</summary>
        public event Action<SquadronMission> OnMissionCompleted;

        /// <summary>Raised when a mission fails (timeout or abort).</summary>
        public event Action<SquadronMission> OnMissionFailed;

        /// <summary>Raised when a single objective within a mission is completed.</summary>
        public event Action<SquadronMission, int> OnObjectiveCompleted;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<SquadronMission> _activeMissions   = new List<SquadronMission>();
        private readonly List<SquadronMission> _completedMissions = new List<SquadronMission>();
        private readonly Dictionary<string, long> _cooldownMap   = new Dictionary<string, long>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMissions();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates and starts a new squadron mission.
        /// Requires <see cref="SquadronPermission.StartMission"/>.
        /// </summary>
        public SquadronMission StartMission(
            SquadronMissionType missionType,
            string title,
            string description,
            List<string> objectives,
            int requiredMembers,
            int difficulty,
            float timeLimitSeconds = 0f)
        {
            var manager = SquadronManager.Instance;
            if (manager == null || !manager.HasPermission(SquadronPermission.StartMission))
            {
                Debug.LogWarning("[SquadronMissionController] No permission to start missions.");
                return null;
            }

            if (_activeMissions.Count >= SquadronConfig.MaxActiveMissions)
            {
                Debug.LogWarning("[SquadronMissionController] Active mission cap reached.");
                return null;
            }

            string cooldownKey = $"{manager.CurrentSquadron?.squadronId}_{missionType}";
            if (_cooldownMap.TryGetValue(cooldownKey, out long nextAvailable) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() < nextAvailable)
            {
                Debug.LogWarning("[SquadronMissionController] Mission on cooldown.");
                return null;
            }

            var mission = new SquadronMission
            {
                missionId       = Guid.NewGuid().ToString(),
                missionType     = missionType,
                title           = title,
                description     = description,
                objectives      = objectives ?? new List<string>(),
                requiredMembers = requiredMembers,
                difficulty      = Mathf.Clamp(difficulty, 1, 5),
                timeLimit       = timeLimitSeconds,
                isActive        = true,
                startedAt       = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Add the initiating player automatically
            if (manager.LocalMember != null)
                mission.participantIds.Add(manager.LocalMember.memberId);

            _activeMissions.Add(mission);

            if (timeLimitSeconds > 0f)
                StartCoroutine(MissionTimeoutCoroutine(mission, timeLimitSeconds));

            SaveMissions();
            OnMissionStarted?.Invoke(mission);
            return mission;
        }

        /// <summary>
        /// Adds a member to an active mission.
        /// </summary>
        public bool JoinMission(string missionId, string memberId)
        {
            var mission = GetActiveMission(missionId);
            if (mission == null) return false;

            if (mission.participantIds.Count >= SquadronConfig.MaxMissionParticipants)
                return false;

            if (!mission.participantIds.Contains(memberId))
                mission.participantIds.Add(memberId);

            SaveMissions();
            return true;
        }

        /// <summary>
        /// Marks a specific objective as complete within an active mission.
        /// </summary>
        public bool CompleteObjective(string missionId, int objectiveIndex)
        {
            var mission = GetActiveMission(missionId);
            if (mission == null || objectiveIndex < 0 || objectiveIndex >= mission.objectives.Count)
                return false;

            if (mission.completedObjectives.Contains(objectiveIndex))
                return false;

            mission.completedObjectives.Add(objectiveIndex);
            OnObjectiveCompleted?.Invoke(mission, objectiveIndex);

            // If all objectives are complete, finish the mission
            if (mission.completedObjectives.Count >= mission.objectives.Count)
                FinishMission(mission, success: true);

            SaveMissions();
            return true;
        }

        /// <summary>
        /// Forcefully completes or fails an active mission.
        /// </summary>
        public void FinishMission(string missionId, bool success)
        {
            var mission = GetActiveMission(missionId);
            if (mission != null)
                FinishMission(mission, success);
        }

        /// <summary>Returns all currently active missions (read-only).</summary>
        public IReadOnlyList<SquadronMission> GetActiveMissions() => _activeMissions.AsReadOnly();

        /// <summary>Returns completed missions (read-only).</summary>
        public IReadOnlyList<SquadronMission> GetCompletedMissions() => _completedMissions.AsReadOnly();

        // ── Private helpers ────────────────────────────────────────────────────

        private void FinishMission(SquadronMission mission, bool success)
        {
            mission.isActive = false;
            _activeMissions.Remove(mission);
            _completedMissions.Add(mission);

            if (success)
            {
                // Award XP to squadron
                int xpReward = CalculateMissionXP(mission);
                SquadronManager.Instance?.AddSquadronXP(xpReward);
                OnMissionCompleted?.Invoke(mission);
            }
            else
            {
                OnMissionFailed?.Invoke(mission);
            }

            // Start cooldown
            string cooldownKey = $"{SquadronManager.Instance?.CurrentSquadron?.squadronId}_{mission.missionType}";
            _cooldownMap[cooldownKey] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + SquadronConfig.MissionCooldownSeconds;

            SaveMissions();
        }

        private static int CalculateMissionXP(SquadronMission mission)
        {
            int baseXP = 100 * mission.difficulty;
            int objectiveBonus = mission.completedObjectives.Count * 25;
            return baseXP + objectiveBonus;
        }

        private SquadronMission GetActiveMission(string id)
            => _activeMissions.FirstOrDefault(m => m.missionId == id);

        private IEnumerator MissionTimeoutCoroutine(SquadronMission mission, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_activeMissions.Contains(mission))
                FinishMission(mission, success: false);
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveMissions()
        {
            try
            {
                var wrapper = new MissionListWrapper
                {
                    active    = _activeMissions,
                    completed = _completedMissions
                };
                File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, SquadronConfig.MissionsDataFile),
                    JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronMissionController] Save error: {ex.Message}");
            }
        }

        private void LoadMissions()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SquadronConfig.MissionsDataFile);
                if (!File.Exists(path)) return;

                var wrapper = JsonUtility.FromJson<MissionListWrapper>(File.ReadAllText(path));
                if (wrapper == null) return;

                _activeMissions.Clear();
                _completedMissions.Clear();

                if (wrapper.active   != null) _activeMissions.AddRange(wrapper.active);
                if (wrapper.completed != null) _completedMissions.AddRange(wrapper.completed);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronMissionController] Load error: {ex.Message}");
            }
        }

        [Serializable]
        private class MissionListWrapper
        {
            public List<SquadronMission> active;
            public List<SquadronMission> completed;
        }
    }
}
