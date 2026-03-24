// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Airshow
{
    /// <summary>
    /// Central singleton that orchestrates the full airshow lifecycle.
    /// Manages state transitions, performer coordination, real-time scoring,
    /// and persistence of best scores.
    /// Persists across scenes via <c>DontDestroyOnLoad</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class AirshowManager : MonoBehaviour
    {
        #region Singleton
        /// <summary>Shared airshow manager instance.</summary>
        public static AirshowManager Instance { get; private set; }

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

        #region Public State
        /// <summary>Current state of the airshow state machine.</summary>
        public AirshowState CurrentState { get; private set; } = AirshowState.Idle;

        /// <summary>The routine currently being performed.</summary>
        public AirshowRoutineData ActiveRoutine { get; private set; }

        /// <summary>All registered performers for the current show.</summary>
        public List<AirshowPerformer> Performers { get; } = new List<AirshowPerformer>();

        /// <summary>Index of the act currently being performed.</summary>
        public int CurrentActIndex { get; private set; }

        /// <summary>Elapsed time within the current act in seconds.</summary>
        public float ActElapsedTime { get; private set; }

        /// <summary>Runtime configuration for the current show.</summary>
        public AirshowConfig Config { get; private set; } = new AirshowConfig();
        #endregion

        #region Events
        /// <summary>Fired whenever the airshow state machine changes state.</summary>
        public event Action<AirshowState> OnAirshowStateChanged;

        /// <summary>Fired when a new act begins.</summary>
        public event Action<int, string> OnActStarted;

        /// <summary>Fired when a maneuver is triggered for a specific performer slot.</summary>
        public event Action<ManeuverType, int> OnManeuverTriggered;

        /// <summary>Fired when a live performance score is updated.</summary>
        public event Action<float, PerformanceRating> OnPerformanceScored;

        /// <summary>Fired when the entire show is completed with the final result.</summary>
        public event Action<AirshowResult> OnAirshowCompleted;
        #endregion

        #region Private
        private float _countdownTimer;
        private float _showElapsedTime;
        private Coroutine _stateCoroutine;
        private readonly List<ManeuverScore> _maneuverScores = new List<ManeuverScore>();

        // Scoring accumulators per act
        private float _actTimingSum;
        private float _actPositionSum;
        private float _actSmoothnessSum;
        private int _actScoreCount;

        private const string PlayerPrefsPrefix = "SWEF_Airshow_";
        #endregion

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Update()
        {
            if (CurrentState != AirshowState.Performing && CurrentState != AirshowState.Finale)
                return;

            ActElapsedTime += Time.deltaTime;
            _showElapsedTime += Time.deltaTime;

            if (_showElapsedTime >= Config.maxShowDuration)
            {
                AbortAirshow("Max show duration exceeded");
                return;
            }

            DistributeManeuverSteps();
            CollectScores();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Validates performers and begins the airshow in Briefing state.
        /// </summary>
        /// <param name="routine">The routine to perform.</param>
        public void StartAirshow(AirshowRoutineData routine)
        {
            if (CurrentState != AirshowState.Idle)
            {
                Debug.LogWarning("[AirshowManager] Cannot start — show already in progress.");
                return;
            }

            if (routine == null)
            {
                Debug.LogError("[AirshowManager] Routine is null.");
                return;
            }

            if (Performers.Count < routine.requiredPerformers)
            {
                Debug.LogWarning($"[AirshowManager] Need {routine.requiredPerformers} performers, have {Performers.Count}.");
                return;
            }

            ActiveRoutine = routine;
            CurrentActIndex = 0;
            ActElapsedTime = 0f;
            _showElapsedTime = 0f;
            _maneuverScores.Clear();

            SetState(AirshowState.Briefing);
        }

        /// <summary>Begins the visual 3-2-1 countdown sequence.</summary>
        public void BeginCountdown()
        {
            if (CurrentState != AirshowState.Briefing) return;
            if (_stateCoroutine != null) StopCoroutine(_stateCoroutine);
            _stateCoroutine = StartCoroutine(CountdownCoroutine());
        }

        /// <summary>Starts the first act and enters Performing state.</summary>
        public void BeginPerformance()
        {
            if (CurrentState != AirshowState.Countdown) return;
            CurrentActIndex = 0;
            ActElapsedTime = 0f;
            ResetActScoreAccumulators();
            SetState(AirshowState.Performing);
            FireActStarted();
        }

        /// <summary>Completes the current act and enters Intermission before the next.</summary>
        public void AdvanceToNextAct()
        {
            if (CurrentState != AirshowState.Performing) return;
            if (_stateCoroutine != null) StopCoroutine(_stateCoroutine);
            _stateCoroutine = StartCoroutine(IntermissionCoroutine());
        }

        /// <summary>Triggers the finale sequence (final act with special scoring).</summary>
        public void TriggerFinale()
        {
            if (CurrentState != AirshowState.Performing) return;
            SetState(AirshowState.Finale);
            FireActStarted();
        }

        /// <summary>Calculates final scores, fires OnAirshowCompleted, and persists best score.</summary>
        public void CompleteAirshow()
        {
            if (CurrentState != AirshowState.Performing && CurrentState != AirshowState.Finale)
                return;

            AirshowResult result = AirshowScoreCalculator.BuildResult(
                ActiveRoutine, _maneuverScores, _showElapsedTime);

            SetState(AirshowState.Completed);
            OnPerformanceScored?.Invoke(result.totalScore,
                AirshowScoreCalculator.GetRating(result.totalScore));
            OnAirshowCompleted?.Invoke(result);

            PersistBestScore(result);
        }

        /// <summary>Immediately aborts the airshow for emergency reasons.</summary>
        /// <param name="reason">Human-readable abort reason for logging.</param>
        public void AbortAirshow(string reason)
        {
            Debug.LogWarning($"[AirshowManager] Aborted: {reason}");
            if (_stateCoroutine != null) StopCoroutine(_stateCoroutine);
            SetState(AirshowState.Aborted);
            ActiveRoutine = null;
        }

        /// <summary>Registers a performer for the next show.</summary>
        public void RegisterPerformer(AirshowPerformer performer)
        {
            if (!Performers.Contains(performer))
                Performers.Add(performer);
        }

        /// <summary>Unregisters a performer.</summary>
        public void UnregisterPerformer(AirshowPerformer performer)
        {
            Performers.Remove(performer);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void SetState(AirshowState state)
        {
            CurrentState = state;
            OnAirshowStateChanged?.Invoke(state);
        }

        private void FireActStarted()
        {
            if (ActiveRoutine == null || ActiveRoutine.acts == null) return;
            if (CurrentActIndex < 0 || CurrentActIndex >= ActiveRoutine.acts.Count) return;
            string actName = ActiveRoutine.acts[CurrentActIndex].actName;
            OnActStarted?.Invoke(CurrentActIndex, actName);
        }

        private void DistributeManeuverSteps()
        {
            if (ActiveRoutine == null || ActiveRoutine.acts == null) return;
            if (CurrentActIndex >= ActiveRoutine.acts.Count) return;

            ManeuverSequence act = ActiveRoutine.acts[CurrentActIndex];
            foreach (ManeuverStep step in act.steps)
            {
                // Trigger the maneuver when its start window begins
                if (ActElapsedTime >= step.startTimeOffset &&
                    ActElapsedTime < step.startTimeOffset + Time.deltaTime)
                {
                    AirshowPerformer performer = FindPerformer(step.assignedSlot);
                    if (performer != null)
                    {
                        performer.ExecuteManeuver(step);
                        OnManeuverTriggered?.Invoke(step.type, step.assignedSlot);
                    }
                }
            }

            // Advance act when all steps are complete
            float actDuration = GetActDuration(act);
            if (ActElapsedTime >= actDuration)
            {
                int nextAct = CurrentActIndex + 1;
                if (nextAct >= ActiveRoutine.acts.Count)
                    CompleteAirshow();
                else if (nextAct == ActiveRoutine.acts.Count - 1)
                    TriggerFinale();
                else
                    AdvanceToNextAct();
            }
        }

        private void CollectScores()
        {
            if (ActiveRoutine == null || ActiveRoutine.acts == null) return;
            if (CurrentActIndex >= ActiveRoutine.acts.Count) return;

            ManeuverSequence act = ActiveRoutine.acts[CurrentActIndex];
            foreach (ManeuverStep step in act.steps)
            {
                AirshowPerformer performer = FindPerformer(step.assignedSlot);
                if (performer == null) continue;

                float t = performer.TimingScore;
                float p = performer.PositionScore;
                float s = performer.SmoothnessScore;

                if (t > 0f || p > 0f || s > 0f)
                {
                    _actTimingSum += t;
                    _actPositionSum += p;
                    _actSmoothnessSum += s;
                    _actScoreCount++;
                }
            }
        }

        private AirshowPerformer FindPerformer(int slot)
        {
            foreach (AirshowPerformer p in Performers)
                if (p.SlotIndex == slot) return p;
            return null;
        }

        private static float GetActDuration(ManeuverSequence act)
        {
            float maxEnd = 0f;
            foreach (ManeuverStep step in act.steps)
            {
                float end = step.startTimeOffset + step.duration;
                if (end > maxEnd) maxEnd = end;
            }
            return maxEnd > 0f ? maxEnd : 30f;
        }

        private void ResetActScoreAccumulators()
        {
            _actTimingSum = 0f;
            _actPositionSum = 0f;
            _actSmoothnessSum = 0f;
            _actScoreCount = 0;
        }

        private void PersistBestScore(AirshowResult result)
        {
            if (ActiveRoutine == null) return;
            string key = PlayerPrefsPrefix + ActiveRoutine.routineId;
            float existing = PlayerPrefs.GetFloat(key, 0f);
            if (result.totalScore > existing)
            {
                PlayerPrefs.SetFloat(key, result.totalScore);
                PlayerPrefs.Save();
            }
        }

        // ── Coroutines ───────────────────────────────────────────────────────

        private IEnumerator CountdownCoroutine()
        {
            SetState(AirshowState.Countdown);
            yield return new WaitForSeconds(Config.countdownDuration);
            BeginPerformance();
        }

        private IEnumerator IntermissionCoroutine()
        {
            SetState(AirshowState.Intermission);
            ResetActScoreAccumulators();
            yield return new WaitForSeconds(Config.intermissionDuration);
            CurrentActIndex++;
            ActElapsedTime = 0f;
            SetState(AirshowState.Performing);
            FireActStarted();
        }
    }
}
