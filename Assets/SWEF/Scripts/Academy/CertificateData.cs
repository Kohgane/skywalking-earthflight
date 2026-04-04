// CertificateData.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>
    /// Immutable data record representing an awarded pilot certificate / digital badge.
    /// Serialised to the player's save file for the certificate gallery.
    /// </summary>
    [Serializable]
    public class CertificateData
    {
        /// <summary>Unique ID for this certificate instance (GUID string).</summary>
        public string certificateId;

        /// <summary>Display name of the certificate (e.g. "Private Pilot License").</summary>
        public string certificateName;

        /// <summary>The license tier this certificate represents.</summary>
        public LicenseTier tier;

        /// <summary>Name of the player / pilot who earned the certificate.</summary>
        public string pilotName;

        /// <summary>UTC timestamp when the certificate was awarded (ISO-8601 string).</summary>
        public string awardedDateUtc;

        /// <summary>Final overall exam score (0–100).</summary>
        public float examScore;

        /// <summary>Name of the curriculum / course this certificate belongs to.</summary>
        public string curriculumName;

        /// <summary>
        /// Resource path (or URL) to the badge artwork displayed in the gallery.
        /// Relative to a <c>Resources/Academy/Badges/</c> folder.
        /// </summary>
        public string badgeResourcePath;

        // ── Factories ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new <see cref="CertificateData"/> with a freshly generated ID and
        /// the current UTC time as the award date.
        /// </summary>
        public static CertificateData Create(
            string         certificateName,
            LicenseTier    tier,
            string         pilotName,
            float          examScore,
            string         curriculumName,
            string         badgeResourcePath = "")
        {
            return new CertificateData
            {
                certificateId     = Guid.NewGuid().ToString(),
                certificateName   = certificateName,
                tier              = tier,
                pilotName         = pilotName,
                awardedDateUtc    = DateTime.UtcNow.ToString("o"),
                examScore         = examScore,
                curriculumName    = curriculumName,
                badgeResourcePath = badgeResourcePath
            };
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Returns a human-readable summary of the certificate.</summary>
        public override string ToString() =>
            $"[Certificate] {certificateName} | Tier: {tier} | Score: {examScore:F1} | Pilot: {pilotName} | Awarded: {awardedDateUtc}";
    }
}
