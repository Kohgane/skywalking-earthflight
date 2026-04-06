// LandingCertificationSystem.cs — Phase 120: Precision Landing Challenge System
// Certification: pass series of landing tests for pilot certification levels.
// Namespace: SWEF.LandingChallenge

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Manages landing pilot certification levels.
    /// Players must pass a series of graded landing tests to earn each
    /// certification tier.  Ties into the Phase 104 Flight Academy.
    /// </summary>
    public class LandingCertificationSystem : MonoBehaviour
    {
        // ── Certification Level ───────────────────────────────────────────────

        public enum CertificationLevel
        {
            None,
            BasicLanding,
            CrosswindRated,
            InstrumentApproach,
            CarrierQualified,
            MasterPilot
        }

        // ── Certification Record ──────────────────────────────────────────────

        [Serializable]
        public class CertificationRecord
        {
            public CertificationLevel Level;
            public DateTime           IssuedDate;
            public string[]           RequiredChallengeIds;
            public float              MinimumScoreRequired;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Certification Definitions")]
        [SerializeField] private float basicPassScore       = 600f;
        [SerializeField] private float crosswindPassScore   = 700f;
        [SerializeField] private float instrumentPassScore  = 750f;
        [SerializeField] private float carrierPassScore     = 800f;
        [SerializeField] private float masterPassScore      = 900f;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly Dictionary<CertificationLevel, CertificationRecord> _earned =
            new Dictionary<CertificationLevel, CertificationRecord>();

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a new certification is awarded.</summary>
        public event Action<CertificationLevel, CertificationRecord> OnCertificationEarned;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if the player holds the specified certification.</summary>
        public bool HasCertification(CertificationLevel level) => _earned.ContainsKey(level);

        /// <summary>Returns the highest certification level currently earned.</summary>
        public CertificationLevel HighestLevel()
        {
            CertificationLevel highest = CertificationLevel.None;
            foreach (var k in _earned.Keys)
                if (k > highest) highest = k;
            return highest;
        }

        /// <summary>
        /// Evaluate whether challenge results justify awarding a certification level.
        /// Call after recording scores for all required challenges.
        /// </summary>
        public void EvaluateCertification(CertificationLevel level,
                                          Dictionary<string, float> scoresByChallengeId)
        {
            if (_earned.ContainsKey(level)) return; // already earned

            var required = GetRequirements(level);
            float passScore = GetPassScore(level);

            foreach (var reqId in required)
            {
                if (!scoresByChallengeId.TryGetValue(reqId, out float score) || score < passScore)
                    return; // prerequisite not met
            }

            var record = new CertificationRecord
            {
                Level                = level,
                IssuedDate           = DateTime.UtcNow,
                RequiredChallengeIds = required,
                MinimumScoreRequired = passScore
            };
            _earned[level] = record;
            OnCertificationEarned?.Invoke(level, record);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private string[] GetRequirements(CertificationLevel level)
        {
            switch (level)
            {
                case CertificationLevel.BasicLanding:     return new[] { "std_jfk_22r" };
                case CertificationLevel.CrosswindRated:   return new[] { "std_jfk_22r", "xwind_gibraltar" };
                case CertificationLevel.InstrumentApproach: return new[] { "std_jfk_22r", "mountain_lukla" };
                case CertificationLevel.CarrierQualified: return new[] { "carrier_cvn68" };
                case CertificationLevel.MasterPilot:     return new[] { "std_jfk_22r", "carrier_cvn68", "mountain_lukla", "xwind_gibraltar" };
                default:                                  return Array.Empty<string>();
            }
        }

        private float GetPassScore(CertificationLevel level)
        {
            switch (level)
            {
                case CertificationLevel.BasicLanding:      return basicPassScore;
                case CertificationLevel.CrosswindRated:    return crosswindPassScore;
                case CertificationLevel.InstrumentApproach: return instrumentPassScore;
                case CertificationLevel.CarrierQualified:  return carrierPassScore;
                case CertificationLevel.MasterPilot:       return masterPassScore;
                default:                                   return 1000f;
            }
        }
    }
}
