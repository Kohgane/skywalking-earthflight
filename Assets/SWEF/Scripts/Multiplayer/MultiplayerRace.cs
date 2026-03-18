using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Configuration for a multiplayer altitude race.
    /// </summary>
    [Serializable]
    public class RaceConfig
    {
        /// <summary>Altitude in metres that constitutes the finish line (default: 100,000 m).</summary>
        public float targetAltitudeM = 100000f;

        /// <summary>Pre-race countdown duration in seconds (default: 5).</summary>
        public float countdownSeconds = 5f;

        /// <summary>Whether teleporting is permitted during the race (default: false).</summary>
        public bool allowTeleport = false;

        /// <summary>Number of intermediate checkpoints (default: 5).</summary>
        public int checkpointCount = 5;

        /// <summary>Altitude of each checkpoint in metres (auto-generated if empty).</summary>
        public float[] checkpointAltitudes;
    }

    /// <summary>
    /// Result record for a single player at the end of a race.
    /// </summary>
    [Serializable]
    public class RaceResult
    {
        /// <summary>Player identifier.</summary>
        public string playerId;

        /// <summary>Display name.</summary>
        public string playerName;

        /// <summary>UTC ticks when the player crossed the finish line (or race ended).</summary>
        public long finishTimeTicks;

        /// <summary>Altitude reached when the race finished.</summary>
        public float finalAltitude;

        /// <summary>UTC ticks when each checkpoint was reached (same length as config checkpoints).</summary>
        public long[] checkpointTimes;

        /// <summary>Finish time as a formatted string (mm:ss.f).</summary>
        public string FinishTimeFormatted
        {
            get
            {
                var span = TimeSpan.FromTicks(finishTimeTicks);
                return $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds / 100}";
            }
        }
    }

    // ── Race State ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Possible states of the race state machine.
    /// </summary>
    public enum RaceState
    {
        /// <summary>Players are in the pre-race lobby.</summary>
        Lobby,
        /// <summary>Countdown is running before race starts.</summary>
        Countdown,
        /// <summary>Race is in progress.</summary>
        Racing,
        /// <summary>Race has finished (all players crossed finish or timeout).</summary>
        Finished
    }

    // ── Multiplayer Race ─────────────────────────────────────────────────────────

    /// <summary>
    /// Manages the full lifecycle of a multiplayer altitude race:
    /// lobby → countdown → racing → finished.
    /// Tracks player altitudes, checkpoints, and live leaderboard.
    /// Includes basic anti-cheat: suspiciously fast altitude gains are rejected.
    /// </summary>
    public class MultiplayerRace : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static MultiplayerRace Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] private RaceConfig config = new RaceConfig();

        [Header("Anti-Cheat")]
        [Tooltip("Maximum altitude gain in metres per second before data is rejected.")]
        [SerializeField] private float maxAltitudeGainPerSec = 5000f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the race state machine transitions to a new state.</summary>
        public event Action<RaceState> OnRaceStateChanged;

        /// <summary>Fired when any player reaches an altitude checkpoint.</summary>
        public event Action<string, int> OnCheckpointReached;

        /// <summary>Fired when the race ends, with final sorted results.</summary>
        public event Action<List<RaceResult>> OnRaceFinished;

        // ── State ────────────────────────────────────────────────────────────────
        private RaceState _state = RaceState.Lobby;
        private float _countdownRemaining;
        private long  _raceStartTicks;

        // Per-player tracking
        private readonly Dictionary<string, RaceResult>       _results    = new Dictionary<string, RaceResult>();
        private readonly Dictionary<string, float>            _lastAlt    = new Dictionary<string, float>();
        private readonly Dictionary<string, float>            _lastAltTime = new Dictionary<string, float>();
        private readonly Dictionary<string, HashSet<int>>     _checkpointsHit = new Dictionary<string, HashSet<int>>();

        // Live leaderboard (sorted by altitude descending)
        private readonly List<RaceResult> _leaderboard = new List<RaceResult>();

        private RoomManager _roomManager;
        private PlayerSyncController _syncController;
        private Flight.AltitudeController _localAltitude;

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>Current state of the race state machine.</summary>
        public RaceState State => _state;

        /// <summary>Remaining countdown time in seconds (only meaningful in Countdown state).</summary>
        public float CountdownRemaining => _countdownRemaining;

        /// <summary>Live-sorted copy of the race leaderboard.</summary>
        public IReadOnlyList<RaceResult> Leaderboard => _leaderboard.AsReadOnly();

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _roomManager    = RoomManager.Instance != null
                ? RoomManager.Instance
                : FindFirstObjectByType<RoomManager>();
            _syncController = FindFirstObjectByType<PlayerSyncController>();
            _localAltitude  = FindFirstObjectByType<Flight.AltitudeController>();

            if (_syncController != null)
                _syncController.OnRemotePlayerUpdated += OnRemoteSyncUpdate;

            GenerateCheckpoints();
        }

        private void Update()
        {
            switch (_state)
            {
                case RaceState.Countdown:
                    TickCountdown();
                    break;

                case RaceState.Racing:
                    TickRacing();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_syncController != null)
                _syncController.OnRemotePlayerUpdated -= OnRemoteSyncUpdate;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the pre-race countdown. May only be called by the room host.
        /// </summary>
        public void StartRace()
        {
            if (_roomManager != null && !_roomManager.IsHost)
            {
                Debug.LogWarning("[SWEF][MultiplayerRace] Only the host can start the race.");
                return;
            }

            if (_state != RaceState.Lobby)
            {
                Debug.LogWarning("[SWEF][MultiplayerRace] Race can only be started from Lobby state.");
                return;
            }

            InitialisePlayers();
            TransitionTo(RaceState.Countdown);
            _countdownRemaining = config.countdownSeconds;
            Debug.Log("[SWEF][MultiplayerRace] Race countdown started.");
        }

        /// <summary>
        /// Immediately aborts the current race and returns to Lobby state.
        /// </summary>
        public void AbortRace()
        {
            if (_state == RaceState.Lobby) return;
            Debug.Log("[SWEF][MultiplayerRace] Race aborted.");
            TransitionTo(RaceState.Lobby);
        }

        /// <summary>
        /// Updates the tracked altitude for a player, validates it against the
        /// anti-cheat ceiling, and checks for checkpoint/finish crossings.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="altitudeM">Reported altitude in metres.</param>
        public void UpdatePlayerAltitude(string playerId, float altitudeM)
        {
            if (_state != RaceState.Racing) return;

            // Anti-cheat: reject suspiciously fast altitude gains
            if (_lastAlt.TryGetValue(playerId, out float prevAlt) &&
                _lastAltTime.TryGetValue(playerId, out float prevTime))
            {
                float dt = Time.time - prevTime;
                if (dt > 0f)
                {
                    float gainRate = (altitudeM - prevAlt) / dt;
                    if (gainRate > maxAltitudeGainPerSec)
                    {
                        Debug.LogWarning($"[SWEF][MultiplayerRace] Anti-cheat: rejected altitude update for '{playerId}' ({gainRate:F0} m/s).");
                        return;
                    }
                }
            }

            _lastAlt[playerId]     = altitudeM;
            _lastAltTime[playerId] = Time.time;

            if (!_results.TryGetValue(playerId, out RaceResult result)) return;
            result.finalAltitude = altitudeM;

            CheckCheckpoints(playerId, altitudeM, result);
            CheckFinish(playerId, altitudeM, result);

            UpdateLeaderboard();
        }

        // ── Race State Machine ────────────────────────────────────────────────────

        private void TransitionTo(RaceState newState)
        {
            _state = newState;
            Debug.Log($"[SWEF][MultiplayerRace] State → {newState}");
            OnRaceStateChanged?.Invoke(newState);
        }

        private void TickCountdown()
        {
            _countdownRemaining -= Time.deltaTime;
            if (_countdownRemaining <= 0f)
            {
                _raceStartTicks = DateTime.UtcNow.Ticks;
                TransitionTo(RaceState.Racing);
            }
        }

        private void TickRacing()
        {
            // Update local player altitude
            if (_localAltitude != null && _roomManager != null)
            {
                string localId = FindFirstObjectByType<MultiplayerManager>()?.LocalPlayerId ?? "";
                if (!string.IsNullOrEmpty(localId))
                    UpdatePlayerAltitude(localId, _localAltitude.CurrentAltitudeMeters);
            }

            // Check if all players have finished
            bool allFinished = true;
            foreach (var kvp in _results)
            {
                if (kvp.Value.finishTimeTicks == 0)
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished && _results.Count > 0)
                FinishRace();
        }

        private void FinishRace()
        {
            // Any player who hasn't finished yet gets their current altitude as final
            long now = DateTime.UtcNow.Ticks;
            foreach (var kvp in _results)
            {
                if (kvp.Value.finishTimeTicks == 0)
                    kvp.Value.finishTimeTicks = now;
            }

            UpdateLeaderboard();
            TransitionTo(RaceState.Finished);

            var sorted = new List<RaceResult>(_results.Values);
            sorted.Sort((a, b) => b.finalAltitude.CompareTo(a.finalAltitude));

            Debug.Log($"[SWEF][MultiplayerRace] Race finished. Winner: {(sorted.Count > 0 ? sorted[0].playerName : "N/A")}");
            OnRaceFinished?.Invoke(sorted);
        }

        // ── Checkpoint Logic ─────────────────────────────────────────────────────

        private void CheckCheckpoints(string playerId, float alt, RaceResult result)
        {
            if (config.checkpointAltitudes == null) return;

            if (!_checkpointsHit.TryGetValue(playerId, out HashSet<int> hit))
            {
                hit = new HashSet<int>();
                _checkpointsHit[playerId] = hit;
            }

            for (int i = 0; i < config.checkpointAltitudes.Length; i++)
            {
                if (!hit.Contains(i) && alt >= config.checkpointAltitudes[i])
                {
                    hit.Add(i);
                    result.checkpointTimes[i] = DateTime.UtcNow.Ticks - _raceStartTicks;
                    Debug.Log($"[SWEF][MultiplayerRace] Checkpoint {i} reached by '{playerId}'.");
                    OnCheckpointReached?.Invoke(playerId, i);
                }
            }
        }

        private void CheckFinish(string playerId, float alt, RaceResult result)
        {
            if (result.finishTimeTicks != 0) return;
            if (alt >= config.targetAltitudeM)
            {
                result.finishTimeTicks = DateTime.UtcNow.Ticks - _raceStartTicks;
                Debug.Log($"[SWEF][MultiplayerRace] Player '{playerId}' finished!");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void InitialisePlayers()
        {
            _results.Clear();
            _leaderboard.Clear();
            _lastAlt.Clear();
            _lastAltTime.Clear();
            _checkpointsHit.Clear();

            if (_roomManager == null) return;

            foreach (PlayerInfo info in _roomManager.PlayersInRoom)
            {
                _results[info.playerId] = new RaceResult
                {
                    playerId       = info.playerId,
                    playerName     = info.playerName,
                    finishTimeTicks = 0,
                    finalAltitude  = 0f,
                    checkpointTimes = new long[config.checkpointAltitudes?.Length ?? 0]
                };
            }
        }

        private void GenerateCheckpoints()
        {
            if (config.checkpointAltitudes != null && config.checkpointAltitudes.Length == config.checkpointCount)
                return;

            config.checkpointAltitudes = new float[] { 1000f, 5000f, 20000f, 50000f, 100000f };
            config.checkpointCount = config.checkpointAltitudes.Length;
        }

        private void UpdateLeaderboard()
        {
            _leaderboard.Clear();
            foreach (var r in _results.Values)
                _leaderboard.Add(r);

            _leaderboard.Sort((a, b) =>
            {
                // Finished players ranked by finish time; non-finished by altitude
                if (a.finishTimeTicks > 0 && b.finishTimeTicks > 0)
                    return a.finishTimeTicks.CompareTo(b.finishTimeTicks);
                if (a.finishTimeTicks > 0) return -1;
                if (b.finishTimeTicks > 0) return 1;
                return b.finalAltitude.CompareTo(a.finalAltitude);
            });
        }

        private void OnRemoteSyncUpdate(string playerId, PlayerSyncData data)
        {
            if (_state != RaceState.Racing) return;
            UpdatePlayerAltitude(playerId, data.altitude);
        }
    }
}
