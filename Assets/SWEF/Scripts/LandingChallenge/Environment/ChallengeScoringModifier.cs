// ChallengeScoringModifier.cs — Phase 120: Precision Landing Challenge System
// Dynamic modifiers: weather bonus, night bonus, manual flight bonus, no-HUD bonus.
// Namespace: SWEF.LandingChallenge

using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Applies dynamic scoring modifiers to a landing result.
    /// Bonuses are applied for adverse conditions, manual flight, and HUD-off mode.
    /// </summary>
    public class ChallengeScoringModifier : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Active Modifiers")]
        [SerializeField] private bool manualFlightActive;
        [SerializeField] private bool nightModeActive;
        [SerializeField] private bool noHudActive;
        [SerializeField] private bool noAutopilotActive;

        [Header("Weather")]
        [SerializeField] [Range(0f, 1f)] private float weatherSeverity;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether manual (no-autopilot) modifier is active.</summary>
        public bool ManualFlightActive  { get => manualFlightActive;  set => manualFlightActive = value; }

        /// <summary>Whether night-mode modifier is active.</summary>
        public bool NightModeActive     { get => nightModeActive;     set => nightModeActive    = value; }

        /// <summary>Whether no-HUD modifier is active.</summary>
        public bool NoHudActive         { get => noHudActive;         set => noHudActive        = value; }

        /// <summary>Weather severity driving the weather bonus (0–1).</summary>
        public float WeatherSeverity    { get => weatherSeverity;     set => weatherSeverity    = Mathf.Clamp01(value); }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Apply all active modifiers to a base score and return the final score.
        /// </summary>
        public float Apply(float baseScore, LandingChallengeConfig cfg)
        {
            if (cfg == null) return baseScore;
            return LandingGradeCalculator.ApplyBonuses(
                baseScore,
                manualFlightActive,
                nightModeActive,
                noHudActive,
                weatherSeverity,
                cfg);
        }

        /// <summary>Returns a readable summary of active modifiers.</summary>
        public string GetModifierSummary()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (manualFlightActive) parts.Add("Manual Flight");
            if (nightModeActive)    parts.Add("Night");
            if (noHudActive)        parts.Add("No HUD");
            if (noAutopilotActive)  parts.Add("No Autopilot");
            if (weatherSeverity > 0f) parts.Add($"Weather x{weatherSeverity:P0}");
            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }
    }
}
