using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Core;
using SWEF.UI;
using SWEF.Analytics;

namespace SWEF.Tutorial
{
    /// <summary>
    /// Main controller for the Phase 23 interactive tutorial.
    /// Replaces the legacy <see cref="TutorialManager"/> with a step-based system
    /// that spotlights HUD elements, shows anchored tooltips, and waits for real
    /// player actions before advancing.
    /// </summary>
    /// <remarks>
    /// Progress is persisted in PlayerPrefs under <c>SWEF_Tutorial2_Progress</c> and
    /// <c>SWEF_Tutorial2_Completed</c>.  On completion the legacy key
    /// <c>SWEF_TutorialCompleted</c> is also set for backward compatibility.
    /// </remarks>
    public class InteractiveTutorialManager : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────────
        private const string PrefProgress  = "SWEF_Tutorial2_Progress";
        private const string PrefCompleted = "SWEF_Tutorial2_Completed";
        private const string PrefLegacy    = "SWEF_TutorialCompleted";

        // ── Inspector references ──────────────────────────────────────────────
        [Header("Components")]
        [SerializeField] private TutorialHighlight      highlight;
        [SerializeField] private TutorialTooltip        tooltip;
        [SerializeField] private TutorialActionDetector actionDetector;

        [Header("HUD Root (used to find spotlight targets by name)")]
        [SerializeField] private Transform hudRoot;

        [Header("Tutorial Steps")]
        [SerializeField] private TutorialStepData[] steps = BuildDefaultSteps();

        // ── Runtime state ─────────────────────────────────────────────────────
        private int     _currentStepIndex;
        private bool    _waitingForAction;
        private bool    _running;
        private Coroutine _timeoutCoroutine;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Clears saved progress and restarts the tutorial from step 0.</summary>
        public void RestartTutorial()
        {
            PlayerPrefs.DeleteKey(PrefProgress);
            PlayerPrefs.DeleteKey(PrefCompleted);
            PlayerPrefs.Save();
            StopAllCoroutines();
            BeginTutorial();
        }

        /// <summary>Marks all steps as complete and hides the tutorial UI immediately.</summary>
        public void SkipTutorial()
        {
            StopAllCoroutines();
            CompleteTutorial(skipped: true);
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (actionDetector == null)
                actionDetector = FindFirstObjectByType<TutorialActionDetector>();
        }

        private void Start()
        {
            // Only run in the World scene (needs a GPS fix).
            if (!SWEFSession.HasFix) return;

            // Legacy: respect old completion key so returning players are not re-tutorialled.
            bool legacyDone  = PlayerPrefs.GetInt(PrefLegacy,    0) == 1;
            bool newDone     = PlayerPrefs.GetInt(PrefCompleted, 0) == 1;
            if (legacyDone || newDone) return;

            BeginTutorial();
        }

        // ── Input handling ────────────────────────────────────────────────────

