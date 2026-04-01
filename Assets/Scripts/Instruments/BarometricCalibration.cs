using UnityEngine;
using System;

public class BarometricCalibration : MonoBehaviour
{
    [Header("Barometric Settings")]
    [SerializeField] private float standardPressureHPa = 1013.25f;
    [SerializeField] private float currentQNH = 1013.25f;
    [SerializeField] private float pressureChangeRate = 0.5f; // hPa per hour simulated drift

    [Header("References")]
    [SerializeField] private FlightInstrument altimeter;

    private float fieldElevation;
    private float qfe;
    private BarometricMode currentMode = BarometricMode.QNH;

    public event Action<float> OnQNHChanged;
    public event Action<BarometricMode> OnModeChanged;

    public float CurrentQNH => currentQNH;
    public float CurrentQFE => qfe;
    public float StandardPressure => standardPressureHPa;
    public BarometricMode CurrentMode => currentMode;

    public float AltitudeCorrection =>
        (currentQNH - standardPressureHPa) * 30f; // ~30 ft per hPa

    private void Update()
    {
        // Simulate slow pressure drift
        float drift = pressureChangeRate / 3600f * Time.deltaTime;
        currentQNH += drift * Mathf.Sin(Time.time * 0.01f); // gentle oscillation
        qfe = currentQNH - (fieldElevation / 30f); // approximate

        if (altimeter != null)
        {
            float correctedAlt = altimeter.CurrentValue + AltitudeCorrection;
            altimeter.SetTargetValue(correctedAlt);
        }
    }

    public void SetQNH(float qnh)
    {
        currentQNH = qnh;
        currentMode = BarometricMode.QNH;
        OnQNHChanged?.Invoke(currentQNH);
        OnModeChanged?.Invoke(currentMode);
    }

    public void SetStandardPressure()
    {
        currentQNH = standardPressureHPa;
        currentMode = BarometricMode.Standard;
        OnQNHChanged?.Invoke(currentQNH);
        OnModeChanged?.Invoke(currentMode);
    }

    public void SetQFE(float elevation)
    {
        fieldElevation = elevation;
        qfe = currentQNH - (elevation / 30f);
        currentMode = BarometricMode.QFE;
        OnModeChanged?.Invoke(currentMode);
    }

    public void SyncFromATIS(float atisQNH)
    {
        SetQNH(atisQNH);
    }
}

public enum BarometricMode
{
    QNH,      // Regional pressure at sea level
    QFE,      // Field elevation pressure
    Standard  // 1013.25 hPa (FL usage)
}
