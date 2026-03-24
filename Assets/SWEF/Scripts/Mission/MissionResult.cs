// MissionResult.cs — SWEF Mission Briefing & Objective System (Phase 70)
using System.Text;
using UnityEngine;

namespace SWEF.Mission
{
    /// <summary>
    /// Phase 70 — Stores the full result of a completed or failed mission run.
    ///
    /// <para>Created by <see cref="MissionManager.CompleteMission"/> /
    /// <see cref="MissionManager.FailMission"/> and passed to subscribers of
    /// <see cref="MissionManager.OnMissionResult"/>.  Also used as the input to
    /// <see cref="MissionReward.CalculateFinalExperience"/> and
    /// <see cref="MissionReward.CalculateFinalCurrency"/>.</para>
    /// </summary>
    public class MissionResult
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>ID of the mission this result belongs to.</summary>
        public string missionId;

        /// <summary>Final lifecycle state — typically <see cref="MissionStatus.Completed"/> or
        /// <see cref="MissionStatus.Failed"/>.</summary>
        public MissionStatus finalStatus;

        // ── Rating ────────────────────────────────────────────────────────────

        /// <summary>Letter rating calculated from <see cref="CalculateRating"/>.</summary>
        public MissionRating rating;

        // ── Time ──────────────────────────────────────────────────────────────

        /// <summary>Total wall-clock seconds elapsed from mission start to end.</summary>
        public float totalTime;

        /// <summary>
        /// Seconds saved vs. the mission's par time (positive = under par, negative = over).
        /// Used by <see cref="MissionReward"/> to compute time-bonus rewards.
        /// </summary>
        public float timeSavedSeconds;

        // ── Score ─────────────────────────────────────────────────────────────

        /// <summary>Raw accumulated score at mission end.</summary>
        public int totalScore;

        // ── Objectives ────────────────────────────────────────────────────────

        /// <summary>Number of required objectives successfully completed.</summary>
        public int objectivesCompleted;

        /// <summary>Total number of required objectives in the mission.</summary>
        public int objectivesTotal;

        /// <summary>Number of optional objectives completed.</summary>
        public int optionalCompleted;

        /// <summary>Total number of optional objectives in the mission.</summary>
        public int optionalTotal;

        // ── Checkpoints ───────────────────────────────────────────────────────

        /// <summary>Number of checkpoints the player passed.</summary>
        public int checkpointsReached;

        /// <summary>Total number of checkpoints in the mission.</summary>
        public int checkpointsTotal;

        /// <summary>Average checkpoint alignment accuracy in the range [0, 1].</summary>
        public float accuracyPercent;

        // ── Personal Bests ────────────────────────────────────────────────────

        /// <summary><c>true</c> if <see cref="totalTime"/> beats the player's previous best time.</summary>
        public bool isNewBestTime;

        /// <summary><c>true</c> if <see cref="totalScore"/> beats the player's previous best score.</summary>
        public bool isNewBestScore;

        /// <summary>Previous personal-best time in seconds (0 = no prior run).</summary>
        public float previousBestTime;

        /// <summary>Previous personal-best score (0 = no prior run).</summary>
        public int previousBestScore;

        // ── Rating Calculation ────────────────────────────────────────────────

        /// <summary>
        /// Derives a <see cref="MissionRating"/> from the combined completion ratio.
        ///
        /// <para>Score ratio = (objectivesCompleted + checkpointsReached) /
        /// max(objectivesTotal + checkpointsTotal, 1).</para>
        /// </summary>
        /// <returns>The calculated <see cref="MissionRating"/>.</returns>
        public MissionRating CalculateRating()
        {
            int totalItems = objectivesTotal + checkpointsTotal;
            if (totalItems <= 0)
            {
                rating = finalStatus == MissionStatus.Completed ? MissionRating.S : MissionRating.F;
                return rating;
            }

            float ratio = (float)(objectivesCompleted + checkpointsReached) / totalItems;

            if (ratio >= MissionConfig.SThreshold)      rating = MissionRating.S;
            else if (ratio >= MissionConfig.AThreshold) rating = MissionRating.A;
            else if (ratio >= MissionConfig.BThreshold) rating = MissionRating.B;
            else if (ratio >= MissionConfig.CThreshold) rating = MissionRating.C;
            else if (ratio >= MissionConfig.DThreshold) rating = MissionRating.D;
            else                                        rating = MissionRating.F;

            return rating;
        }

        // ── Summary ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable summary of the mission result suitable for a results screen.
        /// </summary>
        /// <returns>Formatted multi-line string.</returns>
        public string GetSummaryText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Mission: {missionId}");
            sb.AppendLine($"Status:  {finalStatus}");
            sb.AppendLine($"Rating:  {rating}");
            sb.AppendLine($"Time:    {FormatTime(totalTime)}");
            sb.AppendLine($"Score:   {totalScore:N0}");
            sb.AppendLine($"Objectives: {objectivesCompleted}/{objectivesTotal}" +
                          (optionalTotal > 0 ? $"  +{optionalCompleted}/{optionalTotal} optional" : string.Empty));
            sb.AppendLine($"Checkpoints: {checkpointsReached}/{checkpointsTotal}");
            if (isNewBestTime)  sb.AppendLine("★ New Best Time!");
            if (isNewBestScore) sb.AppendLine("★ New Best Score!");
            return sb.ToString();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m:D2}:{s:00.0}";
        }
    }
}
