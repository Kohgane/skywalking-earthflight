using UnityEngine;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Data definition for a single interactive tutorial step.
    /// Supports localized text, UI spotlight targeting, and action-based auto-advance.
    /// </summary>
    [System.Serializable]
    public class TutorialStepData
    {
        /// <summary>
        /// Localization key used to look up the instruction text via <c>LocalizationManager</c>.
        /// Falls back to <see cref="fallbackText"/> when the key is not found.
        /// </summary>
        public string localizationKey;

        /// <summary>Fallback English text shown when the localization key is missing.</summary>
        [TextArea(2, 4)]
        public string fallbackText;

        /// <summary>
        /// Name of the RectTransform in the HUD to spotlight.
        /// Leave empty for no spotlight (e.g. welcome / completion steps).
        /// </summary>
        public string spotlightTargetName;

        /// <summary>
        /// When <c>true</c> the step auto-advances as soon as the player performs
        /// the action identified by <see cref="requiredActionId"/>.
        /// </summary>
        public bool requiresAction;

        /// <summary>
        /// Identifier for the required gameplay action.
        /// Recognised values: <c>"look_around"</c>, <c>"throttle_change"</c>,
        /// <c>"altitude_change"</c>, <c>"roll_left"</c>, <c>"roll_right"</c>,
        /// <c>"comfort_toggle"</c>, <c>"settings_open"</c>, <c>"teleport_open"</c>,
        /// <c>"screenshot_take"</c>.
        /// </summary>
        public string requiredActionId;

        /// <summary>
        /// Maximum seconds to wait for the required action before offering to skip.
        /// Set to <c>0</c> to wait indefinitely.
        /// </summary>
        public float actionTimeoutSec;

        /// <summary>Which direction the tooltip arrow should point toward the spotlight target.</summary>
        public TooltipAnchor tooltipAnchor;
    }

    /// <summary>Direction the tooltip arrow points toward the highlighted UI element.</summary>
    public enum TooltipAnchor
    {
        /// <summary>Arrow points downward (tooltip is above the target).</summary>
        Bottom,
        /// <summary>Arrow points upward (tooltip is below the target).</summary>
        Top,
        /// <summary>Arrow points to the right (tooltip is to the left of the target).</summary>
        Left,
        /// <summary>Arrow points to the left (tooltip is to the right of the target).</summary>
        Right,
        /// <summary>No directional arrow; tooltip appears centered on screen.</summary>
        Center
    }
}
