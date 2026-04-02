// FlightPlanUI.cs — SWEF Advanced Navigation & Flight Plan System (Phase 87)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SWEF.FlightPlan
{
    /// <summary>
    /// Phase 87 — Pre-flight plan creation and editing UI.
    ///
    /// <para>Provides departure/arrival airport search, SID/STAR dropdown selection,
    /// an editable waypoint list, fuel/time/distance summary, flight-rule selector,
    /// per-waypoint altitude and speed constraint fields, and File/Validate buttons.</para>
    ///
    /// <para>Bind UI elements via the Inspector and ensure
    /// <see cref="FlightPlanManager"/> and <see cref="ProcedureGenerator"/> are
    /// present in the scene.</para>
    /// </summary>
    public class FlightPlanUI : MonoBehaviour
    {
        #region Inspector — Panel Roots

        [Header("Panel Roots")]
        public GameObject panelRoot;
        public GameObject waypointDetailPanel;

        #endregion

        #region Inspector — Airport Search

        [Header("Airport Search")]
        public TMP_InputField departureInputField;
        public TMP_InputField arrivalInputField;
        public TMP_InputField alternateInputField;
        public Button createPlanButton;

        #endregion

        #region Inspector — Procedures

        [Header("SID / STAR Dropdowns")]
        public TMP_Dropdown sidDropdown;
        public TMP_Dropdown starDropdown;
        public Button applySIDButton;
        public Button applySTARButton;

        #endregion

        #region Inspector — Waypoint List

        [Header("Waypoint List")]
        public Transform waypointListParent;
        public GameObject waypointRowPrefab;
        public Button addWaypointButton;

        #endregion

        #region Inspector — Summary

        [Header("Summary Labels")]
        public TMP_Text labelTotalDistance;
        public TMP_Text labelETE;
        public TMP_Text labelFuelRequired;
        public TMP_Text labelFuelOnBoard;
        public TMP_Text labelPaxCargo;

        #endregion

        #region Inspector — Flight Rule & Performance

        [Header("Flight Rule")]
        public TMP_Dropdown flightRuleDropdown;

        [Header("Performance")]
        public TMP_InputField cruiseAltitudeField;
        public TMP_InputField cruiseSpeedField;
        public TMP_InputField callsignField;
        public TMP_InputField fuelOnBoardField;
        public TMP_InputField paxCountField;
        public TMP_InputField cargoWeightField;
        public TMP_InputField remarksField;

        #endregion

        #region Inspector — Action Buttons

        [Header("Action Buttons")]
        public Button fileButton;
        public Button validateButton;
        public Button activateButton;
        public Button savePlanButton;

        #endregion

        #region Inspector — Plan Library

        [Header("Plan Library")]
        public Transform planLibraryParent;
        public GameObject planLibraryRowPrefab;

        #endregion

        #region Inspector — Status

        [Header("Status")]
        public TMP_Text statusLabel;

        #endregion

        #region Private State

        private ProcedureGenerator _procedureGen;
        private int _selectedWaypointIndex = -1;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _procedureGen = FindFirstObjectByType<ProcedureGenerator>();

            // Wire buttons
            if (createPlanButton)   createPlanButton.onClick.AddListener(OnCreatePlanClicked);
            if (applySIDButton)     applySIDButton.onClick.AddListener(OnApplySIDClicked);
            if (applySTARButton)    applySTARButton.onClick.AddListener(OnApplySTARClicked);
            if (fileButton)         fileButton.onClick.AddListener(OnFileClicked);
            if (validateButton)     validateButton.onClick.AddListener(OnValidateClicked);
            if (activateButton)     activateButton.onClick.AddListener(OnActivateClicked);
            if (savePlanButton)     savePlanButton.onClick.AddListener(OnSavePlanClicked);
            if (addWaypointButton)  addWaypointButton.onClick.AddListener(OnAddWaypointClicked);
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        #endregion

        #region Button Handlers

        private void OnCreatePlanClicked()
        {
            string dep = departureInputField != null ? departureInputField.text.Trim().ToUpper() : string.Empty;
            string arr = arrivalInputField   != null ? arrivalInputField.text.Trim().ToUpper()   : string.Empty;

            if (string.IsNullOrEmpty(dep) || string.IsNullOrEmpty(arr))
            {
                SetStatus("Enter departure and arrival airports.");
                return;
            }

            if (FlightPlanManager.Instance == null) return;
            FlightPlanManager.Instance.CreatePlan(dep, arr);

            // Apply callsign if entered
            if (callsignField != null && FlightPlanManager.Instance.activePlan != null)
                FlightPlanManager.Instance.activePlan.callsign = callsignField.text.Trim().ToUpper();

            PopulateProcedureDropdowns(dep, arr);
            RefreshAll();
            SetStatus($"Plan created: {dep} → {arr}");
        }

        private void OnApplySIDClicked()
        {
            if (FlightPlanManager.Instance?.activePlan == null || _procedureGen == null) return;

            string sidName = sidDropdown != null && sidDropdown.options.Count > sidDropdown.value
                ? sidDropdown.options[sidDropdown.value].text
                : string.Empty;
            if (string.IsNullOrEmpty(sidName)) return;

            var plan = FlightPlanManager.Instance.activePlan;
            var sidWps = _procedureGen.GenerateSID(plan.departureAirport, "00", sidName);
            plan.departureSID = sidName;

            // Insert SID waypoints after departure airport waypoint
            for (int i = 0; i < sidWps.Count; i++)
                FlightPlanManager.Instance.InsertWaypoint(sidWps[i], 1 + i);

            RefreshAll();
            SetStatus($"SID applied: {sidName}");
        }

        private void OnApplySTARClicked()
        {
            if (FlightPlanManager.Instance?.activePlan == null || _procedureGen == null) return;

            string starName = starDropdown != null && starDropdown.options.Count > starDropdown.value
                ? starDropdown.options[starDropdown.value].text
                : string.Empty;
            if (string.IsNullOrEmpty(starName)) return;

            var plan    = FlightPlanManager.Instance.activePlan;
            var starWps = _procedureGen.GenerateSTAR(plan.arrivalAirport, "00", starName);
            plan.arrivalSTAR = starName;

            // Insert STAR waypoints before the last (destination) waypoint
            int insertIdx = Mathf.Max(0, plan.waypoints.Count - 1);
            for (int i = 0; i < starWps.Count; i++)
                FlightPlanManager.Instance.InsertWaypoint(starWps[i], insertIdx + i);

            RefreshAll();
            SetStatus($"STAR applied: {starName}");
        }

        private void OnFileClicked()
        {
            CommitPerformanceFields();
            if (FlightPlanManager.Instance == null) return;
            FlightPlanManager.Instance.FileFlightPlan();
            RefreshAll();
            SetStatus("Flight plan filed.");
        }

        private void OnValidateClicked()
        {
            CommitPerformanceFields();
            if (FlightPlanManager.Instance == null) return;
            bool ok = FlightPlanManager.Instance.ValidatePlan();
            SetStatus(ok ? "Plan is valid ✓" : "Validation failed — check fuel and route.");
        }

        private void OnActivateClicked()
        {
            if (FlightPlanManager.Instance == null) return;
            FlightPlanManager.Instance.ActivatePlan();
            RefreshAll();
            SetStatus("Plan activated — navigation started.");
        }

        private void OnSavePlanClicked()
        {
            if (FlightPlanManager.Instance == null) return;
            FlightPlanManager.Instance.SaveActivePlan();
            RefreshPlanLibrary();
            SetStatus("Plan saved to library.");
        }

        private void OnAddWaypointClicked()
        {
            if (FlightPlanManager.Instance?.activePlan == null) return;

            var wp = new FlightPlanWaypoint
            {
                waypointId = "WPT" + FlightPlanManager.Instance.activePlan.waypoints.Count,
                name       = "WPT" + FlightPlanManager.Instance.activePlan.waypoints.Count,
                category   = WaypointCategory.GPS,
                legType    = LegType.DirectTo
            };
            FlightPlanManager.Instance.InsertWaypoint(
                wp, FlightPlanManager.Instance.activePlan.waypoints.Count);
            RefreshWaypointList();
        }

        #endregion

        #region Refresh Helpers

        private void RefreshAll()
        {
            RefreshWaypointList();
            RefreshSummary();
            RefreshStatusLabel();
            RefreshPlanLibrary();
            RefreshPerformanceFields();
        }

        private void RefreshWaypointList()
        {
            if (waypointListParent == null || waypointRowPrefab == null) return;

            // Clear existing rows
            foreach (Transform child in waypointListParent)
                Destroy(child.gameObject);

            var plan = FlightPlanManager.Instance?.activePlan;
            if (plan == null) return;

            for (int i = 0; i < plan.waypoints.Count; i++)
            {
                var wp  = plan.waypoints[i];
                var row = Instantiate(waypointRowPrefab, waypointListParent);

                // Populate row labels via children named "LabelName", "LabelAlt", "LabelSpd"
                SetChildText(row, "LabelName",  wp.name);
                SetChildText(row, "LabelAlt",   wp.altitude > 0 ? $"FL{wp.altitude / 100:0}" : "—");
                SetChildText(row, "LabelSpd",   wp.speedConstraint > 0 ? $"{wp.speedConstraint:0}kts" : "—");
                SetChildText(row, "LabelType",  wp.category.ToString());

                // Delete button
                int idx = i;
                var del = row.transform.Find("DeleteButton")?.GetComponent<Button>();
                if (del != null)
                    del.onClick.AddListener(() =>
                    {
                        FlightPlanManager.Instance.RemoveWaypoint(idx);
                        RefreshAll();
                    });
            }
        }

        private void RefreshSummary()
        {
            var plan = FlightPlanManager.Instance?.activePlan;
            if (plan == null)
            {
                if (labelTotalDistance) labelTotalDistance.text = "—";
                if (labelETE)           labelETE.text           = "—";
                if (labelFuelRequired)  labelFuelRequired.text  = "—";
                return;
            }

            if (labelTotalDistance) labelTotalDistance.text = $"{plan.totalDistanceNm:F0} nm";
            if (labelETE)
            {
                int h = (int)(plan.estimatedTimeEnRoute / 60);
                int m = (int)(plan.estimatedTimeEnRoute % 60);
                labelETE.text = $"{h}h {m:00}m";
            }
            if (labelFuelRequired)  labelFuelRequired.text  = $"{plan.fuelRequired:F0} kg";
            if (labelFuelOnBoard)   labelFuelOnBoard.text   = $"{plan.fuelOnBoard:F0} kg";
            if (labelPaxCargo)      labelPaxCargo.text      = $"{plan.paxCount} pax / {plan.cargoWeight:F0} kg";
        }

        private void RefreshStatusLabel()
        {
            var status = FlightPlanManager.Instance?.currentStatus ?? FlightPlanStatus.Draft;
            SetStatus(status.ToString());
        }

        private void RefreshPlanLibrary()
        {
            if (planLibraryParent == null || planLibraryRowPrefab == null) return;
            foreach (Transform child in planLibraryParent)
                Destroy(child.gameObject);

            if (FlightPlanManager.Instance == null) return;
            foreach (var plan in FlightPlanManager.Instance.savedPlans)
            {
                var row = Instantiate(planLibraryRowPrefab, planLibraryParent);
                SetChildText(row, "LabelPlanId", plan.planId?.Length >= 8 ? plan.planId.Substring(0, 8) : plan.planId);
                SetChildText(row, "LabelRoute",  $"{plan.departureAirport} → {plan.arrivalAirport}");

                var loadBtn = row.transform.Find("LoadButton")?.GetComponent<Button>();
                if (loadBtn != null)
                {
                    var localPlan = plan;
                    loadBtn.onClick.AddListener(() =>
                    {
                        FlightPlanManager.Instance.LoadPlan(localPlan);
                        RefreshAll();
                    });
                }
            }
        }

        private void RefreshPerformanceFields()
        {
            var plan = FlightPlanManager.Instance?.activePlan;
            if (plan == null) return;

            if (cruiseAltitudeField && !cruiseAltitudeField.isFocused)
                cruiseAltitudeField.text = plan.cruiseAltitude.ToString("F0");
            if (cruiseSpeedField && !cruiseSpeedField.isFocused)
                cruiseSpeedField.text = plan.cruiseSpeed.ToString("F0");
            if (callsignField && !callsignField.isFocused)
                callsignField.text = plan.callsign;
            if (fuelOnBoardField && !fuelOnBoardField.isFocused)
                fuelOnBoardField.text = plan.fuelOnBoard.ToString("F0");
            if (paxCountField && !paxCountField.isFocused)
                paxCountField.text = plan.paxCount.ToString();
            if (cargoWeightField && !cargoWeightField.isFocused)
                cargoWeightField.text = plan.cargoWeight.ToString("F0");
            if (remarksField && !remarksField.isFocused)
                remarksField.text = plan.remarks;

            if (flightRuleDropdown)
            {
                // Ensure dropdown options match enum
                if (flightRuleDropdown.options.Count == 0)
                {
                    flightRuleDropdown.ClearOptions();
                    foreach (FlightRuleType frt in System.Enum.GetValues(typeof(FlightRuleType)))
                        flightRuleDropdown.options.Add(new TMP_Dropdown.OptionData(frt.ToString()));
                    flightRuleDropdown.RefreshShownValue();
                }
                flightRuleDropdown.value = (int)plan.flightRule;
                flightRuleDropdown.onValueChanged.RemoveAllListeners();
                flightRuleDropdown.onValueChanged.AddListener(v =>
                {
                    if (FlightPlanManager.Instance?.activePlan != null)
                        FlightPlanManager.Instance.activePlan.flightRule = (FlightRuleType)v;
                });
            }
        }

        private void CommitPerformanceFields()
        {
            var plan = FlightPlanManager.Instance?.activePlan;
            if (plan == null) return;

            if (cruiseAltitudeField && float.TryParse(cruiseAltitudeField.text, out float alt))
                plan.cruiseAltitude = alt;
            if (cruiseSpeedField && float.TryParse(cruiseSpeedField.text, out float spd))
                plan.cruiseSpeed = spd;
            if (callsignField)
                plan.callsign = callsignField.text.Trim().ToUpper();
            if (fuelOnBoardField && float.TryParse(fuelOnBoardField.text, out float fob))
                plan.fuelOnBoard = fob;
            if (paxCountField && int.TryParse(paxCountField.text, out int pax))
                plan.paxCount = pax;
            if (cargoWeightField && float.TryParse(cargoWeightField.text, out float cargo))
                plan.cargoWeight = cargo;
            if (remarksField)
                plan.remarks = remarksField.text;
            if (alternateInputField)
                plan.alternateAirport = alternateInputField.text.Trim().ToUpper();
        }

        private void PopulateProcedureDropdowns(string departureIcao, string arrivalIcao)
        {
            if (_procedureGen == null) return;

            // SID dropdown
            if (sidDropdown != null && NavigationDatabase.Instance != null)
            {
                sidDropdown.ClearOptions();
                var sids = NavigationDatabase.Instance.GetProceduresForAirport(departureIcao, ProcedureType.SID);
                var opts = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("(None)") };
                foreach (var s in sids)
                    opts.Add(new TMP_Dropdown.OptionData(s.procedureName));
                sidDropdown.AddOptions(opts);
            }

            // STAR dropdown
            if (starDropdown != null && NavigationDatabase.Instance != null)
            {
                starDropdown.ClearOptions();
                var stars = NavigationDatabase.Instance.GetProceduresForAirport(arrivalIcao, ProcedureType.STAR);
                var opts = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("(None)") };
                foreach (var s in stars)
                    opts.Add(new TMP_Dropdown.OptionData(s.procedureName));
                starDropdown.AddOptions(opts);
            }
        }

        private void SetStatus(string msg)
        {
            if (statusLabel) statusLabel.text = msg;
        }

        private static void SetChildText(GameObject row, string childName, string text)
        {
            var t = row.transform.Find(childName);
            if (t == null) return;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp) tmp.text = text;
        }

        #endregion

        #region Public API

        /// <summary>Toggles the root panel visibility.</summary>
        public void Show() { if (panelRoot) panelRoot.SetActive(true); }

        /// <summary>Hides the root panel.</summary>
        public void Hide() { if (panelRoot) panelRoot.SetActive(false); }

        /// <summary>Toggles the root panel.</summary>
        public void Toggle() { if (panelRoot) panelRoot.SetActive(!panelRoot.activeSelf); }

        #endregion
    }
}
