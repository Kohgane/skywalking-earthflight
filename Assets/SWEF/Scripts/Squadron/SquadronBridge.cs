// SquadronBridge.cs — Phase 109: Clan/Squadron System
// Integration layer — connects Squadron system to Progression, Achievement, Social,
// Mission, and Multiplayer systems via compile-time feature guards.
// Namespace: SWEF.Squadron

using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Bridges the Squadron system with other SWEF systems.
    /// All integration calls are wrapped in <c>#if SWEF_*_AVAILABLE</c> compile guards
    /// so the Squadron module compiles cleanly without any dependency.
    /// </summary>
    public sealed class SquadronBridge : MonoBehaviour
    {
        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ── Event subscriptions ────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (SquadronManager.Instance != null)
            {
                SquadronManager.Instance.OnSquadronCreated  += HandleSquadronCreated;
                SquadronManager.Instance.OnSquadronJoined   += HandleSquadronJoined;
                SquadronManager.Instance.OnSquadronLeft     += HandleSquadronLeft;
                SquadronManager.Instance.OnSquadronDisbanded += HandleSquadronDisbanded;
                SquadronManager.Instance.OnMemberPromoted   += HandleMemberPromoted;
            }

            if (SquadronMissionController.Instance != null)
            {
                SquadronMissionController.Instance.OnMissionCompleted += HandleMissionCompleted;
            }

            if (SquadronEventScheduler.Instance != null)
            {
                SquadronEventScheduler.Instance.OnEventStarted += HandleEventStarted;
            }

            if (SquadronBaseManager.Instance != null)
            {
                SquadronBaseManager.Instance.OnFacilityUpgraded += HandleFacilityUpgraded;
            }
        }

        private void UnsubscribeEvents()
        {
            if (SquadronManager.Instance != null)
            {
                SquadronManager.Instance.OnSquadronCreated  -= HandleSquadronCreated;
                SquadronManager.Instance.OnSquadronJoined   -= HandleSquadronJoined;
                SquadronManager.Instance.OnSquadronLeft     -= HandleSquadronLeft;
                SquadronManager.Instance.OnSquadronDisbanded -= HandleSquadronDisbanded;
                SquadronManager.Instance.OnMemberPromoted   -= HandleMemberPromoted;
            }

            if (SquadronMissionController.Instance != null)
            {
                SquadronMissionController.Instance.OnMissionCompleted -= HandleMissionCompleted;
            }

            if (SquadronEventScheduler.Instance != null)
            {
                SquadronEventScheduler.Instance.OnEventStarted -= HandleEventStarted;
            }

            if (SquadronBaseManager.Instance != null)
            {
                SquadronBaseManager.Instance.OnFacilityUpgraded -= HandleFacilityUpgraded;
            }
        }

        // ── Squadron lifecycle handlers ─────────────────────────────────────────

        private void HandleSquadronCreated(SquadronInfo info)
        {
#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(100);
#endif
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("first_squadron");
            SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("squadron_leader");
#endif
#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity("squadron_created", info.name);
#endif
        }

        private void HandleSquadronJoined(SquadronInfo info)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("first_squadron");
#endif
#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity("squadron_joined", info.name);
#endif
        }

        private void HandleSquadronLeft(string squadronId)
        {
#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity("squadron_left", squadronId);
#endif
        }

        private void HandleSquadronDisbanded(string squadronId)
        {
#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity("squadron_disbanded", squadronId);
#endif
        }

        private void HandleMemberPromoted(SquadronMember member)
        {
            if (member.rank == SquadronRank.Leader)
            {
#if SWEF_ACHIEVEMENT_AVAILABLE
                SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("squadron_leader");
#endif
            }
        }

        // ── Mission handlers ───────────────────────────────────────────────────

        private void HandleMissionCompleted(SquadronMission mission)
        {
            // Award XP to all participants
            int xp = 50 * mission.difficulty;

#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddXP(xp);
#endif

            // Check achievement milestones
            int totalCompleted = SquadronMissionController.Instance?.GetCompletedMissions().Count ?? 0;

#if SWEF_ACHIEVEMENT_AVAILABLE
            if (totalCompleted >= 10)
                SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("squadron_mission_10");
#endif

#if SWEF_MULTIPLAYER_AVAILABLE
            // Squadron missions auto-create a multiplayer session
            // SWEF.Multiplayer.MultiplayerSessionManager.Instance?.CreateSquadronSession(mission.missionId);
#endif
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void HandleEventStarted(SquadronEvent ev)
        {
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("squadron_event_host");
#endif
#if SWEF_SOCIAL_AVAILABLE
            SWEF.Social.SocialFeedManager.Instance?.PostActivity("squadron_event_started", ev.title);
#endif
        }

        // ── Base handlers ──────────────────────────────────────────────────────

        private void HandleFacilityUpgraded(SquadronFacility facility, int newLevel)
        {
            if (newLevel < SquadronConfig.FacilityMaxLevel) return;

            // Check if ALL facilities are max level
            var baseManager = SquadronBaseManager.Instance;
            if (baseManager?.CurrentBase == null) return;

            bool allMaxed = true;
            foreach (SquadronFacility f in System.Enum.GetValues(typeof(SquadronFacility)))
            {
                if (baseManager.GetFacilityLevel(f) < SquadronConfig.FacilityMaxLevel)
                {
                    allMaxed = false;
                    break;
                }
            }

#if SWEF_ACHIEVEMENT_AVAILABLE
            if (allMaxed)
                SWEF.Achievement.AchievementManager.Instance?.TriggerAchievement("squadron_base_maxed");
#endif
        }
    }
}
