// Phase 98 — PC Input & Controls Polish
// Assets/SWEF/Scripts/PCInput/GamepadProfile.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.PCInput
{
    /// <summary>Type identifier for a gamepad layout.</summary>
    public enum GamepadType
    {
        /// <summary>Xbox controller layout (A/B/X/Y).</summary>
        Xbox,
        /// <summary>PlayStation controller layout (Cross/Circle/Square/Triangle).</summary>
        PlayStation,
        /// <summary>Generic / unknown gamepad.</summary>
        Generic,
        /// <summary>User-created custom profile.</summary>
        Custom
    }

    /// <summary>Maps a logical action name to a Unity Input button/axis string.</summary>
    [Serializable]
    public class ButtonMapping
    {
        /// <summary>Logical action name (e.g., "Jump", "ToggleAutopilot").</summary>
        public string actionName;
        /// <summary>Unity Input Manager button name (e.g., "joystick button 0").</summary>
        public string unityButtonName;
    }

    /// <summary>Maps a logical axis name to a Unity Input axis string with optional inversion.</summary>
    [Serializable]
    public class AxisMapping
    {
        /// <summary>Logical axis name (e.g., "Pitch", "Yaw").</summary>
        public string axisName;
        /// <summary>Unity Input Manager axis name (e.g., "Horizontal").</summary>
        public string unityAxisName;
        /// <summary>Whether to invert the axis value.</summary>
        public bool inverted;
        /// <summary>Dead-zone threshold (0–1).</summary>
        public float deadZone = 0.1f;
    }

    /// <summary>
    /// Data class for a single gamepad mapping profile.
    /// Serializable to JSON for save/load via <see cref="GamepadProfileManager"/>.
    /// </summary>
    [Serializable]
    public class GamepadProfile
    {
        /// <summary>Human-readable profile name.</summary>
        public string ProfileName;

        /// <summary>Gamepad type this profile targets.</summary>
        public GamepadType GamepadType;

        /// <summary>All axis mappings for this profile.</summary>
        public List<AxisMapping> AxisMappings = new List<AxisMapping>();

        /// <summary>All button mappings for this profile.</summary>
        public List<ButtonMapping> ButtonMappings = new List<ButtonMapping>();

        /// <summary>Create a deep copy of this profile.</summary>
        /// <returns>A new <see cref="GamepadProfile"/> with the same values.</returns>
        public GamepadProfile Clone()
        {
            string json = JsonUtility.ToJson(this);
            var clone = JsonUtility.FromJson<GamepadProfile>(json);
            clone.ProfileName = ProfileName + " (Custom)";
            clone.GamepadType = GamepadType.Custom;
            return clone;
        }

        /// <summary>Serialise this profile to a JSON string.</summary>
        public string ToJson() => JsonUtility.ToJson(this, prettyPrint: true);

        /// <summary>Deserialise a profile from a JSON string.</summary>
        /// <param name="json">JSON string produced by <see cref="ToJson"/>.</param>
        /// <returns>Deserialised <see cref="GamepadProfile"/> or <c>null</c> on error.</returns>
        public static GamepadProfile FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<GamepadProfile>(json); }
            catch (Exception e)
            {
                Debug.LogWarning($"[GamepadProfile] FromJson failed: {e.Message}");
                return null;
            }
        }

        #region Factory helpers
        /// <summary>Build the default Xbox controller profile.</summary>
        public static GamepadProfile CreateXboxDefault()
        {
            var p = new GamepadProfile
            {
                ProfileName = "Xbox (Default)",
                GamepadType = GamepadType.Xbox,
                AxisMappings = new List<AxisMapping>
                {
                    new AxisMapping { axisName = "Pitch",           unityAxisName = "Vertical",         inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "Yaw",             unityAxisName = "Horizontal",        inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "CameraVertical",  unityAxisName = "RightStickVertical",inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "CameraHorizontal",unityAxisName = "RightStickHorizontal",inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "ThrottleUp",      unityAxisName = "RightTrigger",      inverted = false, deadZone = 0.05f },
                    new AxisMapping { axisName = "ThrottleDown",    unityAxisName = "LeftTrigger",       inverted = false, deadZone = 0.05f },
                },
                ButtonMappings = new List<ButtonMapping>
                {
                    new ButtonMapping { actionName = "ToggleAutopilot", unityButtonName = "joystick button 0" }, // A
                    new ButtonMapping { actionName = "ToggleHUD",        unityButtonName = "joystick button 2" }, // X
                    new ButtonMapping { actionName = "ToggleMinimap",    unityButtonName = "joystick button 3" }, // Y
                    new ButtonMapping { actionName = "Menu",             unityButtonName = "joystick button 7" }, // Start
                    new ButtonMapping { actionName = "RollLeft",         unityButtonName = "joystick button 4" }, // LB
                    new ButtonMapping { actionName = "RollRight",        unityButtonName = "joystick button 5" }, // RB
                    new ButtonMapping { actionName = "DPadMap",          unityButtonName = "joystick button 11" },// D-pad up
                    new ButtonMapping { actionName = "Screenshot",       unityButtonName = "joystick button 12" },// D-pad right
                }
            };
            return p;
        }

        /// <summary>Build the default PlayStation controller profile.</summary>
        public static GamepadProfile CreatePlayStationDefault()
        {
            var p = new GamepadProfile
            {
                ProfileName = "PlayStation (Default)",
                GamepadType = GamepadType.PlayStation,
                AxisMappings = new List<AxisMapping>
                {
                    new AxisMapping { axisName = "Pitch",           unityAxisName = "Vertical",          inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "Yaw",             unityAxisName = "Horizontal",         inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "CameraVertical",  unityAxisName = "RightStickVertical", inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "CameraHorizontal",unityAxisName = "RightStickHorizontal",inverted = false, deadZone = 0.1f },
                    new AxisMapping { axisName = "ThrottleUp",      unityAxisName = "RightTrigger",       inverted = false, deadZone = 0.05f },
                    new AxisMapping { axisName = "ThrottleDown",    unityAxisName = "LeftTrigger",        inverted = false, deadZone = 0.05f },
                },
                ButtonMappings = new List<ButtonMapping>
                {
                    new ButtonMapping { actionName = "ToggleAutopilot", unityButtonName = "joystick button 0" }, // Cross
                    new ButtonMapping { actionName = "ToggleHUD",        unityButtonName = "joystick button 2" }, // Square
                    new ButtonMapping { actionName = "ToggleMinimap",    unityButtonName = "joystick button 3" }, // Triangle
                    new ButtonMapping { actionName = "Menu",             unityButtonName = "joystick button 9" }, // Options
                    new ButtonMapping { actionName = "RollLeft",         unityButtonName = "joystick button 4" }, // L1
                    new ButtonMapping { actionName = "RollRight",        unityButtonName = "joystick button 5" }, // R1
                    new ButtonMapping { actionName = "DPadMap",          unityButtonName = "joystick button 11" },
                    new ButtonMapping { actionName = "Screenshot",       unityButtonName = "joystick button 12" },
                }
            };
            return p;
        }

        /// <summary>Build a generic / unknown gamepad profile.</summary>
        public static GamepadProfile CreateGenericDefault()
        {
            var p = new GamepadProfile
            {
                ProfileName = "Generic (Default)",
                GamepadType = GamepadType.Generic,
                AxisMappings = new List<AxisMapping>
                {
                    new AxisMapping { axisName = "Pitch",           unityAxisName = "Vertical",   inverted = false, deadZone = 0.15f },
                    new AxisMapping { axisName = "Yaw",             unityAxisName = "Horizontal",  inverted = false, deadZone = 0.15f },
                    new AxisMapping { axisName = "ThrottleUp",      unityAxisName = "RightTrigger",inverted = false, deadZone = 0.1f  },
                    new AxisMapping { axisName = "ThrottleDown",    unityAxisName = "LeftTrigger", inverted = false, deadZone = 0.1f  },
                },
                ButtonMappings = new List<ButtonMapping>
                {
                    new ButtonMapping { actionName = "ToggleAutopilot", unityButtonName = "joystick button 0" },
                    new ButtonMapping { actionName = "Menu",             unityButtonName = "joystick button 7" },
                }
            };
            return p;
        }
        #endregion
    }
}
