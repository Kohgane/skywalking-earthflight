using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.GPWS
{
    #region Enumerations

    /// <summary>Standard GPWS operating modes (1–7).</summary>
    public enum GPWSMode
    {
        Mode1_ExcessiveSinkRate,
        Mode2_TerrainClosure,
        Mode3_AltitudeLossAfterTakeoff,
        Mode4_UnsafeTerrainClearance,
        Mode5_BelowGlideslope,
        Mode6_AdvisoryCallouts,
        Mode7_WindshearWarning
    }

    /// <summary>Severity level of a GPWS alert.</summary>
    public enum GPWSAlertLevel
    {
        None,
        Caution,
        Warning,
        PullUp
    }

    /// <summary>Radio-altitude advisory callout type.</summary>
    public enum GPWSCalloutType
    {
        Minimums,
        HundredAbove,
        FiveHundred,
        FourHundred,
        ThreeHundred,
        TwoHundred,
        OneHundred,
        Fifty,
        Forty,
        Thirty,
        Twenty,
        Ten
    }

    /// <summary>Threat classification for a terrain cell in the TAWS display.</summary>
    public enum TerrainThreatLevel
    {
        Safe,
        Advisory,
        Caution,
        Danger
    }

    /// <summary>TAWS terrain display operating mode.</summary>
    public enum TAWSDisplayMode
    {
        Off,
        Normal,
        PeakMode
    }

    #endregion

    #region Data Classes

    /// <summary>A single GPWS alert record.</summary>
    [Serializable]
    public class GPWSAlert
    {
        public string alertId;
        public GPWSMode mode;
        public GPWSAlertLevel level;
        public string messageKey;
        public float timestamp;
        public bool isActive;
        public bool autoResolvable;
        public float terrainAltitude;
        public float aircraftAltitude;
        public float closureRate;

        public GPWSAlert(GPWSMode mode, GPWSAlertLevel level, string messageKey)
        {
            alertId = Guid.NewGuid().ToString("N").Substring(0, 8);
            this.mode = mode;
            this.level = level;
            this.messageKey = messageKey;
            timestamp = Time.time;
            isActive = true;
            autoResolvable = true;
        }
    }

    /// <summary>A single terrain sample in the TAWS sweep.</summary>
    [Serializable]
    public class TerrainCell
    {
        public Vector3 position;
        public float elevationFt;
        public TerrainThreatLevel threatLevel;
        public float distanceToAircraftNm;
        public float bearing;
    }

    /// <summary>Runtime configuration for the GPWS subsystem.</summary>
    [Serializable]
    public class GPWSConfig
    {
        [Header("Mode Enable Flags")]
        public bool enableMode1 = true;
        public bool enableMode2 = true;
        public bool enableMode3 = true;
        public bool enableMode4 = true;
        public bool enableMode5 = true;
        public bool enableMode6 = true;
        public bool enableMode7 = true;

        [Header("Mode 1 — Excessive Sink Rate")]
        [Tooltip("Descent rate threshold in feet per minute.")]
        public float sinkRateThresholdFpm = 1000f;

        [Header("Mode 2 — Terrain Closure")]
        [Tooltip("Terrain closure rate threshold in feet per minute.")]
        public float terrainClosureThresholdFpm = 2000f;

        [Header("Mode 5 — Glideslope")]
        [Tooltip("Glideslope deviation threshold in dots.")]
        public float glideslopeDeviationDots = 1.3f;

        [Header("Mode 6 — Advisory Callouts")]
        public bool altitudeCalloutEnabled = true;
        [Tooltip("Decision altitude for MINIMUMS callout in feet AGL.")]
        public float minimumDescentAltFt = 200f;

        [Header("Mode 4 — Configuration")]
        [Tooltip("Flap detent considered landing configuration (0-1 normalised).")]
        public float flapsLandingConfig = 0.7f;
        public bool gearDownRequired = true;

        [Header("Terrain Awareness")]
        [Tooltip("Forward-looking terrain scan radius in nautical miles.")]
        public float terrainScanRadiusNm = 5f;
        [Tooltip("Number of radial rays per sweep.")]
        public int terrainScanResolution = 36;

        [Header("Mode 7 — Windshear")]
        [Tooltip("Airspeed deviation threshold in knots for windshear alert.")]
        public float windshearThresholdKts = 15f;
    }

    /// <summary>Result of a TAWS terrain scan pass.</summary>
    [Serializable]
    public class TerrainScanResult
    {
        public TerrainCell[] cells;
        public float maxElevationFt;
        public float minClearanceFt;
        public float averageElevationFt;
        public Vector3 threatDirection;
        public float scanTimestamp;
    }

    #endregion
}
