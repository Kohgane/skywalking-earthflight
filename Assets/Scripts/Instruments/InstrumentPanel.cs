using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class InstrumentPanel : MonoBehaviour
{
    [SerializeField] private FlightInstrument[] instruments;

    private Dictionary<InstrumentType, FlightInstrument> instrumentMap = new Dictionary<InstrumentType, FlightInstrument>();

    public event Action<FlightInstrument, InstrumentFailureMode> OnInstrumentFailed;
    public event Action<FlightInstrument> OnInstrumentCalibrationNeeded;

    public IReadOnlyDictionary<InstrumentType, FlightInstrument> Instruments => instrumentMap;

    private void Awake()
    {
        foreach (var inst in instruments)
        {
            if (inst.Config == null) continue;
            instrumentMap[inst.Config.instrumentType] = inst;
            inst.OnFailureTriggered += mode => OnInstrumentFailed?.Invoke(inst, mode);
            inst.OnCalibrationRequired += () => OnInstrumentCalibrationNeeded?.Invoke(inst);
        }
    }

    public FlightInstrument GetInstrument(InstrumentType type)
    {
        instrumentMap.TryGetValue(type, out var instrument);
        return instrument;
    }

    public void CalibrateAll()
    {
        foreach (var inst in instrumentMap.Values)
            inst.Calibrate();
    }

    public void RepairAll()
    {
        foreach (var inst in instrumentMap.Values)
            inst.Repair();
    }

    public List<FlightInstrument> GetFailedInstruments()
    {
        return instrumentMap.Values.Where(i => i.IsFailed).ToList();
    }

    public float GetOverallHealth()
    {
        if (instrumentMap.Count == 0) return 1f;
        int healthy = instrumentMap.Values.Count(i => !i.IsFailed);
        return (float)healthy / instrumentMap.Count;
    }
}
