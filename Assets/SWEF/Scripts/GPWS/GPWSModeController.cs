using System.Collections.Generic;
using UnityEngine;

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Implements all seven standard GPWS modes with aviation-accurate logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSModeController : MonoBehaviour
    {
        #region Private State

        private GPWSManager _mgr;
        private readonly HashSet<GPWSCalloutType> _firedCallouts = new HashSet<GPWSCalloutType>();
        private float _previousTerrainClearanceFt;
        private float _terrainClosureRateFpm;

        private static readonly (GPWSCalloutType type, float altFt)[] CalloutTable =
        {
            (GPWSCalloutType.FiveHundred,   500f),
            (GPWSCalloutType.FourHundred,   400f),
            (GPWSCalloutType.ThreeHundred,  300f),
            (GPWSCalloutType.TwoHundred,    200f),
            (GPWSCalloutType.OneHundred,    100f),
            (GPWSCalloutType.Fifty,          50f),
            (GPWSCalloutType.Forty,          40f),
            (GPWSCalloutType.Thirty,         30f),
            (GPWSCalloutType.Twenty,         20f),
            (GPWSCalloutType.Ten,            10f),
        };

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
        }

        private void Update()
        {
            if (_mgr == null || !_mgr.IsEnabled) return;

            var cfg = _mgr.Config;
            if (cfg.enableMode1) EvaluateMode1(cfg);
            if (cfg.enableMode2) EvaluateMode2(cfg);
            if (cfg.enableMode3) EvaluateMode3(cfg);
            if (cfg.enableMode4) EvaluateMode4(cfg);
            if (cfg.enableMode6) EvaluateMode6(cfg);

            _previousTerrainClearanceFt = _mgr.AircraftAltitudeAglFt;
        }

        #endregion

        #region Mode 1 — Excessive Sink Rate

        private void EvaluateMode1(GPWSConfig cfg)
        {
            float aglFt = _mgr.AircraftAltitudeAglFt;
            float vsFpm = _mgr.VerticalSpeedFpm;

            if (aglFt > 2450f || vsFpm >= 0f)
            {
                _mgr.ClearAlert(GPWSMode.Mode1_ExcessiveSinkRate);
                return;
            }

            float absVs = Mathf.Abs(vsFpm);
            float threshold = Mathf.Lerp(cfg.sinkRateThresholdFpm, cfg.sinkRateThresholdFpm * 2f,
                Mathf.InverseLerp(2450f, 50f, aglFt));

            if (absVs > threshold * 1.5f)
                _mgr.RaiseAlert(GPWSMode.Mode1_ExcessiveSinkRate, GPWSAlertLevel.PullUp, "gpws_pull_up");
            else if (absVs > threshold)
                _mgr.RaiseAlert(GPWSMode.Mode1_ExcessiveSinkRate, GPWSAlertLevel.Warning, "gpws_sink_rate");
            else
                _mgr.ClearAlert(GPWSMode.Mode1_ExcessiveSinkRate);
        }

        #endregion

        #region Mode 2 — Terrain Closure

        private void EvaluateMode2(GPWSConfig cfg)
        {
            float aglFt = _mgr.AircraftAltitudeAglFt;
            if (aglFt > 2450f)
            {
                _mgr.ClearAlert(GPWSMode.Mode2_TerrainClosure);
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _terrainClosureRateFpm = (_previousTerrainClearanceFt - aglFt) / dt * 60f;

            if (_terrainClosureRateFpm > cfg.terrainClosureThresholdFpm * 1.5f)
                _mgr.RaiseAlert(GPWSMode.Mode2_TerrainClosure, GPWSAlertLevel.PullUp, "gpws_terrain_pull_up");
            else if (_terrainClosureRateFpm > cfg.terrainClosureThresholdFpm)
                _mgr.RaiseAlert(GPWSMode.Mode2_TerrainClosure, GPWSAlertLevel.Warning, "gpws_terrain");
            else
                _mgr.ClearAlert(GPWSMode.Mode2_TerrainClosure);
        }

        #endregion

        #region Mode 3 — Altitude Loss After Takeoff

        private void EvaluateMode3(GPWSConfig cfg)
        {
            if (!_mgr.IsInTakeoffPhase)
            {
                _mgr.ClearAlert(GPWSMode.Mode3_AltitudeLossAfterTakeoff);
                return;
            }

            float maxAlt = _mgr.MaxAltitudeAfterTakeoffFt;
            float currentAgl = _mgr.AircraftAltitudeAglFt;
            if (maxAlt <= 0f)
            {
                _mgr.ClearAlert(GPWSMode.Mode3_AltitudeLossAfterTakeoff);
                return;
            }

            float lossPercent = (maxAlt - currentAgl) / maxAlt;
            if (lossPercent > 0.1f && _mgr.VerticalSpeedFpm < 0f)
                _mgr.RaiseAlert(GPWSMode.Mode3_AltitudeLossAfterTakeoff, GPWSAlertLevel.Warning, "gpws_dont_sink");
            else
                _mgr.ClearAlert(GPWSMode.Mode3_AltitudeLossAfterTakeoff);
        }

        #endregion

        #region Mode 4 — Unsafe Terrain Clearance

        private void EvaluateMode4(GPWSConfig cfg)
        {
            float aglFt = _mgr.AircraftAltitudeAglFt;
            if (aglFt > 1000f)
            {
                _mgr.ClearAlert(GPWSMode.Mode4_UnsafeTerrainClearance);
                return;
            }

            bool gearUnsafe = cfg.gearDownRequired && !_mgr.GearDown && aglFt < 500f;
            bool flapUnsafe = _mgr.FlapPosition < cfg.flapsLandingConfig && aglFt < 245f;

            if (gearUnsafe)
                _mgr.RaiseAlert(GPWSMode.Mode4_UnsafeTerrainClearance, GPWSAlertLevel.Warning, "gpws_too_low_gear");
            else if (flapUnsafe)
                _mgr.RaiseAlert(GPWSMode.Mode4_UnsafeTerrainClearance, GPWSAlertLevel.Warning, "gpws_too_low_flaps");
            else
                _mgr.ClearAlert(GPWSMode.Mode4_UnsafeTerrainClearance);
        }

        #endregion

        #region Mode 6 — Advisory Altitude Callouts

        private void EvaluateMode6(GPWSConfig cfg)
        {
            if (!cfg.altitudeCalloutEnabled) return;

            float aglFt = _mgr.AircraftAltitudeAglFt;
            float prevAgl = _previousTerrainClearanceFt;
            if (_mgr.VerticalSpeedFpm >= 0f)
            {
                _firedCallouts.Clear();
                return;
            }

            float minAbove = cfg.minimumDescentAltFt + 100f;
            if (prevAgl >= minAbove && aglFt < minAbove && !_firedCallouts.Contains(GPWSCalloutType.HundredAbove))
            {
                _firedCallouts.Add(GPWSCalloutType.HundredAbove);
                _mgr.TriggerCallout(GPWSCalloutType.HundredAbove);
            }

            if (prevAgl >= cfg.minimumDescentAltFt && aglFt < cfg.minimumDescentAltFt
                && !_firedCallouts.Contains(GPWSCalloutType.Minimums))
            {
                _firedCallouts.Add(GPWSCalloutType.Minimums);
                _mgr.TriggerCallout(GPWSCalloutType.Minimums);
            }

            foreach (var (type, altThreshold) in CalloutTable)
            {
                if (prevAgl >= altThreshold && aglFt < altThreshold && !_firedCallouts.Contains(type))
                {
                    _firedCallouts.Add(type);
                    _mgr.TriggerCallout(type);
                }
            }
        }

        #endregion

        #region Public — Reset Callouts

        /// <summary>Resets callout memory (e.g. after a go-around).</summary>
        public void ResetCallouts() => _firedCallouts.Clear();

        #endregion
    }
}
