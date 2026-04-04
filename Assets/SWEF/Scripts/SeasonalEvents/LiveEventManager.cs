// Phase 99 — Seasonal Live Events & Battle Pass
// Assets/SWEF/Scripts/SeasonalEvents/LiveEventManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.SeasonalEvents
{
    /// <summary>
    /// Manages limited-time live events within the active season.
    ///
    /// <para>Responsibilities:</para>
    /// <list type="bullet">
    ///   <item>Register and deregister <see cref="LiveEvent"/> definitions.</item>
    ///   <item>Poll event start/end times and fire lifecycle events.</item>
    ///   <item>Track community-goal progress and fire progress events.</item>
    ///   <item>Provide countdown timers for event start and end.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-43)]
    [DisallowMultipleComponent]
    public class LiveEventManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static LiveEventManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Events
        /// <summary>Fired when a live event transitions from pending to active.</summary>
        public event Action<LiveEvent> OnLiveEventStarted;

        /// <summary>Fired when an active live event ends.</summary>
        public event Action<LiveEvent> OnLiveEventEnded;

        /// <summary>
        /// Fired when community-goal progress changes.
        /// Parameter: current progress as a 0–1 fraction.
        /// </summary>
        public event Action<float> OnCommunityGoalProgress;
        #endregion

        #region Inspector
        [Header("Poll Interval")]
        [Tooltip("How often (seconds) the manager checks event start/end transitions.")]
        [SerializeField, Range(5f, 300f)] private float pollIntervalSeconds = 30f;
        #endregion

        #region State
        private readonly List<LiveEvent> _allEvents    = new List<LiveEvent>();
        private readonly HashSet<string> _activeIds    = new HashSet<string>();
        private Coroutine _pollCoroutine;

        /// <summary>All registered live events (active and pending).</summary>
        public IReadOnlyList<LiveEvent> AllEvents => _allEvents.AsReadOnly();

        /// <summary>Currently active live events.</summary>
        public IEnumerable<LiveEvent> ActiveEvents
        {
            get
            {
                foreach (var e in _allEvents)
                    if (e.IsActive()) yield return e;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _pollCoroutine = StartCoroutine(PollEvents());
        }
        #endregion

        #region Event Registration
        /// <summary>Registers a new live event definition.</summary>
        public void RegisterEvent(LiveEvent liveEvent)
        {
            if (liveEvent == null || string.IsNullOrEmpty(liveEvent.EventId)) return;
            if (_allEvents.Exists(e => e.EventId == liveEvent.EventId))
            {
                Debug.LogWarning($"[SWEF] LiveEventManager: Duplicate event id '{liveEvent.EventId}' ignored.");
                return;
            }
            _allEvents.Add(liveEvent);
        }

        /// <summary>Removes a live event by its ID.</summary>
        public void UnregisterEvent(string eventId)
        {
            _allEvents.RemoveAll(e => e.EventId == eventId);
            _activeIds.Remove(eventId);
        }
        #endregion

        #region Community Goal
        /// <summary>
        /// Adds a player's contribution to a community-goal event.
        /// </summary>
        /// <param name="eventId">Target event ID.</param>
        /// <param name="contribution">Amount contributed by this player in this session.</param>
        public void AddCommunityContribution(string eventId, float contribution)
        {
            if (contribution <= 0f) return;
            var evt = _allEvents.Find(e => e.EventId == eventId);
            if (evt == null || !evt.IsActive()) return;

            evt.CurrentValue = Mathf.Min(evt.CurrentValue + contribution, evt.TargetValue);
            float fraction = evt.CommunityProgressFraction;
            OnCommunityGoalProgress?.Invoke(fraction);

            CheckCommunityRewardTiers(evt);
        }

        private void CheckCommunityRewardTiers(LiveEvent evt)
        {
            if (evt.RewardTiers == null) return;
            float pct = evt.CommunityProgressFraction * 100f;
            foreach (var tier in evt.RewardTiers)
            {
                if (pct >= tier.ProgressPercent && tier.Reward != null)
                {
                    Debug.Log($"[SWEF] LiveEventManager: Community reward tier unlocked — '{tier.Reward.DisplayName}' for event '{evt.EventId}'.");
                }
            }
        }
        #endregion

        #region Countdown Timers
        /// <summary>
        /// Returns the time remaining until the specified event ends.
        /// Returns <see cref="TimeSpan.Zero"/> if the event has already ended or is not found.
        /// </summary>
        public TimeSpan GetEventTimeRemaining(string eventId)
        {
            var evt = _allEvents.Find(e => e.EventId == eventId);
            return evt?.TimeRemaining() ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Returns the time until the specified event starts.
        /// Returns <see cref="TimeSpan.Zero"/> if the event has already started or is not found.
        /// </summary>
        public TimeSpan GetEventTimeUntilStart(string eventId)
        {
            var evt = _allEvents.Find(e => e.EventId == eventId);
            if (evt == null) return TimeSpan.Zero;
            var start = evt.GetStartTimeUtc();
            var remaining = start - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        #endregion

        #region Poll Coroutine
        private IEnumerator PollEvents()
        {
            while (true)
            {
                CheckTransitions();
                yield return new WaitForSeconds(pollIntervalSeconds);
            }
        }

        private void CheckTransitions()
        {
            foreach (var evt in _allEvents)
            {
                bool isCurrentlyActive = evt.IsActive();

                if (isCurrentlyActive && !_activeIds.Contains(evt.EventId))
                {
                    _activeIds.Add(evt.EventId);
                    Debug.Log($"[SWEF] LiveEventManager: Event started — '{evt.EventName}'");
                    OnLiveEventStarted?.Invoke(evt);
                }
                else if (!isCurrentlyActive && _activeIds.Contains(evt.EventId))
                {
                    _activeIds.Remove(evt.EventId);
                    Debug.Log($"[SWEF] LiveEventManager: Event ended — '{evt.EventName}'");
                    OnLiveEventEnded?.Invoke(evt);
                }
            }
        }
        #endregion
    }
}
