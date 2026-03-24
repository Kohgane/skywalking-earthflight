// Phase 72 — Autopilot & Cruise Control System
// Assets/SWEF/Scripts/Autopilot/AutopilotHUD.cs
using UnityEngine;
using UnityEngine.UI;
using SWEF.Localization;
using SWEF.CockpitHUD;

namespace SWEF.Autopilot
{
    /// <summary>
    /// Autopilot status panel on the flight HUD.
    /// Displays mode, target values, deviation bars, approach phase,
    /// cruise profile, and estimated range / fuel.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutopilotHUD : MonoBehaviour
    {
        #region Private — cached references
        private FlightDataProvider _dataProvider;
        #endregion

        #region Inspector — Containers
        [Header("Root Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject warningBanner;
        #endregion

        #region Inspector — Mode Indicator
        [Header("Mode Indicator")]
        [SerializeField] private Text  modeLabel;
        [SerializeField] private Image modeIndicatorDot;

        private static readonly Color ColorEngaged     = new Color(0.2f, 0.9f, 0.2f);
        private static readonly Color ColorTransitioning = new Color(0.9f, 0.9f, 0.1f);
        private static readonly Color ColorWarning     = new Color(0.9f, 0.2f, 0.1f);
        #endregion

        #region Inspector — Target / Deviation
        [Header("Target Readouts")]
        [SerializeField] private Text altitudeTargetLabel;
        [SerializeField] private Text headingTargetLabel;
        [SerializeField] private Text speedTargetLabel;

        [Header("Deviation Bars")]
        [SerializeField] private Slider altitudeDeviationBar;
        [SerializeField] private Slider headingDeviationBar;
        [SerializeField] private Slider speedDeviationBar;
        #endregion

        #region Inspector — Approach
        [Header("Approach Phase")]
        [SerializeField] private Text  approachPhaseLabel;
        [SerializeField] private Text  approachDistanceLabel;
        #endregion

        #region Inspector — Cruise
        [Header("Cruise Control")]
        [SerializeField] private Text cruiseSpeedLabel;
        [SerializeField] private Text cruiseProfileBadge;
        [SerializeField] private Text estimatedRangeLabel;
        [SerializeField] private Text estimatedFuelLabel;
        #endregion

        #region Inspector — Warning
        [Header("Warning Text")]
        [SerializeField] private Text warningText;
        #endregion

        #region Inspector — Quick Buttons
        [Header("Quick-Action Buttons")]
        [SerializeField] private Button btnAltHold;
        [SerializeField] private Button btnHdgHold;
        [SerializeField] private Button btnSpdHold;
        [SerializeField] private Button btnApToggle;
        [SerializeField] private Button btnApproach;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            _dataProvider = FindObjectOfType<FlightDataProvider>();
            btnAltHold?.onClick.AddListener(OnAltHold);
            btnHdgHold?.onClick.AddListener(OnHdgHold);
            btnSpdHold?.onClick.AddListener(OnSpdHold);
            btnApToggle?.onClick.AddListener(OnApToggle);
            btnApproach?.onClick.AddListener(OnApproach);
        }

        private void OnEnable()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap != null)
            {
                ap.OnModeChanged           += HandleModeChanged;
                ap.OnEngagementChanged     += HandleEngagementChanged;
                ap.OnApproachPhaseChanged  += HandleApproachPhaseChanged;
                ap.OnAutopilotWarning      += HandleWarning;
            }

            CruiseControlManager cc = CruiseControlManager.Instance;
            if (cc != null)
            {
                cc.OnCruiseStateChanged  += HandleCruiseStateChanged;
                cc.OnProfileChanged      += HandleProfileChanged;
                cc.OnSpeedTargetChanged  += HandleSpeedTargetChanged;
            }
        }

