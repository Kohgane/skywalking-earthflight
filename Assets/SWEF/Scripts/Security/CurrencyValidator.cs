// CurrencyValidator.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Serializable record of a single currency transaction.
    /// </summary>
    [Serializable]
    public class CurrencyTransaction
    {
        /// <summary>Signed amount (positive = gain, negative = spend).</summary>
        public float amount;
        /// <summary>Human-readable source label (e.g. "mission_reward", "purchase").</summary>
        public string source;
        /// <summary>UTC timestamp when the transaction occurred.</summary>
        public string timestamp;
    }

    /// <summary>
    /// Static utility that maintains a transaction log for the player's in-game
    /// currency and validates every change against it.
    ///
    /// <para>Call <see cref="RecordTransaction"/> every time currency legitimately
    /// changes. Call <see cref="ValidateTransaction"/> before applying an
    /// unverified delta. Use <see cref="GetExpectedBalance"/> vs
    /// <see cref="GetActualBalance"/> to spot discrepancies.</para>
    /// </summary>
    public static class CurrencyValidator
    {
        // ── Private state ─────────────────────────────────────────────────────

        private static readonly List<CurrencyTransaction> _log =
            new List<CurrencyTransaction>();

        private static float _expectedBalance;
        private static float _actualBalance;

        private static SecurityConfig _config = SecurityConfig.Default();

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>Updates the active <see cref="SecurityConfig"/> used for threshold checks.</summary>
        /// <param name="config">New configuration.</param>
        public static void SetConfig(SecurityConfig config)
        {
            _config = config ?? SecurityConfig.Default();
        }

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the validator with the player's current persisted balance.
        /// Call once at game start after loading the save file.
        /// </summary>
        /// <param name="currentBalance">The balance read from the save file.</param>
        public static void Initialise(float currentBalance)
        {
            _expectedBalance = currentBalance;
            _actualBalance   = currentBalance;
            _log.Clear();
        }

        // ── Transaction API ───────────────────────────────────────────────────

        /// <summary>
        /// Records a legitimate currency change in the transaction log.
        /// Call this every time the game awards or deducts currency.
        /// </summary>
        /// <param name="amount">Signed delta (positive = gain).</param>
        /// <param name="source">Source label for the transaction.</param>
        public static void RecordTransaction(float amount, string source)
        {
            _expectedBalance += amount;
            _log.Add(new CurrencyTransaction
            {
                amount    = amount,
                source    = source ?? "unknown",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        /// <summary>
        /// Validates a proposed currency delta before it is applied.
        /// Returns <see cref="ValidationResult.Invalid"/> when the gain rate is
        /// too high or the amount is unreasonably large.
        /// </summary>
        /// <param name="amount">Proposed delta.</param>
        /// <param name="source">Source label.</param>
        /// <returns>Validation result.</returns>
        public static ValidationResult ValidateTransaction(float amount, string source)
        {
            if (amount <= 0f) return ValidationResult.Valid(); // spending is always OK

            // Check single-transaction gain cap (1 minute worth of max gain)
            float maxSingleGain = _config.maxCurrencyGainPerMinute;
            if (amount > maxSingleGain)
                return ValidationResult.Invalid(
                    $"Currency gain {amount} exceeds single-transaction cap {maxSingleGain} (source: {source}).",
                    maxSingleGain);

            return ValidationResult.Valid();
        }

        // ── Balance comparison ────────────────────────────────────────────────

        /// <summary>Returns the balance computed from the transaction log.</summary>
        public static float GetExpectedBalance() => _expectedBalance;

        /// <summary>
        /// Updates the known actual balance (e.g. from ProgressionManager) and
        /// returns whether it matches the expected balance within a tolerance.
        /// </summary>
        /// <param name="actualBalance">Balance reported by the game's economy system.</param>
        /// <returns><c>true</c> if balances match within floating-point tolerance.</returns>
        public static bool SetAndVerifyActualBalance(float actualBalance)
        {
            _actualBalance = actualBalance;
            return Mathf.Approximately(_actualBalance, _expectedBalance);
        }

        /// <summary>
        /// Compares expected vs actual balance and returns a <see cref="ValidationResult"/>.
        /// When balances diverge the correctedValue is set to <see cref="GetExpectedBalance"/>.
        /// </summary>
        public static ValidationResult VerifyBalance()
        {
            if (Mathf.Approximately(_actualBalance, _expectedBalance))
                return ValidationResult.Valid();

            float diff = _actualBalance - _expectedBalance;
            return ValidationResult.Invalid(
                $"Balance mismatch: actual={_actualBalance:F0} expected={_expectedBalance:F0} diff={diff:F0}",
                _expectedBalance);
        }

        /// <summary>Returns a read-only snapshot of the transaction log.</summary>
        public static IReadOnlyList<CurrencyTransaction> GetTransactionLog() =>
            _log.AsReadOnly();

        /// <summary>
        /// Reverts the actual balance to the expected (transaction-log) value.
        /// Call this after detecting and reporting a mismatch.
        /// </summary>
        /// <returns>The corrected balance value.</returns>
        public static float Revert()
        {
            _actualBalance = _expectedBalance;
            return _expectedBalance;
        }
    }
}
