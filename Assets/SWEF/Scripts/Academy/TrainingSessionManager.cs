// TrainingSessionManager.cs — SWEF Flight Academy & Certification System (Phase 104)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Academy
{
    /// <summary>Current phase of an active training session.</summary>
    public enum SessionPhase
    {
        /// <summary>No session is running.</summary>
        Idle,
        /// <summary>Player is reading lesson introduction / briefing.</summary>
        Briefing,
        /// <summary>Player is answering theory questions.</summary>
        TheoryQuiz,
        /// <summary>Player is performing an in-flight exercise.</summary>
        PracticalFlight,
        /// <summary>Session results are being displayed.</summary>
        Debrief,
        /// <summary>Session completed successfully.</summary>
        Completed,
        /// <summary>Session aborted by the player or system.</summary>
        Aborted
    }

    /// <summary>
    /// Snapshot of the player's performance at the end of a session.
    /// </summary>
    [Serializable]
    public class SessionResult
    {
        public string         curriculumId;
        public string         lessonId;
        public SessionPhase   finalPhase;
        public float          theoryScore;
        public float          practicalScore;
        public bool           theoryPassed;
        public bool           practicalPassed;
        public bool           lessonCompleted;
        public string         completedAtUtc;
        public int            xpAwarded;
    }

    /// <summary>
    /// Manages the lifecycle of an active training session: phase transitions,
    /// theory scoring, practical objective tracking, and debrief generation.
    /// Intended to be used by <see cref="FlightAcademyManager"/>.
    /// </summary>
    public class TrainingSessionManager
    {
        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Raised when the session phase changes.</summary>
        public event Action<SessionPhase> OnPhaseChanged;

        /// <summary>Raised when the session concludes (pass or fail).</summary>
        public event Action<SessionResult> OnSessionCompleted;

        // ── State ──────────────────────────────────────────────────────────────
        private SessionPhase _phase = SessionPhase.Idle;
        private FlightLesson _activeLesson;
        private string       _curriculumId;

        private List<int>              _theoryAnswers   = new List<int>();
        private Dictionary<string,int> _practicalPoints = new Dictionary<string, int>();
        private float                  _sessionStartTime;

        // ── Properties ─────────────────────────────────────────────────────────
        /// <summary>Current phase of the running session.</summary>
        public SessionPhase Phase => _phase;

        /// <summary>Lesson currently being trained (null when idle).</summary>
        public FlightLesson ActiveLesson => _activeLesson;

        // ── Session control ────────────────────────────────────────────────────

        /// <summary>Begins a new training session for the given lesson inside a curriculum.</summary>
        public void StartSession(string curriculumId, FlightLesson lesson)
        {
            if (lesson == null) throw new ArgumentNullException(nameof(lesson));
            _curriculumId     = curriculumId;
            _activeLesson     = lesson;
            _theoryAnswers.Clear();
            _practicalPoints.Clear();
            _sessionStartTime = Time.realtimeSinceStartup;
            SetPhase(SessionPhase.Briefing);
        }

        /// <summary>Advances from Briefing to the appropriate first active phase.</summary>
        public void BeginLesson()
        {
            if (_phase != SessionPhase.Briefing) return;

            if (_activeLesson.theoryModule != null)
                SetPhase(SessionPhase.TheoryQuiz);
            else if (_activeLesson.practicalExercise != null)
                SetPhase(SessionPhase.PracticalFlight);
            else
                FinishSession();     // lesson has no content
        }

        /// <summary>Aborts the running session.</summary>
        public void AbortSession()
        {
            SetPhase(SessionPhase.Aborted);
            _activeLesson = null;
        }

        // ── Theory handling ────────────────────────────────────────────────────

        /// <summary>
        /// Submits the player's answers for the theory quiz.
        /// Call when the player has answered all questions.
        /// </summary>
        public void SubmitTheoryAnswers(IList<int> answers)
        {
            if (_phase != SessionPhase.TheoryQuiz) return;
            _theoryAnswers.Clear();
            if (answers != null)
                _theoryAnswers.AddRange(answers);

            // Move on to practical (or finish if no practical)
            if (_activeLesson.practicalExercise != null)
                SetPhase(SessionPhase.PracticalFlight);
            else
                FinishSession();
        }

        // ── Practical handling ─────────────────────────────────────────────────

        /// <summary>
        /// Records earned points for a specific objective during the practical phase.
        /// Can be called multiple times as objectives are completed.
        /// </summary>
        public void RecordObjectivePoints(string objectiveTag, int points)
        {
            if (_phase != SessionPhase.PracticalFlight) return;
            _practicalPoints[objectiveTag] = points;
        }

        /// <summary>
        /// Signals that the practical exercise has ended (success or timeout).
        /// Triggers debrief generation.
        /// </summary>
        public void FinishPractical()
        {
            if (_phase != SessionPhase.PracticalFlight) return;
            FinishSession();
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void FinishSession()
        {
            SetPhase(SessionPhase.Debrief);

            float theoryScore    = 0f;
            float practicalScore = 0f;
            bool  theoryPassed   = true;
            bool  practicalPassed = true;

            if (_activeLesson.theoryModule != null)
            {
                theoryScore  = _activeLesson.theoryModule.CalculateScore(_theoryAnswers);
                theoryPassed = _activeLesson.theoryModule.IsPassing(theoryScore);
            }

            if (_activeLesson.practicalExercise != null)
            {
                practicalScore  = _activeLesson.practicalExercise.CalculateScore(_practicalPoints);
                practicalPassed = _activeLesson.practicalExercise.IsPassing(practicalScore);
            }

            bool lessonCompleted = theoryPassed && practicalPassed;
            int  xpAwarded       = lessonCompleted ? _activeLesson.xpReward : 0;

            var result = new SessionResult
            {
                curriculumId     = _curriculumId,
                lessonId         = _activeLesson.lessonId,
                finalPhase       = lessonCompleted ? SessionPhase.Completed : SessionPhase.Debrief,
                theoryScore      = theoryScore,
                practicalScore   = practicalScore,
                theoryPassed     = theoryPassed,
                practicalPassed  = practicalPassed,
                lessonCompleted  = lessonCompleted,
                completedAtUtc   = DateTime.UtcNow.ToString("o"),
                xpAwarded        = xpAwarded
            };

            SetPhase(lessonCompleted ? SessionPhase.Completed : SessionPhase.Debrief);
            OnSessionCompleted?.Invoke(result);
            _activeLesson = null;
        }

        private void SetPhase(SessionPhase phase)
        {
            _phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