        private void OnDisable()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap != null)
            {
                ap.OnModeChanged           -= HandleModeChanged;
                ap.OnEngagementChanged     -= HandleEngagementChanged;
                ap.OnApproachPhaseChanged  -= HandleApproachPhaseChanged;
                ap.OnAutopilotWarning      -= HandleWarning;
            }

            CruiseControlManager cc = CruiseControlManager.Instance;
            if (cc != null)
            {
                cc.OnCruiseStateChanged  -= HandleCruiseStateChanged;
                cc.OnProfileChanged      -= HandleProfileChanged;
                cc.OnSpeedTargetChanged  -= HandleSpeedTargetChanged;
            }
        }

        private void Update()
        {
            RefreshReadouts();
        }
        #endregion

        #region Refresh
        private void RefreshReadouts()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;

            // Mode indicator colour
            if (modeIndicatorDot != null)
                modeIndicatorDot.color = ap.IsEngaged ? ColorEngaged : ColorWarning;

            // Target labels
            if (altitudeTargetLabel != null)
                altitudeTargetLabel.text = $"{Loc("ap_target_altitude")}: {ap.TargetAltitude:F0} m";
            if (headingTargetLabel != null)
                headingTargetLabel.text  = $"{Loc("ap_target_heading")}: {ap.TargetHeading:F0}°";
            if (speedTargetLabel != null)
                speedTargetLabel.text    = $"{Loc("ap_target_speed")}: {ap.TargetSpeed:F0} km/h";

            // Deviation bars (normalised to ±1)
            RefreshDeviationBars(ap);

            // Cruise info
            CruiseControlManager cc = CruiseControlManager.Instance;
            if (cc != null)
            {
                if (cruiseSpeedLabel  != null) cruiseSpeedLabel.text   = $"{cc.TargetSpeed:F0} km/h";
                if (estimatedRangeLabel != null) estimatedRangeLabel.text = $"{Loc("ap_range_estimate")}: {cc.GetEstimatedRange():F0} km";
                if (estimatedFuelLabel  != null) estimatedFuelLabel.text  = $"{Loc("ap_fuel_estimate")}: {cc.GetEstimatedFuelConsumptionRate():F1} L/hr";
            }
        }

        private void RefreshDeviationBars(AutopilotController ap)
        {
            FlightData data = _dataProvider?.CurrentData;
            if (data == null) return;

            if (altitudeDeviationBar != null)
            {
                float err = ap.TargetAltitude - data.altitude;
                altitudeDeviationBar.value = Mathf.InverseLerp(-500f, 500f, err);
            }

            if (headingDeviationBar != null)
            {
                float err = ShortestHeadingError(data.heading, ap.TargetHeading);
                headingDeviationBar.value = Mathf.InverseLerp(-180f, 180f, err);
            }

            if (speedDeviationBar != null)
            {
                float currentKmh = data.speed * 3.6f;
                float err = ap.TargetSpeed - currentKmh;
                speedDeviationBar.value = Mathf.InverseLerp(-100f, 100f, err);
            }
        }

        private static float ShortestHeadingError(float current, float target)
        {
            float d = target - current;
            while (d >  180f) d -= 360f;
            while (d < -180f) d += 360f;
            return d;
        }
        #endregion

        #region Event Handlers
        private void HandleModeChanged(AutopilotMode mode)
        {
            if (modeLabel != null)
                modeLabel.text = Loc(ModeKey(mode));

            if (modeIndicatorDot != null)
                modeIndicatorDot.color = mode == AutopilotMode.Off ? ColorWarning : ColorEngaged;
        }

        private void HandleEngagementChanged(bool engaged)
        {
            if (hudPanel != null) hudPanel.SetActive(engaged);
        }

        private void HandleApproachPhaseChanged(ApproachPhase phase)
        {
            if (approachPhaseLabel != null)
                approachPhaseLabel.text = Loc(PhaseKey(phase));
        }

        private void HandleWarning(string key)
        {
            if (warningBanner != null) warningBanner.SetActive(true);
            if (warningText   != null) warningText.text = Loc(key);
            // Auto-hide after 4 seconds
            StopCoroutine(nameof(HideWarningCoroutine));
            StartCoroutine(nameof(HideWarningCoroutine));
        }

        private System.Collections.IEnumerator HideWarningCoroutine()
        {
            yield return new WaitForSeconds(4f);
            HideWarning();
        }

        private void HideWarning()
        {
            if (warningBanner != null) warningBanner.SetActive(false);
        }

        private void HandleCruiseStateChanged(CruiseControlState state) { /* future: animate state badge */ }

        private void HandleProfileChanged(CruiseControlManager.CruiseProfile profile)
        {
            if (cruiseProfileBadge == null) return;
            switch (profile)
            {
                case CruiseControlManager.CruiseProfile.Economy:
                    cruiseProfileBadge.text = Loc("ap_cruise_economy"); break;
                case CruiseControlManager.CruiseProfile.Sport:
                    cruiseProfileBadge.text = Loc("ap_cruise_sport");   break;
                default:
                    cruiseProfileBadge.text = Loc("ap_cruise_normal");  break;
            }
        }

        private void HandleSpeedTargetChanged(float speed)
        {
            if (cruiseSpeedLabel != null)
                cruiseSpeedLabel.text = $"{speed:F0} km/h";
        }
        #endregion

        #region Button Callbacks
        private void OnAltHold()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;
            bool engage = !(ap.IsEngaged && ap.CurrentMode == AutopilotMode.AltitudeHold);
            if (engage) ap.Engage(AutopilotMode.AltitudeHold);
            else        ap.Disengage();
        }

        private void OnHdgHold()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;
            bool engage = !(ap.IsEngaged && ap.CurrentMode == AutopilotMode.HeadingHold);
            if (engage) ap.Engage(AutopilotMode.HeadingHold);
            else        ap.Disengage();
        }

        private void OnSpdHold()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;
            bool engage = !(ap.IsEngaged && ap.CurrentMode == AutopilotMode.SpeedHold);
            if (engage) ap.Engage(AutopilotMode.SpeedHold);
            else        ap.Disengage();
        }

        private void OnApToggle()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;
            if (ap.IsEngaged) ap.Disengage();
            else              ap.Engage(AutopilotMode.FullAutopilot);
        }

        private void OnApproach()
        {
            AutopilotController ap = AutopilotController.Instance;
            if (ap == null) return;
            // Find nearest airport and begin approach
            var registry = Landing.AirportRegistry.Instance;
            if (registry == null) return;
            Vector3 pos = ap.transform.position;
            var nearest = registry.GetNearestAirport(pos);
            if (nearest != null) ap.StartApproach(nearest);
        }
        #endregion

        #region Helpers
        private static string Loc(string key)
        {
            var lm = LocalizationManager.Instance;
            return lm != null ? lm.GetText(key) : key;
        }

        private static string ModeKey(AutopilotMode mode)
        {
            switch (mode)
            {
                case AutopilotMode.AltitudeHold:   return "ap_mode_altitude_hold";
                case AutopilotMode.HeadingHold:    return "ap_mode_heading_hold";
                case AutopilotMode.SpeedHold:      return "ap_mode_speed_hold";
                case AutopilotMode.RouteFollow:    return "ap_mode_route_follow";
                case AutopilotMode.ApproachAssist: return "ap_mode_approach";
                case AutopilotMode.FullAutopilot:  return "ap_mode_full";
                default:                           return "ap_mode_off";
            }
        }

        private static string PhaseKey(ApproachPhase phase)
        {
            switch (phase)
            {
                case ApproachPhase.Intercept:  return "ap_approach_intercept";
                case ApproachPhase.Glideslope: return "ap_approach_glideslope";
                case ApproachPhase.Flare:      return "ap_approach_flare";
                case ApproachPhase.Rollout:    return "ap_approach_rollout";
                default:                       return "ap_mode_off";
            }
        }
        #endregion
    }
}
