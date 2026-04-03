// InputRemapData.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Serializable custom input mapping for a single remappable action.
    /// Stored and restored by <see cref="InputRemapController"/>.
    /// </summary>
    [Serializable]
    public class InputRemapData
    {
        [Tooltip("Unique action identifier matching the SWEF action registry.")]
        public string actionName;

        [Tooltip("Primary keyboard / mouse binding (e.g. \"Space\", \"Mouse0\").")]
        public string primaryKey;

        [Tooltip("Secondary / alternative keyboard binding.")]
        public string secondaryKey;

        [Tooltip("Gamepad button binding (e.g. \"ButtonSouth\", \"LeftTrigger\").")]
        public string gamepadButton;

        [Tooltip("Touch gesture label (e.g. \"SwipeUp\", \"TwoFingerTap\").")]
        public string touchGesture;
    }

    /// <summary>
    /// Static registry of all remappable action names in SWEF.
    /// </summary>
    public static class RemappableActions
    {
        // Flight controls
        public const string Throttle        = "Flight_Throttle";
        public const string PitchUp         = "Flight_PitchUp";
        public const string PitchDown       = "Flight_PitchDown";
        public const string RollLeft        = "Flight_RollLeft";
        public const string RollRight       = "Flight_RollRight";
        public const string YawLeft         = "Flight_YawLeft";
        public const string YawRight        = "Flight_YawRight";
        public const string Brake           = "Flight_Brake";
        public const string Gear            = "Flight_Gear";
        public const string Flaps           = "Flight_Flaps";
        public const string AutoHover       = "Flight_AutoHover";

        // Camera
        public const string CameraOrbit     = "Camera_Orbit";
        public const string CameraZoom      = "Camera_Zoom";
        public const string CameraMode      = "Camera_CycleMode";
        public const string PhotoMode       = "Camera_PhotoMode";

        // Navigation
        public const string Map             = "Nav_OpenMap";
        public const string Waypoint        = "Nav_SetWaypoint";
        public const string FlightPlan      = "Nav_FlightPlan";

        // HUD
        public const string HUDToggle       = "HUD_Toggle";
        public const string HUDScale        = "HUD_ScaleCycle";
        public const string Subtitles       = "HUD_ToggleSubtitles";

        // UI
        public const string Pause           = "UI_Pause";
        public const string Confirm         = "UI_Confirm";
        public const string Cancel          = "UI_Cancel";
        public const string Screenshot      = "UI_Screenshot";

        /// <summary>Returns an ordered list of all default remappable action names.</summary>
        public static IReadOnlyList<string> All { get; } = new[]
        {
            Throttle, PitchUp, PitchDown, RollLeft, RollRight, YawLeft, YawRight,
            Brake, Gear, Flaps, AutoHover,
            CameraOrbit, CameraZoom, CameraMode, PhotoMode,
            Map, Waypoint, FlightPlan,
            HUDToggle, HUDScale, Subtitles,
            Pause, Confirm, Cancel, Screenshot
        };
    }
}
