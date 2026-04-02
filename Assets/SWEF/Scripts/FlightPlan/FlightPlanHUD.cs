// FlightPlanHUD.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — In-flight HUD overlay for the active flight plan.
    ///
    /// <para>Displays: active waypoint info, LNAV/VNAV mode indicators, cross-track /
    /// vertical deviation bars, TOD/TOC markers, fuel range ring, next-waypoint
    /// directional arrow (screen-edge clamped), per-waypoint speed/altitude constraints,
    /// waypoint progress bar, and alert banners for
    /// <see cref="FlightPlanAlertType"/> events.</para>
    ///
    /// <para>Attach to a persistent Canvas GameObject in the scene.</para>
    /// </summary>
    public class FlightPlanHUD : MonoBehaviour
    {
        #region Inspector — Panels

        [Header("Panels")]
        public GameObject hudRoot;
        public bool compactMode;

        #endregion

        #region Inspector — Waypoint Info

        [Header("Waypoint Info")]
        public TMP_Text waypointNameLabel;
        public TMP_Text waypointDistanceLabel;
        public TMP_Text waypointETALabel;
        public TMP_Text waypointAltConstraintLabel;
        public TMP_Text waypointSpdConstraintLabel;

        #endregion

        #region Inspector — Mode Indicators

        [Header("Mode Indicators")]
        public TMP_Text lnavModeLabel;
        public TMP_Text vnavModeLabel;
        public Image    lnavModeBackground;
        public Image    vnavModeBackground;

        [Header("Mode Colors")]
        public Color colorEngaged   = new Color(0.2f, 0.9f, 0.2f);
        public Color colorArmed     = new Color(0.9f, 0.9f, 0.2f);
        public Color colorOff       = new Color(0.4f, 0.4f, 0.4f);

        #endregion

        #region Inspector — Deviation Bars

        [Header("Deviation Bars")]
        public RectTransform xtkDeviationIndicator;
        public RectTransform vertDeviationIndicator;
        [Tooltip("Max bar travel in pixels for full-scale deviation.")]
        public float deviationBarMaxPx = 80f;

        #endregion

        #region Inspector — TOD / TOC Markers

        [Header("TOD / TOC")]
        public TMP_Text todLabel;
        public TMP_Text tocLabel;

        #endregion

        #region Inspector — Fuel

        [Header("Fuel")]
        public TMP_Text fuelRemainingLabel;
        public TMP_Text fuelRangeLabel;

        #endregion

        #region Inspector — Directional Arrow

        [Header("Directional Arrow")]
        public RectTransform waypointArrow;
        [Tooltip("Camera used for screen-space arrow projection.")]
        public Camera hudCamera;

        #endregion

        #region Inspector — Progress Bar

        [Header("Progress Bar")]
        public Slider progressBar;
        public TMP_Text progressLabel;

        #endregion

        #region Inspector — Alert Banner

        [Header("Alert Banner")]
        public GameObject alertBanner;
        public TMP_Text   alertLabel;
        public float alertDisplaySeconds = 4f;

        #endregion

        #region Private State

        private FMSController _fms;
        private Coroutine _alertCoroutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _fms = FindFirstObjectByType<FMSController>();
            if (hudCamera == null) hudCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (FlightPlanManager.Instance != null)
            {
                FlightPlanManager.Instance.OnPlanAlert       += OnAlert;
                FlightPlanManager.Instance.OnWaypointCaptured += OnWaypointCaptured;
            }
        }

        private void OnDisable()
        {
            if (FlightPlanManager.Instance != null)
            {
                FlightPlanManager.Instance.OnPlanAlert       -= OnAlert;
                FlightPlanManager.Instance.OnWaypointCaptured -= OnWaypointCaptured;
            }
        }

        private void Update()
        {
            if (FlightPlanManager.Instance?.activePlan == null
                || FlightPlanManager.Instance.activePlan.status != FlightPlanStatus.Active)
            {
                if (hudRoot) hudRoot.SetActive(false);
                return;
            }

            if (hudRoot) hudRoot.SetActive(true);

            RefreshWaypointInfo();
            RefreshModeIndicators();
            RefreshDeviationBars();
            RefreshTODTOC();
            RefreshFuel();
            RefreshDirectionalArrow();
            RefreshProgress();
        }

        #endregion

        #region Refresh Methods

        private void RefreshWaypointInfo()
        {
            var wp  = FlightPlanManager.Instance.ActiveWaypoint;
            if (wp == null) return;

            if (waypointNameLabel)
                waypointNameLabel.text = wp.name;

            if (waypointDistanceLabel)
            {
                float d = FlightPlanManager.Instance.GetDistanceToNextNm();
                waypointDistanceLabel.text = $"{d:F1} nm";
            }

            if (waypointETALabel)
            {
                float eta = FlightPlanManager.Instance.GetETAMinutes();
                if (eta >= float.MaxValue)
                    waypointETALabel.text = "—";
                else
                {
                    int h = (int)(eta / 60);
                    int m = (int)(eta % 60);
                    waypointETALabel.text = h > 0 ? $"{h}h{m:00}m" : $"{m}m";
                }
            }

            if (waypointAltConstraintLabel)
                waypointAltConstraintLabel.text = wp.altitude > 0 ? $"FL{wp.altitude / 100:0}" : "—";

            if (waypointSpdConstraintLabel)
                waypointSpdConstraintLabel.text = wp.speedConstraint > 0 ? $"{wp.speedConstraint:0}kts" : "—";
        }

        private void RefreshModeIndicators()
        {
            if (_fms == null)
            {
                _fms = FindFirstObjectByType<FMSController>();
                if (_fms == null) return;
            }

            string modeStr = _fms.currentMode.ToString();

            if (lnavModeLabel)     lnavModeLabel.text = compactMode ? "L" : "LNAV";
            if (vnavModeLabel)     vnavModeLabel.text = compactMode ? "V" : "VNAV";

            bool lnavOn = _fms.currentMode == FMSMode.LNAV
                       || _fms.currentMode == FMSMode.LNAVAndVNAV
                       || _fms.currentMode == FMSMode.Approach;
            bool vnavOn = _fms.currentMode == FMSMode.VNAV
                       || _fms.currentMode == FMSMode.LNAVAndVNAV
                       || _fms.currentMode == FMSMode.Approach;

            if (lnavModeBackground) lnavModeBackground.color = lnavOn ? colorEngaged : colorOff;
            if (vnavModeBackground) vnavModeBackground.color = vnavOn ? colorEngaged : colorOff;
        }

        private void RefreshDeviationBars()
        {
            if (_fms == null) return;

            // Cross-track (lateral) bar
            if (xtkDeviationIndicator != null)
            {
                float xtkPx = Mathf.Clamp(
                    _fms.crossTrackErrorNm / FlightPlanConfig.XTKErrorMaxCorrectionNm,
                    -1f, 1f) * deviationBarMaxPx;
                var pos = xtkDeviationIndicator.anchoredPosition;
                pos.x = xtkPx;
                xtkDeviationIndicator.anchoredPosition = pos;
            }

            // Vertical deviation bar
            if (vertDeviationIndicator != null)
            {
                float vDevPx = Mathf.Clamp(
                    _fms.verticalDeviationFt / (FlightPlanConfig.VNAVAltitudeDeviationFt * 2f),
                    -1f, 1f) * deviationBarMaxPx;
                var pos = vertDeviationIndicator.anchoredPosition;
                pos.y = -vDevPx; // positive deviation = above glidepath = bar up
                vertDeviationIndicator.anchoredPosition = pos;
            }
        }

        private void RefreshTODTOC()
        {
            if (_fms == null) return;

            if (todLabel)
            {
                float tod = _fms.distanceToTODNm;
                todLabel.text = tod > 0 ? $"TOD {tod:F0}nm" : "TOD";
                todLabel.color = tod <= FlightPlanConfig.WaypointApproachingAlertNm ? colorEngaged : Color.white;
            }

            if (tocLabel)
            {
                float toc = _fms.distanceToTOCNm;
                tocLabel.text = toc > 0 ? $"TOC {toc:F0}nm" : "TOC";
                tocLabel.color = toc <= FlightPlanConfig.WaypointApproachingAlertNm ? colorEngaged : Color.white;
            }
        }

        private void RefreshFuel()
        {
            var plan = FlightPlanManager.Instance.activePlan;

            if (fuelRemainingLabel)
                fuelRemainingLabel.text = $"{plan.fuelOnBoard:F0} kg";

            if (fuelRangeLabel)
            {
                float rangeNm = FuelCalculator.CalculateRange(plan.fuelOnBoard,
                                                              plan.cruiseAltitude,
                                                              plan.cruiseSpeed);
                fuelRangeLabel.text = $"{rangeNm:F0} nm range";
            }
        }

        private void RefreshDirectionalArrow()
        {
            if (waypointArrow == null || hudCamera == null) return;
            var wp = FlightPlanManager.Instance.ActiveWaypoint;
            if (wp == null) { waypointArrow.gameObject.SetActive(false); return; }

            // Convert lat/lon to world position
            var worldPos = new Vector3(
                (float)(wp.longitude * 111320.0),
                wp.altitude * 0.3048f,
                (float)(wp.latitude  * 111320.0));

            Vector3 screenPos = hudCamera.WorldToScreenPoint(worldPos);
            bool    inFront   = screenPos.z > 0f;

            if (!inFront) screenPos *= -1f;

            Rect  canvasRect = (transform.root.GetComponent<RectTransform>())?.rect
                               ?? new Rect(0, 0, Screen.width, Screen.height);
            float hw = canvasRect.width  / 2f;
            float hh = canvasRect.height / 2f;
            float margin = 60f;

            Vector2 centred   = new Vector2(screenPos.x - Screen.width  / 2f,
                                             screenPos.y - Screen.height / 2f);

            bool onScreen = Mathf.Abs(centred.x) < hw - margin
                         && Mathf.Abs(centred.y) < hh - margin;

            waypointArrow.gameObject.SetActive(!onScreen || !inFront);
            if (!onScreen || !inFront)
            {
                // Clamp to screen edge
                float   angle = Mathf.Atan2(centred.y, centred.x);
                float   cos   = Mathf.Cos(angle);
                float   sin   = Mathf.Sin(angle);
                float   scaleX = cos != 0 ? (hw - margin) / Mathf.Abs(cos) : float.MaxValue;
                float   scaleY = sin != 0 ? (hh - margin) / Mathf.Abs(sin) : float.MaxValue;
                float   scale  = Mathf.Min(scaleX, scaleY);
                centred = new Vector2(cos * scale, sin * scale);

                waypointArrow.anchoredPosition = centred;
                waypointArrow.localEulerAngles = new Vector3(0f, 0f, angle * Mathf.Rad2Deg);
            }
        }

        private void RefreshProgress()
        {
            var mgr = FlightPlanManager.Instance;
            if (mgr.activePlan == null) return;

            int total = mgr.activePlan.waypoints.Count;
            int done  = Mathf.Clamp(mgr.activeWaypointIndex, 0, total);

            if (progressBar)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = Mathf.Max(1f, total - 1);
                progressBar.value    = done;
            }

            if (progressLabel)
                progressLabel.text = $"{done}/{total}";
        }

        #endregion

        #region Alert Banner

        private void OnAlert(FlightPlanAlertType alert)
        {
            string msg = alert switch
            {
                FlightPlanAlertType.WaypointApproaching => "Approaching waypoint",
                FlightPlanAlertType.TopOfDescent        => "Begin descent ↓",
                FlightPlanAlertType.TopOfClimb          => "Top of climb reached ↑",
                FlightPlanAlertType.FuelWarning         => "⚠ Low fuel",
                FlightPlanAlertType.ETAUpdate           => "ETA updated",
                FlightPlanAlertType.WeatherAdvisory     => "⚠ Weather advisory",
                FlightPlanAlertType.DisasterHazard      => "⚠ Hazard zone ahead",
                FlightPlanAlertType.AirspaceEntry       => "Entering new airspace",
                _                                       => alert.ToString()
            };
            ShowAlert(msg);
        }

        private void OnWaypointCaptured(FlightPlanWaypoint wp)
        {
            ShowAlert($"Captured: {wp.name}");
        }

        private void ShowAlert(string message)
        {
            if (alertBanner == null) return;

            if (alertLabel) alertLabel.text = message;
            alertBanner.SetActive(true);

            if (_alertCoroutine != null) StopCoroutine(_alertCoroutine);
            _alertCoroutine = StartCoroutine(HideAlertAfter(alertDisplaySeconds));
        }

        private IEnumerator HideAlertAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (alertBanner) alertBanner.SetActive(false);
        }

        #endregion

        #region Public API

        /// <summary>Toggles compact / full HUD mode.</summary>
        public void ToggleCompactMode()
        {
            compactMode = !compactMode;
        }

        #endregion
    }
}
