using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.InputSystem
{
    // ── Enumerations ──────────────────────────────────────────────────────────────

    /// <summary>Physical device type supplying player input.</summary>
    public enum InputDeviceType
    {
        /// <summary>Standard keyboard and mouse.</summary>
        Keyboard,
        /// <summary>Console or PC gamepad / controller.</summary>
        Gamepad,
        /// <summary>Touchscreen gestures and virtual controls.</summary>
        Touch,
        /// <summary>VR motion controllers (6-DOF).</summary>
        VR,
        /// <summary>Hands-On Throttle-And-Stick flight controller.</summary>
        HOTAS
    }

    /// <summary>High-level category grouping input actions by game system.</summary>
    public enum InputActionCategory
    {
        /// <summary>Aircraft flight controls — throttle, pitch, roll, yaw.</summary>
        Flight,
        /// <summary>Camera controls — look, zoom, switch angle.</summary>
        Camera,
        /// <summary>Menus, pause, confirm, back.</summary>
        UI,
        /// <summary>Social features — chat, emotes, voice toggle.</summary>
        Social,
        /// <summary>Photo-mode capture and adjustments.</summary>
        PhotoMode,
        /// <summary>In-flight music player controls.</summary>
        MusicPlayer
    }

    // ── Serialisable Data Structures ──────────────────────────────────────────────

    /// <summary>
    /// A single rebindable action entry combining keyboard, gamepad, and secondary bindings.
    /// </summary>
    [Serializable]
    public struct BindingEntry
    {
        /// <summary>Unique internal action name, e.g. <c>"ThrottleUp"</c>.</summary>
        public string actionName;

        /// <summary>Category this action belongs to.</summary>
        public InputActionCategory category;

        /// <summary>Primary keyboard key name (matches <see cref="KeyCode"/> names).</summary>
        public string primaryKey;

        /// <summary>Optional secondary / alternative keyboard key name.</summary>
        public string secondaryKey;

        /// <summary>Gamepad button or axis name, e.g. <c>"joystick button 0"</c> or <c>"Left Trigger"</c>.</summary>
        public string gamepadButton;

        /// <summary>When <c>false</c> the binding is locked and may not be rebound in the UI.</summary>
        public bool isRebindable;
    }

    /// <summary>
    /// Configuration profile for a gamepad device including dead-zones, sensitivity, and vibration.
    /// </summary>
    [Serializable]
    public struct GamepadProfile
    {
        /// <summary>Display name for this profile, e.g. <c>"Xbox Standard"</c>.</summary>
        public string profileName;

        /// <summary>Normalised inner dead-zone radius — inputs below this are zero [0, 1].</summary>
        [Range(0f, 0.5f)] public float deadzoneInner;

        /// <summary>Normalised outer dead-zone radius — inputs above this saturate to ±1 [0, 1].</summary>
        [Range(0.5f, 1f)] public float deadzoneOuter;

        /// <summary>
        /// Control-point Y values for a custom sensitivity curve.
        /// Sampled uniformly across the 0–1 input range.
        /// </summary>
        public float[] sensitivityCurvePoints;

        /// <summary>Invert the pitch (vertical) axis when <c>true</c>.</summary>
        public bool invertPitch;

        /// <summary>Invert the yaw/look-horizontal axis when <c>true</c>.</summary>
        public bool invertYaw;

        /// <summary>Invert the roll axis when <c>true</c>.</summary>
        public bool invertRoll;

        /// <summary>Enable controller vibration/rumble when <c>true</c>.</summary>
        public bool vibrationEnabled;

        /// <summary>Returns a <see cref="GamepadProfile"/> with sensible default values.</summary>
        public static GamepadProfile Default => new GamepadProfile
        {
            profileName            = "Default",
            deadzoneInner          = 0.1f,
            deadzoneOuter          = 0.95f,
            sensitivityCurvePoints = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f },
            invertPitch            = false,
            invertYaw              = false,
            invertRoll             = false,
            vibrationEnabled       = true
        };
    }

    /// <summary>
    /// Layout descriptor for the touch-screen virtual control overlay.
    /// </summary>
    [Serializable]
    public struct TouchControlLayout
    {
        /// <summary>Anchor position of the virtual joystick in normalised screen space [0, 1]².</summary>
        public Vector2 joystickPosition;

        /// <summary>Diameter of the virtual joystick thumb area in normalised screen width.</summary>
        [Range(0.05f, 0.4f)] public float joystickSize;

        /// <summary>
        /// Positions of on-screen action buttons in normalised screen space.
        /// Each element maps to a corresponding <see cref="BindingEntry"/>.
        /// </summary>
        public Vector2[] buttonPositions;

        /// <summary>Swipe/gesture sensitivity multiplier [0.1, 5].</summary>
        [Range(0.1f, 5f)] public float gestureSensitivity;

        /// <summary>Returns a <see cref="TouchControlLayout"/> with sensible default values.</summary>
        public static TouchControlLayout Default => new TouchControlLayout
        {
            joystickPosition   = new Vector2(0.15f, 0.25f),
            joystickSize       = 0.15f,
            buttonPositions    = new Vector2[] { new Vector2(0.85f, 0.25f), new Vector2(0.75f, 0.15f) },
            gestureSensitivity = 1f
        };
    }

    /// <summary>
    /// A named collection of bindings that can be saved, loaded, and shared.
    /// </summary>
    [Serializable]
    public struct InputPreset
    {
        /// <summary>Human-readable preset name, e.g. <c>"Arcade"</c> or <c>"Sim"</c>.</summary>
        public string presetName;

        /// <summary>Short description shown in the preset selection UI.</summary>
        public string description;

        /// <summary>All binding entries contained in this preset.</summary>
        public List<BindingEntry> bindings;
    }

    // ── ScriptableObject ──────────────────────────────────────────────────────────

    /// <summary>
    /// Project-wide input system configuration asset.
    /// Create via <em>Assets → Create → SWEF → InputSystem → Input System Profile</em>.
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/InputSystem/Input System Profile", fileName = "InputSystemProfile")]
    public class InputSystemProfile : ScriptableObject
    {
        [Header("Device")]
        [Tooltip("Device type assumed at startup before auto-detection runs.")]
        public InputDeviceType defaultDeviceType = InputDeviceType.Keyboard;

        [Header("Gamepad")]
        [Tooltip("Gamepad configuration profile applied on startup.")]
        public GamepadProfile gamepadProfile = GamepadProfile.Default;

        [Header("Touch")]
        [Tooltip("Virtual control layout for touch-screen devices.")]
        public TouchControlLayout touchLayout = TouchControlLayout.Default;

        [Header("Presets")]
        [Tooltip("Built-in binding presets available to the player.")]
        public InputPreset[] defaultPresets = Array.Empty<InputPreset>();

        [Header("Behaviour")]
        [Tooltip("Persist custom bindings to PlayerPrefs across sessions.")]
        public bool persistBindings = true;

        [Tooltip("Allow the player to rebind any action marked isRebindable.")]
        public bool allowRebinding = true;

        [Tooltip("Maximum seconds to wait for a new key during a rebind listening window.")]
        [Range(3f, 30f)] public float rebindTimeoutSeconds = 8f;
    }
}
