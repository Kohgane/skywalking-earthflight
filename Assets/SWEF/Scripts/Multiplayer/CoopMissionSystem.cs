using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Available co-operative mission types.
    /// </summary>
    public enum MissionType
    {
        /// <summary>Protect a target aircraft to its destination.</summary>
        Escort,
        /// <summary>Pass a token between waypoints as a team.</summary>
        Relay,
        /// <summary>Maintain a specified formation for a set duration.</summary>
        FormationChallenge,
        /// <summary>Locate and retrieve survivors scattered across the map.</summary>
        SearchAndRescue,
        /// <summary>Photograph or scan a series of ground targets.</summary>
        Recon,
        /// <summary>Complete all objectives before the countdown expires.</summary>
        TimeAttack_Coop
    }

    /// <summary>
    /// Types of individual mission objectives.
    /// </summary>
    public enum ObjectiveType
    {
        ReachWaypoint,
        FormationHold,
        EscortTarget,
        PhotographTarget,
        RescueTarget,
        SurviveFor,
        DefeatEnemies
    }

    /// <summary>
    /// Player roles that can be assigned within a co-op mission.
    /// </summary>
    public enum MissionRole
    {
        Lead,
        Wingman,
        Support,
        Scout
    }

    /// <summary>
    /// Mission lifecycle state machine.
    /// </summary>
    public enum MissionState
    {
        NotStarted,
        Briefing,
        InProgress,
        Completed,
        Failed
    }

    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A single mission objective that all players work toward.
    /// </summary>
    [Serializable]
    public class MissionObjective
    {
        /// <summary>Unique identifier for this objective.</summary>
        public string id;
        /// <summary>Human-readable description shown in the HUD.</summary>
        public string description;
        /// <summary>Type of action required to complete this objective.</summary>
        public ObjectiveType type;
        /// <summary>Target world-space position (used for waypoints, escort targets, etc.).</summary>
        public Vector3 targetPosition;
        /// <summary>Completion radius around the target position (metres).</summary>
        public float radius;
        /// <summary>Whether this objective is optional (failure does not fail the mission).</summary>
        public bool isOptional;
        /// <summary>Whether this objective has been completed.</summary>
        public bool isCompleted;
        /// <summary>Floating-point progress in [0, 1] for gradual objectives.</summary>
        public float progress;
    }

    /// <summary>
    /// Definition of a co-operative mission. Can be treated like a ScriptableObject data class.
    /// </summary>
    [Serializable]
    public class CoopMissionData
    {
        /// <summary>Unique mission identifier (e.g. "mission_escort_01").</summary>
        public string missionId;
        /// <summary>Mission category type.</summary>
        public MissionType type;
        /// <summary>Display title shown to players.</summary>
        public string title;
        /// <summary>Mission briefing description.</summary>
        public string description;
        /// <summary>All objectives for this mission.</summary>
        public List<MissionObjective> objectives = new();
        /// <summary>Time limit in seconds (0 = no limit).</summary>
        public float timeLimit;
        /// <summary>Minimum number of players required to start the mission.</summary>
        public int minPlayers = 1;
        /// <summary>Maximum number of players allowed in the mission.</summary>
        public int maxPlayers = 8;
        /// <summary>Base XP reward on mission completion.</summary>
        public int baseXpReward = 500;
    }

    /// <summary>
    /// Runtime state for a mission session in progress.
    /// </summary>
    [Serializable]
    public class MissionSession
    {
        /// <summary>The mission definition being played.</summary>
        public CoopMissionData missionData;
        /// <summary>Current lifecycle state.</summary>
        public MissionState state = MissionState.NotStarted;
        /// <summary>Remaining time in seconds (counts down from <see cref="CoopMissionData.timeLimit"/>).</summary>
        public float remainingTime;
        /// <summary>Role assignments: playerId → role.</summary>
        public Dictionary<string, MissionRole> roleAssignments = new();
        /// <summary>Formation quality bonus multiplier tracked during FormationChallenge missions.</summary>
        public float formationBonus = 1f;
    }

    // ── CoopMissionSystem ─────────────────────────────────────────────────────────

    /// <summary>
    /// Co-operative mission framework for Phase 33.
    ///
    /// <para>Manages mission lifecycle (NotStarted → Briefing → InProgress → Completed/Failed),
    /// role assignment, shared objective progress, difficulty scaling based on player count,
    /// and XP rewards with formation quality bonus.</para>
    /// </summary>
    public class CoopMissionSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static CoopMissionSystem Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Briefing")]
        [Tooltip("Duration of the briefing phase before the mission starts (seconds).")]
        [SerializeField] private float briefingDuration = 10f;

        [Header("Difficulty Scaling")]
        [Tooltip("Multiplier applied to objective radius per additional player beyond minimum.")]
        [SerializeField] private float radiusScalePerPlayer = 0.9f;

        [Tooltip("Multiplier applied to time limit per additional player beyond minimum.")]
        [SerializeField] private float timeLimitScalePerPlayer = 1.05f;

        [Header("Rewards")]
        [Tooltip("Maximum formation quality bonus multiplier.")]
        [SerializeField] private float maxFormationBonus = 2f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when the mission transitions to InProgress state.</summary>
        public event Action<MissionSession> OnMissionStarted;

        /// <summary>Fired when any objective is completed, with the objective ID.</summary>
        public event Action<string> OnObjectiveCompleted;

        /// <summary>Fired when the mission is successfully completed.</summary>
        public event Action<MissionSession, int> OnMissionCompleted;

        /// <summary>Fired when the mission fails (timeout or critical objective failed).</summary>
        public event Action<MissionSession> OnMissionFailed;

        /// <summary>Fired when a role is assigned to a player.</summary>
        public event Action<string, MissionRole> OnRoleAssigned;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The currently active mission session, or null if no mission is running.</summary>
        public MissionSession CurrentSession { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<CoopMissionData> _availableMissions = new();
        private Coroutine _briefingCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            RegisterDefaultMissions();
        }

        private void Update()
        {
            if (CurrentSession == null || CurrentSession.state != MissionState.InProgress) return;

            // Tick countdown timer.
            if (CurrentSession.missionData.timeLimit > 0f)
            {
                CurrentSession.remainingTime -= Time.deltaTime;
                if (CurrentSession.remainingTime <= 0f)
                {
                    CurrentSession.remainingTime = 0f;
                    FailMission("Time limit exceeded.");
                    return;
                }
            }

            // Check for all required objectives completed.
            CheckObjectiveCompletion();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts a mission by ID, entering the briefing phase first.
        /// </summary>
        /// <param name="missionId">Target mission identifier.</param>
        /// <param name="playerIds">All participating player IDs.</param>
        public void StartMission(string missionId, List<string> playerIds)
        {
            if (CurrentSession != null && CurrentSession.state == MissionState.InProgress)
            {
                Debug.LogWarning("[SWEF][CoopMissionSystem] A mission is already in progress.");
                return;
            }

            var data = _availableMissions.Find(m => m.missionId == missionId);
            if (data == null)
            {
                Debug.LogWarning($"[SWEF][CoopMissionSystem] Mission '{missionId}' not found.");
                return;
            }

            if (playerIds.Count < data.minPlayers)
            {
                Debug.LogWarning($"[SWEF][CoopMissionSystem] Not enough players ({playerIds.Count}/{data.minPlayers}).");
                return;
            }

            // Clone the mission data and scale difficulty.
            var scaledData = ScaleDifficulty(data, playerIds.Count);

            CurrentSession = new MissionSession
            {
                missionData   = scaledData,
                state         = MissionState.Briefing,
                remainingTime = scaledData.timeLimit
            };

            // Auto-assign roles.
            AssignRolesAutomatically(playerIds);

            if (_briefingCoroutine != null) StopCoroutine(_briefingCoroutine);
            _briefingCoroutine = StartCoroutine(BriefingPhase());

            Debug.Log($"[SWEF][CoopMissionSystem] Starting mission '{scaledData.title}' with {playerIds.Count} players.");
        }

        /// <summary>
        /// Manually assigns a role to a specific player (leader action).
        /// </summary>
        /// <param name="playerId">Player to assign.</param>
        /// <param name="role">Role to assign.</param>
        public void AssignRole(string playerId, MissionRole role)
        {
            if (CurrentSession == null) return;
            CurrentSession.roleAssignments[playerId] = role;
            Debug.Log($"[SWEF][CoopMissionSystem] {playerId} assigned role {role}.");
            OnRoleAssigned?.Invoke(playerId, role);
        }

        /// <summary>
        /// Reports progress toward a specific objective (0 = start, 1 = complete).
        /// </summary>
        /// <param name="objectiveId">Target objective identifier.</param>
        /// <param name="progress">Progress value in [0, 1].</param>
        public void ReportObjectiveProgress(string objectiveId, float progress)
        {
            if (CurrentSession == null || CurrentSession.state != MissionState.InProgress) return;

            var obj = CurrentSession.missionData.objectives.Find(o => o.id == objectiveId);
            if (obj == null || obj.isCompleted) return;

            obj.progress = Mathf.Clamp01(progress);
            if (obj.progress >= 1f)
                CompleteObjective(obj);
        }

        /// <summary>
        /// Sets the formation quality bonus (0–1) to influence the XP multiplier.
        /// </summary>
        /// <param name="formationQuality">Formation quality in [0, 1].</param>
        public void UpdateFormationBonus(float formationQuality)
        {
            if (CurrentSession == null) return;
            CurrentSession.formationBonus = 1f + Mathf.Clamp01(formationQuality) * (maxFormationBonus - 1f);
        }

        /// <summary>
        /// Returns available missions filtered by player count.
        /// </summary>
        /// <param name="playerCount">Number of players in the session.</param>
        public List<CoopMissionData> GetAvailableMissions(int playerCount) =>
            _availableMissions.FindAll(m => playerCount >= m.minPlayers && playerCount <= m.maxPlayers);

        // ── Mission lifecycle ─────────────────────────────────────────────────────

        private IEnumerator BriefingPhase()
        {
            yield return new WaitForSeconds(briefingDuration);
            CurrentSession.state = MissionState.InProgress;
            Debug.Log("[SWEF][CoopMissionSystem] Mission in progress!");
            OnMissionStarted?.Invoke(CurrentSession);
        }

        private void CheckObjectiveCompletion()
        {
            var objectives = CurrentSession.missionData.objectives;
            bool allRequired = true;

            foreach (var obj in objectives)
            {
                if (!obj.isOptional && !obj.isCompleted)
                    allRequired = false;
            }

            if (allRequired)
                CompleteMission();
        }

        private void CompleteObjective(MissionObjective obj)
        {
            obj.isCompleted = true;
            obj.progress    = 1f;
            Debug.Log($"[SWEF][CoopMissionSystem] Objective '{obj.id}' completed.");
            OnObjectiveCompleted?.Invoke(obj.id);
        }

        private void CompleteMission()
        {
            if (CurrentSession == null || CurrentSession.state != MissionState.InProgress) return;

            CurrentSession.state = MissionState.Completed;
            int xp = Mathf.RoundToInt(CurrentSession.missionData.baseXpReward * CurrentSession.formationBonus);

            Debug.Log($"[SWEF][CoopMissionSystem] Mission completed! XP awarded: {xp} (bonus ×{CurrentSession.formationBonus:F2}).");
            OnMissionCompleted?.Invoke(CurrentSession, xp);
        }

        private void FailMission(string reason)
        {
            if (CurrentSession == null) return;
            CurrentSession.state = MissionState.Failed;
            Debug.Log($"[SWEF][CoopMissionSystem] Mission failed: {reason}");
            OnMissionFailed?.Invoke(CurrentSession);
        }

        // ── Role assignment ───────────────────────────────────────────────────────

        private void AssignRolesAutomatically(List<string> playerIds)
        {
            var roles = new[] { MissionRole.Lead, MissionRole.Wingman, MissionRole.Support, MissionRole.Scout };
            for (int i = 0; i < playerIds.Count; i++)
            {
                var role = roles[i % roles.Length];
                AssignRole(playerIds[i], role);
            }
        }

        // ── Difficulty scaling ────────────────────────────────────────────────────

        private CoopMissionData ScaleDifficulty(CoopMissionData original, int playerCount)
        {
            int extra = Mathf.Max(0, playerCount - original.minPlayers);

            // Deep-copy objectives.
            var scaledObjectives = new List<MissionObjective>(original.objectives.Count);
            foreach (var obj in original.objectives)
            {
                scaledObjectives.Add(new MissionObjective
                {
                    id              = obj.id,
                    description     = obj.description,
                    type            = obj.type,
                    targetPosition  = obj.targetPosition,
                    radius          = obj.radius * Mathf.Pow(radiusScalePerPlayer, extra),
                    isOptional      = obj.isOptional,
                    isCompleted     = false,
                    progress        = 0f
                });
            }

            return new CoopMissionData
            {
                missionId   = original.missionId,
                type        = original.type,
                title       = original.title,
                description = original.description,
                objectives  = scaledObjectives,
                timeLimit   = original.timeLimit > 0f
                              ? original.timeLimit * Mathf.Pow(timeLimitScalePerPlayer, extra)
                              : 0f,
                minPlayers  = original.minPlayers,
                maxPlayers  = original.maxPlayers,
                baseXpReward = original.baseXpReward
            };
        }

        // ── Default missions ──────────────────────────────────────────────────────

        private void RegisterDefaultMissions()
        {
            _availableMissions.Add(new CoopMissionData
            {
                missionId   = "mission_escort_01",
                type        = MissionType.Escort,
                title       = "Operation Safe Passage",
                description = "Escort the supply aircraft to the landing zone.",
                timeLimit   = 600f,
                minPlayers  = 2,
                maxPlayers  = 4,
                baseXpReward = 750,
                objectives  = new List<MissionObjective>
                {
                    new MissionObjective { id="obj_escort_arrive", description="Escort the target to the LZ", type=ObjectiveType.EscortTarget, targetPosition=new Vector3(5000,500,5000), radius=200f }
                }
            });

            _availableMissions.Add(new CoopMissionData
            {
                missionId   = "mission_formation_01",
                type        = MissionType.FormationChallenge,
                title       = "Diamond Formation Hold",
                description = "Hold a diamond formation for 3 minutes.",
                timeLimit   = 300f,
                minPlayers  = 4,
                maxPlayers  = 4,
                baseXpReward = 500,
                objectives  = new List<MissionObjective>
                {
                    new MissionObjective { id="obj_hold_diamond", description="Hold diamond formation for 3 min", type=ObjectiveType.FormationHold, radius=50f }
                }
            });

            _availableMissions.Add(new CoopMissionData
            {
                missionId   = "mission_sar_01",
                type        = MissionType.SearchAndRescue,
                title       = "Search and Rescue",
                description = "Locate all survivors before time runs out.",
                timeLimit   = 900f,
                minPlayers  = 2,
                maxPlayers  = 8,
                baseXpReward = 1000,
                objectives  = new List<MissionObjective>
                {
                    new MissionObjective { id="obj_rescue_1", description="Rescue survivor Alpha", type=ObjectiveType.RescueTarget, targetPosition=new Vector3(1000,50,2000),  radius=100f },
                    new MissionObjective { id="obj_rescue_2", description="Rescue survivor Bravo", type=ObjectiveType.RescueTarget, targetPosition=new Vector3(3000,50,500),   radius=100f },
                    new MissionObjective { id="obj_rescue_3", description="Rescue survivor Charlie", type=ObjectiveType.RescueTarget, targetPosition=new Vector3(2000,50,4000), radius=100f, isOptional=true }
                }
            });
        }
    }
}
