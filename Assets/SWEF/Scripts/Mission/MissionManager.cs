// MissionManager.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Singleton MonoBehaviour that manages the full lifecycle of a mission:
    /// loading, briefing, in-progress tracking, checkpoint detection, objective monitoring,
    /// time-limit countdown, completion, failure, and result generation.
    ///
    /// <para>Attach to a persistent scene object (DontDestroyOnLoad).  Feed it a
    /// <see cref="MissionData"/> asset via <see cref="LoadMission"/> and call
    /// <see cref="StartMission"/> to begin.</para>
    /// </summary>
    public sealed class MissionManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Shared singleton instance.</summary>
        public static MissionManager Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Mission Pool")]
        [Tooltip("All MissionData assets that can be selected and started.")]
        /// <summary>Full pool of loadable missions.</summary>
        public List<MissionData> availableMissions = new List<MissionData>();

        [Header("Player Reference")]
        [Tooltip("Transform of the player aircraft used for checkpoint proximity tests.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Checkpoint Detection")]
        [Tooltip("Interval in seconds between checkpoint proximity checks.")]
        [SerializeField] [Min(0.05f)] private float _checkpointPollInterval = 0.1f;

        #endregion

        #region Public State

        /// <summary>The mission currently loaded into the manager (may be <c>null</c>).</summary>
        public MissionData currentMission { get; private set; }

        /// <summary>Current lifecycle state of the loaded mission.</summary>
        public MissionStatus currentStatus { get; private set; } = MissionStatus.Locked;

        /// <summary>Completed-mission history keyed by mission ID.</summary>
        public Dictionary<string, MissionResult> completedMissions { get; } =
            new Dictionary<string, MissionResult>();

        /// <summary>Elapsed time in seconds since the mission entered <see cref="MissionStatus.InProgress"/>.</summary>
        public float missionElapsedTime { get; private set; }

        /// <summary>Index of the next checkpoint the player must pass.</summary>
        public int currentCheckpointIndex { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired whenever <see cref="currentStatus"/> changes.</summary>
        public event Action<MissionStatus> OnMissionStatusChanged;

        /// <summary>Fired when any objective's status changes to <see cref="ObjectiveStatus.Completed"/>.</summary>
        public event Action<MissionObjective> OnObjectiveCompleted;

        /// <summary>Fired when the player passes a checkpoint.</summary>
        public event Action<MissionCheckpoint> OnCheckpointReached;

        /// <summary>Fired when a mission ends; the result object contains all outcome data.</summary>
        public event Action<MissionResult> OnMissionResult;

        #endregion

        #region Unity Lifecycle

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

        private void Update()
        {
            if (currentStatus != MissionStatus.InProgress) return;

            missionElapsedTime += Time.deltaTime;
            TickObjectiveTimers(Time.deltaTime);
            CheckTimeLimit();
        }

        #endregion

        #region Public API — Mission Lifecycle

        /// <summary>
        /// Loads a <see cref="MissionData"/> asset, resets all state, and enters the
        /// <see cref="MissionStatus.Briefing"/> state so the briefing UI can display.
        /// </summary>
        /// <param name="mission">The mission to load. Must not be <c>null</c>.</param>
        public void LoadMission(MissionData mission)
        {
            if (mission == null)
            {
                Debug.LogError("[MissionManager] LoadMission called with null MissionData.");
                return;
            }
            if (!IsMissionAvailable(mission))
            {
                Debug.LogWarning($"[MissionManager] Mission '{mission.missionId}' prerequisites not met.");
                return;
            }

            currentMission = mission;
            missionElapsedTime = 0f;
            currentCheckpointIndex = 0;

            ResetMissionState(mission);
            StopAllCoroutines();
            SetStatus(MissionStatus.Briefing);
        }

        /// <summary>
        /// Transitions from <see cref="MissionStatus.Briefing"/> to
        /// <see cref="MissionStatus.InProgress"/> and begins all runtime tracking.
        /// </summary>
        public void StartMission()
        {
            if (currentMission == null)
            {
                Debug.LogError("[MissionManager] StartMission called but no mission is loaded.");
                return;
            }
            if (currentStatus != MissionStatus.Briefing)
            {
                Debug.LogWarning($"[MissionManager] StartMission called in invalid state: {currentStatus}");
                return;
            }

            missionElapsedTime = 0f;
            ActivateFirstObjective();
            SetStatus(MissionStatus.InProgress);
            StartCoroutine(CheckpointPollingCoroutine());
        }

        /// <summary>Suspends the mission timer and tracking.</summary>
        public void PauseMission()
        {
            if (currentStatus != MissionStatus.InProgress) return;
            SetStatus(MissionStatus.Paused);
            StopAllCoroutines();
        }

        /// <summary>Resumes a paused mission.</summary>
        public void ResumeMission()
        {
            if (currentStatus != MissionStatus.Paused) return;
            SetStatus(MissionStatus.InProgress);
            StartCoroutine(CheckpointPollingCoroutine());
        }

        /// <summary>
        /// Evaluates all objectives and checkpoints, generates a <see cref="MissionResult"/>,
        /// fires <see cref="OnMissionResult"/>, and marks the mission as
        /// <see cref="MissionStatus.Completed"/>.
        /// </summary>
        public void CompleteMission()
        {
            if (currentMission == null) return;
            StopAllCoroutines();
            MissionResult result = BuildResult(MissionStatus.Completed);
            RecordAndBroadcast(result);
            SetStatus(MissionStatus.Completed);
        }

        /// <summary>Fails the mission with an optional reason string logged to the console.</summary>
        /// <param name="reason">Human-readable description of why the mission failed.</param>
        public void FailMission(string reason = "")
        {
            if (currentStatus != MissionStatus.InProgress && currentStatus != MissionStatus.Paused) return;
            StopAllCoroutines();
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[MissionManager] Mission failed: {reason}");
            MissionResult result = BuildResult(MissionStatus.Failed);
            RecordAndBroadcast(result);
            SetStatus(MissionStatus.Failed);
        }

        /// <summary>Abandons the current mission without generating a scored result.</summary>
        public void AbandonMission()
        {
            if (currentMission == null) return;
            StopAllCoroutines();
            SetStatus(MissionStatus.Abandoned);
            currentMission = null;
        }

        /// <summary>Reloads and restarts the current mission from the beginning.</summary>
        public void RestartMission()
        {
            if (currentMission == null) return;
            MissionData mission = currentMission;
            SetStatus(MissionStatus.Available);
            LoadMission(mission);
        }

        #endregion

        #region Public API — Queries

        /// <summary>
        /// Returns <c>true</c> if all prerequisite missions have been completed and — for
        /// already-completed missions — the mission is marked as replayable.
        /// </summary>
        /// <param name="mission">The candidate mission.</param>
        public bool IsMissionAvailable(MissionData mission)
        {
            if (mission == null) return false;
            if (completedMissions.ContainsKey(mission.missionId) && !mission.isReplayable)
                return false;
            foreach (string prereq in mission.prerequisiteMissionIds)
            {
                if (!completedMissions.ContainsKey(prereq)) return false;
            }
            return true;
        }

        #endregion

        #region Private — State Helpers

        private void SetStatus(MissionStatus newStatus)
        {
            currentStatus = newStatus;
            OnMissionStatusChanged?.Invoke(newStatus);
        }

        private void ResetMissionState(MissionData mission)
        {
            foreach (MissionObjective obj in mission.objectives)
            {
                obj.currentCount = 0;
                obj.status = ObjectiveStatus.Pending;
                obj.remainingTime = obj.timeLimit;
                obj.OnObjectiveStatusChanged -= HandleObjectiveStatusChanged;
                obj.OnObjectiveStatusChanged += HandleObjectiveStatusChanged;
            }
            foreach (MissionCheckpoint cp in mission.checkpoints)
            {
                cp.isPassed = false;
                cp.passedTime = 0f;
            }
        }

        private void ActivateFirstObjective()
        {
            if (currentMission == null) return;
            foreach (MissionObjective obj in currentMission.objectives)
            {
                if (obj.isHidden) continue;
                obj.status = ObjectiveStatus.Active;
                obj.remainingTime = obj.timeLimit;
                break;
            }
        }

        #endregion

        #region Private — Objective Handling

        private void HandleObjectiveStatusChanged(MissionObjective obj)
        {
            if (obj.status == ObjectiveStatus.Completed)
            {
                OnObjectiveCompleted?.Invoke(obj);
                ActivateNextObjective(obj);
                CheckAllRequiredObjectivesDone();
            }
            else if (obj.status == ObjectiveStatus.Failed && !obj.isOptional)
            {
                FailMission($"Required objective failed: {obj.description}");
            }
        }

        private void ActivateNextObjective(MissionObjective justCompleted)
        {
            if (currentMission == null) return;
            bool found = false;
            foreach (MissionObjective obj in currentMission.objectives)
            {
                if (found && obj.status == ObjectiveStatus.Pending && !obj.isHidden)
                {
                    obj.status = ObjectiveStatus.Active;
                    obj.remainingTime = obj.timeLimit;
                    break;
                }
                if (obj == justCompleted) found = true;
            }
        }

        private void CheckAllRequiredObjectivesDone()
        {
            if (currentMission == null) return;
            foreach (MissionObjective obj in currentMission.objectives)
            {
                if (!obj.isOptional && obj.status != ObjectiveStatus.Completed)
                    return;
            }
            CompleteMission();
        }

        private void TickObjectiveTimers(float delta)
        {
            if (currentMission == null) return;
            foreach (MissionObjective obj in currentMission.objectives)
            {
                if (obj.status != ObjectiveStatus.Active) continue;
                if (obj.timeLimit <= 0f) continue;

                obj.remainingTime -= delta;
                if (obj.remainingTime <= 0f)
                {
                    obj.remainingTime = 0f;
                    obj.Fail();
                }
            }
        }

        #endregion

        #region Private — Checkpoint Detection

        private IEnumerator CheckpointPollingCoroutine()
        {
            var wait = new WaitForSeconds(_checkpointPollInterval);
            while (currentStatus == MissionStatus.InProgress && currentMission != null)
            {
                PollCheckpoints();
                yield return wait;
            }
        }

        private void PollCheckpoints()
        {
            if (_playerTransform == null) return;
            if (currentMission == null) return;

            List<MissionCheckpoint> cps = currentMission.checkpoints;
            if (currentCheckpointIndex >= cps.Count) return;

            MissionCheckpoint next = cps[currentCheckpointIndex];
            if (next.isPassed) { currentCheckpointIndex++; return; }

            float dist = Vector3.Distance(_playerTransform.position, next.position);
            if (dist <= next.radius)
            {
                next.isPassed = true;
                next.passedTime = Time.time;
                OnCheckpointReached?.Invoke(next);
                currentCheckpointIndex++;
            }
        }

        #endregion

        #region Private — Time Limit

        private void CheckTimeLimit()
        {
            if (currentMission == null || currentMission.timeLimit <= 0f) return;
            if (missionElapsedTime >= currentMission.timeLimit)
                FailMission("Time limit exceeded.");
        }

        #endregion

        #region Private — Result Building

        private MissionResult BuildResult(MissionStatus finalStatus)
        {
            var result = new MissionResult();
            if (currentMission == null) return result;

            result.missionId    = currentMission.missionId;
            result.finalStatus  = finalStatus;
            result.totalTime    = missionElapsedTime;
            result.timeSavedSeconds = currentMission.timeLimit > 0f
                ? Mathf.Max(0f, currentMission.timeLimit - missionElapsedTime)
                : 0f;

            // Tally objectives
            foreach (MissionObjective obj in currentMission.objectives)
            {
                if (obj.isOptional)
                {
                    result.optionalTotal++;
                    if (obj.status == ObjectiveStatus.Completed)
                    {
                        result.optionalCompleted++;
                        result.totalScore += MissionConfig.OptionalObjectiveBonus;
                    }
                }
                else
                {
                    result.objectivesTotal++;
                    if (obj.status == ObjectiveStatus.Completed)
                    {
                        result.objectivesCompleted++;
                        result.totalScore += obj.scoreValue;
                    }
                }
            }

            // Tally checkpoints
            foreach (MissionCheckpoint cp in currentMission.checkpoints)
            {
                result.checkpointsTotal++;
                if (cp.isPassed) result.checkpointsReached++;
            }

            // Time bonus
            result.totalScore += Mathf.RoundToInt(result.timeSavedSeconds * MissionConfig.TimeBonusPerSecond);

            // Personal bests
            if (completedMissions.TryGetValue(currentMission.missionId, out MissionResult prev))
            {
                result.previousBestTime  = prev.totalTime;
                result.previousBestScore = prev.totalScore;
                result.isNewBestTime  = result.totalTime < prev.totalTime;
                result.isNewBestScore = result.totalScore > prev.totalScore;
            }
            else
            {
                result.isNewBestTime  = finalStatus == MissionStatus.Completed;
                result.isNewBestScore = finalStatus == MissionStatus.Completed;
            }

            result.CalculateRating();
            return result;
        }

        private void RecordAndBroadcast(MissionResult result)
        {
            if (result.finalStatus == MissionStatus.Completed)
                completedMissions[result.missionId] = result;

            OnMissionResult?.Invoke(result);
        }

        #endregion
    }
}
