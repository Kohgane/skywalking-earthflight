// RaceManager.cs — SWEF Competitive Racing & Time Trial System (Phase 88)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if SWEF_LEADERBOARD_AVAILABLE
using SWEF.Leaderboard;
#endif

#if SWEF_ACHIEVEMENT_AVAILABLE
using SWEF.Achievement;
#endif

namespace SWEF.CompetitiveRacing
{
    /// <summary>
    /// Phase 88 — Singleton MonoBehaviour that drives the full race session lifecycle:
    /// countdown, per-frame elapsed timing, checkpoint detection, lap management,
    /// wrong-way detection, elimination-mode coroutine, and final result submission.
    ///
    /// <para>Attach to a persistent scene object — uses DontDestroyOnLoad.</para>
    /// </summary>
    public sealed class RaceManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RaceManager Instance { get; private set; }

        #endregion

        #region Public State

        /// <summary>Course currently loaded for racing.</summary>
        public RaceCourse activeCourse { get; private set; }

        /// <summary>Race mode for the current session.</summary>
        public RaceMode activeMode { get; private set; }

        /// <summary>Current lifecycle state of the race.</summary>
        public RaceStatus status { get; private set; } = RaceStatus.Setup;

        /// <summary>Elapsed race time in seconds (only increments while <see cref="RaceStatus.Racing"/>).</summary>
        public float elapsedTime { get; private set; }

        /// <summary>Current lap number (1-based; circuit mode only).</summary>
        public int currentLap { get; private set; }

        /// <summary>Index of the next checkpoint the player must pass through.</summary>
        public int currentCheckpointIndex { get; private set; }

        /// <summary>Result being accumulated during the current race session.</summary>
        public RaceResult currentResult { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when a race begins (after countdown).</summary>
        public event Action<RaceCourse>        OnRaceStarted;

        /// <summary>Fired each time a checkpoint is captured.</summary>
        public event Action<RaceCheckpoint, float> OnCheckpointCaptured;

        /// <summary>Fired when a lap is completed (circuit mode).</summary>
        public event Action<int>               OnLapCompleted;

        /// <summary>Fired when the race is finished and the result is final.</summary>
        public event Action<RaceResult>        OnRaceFinished;

        /// <summary>Fired when the player sets a new personal best.</summary>
        public event Action<RaceResult>        OnPersonalBest;

        /// <summary>Fired when the player sets a new global course record.</summary>
        public event Action<RaceResult>        OnNewRecord;

        /// <summary>Fired while the player is going the wrong way.</summary>
        public event Action                    OnWrongWay;

        /// <summary>Fired when a player is eliminated in Elimination mode.</summary>
        public event Action<string>            OnEliminated;

        /// <summary>Fired for any <see cref="RaceAlertType"/> during the race.</summary>
        public event Action<RaceAlertType>     OnRaceAlert;

        #endregion

        #region Inspector

        [Header("Player Reference")]
        [Tooltip("Player aircraft FlightController. Auto-found if null.")]
        [SerializeField] private Flight.FlightController _playerFlight;

        [Header("Wrong-Way")]
        [Tooltip("Seconds between wrong-way event fires to avoid spam.")]
        [SerializeField] [Min(1f)] private float _wrongWayFireInterval = 3f;

        [Header("Countdown")]
        [Tooltip("Countdown duration override. Uses CompetitiveRacingConfig default if 0.")]
        [SerializeField] [Min(0f)] private float _countdownOverride = 0f;

        #endregion

        #region Private State

        private Coroutine _countdownCoroutine;
        private Coroutine _eliminationCoroutine;
        private float     _wrongWayTimer;
        private bool      _raceRecorderStarted;

        // Saved personal-best times keyed by courseId
        private readonly Dictionary<string, float> _personalBests = new Dictionary<string, float>();

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
            Debug.Log("[SWEF] RaceManager: initialised.");
        }

        private void Start()
        {
            if (_playerFlight == null)
                _playerFlight = FindFirstObjectByType<Flight.FlightController>();
        }

        private void Update()
        {
            if (status != RaceStatus.Racing) return;

            elapsedTime += Time.deltaTime;

            CheckWrongWay();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Loads <paramref name="course"/> and starts the countdown sequence, then the race.
        /// </summary>
        public void StartRace(RaceCourse course, RaceMode mode)
        {
            if (course == null)
            {
                Debug.LogWarning("[SWEF] RaceManager: StartRace called with null course.");
                return;
            }

            activeCourse           = course;
            activeMode             = mode;
            elapsedTime            = 0f;
            currentLap             = 1;
            currentCheckpointIndex = 0;
            status                 = RaceStatus.Countdown;

            currentResult = new RaceResult
            {
                resultId   = Guid.NewGuid().ToString(),
                courseId   = course.courseId,
                mode       = mode,
                raceDate   = DateTime.UtcNow,
                lapCount   = 1
            };

            // Auto-start flight recorder for ghost replay
            TryStartRecorder();

            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());

            CompetitiveRacingAnalytics.RecordRaceStart(course.courseId, mode);
        }

