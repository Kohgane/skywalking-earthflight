namespace SWEF.UI
{
    /// <summary>
    /// Colorblind correction modes supported by <see cref="AccessibilityController"/>.
    /// </summary>
    public enum ColorblindMode
    {
        /// <summary>No correction applied — full colour vision.</summary>
        Normal,

        /// <summary>Red-blind — difficulty distinguishing red and green.</summary>
        Protanopia,

        /// <summary>Green-blind — difficulty distinguishing red and green.</summary>
        Deuteranopia,

        /// <summary>Blue-yellow blind — difficulty distinguishing blue and yellow.</summary>
        Tritanopia,

        /// <summary>Total colour blindness — sees only shades of grey.</summary>
        Achromatopsia,
    }
}