        private void Update()
        {
            if (!_running) return;

            // Tap-to-continue for non-action steps
            if (!_waitingForAction)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    AdvanceStep();
            }
        }

        // ── Tutorial flow ─────────────────────────────────────────────────────

        private void BeginTutorial()
        {
            _running = true;
            _currentStepIndex = PlayerPrefs.GetInt(PrefProgress, 0);

            if (steps == null || steps.Length == 0)
            {
                CompleteTutorial();
                return;
            }

            if (actionDetector != null)
            {
                actionDetector.OnActionDetected += OnActionDetected;
                actionDetector.SetActive(false);
            }

            ShowStep(_currentStepIndex);
        }

        private void ShowStep(int index)
        {
            if (index >= steps.Length)
            {
                CompleteTutorial();
                return;
            }

            TutorialStepData step = steps[index];

            // Save progress
            PlayerPrefs.SetInt(PrefProgress, index);
            PlayerPrefs.Save();

            // Fire analytics
            FireStepAnalytics(index, step);

            // Resolve spotlight target
            RectTransform targetRect = FindHudTarget(step.spotlightTargetName);

            // Show highlight
            if (highlight != null)
            {
                if (targetRect != null)
                    highlight.Show(targetRect);
                else
                    highlight.Hide();
            }

            // Show tooltip
            string message = ResolveText(step);
            if (tooltip != null)
                tooltip.Show(message, targetRect, step.tooltipAnchor, step.requiresAction);

            // Manage action detection
            _waitingForAction = step.requiresAction;
            if (actionDetector != null)
                actionDetector.SetActive(step.requiresAction);

            // Start timeout if configured
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            if (step.requiresAction && step.actionTimeoutSec > 0f)
                _timeoutCoroutine = StartCoroutine(ActionTimeout(step.actionTimeoutSec));
        }

        private void AdvanceStep()
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }

            _currentStepIndex++;
            ShowStep(_currentStepIndex);
        }

        private void CompleteTutorial(bool skipped = false)
        {
            _running          = false;
            _waitingForAction = false;

            if (actionDetector != null)
            {
                actionDetector.OnActionDetected -= OnActionDetected;
                actionDetector.SetActive(false);
            }

            if (highlight != null) highlight.Hide();
            if (tooltip   != null) tooltip.Hide();

            PlayerPrefs.SetInt(PrefCompleted, 1);
            PlayerPrefs.SetInt(PrefLegacy,    1);
            PlayerPrefs.Save();

            // Analytics: completion event
            AnalyticsLogger.LogEvent(AnalyticsEvents.TutorialStep, skipped ? "tutorial_skipped" : "tutorial_completed");
        }

        // ── Action detection ──────────────────────────────────────────────────

        private void OnActionDetected(string actionId)
        {
            if (!_running || !_waitingForAction) return;
            if (steps == null || _currentStepIndex >= steps.Length) return;

            TutorialStepData step = steps[_currentStepIndex];
            if (!step.requiresAction) return;

            // Check if this action matches the required one.
            // A step with "roll_left" or "roll_right" accepts either roll direction.
            bool matches = step.requiredActionId == actionId
                || (step.requiredActionId == "roll_left"  && actionId == "roll_right")
                || (step.requiredActionId == "roll_right" && actionId == "roll_left");

            if (matches)
                AdvanceStep();
        }

        // ── Timeout coroutine ─────────────────────────────────────────────────

        private IEnumerator ActionTimeout(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            // Offer a skip by converting the step to tap-to-continue
            _waitingForAction = false;
            if (actionDetector != null) actionDetector.SetActive(false);
            if (tooltip != null)
            {
                // Re-show with tap-to-continue prompt
                TutorialStepData step = steps[_currentStepIndex];
                RectTransform target = FindHudTarget(step.spotlightTargetName);
                string msg = ResolveText(step) + "\n(Tap to skip)";
                tooltip.Show(msg, target, step.tooltipAnchor, requiresAction: false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string ResolveText(TutorialStepData step)
        {
            if (!string.IsNullOrEmpty(step.localizationKey) && LocalizationManager.Instance != null)
            {
                string localized = LocalizationManager.Instance.Get(step.localizationKey);
                // LocalizationManager returns the key itself when not found
                if (localized != step.localizationKey)
                    return localized;
            }
            return step.fallbackText;
        }

        private RectTransform FindHudTarget(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;

            // Search the HUD root first for performance
            if (hudRoot != null)
            {
                Transform t = hudRoot.Find(targetName);
                if (t != null) return t as RectTransform;

                // Deep search within HUD
                foreach (Transform child in hudRoot.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (child.name == targetName)
                        return child as RectTransform;
                }
            }

            // Fallback: search entire scene
            GameObject go = GameObject.Find(targetName);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private void FireStepAnalytics(int index, TutorialStepData step)
        {
            AnalyticsLogger.LogEvent(AnalyticsEvents.TutorialStep, $"step_{index}:{step.requiredActionId ?? step.fallbackText}");

            var tracker = FindFirstObjectByType<UserBehaviorTracker>();
            if (tracker != null)
                tracker.TrackTutorialStep(index, step.fallbackText);
        }

        // ── Default step definitions ──────────────────────────────────────────

        private static TutorialStepData[] BuildDefaultSteps()
        {
            return new TutorialStepData[]
            {
                // 0 — Welcome
                new TutorialStepData
                {
                    localizationKey    = "tutorial_welcome",
                    fallbackText       = "Welcome to Skywalking: Earth Flight! 🚀\nYou're about to soar above the planet. Let's take a quick tour!",
                    spotlightTargetName = "",
                    requiresAction     = false,
                    tooltipAnchor      = TooltipAnchor.Center
                },
                // 1 — Look around
                new TutorialStepData
                {
                    localizationKey    = "tutorial_look_around",
                    fallbackText       = "Drag the screen to look around. 👀\nExplore the world below you!",
                    spotlightTargetName = "",
                    requiresAction     = true,
                    requiredActionId   = "look_around",
                    actionTimeoutSec   = 15f,
                    tooltipAnchor      = TooltipAnchor.Center
                },
                // 2 — Throttle
                new TutorialStepData
                {
                    localizationKey    = "tutorial_throttle",
                    fallbackText       = "Use the throttle slider to fly forward. ✈️\nSlide it up to accelerate!",
                    spotlightTargetName = "ThrottleSlider",
                    requiresAction     = true,
                    requiredActionId   = "throttle_change",
                    actionTimeoutSec   = 20f,
                    tooltipAnchor      = TooltipAnchor.Right
                },
                // 3 — Altitude
                new TutorialStepData
                {
                    localizationKey    = "tutorial_altitude",
                    fallbackText       = "Adjust the altitude slider to climb higher. 🌤\nWatch the sky change as you ascend!",
                    spotlightTargetName = "AltitudeSlider",
                    requiresAction     = true,
                    requiredActionId   = "altitude_change",
                    actionTimeoutSec   = 20f,
                    tooltipAnchor      = TooltipAnchor.Left
                },
                // 4 — Roll
                new TutorialStepData
                {
                    localizationKey    = "tutorial_roll",
                    fallbackText       = "Tap the roll buttons (◀ ▶) to bank left and right. 🔄",
                    spotlightTargetName = "RollButtons",
                    requiresAction     = true,
                    requiredActionId   = "roll_left",
                    actionTimeoutSec   = 20f,
                    tooltipAnchor      = TooltipAnchor.Top
                },
                // 5 — Comfort mode
                new TutorialStepData
                {
                    localizationKey    = "tutorial_comfort",
                    fallbackText       = "Toggle Comfort Mode for a smoother, auto-leveling flight. 😌",
                    spotlightTargetName = "ComfortToggle",
                    requiresAction     = true,
                    requiredActionId   = "comfort_toggle",
                    actionTimeoutSec   = 15f,
                    tooltipAnchor      = TooltipAnchor.Bottom
                },
                // 6 — Settings
                new TutorialStepData
                {
                    localizationKey    = "tutorial_settings",
                    fallbackText       = "Open Settings ⚙ to customize speed, audio, language, and more.",
                    spotlightTargetName = "SettingsButton",
                    requiresAction     = true,
                    requiredActionId   = "settings_open",
                    actionTimeoutSec   = 15f,
                    tooltipAnchor      = TooltipAnchor.Bottom
                },
                // 7 — Screenshot
                new TutorialStepData
                {
                    localizationKey    = "tutorial_screenshot",
                    fallbackText       = "Capture stunning views with the screenshot button 📷.",
                    spotlightTargetName = "ScreenshotButton",
                    requiresAction     = true,
                    requiredActionId   = "screenshot_take",
                    actionTimeoutSec   = 20f,
                    tooltipAnchor      = TooltipAnchor.Bottom
                },
                // 8 — Teleport
                new TutorialStepData
                {
                    localizationKey    = "tutorial_teleport",
                    fallbackText       = "Tap the Teleport button 🔍 to search for any place on Earth and fly there instantly!",
                    spotlightTargetName = "TeleportButton",
                    requiresAction     = true,
                    requiredActionId   = "teleport_open",
                    actionTimeoutSec   = 20f,
                    tooltipAnchor      = TooltipAnchor.Bottom
                },
                // 9 — Achievements
                new TutorialStepData
                {
                    localizationKey    = "tutorial_achievements",
                    fallbackText       = "Check your Achievements 🏆 to discover goals and earn rewards.",
                    spotlightTargetName = "AchievementsButton",
                    requiresAction     = false,
                    tooltipAnchor      = TooltipAnchor.Bottom
                },
                // 10 — Completion
                new TutorialStepData
                {
                    localizationKey    = "tutorial_complete",
                    fallbackText       = "You're ready! 🌍✨\nEnjoy exploring Earth from above. The sky is just the beginning!",
                    spotlightTargetName = "",
                    requiresAction     = false,
                    tooltipAnchor      = TooltipAnchor.Center
                }
            };
        }
    }
}
