using UnityEngine;

namespace SWEF.PassengerCargo
{
    /// <summary>
    /// Static utility that calculates the final <see cref="DeliveryResult"/> for
    /// a completed transport mission.
    ///
    /// Multiplier table:
    ///   Comfort Excellent (≥90)   ×2.0
    ///   Comfort Good      (70–89) ×1.5
    ///   Comfort Fair      (50–69) ×1.0
    ///   Comfort Poor      (30–49) ×0.5
    ///   Comfort Critical  (&lt;30)  ×0.25
    ///   Time — early delivery    up to +50 %
    ///   Time — overtime          −25 %
    ///   Cargo — per 10 % damage  −10 %
    ///   VIP mission              ×1.5
    ///   Streak (5+ consecutive)  ×1.25
    /// </summary>
    public static class TransportRewardCalculator
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Calculates the complete <see cref="DeliveryResult"/> from mission
        /// metrics.
        /// </summary>
        /// <param name="contract">The completed contract.</param>
        /// <param name="comfortScore">Final comfort score 0–100.</param>
        /// <param name="timeRemaining">Seconds remaining when delivered (negative = overtime).</param>
        /// <param name="totalTime">Total allotted time in seconds.</param>
        /// <param name="cargoDamagePercent">Cargo damage 0–100.</param>
        /// <param name="streak">Consecutive successful deliveries including this one.</param>
        public static DeliveryResult CalculateResult(
            TransportContract contract,
            float comfortScore,
            float timeRemaining,
            float totalTime,
            float cargoDamagePercent,
            int   streak)
        {
            float comfortMult   = ComfortMultiplier(comfortScore);
            float timeBonus     = TimeBonus(timeRemaining, totalTime);
            float cargoFactor   = CargoPenaltyFactor(cargoDamagePercent);
            float vipMult       = contract.passengerProfile.vipLevel >= 2 ? 1.5f : 1.0f;
            float streakMult    = streak >= 5 ? 1.25f : 1.0f;

            float totalMult = comfortMult * cargoFactor * vipMult * streakMult;
            float timeBonusAdd = timeBonus * contract.baseReward;

            long totalCoins = Mathf.RoundToInt(contract.baseReward * totalMult + timeBonusAdd
                                               + contract.bonusReward);
            long totalXP    = Mathf.RoundToInt(contract.baseXP * totalMult);

            totalCoins = System.Math.Max(0, totalCoins);
            totalXP    = System.Math.Max(0, totalXP);

            float compositeScore = CompositeScore(comfortScore, timeRemaining,
                                                   totalTime, cargoDamagePercent);
            int stars = StarRating(compositeScore);

            return new DeliveryResult
            {
                success           = true,
                comfortScore      = comfortScore,
                timeBonus         = timeBonus,
                damagePercentage  = cargoDamagePercent,
                totalXP           = totalXP,
                totalCoins        = totalCoins,
                starRating        = stars,
                contractId        = contract.contractId
            };
        }

        // ── Scoring Helpers ───────────────────────────────────────────────────

        /// <summary>Returns the reward multiplier for a given comfort score.</summary>
        public static float ComfortMultiplier(float score)
        {
            if (score >= 90f) return 2.00f;
            if (score >= 70f) return 1.50f;
            if (score >= 50f) return 1.00f;
            if (score >= 30f) return 0.50f;
            return 0.25f;
        }

        /// <summary>
        /// Returns an additive time-bonus fraction (e.g. 0.3 = +30 %).
        /// Negative when in overtime.
        /// </summary>
        public static float TimeBonus(float timeRemaining, float totalTime)
        {
            if (totalTime <= 0f) return 0f;

            if (timeRemaining >= 0f)
            {
                float fraction = timeRemaining / totalTime;
                return fraction * 0.50f;      // up to +50 %
            }

            return -0.25f;   // overtime penalty
        }

        /// <summary>
        /// Returns a multiplicative cargo-damage factor (1.0 = no damage).
        /// −10 % per 10 % damage.
        /// </summary>
        public static float CargoPenaltyFactor(float damagePercent)
        {
            float penalty = Mathf.Floor(damagePercent / 10f) * 0.10f;
            return Mathf.Max(0f, 1f - penalty);
        }

        /// <summary>Composite score in [0, 100] used for star rating.</summary>
        public static float CompositeScore(
            float comfortScore, float timeRemaining,
            float totalTime, float cargoDamagePercent)
        {
            float comfortContrib = comfortScore * 0.50f;   // 50 % weight

            float timeContrib;
            if (totalTime > 0f)
            {
                float frac = Mathf.Clamp(timeRemaining / totalTime, -1f, 1f);
                timeContrib = (frac * 0.5f + 0.5f) * 30f;  // 30 % weight → [0,30]
            }
            else
            {
                timeContrib = 30f;  // no time limit = full score
            }

            float cargoContrib = (1f - cargoDamagePercent / 100f) * 20f; // 20 % weight

            return Mathf.Clamp(comfortContrib + timeContrib + cargoContrib, 0f, 100f);
        }

        /// <summary>Maps a composite percentage score to 1–5 stars.</summary>
        public static int StarRating(float compositeScore)
        {
            if (compositeScore >= 90f) return 5;
            if (compositeScore >= 75f) return 4;
            if (compositeScore >= 55f) return 3;
            if (compositeScore >= 35f) return 2;
            return 1;
        }
    }
}
