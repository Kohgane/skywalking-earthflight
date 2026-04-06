// LandingScoringEngine.cs — Phase 120: Precision Landing Challenge System
// Multi-factor scoring: centreline deviation, touchdown point, glideslope, speed, sink rate, smoothness.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Computes a multi-factor landing score from touchdown and approach data.
    /// Applies configured weights, difficulty multipliers, and scoring modifiers.
    /// </summary>
    public class LandingScoringEngine : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Score a landing attempt and return a complete <see cref="LandingResult"/>.
        /// </summary>
        public LandingResult Score(TouchdownData td,
                                   List<ApproachSnapshot> approach,
                                   ChallengeDefinition challenge,
                                   LandingChallengeConfig cfg)
        {
            var result = new LandingResult { Timestamp = System.DateTime.UtcNow };

            float centreScore  = ScoreCentreline(td, cfg);
            float zoneScore    = ScoreTouchdownZone(td, challenge, cfg);
            float gsScore      = ScoreGlideslope(approach, cfg);
            float speedScore   = ScoreSpeed(approach, cfg);
            float sinkScore    = ScoreSinkRate(td, cfg);
            float smoothScore  = ScoreSmoothness(td, cfg);

            result.CategoryScores[ScoringCategory.CenterlineAccuracy]   = centreScore;
            result.CategoryScores[ScoringCategory.TouchdownZone]        = zoneScore;
            result.CategoryScores[ScoringCategory.GlideSlopeAdherence]  = gsScore;
            result.CategoryScores[ScoringCategory.SpeedControl]         = speedScore;
            result.CategoryScores[ScoringCategory.SinkRate]             = sinkScore;
            result.CategoryScores[ScoringCategory.Smoothness]           = smoothScore;

            float raw = centreScore  * cfg.centrelineWeight
                      + zoneScore    * cfg.touchdownZoneWeight
                      + gsScore      * cfg.glideSlopeWeight
                      + speedScore   * cfg.speedControlWeight
                      + sinkScore    * cfg.sinkRateWeight
                      + smoothScore  * cfg.smoothnessWeight;

            raw *= 1000f; // convert 0–1 → 0–1000
            raw *= GetDifficultyMultiplier(challenge.Difficulty, cfg);
            // Apply bounce deductions
            if (cfg != null) raw -= td.BounceCount * cfg.bouncePenalty;

            result.TotalScore                = Mathf.Clamp(raw, 0f, 1000f);
            result.CenterlineDeviationMetres = td.CentrelineOffsetMetres;
            result.SinkRateFPM               = td.VerticalSpeedFPM;
            result.TouchdownSpeedKnots       = td.SpeedKnots;
            result.InTouchdownZone           = zoneScore >= 0.8f;
            result.Grade                     = LandingGradeCalculator.ComputeGrade(result.TotalScore, cfg);
            result.Stars                     = ComputeStars(result.TotalScore, challenge.StarThresholds);

            return result;
        }

        // ── Private scoring factors ───────────────────────────────────────────

        private float ScoreCentreline(TouchdownData td, LandingChallengeConfig cfg)
        {
            float dev = Mathf.Abs(td.CentrelineOffsetMetres);
            if (dev <= cfg.perfectCentrelineMetres) return 1f;
            return Mathf.Clamp01(1f - (dev - cfg.perfectCentrelineMetres) / 20f);
        }

        private float ScoreTouchdownZone(TouchdownData td, ChallengeDefinition challenge, LandingChallengeConfig cfg)
        {
            float dist = td.ThresholdDistanceMetres;
            if (dist < 0f)          return 0f; // short landing
            if (dist <= cfg.perfectTouchdownMetres) return 1f;
            return Mathf.Clamp01(1f - (dist - cfg.perfectTouchdownMetres) / 500f);
        }

        private float ScoreGlideslope(List<ApproachSnapshot> approach, LandingChallengeConfig cfg)
        {
            if (approach == null || approach.Count == 0) return 0.5f;
            float sum = 0f;
            foreach (var s in approach)
            {
                float dev = Mathf.Abs(s.GlideSlopeDots);
                float score = dev <= cfg.perfectGlideSlopeDots ? 1f :
                              Mathf.Clamp01(1f - (dev - cfg.perfectGlideSlopeDots) / 2f);
                sum += score;
            }
            return sum / approach.Count;
        }

        private float ScoreSpeed(List<ApproachSnapshot> approach, LandingChallengeConfig cfg)
        {
            if (approach == null || approach.Count == 0) return 0.5f;
            float sum = 0f;
            foreach (var s in approach)
            {
                float dev = Mathf.Abs(s.SpeedKnots - s.TargetSpeedKnots);
                float score = dev <= cfg.perfectSpeedKnots ? 1f :
                              Mathf.Clamp01(1f - (dev - cfg.perfectSpeedKnots) / 20f);
                sum += score;
            }
            return sum / approach.Count;
        }

        private float ScoreSinkRate(TouchdownData td, LandingChallengeConfig cfg)
        {
            float rate = Mathf.Abs(td.VerticalSpeedFPM);
            if (rate <= cfg.perfectSinkRateFPM) return 1f;
            if (rate >= 800f) return 0f;
            return Mathf.Clamp01(1f - (rate - cfg.perfectSinkRateFPM) / (800f - cfg.perfectSinkRateFPM));
        }

        private float ScoreSmoothness(TouchdownData td, LandingChallengeConfig cfg)
        {
            float bank  = Mathf.Abs(td.BankAngleDeg);
            float crab  = Mathf.Abs(td.CrabAngleDeg);
            float gForce = Mathf.Abs(td.GForce - 1f); // deviation from 1G
            float bankScore  = Mathf.Clamp01(1f - bank  / 10f);
            float crabScore  = Mathf.Clamp01(1f - crab  / 15f);
            float gScore     = Mathf.Clamp01(1f - gForce / 2f);
            return (bankScore + crabScore + gScore) / 3f;
        }

        private float GetDifficultyMultiplier(DifficultyLevel level, LandingChallengeConfig cfg)
        {
            switch (level)
            {
                case DifficultyLevel.Beginner:     return cfg.beginnerMultiplier;
                case DifficultyLevel.Intermediate: return cfg.intermediateMultiplier;
                case DifficultyLevel.Advanced:     return cfg.advancedMultiplier;
                case DifficultyLevel.Expert:       return cfg.expertMultiplier;
                case DifficultyLevel.Legendary:    return cfg.legendaryMultiplier;
                default:                           return 1f;
            }
        }

        private int ComputeStars(float score, float[] thresholds)
        {
            if (thresholds == null || thresholds.Length < 3) return 0;
            if (score >= thresholds[2]) return 3;
            if (score >= thresholds[1]) return 2;
            if (score >= thresholds[0]) return 1;
            return 0;
        }
    }
}
