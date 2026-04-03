// ValidationResult.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;

namespace SWEF.Security
{
    /// <summary>
    /// Lightweight value type returned by all validation utilities.
    /// Carries a pass/fail flag, a list of human-readable violation messages,
    /// and an optional corrected value when the system can auto-repair the data.
    /// </summary>
    [Serializable]
    public struct ValidationResult
    {
        /// <summary><c>true</c> if no violations were found.</summary>
        public bool isValid;

        /// <summary>List of human-readable violation messages (empty when <see cref="isValid"/> is <c>true</c>).</summary>
        public List<string> violations;

        /// <summary>
        /// An optional corrected value produced by the validator.
        /// May be <c>null</c> if no automatic correction was possible.
        /// </summary>
        public object correctedValue;

        /// <summary>Creates a passing result with no violations.</summary>
        public static ValidationResult Valid() =>
            new ValidationResult { isValid = true, violations = new List<string>() };

        /// <summary>Creates a failing result with a single violation message.</summary>
        /// <param name="violation">Description of the violation.</param>
        /// <param name="correctedValue">Optional auto-corrected replacement value.</param>
        public static ValidationResult Invalid(string violation, object correctedValue = null)
        {
            return new ValidationResult
            {
                isValid        = false,
                violations     = new List<string> { violation },
                correctedValue = correctedValue
            };
        }

        /// <summary>Creates a failing result with multiple violation messages.</summary>
        /// <param name="violations">Descriptions of all violations.</param>
        /// <param name="correctedValue">Optional auto-corrected replacement value.</param>
        public static ValidationResult InvalidMultiple(List<string> violations, object correctedValue = null)
        {
            return new ValidationResult
            {
                isValid        = false,
                violations     = violations ?? new List<string>(),
                correctedValue = correctedValue
            };
        }

        /// <summary>Adds a violation to an existing result and marks it as invalid.</summary>
        /// <param name="violation">Additional violation message.</param>
        public void AddViolation(string violation)
        {
            if (violations == null) violations = new List<string>();
            violations.Add(violation);
            isValid = false;
        }
    }
}