        /// <summary>Pauses an active race (timer stops).</summary>
        public void PauseRace()
        {
            if (status != RaceStatus.Racing) return;
            status = RaceStatus.Paused;
            Debug.Log("[SWEF] RaceManager: Race paused.");
        }

        /// <summary>Resumes a paused race.</summary>
        public void ResumeRace()
        {
            if (status != RaceStatus.Paused) return;
            status = RaceStatus.Racing;
            Debug.Log("[SWEF] RaceManager: Race resumed.");
        }

        /// <summary>Abandons the current race without submitting a result.</summary>
        public void AbandonRace()
        {
            if (status == RaceStatus.Setup || status == RaceStatus.Finished) return;
            status = RaceStatus.Abandoned;
            StopAllCoroutines();
            Debug.Log("[SWEF] RaceManager: Race abandoned.");
            OnRaceAlert?.Invoke(RaceAlertType.RaceFinished);
        }

        /// <summary>
        /// Records the capture of checkpoint at <paramref name="index"/> and
        /// advances the next-checkpoint pointer.
        /// </summary>
        public void CaptureCheckpoint(int index)
        {
            if (status != RaceStatus.Racing) return;
            if (activeCourse == null) return;
            if (index != currentCheckpointIndex) return;
            if (index >= activeCourse.checkpoints.Count) return;

            var cp = activeCourse.checkpoints[index];

            // Anti-cheat: ignore impossibly fast times
            if (elapsedTime < CompetitiveRacingConfig.MinimumLapTimeAntiCheat && index > 0)
            {
                Debug.LogWarning($"[SWEF] RaceManager: Checkpoint {index} captured suspiciously fast — ignored.");
                return;
            }

            float splitTime = elapsedTime;
            float prevTime  = currentResult.splits.Count > 0
                                  ? currentResult.splits[currentResult.splits.Count - 1].elapsedTime
                                  : 0f;

            var split = new CheckpointSplit
            {
                checkpointIndex = index,
                elapsedTime     = splitTime,
                splitTime       = splitTime - prevTime
            };

            // Apply bonus/penalty seconds
            if (!Mathf.Approximately(cp.bonusSeconds, 0f))
                elapsedTime -= cp.bonusSeconds; // negative bonus = penalty

            currentResult.splits.Add(split);

            OnCheckpointCaptured?.Invoke(cp, splitTime);

            if (cp.type == CheckpointType.Bonus)
                OnRaceAlert?.Invoke(RaceAlertType.BonusCheckpoint);

            // Advance pointer
            currentCheckpointIndex++;

            // Check for lap / finish
            if (currentCheckpointIndex >= activeCourse.checkpoints.Count)
            {
                if (activeMode == RaceMode.Circuit && currentLap < activeCourse.lapCount)
                    CompleteLap();
                else
                    FinishRace();
            }
        }

        /// <summary>Completes the current lap in circuit mode and resets checkpoint index.</summary>
        public void CompleteLap()
        {
            currentLap++;
            currentCheckpointIndex = 0;
            currentResult.lapCount = currentLap - 1;
            OnLapCompleted?.Invoke(currentLap - 1);
            OnRaceAlert?.Invoke(RaceAlertType.LapComplete);
            Debug.Log($"[SWEF] RaceManager: Lap {currentLap - 1} complete.");
        }

