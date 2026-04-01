using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InstrumentCalibrationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject calibrationPanel;
    [SerializeField] private TMP_Text instrumentNameText;
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider qnhSlider;
    [SerializeField] private TMP_Text qnhValueText;
    [SerializeField] private Button calibrateButton;
    [SerializeField] private Button calibrateAllButton;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text panelHealthText;
    [SerializeField] private Transform failedInstrumentList;
    [SerializeField] private GameObject failedInstrumentItemPrefab;

    [Header("References")]
    [SerializeField] private InstrumentPanel instrumentPanel;
    [SerializeField] private BarometricCalibration barometricCalibration;

    private FlightInstrument selectedInstrument;

    private void Start()
    {
        calibrateButton?.onClick.AddListener(OnCalibrateClicked);
        calibrateAllButton?.onClick.AddListener(OnCalibrateAllClicked);
        repairButton?.onClick.AddListener(OnRepairClicked);
        closeButton?.onClick.AddListener(() => calibrationPanel?.SetActive(false));

        if (qnhSlider != null)
        {
            qnhSlider.minValue = 940f;
            qnhSlider.maxValue = 1060f;
            qnhSlider.value = barometricCalibration != null ? barometricCalibration.CurrentQNH : 1013.25f;
            qnhSlider.onValueChanged.AddListener(OnQNHSliderChanged);
        }

        if (instrumentPanel != null)
        {
            instrumentPanel.OnInstrumentFailed += HandleInstrumentFailed;
            instrumentPanel.OnInstrumentCalibrationNeeded += HandleCalibrationNeeded;
        }

        calibrationPanel?.SetActive(false);
    }

    public void OpenPanel()
    {
        calibrationPanel?.SetActive(true);
        RefreshUI();
    }

    public void SelectInstrument(FlightInstrument instrument)
    {
        selectedInstrument = instrument;
        RefreshInstrumentInfo();
    }

    private void Update()
    {
        if (calibrationPanel != null && calibrationPanel.activeSelf)
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (instrumentPanel != null && panelHealthText != null)
            panelHealthText.text = $"Panel Health: {instrumentPanel.GetOverallHealth() * 100f:F0}%";

        RefreshInstrumentInfo();
        RefreshFailedList();
    }

    private void RefreshInstrumentInfo()
    {
        if (selectedInstrument == null) return;

        if (instrumentNameText != null)
            instrumentNameText.text = selectedInstrument.Config.instrumentName;
        if (currentValueText != null)
            currentValueText.text = $"{selectedInstrument.CurrentValue:F1} {selectedInstrument.Config.displayUnit}";
        if (accuracyText != null)
            accuracyText.text = $"Accuracy: {selectedInstrument.CalibrationAccuracy * 100f:F1}%";
        if (statusText != null)
            statusText.text = selectedInstrument.IsFailed
                ? $"⚠ FAILED: {selectedInstrument.ActiveFailure}"
                : "✅ Operational";

        if (repairButton != null)
            repairButton.interactable = selectedInstrument.IsFailed;
    }

    private void RefreshFailedList()
    {
        if (failedInstrumentList == null || instrumentPanel == null) return;

        foreach (Transform child in failedInstrumentList)
            Destroy(child.gameObject);

        var failed = instrumentPanel.GetFailedInstruments();
        foreach (var inst in failed)
        {
            if (failedInstrumentItemPrefab == null) continue;
            var item = Instantiate(failedInstrumentItemPrefab, failedInstrumentList);
            var text = item.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = $"{inst.Config.instrumentName} — {inst.ActiveFailure}";
        }
    }

    private void OnCalibrateClicked()
    {
        selectedInstrument?.Calibrate();
        RefreshInstrumentInfo();
    }

    private void OnCalibrateAllClicked()
    {
        instrumentPanel?.CalibrateAll();
        RefreshUI();
    }

    private void OnRepairClicked()
    {
        selectedInstrument?.Repair();
        RefreshInstrumentInfo();
    }

    private void OnQNHSliderChanged(float value)
    {
        barometricCalibration?.SetQNH(value);
        if (qnhValueText != null)
            qnhValueText.text = $"QNH: {value:F1} hPa";
    }

    private void HandleInstrumentFailed(FlightInstrument inst, InstrumentFailureMode mode)
    {
        Debug.LogWarning($"[SWEF] Instrument Failed: {inst.Config.instrumentName} — {mode}");
    }

    private void HandleCalibrationNeeded(FlightInstrument inst)
    {
        Debug.Log($"[SWEF] Calibration needed: {inst.Config.instrumentName}");
    }
}
