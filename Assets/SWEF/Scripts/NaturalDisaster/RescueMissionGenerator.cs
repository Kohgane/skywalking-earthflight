// RescueMissionGenerator.cs — SWEF Natural Disaster & Dynamic World Events (Phase 86)
using System.Collections.Generic;
using UnityEngine;

#if SWEF_MISSION_AVAILABLE
using SWEF.Mission;
#endif

namespace SWEF.NaturalDisaster
{
    /// <summary>
    /// Phase 86 — MonoBehaviour that listens to <see cref="DisasterManager"/> events and
    /// auto-generates rescue <c>MissionData</c> ScriptableObjects when a disaster reaches
    /// the Onset or Peak phase (if <see cref="DisasterData.canTriggerRescueMission"/> is
    /// true and the random roll passes <see cref="DisasterData.rescueMissionChance"/>).
    ///
    /// <para>Generated missions are submitted to <c>MissionManager.Instance.LoadMission()</c>
    /// when the <c>SWEF_MISSION_AVAILABLE</c> compile symbol is defined.</para>
    /// </summary>
    public class RescueMissionGenerator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Rescue Mission Settings")]
        [Tooltip("Base reward XP for a difficulty-1 rescue mission.")]
        [SerializeField] [Min(0)] private int _baseRewardXP = 200;

        [Tooltip("XP reward multiplier added per difficulty level.")]
        [SerializeField] [Min(0)] private int _xpPerDifficulty = 100;

        // ── Tracking ──────────────────────────────────────────────────────────────

