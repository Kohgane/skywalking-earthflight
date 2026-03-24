// RadarEnums.cs — SWEF Radar & Threat Detection System (Phase 67)
namespace SWEF.Radar
{
    /// <summary>IFF classification assigned to a radar contact.</summary>
    public enum ContactClassification
    {
        /// <summary>Identity could not be determined.</summary>
        Unknown,
        /// <summary>Confirmed allied aircraft or unit.</summary>
        Friendly,
        /// <summary>Non-combatant, neutral party.</summary>
        Neutral,
        /// <summary>Confirmed enemy contact.</summary>
        Hostile,
        /// <summary>Non-military civilian aircraft or vessel.</summary>
        Civilian,
        /// <summary>Fixed navigation or terrain landmark.</summary>
        Landmark,
        /// <summary>Dynamic world-event beacon or objective marker.</summary>
        Event
    }

    /// <summary>Threat severity rating assigned by <see cref="ThreatDetector"/>.</summary>
    public enum ThreatLevel
    {
        /// <summary>No threat detected.</summary>
        None,
        /// <summary>Distant or slow-moving hostile — monitor only.</summary>
        Low,
        /// <summary>Closing hostile within medium range.</summary>
        Medium,
        /// <summary>Fast-closing hostile within close range.</summary>
        High,
        /// <summary>Immediate engagement — missile or collision risk.</summary>
        Imminent
    }

    /// <summary>Operating mode of the radar system.</summary>
    public enum RadarMode
    {
        /// <summary>Radar is completely powered down.</summary>
        Off,
        /// <summary>Receive-only — detects active emissions without transmitting.</summary>
        Passive,
        /// <summary>Full transmit/receive operation.</summary>
        Active,
        /// <summary>Wide-area search sweep.</summary>
        Search,
        /// <summary>Narrow beam locked onto a specific contact.</summary>
        Track
    }

    /// <summary>Relative radar cross-section of a detectable object.</summary>
    public enum BlipSize
    {
        /// <summary>Low-observable or small target (e.g., UAV, stealth aircraft).</summary>
        Small,
        /// <summary>Standard fighter or light transport.</summary>
        Medium,
        /// <summary>Large transport, bomber, or warship.</summary>
        Large,
        /// <summary>Capital ship or very-large structure.</summary>
        VeryLarge
    }
}
