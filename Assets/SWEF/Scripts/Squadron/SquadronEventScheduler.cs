// SquadronEventScheduler.cs — Phase 109: Clan/Squadron System
// Manages scheduled squadron events: creation, RSVP, recurring logic, reminders.
// Namespace: SWEF.Squadron

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Squadron
{
    /// <summary>
    /// Phase 109 — Manages the full lifecycle of scheduled squadron events,
    /// including creation, cancellation, RSVP tracking, recurring event support,
    /// and in-game reminder notifications.
    ///
    /// <para>Attach alongside <see cref="SquadronManager"/> on the persistent scene object.</para>
    /// </summary>
    public sealed class SquadronEventScheduler : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static SquadronEventScheduler Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Raised when a new event is created.</summary>
        public event Action<SquadronEvent> OnEventCreated;

        /// <summary>Raised when an event's start time is reached.</summary>
        public event Action<SquadronEvent> OnEventStarted;

        /// <summary>Raised when an event's end time is reached.</summary>
        public event Action<SquadronEvent> OnEventEnded;

        /// <summary>Raised when an event is cancelled by an officer/leader.</summary>
        public event Action<SquadronEvent> OnEventCancelled;

        // ── State ──────────────────────────────────────────────────────────────

        private readonly List<SquadronEvent> _upcomingEvents = new List<SquadronEvent>();
        private readonly List<SquadronEvent> _pastEvents     = new List<SquadronEvent>();
        private readonly HashSet<string>     _startedEventIds = new HashSet<string>();

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEvents();
        }

        private void Start()
        {
            StartCoroutine(EventTickCoroutine());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates and schedules a new squadron event.
        /// Requires <see cref="SquadronPermission.ManageEvents"/>.
        /// </summary>
        public SquadronEvent CreateEvent(
            SquadronEventType eventType,
            string title,
            string description,
            long startTimeUtc,
            long endTimeUtc,
            string location      = "",
            int requiredMembers  = SquadronConfig.EventMinParticipants,
            string rewards       = "",
            bool isRecurring     = false,
            string recurrencePattern = "")
        {
            var manager = SquadronManager.Instance;
            if (manager == null || !manager.HasPermission(SquadronPermission.ManageEvents))
            {
                Debug.LogWarning("[SquadronEventScheduler] No permission to manage events.");
                return null;
            }

            if (_upcomingEvents.Count >= SquadronConfig.MaxUpcomingEvents)
            {
                Debug.LogWarning("[SquadronEventScheduler] Max upcoming events reached.");
                return null;
            }

            if (endTimeUtc <= startTimeUtc)
            {
                Debug.LogWarning("[SquadronEventScheduler] End time must be after start time.");
                return null;
            }

            var ev = new SquadronEvent
            {
                eventId           = Guid.NewGuid().ToString(),
                eventType         = eventType,
                title             = title,
                description       = description,
                startTime         = startTimeUtc,
                endTime           = endTimeUtc,
                location          = location ?? string.Empty,
                requiredMembers   = Mathf.Max(SquadronConfig.EventMinParticipants, requiredMembers),
                rewards           = rewards ?? string.Empty,
                isRecurring       = isRecurring,
                recurrencePattern = recurrencePattern ?? string.Empty
            };

            _upcomingEvents.Add(ev);
            SaveEvents();
            OnEventCreated?.Invoke(ev);
            return ev;
        }

        /// <summary>
        /// Cancels an upcoming event. Requires <see cref="SquadronPermission.ManageEvents"/>.
        /// </summary>
        public bool CancelEvent(string eventId)
        {
            var manager = SquadronManager.Instance;
            if (manager == null || !manager.HasPermission(SquadronPermission.ManageEvents))
                return false;

            var ev = _upcomingEvents.FirstOrDefault(e => e.eventId == eventId);
            if (ev == null) return false;

            _upcomingEvents.Remove(ev);
            _startedEventIds.Remove(ev.eventId);
            SaveEvents();
            OnEventCancelled?.Invoke(ev);
            return true;
        }

        /// <summary>
        /// Records or updates the local player's RSVP for an event.
        /// </summary>
        public bool SetRSVP(string eventId, SquadronRSVP rsvp)
        {
            var manager = SquadronManager.Instance;
            if (manager?.LocalMember == null) return false;

            var ev = _upcomingEvents.FirstOrDefault(e => e.eventId == eventId);
            if (ev == null) return false;

            ev.rsvpMap[manager.LocalMember.memberId] = rsvp;
            SaveEvents();
            return true;
        }

        /// <summary>Returns all upcoming events (read-only).</summary>
        public IReadOnlyList<SquadronEvent> GetUpcomingEvents() => _upcomingEvents.AsReadOnly();

        /// <summary>Returns past events (read-only).</summary>
        public IReadOnlyList<SquadronEvent> GetPastEvents() => _pastEvents.AsReadOnly();

        /// <summary>Returns members who RSVP'd as <see cref="SquadronRSVP.Attending"/> for the event.</summary>
        public List<string> GetConfirmedAttendees(string eventId)
        {
            var ev = _upcomingEvents.FirstOrDefault(e => e.eventId == eventId);
            if (ev == null) return new List<string>();

            return ev.rsvpMap
                .Where(kvp => kvp.Value == SquadronRSVP.Attending)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // ── Event tick coroutine ───────────────────────────────────────────────

        private IEnumerator EventTickCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f);
                TickEvents();
            }
        }

        private void TickEvents()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool changed = false;

            for (int i = _upcomingEvents.Count - 1; i >= 0; i--)
            {
                var ev = _upcomingEvents[i];

                // Fire OnEventStarted once when the event becomes active
                if (ev.startTime <= now && !_startedEventIds.Contains(ev.eventId))
                {
                    _startedEventIds.Add(ev.eventId);
                    OnEventStarted?.Invoke(ev);
                }

                if (ev.endTime <= now)
                {
                    ev.attendedMemberIds = ev.rsvpMap
                        .Where(kvp => kvp.Value == SquadronRSVP.Attending)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    _upcomingEvents.RemoveAt(i);
                    _pastEvents.Add(ev);
                    _startedEventIds.Remove(ev.eventId);
                    OnEventEnded?.Invoke(ev);

                    // Schedule next occurrence for recurring events
                    if (ev.isRecurring && !string.IsNullOrEmpty(ev.recurrencePattern))
                        ScheduleRecurrence(ev);

                    changed = true;
                }
            }

            if (changed) SaveEvents();
        }

        private void ScheduleRecurrence(SquadronEvent original)
        {
            long duration = original.endTime - original.startTime;
            // Default: weekly recurrence (7 days)
            long offsetSeconds = 7L * 24 * 3600;

            var next = new SquadronEvent
            {
                eventId           = Guid.NewGuid().ToString(),
                eventType         = original.eventType,
                title             = original.title,
                description       = original.description,
                startTime         = original.startTime + offsetSeconds,
                endTime           = original.endTime   + offsetSeconds,
                location          = original.location,
                requiredMembers   = original.requiredMembers,
                rewards           = original.rewards,
                isRecurring       = true,
                recurrencePattern = original.recurrencePattern
            };

            if (_upcomingEvents.Count < SquadronConfig.MaxUpcomingEvents)
            {
                _upcomingEvents.Add(next);
                OnEventCreated?.Invoke(next);
            }
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void SaveEvents()
        {
            try
            {
                var wrapper = new EventListWrapper
                {
                    upcoming = _upcomingEvents,
                    past     = _pastEvents
                };
                File.WriteAllText(
                    Path.Combine(Application.persistentDataPath, SquadronConfig.EventsDataFile),
                    JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronEventScheduler] Save error: {ex.Message}");
            }
        }

        private void LoadEvents()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SquadronConfig.EventsDataFile);
                if (!File.Exists(path)) return;

                var wrapper = JsonUtility.FromJson<EventListWrapper>(File.ReadAllText(path));
                if (wrapper == null) return;

                _upcomingEvents.Clear();
                _pastEvents.Clear();

                if (wrapper.upcoming != null) _upcomingEvents.AddRange(wrapper.upcoming);
                if (wrapper.past     != null) _pastEvents.AddRange(wrapper.past);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SquadronEventScheduler] Load error: {ex.Message}");
            }
        }

        [Serializable]
        private class EventListWrapper
        {
            public List<SquadronEvent> upcoming;
            public List<SquadronEvent> past;
        }
    }
}