        /// <summary>Finalises the race result and submits it to the leaderboard.</summary>
        public void FinishRace()
        {
            if (status != RaceStatus.Racing) return;
            status = RaceStatus.Finished;

            currentResult.totalTime = elapsedTime;
            // lapCount reflects the number of laps actually completed
            currentResult.lapCount = activeMode == RaceMode.Circuit
                ? activeCourse.lapCount
                : 1;

            // Personal best check
            bool isPB = false;
            if (!_personalBests.TryGetValue(activeCourse.courseId, out float prev) || elapsedTime < prev)
            {
                _personalBests[activeCourse.courseId] = elapsedTime;
                currentResult.isPersonalBest = true;
                isPB = true;
                OnPersonalBest?.Invoke(currentResult);
                OnRaceAlert?.Invoke(RaceAlertType.NewPersonalBest);
#if SWEF_ACHIEVEMENT_AVAILABLE
                AchievementManager.Instance?.TryUnlock("first_race_finish");
                if (elapsedTime <= activeCourse.goldTime)
                    AchievementManager.Instance?.TryUnlock("gold_medal");
#endif
            }

            TryStopRecorder(currentResult.resultId);

            // Submit to leaderboard
            SubmitToLeaderboard(currentResult);

            OnRaceFinished?.Invoke(currentResult);
            OnRaceAlert?.Invoke(RaceAlertType.RaceFinished);

            CompetitiveRacingAnalytics.RecordRaceFinish(activeCourse.courseId, elapsedTime,
                GetMedalLabel(elapsedTime));
            if (isPB)
                CompetitiveRacingAnalytics.RecordPersonalBest(activeCourse.courseId, elapsedTime);

            Debug.Log($"[SWEF] RaceManager: Race finished in {elapsedTime:F2}s.");
        }

        #endregion

        #region Private Helpers

        private IEnumerator CountdownCoroutine()
        {
            float dur = _countdownOverride > 0f
                ? _countdownOverride
                : CompetitiveRacingConfig.DefaultCountdownDuration;

            yield return new WaitForSeconds(dur);

            status = RaceStatus.Racing;

            if (activeMode == RaceMode.Elimination)
            {
                if (_eliminationCoroutine != null) StopCoroutine(_eliminationCoroutine);
                _eliminationCoroutine = StartCoroutine(EliminationCoroutine());
            }

            OnRaceStarted?.Invoke(activeCourse);
            Debug.Log("[SWEF] RaceManager: Race started!");
        }

        private IEnumerator EliminationCoroutine()
        {
            while (status == RaceStatus.Racing)
            {
                yield return new WaitForSeconds(CompetitiveRacingConfig.EliminationInterval);
                if (status != RaceStatus.Racing) break;
                OnEliminated?.Invoke("last_place");
                OnRaceAlert?.Invoke(RaceAlertType.Elimination);
            }
        }

        private void CheckWrongWay()
        {
            if (_playerFlight == null || activeCourse == null) return;
            if (currentCheckpointIndex >= activeCourse.checkpoints.Count) return;

            _wrongWayTimer -= Time.deltaTime;
            if (_wrongWayTimer > 0f) return;

            var cp     = activeCourse.checkpoints[currentCheckpointIndex];
            Vector3 toCheckpoint = new Vector3(
                (float)(cp.longitude * 111320.0 * Math.Cos(cp.latitude * Mathf.Deg2Rad)),
                cp.altitude,
                (float)(cp.latitude * 111320.0)) - _playerFlight.transform.position;

            Vector3 vel = _playerFlight.Velocity;
            if (vel.sqrMagnitude < 0.01f) return;

            float dot = Vector3.Dot(vel.normalized, toCheckpoint.normalized);
            if (dot < CompetitiveRacingConfig.WrongWayDotThreshold)
            {
                OnWrongWay?.Invoke();
                OnRaceAlert?.Invoke(RaceAlertType.WrongWay);
                _wrongWayTimer = _wrongWayFireInterval;
            }
        }

        private void TryStartRecorder()
        {
            var recorder = FindFirstObjectByType<Recorder.FlightRecorder>();
            if (recorder != null && !recorder.IsRecording)
            {
                recorder.StartRecording();
                _raceRecorderStarted = true;
            }
        }

        private void TryStopRecorder(string replayId)
        {
            if (!_raceRecorderStarted) return;
            var recorder = FindFirstObjectByType<Recorder.FlightRecorder>();
            if (recorder != null && recorder.IsRecording)
            {
                recorder.StopRecording();
                currentResult.replayId = replayId;
            }
            _raceRecorderStarted = false;
        }

        private void SubmitToLeaderboard(RaceResult result)
        {
#if SWEF_LEADERBOARD_AVAILABLE
            var entry = new GlobalLeaderboardEntry
            {
                playerId    = result.playerId,
                playerName  = result.playerName,
                score       = result.totalTime,
                categoryId  = result.courseId
            };
            GlobalLeaderboardService.Instance?.SubmitScore(entry, null, null);
#endif
        }

        private string GetMedalLabel(float time)
        {
            if (activeCourse == null) return "none";
            if (time <= activeCourse.goldTime)   return "gold";
            if (time <= activeCourse.silverTime) return "silver";
            if (time <= activeCourse.bronzeTime) return "bronze";
            return "none";
        }

        #endregion
    }
}
