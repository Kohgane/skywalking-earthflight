using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.Multiplayer
{
    /// <summary>
    /// Singleton MonoBehaviour that manages the lifecycle of multiplayer flight sessions.
    /// Handles session creation, discovery, joining, leaving, and participant tracking.
    /// Session history is persisted to <c>multiplayer_sessions.json</c>.
    /// </summary>
    public class MultiplayerSessionManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared instance of the session manager.</summary>
        public static MultiplayerSessionManager Instance { get; private set; }
        #endregion

        #region Constants
        private const string PersistenceFileName = "multiplayer_sessions.json";
        private const int MaxSessionHistory = 50;
        private const float DefaultPositionSyncInterval = 2f;
        #endregion

        #region Inspector
        [Header("Session Settings")]
        [SerializeField, Tooltip("How often (seconds) participant positions are synced.")]
        private float positionSyncInterval = DefaultPositionSyncInterval;

        [SerializeField, Tooltip("Maximum participants allowed when creating a session (default).")]
        private int defaultMaxParticipants = 8;
        #endregion

        #region Events
        /// <summary>Fired when the local player successfully creates a new session.</summary>
        public event Action<FlightSessionData> OnSessionCreated;
        /// <summary>Fired when the local player joins an existing session.</summary>
        public event Action<FlightSessionData> OnSessionJoined;
        /// <summary>Fired when the local player leaves the current session.</summary>
        public event Action<string> OnSessionLeft;
        /// <summary>Fired when a new participant enters the current session.</summary>
        public event Action<string> OnParticipantJoined;
        /// <summary>Fired when a participant leaves the current session.</summary>
        public event Action<string> OnParticipantLeft;
        /// <summary>Fired when the session status changes (e.g. Lobby → InProgress).</summary>
        public event Action<SessionStatus> OnSessionStateChanged;
        #endregion

        #region Public Properties
        /// <summary>The session the local player is currently in, or null.</summary>
        public FlightSessionData CurrentSession { get; private set; }

        /// <summary>Whether the local player is currently in a session.</summary>
        public bool IsInSession => CurrentSession != null;

        /// <summary>Read-only list of all public sessions available for discovery.</summary>
        public IReadOnlyList<FlightSessionData> PublicSessions => _publicSessions.AsReadOnly();
        #endregion

        #region Private State
        private readonly List<FlightSessionData> _sessionHistory = new List<FlightSessionData>();
        private readonly List<FlightSessionData> _publicSessions = new List<FlightSessionData>();
        private Coroutine _syncCoroutine;
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
            LoadSessionHistory();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Session Lifecycle
        /// <summary>
        /// Creates a new flight session hosted by the local player.
        /// </summary>
        /// <param name="sessionType">Activity type for the session.</param>
        /// <param name="isPublic">Whether the session is publicly discoverable.</param>
        /// <param name="maxParticipants">Optional participant cap; uses inspector default if 0.</param>
        /// <returns>The newly created <see cref="FlightSessionData"/>.</returns>
        public FlightSessionData CreateSession(SessionType sessionType, bool isPublic = true, int maxParticipants = 0)
        {
            if (IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: CreateSession called while already in a session. Leave current session first.");
                return null;
            }

            var session = new FlightSessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                hostId = PlayerProfileManager.Instance != null
                    ? PlayerProfileManager.Instance.GetLocalProfile()?.playerId ?? "local"
                    : "local",
                sessionType = sessionType,
                isPublic = isPublic,
                maxParticipants = maxParticipants > 0 ? maxParticipants : defaultMaxParticipants,
                startTime = DateTime.UtcNow.ToString("o"),
                status = SessionStatus.Lobby
            };

            string localId = session.hostId;
            session.participants.Add(localId);

            CurrentSession = session;
            if (isPublic)
                _publicSessions.Add(session);

            _syncCoroutine = StartCoroutine(PositionSyncLoop());

            MultiplayerAnalytics.RecordSessionCreated(sessionType.ToString());
            MultiplayerBridge.OnSessionCreated(session);

            OnSessionCreated?.Invoke(session);
            Debug.Log($"[SWEF] Multiplayer: Session created — {session.sessionId} ({sessionType})");
            return session;
        }

        /// <summary>
        /// Joins an existing session by its ID.
        /// </summary>
        /// <param name="sessionId">The ID of the session to join.</param>
        /// <returns>True if join was successful.</returns>
        public bool JoinSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Debug.LogWarning("[SWEF] Multiplayer: JoinSession called with null/empty sessionId.");
                return false;
            }
            if (IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: JoinSession called while already in a session.");
                return false;
            }

            FlightSessionData target = _publicSessions.Find(s => s.sessionId == sessionId);
            if (target == null)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Session {sessionId} not found in public list.");
                return false;
            }
            if (target.participants.Count >= target.maxParticipants)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Session {sessionId} is full.");
                return false;
            }

            string localId = PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.GetLocalProfile()?.playerId ?? "local"
                : "local";

            target.participants.Add(localId);
            CurrentSession = target;

            _syncCoroutine = StartCoroutine(PositionSyncLoop());

            MultiplayerAnalytics.RecordSessionJoined(target.sessionType.ToString());
            MultiplayerBridge.OnSessionJoined(target);

            OnSessionJoined?.Invoke(target);
            Debug.Log($"[SWEF] Multiplayer: Joined session {sessionId}");
            return true;
        }

        /// <summary>
        /// Leaves the current session. If the local player is the host, triggers host migration.
        /// </summary>
        public void LeaveSession()
        {
            if (!IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: LeaveSession called but not in a session.");
                return;
            }

            string localId = PlayerProfileManager.Instance != null
                ? PlayerProfileManager.Instance.GetLocalProfile()?.playerId ?? "local"
                : "local";
            string sessionId = CurrentSession.sessionId;

            CurrentSession.participants.Remove(localId);

            bool wasHost = CurrentSession.hostId == localId;
            if (wasHost && CurrentSession.participants.Count > 0)
                MigrateHost(CurrentSession);

            if (CurrentSession.participants.Count == 0)
            {
                CurrentSession.status = SessionStatus.Completed;
                CurrentSession.endTime = DateTime.UtcNow.ToString("o");
                _publicSessions.Remove(CurrentSession);
            }

            AddToHistory(CurrentSession);
            SaveSessionHistory();

            if (_syncCoroutine != null)
            {
                StopCoroutine(_syncCoroutine);
                _syncCoroutine = null;
            }

            CurrentSession = null;

            MultiplayerAnalytics.RecordSessionLeft();
            OnSessionLeft?.Invoke(sessionId);
            Debug.Log($"[SWEF] Multiplayer: Left session {sessionId}");
        }

        /// <summary>
        /// Returns all currently discoverable public sessions.
        /// </summary>
        /// <returns>Snapshot list of public sessions.</returns>
        public List<FlightSessionData> DiscoverPublicSessions()
        {
            return new List<FlightSessionData>(_publicSessions);
        }

        /// <summary>
        /// Transitions the current session from Lobby to InProgress.
        /// Only the host should call this.
        /// </summary>
        public void StartCurrentSession()
        {
            if (!IsInSession)
            {
                Debug.LogWarning("[SWEF] Multiplayer: StartCurrentSession called but not in a session.");
                return;
            }
            if (CurrentSession.status != SessionStatus.Lobby)
            {
                Debug.LogWarning("[SWEF] Multiplayer: Session is not in Lobby state.");
                return;
            }

            CurrentSession.status = SessionStatus.InProgress;
            CurrentSession.startTime = DateTime.UtcNow.ToString("o");
            OnSessionStateChanged?.Invoke(SessionStatus.InProgress);
        }
        #endregion

        #region Host Migration
        /// <summary>
        /// Migrates session host to the next available participant.
        /// </summary>
        private void MigrateHost(FlightSessionData session)
        {
            if (session.participants.Count == 0) return;
            string newHost = session.participants[0];
            session.hostId = newHost;
            Debug.Log($"[SWEF] Multiplayer: Host migrated to {newHost} for session {session.sessionId}");
        }
        #endregion

        #region Position Sync
        private IEnumerator PositionSyncLoop()
        {
            var wait = new WaitForSeconds(positionSyncInterval);
            while (IsInSession)
            {
                SyncLocalPosition();
                yield return wait;
            }
        }

        private void SyncLocalPosition()
        {
            if (PlayerProfileManager.Instance == null) return;
            var profile = PlayerProfileManager.Instance.GetLocalProfile();
            if (profile == null) return;
            // Position is updated on the profile; in a real network stack this would
            // broadcast to other session participants via NetworkManager2 / NetworkTransport.
            MultiplayerAnalytics.RecordPositionSync();
        }
        #endregion

        #region Persistence
        private void AddToHistory(FlightSessionData session)
        {
            _sessionHistory.Insert(0, session);
            if (_sessionHistory.Count > MaxSessionHistory)
                _sessionHistory.RemoveRange(MaxSessionHistory, _sessionHistory.Count - MaxSessionHistory);
        }

        private void SaveSessionHistory()
        {
            try
            {
                var wrapper = new SessionHistoryWrapper { sessions = _sessionHistory };
                File.WriteAllText(_persistencePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to save session history — {ex.Message}");
            }
        }

        private void LoadSessionHistory()
        {
            if (!File.Exists(_persistencePath)) return;
            try
            {
                string json = File.ReadAllText(_persistencePath);
                var wrapper = JsonUtility.FromJson<SessionHistoryWrapper>(json);
                if (wrapper?.sessions != null)
                    _sessionHistory.AddRange(wrapper.sessions);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Multiplayer: Failed to load session history — {ex.Message}");
            }
        }

        [Serializable]
        private class SessionHistoryWrapper
        {
            public List<FlightSessionData> sessions;
        }
        #endregion
    }
}
