// CertificationManager.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// Manages the issuance and revocation of pilot certificates / digital badges.
    /// Works with an <see cref="AcademyProgressTracker"/> to persist awards.
    /// </summary>
    public class CertificationManager
    {
        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Raised after a new certificate has been issued.</summary>
        public event Action<CertificateData> OnCertificateIssued;

        // ── Dependencies ───────────────────────────────────────────────────────
        private readonly AcademyProgressTracker _tracker;

        // ── Constructor ────────────────────────────────────────────────────────
        public CertificationManager(AcademyProgressTracker tracker)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Issues a certificate for the given <paramref name="tier"/> based on a completed
        /// <paramref name="examResult"/>.
        /// </summary>
        /// <param name="pilotName">Display name of the pilot.</param>
        /// <param name="tier">License tier being awarded.</param>
        /// <param name="examResult">The passing exam result that triggered this award.</param>
        /// <param name="curriculumName">Human-readable name of the completed curriculum.</param>
        /// <returns>The newly issued <see cref="CertificateData"/>.</returns>
        public CertificateData IssueCertificate(
            string     pilotName,
            LicenseTier tier,
            ExamResult examResult,
            string     curriculumName)
        {
            if (examResult == null) throw new ArgumentNullException(nameof(examResult));
            if (!examResult.passed)
                throw new InvalidOperationException("Cannot issue a certificate for a failed exam.");

            string certName         = GetCertificateName(tier);
            string badgeResourcePath = GetBadgeResourcePath(tier);

            var cert = CertificateData.Create(
                certName,
                tier,
                pilotName,
                examResult.overallScore,
                curriculumName,
                badgeResourcePath);

            _tracker.AddCertificate(cert);
            OnCertificateIssued?.Invoke(cert);
            return cert;
        }

        /// <summary>Returns all certificates in the player's gallery.</summary>
        public IReadOnlyList<CertificateData> GetAllCertificates() =>
            _tracker.Data.certificates;

        /// <summary>Returns the highest license tier the player has earned.</summary>
        public LicenseTier GetHighestTier() => _tracker.HighestTier;

        /// <summary>
        /// Returns <c>true</c> when the player already holds a certificate at or above
        /// the requested <paramref name="tier"/>.
        /// </summary>
        public bool HasCertificate(LicenseTier tier) =>
            _tracker.HighestTier >= tier;

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string GetCertificateName(LicenseTier tier)
        {
            switch (tier)
            {
                case LicenseTier.StudentPilot:          return "Student Pilot Certificate";
                case LicenseTier.PrivatePilot:          return "Private Pilot License (PPL)";
                case LicenseTier.CommercialPilot:       return "Commercial Pilot License (CPL)";
                case LicenseTier.AirlineTransportPilot: return "Airline Transport Pilot (ATP)";
                default:                                return "Certificate";
            }
        }

        private static string GetBadgeResourcePath(LicenseTier tier)
        {
            switch (tier)
            {
                case LicenseTier.StudentPilot:          return "Academy/Badges/badge_student_pilot";
                case LicenseTier.PrivatePilot:          return "Academy/Badges/badge_private_pilot";
                case LicenseTier.CommercialPilot:       return "Academy/Badges/badge_commercial_pilot";
                case LicenseTier.AirlineTransportPilot: return "Academy/Badges/badge_atp";
                default:                                return "Academy/Badges/badge_default";
            }
        }
    }
}
