// LandingTutorialController.cs — Phase 120: Precision Landing Challenge System
// Landing tutorial: step-by-step approach guidance, visual cues, auto-speed hints for beginners.
// Namespace: SWEF.LandingChallenge

using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LandingChallenge
{
    /// <summary>
    /// Phase 120 — Step-by-step landing tutorial controller.
    /// Guides beginners through an approach with visual cues, speed hints,
    /// and configuration reminders.
    /// </summary>
    public class LandingTutorialController : MonoBehaviour
    {
        // ── Tutorial Step ─────────────────────────────────────────────────────

        [System.Serializable]
        public class TutorialStep
        {
            public string Title;
            public string Instruction;
            public float  TriggerAltitudeAGL;
            public bool   RequireGearDown;
            public int    RequireFlapSetting;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Tutorial Steps")]
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Settings")]
        [SerializeField] private bool showSpeedHints = true;
        [SerializeField] private float targetSpeedKnots = 135f;

        // ── State ─────────────────────────────────────────────────────────────

        private int  _currentStepIndex;
        private bool _isActive;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised when a tutorial step becomes active.</summary>
        public event System.Action<TutorialStep> OnStepActivated;

        /// <summary>Raised when the tutorial completes all steps.</summary>
        public event System.Action OnTutorialComplete;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current tutorial step, or <c>null</c> if tutorial is complete.</summary>
        public TutorialStep CurrentStep =>
            _isActive && _currentStepIndex < steps.Count ? steps[_currentStepIndex] : null;

        /// <summary>Whether the tutorial is currently active.</summary>
        public bool IsActive => _isActive;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate the tutorial from the first step.</summary>
        public void Activate()
        {
            _currentStepIndex = 0;
            _isActive         = true;
            if (steps.Count == 0) PopulateDefaultSteps();
            if (steps.Count > 0) OnStepActivated?.Invoke(steps[0]);
        }

        /// <summary>Deactivate the tutorial.</summary>
        public void Deactivate() => _isActive = false;

        /// <summary>Advance to the next tutorial step.</summary>
        public void NextStep()
        {
            _currentStepIndex++;
            if (_currentStepIndex >= steps.Count)
            {
                _isActive = false;
                OnTutorialComplete?.Invoke();
                return;
            }
            OnStepActivated?.Invoke(steps[_currentStepIndex]);
        }

        /// <summary>
        /// Feed altitude data to auto-advance steps based on altitude triggers.
        /// </summary>
        public void UpdateAltitude(float altAGL, bool gearDown, int flapSetting)
        {
            if (!_isActive || CurrentStep == null) return;
            var step = CurrentStep;
            if (altAGL <= step.TriggerAltitudeAGL)
            {
                bool gearOk  = !step.RequireGearDown || gearDown;
                bool flapsOk = flapSetting >= step.RequireFlapSetting;
                if (gearOk && flapsOk) NextStep();
            }
        }

        /// <summary>Returns a speed hint string if the player is off target speed.</summary>
        public string GetSpeedHint(float currentSpeedKnots)
        {
            if (!showSpeedHints) return null;
            float delta = currentSpeedKnots - targetSpeedKnots;
            if (delta > 10f)  return "Reduce speed";
            if (delta < -10f) return "Increase speed";
            return null;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void PopulateDefaultSteps()
        {
            steps.Add(new TutorialStep { Title = "Initial Approach", Instruction = "Maintain 3,000 ft and reduce speed below 200 KIAS.", TriggerAltitudeAGL = 3000f, RequireGearDown = false, RequireFlapSetting = 0 });
            steps.Add(new TutorialStep { Title = "Gear Down",       Instruction = "Lower landing gear below 2,000 ft AGL.",              TriggerAltitudeAGL = 2000f, RequireGearDown = true,  RequireFlapSetting = 0 });
            steps.Add(new TutorialStep { Title = "Configure Flaps", Instruction = "Set flaps to landing position at 1,500 ft AGL.",       TriggerAltitudeAGL = 1500f, RequireGearDown = true,  RequireFlapSetting = 3 });
            steps.Add(new TutorialStep { Title = "Final Approach",  Instruction = "Fly 3° glideslope, target Vref speed.",               TriggerAltitudeAGL = 500f,  RequireGearDown = true,  RequireFlapSetting = 3 });
            steps.Add(new TutorialStep { Title = "Flare",           Instruction = "At 30 ft AGL, raise nose gently and reduce thrust.",   TriggerAltitudeAGL = 30f,   RequireGearDown = true,  RequireFlapSetting = 3 });
        }
    }
}
