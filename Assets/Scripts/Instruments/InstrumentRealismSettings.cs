using UnityEngine;
using System;

[CreateAssetMenu(fileName = "InstrumentRealismSettings", menuName = "SWEF/Instruments/RealismSettings")]
public class InstrumentRealismSettings : ScriptableObject
{
    [Header("Realism Level")]
    public RealismLevel realismLevel = RealismLevel.Realistic;

    [Header("Casual Overrides")]
    public bool disableCalibrationDrift = false;
    public bool disableInstrumentFailures = true;
    public bool disableResponseLag = false;

    [Header("Realistic Settings")]
    public float globalDriftMultiplier = 1f;
    public float globalFailureMultiplier = 1f;
    public float globalLagMultiplier = 1f;

    [Header("Hardcore Settings")]
    public bool requireManualCalibration = true;
    public bool enablePartialFailures = true;
    public float hardcoreFailureMultiplier = 2f;

    public event Action<RealismLevel> OnRealismLevelChanged;

    public void SetRealismLevel(RealismLevel level)
    {
        realismLevel = level;
        ApplyPreset(level);
        OnRealismLevelChanged?.Invoke(level);
    }

    private void ApplyPreset(RealismLevel level)
    {
        switch (level)
        {
            case RealismLevel.Casual:
                disableCalibrationDrift = true;
                disableInstrumentFailures = true;
                disableResponseLag = true;
                globalDriftMultiplier = 0f;
                globalFailureMultiplier = 0f;
                globalLagMultiplier = 0f;
                break;
            case RealismLevel.Realistic:
                disableCalibrationDrift = false;
                disableInstrumentFailures = false;
                disableResponseLag = false;
                globalDriftMultiplier = 1f;
                globalFailureMultiplier = 1f;
                globalLagMultiplier = 1f;
                requireManualCalibration = false;
                enablePartialFailures = false;
                break;
            case RealismLevel.Hardcore:
                disableCalibrationDrift = false;
                disableInstrumentFailures = false;
                disableResponseLag = false;
                globalDriftMultiplier = 1.5f;
                globalFailureMultiplier = hardcoreFailureMultiplier;
                globalLagMultiplier = 1.2f;
                requireManualCalibration = true;
                enablePartialFailures = true;
                break;
        }
    }
}

public enum RealismLevel
{
    Casual,
    Realistic,
    Hardcore
}
