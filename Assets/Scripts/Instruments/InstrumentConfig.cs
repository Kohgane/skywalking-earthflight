using UnityEngine;

[CreateAssetMenu(fileName = "InstrumentConfig", menuName = "SWEF/Instruments/InstrumentConfig")]
public class InstrumentConfig : ScriptableObject
{
    [Header("Instrument Identity")]
    public string instrumentName;
    public InstrumentType instrumentType;

    [Header("Calibration")]
    [Range(0f, 1f)] public float calibrationAccuracy = 1f;
    public float calibrationDriftRate = 0.001f; // drift per minute
    public float maxCalibrationOffset = 5f;

    [Header("Response")]
    public float responseLag = 0.1f; // seconds
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float dampingFactor = 0.85f;

    [Header("Failure")]
    [Range(0f, 1f)] public float failureProbability = 0.001f;
    public float meanTimeBetweenFailures = 3600f; // seconds
    public InstrumentFailureMode[] possibleFailureModes;

    [Header("Display")]
    public float minDisplayValue;
    public float maxDisplayValue;
    public float displayPrecision = 0.1f;
    public string displayUnit = "";
}

public enum InstrumentType
{
    Altimeter,
    Airspeed,
    VerticalSpeed,
    Heading,
    Attitude,
    TurnCoordinator,
    BarometricPressure,
    EngineRPM,
    FuelGauge,
    OilPressure,
    OilTemperature,
    Tachometer
}

public enum InstrumentFailureMode
{
    None,
    Frozen,
    Erratic,
    SlowDrift,
    BlackOut,
    StuckAtValue,
    Oscillating
}
