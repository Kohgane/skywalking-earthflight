// WorldEventType.cs — SWEF Dynamic Event & World Quest System (Phase 64)
namespace SWEF.WorldEvent
{
    /// <summary>Broad category that determines how an event is handled and displayed.</summary>
    public enum EventCategory
    {
        /// <summary>Time-sensitive distress or rescue scenario.</summary>
        Emergency,
        /// <summary>Player discovers a hidden or rare location/phenomenon.</summary>
        Discovery,
        /// <summary>Skill-based timed trial or test.</summary>
        Challenge,
        /// <summary>Guard or accompany an NPC aircraft to a destination.</summary>
        Escort,
        /// <summary>Competitive speed race through a defined course.</summary>
        Race,
        /// <summary>Meteorological phenomenon that creates gameplay opportunities.</summary>
        Weather,
        /// <summary>Unexplained occurrence that builds world lore.</summary>
        Mystery
    }

    /// <summary>Determines how urgently the event is surfaced in the UI queue.</summary>
    public enum EventPriority
    {
        /// <summary>Background flavour event; shown passively.</summary>
        Low,
        /// <summary>Standard world event worth pursuing.</summary>
        Medium,
        /// <summary>Important event that interrupts passive notifications.</summary>
        High,
        /// <summary>Must-act-now event; full-screen alert.</summary>
        Critical
    }

    /// <summary>Lifecycle state of an event instance.</summary>
    public enum EventStatus
    {
        /// <summary>Event has been spawned but the player has not entered the area yet.</summary>
        Pending,
        /// <summary>Player is engaged; objectives are live.</summary>
        Active,
        /// <summary>All objectives were satisfied successfully.</summary>
        Completed,
        /// <summary>A fail condition was triggered before completion.</summary>
        Failed,
        /// <summary>The event timer ran out before the player completed it.</summary>
        Expired
    }

    /// <summary>How demanding the objectives are for the player.</summary>
    public enum QuestDifficulty
    {
        /// <summary>Suitable for new players; generous tolerances.</summary>
        Easy,
        /// <summary>Standard challenge for experienced players.</summary>
        Normal,
        /// <summary>Requires precise flying and good knowledge of the system.</summary>
        Hard,
        /// <summary>Near-perfect execution required; no margin for error.</summary>
        Extreme
    }
}
