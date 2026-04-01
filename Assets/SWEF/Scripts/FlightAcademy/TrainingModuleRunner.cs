using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Optional integration compile guards
#if SWEF_FLIGHTSCHOOL_AVAILABLE
using SWEF.FlightSchool;
#endif
#if SWEF_GUIDEDTOUR_AVAILABLE
using SWEF.GuidedTour;
#endif

namespace SWEF.FlightAcademy
{
    /// <summary>
    /// Pre-exam training mode: guided walkthrough of exam objectives with
    /// instructor dialogue, ghost demonstration paths, and unlimited practice retries.
    /// </summary>
    public class TrainingModuleRunner : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when training for a module begins.</summary>
        public event System.Action<TrainingModule> OnTrainingStarted;

        /// <summary>Fired when a section replay is triggered.</summary>
        public event System.Action<int> OnSectionReplayed;

        /// <summary>Fired when the player skips to the exam.</summary>
        public event System.Action<TrainingModule> OnSkippedToExam;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [SerializeField] private InstructorDialogueController _instructorDialogue;

        // ── State ─────────────────────────────────────────────────────────────────
        private TrainingModule _module;
        private int _currentSection;
        private bool _trainingActive;
        private int _practiceAttempts;

        // ── Public API ─────────────────────────────────────────────────────────────

        /// <summary>Starts guided training for <paramref name="module"/>.</summary>
        public void StartTraining(TrainingModule module)
        {
            if (module == null) return;
            _module = module;
            _currentSection = 0;
            _practiceAttempts = 0;
            _trainingActive = true;

            OnTrainingStarted?.Invoke(module);
            BeginSection(0);

            // Bridge to existing FlightSchool instructor (null-safe)
#if SWEF_FLIGHTSCHOOL_AVAILABLE
            var instructor = FindObjectOfType<FlightInstructor>();
            instructor?.BeginLesson(module.moduleId);
#endif
        }

        /// <summary>Skips the remaining training walkthrough and begins the exam.</summary>
        public void SkipToExam()
        {
            if (!_trainingActive || _module == null) return;
            StopAllCoroutines();
            _trainingActive = false;
            OnSkippedToExam?.Invoke(_module);

            if (FlightAcademyManager.Instance != null)
                FlightAcademyManager.Instance.StartExam(_module);
        }

        /// <summary>Replays the training section at <paramref name="sectionIndex"/>.</summary>
        public void ReplaySection(int sectionIndex)
        {
            if (!_trainingActive) return;
            _currentSection = sectionIndex;
            OnSectionReplayed?.Invoke(sectionIndex);
            BeginSection(sectionIndex);
        }

        /// <summary>Begins another practice attempt for the current module.</summary>
        public void PracticeAgain()
        {
            _practiceAttempts++;
            BeginSection(0);
        }

        // ── Private helpers ───────────────────────────────────────────────────────
        private void BeginSection(int index)
        {
            if (_module == null || _module.objectives == null) return;
            if (index >= _module.objectives.Count) return;

            var objective = _module.objectives[index];
            _instructorDialogue?.QueueDialogue(
                $"training_{_module.moduleId}_section_{index}",
                InstructorDialoguePriority.Normal);

            // Bridge to GuidedTour waypoint navigator (null-safe)
#if SWEF_GUIDEDTOUR_AVAILABLE
            var navigator = FindObjectOfType<WaypointNavigator>();
            navigator?.SetTargetByKey(objective.objectiveType);
#endif
        }
    }
}
