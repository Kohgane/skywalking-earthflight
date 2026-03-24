using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Emergency UI overlay providing alert banners, checklist panel,
    /// landing guidance, distress call button, and rescue tracker.
    /// Integrates with SWEF.Localization and SWEF.Accessibility (null-safe).
    /// </summary>
    [DisallowMultipleComponent]
    public class EmergencyHUD : MonoBehaviour
    {
        #region Inspector

        [Header("HUD Panels")]
        [Tooltip("Root GameObject for the alert banner.")]
        [SerializeField] private GameObject alertBannerPanel;

        [Tooltip("Root GameObject for the checklist panel.")]
        [SerializeField] private GameObject checklistPanel;

        [Tooltip("Root GameObject for the landing guidance overlay.")]
        [SerializeField] private GameObject landingGuidancePanel;

        [Tooltip("Root GameObject for the rescue tracker panel.")]
        [SerializeField] private GameObject rescueTrackerPanel;

        [Header("Colors")]
        [SerializeField] private Color cautionBannerColor   = new Color(1f, 1f, 0f, 0.85f);
        [SerializeField] private Color warningBannerColor   = new Color(1f, 0.5f, 0f, 0.9f);
        [SerializeField] private Color emergencyBannerColor = new Color(1f, 0f, 0f, 0.95f);

        [Header("References")]
        [Tooltip("Checklist controller this HUD is bound to.")]
        [SerializeField] private EmergencyChecklistController checklistController;

        [Tooltip("Landing controller this HUD is bound to.")]
        [SerializeField] private EmergencyLandingController landingController;

        [Tooltip("Rescue controller this HUD is bound to.")]
        [SerializeField] private RescueSimulationController rescueController;

        #endregion

        #region Private State

        private ActiveEmergency _displayedEmergency;
        private bool _hudVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetPanelsActive(false, false, false, false);
        }

        private void OnEnable()
        {
            if (EmergencyManager.Instance != null)
            {
                EmergencyManager.Instance.OnEmergencyTriggered     += ShowEmergency;
                EmergencyManager.Instance.OnEmergencyPhaseChanged  += OnPhaseChanged;
                EmergencyManager.Instance.OnEmergencyEscalated     += OnSeverityEscalated;
                EmergencyManager.Instance.OnEmergencyResolved      += OnEmergencyResolved;
            }

            if (landingController != null)
                landingController.OnApproachDeviationUpdated += UpdateApproachGuidance;

            if (rescueController != null)
            {
                rescueController.OnUnitDispatched += _ => RefreshRescueTracker();
                rescueController.OnUnitArrived    += _ => RefreshRescueTracker();
            }
        }

        private void OnDisable()
        {
            if (EmergencyManager.Instance != null)
            {
                EmergencyManager.Instance.OnEmergencyTriggered     -= ShowEmergency;
                EmergencyManager.Instance.OnEmergencyPhaseChanged  -= OnPhaseChanged;
                EmergencyManager.Instance.OnEmergencyEscalated     -= OnSeverityEscalated;
                EmergencyManager.Instance.OnEmergencyResolved      -= OnEmergencyResolved;
            }

            if (landingController != null)
                landingController.OnApproachDeviationUpdated -= UpdateApproachGuidance;
        }

        #endregion

        #region Public API

        /// <summary>Show the HUD for the given active emergency.</summary>
        public void ShowEmergency(ActiveEmergency emergency)
        {
            _displayedEmergency = emergency;
            _hudVisible = true;
            SetPanelsActive(true, true, false, false);
            RefreshAlertBanner();
            RefreshChecklist();
        }

        /// <summary>Hide the emergency HUD completely.</summary>
        public void Hide()
        {
            _hudVisible = false;
            _displayedEmergency = null;
            SetPanelsActive(false, false, false, false);
        }

        /// <summary>Execute the current checklist step via button press.</summary>
        public void OnExecuteStepButton()
        {
            checklistController?.CompleteCurrentStep();
            RefreshChecklist();
        }

        /// <summary>Skip the current checklist step via button press.</summary>
        public void OnSkipStepButton()
        {
            checklistController?.SkipCurrentStep();
            RefreshChecklist();
        }

        /// <summary>Trigger the distress call flow via button press.</summary>
        public void OnDistressCallButton()
        {
            if (_displayedEmergency == null) return;
            var dcs = GetComponent<DistressCallSystem>();
            dcs?.MakeDistressCall(_displayedEmergency, _displayedEmergency.scenario.requiredCall);
        }

        #endregion

        #region Private Helpers

        private void RefreshAlertBanner()
        {
            if (_displayedEmergency == null || alertBannerPanel == null) return;
            alertBannerPanel.SetActive(true);

            // Tint color by severity
            var img = alertBannerPanel.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = SeverityToColor(_displayedEmergency.currentSeverity);

            // Localized text
            SetLocalizedText(alertBannerPanel, _displayedEmergency.scenario.displayNameKey);
        }

        private void RefreshChecklist()
        {
            if (checklistPanel == null) return;
            var step = checklistController?.GetCurrentStep();
            checklistPanel.SetActive(step != null);
            if (step != null)
                SetLocalizedText(checklistPanel, step.stepKey);
        }

        private void OnPhaseChanged(ActiveEmergency em, EmergencyPhase phase)
        {
            if (em != _displayedEmergency) return;
            bool guidanceVisible = phase == EmergencyPhase.OnApproach || phase == EmergencyPhase.Diverting;
            bool rescueVisible   = phase == EmergencyPhase.Landed || phase == EmergencyPhase.Crashed;
            SetPanelsActive(true, phase == EmergencyPhase.ChecklistActive, guidanceVisible, rescueVisible);
        }

        private void OnSeverityEscalated(ActiveEmergency em, EmergencySeverity previous)
        {
            if (em == _displayedEmergency)
                RefreshAlertBanner();
        }

        private void OnEmergencyResolved(EmergencyResolution resolution)
        {
            if (_displayedEmergency != null && resolution.emergencyId == _displayedEmergency.emergencyId)
                Hide();
        }

        private void UpdateApproachGuidance(float lateralDev, float verticalDev)
        {
            if (landingGuidancePanel == null || !landingGuidancePanel.activeSelf) return;
            // Update deviation indicators — actual UI manipulation delegated to UI layout.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EmergencyHUD] Approach: lat={lateralDev:F1}° vert={verticalDev:F1}m");
#endif
        }

        private void RefreshRescueTracker()
        {
            if (rescueTrackerPanel == null) return;
            var units = rescueController?.ActiveUnits;
            rescueTrackerPanel.SetActive(units != null && units.Count > 0);
        }

        private void SetPanelsActive(bool alert, bool checklist, bool guidance, bool rescue)
        {
            if (alertBannerPanel    != null) alertBannerPanel.SetActive(alert);
            if (checklistPanel      != null) checklistPanel.SetActive(checklist);
            if (landingGuidancePanel!= null) landingGuidancePanel.SetActive(guidance);
            if (rescueTrackerPanel  != null) rescueTrackerPanel.SetActive(rescue);
        }

        private Color SeverityToColor(EmergencySeverity severity)
        {
            return severity switch
            {
                EmergencySeverity.Caution    => cautionBannerColor,
                EmergencySeverity.Warning    => warningBannerColor,
                _                            => emergencyBannerColor
            };
        }

        private void SetLocalizedText(GameObject panel, string key)
        {
            if (panel == null) return;
#if SWEF_LOCALIZATION_AVAILABLE
            string text = SWEF.Localization.LocalizationManager.Instance?.GetString(key) ?? key;
#else
            string text = key;
#endif
            var label = panel.GetComponentInChildren<UnityEngine.UI.Text>();
            if (label != null) label.text = text;
        }

        #endregion
    }
}
