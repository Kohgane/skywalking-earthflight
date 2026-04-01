using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class InstrumentCalibrationTests
{
    private GameObject testObj;
    private FlightInstrument instrument;
    private InstrumentConfig config;

    [SetUp]
    public void SetUp()
    {
        testObj = new GameObject("TestInstrument");
        instrument = testObj.AddComponent<FlightInstrument>();

        config = ScriptableObject.CreateInstance<InstrumentConfig>();
        config.instrumentName = "Test Altimeter";
        config.instrumentType = InstrumentType.Altimeter;
        config.calibrationAccuracy = 1f;
        config.calibrationDriftRate = 0.01f;
        config.maxCalibrationOffset = 5f;
        config.responseLag = 0.05f;
        config.responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        config.dampingFactor = 0.9f;
        config.failureProbability = 0f;
        config.meanTimeBetweenFailures = 99999f;
        config.minDisplayValue = 0f;
        config.maxDisplayValue = 50000f;
        config.displayPrecision = 1f;
        config.displayUnit = "ft";

        // Use reflection to set the private config field
        var configField = typeof(FlightInstrument).GetField("config",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        configField?.SetValue(instrument, config);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObj);
        Object.DestroyImmediate(config);
    }

    [Test]
    public void Instrument_InitialValue_IsZero()
    {
        Assert.AreEqual(0f, instrument.CurrentValue);
    }

    [Test]
    public void Instrument_NotFailed_ByDefault()
    {
        Assert.IsFalse(instrument.IsFailed);
        Assert.AreEqual(InstrumentFailureMode.None, instrument.ActiveFailure);
    }

    [Test]
    public void Instrument_ForceFailure_SetsFailedState()
    {
        instrument.ForceFailure(InstrumentFailureMode.Frozen);
        Assert.IsTrue(instrument.IsFailed);
        Assert.AreEqual(InstrumentFailureMode.Frozen, instrument.ActiveFailure);
    }

    [Test]
    public void Instrument_Repair_ClearsFailure()
    {
        instrument.ForceFailure(InstrumentFailureMode.Erratic);
        instrument.Repair();
        Assert.IsFalse(instrument.IsFailed);
        Assert.AreEqual(InstrumentFailureMode.None, instrument.ActiveFailure);
    }

    [Test]
    public void Instrument_Calibrate_ResetsAccuracy()
    {
        instrument.Calibrate();
        Assert.AreEqual(1f, instrument.CalibrationAccuracy, 0.01f);
    }

    [Test]
    public void Instrument_ForceFailure_InvokesEvent()
    {
        InstrumentFailureMode receivedMode = InstrumentFailureMode.None;
        instrument.OnFailureTriggered += mode => receivedMode = mode;
        instrument.ForceFailure(InstrumentFailureMode.BlackOut);
        Assert.AreEqual(InstrumentFailureMode.BlackOut, receivedMode);
    }

    [Test]
    public void Instrument_Calibrate_InvokesEvent()
    {
        bool calibrated = false;
        instrument.OnCalibrated += () => calibrated = true;
        instrument.Calibrate();
        Assert.IsTrue(calibrated);
    }

    [Test]
    public void BarometricMode_EnumValues_Exist()
    {
        Assert.AreEqual(3, System.Enum.GetValues(typeof(BarometricMode)).Length);
    }

    [Test]
    public void RealismLevel_EnumValues_Exist()
    {
        Assert.AreEqual(3, System.Enum.GetValues(typeof(RealismLevel)).Length);
    }

    [Test]
    public void InstrumentType_AllExpectedTypes_Exist()
    {
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.Altimeter));
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.Airspeed));
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.VerticalSpeed));
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.Heading));
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.Attitude));
        Assert.IsTrue(System.Enum.IsDefined(typeof(InstrumentType), InstrumentType.FuelGauge));
    }
}
