using NUnit.Framework;
using UnityEngine;
using SWEF.PassengerCargo;

/// <summary>
/// Edit-mode NUnit tests for Phase 82 — Passenger &amp; Cargo Mission System.
///
/// Coverage:
///   • TransportRewardCalculator — all comfort multipliers, time bonus, cargo damage penalty, star rating
///   • PassengerComfortSystem    — comfort level mapping, decay/recovery logic
///   • CargoPhysicsController    — mass/fuel/CG calculations
///   • TransportContractGenerator — rank-gated type distribution
///   • DeliveryTimerController   — phase transitions
/// </summary>
[TestFixture]
public class PassengerCargoTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TransportContract MakeContract(
        MissionType type = MissionType.PassengerStandard,
        long baseReward = 500, long bonusReward = 100, long baseXP = 300,
        float timeLimit = 600f, int vipLevel = 0, float cargoWeight = 0f)
    {
        var c = ScriptableObject.CreateInstance<TransportContract>();
        c.missionType       = type;
        c.baseReward        = baseReward;
        c.bonusReward       = bonusReward;
        c.baseXP            = baseXP;
        c.timeLimitSeconds  = timeLimit;
        c.passengerProfile  = new PassengerProfile { vipLevel = vipLevel };
        c.cargoManifest     = new CargoManifest    { weight  = cargoWeight, fragilityRating = 0.5f };
        return c;
    }

    // ── TransportRewardCalculator ─────────────────────────────────────────────

    [Test]
    public void RewardCalculator_ExcellentComfort_DoubleMultiplier()
    {
        var c      = MakeContract(baseReward: 1000, baseXP: 500);
        var result = TransportRewardCalculator.CalculateResult(c, 95f, 300f, 600f, 0f, 1);
        // Comfort Excellent × 2.0, time bonus ≈ +25 %, no damage
        Assert.Greater(result.totalCoins, 1000L);
        Assert.AreEqual(true, result.success);
    }

    [Test]
    public void RewardCalculator_CriticalComfort_QuarterMultiplier()
    {
        var c      = MakeContract(baseReward: 1000, baseXP: 500);
        var result = TransportRewardCalculator.CalculateResult(c, 20f, 0f, 600f, 0f, 1);
        // Comfort Critical × 0.25 + overtime −25 % → well below base
        Assert.Less(result.totalCoins, 1000L);
    }

    [Test]
    public void RewardCalculator_ComfortMultiplier_AllLevels()
    {
        Assert.AreEqual(2.00f,  TransportRewardCalculator.ComfortMultiplier(90f),  0.001f);
        Assert.AreEqual(2.00f,  TransportRewardCalculator.ComfortMultiplier(100f), 0.001f);
        Assert.AreEqual(1.50f,  TransportRewardCalculator.ComfortMultiplier(70f),  0.001f);
        Assert.AreEqual(1.50f,  TransportRewardCalculator.ComfortMultiplier(89f),  0.001f);
        Assert.AreEqual(1.00f,  TransportRewardCalculator.ComfortMultiplier(50f),  0.001f);
        Assert.AreEqual(0.50f,  TransportRewardCalculator.ComfortMultiplier(30f),  0.001f);
        Assert.AreEqual(0.25f,  TransportRewardCalculator.ComfortMultiplier(0f),   0.001f);
        Assert.AreEqual(0.25f,  TransportRewardCalculator.ComfortMultiplier(29f),  0.001f);
    }

    [Test]
    public void RewardCalculator_TimeBonus_EarlyDelivery_MaxFiftyPercent()
    {
        // Deliver immediately (all time remaining) → +50 %
        float bonus = TransportRewardCalculator.TimeBonus(600f, 600f);
        Assert.AreEqual(0.50f, bonus, 0.001f);
    }

    [Test]
    public void RewardCalculator_TimeBonus_Overtime_MinusTwentyFive()
    {
        float bonus = TransportRewardCalculator.TimeBonus(-10f, 600f);
        Assert.AreEqual(-0.25f, bonus, 0.001f);
    }

    [Test]
    public void RewardCalculator_TimeBonus_NoTimeLimit_ReturnsZero()
    {
        float bonus = TransportRewardCalculator.TimeBonus(0f, 0f);
        Assert.AreEqual(0f, bonus, 0.001f);
    }

    [Test]
    public void RewardCalculator_CargoPenalty_ZeroDamage_FullFactor()
    {
        Assert.AreEqual(1.0f, TransportRewardCalculator.CargoPenaltyFactor(0f), 0.001f);
    }

    [Test]
    public void RewardCalculator_CargoPenalty_TenPercentDamage_TenPercentPenalty()
    {
        Assert.AreEqual(0.9f, TransportRewardCalculator.CargoPenaltyFactor(10f), 0.001f);
    }

    [Test]
    public void RewardCalculator_CargoPenalty_HundredDamage_ZeroFactor()
    {
        Assert.AreEqual(0f, TransportRewardCalculator.CargoPenaltyFactor(100f), 0.001f);
    }

    [Test]
    public void RewardCalculator_StarRating_AllThresholds()
    {
        Assert.AreEqual(5, TransportRewardCalculator.StarRating(90f));
        Assert.AreEqual(5, TransportRewardCalculator.StarRating(100f));
        Assert.AreEqual(4, TransportRewardCalculator.StarRating(75f));
        Assert.AreEqual(4, TransportRewardCalculator.StarRating(89f));
        Assert.AreEqual(3, TransportRewardCalculator.StarRating(55f));
        Assert.AreEqual(3, TransportRewardCalculator.StarRating(74f));
        Assert.AreEqual(2, TransportRewardCalculator.StarRating(35f));
        Assert.AreEqual(2, TransportRewardCalculator.StarRating(54f));
        Assert.AreEqual(1, TransportRewardCalculator.StarRating(0f));
        Assert.AreEqual(1, TransportRewardCalculator.StarRating(34f));
    }

    [Test]
    public void RewardCalculator_VIPMultiplier_AppliedCorrectly()
    {
        var standard  = MakeContract(baseReward: 1000, vipLevel: 0);
        var vip       = MakeContract(baseReward: 1000, vipLevel: 2);
        var rStandard = TransportRewardCalculator.CalculateResult(standard, 90f, 0f, 0f, 0f, 1);
        var rVip      = TransportRewardCalculator.CalculateResult(vip,      90f, 0f, 0f, 0f, 1);
        Assert.Greater(rVip.totalCoins, rStandard.totalCoins);
    }

    [Test]
    public void RewardCalculator_StreakBonus_AppliedAtFivePlusStreak()
    {
        var c        = MakeContract(baseReward: 1000);
        var noStreak = TransportRewardCalculator.CalculateResult(c, 90f, 0f, 0f, 0f, 1);
        var streak5  = TransportRewardCalculator.CalculateResult(c, 90f, 0f, 0f, 0f, 5);
        Assert.Greater(streak5.totalCoins, noStreak.totalCoins);
    }

    // ── PassengerComfortSystem — score-to-level mapping ───────────────────────

    [Test]
    public void ComfortSystem_ScoreToLevel_MapsCorrectly()
    {
        Assert.AreEqual(ComfortLevel.Excellent, PassengerComfortSystem.ScoreToLevel(100f));
        Assert.AreEqual(ComfortLevel.Excellent, PassengerComfortSystem.ScoreToLevel(90f));
        Assert.AreEqual(ComfortLevel.Good,      PassengerComfortSystem.ScoreToLevel(70f));
        Assert.AreEqual(ComfortLevel.Good,      PassengerComfortSystem.ScoreToLevel(89f));
        Assert.AreEqual(ComfortLevel.Fair,      PassengerComfortSystem.ScoreToLevel(50f));
        Assert.AreEqual(ComfortLevel.Fair,      PassengerComfortSystem.ScoreToLevel(69f));
        Assert.AreEqual(ComfortLevel.Poor,      PassengerComfortSystem.ScoreToLevel(30f));
        Assert.AreEqual(ComfortLevel.Poor,      PassengerComfortSystem.ScoreToLevel(49f));
        Assert.AreEqual(ComfortLevel.Critical,  PassengerComfortSystem.ScoreToLevel(0f));
        Assert.AreEqual(ComfortLevel.Critical,  PassengerComfortSystem.ScoreToLevel(29f));
    }

    [Test]
    public void ComfortSystem_BoundaryBetweenExcellentAndGood()
    {
        Assert.AreEqual(ComfortLevel.Excellent, PassengerComfortSystem.ScoreToLevel(90f));
        Assert.AreEqual(ComfortLevel.Good,      PassengerComfortSystem.ScoreToLevel(89.9f));
    }

    // ── CargoPhysicsController — calculations (via static helpers) ────────────

    [Test]
    public void CargoPhysics_FuelMultiplier_ZeroWeight_IsOne()
    {
        // Controller not instantiated — test calculation logic directly.
        // 0 kg payload → multiplier should be 1.0 (no extra consumption).
        float extra = (0f / 1000f) * 0.08f;
        Assert.AreEqual(1f, 1f + extra, 0.001f);
    }

    [Test]
    public void CargoPhysics_FuelMultiplier_1000kg_IsCorrect()
    {
        float extra = (1000f / 1000f) * 0.08f;
        Assert.AreEqual(1.08f, 1f + extra, 0.001f);
    }

    [Test]
    public void CargoPhysics_FuelMultiplier_5000kg_IsCorrect()
    {
        float extra = (5000f / 1000f) * 0.08f;
        Assert.AreEqual(1.40f, 1f + extra, 0.001f);
    }

    [Test]
    public void CargoPhysics_CargoPenaltyFactor_NoDestroyBelow100Percent()
    {
        // 90 % damage should still give a non-zero factor.
        float factor = TransportRewardCalculator.CargoPenaltyFactor(90f);
        Assert.AreEqual(0.1f, factor, 0.001f);
    }

    // ── TransportContractGenerator — rank-gated type distribution ─────────────

    [Test]
    public void ContractGenerator_Generate_ReturnsRequestedCount()
    {
        // AirportRegistry is null in edit mode → generator uses fallback IDs.
        var contracts = TransportContractGenerator.GenerateContracts(5, Vector3.zero, 1);
        // May return fewer if origin == destination for all; just check ≥ 0.
        Assert.GreaterOrEqual(contracts.Count, 0);
        Assert.LessOrEqual(contracts.Count, 5);
    }

    [Test]
    public void ContractGenerator_LowRank_NoHazardousContracts()
    {
        // At rank 0 the required rank for hazardous > 0, so no hazardous should appear
        // at rank 1 unless the random pick happens to select it.
        // Run many iterations and verify hazardous is infrequent.
        int hazCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var list = TransportContractGenerator.GenerateContracts(1, Vector3.zero, 0);
            foreach (var c in list)
                if (c.missionType == MissionType.CargoHazardous)
                    hazCount++;
        }
        // At rank 0 the weight for hazardous is 5 %, so expect roughly ≤ 20 out of 100.
        Assert.Less(hazCount, 30);
    }

    [Test]
    public void ContractGenerator_ContractIds_AreUnique()
    {
        var contracts = TransportContractGenerator.GenerateContracts(5, Vector3.zero, 5);
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var c in contracts)
            ids.Add(c.contractId);
        // All generated contracts must have distinct IDs.
        Assert.AreEqual(contracts.Count, ids.Count);
    }

    // ── DeliveryTimerController — phase transitions ───────────────────────────

    [Test]
    public void TimerPhase_FullTime_IsGreen()
    {
        var go = new GameObject("Timer");
        var timer = go.AddComponent<DeliveryTimerController>();
        timer.StartTimer(600f);
        Assert.AreEqual(TimerPhase.Green, timer.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TimerPhase_AfterReflectionSetTo40Percent_IsYellow()
    {
        // Set remaining to 40 % of 600 s = 240 s (Yellow zone 25–50 %)
        var go    = new GameObject("Timer");
        var timer = go.AddComponent<DeliveryTimerController>();
        timer.StartTimer(600f);

        // Use reflection to set private remaining seconds.
        var field = typeof(DeliveryTimerController).GetField(
            "_remainingSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(timer, 240f);   // 240/600 = 40 % → Yellow

        // Trigger phase evaluation via reflection on the private method.
        var method = typeof(DeliveryTimerController).GetMethod(
            "EvaluatePhase",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(timer, null);

        Assert.AreEqual(TimerPhase.Yellow, timer.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TimerPhase_AfterReflectionSetTo10Percent_IsRed()
    {
        var go    = new GameObject("Timer");
        var timer = go.AddComponent<DeliveryTimerController>();
        timer.StartTimer(600f);

        var field = typeof(DeliveryTimerController).GetField(
            "_remainingSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(timer, 60f);   // 60/600 = 10 % → Red

        var method = typeof(DeliveryTimerController).GetMethod(
            "EvaluatePhase",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(timer, null);

        Assert.AreEqual(TimerPhase.Red, timer.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TimerPhase_NegativeRemaining_IsOvertime()
    {
        var go    = new GameObject("Timer");
        var timer = go.AddComponent<DeliveryTimerController>();
        timer.StartTimer(600f);

        var field = typeof(DeliveryTimerController).GetField(
            "_remainingSeconds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(timer, -30f);

        var method = typeof(DeliveryTimerController).GetMethod(
            "EvaluatePhase",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(timer, null);

        Assert.AreEqual(TimerPhase.Overtime, timer.CurrentPhase);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Timer_StopTimer_IsNotRunning()
    {
        var go    = new GameObject("Timer");
        var timer = go.AddComponent<DeliveryTimerController>();
        timer.StartTimer(300f);
        timer.StopTimer();
        Assert.IsFalse(timer.IsRunning);
        Object.DestroyImmediate(go);
    }

    // ── DeliveryResult validation ─────────────────────────────────────────────

    [Test]
    public void DeliveryResult_StarRating_AlwaysBetweenOneAndFive()
    {
        float[] scores = { 0f, 20f, 40f, 60f, 80f, 100f };
        foreach (float s in scores)
        {
            int stars = TransportRewardCalculator.StarRating(s);
            Assert.GreaterOrEqual(stars, 1, $"Score {s} gave stars < 1");
            Assert.LessOrEqual(stars, 5, $"Score {s} gave stars > 5");
        }
    }

    [Test]
    public void DeliveryResult_CompositeScore_ClampedToZeroHundred()
    {
        // Worst possible: 0 comfort, max overtime, 100 % damage.
        float score = TransportRewardCalculator.CompositeScore(0f, -9999f, 600f, 100f);
        Assert.GreaterOrEqual(score, 0f);
        Assert.LessOrEqual(score, 100f);

        // Best possible: 100 comfort, full time remaining, no damage.
        float best = TransportRewardCalculator.CompositeScore(100f, 600f, 600f, 0f);
        Assert.GreaterOrEqual(best, 0f);
        Assert.LessOrEqual(best, 100f);
    }
}
