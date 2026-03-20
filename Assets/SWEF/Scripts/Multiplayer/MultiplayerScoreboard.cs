using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Multiplayer
{
    // ── Enums ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// End-of-session awards given to standout players.
    /// </summary>
    public enum SessionAward
    {
        /// <summary>Highest overall score.</summary>
        MVP,
        /// <summary>Best formation accuracy percentage.</summary>
        BestWingman,
        /// <summary>Most objectives completed.</summary>
        MostObjectives,
        /// <summary>Longest total flight time.</summary>
        IronPilot,
        /// <summary>Most missions completed.</summary>
        SharpShooter
    }

    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggregated stats for a single player during a multiplayer session.
    /// </summary>
    [Serializable]
    public class PlayerSessionStats
    {
        /// <summary>Player identifier.</summary>
        public string playerId;
        /// <summary>Display name shown in the scoreboard.</summary>
        public string displayName;
        /// <summary>Total accumulated score.</summary>
        public float totalScore;
        /// <summary>Formation accuracy score (0–100 %).</summary>
        public float formationScore;
        /// <summary>Total distance flown in kilometres.</summary>
        public float distanceFlownKm;
        /// <summary>Number of mission objectives completed.</summary>
        public int objectivesCompleted;
        /// <summary>Total flight time in seconds.</summary>
        public float flightTime;
        /// <summary>Number of co-op missions completed.</summary>
        public int missionsCompleted;
        /// <summary>Current connection quality.</summary>
        public ConnectionQuality connectionQuality;
        /// <summary>Current RTT in milliseconds (for ping display).</summary>
        public float rttMs;
        /// <summary>Awards received at the end of the session.</summary>
        public List<SessionAward> awards = new();
    }

    /// <summary>
    /// Final session summary passed to the <see cref="MultiplayerScoreboard.OnSessionSummaryReady"/> event.
    /// </summary>
    [Serializable]
    public class SessionSummary
    {
        /// <summary>All player stats, sorted by score descending.</summary>
        public List<PlayerSessionStats> rankings = new();
        /// <summary>Player ID of the MVP award winner.</summary>
        public string mvpPlayerId;
        /// <summary>Player ID of the best wingman award winner.</summary>
        public string bestWingmanPlayerId;
        /// <summary>Total session duration in seconds.</summary>
        public float sessionDurationSec;
    }

    // ── MultiplayerScoreboard ─────────────────────────────────────────────────────

    /// <summary>
    /// Real-time scoreboard and end-of-session summary for Phase 33.
    ///
    /// <para>The host broadcasts a <see cref="PlayerSessionStats"/> snapshot every
    /// <see cref="statBroadcastInterval"/> seconds.  All connected players display the same
    /// live scoreboard, sortable by score, formation accuracy, distance, or objectives.
    /// At session end, awards are computed and the <see cref="OnSessionSummaryReady"/> event
    /// is fired.</para>
    /// </summary>
    public class MultiplayerScoreboard : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MultiplayerScoreboard Instance { get; private set; }

        // ── Inspector — Sync ──────────────────────────────────────────────────────
        [Header("Sync")]
        [Tooltip("How often (seconds) the host broadcasts the full stats snapshot.")]
        [SerializeField] private float statBroadcastInterval = 5f;

        // ── Inspector — UI ────────────────────────────────────────────────────────
        [Header("UI — Scoreboard Panel")]
        [Tooltip("Root panel for the live scoreboard.")]
        [SerializeField] private RectTransform scoreboardPanel;

        [Tooltip("Prefab for a single scoreboard row.")]
        [SerializeField] private RectTransform rowPrefab;

        [Tooltip("Content RectTransform inside the scoreboard scroll rect.")]
        [SerializeField] private RectTransform contentParent;

        [Header("UI — Session Summary")]
        [Tooltip("Root panel for the session summary screen.")]
        [SerializeField] private RectTransform summaryPanel;

        [Tooltip("Text element displaying the MVP player name.")]
        [SerializeField] private Text mvpNameText;

        [Tooltip("Text element displaying the best wingman name.")]
        [SerializeField] private Text bestWingmanText;

        [Tooltip("Text element displaying the longest flight time.")]
        [SerializeField] private Text ironPilotText;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when any player's score is updated.</summary>
        public event Action<PlayerSessionStats> OnScoreUpdated;

        /// <summary>Fired when a player receives a session award.</summary>
        public event Action<string, SessionAward> OnAwardGranted;

        /// <summary>Fired when the session summary is ready to display.</summary>
        public event Action<SessionSummary> OnSessionSummaryReady;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Read-only live stats for all tracked players.</summary>
        public IReadOnlyList<PlayerSessionStats> AllStats => _stats;

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<PlayerSessionStats> _stats = new();
        private readonly List<RectTransform>      _rows  = new();
        private float _broadcastTimer;
        private float _sessionStartTime;
        private SortMode _currentSortMode = SortMode.Score;

        private enum SortMode { Score, Formation, Distance, Objectives }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sessionStartTime = Time.time;
        }

        private void Start()
        {
            // Listen for formation score updates.
            if (FormationFlyingManager.Instance != null)
                FormationFlyingManager.Instance.OnSlotScoreUpdated += HandleFormationScoreUpdated;

            // Listen for objective completions.
            if (CoopMissionSystem.Instance != null)
                CoopMissionSystem.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;

            if (scoreboardPanel != null)
                scoreboardPanel.gameObject.SetActive(false);
            if (summaryPanel != null)
                summaryPanel.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Host periodically broadcasts stats.
            if (NetworkManager2.Instance != null && NetworkManager2.Instance.IsHost)
            {
                _broadcastTimer += Time.deltaTime;
                if (_broadcastTimer >= statBroadcastInterval)
                {
                    _broadcastTimer = 0f;
                    BroadcastStats();
                }
            }
        }

        private void OnDestroy()
        {
            if (FormationFlyingManager.Instance != null)
                FormationFlyingManager.Instance.OnSlotScoreUpdated -= HandleFormationScoreUpdated;
            if (CoopMissionSystem.Instance != null)
                CoopMissionSystem.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a player to be tracked in the scoreboard.
        /// </summary>
        /// <param name="playerId">Player identifier.</param>
        /// <param name="displayName">Display name shown in the scoreboard.</param>
        public void RegisterPlayer(string playerId, string displayName)
        {
            if (_stats.Any(s => s.playerId == playerId)) return;

            _stats.Add(new PlayerSessionStats
            {
                playerId    = playerId,
                displayName = displayName
            });

            Debug.Log($"[SWEF][MultiplayerScoreboard] Registered player {displayName} ({playerId}).");
        }

        /// <summary>
        /// Removes a player from the scoreboard (e.g. on disconnect).
        /// </summary>
        /// <param name="playerId">Player to remove.</param>
        public void UnregisterPlayer(string playerId)
        {
            _stats.RemoveAll(s => s.playerId == playerId);
            RefreshUI();
        }

        /// <summary>
        /// Adds score to a player's total and fires <see cref="OnScoreUpdated"/>.
        /// </summary>
        /// <param name="playerId">Target player.</param>
        /// <param name="scoreToAdd">Points to add.</param>
        public void AddScore(string playerId, float scoreToAdd)
        {
            var stat = GetOrCreateStats(playerId);
            stat.totalScore += scoreToAdd;
            OnScoreUpdated?.Invoke(stat);
            RefreshUI();
        }

        /// <summary>
        /// Records additional flight distance for a player.
        /// </summary>
        /// <param name="playerId">Target player.</param>
        /// <param name="deltaKm">Kilometres flown since last update.</param>
        public void AddFlightDistance(string playerId, float deltaKm)
        {
            var stat = GetOrCreateStats(playerId);
            stat.distanceFlownKm += deltaKm;
            stat.flightTime += Time.deltaTime;
        }

        /// <summary>
        /// Updates connection quality metrics for a player.
        /// </summary>
        /// <param name="playerId">Target player.</param>
        /// <param name="quality">Connection quality tier.</param>
        /// <param name="rttMs">Round-trip time in milliseconds.</param>
        public void UpdateConnectionMetrics(string playerId, ConnectionQuality quality, float rttMs)
        {
            var stat = GetOrCreateStats(playerId);
            stat.connectionQuality = quality;
            stat.rttMs = rttMs;
        }

        /// <summary>
        /// Receives a full stats snapshot from the host and updates the local scoreboard.
        /// </summary>
        /// <param name="receivedStats">List of player stats from the host broadcast.</param>
        public void ReceiveStatsBroadcast(List<PlayerSessionStats> receivedStats)
        {
            if (receivedStats == null) return;

            foreach (var incoming in receivedStats)
            {
                var existing = _stats.FirstOrDefault(s => s.playerId == incoming.playerId);
                if (existing != null)
                {
                    existing.totalScore        = incoming.totalScore;
                    existing.formationScore    = incoming.formationScore;
                    existing.distanceFlownKm   = incoming.distanceFlownKm;
                    existing.objectivesCompleted = incoming.objectivesCompleted;
                    existing.flightTime        = incoming.flightTime;
                    existing.missionsCompleted = incoming.missionsCompleted;
                    existing.connectionQuality = incoming.connectionQuality;
                    existing.rttMs             = incoming.rttMs;
                }
                else
                {
                    _stats.Add(incoming);
                }
            }

            RefreshUI();
        }

        /// <summary>
        /// Shows the live scoreboard panel.
        /// </summary>
        public void ShowScoreboard()
        {
            if (scoreboardPanel != null) scoreboardPanel.gameObject.SetActive(true);
            RefreshUI();
        }

        /// <summary>
        /// Hides the live scoreboard panel.
        /// </summary>
        public void HideScoreboard()
        {
            if (scoreboardPanel != null) scoreboardPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sorts the scoreboard by the specified column.
        /// </summary>
        /// <param name="columnName">Column name: "Score", "Formation", "Distance", or "Objectives".</param>
        public void SortBy(string columnName)
        {
            _currentSortMode = columnName switch
            {
                "Formation"  => SortMode.Formation,
                "Distance"   => SortMode.Distance,
                "Objectives" => SortMode.Objectives,
                _            => SortMode.Score
            };
            RefreshUI();
        }

        /// <summary>
        /// Ends the session, computes awards, builds the summary, and shows the summary panel.
        /// </summary>
        public void EndSession()
        {
            float sessionDuration = Time.time - _sessionStartTime;
            var summary = BuildSummary(sessionDuration);

            if (summaryPanel != null) summaryPanel.gameObject.SetActive(true);
            PopulateSummaryUI(summary);

            Debug.Log($"[SWEF][MultiplayerScoreboard] Session ended. Duration: {sessionDuration:F0}s. MVP: {summary.mvpPlayerId}.");
            OnSessionSummaryReady?.Invoke(summary);
        }

        // ── Stats broadcast ───────────────────────────────────────────────────────

        private void BroadcastStats()
        {
            // In production: serialise _stats and send via NetworkTransport.
            Debug.Log($"[SWEF][MultiplayerScoreboard] Broadcasting stats for {_stats.Count} players.");
        }

        // ── Awards & Summary ──────────────────────────────────────────────────────

        private SessionSummary BuildSummary(float sessionDuration)
        {
            var sorted = new List<PlayerSessionStats>(_stats);
            sorted.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));

            // Assign awards.
            AssignAward(sorted, s => s.totalScore,        SessionAward.MVP);
            AssignAward(sorted, s => s.formationScore,    SessionAward.BestWingman);
            AssignAward(sorted, s => s.objectivesCompleted, SessionAward.MostObjectives);
            AssignAward(sorted, s => s.flightTime,        SessionAward.IronPilot);
            AssignAward(sorted, s => s.missionsCompleted, SessionAward.SharpShooter);

            string mvpId = sorted.Count > 0 ? sorted[0].playerId : string.Empty;
            string bestWingmanId = _stats
                .OrderByDescending(s => s.formationScore)
                .FirstOrDefault()?.playerId ?? string.Empty;

            return new SessionSummary
            {
                rankings           = sorted,
                mvpPlayerId        = mvpId,
                bestWingmanPlayerId = bestWingmanId,
                sessionDurationSec = sessionDuration
            };
        }

        private void AssignAward<TKey>(List<PlayerSessionStats> sorted, Func<PlayerSessionStats, TKey> selector, SessionAward award)
            where TKey : IComparable<TKey>
        {
            var winner = sorted
                .OrderByDescending(selector)
                .FirstOrDefault();

            if (winner == null) return;
            if (!winner.awards.Contains(award))
            {
                winner.awards.Add(award);
                OnAwardGranted?.Invoke(winner.playerId, award);
                Debug.Log($"[SWEF][MultiplayerScoreboard] Award '{award}' → {winner.displayName}.");
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        private void RefreshUI()
        {
            if (rowPrefab == null || contentParent == null) return;

            // Destroy old rows.
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();

            var sorted = new List<PlayerSessionStats>(_stats);
            switch (_currentSortMode)
            {
                case SortMode.Formation:  sorted.Sort((a, b) => b.formationScore.CompareTo(a.formationScore));  break;
                case SortMode.Distance:   sorted.Sort((a, b) => b.distanceFlownKm.CompareTo(a.distanceFlownKm)); break;
                case SortMode.Objectives: sorted.Sort((a, b) => b.objectivesCompleted.CompareTo(a.objectivesCompleted)); break;
                default:                  sorted.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));           break;
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                var stat = sorted[i];
                var row  = Instantiate(rowPrefab, contentParent);
                row.name = $"ScoreRow_{stat.playerId}";
                _rows.Add(row);

                var texts = row.GetComponentsInChildren<Text>();
                if (texts.Length >= 7)
                {
                    texts[0].text = (i + 1).ToString();                             // Rank
                    texts[1].text = stat.displayName;                               // Name
                    texts[2].text = stat.totalScore.ToString("F0");                 // Score
                    texts[3].text = $"{stat.formationScore:F0} %";                  // Formation %
                    texts[4].text = stat.objectivesCompleted.ToString();            // Objectives
                    texts[5].text = $"{stat.distanceFlownKm:F1} km";               // Distance
                    texts[6].text = $"{stat.rttMs:F0} ms";                         // Ping
                    texts[6].color = QualityToColour(stat.connectionQuality);
                }
            }
        }

        private void PopulateSummaryUI(SessionSummary summary)
        {
            var mvp        = summary.rankings.FirstOrDefault(s => s.playerId == summary.mvpPlayerId);
            var wingman    = summary.rankings.FirstOrDefault(s => s.playerId == summary.bestWingmanPlayerId);
            var ironPilot  = summary.rankings.OrderByDescending(s => s.flightTime).FirstOrDefault();

            if (mvpNameText       != null) mvpNameText.text       = mvp?.displayName        ?? "-";
            if (bestWingmanText   != null) bestWingmanText.text   = wingman?.displayName     ?? "-";
            if (ironPilotText     != null) ironPilotText.text     = ironPilot?.displayName   ?? "-";
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleFormationScoreUpdated(string playerId, float score)
        {
            var stat = GetOrCreateStats(playerId);
            // Rolling average (EMA): new value weighted at α = 0.1, old value at 1 − α = 0.9.
            stat.formationScore = stat.formationScore * 0.9f + score * 0.1f;
        }

        private void HandleObjectiveCompleted(string objectiveId)
        {
            // Award score to all mission participants.
            foreach (var stat in _stats)
            {
                stat.objectivesCompleted++;
                AddScore(stat.playerId, 100f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private PlayerSessionStats GetOrCreateStats(string playerId)
        {
            var stat = _stats.FirstOrDefault(s => s.playerId == playerId);
            if (stat == null)
            {
                stat = new PlayerSessionStats { playerId = playerId, displayName = playerId };
                _stats.Add(stat);
            }
            return stat;
        }

        private Color QualityToColour(ConnectionQuality q) => q switch
        {
            ConnectionQuality.Excellent => Color.green,
            ConnectionQuality.Good      => new Color(0.6f, 1f, 0.2f),
            ConnectionQuality.Fair      => Color.yellow,
            _                           => Color.red
        };
    }
}
