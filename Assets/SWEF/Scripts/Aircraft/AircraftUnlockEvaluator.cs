using UnityEngine;
using SWEF.Achievement;
using SWEF.DailyChallenge;
using SWEF.Events;
using SWEF.HiddenGems;
using SWEF.IAP;
using SWEF.Progression;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Static utility that evaluates whether an
    /// <see cref="AircraftUnlockCondition"/> is currently satisfied.
    /// Does not hold any state of its own; delegates to the relevant singleton
    /// managers using null-guarded <c>FindObjectOfType</c> calls where
    /// compile-time types are not guaranteed to be present.
    /// </summary>
    public static class AircraftUnlockEvaluator
    {
        /// <summary>
        /// Returns <c>true</c> if every requirement attached to
        /// <paramref name="skin"/> is currently satisfied.
        /// </summary>
        public static bool IsUnlockable(AircraftSkinDefinition skin)
        {
            if (skin == null) return false;

            // Already unlocked? Always report as unlockable.
            if (AircraftCustomizationManager.Instance != null &&
                AircraftCustomizationManager.Instance.IsSkinUnlocked(skin.skinId))
                return true;

            // Pilot rank hard-requirement
            if (skin.requiredPilotRank > 0)
            {
                var rankMgr = ProgressionManager.Instance;
                if (rankMgr == null || rankMgr.CurrentRankLevel < skin.requiredPilotRank)
                    return false;
            }

            return EvaluateCondition(skin.unlockCondition);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="condition"/> is currently met.
        /// </summary>
        public static bool EvaluateCondition(AircraftUnlockCondition condition)
        {
            if (condition == null) return true;

            switch (condition.conditionType)
            {
                case AircraftUnlockType.Free:
                    return true;

                case AircraftUnlockType.PilotRank:
                {
                    var mgr = ProgressionManager.Instance;
                    if (mgr == null)
                    {
                        Debug.LogWarning("[AircraftUnlockEvaluator] ProgressionManager not found.");
                        return false;
                    }
                    return mgr.CurrentRankLevel >= (int)condition.targetValue;
                }

                case AircraftUnlockType.Achievement:
                {
                    var mgr = AchievementManager.Instance;
                    if (mgr == null)
                    {
                        Debug.LogWarning("[AircraftUnlockEvaluator] AchievementManager not found.");
                        return false;
                    }
                    return mgr.IsUnlocked(condition.targetId);
                }

                case AircraftUnlockType.Purchase:
                {
                    var mgr = IAPManager.Instance;
                    if (mgr == null)
                    {
                        Debug.LogWarning("[AircraftUnlockEvaluator] IAPManager not found.");
                        return false;
                    }
                    return mgr.HasPurchased(condition.targetId);
                }

                case AircraftUnlockType.SeasonPass:
                {
                    var mgr = SeasonPassManager.Instance;
                    if (mgr == null) return false;
                    // Check that the season is active and the player has reached the required tier.
                    return mgr.IsSeasonActive() &&
                           mgr.GetCurrentTier() >= (int)condition.targetValue;
                }

                case AircraftUnlockType.HiddenGem:
                {
                    var mgr = HiddenGemManager.Instance;
                    if (mgr == null) return false;
                    return mgr.IsGemDiscovered(condition.targetId);
                }

                case AircraftUnlockType.Event:
                {
                    var mgr = Object.FindObjectOfType<EventParticipationTracker>();
                    if (mgr == null) return false;
                    // targetId stores the event instance GUID as a string.
                    if (System.Guid.TryParse(condition.targetId, out System.Guid guid))
                        return mgr.IsParticipatingIn(guid);
                    return false;
                }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns a human-readable progress hint for the given unlock condition,
        /// e.g. "Reach Rank 5" or "Complete 'Sky Explorer' achievement".
        /// </summary>
        public static string GetUnlockProgressText(AircraftUnlockCondition condition)
        {
            if (condition == null) return string.Empty;

            switch (condition.conditionType)
            {
                case AircraftUnlockType.Free:
                    return "Free";

                case AircraftUnlockType.PilotRank:
                    return $"Reach Rank {(int)condition.targetValue}";

                case AircraftUnlockType.Achievement:
                    return $"Complete '{condition.targetId}' achievement";

                case AircraftUnlockType.Purchase:
                    return "Available for purchase";

                case AircraftUnlockType.SeasonPass:
                    return $"Requires Season Pass: {condition.targetId}";

                case AircraftUnlockType.HiddenGem:
                    return $"Discover hidden gem: {condition.targetId}";

                case AircraftUnlockType.Event:
                    return $"Participate in event: {condition.targetId}";

                default:
                    return "Unknown unlock condition";
            }
        }
    }
}
