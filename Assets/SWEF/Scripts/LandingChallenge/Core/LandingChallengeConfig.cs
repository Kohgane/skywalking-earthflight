// LandingChallengeConfig.cs — Phase 120: Precision Landing Challenge System
// ScriptableObject configuration for the Precision Landing Challenge system.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — ScriptableObject that holds all tunable parameters for the
    /// Precision Landing Challenge System.  Create one instance via
    /// Assets → Create → SWEF → LandingChallenge → Landing Challenge Config.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/LandingChallenge/Landing Challenge Config",
                     fileName = "LandingChallengeConfig")]
    public class LandingChallengeConfig : ScriptableObject
    {
        // ── Scoring Weights ───────────────────────────────────────────────────────

        [Header("Scoring Weights (must sum to 1)")]
        [Tooltip("Weight for centreline accuracy in total score.")]
        [Range(0f, 1f)] public float centrelineWeight = 0.25f;

        [Tooltip("Weight for touchdown zone accuracy in total score.")]
        [Range(0f, 1f)] public float touchdownZoneWeight = 0.20f;

        [Tooltip("Weight for glideslope adherence in total score.")]
        [Range(0f, 1f)] public float glideSlopeWeight = 0.20f;

        [Tooltip("Weight for approach speed control in total score.")]
        [Range(0f, 1f)] public float speedControlWeight = 0.15f;

        [Tooltip("Weight for sink rate at touchdown in total score.")]
        [Range(0f, 1f)] public float sinkRateWeight = 0.10f;

        [Tooltip("Weight for overall smoothness of the approach in total score.")]
        [Range(0f, 1f)] public float smoothnessWeight = 0.10f;

        // ── Difficulty Multipliers ────────────────────────────────────────────────

        [Header("Difficulty Score Multipliers")]
        [Tooltip("Score multiplier applied for Beginner difficulty.")]
        [Range(0.5f, 2f)] public float beginnerMultiplier = 0.8f;

        [Tooltip("Score multiplier applied for Intermediate difficulty.")]
        [Range(0.5f, 2f)] public float intermediateMultiplier = 1.0f;

        [Tooltip("Score multiplier applied for Advanced difficulty.")]
        [Range(0.5f, 2f)] public float advancedMultiplier = 1.2f;

        [Tooltip("Score multiplier applied for Expert difficulty.")]
        [Range(0.5f, 2f)] public float expertMultiplier = 1.5f;

        [Tooltip("Score multiplier applied for Legendary difficulty.")]
        [Range(0.5f, 2f)] public float legendaryMultiplier = 2.0f;

        // ── Precision Thresholds ──────────────────────────────────────────────────

        [Header("Precision Thresholds")]
        [Tooltip("Maximum centreline deviation (metres) for a Perfect score.")]
        [Range(0.5f, 10f)] public float perfectCentrelineMetres = 1.5f;

        [Tooltip("Maximum touchdown point deviation (metres) for a Perfect score.")]
        [Range(5f, 50f)] public float perfectTouchdownMetres = 15f;

        [Tooltip("Maximum glideslope deviation (dots) for a Perfect score.")]
        [Range(0.1f, 1f)] public float perfectGlideSlopeDots = 0.25f;

        [Tooltip("Maximum speed deviation from Vref (knots) for a Perfect score.")]
        [Range(1f, 10f)] public float perfectSpeedKnots = 3f;

        [Tooltip("Maximum sink rate (FPM) for a Perfect landing (abs value).")]
        [Range(50f, 300f)] public float perfectSinkRateFPM = 150f;

        // ── Wind Tolerance ────────────────────────────────────────────────────────

        [Header("Wind Tolerance")]
        [Tooltip("Maximum demonstrated crosswind component (knots).")]
        [Range(10f, 40f)] public float maxCrosswindKnots = 25f;

        [Tooltip("Wind gust factor: multiplied by base wind speed to get gust speed.")]
        [Range(1f, 2f)] public float gustFactor = 1.4f;

        [Tooltip("Wind shear layer height AGL (feet) for short-final shear events.")]
        [Range(100f, 500f)] public float windShearHeightFeet = 300f;

        // ── Replay Settings ───────────────────────────────────────────────────────

        [Header("Replay")]
        [Tooltip("Duration of approach to capture in the replay buffer (seconds).")]
        [Range(30f, 120f)] public float replayBufferSeconds = 60f;

        [Tooltip("Frame capture rate for replay recording (frames per second).")]
        [Range(5f, 30f)] public float replayFrameRate = 10f;

        // ── Grading Thresholds ────────────────────────────────────────────────────

        [Header("Grade Thresholds (0–1000 score)")]
        [Tooltip("Minimum score for a Perfect grade.")]
        [Range(900f, 1000f)] public float perfectThreshold = 950f;

        [Tooltip("Minimum score for an Excellent grade.")]
        [Range(800f, 950f)] public float excellentThreshold = 850f;

        [Tooltip("Minimum score for a Good grade.")]
        [Range(650f, 850f)] public float goodThreshold = 700f;

        [Tooltip("Minimum score for a Fair grade.")]
        [Range(400f, 700f)] public float fairThreshold = 500f;

        [Tooltip("Minimum score for a Poor grade (below this = Crash).")]
        [Range(100f, 500f)] public float poorThreshold = 200f;

        // ── Penalty Configuration ─────────────────────────────────────────────────

        [Header("Penalties")]
        [Tooltip("Score deducted per bounce at touchdown.")]
        [Range(10f, 100f)] public float bouncePenalty = 50f;

        [Tooltip("Score deducted for performing a go-around.")]
        [Range(0f, 200f)] public float goAroundPenalty = 75f;

        [Tooltip("Score deducted for touching down before the threshold.")]
        [Range(50f, 500f)] public float shortLandingPenalty = 200f;

        // ── Bonus Configuration ───────────────────────────────────────────────────

        [Header("Bonuses")]
        [Tooltip("Bonus score for landing without autopilot (manual flight).")]
        [Range(0f, 100f)] public float manualFlightBonus = 50f;

        [Tooltip("Bonus score for completing a night challenge.")]
        [Range(0f, 100f)] public float nightBonus = 30f;

        [Tooltip("Bonus score for completing with no HUD aids.")]
        [Range(0f, 150f)] public float noHudBonus = 75f;

        [Tooltip("Weather severity bonus multiplier added to score.")]
        [Range(0f, 0.5f)] public float weatherBonusMultiplier = 0.15f;

        // ── Progression ───────────────────────────────────────────────────────────

        [Header("Progression")]
        [Tooltip("XP awarded per star earned in a challenge.")]
        [Range(10, 500)] public int xpPerStar = 100;

        [Tooltip("Currency awarded for completing a challenge with at least 1 star.")]
        [Range(10, 1000)] public int currencyOnCompletion = 200;

        [Tooltip("Bonus currency for a Perfect grade.")]
        [Range(0, 2000)] public int perfectGradeBonus = 500;

        // ── Daily Challenge ───────────────────────────────────────────────────────

        [Header("Daily Challenge")]
        [Tooltip("Daily challenge reset hour (UTC, 0–23).")]
        [Range(0, 23)] public int dailyResetHourUTC = 0;

        [Tooltip("Maximum daily challenge bonus XP multiplier.")]
        [Range(1f, 5f)] public float dailyBonusMultiplier = 2f;
    }
}
