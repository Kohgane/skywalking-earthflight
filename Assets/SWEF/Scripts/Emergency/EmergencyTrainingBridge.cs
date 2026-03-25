using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Flight School training integration for the emergency system.
    /// Provides 6 preset training scenarios, instructor guidance, no crash consequences,
    /// and achievement / journal reporting. All cross-system calls are null-safe.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyTrainingBridge : MonoBehaviour
    {
        #region Inner Types

        /// <summary>A single preset training scenario definition.</summary>
        [Serializable]
        public class TrainingScenario
        {
            public string scenarioId       = string.Empty;
            public string titleKey         = string.Empty;
            public string instructionKey   = string.Empty;
            public int    difficultyRating = 1;
        }

        #endregion

        #region Inspector

        [Header("Training Scenarios")]
        [SerializeField] private List<TrainingScenario> trainingScenarios = new List<TrainingScenario>();

        [Header("Instructor")]
        [Tooltip("Delay in seconds before the instructor delivers guidance after a step.")]
        [SerializeField] private float instructorDelay = 2f;

        #endregion

        #region Events

        /// <summary>Fired when a training run starts.</summary>
        public event Action<TrainingScenario> OnTrainingStarted;

        /// <summary>Fired when a training run completes.</summary>
        public event Action<TrainingScenario, EmergencyResolution> OnTrainingCompleted;

        #endregion

        #region Private State

        private TrainingScenario _activeTraining;
        private ActiveEmergency  _trainingEmergency;
        private bool _trainingActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BuildDefaultScenarios();
        }

        private void OnEnable()
        {
            if (EmergencyManager.Instance != null)
                EmergencyManager.Instance.OnEmergencyResolved += OnEmergencyResolved;
        }

        private void OnDisable()
        {
            if (EmergencyManager.Instance != null)
                EmergencyManager.Instance.OnEmergencyResolved -= OnEmergencyResolved;
        }

        #endregion

        #region Public API

        /// <summary>Begin a training run for the preset at the given index.</summary>
        /// <param name="index">Index into <see cref="trainingScenarios"/>.</param>
        public void StartTraining(int index)
        {
            if (index < 0 || index >= trainingScenarios.Count) return;
            if (_trainingActive) return;

            _activeTraining = trainingScenarios[index];
            _trainingActive = true;

            // Disable crash consequences for training
            var config = EmergencyManager.Instance?.Config;
            if (config != null) config.enableCrashConsequences = false;

            _trainingEmergency = EmergencyManager.Instance?.TriggerEmergency(
                _activeTraining.scenarioId, Vector3.zero, 2000f);

            OnTrainingStarted?.Invoke(_activeTraining);
            DeliverInstructorGuidance(_activeTraining.instructionKey);
        }

        /// <summary>All available training preset titles as localized strings.</summary>
        public IReadOnlyList<TrainingScenario> TrainingScenarios => trainingScenarios;

        #endregion

        #region Private Helpers

        private void BuildDefaultScenarios()
        {
            if (trainingScenarios.Count > 0) return;

            trainingScenarios.AddRange(new[]
            {
                new TrainingScenario { scenarioId="engine_failure",         titleKey="training_em_engine",       instructionKey="training_em_engine_hint",       difficultyRating=1 },
                new TrainingScenario { scenarioId="fire_onboard",           titleKey="training_em_fire",         instructionKey="training_em_fire_hint",         difficultyRating=3 },
                new TrainingScenario { scenarioId="depressurization",       titleKey="training_em_depress",      instructionKey="training_em_depress_hint",      difficultyRating=3 },
                new TrainingScenario { scenarioId="landing_gear_malfunction",titleKey="training_em_gear",        instructionKey="training_em_gear_hint",         difficultyRating=2 },
                new TrainingScenario { scenarioId="fuel_starvation",        titleKey="training_em_fuel",         instructionKey="training_em_fuel_hint",         difficultyRating=2 },
                new TrainingScenario { scenarioId="dual_engine_failure",    titleKey="training_em_dual_engine",  instructionKey="training_em_dual_engine_hint",  difficultyRating=5 }
            });
        }

        private void OnEmergencyResolved(EmergencyResolution resolution)
        {
            if (!_trainingActive) return;
            if (_trainingEmergency == null || resolution.emergencyId != _trainingEmergency.emergencyId) return;

            _trainingActive = false;

            // Re-enable crash consequences after training
            var config = EmergencyManager.Instance?.Config;
            if (config != null) config.enableCrashConsequences = true;

            OnTrainingCompleted?.Invoke(_activeTraining, resolution);

            ReportAchievement(resolution);
            LogJournal(_activeTraining, resolution);

            _activeTraining   = null;
            _trainingEmergency = null;
        }

        private void DeliverInstructorGuidance(string guidanceKey)
        {
#if SWEF_FLIGHTSCHOOL_AVAILABLE
            SWEF.FlightSchool.FlightInstructor.Instance?.Speak(guidanceKey, instructorDelay);
#else
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EmergencyTrainingBridge] Instructor guidance: {guidanceKey}");
#endif
#endif
        }

        private void ReportAchievement(EmergencyResolution resolution)
        {
            if (!resolution.wasSuccessful) return;
#if SWEF_ACHIEVEMENT_AVAILABLE
            SWEF.Achievement.AchievementManager.Instance?.Unlock("emergency_training_complete");
            if (resolution.score >= 90f)
                SWEF.Achievement.AchievementManager.Instance?.Unlock("emergency_training_ace");
#endif
        }

        private void LogJournal(TrainingScenario scenario, EmergencyResolution resolution)
        {
#if SWEF_JOURNAL_AVAILABLE
            SWEF.Journal.JournalManager.Instance?.AddEntry(
                "emergency_training",
                scenario.titleKey,
                $"Score: {resolution.score:F0}/100 — {(resolution.wasSuccessful ? "PASS" : "FAIL")}");
#endif
        }

        #endregion
    }
}
