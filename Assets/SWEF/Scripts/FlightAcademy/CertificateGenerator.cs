using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Static utility for creating, verifying, and formatting pilot certificates.
    /// </summary>
    public static class CertificateGenerator
    {
        // ── Certificate creation ──────────────────────────────────────────────────

        /// <summary>
        /// Generates a new <see cref="Certificate"/> for the given license grade.
        /// Computes a SHA-256 signature over the canonical certificate fields.
        /// </summary>
        /// <param name="grade">License grade being awarded.</param>
        /// <param name="playerName">Display name of the pilot.</param>
        /// <param name="examScores">Module ID → score dictionary for modules in this grade.</param>
        /// <param name="totalFlightHours">Cumulative flight hours at time of issue.</param>
        /// <returns>A fully populated and signed <see cref="Certificate"/>.</returns>
        public static Certificate GenerateCertificate(LicenseGrade grade,
                                                       string playerName,
                                                       Dictionary<string, float> examScores,
                                                       float totalFlightHours)
        {
            string issueDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string certId    = Guid.NewGuid().ToString("N");

            var cert = new Certificate
            {
                certificateId   = certId,
                licenseGrade    = grade,
                playerName      = playerName ?? string.Empty,
                issueDate       = issueDate,
                examScores      = examScores ?? new Dictionary<string, float>(),
                totalFlightHours = totalFlightHours
            };

            cert.signatureHash = ComputeSignature(cert);
            return cert;
        }

        // ── Verification ──────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that <paramref name="certificate"/>'s signature hash matches
        /// a freshly computed hash of its canonical fields.
        /// </summary>
        /// <returns>True if the certificate is unmodified and authentic.</returns>
        public static bool VerifyCertificate(Certificate certificate)
        {
            if (certificate == null) return false;
            string expected = ComputeSignature(certificate);
            return string.Equals(expected, certificate.signatureHash,
                                 StringComparison.Ordinal);
        }

        // ── Display formatting ────────────────────────────────────────────────────

        /// <summary>
        /// Produces a localised multi-line text representation of the certificate
        /// suitable for display in the certificate gallery.
        /// </summary>
        public static string FormatCertificateText(Certificate certificate)
        {
            if (certificate == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("=== CERTIFICATE OF ACHIEVEMENT ===");
            sb.AppendLine($"Pilot:     {certificate.playerName}");
            sb.AppendLine($"License:   {LicenseGradeToString(certificate.licenseGrade)}");
            sb.AppendLine($"Issued:    {certificate.issueDate}");
            sb.AppendLine($"Cert ID:   {certificate.certificateId}");
            sb.AppendLine($"Hours:     {certificate.totalFlightHours:F1} h");
            if (certificate.examScores != null && certificate.examScores.Count > 0)
            {
                sb.AppendLine("--- Exam Scores ---");
                foreach (var kvp in certificate.examScores)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value:F1}");
            }
            sb.AppendLine($"Signature: {certificate.signatureHash?.Substring(0, 16)}…");
            return sb.ToString();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Computes a deterministic SHA-256 hash over the canonical certificate fields
        /// (id, grade, playerName, issueDate, totalFlightHours).
        /// Exam scores are intentionally excluded so that corrections do not
        /// invalidate certificates.
        /// </summary>
        private static string ComputeSignature(Certificate cert)
        {
            string canonical = $"{cert.certificateId}|{(int)cert.licenseGrade}|{cert.playerName}|{cert.issueDate}|{cert.totalFlightHours:F2}";
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string LicenseGradeToString(LicenseGrade grade)
        {
            switch (grade)
            {
                case LicenseGrade.StudentPilot:    return "Student Pilot";
                case LicenseGrade.PPL:             return "Private Pilot License (PPL)";
                case LicenseGrade.CPL:             return "Commercial Pilot License (CPL)";
                case LicenseGrade.ATPL:            return "Airline Transport Pilot License (ATPL)";
                case LicenseGrade.InstructorRating:return "Instructor Rating";
                case LicenseGrade.TestPilot:       return "Test Pilot";
                default:                           return grade.ToString();
            }
        }
    }
}
