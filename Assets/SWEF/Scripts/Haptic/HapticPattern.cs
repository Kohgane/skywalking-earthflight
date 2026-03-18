namespace SWEF.Haptic
{
    /// <summary>
    /// Named haptic patterns available throughout the SWEF app.
    /// </summary>
    public enum HapticPattern
    {
        /// <summary>Short, gentle tap — UI interactions.</summary>
        Light,

        /// <summary>Medium tap — standard button press.</summary>
        Medium,

        /// <summary>Strong pulse — significant actions.</summary>
        Heavy,

        /// <summary>Double-tap pattern — positive confirmation.</summary>
        Success,

        /// <summary>Single medium pulse — caution / advisory.</summary>
        Warning,

        /// <summary>Long heavy pulse — error / failure.</summary>
        Error,

        /// <summary>Two quick taps — for toggle or double-tap interactions.</summary>
        DoubleTap,

        /// <summary>Series of rapid light pulses.</summary>
        RapidPulse,

        /// <summary>Three short bursts — approaching the Kármán line.</summary>
        AltitudeWarning,

        /// <summary>Rising three-step pattern — teleport arrival.</summary>
        TeleportComplete,

        /// <summary>Single crisp pulse — camera shutter feedback.</summary>
        ScreenshotSnap,

        /// <summary>Rising five-step celebration pattern — achievement unlocked.</summary>
        AchievementUnlock,

        /// <summary>Continuous light pulses while boost throttle is active.</summary>
        Boost,

        /// <summary>Heavy double-pulse — stall speed warning.</summary>
        Stall,
    }
}
