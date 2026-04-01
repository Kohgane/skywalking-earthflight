// CommandRegistry.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Singleton registry that stores all active <see cref="VoiceCommandDefinition"/>s.
    /// Built-in commands (40+) are registered on Awake; external systems can call
    /// <see cref="Register"/> to add custom commands at runtime.
    /// </summary>
    public class CommandRegistry : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────

        private static CommandRegistry _instance;

        public static CommandRegistry Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<CommandRegistry>();
                return _instance;
            }
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<string, VoiceCommandDefinition> _registry =
            new Dictionary<string, VoiceCommandDefinition>(StringComparer.OrdinalIgnoreCase);

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
            RegisterBuiltInCommands();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a command. Overwrites if the same <c>commandId</c> already exists.</summary>
        public void Register(VoiceCommandDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.commandId))
            {
                Debug.LogWarning("[CommandRegistry] Attempted to register null or id-less command.");
                return;
            }
            _registry[def.commandId] = def;
        }

        /// <summary>Removes the command with the given id, if present.</summary>
        public void Unregister(string commandId)
        {
            if (!string.IsNullOrEmpty(commandId))
                _registry.Remove(commandId);
        }

        /// <summary>Returns all commands in the given category.</summary>
        public VoiceCommandDefinition[] GetByCategory(CommandCategory category)
        {
            var result = new List<VoiceCommandDefinition>();
            foreach (var def in _registry.Values)
                if (def.category == category) result.Add(def);
            return result.ToArray();
        }

        /// <summary>Returns every registered command.</summary>
        public VoiceCommandDefinition[] GetAll() => new List<VoiceCommandDefinition>(_registry.Values).ToArray();

        /// <summary>Returns the command with the given id, or null if not found.</summary>
        public VoiceCommandDefinition GetById(string commandId)
        {
            if (string.IsNullOrEmpty(commandId)) return null;
            _registry.TryGetValue(commandId, out var def);
            return def;
        }

        // ── Built-in commands ─────────────────────────────────────────────────────

        private void RegisterBuiltInCommands()
        {
            // ── Flight ───────────────────────────────────────────────────────────
            Reg("cmd_increase_throttle", "increase throttle",
                new[] { "throttle up", "more power", "speed up" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_increase_throttle");

            Reg("cmd_decrease_throttle", "decrease throttle",
                new[] { "throttle down", "reduce power", "slow down" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_decrease_throttle");

            Reg("cmd_set_altitude", "set altitude",
                new[] { "climb to", "descend to", "altitude" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_set_altitude", new[] { "altitude" });

            Reg("cmd_bank_left", "bank left",
                new[] { "turn left", "roll left" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_bank_left");

            Reg("cmd_bank_right", "bank right",
                new[] { "turn right", "roll right" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_bank_right");

            Reg("cmd_level_wings", "level wings",
                new[] { "wings level", "stabilise", "stabilize" },
                CommandCategory.Flight, CommandPriority.High, false,
                "voice_cmd_desc_level_wings");

            Reg("cmd_nose_up", "nose up",
                new[] { "pitch up", "pull up" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_nose_up");

            Reg("cmd_nose_down", "nose down",
                new[] { "pitch down", "push down" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_nose_down");

            Reg("cmd_engage_autopilot", "engage autopilot",
                new[] { "autopilot on", "activate autopilot" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_engage_autopilot");

            Reg("cmd_disengage_autopilot", "disengage autopilot",
                new[] { "autopilot off", "deactivate autopilot", "disable autopilot" },
                CommandCategory.Flight, CommandPriority.High, false,
                "voice_cmd_desc_disengage_autopilot");

            Reg("cmd_flaps_up", "flaps up",
                new[] { "retract flaps" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_flaps_up");

            Reg("cmd_flaps_down", "flaps down",
                new[] { "extend flaps", "deploy flaps" },
                CommandCategory.Flight, CommandPriority.Normal, false,
                "voice_cmd_desc_flaps_down");

            Reg("cmd_landing_gear_up", "landing gear up",
                new[] { "retract gear", "gear up" },
                CommandCategory.Flight, CommandPriority.Critical, true,
                "voice_cmd_desc_landing_gear_up");

            Reg("cmd_landing_gear_down", "landing gear down",
                new[] { "deploy gear", "gear down", "lower gear" },
                CommandCategory.Flight, CommandPriority.Critical, true,
                "voice_cmd_desc_landing_gear_down");

            Reg("cmd_emergency_landing", "emergency landing",
                new[] { "mayday", "emergency", "declare emergency" },
                CommandCategory.Emergency, CommandPriority.Critical, true,
                "voice_cmd_desc_emergency_landing");

            // ── Navigation ───────────────────────────────────────────────────────
            Reg("cmd_set_waypoint", "set waypoint",
                new[] { "navigate to", "go to" },
                CommandCategory.Navigation, CommandPriority.Normal, false,
                "voice_cmd_desc_set_waypoint", new[] { "name" });

            Reg("cmd_next_waypoint", "next waypoint",
                new[] { "skip waypoint", "advance waypoint" },
                CommandCategory.Navigation, CommandPriority.Normal, false,
                "voice_cmd_desc_next_waypoint");

            Reg("cmd_show_route", "show route",
                new[] { "display route", "view route" },
                CommandCategory.Navigation, CommandPriority.Low, false,
                "voice_cmd_desc_show_route");

            Reg("cmd_distance_destination", "distance to destination",
                new[] { "how far", "range to destination" },
                CommandCategory.Navigation, CommandPriority.Low, false,
                "voice_cmd_desc_distance_destination");

            Reg("cmd_eta", "estimated time of arrival",
                new[] { "eta", "time to destination", "arrival time" },
                CommandCategory.Navigation, CommandPriority.Low, false,
                "voice_cmd_desc_eta");

            Reg("cmd_heading", "heading",
                new[] { "set heading", "fly heading" },
                CommandCategory.Navigation, CommandPriority.Normal, false,
                "voice_cmd_desc_heading", new[] { "degrees" });

            // ── Instruments ──────────────────────────────────────────────────────
            Reg("cmd_show_altimeter", "show altimeter",
                new[] { "altitude gauge", "display altimeter" },
                CommandCategory.Instruments, CommandPriority.Low, false,
                "voice_cmd_desc_show_altimeter");

            Reg("cmd_show_speed", "show speed",
                new[] { "display speed", "airspeed indicator" },
                CommandCategory.Instruments, CommandPriority.Low, false,
                "voice_cmd_desc_show_speed");

            Reg("cmd_calibrate_instruments", "calibrate instruments",
                new[] { "reset instruments", "instrument reset" },
                CommandCategory.Instruments, CommandPriority.Normal, false,
                "voice_cmd_desc_calibrate_instruments");

            // ── Weather ──────────────────────────────────────────────────────────
            Reg("cmd_weather_report", "weather report",
                new[] { "weather update", "current weather", "what's the weather" },
                CommandCategory.Weather, CommandPriority.Low, false,
                "voice_cmd_desc_weather_report");

            Reg("cmd_turbulence_level", "turbulence level",
                new[] { "turbulence", "how rough" },
                CommandCategory.Weather, CommandPriority.Low, false,
                "voice_cmd_desc_turbulence_level");

            Reg("cmd_wind_direction", "wind direction",
                new[] { "wind check", "wind speed" },
                CommandCategory.Weather, CommandPriority.Low, false,
                "voice_cmd_desc_wind_direction");

            Reg("cmd_visibility_check", "visibility check",
                new[] { "visibility", "how clear" },
                CommandCategory.Weather, CommandPriority.Low, false,
                "voice_cmd_desc_visibility_check");

            // ── Music ────────────────────────────────────────────────────────────
            Reg("cmd_play_music", "play music",
                new[] { "start music", "music on" },
                CommandCategory.Music, CommandPriority.Low, false,
                "voice_cmd_desc_play_music");

            Reg("cmd_pause_music", "pause music",
                new[] { "stop music", "music off" },
                CommandCategory.Music, CommandPriority.Low, false,
                "voice_cmd_desc_pause_music");

            Reg("cmd_next_track", "next track",
                new[] { "skip track", "next song" },
                CommandCategory.Music, CommandPriority.Low, false,
                "voice_cmd_desc_next_track");

            Reg("cmd_volume_up", "volume up",
                new[] { "louder", "increase volume" },
                CommandCategory.Music, CommandPriority.Low, false,
                "voice_cmd_desc_volume_up");

            Reg("cmd_volume_down", "volume down",
                new[] { "quieter", "decrease volume" },
                CommandCategory.Music, CommandPriority.Low, false,
                "voice_cmd_desc_volume_down");

            // ── Camera ───────────────────────────────────────────────────────────
            Reg("cmd_photo_mode", "photo mode",
                new[] { "camera mode", "enable photo mode" },
                CommandCategory.Camera, CommandPriority.Low, false,
                "voice_cmd_desc_photo_mode");

            Reg("cmd_take_screenshot", "take screenshot",
                new[] { "screenshot", "capture" },
                CommandCategory.Camera, CommandPriority.Low, false,
                "voice_cmd_desc_take_screenshot");

            Reg("cmd_cinematic_view", "cinematic view",
                new[] { "cinematic camera", "movie mode" },
                CommandCategory.Camera, CommandPriority.Low, false,
                "voice_cmd_desc_cinematic_view");

            Reg("cmd_cockpit_view", "cockpit view",
                new[] { "interior view", "inside view" },
                CommandCategory.Camera, CommandPriority.Low, false,
                "voice_cmd_desc_cockpit_view");

            Reg("cmd_chase_view", "chase view",
                new[] { "follow cam", "chase camera" },
                CommandCategory.Camera, CommandPriority.Low, false,
                "voice_cmd_desc_chase_view");

            // ── System ───────────────────────────────────────────────────────────
            Reg("cmd_pause_game", "pause game",
                new[] { "pause", "freeze" },
                CommandCategory.System, CommandPriority.Normal, false,
                "voice_cmd_desc_pause_game");

            Reg("cmd_resume", "resume",
                new[] { "resume game", "unpause", "continue" },
                CommandCategory.System, CommandPriority.Normal, false,
                "voice_cmd_desc_resume");

            Reg("cmd_save_flight", "save flight",
                new[] { "save game", "save progress" },
                CommandCategory.System, CommandPriority.Normal, false,
                "voice_cmd_desc_save_flight");

            Reg("cmd_show_map", "show map",
                new[] { "open map", "display map" },
                CommandCategory.System, CommandPriority.Low, false,
                "voice_cmd_desc_show_map");

            Reg("cmd_toggle_hud", "toggle hud",
                new[] { "hide hud", "show hud", "hud toggle" },
                CommandCategory.System, CommandPriority.Low, false,
                "voice_cmd_desc_toggle_hud");

            Reg("cmd_toggle_minimap", "toggle minimap",
                new[] { "hide minimap", "show minimap", "minimap toggle" },
                CommandCategory.System, CommandPriority.Low, false,
                "voice_cmd_desc_toggle_minimap");
        }

        // ── Helper ────────────────────────────────────────────────────────────────

        private void Reg(
            string id, string primary, string[] aliases,
            CommandCategory category, CommandPriority priority,
            bool requiresConfirmation, string descKey,
            string[] paramHints = null)
        {
            Register(new VoiceCommandDefinition
            {
                commandId             = id,
                primaryPhrase         = primary,
                aliases               = aliases ?? Array.Empty<string>(),
                category              = category,
                priority              = priority,
                requiresConfirmation  = requiresConfirmation,
                descriptionLocKey     = descKey,
                parameterHints        = paramHints ?? Array.Empty<string>()
            });
        }
    }
}
