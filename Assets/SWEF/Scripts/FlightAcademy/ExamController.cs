using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Optional integration compile guards
#if SWEF_FLIGHT_AVAILABLE
using SWEF.Flight;
#endif
#if SWEF_LANDING_AVAILABLE
using SWEF.Landing;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Runs an active exam session for a <see cref="TrainingModule"/>.
    /// Monitors objective progress in real time, applies penalties and bonuses,
    /// then produces a final <see cref="ExamResult"/> via <see cref="ExamScoringEngine"/>.
    /// </summary>
    public class ExamController : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired each frame with the current estimated score 0–100.</summary>
        public event Action<float> OnScoreUpdated;

        /// <summary>Fired when a penalty is applied. Parameters: reason, amount.</summary>
        public event Action<string, float> OnPenaltyApplied;

        /// <summary>Fired when a bonus is applied. Parameters: reason, amount.</summary>
        public event Action<string, float> OnBonusApplied;

        /// <summary>Fired when the exam ends (pass or abort).</summary>
        public event Action<ExamResult> OnExamEnded;

        // ── State ─────────────────────────────────────────────────────────────────
        private TrainingModule _module;
        private List<ObjectiveScore> _objectiveScores = new List<ObjectiveScore>();
        private float _elapsed;
        private float _penaltyPoints;
        private float _bonusPoints;
        private bool _paused;
        private bool _running;
        private Coroutine _timerCoroutine;

        // Landing metrics collected during exam
        private float _touchdownSpeed;
        private float _centerlineDeviation;
        private float _descentRate;
        private float _gForce;
        private bool _landingRecorded;

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Initialises and starts the exam for <paramref name="module"/>.</summary>
        public void StartExam(TrainingModule module)
        {
            if (module == null || _running) return;
            _module = module;
            _elapsed = 0f;
            _penaltyPoints = 0f;
            _bonusPoints = 0f;
            _paused = false;
            _running = true;
            _landingRecorded = false;

            _objectiveScores.Clear();
            if (module.objectives != null)
            {
                foreach (var obj in module.objectives)
                    _objectiveScores.Add(new ObjectiveScore { objectiveType = obj.objectiveType });
            }

            if (module.timeLimit > 0f)
                _timerCoroutine = StartCoroutine(TimerCoroutine(module.timeLimit));
        }

        /// <summary>Pauses the exam timer and monitoring.</summary>
        public void PauseExam()
        {
            if (_running) _paused = true;
        }

        /// <summary>Resumes a paused exam.</summary>
        public void ResumeExam()
        {
            if (_running) _paused = false;
        }

        /// <summary>Aborts the exam without recording a result.</summary>
        public void AbortExam()
        {
            if (!_running) return;
            StopTimer();
            _running = false;
            var result = BuildResult();
            result.passed = false;
            OnExamEnded?.Invoke(result);
        }

        /// <summary>Manually completes the exam (all objectives done).</summary>
        public void FinishExam()
        {
            if (!_running) return;
            StopTimer();
            _running = false;
            var result = BuildResult();
            OnExamEnded?.Invoke(result);

            if (FlightAcademyManager.Instance != null)
                FlightAcademyManager.Instance.CompleteExam(result);
        }

        // ── Penalty / Bonus API ───────────────────────────────────────────────────

        /// <summary>Applies a score penalty (e.g., hard landing, stall, overspeed).</summary>
        public void ApplyPenalty(string reason, float amount)
        {
            if (!_running || _paused || amount <= 0f) return;
            _penaltyPoints += amount;
            OnPenaltyApplied?.Invoke(reason, amount);
        }

        /// <summary>Applies a score bonus (e.g., smooth landing, fuel efficiency).</summary>
        public void ApplyBonus(string reason, float amount)
        {
            if (!_running || _paused || amount <= 0f) return;
            _bonusPoints += amount;
            OnBonusApplied?.Invoke(reason, amount);
        }

        // ── Landing Metrics ───────────────────────────────────────────────────────

        /// <summary>
        /// Records landing metrics for a landing exam.
        /// Call from the landing detection system on touchdown.
        /// </summary>
        public void RecordLanding(float touchdownSpeedKnots, float centerlineDeviationMeters,
                                  float descentRateFpm, float gForce)
        {
            _touchdownSpeed = touchdownSpeedKnots;
            _centerlineDeviation = centerlineDeviationMeters;
            _descentRate = descentRateFpm;
            _gForce = gForce;
            _landingRecorded = true;

            if (_module != null && _module.examType == ExamType.Landing)
            {
                float landingScore = ExamScoringEngine.CalculateLandingScore(
                    touchdownSpeedKnots, centerlineDeviationMeters, descentRateFpm, gForce);
                UpdateObjectiveScore("landing_touchdown", landingScore, true);
            }
        }

        // ── Update ────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_running || _paused) return;
            _elapsed += Time.deltaTime;

            float estimate = ExamScoringEngine.CalculateScore(_objectiveScores, _penaltyPoints, _bonusPoints);
            OnScoreUpdated?.Invoke(estimate);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private void UpdateObjectiveScore(string type, float score, bool completed)
        {
            var existing = _objectiveScores.Find(o => o.objectiveType == type);
            if (existing != null)
            {
                existing.score = score;
                existing.completed = completed;
            }
            else
            {
                _objectiveScores.Add(new ObjectiveScore
                {
                    objectiveType = type,
                    score = score,
                    completed = completed
                });
            }
        }

        private ExamResult BuildResult()
        {
            float score = ExamScoringEngine.CalculateScore(_objectiveScores, _penaltyPoints, _bonusPoints);
            float threshold = ExamScoringEngine.GetPassingThreshold(_module != null ? _module.examDifficulty : ExamDifficulty.Bronze);
            return new ExamResult
            {
                score = score,
                grade = ExamScoringEngine.GetLetterGrade(score),
                passed = ExamScoringEngine.GetPassStatus(score, threshold),
                objectiveScores = new List<ObjectiveScore>(_objectiveScores),
                totalTime = _elapsed,
                penaltyPoints = _penaltyPoints,
                bonusPoints = _bonusPoints,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private IEnumerator TimerCoroutine(float limit)
        {
            float remaining = limit;
            while (remaining > 0f && _running)
            {
                if (!_paused)
                    remaining -= Time.deltaTime;
                yield return null;
            }
            if (_running)
                FinishExam();
        }

        private void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void OnDestroy() => StopTimer();
    }
}
