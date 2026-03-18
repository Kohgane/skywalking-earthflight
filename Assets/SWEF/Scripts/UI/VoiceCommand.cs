namespace SWEF.UI
{
    /// <summary>
    /// Voice commands recognised by <see cref="VoiceCommandManager"/>.
    /// </summary>
    public enum VoiceCommand
    {
        /// <summary>Capture a screenshot.</summary>
        Screenshot,

        /// <summary>Open the teleport destination picker.</summary>
        Teleport,

        /// <summary>Pause the flight simulation.</summary>
        Pause,

        /// <summary>Resume the flight simulation.</summary>
        Resume,

        /// <summary>Increase target altitude.</summary>
        AltitudeUp,

        /// <summary>Decrease target altitude.</summary>
        AltitudeDown,

        /// <summary>Increase throttle / speed.</summary>
        SpeedUp,

        /// <summary>Decrease throttle / speed.</summary>
        SlowDown,

        /// <summary>Toggle HUD visibility.</summary>
        ToggleHUD,

        /// <summary>Recenter the XR camera origin.</summary>
        Recenter,
    }
}
