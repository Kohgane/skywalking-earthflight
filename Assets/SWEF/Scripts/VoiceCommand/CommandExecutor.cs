// CommandExecutor.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Singleton MonoBehaviour that executes parsed voice commands by dispatching
    /// to the appropriate subsystem.  All subsystem references are null-safe; missing
    /// integrations degrade gracefully so the game runs without every module present.
    /// </summary>
    public class CommandExecutor : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        private static CommandExecutor _instance;

        public static CommandExecutor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<CommandExecutor>();
                return _instance;
            }
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private VoiceAssistantConfig _config;

        [Header("Cooldowns")]
        [Tooltip("Default cooldown in seconds between repeated invocations of the same command.")]
        [SerializeField] private float _defaultCooldownSeconds = 1f;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private readonly Dictionary<string, float> _cooldownTimestamps =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired after a command executes (success or failure).</summary>
        public event Action<VoiceCommandResult> OnCommandExecuted;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes the given command with the supplied parameters.
        /// Returns a <see cref="VoiceCommandResult"/> describing the outcome.
        /// </summary>
        public VoiceCommandResult ExecuteCommand(
            VoiceCommandDefinition command,
            Dictionary<string, string> parameters)
        {
            if (command == null)
            {
                var fail = VoiceCommandResult.Failure(null, "voice_error_no_match");
                OnCommandExecuted?.Invoke(fail);
                return fail;
            }

            // Cooldown guard.
            if (IsOnCooldown(command.commandId))
            {
                var cd = VoiceCommandResult.Failure(command, "voice_error_cooldown");
                OnCommandExecuted?.Invoke(cd);
                return cd;
            }

            // Category enabled guard.
            if (_config != null &&
                _config.enabledCategories != null &&
                !_config.enabledCategories.Contains(command.category))
            {
                var disabled = VoiceCommandResult.Failure(command, "voice_error_category_disabled");
                OnCommandExecuted?.Invoke(disabled);
                return disabled;
            }

            // Dispatch.
            VoiceCommandResult result = Dispatch(command, parameters ?? new Dictionary<string, string>());

            if (result.success)
                RecordCooldown(command.commandId);

            OnCommandExecuted?.Invoke(result);
            return result;
        }

        // ── Dispatch ──────────────────────────────────────────────────────────────

        private VoiceCommandResult Dispatch(VoiceCommandDefinition cmd, Dictionary<string, string> p)
        {
            switch (cmd.commandId)
            {
                // Flight
                case "cmd_increase_throttle":    return FlightCmd(cmd, "increase_throttle");
                case "cmd_decrease_throttle":    return FlightCmd(cmd, "decrease_throttle");
                case "cmd_bank_left":            return FlightCmd(cmd, "bank_left");
                case "cmd_bank_right":           return FlightCmd(cmd, "bank_right");
                case "cmd_level_wings":          return FlightCmd(cmd, "level_wings");
                case "cmd_nose_up":              return FlightCmd(cmd, "nose_up");
                case "cmd_nose_down":            return FlightCmd(cmd, "nose_down");
                case "cmd_engage_autopilot":     return FlightCmd(cmd, "engage_autopilot");
                case "cmd_disengage_autopilot":  return FlightCmd(cmd, "disengage_autopilot");
                case "cmd_flaps_up":             return FlightCmd(cmd, "flaps_up");
                case "cmd_flaps_down":           return FlightCmd(cmd, "flaps_down");
                case "cmd_landing_gear_up":      return FlightCmd(cmd, "landing_gear_up");
                case "cmd_landing_gear_down":    return FlightCmd(cmd, "landing_gear_down");
                case "cmd_set_altitude":         return FlightCmdWithParam(cmd, p, "altitude");
                case "cmd_emergency_landing":    return FlightCmd(cmd, "emergency_landing");

                // Navigation
                case "cmd_set_waypoint":         return NavCmd(cmd, "set_waypoint", p);
                case "cmd_next_waypoint":        return NavCmd(cmd, "next_waypoint", p);
                case "cmd_show_route":           return NavCmd(cmd, "show_route", p);
                case "cmd_distance_destination": return NavCmd(cmd, "distance_destination", p);
                case "cmd_eta":                  return NavCmd(cmd, "eta", p);
                case "cmd_heading":              return NavCmd(cmd, "heading", p);

                // Instruments
                case "cmd_show_altimeter":          return InstrCmd(cmd, "show_altimeter");
                case "cmd_show_speed":              return InstrCmd(cmd, "show_speed");
                case "cmd_calibrate_instruments":   return InstrCmd(cmd, "calibrate");

                // Weather
                case "cmd_weather_report":       return WeatherCmd(cmd, "report");
                case "cmd_turbulence_level":     return WeatherCmd(cmd, "turbulence");
                case "cmd_wind_direction":       return WeatherCmd(cmd, "wind");
                case "cmd_visibility_check":     return WeatherCmd(cmd, "visibility");

                // Music
                case "cmd_play_music":           return MusicCmd(cmd, "play");
                case "cmd_pause_music":          return MusicCmd(cmd, "pause");
                case "cmd_next_track":           return MusicCmd(cmd, "next");
                case "cmd_volume_up":            return MusicCmd(cmd, "volume_up");
                case "cmd_volume_down":          return MusicCmd(cmd, "volume_down");

                // Camera
                case "cmd_photo_mode":           return CameraCmd(cmd, "photo_mode");
                case "cmd_take_screenshot":      return CameraCmd(cmd, "screenshot");
                case "cmd_cinematic_view":       return CameraCmd(cmd, "cinematic");
                case "cmd_cockpit_view":         return CameraCmd(cmd, "cockpit");
                case "cmd_chase_view":           return CameraCmd(cmd, "chase");

                // System
                case "cmd_pause_game":           return SysCmd(cmd, "pause");
                case "cmd_resume":               return SysCmd(cmd, "resume");
                case "cmd_save_flight":          return SysCmd(cmd, "save");
                case "cmd_show_map":             return SysCmd(cmd, "show_map");
                case "cmd_toggle_hud":           return SysCmd(cmd, "toggle_hud");
                case "cmd_toggle_minimap":       return SysCmd(cmd, "toggle_minimap");

                default:
                    return VoiceCommandResult.Failure(cmd, "voice_error_no_handler",
                        $"No handler for command id '{cmd.commandId}'.");
            }
        }

        // ── Subsystem stubs ───────────────────────────────────────────────────────
        // Each method is a null-safe integration point. Replace the log calls with
        // real subsystem references as those systems are available at runtime.

        private VoiceCommandResult FlightCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] Flight action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        private VoiceCommandResult FlightCmdWithParam(
            VoiceCommandDefinition cmd, Dictionary<string, string> p, string key)
        {
            p.TryGetValue(key, out string val);
            Debug.Log($"[CommandExecutor] Flight action: set_{key} = {val}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged",
                $"Set {key} to {val}.");
        }

        private VoiceCommandResult NavCmd(
            VoiceCommandDefinition cmd, string action, Dictionary<string, string> p)
        {
            Debug.Log($"[CommandExecutor] Nav action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        private VoiceCommandResult InstrCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] Instruments action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        private VoiceCommandResult WeatherCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] Weather action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_weather_report");
        }

        private VoiceCommandResult MusicCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] Music action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        private VoiceCommandResult CameraCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] Camera action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        private VoiceCommandResult SysCmd(VoiceCommandDefinition cmd, string action)
        {
            Debug.Log($"[CommandExecutor] System action: {action}");
            return VoiceCommandResult.Success(cmd, "voice_response_acknowledged");
        }

        // ── Cooldown helpers ──────────────────────────────────────────────────────

        private bool IsOnCooldown(string commandId)
        {
            if (_cooldownTimestamps.TryGetValue(commandId, out float last))
                return Time.realtimeSinceStartup - last < _defaultCooldownSeconds;
            return false;
        }

        private void RecordCooldown(string commandId)
        {
            _cooldownTimestamps[commandId] = Time.realtimeSinceStartup;
        }
    }
}
