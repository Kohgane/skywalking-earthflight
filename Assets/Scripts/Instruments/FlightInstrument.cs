using UnityEngine;
using System;

public class FlightInstrument : MonoBehaviour
{
    [SerializeField] private InstrumentConfig config;

    // State
    private float currentDisplayValue;
    private float targetValue;
    private float calibrationOffset;
    private float timeSinceCalibration;
    private bool isFailed;
    private InstrumentFailureMode activeFailureMode = InstrumentFailureMode.None;
    private float failureTimestamp;
    private float frozenValue;

    // Events
    public event Action<float> OnValueChanged;
    public event Action<InstrumentFailureMode> OnFailureTriggered;
    public event Action OnCalibrationRequired;
    public event Action OnCalibrated;

    public InstrumentConfig Config => config;
    public float CurrentValue => currentDisplayValue;
    public bool IsFailed => isFailed;
    public InstrumentFailureMode ActiveFailure => activeFailureMode;
    public float CalibrationAccuracy => config != null ? config.calibrationAccuracy - Mathf.Abs(calibrationOffset / config.maxCalibrationOffset) : 0f;

    protected virtual void Update()
    {
        if (config == null) return;

        UpdateCalibrationDrift();
        CheckForFailure();
        UpdateDisplayValue();
    }

    public void SetTargetValue(float value)
    {
        targetValue = value;
    }

    public void Calibrate()
    {
        calibrationOffset = 0f;
        timeSinceCalibration = 0f;
        OnCalibrated?.Invoke();
    }

    public void ForceFailure(InstrumentFailureMode mode)
    {
        isFailed = true;
        activeFailureMode = mode;
        failureTimestamp = Time.time;
        if (mode == InstrumentFailureMode.Frozen || mode == InstrumentFailureMode.StuckAtValue)
            frozenValue = currentDisplayValue;
        OnFailureTriggered?.Invoke(mode);
    }

    public void Repair()
    {
        isFailed = false;
        activeFailureMode = InstrumentFailureMode.None;
    }

    private void UpdateCalibrationDrift()
    {
        timeSinceCalibration += Time.deltaTime;
        float driftPerSecond = config.calibrationDriftRate / 60f;
        calibrationOffset += driftPerSecond * Time.deltaTime * UnityEngine.Random.Range(-1f, 1f);
        calibrationOffset = Mathf.Clamp(calibrationOffset, -config.maxCalibrationOffset, config.maxCalibrationOffset);

        if (Mathf.Abs(calibrationOffset) > config.maxCalibrationOffset * 0.7f)
            OnCalibrationRequired?.Invoke();
    }

    private void CheckForFailure()
    {
        if (isFailed) return;
        if (timeSinceCalibration > config.meanTimeBetweenFailures)
        {
            if (UnityEngine.Random.value < config.failureProbability * Time.deltaTime)
            {
                var modes = config.possibleFailureModes;
                if (modes != null && modes.Length > 0)
                    ForceFailure(modes[UnityEngine.Random.Range(0, modes.Length)]);
            }
        }
    }

    private void UpdateDisplayValue()
    {
        float rawValue = targetValue + calibrationOffset;

        if (isFailed)
        {
            rawValue = ApplyFailureEffect(rawValue);
        }

        // Apply response lag (damping)
        float laggedValue = Mathf.Lerp(currentDisplayValue, rawValue, Time.deltaTime / Mathf.Max(config.responseLag, 0.001f));
        laggedValue *= config.dampingFactor + (1f - config.dampingFactor) * config.responseCurve.Evaluate(
            Mathf.InverseLerp(config.minDisplayValue, config.maxDisplayValue, laggedValue));

        // Clamp and quantize
        laggedValue = Mathf.Clamp(laggedValue, config.minDisplayValue, config.maxDisplayValue);
        laggedValue = Mathf.Round(laggedValue / config.displayPrecision) * config.displayPrecision;

        if (Mathf.Abs(laggedValue - currentDisplayValue) > 0.001f)
        {
            currentDisplayValue = laggedValue;
            OnValueChanged?.Invoke(currentDisplayValue);
        }
    }

    private float ApplyFailureEffect(float value)
    {
        float elapsed = Time.time - failureTimestamp;
        switch (activeFailureMode)
        {
            case InstrumentFailureMode.Frozen:
            case InstrumentFailureMode.StuckAtValue:
                return frozenValue;
            case InstrumentFailureMode.Erratic:
                return value + UnityEngine.Random.Range(-config.maxDisplayValue * 0.3f, config.maxDisplayValue * 0.3f);
            case InstrumentFailureMode.SlowDrift:
                return value + elapsed * config.calibrationDriftRate * 10f;
            case InstrumentFailureMode.BlackOut:
                return 0f;
            case InstrumentFailureMode.Oscillating:
                return value + Mathf.Sin(elapsed * 5f) * config.maxDisplayValue * 0.2f;
            default:
                return value;
        }
    }
}
