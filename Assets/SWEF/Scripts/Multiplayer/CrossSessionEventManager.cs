using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Singleton that manages community-wide cross-session events.
    /// Handles event discovery, joining, leaderboards, and reward distribution.
    /// Persisted to <c>cross_session_events.json</c>.
    /// </summary>
    public class CrossSessionEventManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance of the event manager.</summary>
        public static CrossSessionEventManager Instance { get; private set; }
        #endregion

        #region Constants
        private const string PersistenceFileName = "cross_session_events.json";
        private const float EventCheckInterval = 60f;
        #endregion

        #region Events
        /// <summary>Fired when a new event becomes active.</summary>
        public event Action<CrossSessionEventData> OnEventStarted;
        /// <summary>Fired when an active event concludes.</summary>
        public event Action<CrossSessionEventData> OnEventEnded;
        /// <summary>Fired when the local player joins an event.</summary>
        public event Action<CrossSessionEventData> OnEventJoined;
        /// <summary>Fired when the local player earns an event reward.</summary>
        public event Action<string, string> OnEventRewardEarned;
        #endregion

        #region Public Properties
        /// <summary>The event the local player is currently participating in, or null.</summary>
        public CrossSessionEventData ActiveEvent { get; private set; }
        #endregion

        #region Private State
        private readonly List<CrossSessionEventData> _allEvents = new List<CrossSessionEventData>();
        private readonly Dictionary<string, List<string>> _leaderboards =
            new Dictionary<string, List<string>>();
        private Coroutine _eventCheckCoroutine;
        private string _persistencePath;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _persistencePath = Path.Combine(Application.persistentDataPath, PersistenceFileName);
            LoadEvents();
            SeedDefaultEvents();
        }

        private void Start()
        {
            _eventCheckCoroutine = StartCoroutine(EventCheckLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Event Queries
        /// <summary>
        /// Returns all events that are currently active (within their time window).
        /// </summary>
        public List<CrossSessionEventData> GetActiveEvents()
        {
            DateTime now = DateTime.UtcNow;
            return _allEvents.FindAll(e =>
                DateTime.TryParse(e.startTime, out DateTime start) &&
                DateTime.TryParse(e.endTime, out DateTime end) &&
                now >= start && now <= end);
        }

        /// <summary>
        /// Joins a specific event by its ID.
        /// </summary>
        /// <param name="eventId">The unique event ID.</param>
        /// <returns>True if the event was found and joined successfully.</returns>
        public bool JoinEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: JoinEvent called with null/empty eventId.");
                return false;
            }

            CrossSessionEventData evt = _allEvents.Find(e => e.eventId == eventId);
            if (evt == null)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Event {eventId} not found.");
                return false;
            }
            if (!evt.isActive)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Event {eventId} is not active.");
                return false;
            }

            ActiveEvent = evt;
            evt.participantCount++;
            SaveEvents();

            MultiplayerAnalytics.RecordEventJoined(evt.eventType.ToString());
            MultiplayerBridge.OnEventJoined(evt);

            OnEventJoined?.Invoke(evt);
            Debug.Log($"[SWEF] Multiplayer: Joined event — {evt.title}");
            return true;
        }

        /// <summary>
        /// Leaves the current active event.
        /// </summary>
        public void LeaveEvent()
        {
            if (ActiveEvent == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: LeaveEvent called but not in an event.");
                return;
            }
            ActiveEvent = null;
        }

        /// <summary>
        /// Returns the ordered leaderboard for a specific event.
        /// </summary>
        /// <param name="eventId">Event ID to query.</param>
        /// <returns>Ordered list of player IDs (best first).</returns>
        public List<string> GetEventLeaderboard(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return new List<string>();
            return _leaderboards.TryGetValue(eventId, out var board)
                ? new List<string>(board)
                : new List<string>();
        }

        /// <summary>
        /// Distributes rewards to the local player for completing the current event.
        /// </summary>
        public void CompleteCurrentEvent()
        {
            if (ActiveEvent == null)
            {
                Debug.LogWarning("[SWEF] Multiplayer: CompleteCurrentEvent — no active event.");
                return;
            }

            string rewardPayload = ActiveEvent.rewards;
            MultiplayerAnalytics.RecordEventCompleted(ActiveEvent.eventType.ToString());
            MultiplayerBridge.OnEventCompleted(ActiveEvent);

            OnEventRewardEarned?.Invoke(ActiveEvent.eventId, rewardPayload);
            ActiveEvent = null;
        }
        #endregion

        #region Event Scheduling Loop
        private IEnumerator EventCheckLoop()
        {
            var wait = new WaitForSeconds(EventCheckInterval);
            while (true)
            {
                RefreshEventStates();
                yield return wait;
            }
        }

        private void RefreshEventStates()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var evt in _allEvents)
            {
                bool shouldBeActive =
                    DateTime.TryParse(evt.startTime, out DateTime start) &&
                    DateTime.TryParse(evt.endTime, out DateTime end) &&
                    now >= start && now <= end;

                if (!evt.isActive && shouldBeActive)
                {
                    evt.isActive = true;
                    OnEventStarted?.Invoke(evt);
                }
                else if (evt.isActive && !shouldBeActive)
                {
                    evt.isActive = false;
                    OnEventEnded?.Invoke(evt);
                    if (ActiveEvent?.eventId == evt.eventId)
                        ActiveEvent = null;
                }
            }
        }
        #endregion

        #region Default Event Seeding
        private void SeedDefaultEvents()
        {
            if (_allEvents.Count > 0) return;
            DateTime now = DateTime.UtcNow;

            var templates = EventScheduler.GetActiveEventTemplates(now);
            foreach (var template in templates)
            {
                template.isActive = true;
                _allEvents.Add(template);
            }
            SaveEvents();
        }
        #endregion

        #region Persistence
        private void SaveEvents()
        {
            try
            {
                var wrapper = new EventListWrapper { events = _allEvents };
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save events — {ex.Message}");
            }
        }

        private void LoadEvents()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json = File.ReadAllText(_persistencePath);
                var wrapper = JsonUtility.FromJson<EventListWrapper>(json);
                if (wrapper?.events != null)
                    _allEvents.AddRange(wrapper.events);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load events — {ex.Message}");
            }
        }

        [Serializable]
        private class EventListWrapper
        {
            public List<CrossSessionEventData> events;
        }
        #endregion
    }
}
