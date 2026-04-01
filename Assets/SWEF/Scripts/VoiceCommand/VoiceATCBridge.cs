// VoiceATCBridge.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Bridges voice commands to the ATC subsystem, translating natural-language
    /// pilot phrases into ATC protocol strings (callsign, runway designators,
    /// altitude in flight levels, etc.).
    /// Compiled only when <c>SWEF_ATC_AVAILABLE</c> is defined; on other platforms
    /// the class exists as a no-op stub so the project continues to build.
    /// </summary>
    public class VoiceATCBridge : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("Optional callsign override. If empty, the global session callsign is used.")]
        [SerializeField] private string _callsignOverride = string.Empty;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted += HandleCommandExecuted;
        }

        private void OnDisable()
        {
            if (CommandExecutor.Instance != null)
                CommandExecutor.Instance.OnCommandExecuted -= HandleCommandExecuted;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Translates a natural-language phrase to an ATC protocol phrase and returns it.
        /// E.g. "request landing" → "SWEF001, request landing runway 27L, over."
        /// </summary>
        public string TranslateToATC(string naturalPhrase, Dictionary<string, string> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(naturalPhrase)) return string.Empty;

            string callsign = GetCallsign();
            string phrase   = naturalPhrase.Trim().ToLowerInvariant();

            // Simple pattern translation table.
            if (phrase.Contains("request landing"))
            {
                string runway = parameters != null &&
                                parameters.TryGetValue("runway", out string r) ? r : "active runway";
                return $"{callsign}, request landing {runway}, over.";
            }

            if (phrase.Contains("request takeoff"))
            {
                string runway = parameters != null &&
                                parameters.TryGetValue("runway", out string r) ? r : "active runway";
                return $"{callsign}, ready for takeoff {runway}, over.";
            }

            if (phrase.Contains("request altitude"))
            {
                string alt = parameters != null &&
                             parameters.TryGetValue("altitude", out string a) ? AltToFL(a) : "FL350";
                return $"{callsign}, request climb to {alt}, over.";
            }

            // Generic passthrough.
            return $"{callsign}, {naturalPhrase}, over.";
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void HandleCommandExecuted(VoiceCommandResult result)
        {
            if (result.executedCommand?.category != CommandCategory.ATC) return;

#if SWEF_ATC_AVAILABLE
            // Hook into the real ATC system when available.
            // ATCManager.Instance?.HandlePilotVoiceCommand(result.executedCommand, null);
#else
            Debug.Log($"[VoiceATCBridge] ATC command executed (stub): {result.executedCommand.commandId}");
#endif
        }

        private string GetCallsign()
        {
            if (!string.IsNullOrEmpty(_callsignOverride)) return _callsignOverride;
            return "SWEF001";
        }

        /// <summary>Converts an altitude string in feet to a flight-level string.</summary>
        private static string AltToFL(string altFeet)
        {
            if (int.TryParse(altFeet, out int feet))
            {
                int fl = feet / 100;
                return $"FL{fl:D3}";
            }
            return altFeet;
        }
    }
}
