using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Interactive step-by-step emergency checklist controller.
    /// Auto-completes automatic steps, enforces per-step timers, and applies
    /// severity penalties for skipped critical steps.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyChecklistController : MonoBehaviour
    {
        #region Inspector

        [Header("Configuration")]
        [SerializeField] private float defaultStepTimeout = 30f;
        [SerializeField] private bool  autoCompleteOnStart = true;

        #endregion

        #region Events

        /// <summary>Fired when a checklist step is completed.</summary>
        public event Action<ActiveEmergency, int, EmergencyChecklistItem> OnStepCompleted;

        /// <summary>Fired when a checklist step times out.</summary>
        public event Action<ActiveEmergency, int, EmergencyChecklistItem> OnStepTimedOut;

        /// <summary>Fired when a step is skipped (penalty applied if critical).</summary>
        public event Action<ActiveEmergency, int, EmergencyChecklistItem> OnStepSkipped;

        /// <summary>Fired when the full checklist is completed.</summary>
        public event Action<ActiveEmergency> OnChecklistCompleted;

        #endregion

        #region Private State

        private ActiveEmergency _currentEmergency;
        private List<EmergencyChecklistItem> _steps = new List<EmergencyChecklistItem>();
        private int _currentStepIndex;
        private float _stepTimer;
        private bool _isActive;
        private Coroutine _autoCompleteCoroutine;

        // Per-emergency-type contextual checklists
        private static readonly Dictionary<EmergencyType, EmergencyChecklistItem[]> _contextualChecklists
            = new Dictionary<EmergencyType, EmergencyChecklistItem[]>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BuildContextualChecklists();
        }

        private void Update()
        {
            if (!_isActive || _currentEmergency == null) return;
            if (_currentStepIndex >= _steps.Count) return;

            var step = _steps[_currentStepIndex];
            if (step.isAutomatic) return; // handled by coroutine

            float limit = step.timeLimit > 0f ? step.timeLimit : defaultStepTimeout;
            _stepTimer += Time.deltaTime;

            if (_stepTimer >= limit)
                HandleStepTimeout(step);
        }

        #endregion

        #region Public API

        /// <summary>Begin the checklist procedure for the given active emergency.</summary>
        public void StartChecklist(ActiveEmergency emergency)
        {
            if (emergency == null) return;

            StopCurrentChecklist();

            _currentEmergency = emergency;
            _steps = BuildStepsFor(emergency);
            _currentStepIndex = 0;
            _stepTimer = 0f;
            _isActive = true;

            EmergencyManager.Instance?.SetPhase(emergency, EmergencyPhase.ChecklistActive);

            if (autoCompleteOnStart)
                _autoCompleteCoroutine = StartCoroutine(AutoCompleteLoop());
        }

        /// <summary>Mark the current step as completed by the player.</summary>
        public void CompleteCurrentStep()
        {
            if (!_isActive || _currentEmergency == null) return;
            if (_currentStepIndex >= _steps.Count) return;

            var step = _steps[_currentStepIndex];
            AdvanceStep(step, completed: true);
        }

        /// <summary>Skip the current step (applies severity penalty if critical).</summary>
        public void SkipCurrentStep()
        {
            if (!_isActive || _currentEmergency == null) return;
            if (_currentStepIndex >= _steps.Count) return;

            var step = _steps[_currentStepIndex];
            if (step.isCritical)
                ApplyCriticalSkipPenalty();

            OnStepSkipped?.Invoke(_currentEmergency, _currentStepIndex, step);
            AdvanceStep(step, completed: false);
        }

        /// <summary>Abort the active checklist without resolution.</summary>
        public void StopCurrentChecklist()
        {
            _isActive = false;
            _currentEmergency = null;
            _steps.Clear();
            _currentStepIndex = 0;
            _stepTimer = 0f;

            if (_autoCompleteCoroutine != null)
            {
                StopCoroutine(_autoCompleteCoroutine);
                _autoCompleteCoroutine = null;
            }
        }

        /// <summary>Returns the current checklist step, or null if no checklist is active.</summary>
        public EmergencyChecklistItem GetCurrentStep()
        {
            if (!_isActive || _currentStepIndex >= _steps.Count) return null;
            return _steps[_currentStepIndex];
        }

        /// <summary>Current step index (0-based).</summary>
        public int CurrentStepIndex => _currentStepIndex;

        /// <summary>Total number of steps in the active checklist.</summary>
        public int TotalSteps => _steps.Count;

        /// <summary>Whether a checklist is currently running.</summary>
        public bool IsActive => _isActive;

        #endregion

        #region Private Helpers

        private List<EmergencyChecklistItem> BuildStepsFor(ActiveEmergency emergency)
        {
            var list = new List<EmergencyChecklistItem>();

            // Use contextual checklist if available
            if (_contextualChecklists.TryGetValue(emergency.scenario.type, out var preset))
            {
                list.AddRange(preset);
            }
            else
            {
                // Fall back to scenario step keys
                foreach (var key in emergency.scenario.checklistStepKeys)
                {
                    list.Add(new EmergencyChecklistItem
                    {
                        stepKey     = key,
                        actionKey   = key + "_action",
                        isAutomatic = false,
                        isCritical  = false,
                        timeLimit   = defaultStepTimeout
                    });
                }
            }

            emergency.totalChecklistSteps = list.Count;
            return list;
        }

        private void AdvanceStep(EmergencyChecklistItem step, bool completed)
        {
            if (completed)
            {
                _currentEmergency.checklistProgress++;
                OnStepCompleted?.Invoke(_currentEmergency, _currentStepIndex, step);
            }

            _currentStepIndex++;
            _stepTimer = 0f;

            if (_currentStepIndex >= _steps.Count)
                FinishChecklist();
        }

        private void HandleStepTimeout(EmergencyChecklistItem step)
        {
            OnStepTimedOut?.Invoke(_currentEmergency, _currentStepIndex, step);
            if (step.isCritical)
                ApplyCriticalSkipPenalty();
            AdvanceStep(step, completed: false);
        }

        private void ApplyCriticalSkipPenalty()
        {
            if (_currentEmergency == null) return;
            if (_currentEmergency.currentSeverity < EmergencySeverity.Mayday)
                _currentEmergency.currentSeverity = (EmergencySeverity)((int)_currentEmergency.currentSeverity + 1);
        }

        private void FinishChecklist()
        {
            _isActive = false;
            OnChecklistCompleted?.Invoke(_currentEmergency);
            EmergencyManager.Instance?.SetPhase(_currentEmergency, EmergencyPhase.ExecutingProcedure);
        }

        private IEnumerator AutoCompleteLoop()
        {
            while (_isActive && _currentStepIndex < _steps.Count)
            {
                var step = _steps[_currentStepIndex];
                if (step.isAutomatic)
                {
                    yield return new WaitForSeconds(0.5f);
                    AdvanceStep(step, completed: true);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private static void BuildContextualChecklists()
        {
            if (_contextualChecklists.Count > 0) return;

            _contextualChecklists[EmergencyType.EngineFailure] = new[]
            {
                MakeStep("checklist_throttle_idle",    "checklist_throttle_idle_action",    false, true,  20f),
                MakeStep("checklist_fuel_switch",      "checklist_fuel_switch_action",      false, true,  20f),
                MakeStep("checklist_engine_restart",   "checklist_engine_restart_action",   false, false, 30f),
                MakeStep("checklist_squawk_7700",      "checklist_squawk_7700_action",      true,  false, 0f),
                MakeStep("checklist_mayday_call",      "checklist_mayday_call_action",      false, true,  15f)
            };

            _contextualChecklists[EmergencyType.FuelStarvation] = new[]
            {
                MakeStep("checklist_fuel_switch",      "checklist_fuel_switch_action",      false, true,  15f),
                MakeStep("checklist_cross_feed",       "checklist_cross_feed_action",       false, false, 20f),
                MakeStep("checklist_squawk_7700",      "checklist_squawk_7700_action",      true,  false, 0f),
                MakeStep("checklist_divert_nearest",   "checklist_divert_nearest_action",   false, true,  30f)
            };

            _contextualChecklists[EmergencyType.FireOnboard] = new[]
            {
                MakeStep("checklist_fire_agent_1",     "checklist_fire_agent_1_action",     false, true,  15f),
                MakeStep("checklist_fire_agent_2",     "checklist_fire_agent_2_action",     false, true,  15f),
                MakeStep("checklist_squawk_7700",      "checklist_squawk_7700_action",      true,  false, 0f),
                MakeStep("checklist_mayday_call",      "checklist_mayday_call_action",      false, true,  15f),
                MakeStep("checklist_rapid_descent",    "checklist_rapid_descent_action",    false, true,  20f)
            };

            _contextualChecklists[EmergencyType.Depressurization] = new[]
            {
                MakeStep("checklist_oxygen_masks",     "checklist_oxygen_masks_action",     true,  true,  0f),
                MakeStep("checklist_rapid_descent",    "checklist_rapid_descent_action",    false, true,  15f),
                MakeStep("checklist_squawk_7700",      "checklist_squawk_7700_action",      true,  false, 0f),
                MakeStep("checklist_mayday_call",      "checklist_mayday_call_action",      false, true,  15f)
            };

            _contextualChecklists[EmergencyType.IcingCritical] = new[]
            {
                MakeStep("checklist_anti_ice_on",      "checklist_anti_ice_on_action",      false, true,  15f),
                MakeStep("checklist_descend_warmer",   "checklist_descend_warmer_action",   false, false, 30f),
                MakeStep("checklist_reduce_aoa",       "checklist_reduce_aoa_action",       false, false, 20f)
            };

            _contextualChecklists[EmergencyType.LandingGearMalfunction] = new[]
            {
                MakeStep("checklist_gear_gravity",     "checklist_gear_gravity_action",     false, true,  20f),
                MakeStep("checklist_gear_manual",      "checklist_gear_manual_action",      false, false, 30f),
                MakeStep("checklist_low_pass",         "checklist_low_pass_action",         false, false, 0f),
                MakeStep("checklist_squawk_7700",      "checklist_squawk_7700_action",      true,  false, 0f)
            };
        }

        private static EmergencyChecklistItem MakeStep(string stepKey, string actionKey,
            bool automatic, bool critical, float limit) => new EmergencyChecklistItem
        {
            stepKey     = stepKey,
            actionKey   = actionKey,
            isAutomatic = automatic,
            isCritical  = critical,
            timeLimit   = limit
        };

        #endregion
    }
}
