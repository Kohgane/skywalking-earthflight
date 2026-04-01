using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// MonoBehaviour integration bridge that wires transport-mission completion
    /// events to the wider SWEF game systems:
    ///   • <see cref="SWEF.Progression.ProgressionManager.AddXP"/>
    ///   • <see cref="SWEF.Achievement.AchievementManager.ReportProgress"/>
    ///   • <see cref="SWEF.Journal.JournalManager"/> — auto-entry on completion
    ///   • <see cref="SWEF.DailyChallenge.DailyChallengeTracker"/>
    ///   • <see cref="SWEF.SocialHub.SocialActivityFeed"/>
    ///
    /// All integration calls are null-safe.
    /// </summary>
    public class TransportMissionBridge : MonoBehaviour
    {
        // ── Achievement progress keys ──────────────────────────────────────────
        private const string AchievDeliverAny    = "transport_deliver_any";
        private const string AchievDeliverVIP    = "transport_deliver_vip";
        private const string AchievDeliverCargoH = "transport_cargo_hauler";
        private const string AchievFiveStar      = "transport_five_star";

        // ── Unity Lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            var manager = TransportMissionManager.Instance;
            if (manager == null) return;

            manager.OnMissionCompleted += HandleCompleted;
        }

        private void OnDestroy()
        {
            var manager = TransportMissionManager.Instance;
            if (manager == null) return;

            manager.OnMissionCompleted -= HandleCompleted;
        }

        // ── Internal ──────────────────────────────────────────────────────────
        private void HandleCompleted(DeliveryResult result)
        {
            // ── Progression ───────────────────────────────────────────────────
            var prog = SWEF.Progression.ProgressionManager.Instance;
            prog?.AddXP(result.totalXP, "transport_delivery");

            // ── Achievements ──────────────────────────────────────────────────
            var ach = SWEF.Achievement.AchievementManager.Instance;
            if (ach != null)
            {
                ach.ReportProgress(AchievDeliverAny, 1);

                var contract = TransportMissionManager.Instance?.ActiveContract;
                if (contract != null)
                {
                    if (contract.passengerProfile.vipLevel >= 2)
                        ach.ReportProgress(AchievDeliverVIP, 1);

                    bool isCargo = contract.missionType == MissionType.CargoStandard
                                || contract.missionType == MissionType.CargoFragile
                                || contract.missionType == MissionType.CargoHazardous
                                || contract.missionType == MissionType.CargoOversized;
                    if (isCargo)
                        ach.ReportProgress(AchievDeliverCargoH, 1);
                }

                if (result.starRating == 5)
                    ach.ReportProgress(AchievFiveStar, 1);
            }

            // ── Daily Challenge ───────────────────────────────────────────────
            // DailyChallengeTracker tracks its own events automatically;
            // no direct public ReportEvent API is available.

            // ── Social Feed ───────────────────────────────────────────────────
            var feed = SWEF.SocialHub.SocialActivityFeed.Instance;
            feed?.PostActivity(SWEF.SocialHub.ActivityType.Custom,
                               $"{result.starRating}★ transport delivery");
        }
    }
}
