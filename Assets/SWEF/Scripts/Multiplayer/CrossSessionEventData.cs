using System;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>Type of community-wide cross-session event.</summary>
    public enum CrossSessionEventType
    {
        /// <summary>Organised airshow with formation routines and manoeuvres.</summary>
        AirShow,
        /// <summary>Multi-group formation flying challenge.</summary>
        FormationChallenge,
        /// <summary>Rally to discover and visit waypoints across a region.</summary>
        ExplorationRally,
        /// <summary>Timed speed run over a defined course.</summary>
        SpeedRun,
        /// <summary>Cooperative delivery or rescue mission for the whole community.</summary>
        CommunityMission,
        /// <summary>Community response to an extreme weather scenario.</summary>
        WeatherEvent,
        /// <summary>Seasonal festival tied to real-world calendar events.</summary>
        SeasonalFestival
    }

    /// <summary>
    /// Serializable record of a community-wide cross-session event.
    /// Persisted in <c>cross_session_events.json</c>.
    /// </summary>
    [Serializable]
    public class CrossSessionEventData
    {
        /// <summary>Unique event identifier (GUID string).</summary>
        [Tooltip("Unique event ID (GUID).")]
        public string eventId;

        /// <summary>Classification of the event.</summary>
        [Tooltip("Event category.")]
        public CrossSessionEventType eventType;

        /// <summary>Short display title of the event.</summary>
        [Tooltip("Event title shown in the UI.")]
        public string title;

        /// <summary>Longer description explaining the event objectives.</summary>
        [Tooltip("Event description and objectives.")]
        public string description;

        /// <summary>UTC timestamp (ISO-8601) when the event starts.</summary>
        [Tooltip("Event start time (UTC ISO-8601).")]
        public string startTime;

        /// <summary>UTC timestamp (ISO-8601) when the event ends.</summary>
        [Tooltip("Event end time (UTC ISO-8601).")]
        public string endTime;

        /// <summary>Latitude of the event's primary location.</summary>
        [Tooltip("Event centre latitude (decimal degrees).")]
        public double locationLatitude;

        /// <summary>Longitude of the event's primary location.</summary>
        [Tooltip("Event centre longitude (decimal degrees).")]
        public double locationLongitude;

        /// <summary>Radius of the event area in kilometres.</summary>
        [Tooltip("Event area radius (km).")]
        public float radius;

        /// <summary>JSON-serialised reward data (XP, unlocks, badges).</summary>
        [Tooltip("Serialised reward payload.")]
        public string rewards;

        /// <summary>Number of players who have joined this event.</summary>
        [Tooltip("Current participant count.")]
        public int participantCount;

        /// <summary>Whether the event is currently active (derived from start/end times).</summary>
        [Tooltip("True while the event window is open.")]
        public bool isActive;
    }
}
