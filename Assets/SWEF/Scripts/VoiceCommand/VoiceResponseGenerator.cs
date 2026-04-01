// VoiceResponseGenerator.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Static utility that generates localised text responses from a
    /// <see cref="VoiceCommandResult"/> using template-based parameter substitution.
    /// </summary>
    public static class VoiceResponseGenerator
    {
        // ── Built-in response templates ───────────────────────────────────────────
        // Keys map to localisation keys; values are English fallback strings used
        // when the localisation system is unavailable.

        private static readonly Dictionary<string, string> _fallbackTemplates =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "voice_response_acknowledged",       "Acknowledged." },
                { "voice_response_weather_report",     "Weather report: {weather}." },
                { "voice_response_altitude_set",       "Altitude set to {altitude} feet." },
                { "voice_response_heading_set",        "Heading set to {degrees} degrees." },
                { "voice_response_waypoint_set",       "Waypoint set to {name}." },
                { "voice_response_autopilot_on",       "Autopilot engaged." },
                { "voice_response_autopilot_off",      "Autopilot disengaged." },
                { "voice_response_confirm_prompt",     "Confirm {command}? Say Confirm or Cancel." },
                { "voice_response_cancelled",          "Command cancelled." },
                { "voice_error_no_match",              "Command not recognised. Please try again." },
                { "voice_error_cooldown",              "Please wait before repeating that command." },
                { "voice_error_category_disabled",     "That command category is currently disabled." },
                { "voice_error_no_handler",            "Command handler not available." },
            };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a short response string for the HUD toast, with parameters substituted.
        /// Falls back to English when no localisation manager is present.
        /// </summary>
        public static string GetShortResponse(
            VoiceCommandResult result,
            Dictionary<string, string> parameters = null)
        {
            string locKey  = string.IsNullOrEmpty(result.responseLocKey)
                ? (result.success ? "voice_response_acknowledged" : "voice_error_no_match")
                : result.responseLocKey;

            string template = Localise(locKey);
            return SubstituteParameters(template, parameters);
        }

        /// <summary>
        /// Returns a detailed response string for the history log view.
        /// Includes the command id and timestamp in addition to the short response.
        /// </summary>
        public static string GetDetailedResponse(
            VoiceCommandResult result,
            Dictionary<string, string> parameters = null)
        {
            string shortText = GetShortResponse(result, parameters);
            string cmdId     = result.executedCommand?.commandId ?? "unknown";
            string ts        = result.timestamp.ToString("HH:mm:ss");

            return string.IsNullOrEmpty(result.detailMessage)
                ? $"[{ts}] {cmdId}: {shortText}"
                : $"[{ts}] {cmdId}: {shortText} — {result.detailMessage}";
        }

        /// <summary>
        /// Builds the confirmation prompt string for a critical command.
        /// </summary>
        public static string GetConfirmationPrompt(VoiceCommandDefinition command)
        {
            if (command == null) return Localise("voice_response_confirm_prompt");

            var p = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "command", command.primaryPhrase }
            };

            return SubstituteParameters(Localise("voice_response_confirm_prompt"), p);
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Looks up <paramref name="key"/> in the localisation system.
        /// Falls back to the built-in English template when unavailable.
        /// </summary>
        private static string Localise(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            _fallbackTemplates.TryGetValue(key, out string fallback);
            return fallback ?? key;
        }

        /// <summary>
        /// Replaces <c>{paramName}</c> tokens in <paramref name="template"/> with
        /// the corresponding values from <paramref name="parameters"/>.
        /// Unmatched tokens are left as-is.
        /// </summary>
        internal static string SubstituteParameters(
            string template,
            Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            if (parameters == null || parameters.Count == 0) return template;

            return Regex.Replace(template, @"\{(\w+)\}", match =>
            {
                string tokenName = match.Groups[1].Value;
                return parameters.TryGetValue(tokenName, out string val) ? val : match.Value;
            });
        }
    }
}
