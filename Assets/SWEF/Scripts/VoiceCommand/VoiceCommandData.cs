// VoiceCommandData.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>Category of a voice command for filtering and enabling/disabling groups.</summary>
    public enum CommandCategory
    {
        Flight,
        Navigation,
        Instruments,
        Weather,
        Music,
        Camera,
        System,
        ATC,
        Emergency
    }

    /// <summary>Priority level used to resolve conflicts and confirmation requirements.</summary>
    public enum CommandPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    // ── Classes ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single recognisable voice command with its aliases, category,
    /// confirmation requirements, and optional parameter hints.
    /// </summary>
    [Serializable]
    public class VoiceCommandDefinition
    {
        [Tooltip("Unique identifier for this command (e.g. 'cmd_increase_throttle').")]
        public string commandId = string.Empty;

        [Tooltip("Primary phrase that triggers this command.")]
        public string primaryPhrase = string.Empty;

        [Tooltip("Alternative phrases that map to the same command.")]
        public string[] aliases = Array.Empty<string>();

        [Tooltip("Category used for filtering and batch enable/disable.")]
        public CommandCategory category = CommandCategory.System;

        [Tooltip("Priority that determines resolution order and confirmation requirements.")]
        public CommandPriority priority = CommandPriority.Normal;

        [Tooltip("When true, the executor requests a voice/touch confirmation before acting.")]
        public bool requiresConfirmation = false;

        [Tooltip("Localization key for the command description shown in the reference panel.")]
        public string descriptionLocKey = string.Empty;

        [Tooltip("Named parameters this command accepts (e.g. 'altitude', 'heading').")]
        public string[] parameterHints = Array.Empty<string>();
    }

    // ── Structs ───────────────────────────────────────────────────────────────────

    /// <summary>Result returned after a voice command has been executed.</summary>
    [Serializable]
    public struct VoiceCommandResult
    {
        /// <summary>Whether the command was executed successfully.</summary>
        public bool success;

        /// <summary>Localization key for the response text shown on the HUD toast.</summary>
        public string responseLocKey;

        /// <summary>The command definition that was executed (or the closest match attempted).</summary>
        public VoiceCommandDefinition executedCommand;

        /// <summary>UTC timestamp of execution.</summary>
        public DateTime timestamp;

        /// <summary>Optional free-form detail message for the log view.</summary>
        public string detailMessage;

        /// <summary>Returns a successful result with the given command and response key.</summary>
        public static VoiceCommandResult Success(VoiceCommandDefinition cmd, string locKey, string detail = "")
        {
            return new VoiceCommandResult
            {
                success          = true,
                responseLocKey   = locKey,
                executedCommand  = cmd,
                timestamp        = DateTime.UtcNow,
                detailMessage    = detail
            };
        }

        /// <summary>Returns a failed result with the given command and response key.</summary>
        public static VoiceCommandResult Failure(VoiceCommandDefinition cmd, string locKey, string detail = "")
        {
            return new VoiceCommandResult
            {
                success          = false,
                responseLocKey   = locKey,
                executedCommand  = cmd,
                timestamp        = DateTime.UtcNow,
                detailMessage    = detail
            };
        }
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// Project-wide configuration for the Voice Command system.
    /// Create via <c>Assets → Create → SWEF → Voice Assistant Config</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/Voice Assistant Config", fileName = "VoiceAssistantConfig")]
    public class VoiceAssistantConfig : ScriptableObject
    {
        [Header("Activation")]
        [Tooltip("Wake-word phrase (case-insensitive) that activates the assistant.")]
        public string activationKeyword = "Hey Pilot";

        [Tooltip("Seconds the system listens for a command after activation before timing out.")]
        [Range(1f, 30f)]
        public float listenTimeoutSeconds = 5f;

        [Tooltip("Seconds to wait for voice/touch confirmation on critical commands.")]
        [Range(1f, 30f)]
        public float confirmationTimeoutSeconds = 10f;

        [Header("Recognition")]
        [Tooltip("Minimum confidence score (0–1) to accept a recognised phrase.")]
        [Range(0f, 1f)]
        public float confidenceThreshold = 0.6f;

        [Header("Categories")]
        [Tooltip("Which command categories are active. All enabled by default.")]
        public List<CommandCategory> enabledCategories = new List<CommandCategory>
        {
            CommandCategory.Flight,
            CommandCategory.Navigation,
            CommandCategory.Instruments,
            CommandCategory.Weather,
            CommandCategory.Music,
            CommandCategory.Camera,
            CommandCategory.System,
            CommandCategory.ATC,
            CommandCategory.Emergency
        };

        [Header("History")]
        [Tooltip("Maximum number of past commands stored in the history buffer.")]
        [Range(10, 500)]
        public int maxHistoryEntries = 100;
    }
}
