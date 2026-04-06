// ApproachAnalyzer.cs — Phase 120: Precision Landing Challenge System
// Approach quality analysis: glideslope, localiser, speed stability, configuration timing.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Analyses approach quality from a sequence of
    /// <see cref="ApproachSnapshot"/> samples.  Computes average deviations,
    /// stability metrics, and configuration correctness.
    /// </summary>
    public class ApproachAnalyzer : MonoBehaviour
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Analyse a list of approach snapshots and return an
        /// <see cref="ApproachQualityReport"/>.
        /// </summary>
        public ApproachQualityReport Analyse(List<ApproachSnapshot> snapshots)
        {
            var report = new ApproachQualityReport();
            if (snapshots == null || snapshots.Count == 0) return report;

            float gsSum = 0f, locSum = 0f, speedDevSum = 0f;
            int gearDownCount = 0;
            float flapsCorrect = 0f;

            foreach (var s in snapshots)
            {
                gsSum       += Mathf.Abs(s.GlideSlopeDots);
                locSum      += Mathf.Abs(s.LocaliserDots);
                speedDevSum += Mathf.Abs(s.SpeedKnots - s.TargetSpeedKnots);
                if (s.GearDown) gearDownCount++;
                if (s.FlapSetting >= 2) flapsCorrect += 1f;
            }

            int count = snapshots.Count;
            report.AverageGlideSlopeDevDots  = gsSum       / count;
            report.AverageLocDevDots         = locSum      / count;
            report.AverageSpeedDeviationKnots = speedDevSum / count;
            report.GearDownFraction          = (float)gearDownCount / count;
            report.FlapsConfiguredFraction   = flapsCorrect / count;

            // Stability: low standard deviation of glideslope = stable
            float gsSq = 0f;
            foreach (var s in snapshots)
            {
                float d = Mathf.Abs(s.GlideSlopeDots) - report.AverageGlideSlopeDevDots;
                gsSq += d * d;
            }
            report.GlideSlopeStabilityDots = Mathf.Sqrt(gsSq / count);

            // Overall quality score (0–1)
            float gsScore    = Mathf.Clamp01(1f - report.AverageGlideSlopeDevDots  / 2f);
            float locScore   = Mathf.Clamp01(1f - report.AverageLocDevDots         / 2f);
            float spdScore   = Mathf.Clamp01(1f - report.AverageSpeedDeviationKnots / 15f);
            float cfgScore   = (report.GearDownFraction + report.FlapsConfiguredFraction) * 0.5f;
            report.OverallQuality = (gsScore * 0.35f + locScore * 0.25f + spdScore * 0.25f + cfgScore * 0.15f);

            return report;
        }
    }

    /// <summary>Summary of approach quality metrics.</summary>
    [System.Serializable]
    public class ApproachQualityReport
    {
        /// <summary>Mean absolute glideslope deviation across approach (dots).</summary>
        public float AverageGlideSlopeDevDots;

        /// <summary>Mean absolute localiser deviation across approach (dots).</summary>
        public float AverageLocDevDots;

        /// <summary>Mean absolute speed deviation from target (knots).</summary>
        public float AverageSpeedDeviationKnots;

        /// <summary>Fraction of snapshots with gear down.</summary>
        public float GearDownFraction;

        /// <summary>Fraction of snapshots with correct flap configuration.</summary>
        public float FlapsConfiguredFraction;

        /// <summary>Standard deviation of glideslope deviation (lower = more stable).</summary>
        public float GlideSlopeStabilityDots;

        /// <summary>Composite quality score (0–1).</summary>
        public float OverallQuality;
    }
}