        private readonly Dictionary<string, string> _disasterToMissionId
            = new Dictionary<string, string>();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DisasterManager.Instance != null)
            {
                DisasterManager.Instance.OnDisasterPhaseChanged += HandlePhaseChanged;
                DisasterManager.Instance.OnDisasterEnded        += HandleDisasterEnded;
            }
        }

        private void OnDisable()
        {
            if (DisasterManager.Instance != null)
            {
                DisasterManager.Instance.OnDisasterPhaseChanged -= HandlePhaseChanged;
                DisasterManager.Instance.OnDisasterEnded        -= HandleDisasterEnded;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandlePhaseChanged(ActiveDisaster disaster)
        {
            if (disaster == null || disaster.data == null) return;
            if (!disaster.data.canTriggerRescueMission)    return;

            // Only trigger on Onset or Peak
            if (disaster.currentPhase != DisasterPhase.Onset &&
                disaster.currentPhase != DisasterPhase.Peak)
                return;

            // Avoid generating a second mission for the same disaster
            string disasterId = disaster.data.disasterId + "_" + disaster.GetInstanceID();
            if (_disasterToMissionId.ContainsKey(disasterId)) return;

            // Roll against rescueMissionChance
            if (UnityEngine.Random.value > disaster.data.rescueMissionChance) return;

            GenerateMission(disaster, disasterId);
        }

        private void HandleDisasterEnded(ActiveDisaster disaster)
        {
            if (disaster == null || disaster.data == null) return;
            string disasterId = disaster.data.disasterId + "_" + disaster.GetInstanceID();
            _disasterToMissionId.Remove(disasterId);
        }

        // ── Mission Generation ────────────────────────────────────────────────────

        private void GenerateMission(ActiveDisaster disaster, string trackingKey)
        {
            DisasterData d = disaster.data;

#if SWEF_MISSION_AVAILABLE
            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.missionId          = $"rescue_{d.disasterId}_{System.Guid.NewGuid():N}";
            mission.missionName        = $"Rescue: {d.disasterName}";
            mission.shortDescription   = $"Emergency rescue operation — {d.disasterName} event.";
            mission.briefingText       = BuildBriefingText(d, disaster.currentSeverity);
            mission.type               = MissionType.Custom;
            mission.difficulty         = MapDifficulty(d.rescueMissionDifficulty);
            mission.recommendedLevel   = d.rescueMissionDifficulty;
            mission.timeLimit          = CalcTimeLimit(disaster.currentSeverity);
            mission.hasTimeLimit       = true;
            mission.objectives         = BuildObjectives(d, disaster.epicenter);
            mission.checkpoints        = BuildCheckpoints(disaster.epicenter, d.hazardRadius);

            MissionReward reward        = new MissionReward();
            reward.xpReward            = _baseRewardXP + _xpPerDifficulty * d.rescueMissionDifficulty;
            mission.reward             = reward;

            _disasterToMissionId[trackingKey] = mission.missionId;

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.LoadMission(mission);
                Debug.Log($"[SWEF] RescueMissionGenerator: loaded rescue mission '{mission.missionName}'.");
            }
            else
            {
                Debug.LogWarning("[SWEF] RescueMissionGenerator: MissionManager not found; mission not loaded.");
            }
#else
            _disasterToMissionId[trackingKey] = $"rescue_{d.disasterId}";
            Debug.Log($"[SWEF] RescueMissionGenerator: rescue mission would be generated for '{d.disasterName}' " +
                      "(SWEF_MISSION_AVAILABLE not defined).");
#endif
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildBriefingText(DisasterData d, DisasterSeverity severity)
        {
            return $"A {severity} {d.type} has struck the area. " +
                   "Civilians are in danger. Complete all rescue objectives before time runs out.";
        }

        private static float CalcTimeLimit(DisasterSeverity severity)
        {
            return DisasterConfig.RescueMissionTimeLimitBase
                   + DisasterConfig.RescueMissionTimeLimitScalar * (int)severity;
        }

#if SWEF_MISSION_AVAILABLE
        private static MissionDifficulty MapDifficulty(int level)
        {
            switch (level)
            {
                case 1:  return MissionDifficulty.Easy;
                case 2:  return MissionDifficulty.Normal;
                case 3:  return MissionDifficulty.Normal;
                case 4:  return MissionDifficulty.Hard;
                default: return MissionDifficulty.Expert;
            }
        }

        private static List<MissionObjective> BuildObjectives(DisasterData d, Vector3 epicenter)
        {
            var objectives = new List<MissionObjective>();
            RescueObjectiveType[] types = PickObjectiveTypes(d);

            foreach (RescueObjectiveType ot in types)
            {
                var obj = new MissionObjective
                {
                    objectiveId = $"obj_{ot}_{System.Guid.NewGuid():N}",
                    description = DescribeObjective(ot, d),
                    isRequired  = true
                };
                objectives.Add(obj);
            }
            return objectives;
        }

        private static List<MissionCheckpoint> BuildCheckpoints(Vector3 epicenter, float radius)
        {
            var checkpoints = new List<MissionCheckpoint>();
            int count = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                Vector3 pos = epicenter + new Vector3(
                    Mathf.Cos(angle) * radius * 1.1f,
                    0f,
                    Mathf.Sin(angle) * radius * 1.1f);

                checkpoints.Add(new MissionCheckpoint
                {
                    checkpointId    = $"cp_{i}",
                    worldPosition   = pos,
                    triggerRadius   = 300f,
                    isRequired      = true
                });
            }
            return checkpoints;
        }
#endif

        private static RescueObjectiveType[] PickObjectiveTypes(DisasterData d)
        {
            switch (d.type)
            {
                case DisasterType.Wildfire:
                    return new[] { RescueObjectiveType.Extinguish, RescueObjectiveType.Evacuate };
                case DisasterType.Flood:
                case DisasterType.Tsunami:
                    return new[] { RescueObjectiveType.Evacuate, RescueObjectiveType.SupplyDrop };
                case DisasterType.Earthquake:
                    return new[] { RescueObjectiveType.Search, RescueObjectiveType.MedicalAid };
                case DisasterType.Hurricane:
                case DisasterType.Tornado:
                    return new[] { RescueObjectiveType.Evacuate, RescueObjectiveType.Escort };
                default:
                    return new[] { RescueObjectiveType.Evacuate, RescueObjectiveType.SupplyDrop,
                                   RescueObjectiveType.MedicalAid };
            }
        }

        private static string DescribeObjective(RescueObjectiveType ot, DisasterData d)
        {
            switch (ot)
            {
                case RescueObjectiveType.Evacuate:    return $"Evacuate civilians from the {d.disasterName} zone.";
                case RescueObjectiveType.SupplyDrop:  return "Deliver emergency supplies to the affected area.";
                case RescueObjectiveType.MedicalAid:  return "Transport medical aid teams to the disaster site.";
                case RescueObjectiveType.Search:      return "Search for survivors in the disaster zone.";
                case RescueObjectiveType.Escort:      return "Escort evacuation convoy to safety.";
                case RescueObjectiveType.Extinguish:  return "Assist firefighting aircraft in suppressing the blaze.";
                default:                              return "Complete the rescue objective.";
            }
        }
    }
}
