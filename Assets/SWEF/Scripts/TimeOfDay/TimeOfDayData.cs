using System;
using UnityEngine;

namespace SWEF.TimeOfDay
{
    // ── Enums ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes the current phase of the day based on the sun's altitude angle.
    /// Phases progress from Night through twilight bands to full Day and Solar Noon.
    /// </summary>
    public enum DayPhase
    {
        /// <summary>Sun is more than 18° below the horizon. True darkness.</summary>
        Night,
        /// <summary>Sun is between −18° and −12° below the horizon. Stars barely visible.</summary>
        AstronomicalTwilight,
        /// <summary>Sun is between −12° and −6° below the horizon. Horizon faintly visible.</summary>
        NauticalTwilight,
        /// <summary>Sun is between −6° and 0° below the horizon. Enough light for outdoor activity.</summary>
        CivilTwilight,
        /// <summary>Sun is between 0° and 6° above the horizon. Warm golden-orange tones.</summary>
        GoldenHour,
        /// <summary>Sun is more than 6° above the horizon. Full daylight.</summary>
        Day,
        /// <summary>Sun is near its highest point (within 1 hour of solar noon).</summary>
        SolarNoon
    }

    /// <summary>Meteorological season based on calendar month and hemisphere.</summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>Current lunar phase based on the synodic cycle (~29.5 days).</summary>
    public enum MoonPhase
    {
        NewMoon,
        WaxingCrescent,
        FirstQuarter,
        WaxingGibbous,
        FullMoon,
        WaningGibbous,
        ThirdQuarter,
        WaningCrescent
    }

    // ── SunMoonState ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of current sun and moon positional data computed by <see cref="SolarCalculator"/>.
    /// </summary>
    [Serializable]
    public class SunMoonState
    {
        // ── Sun ───────────────────────────────────────────────────────────────────

        /// <summary>Sun altitude angle in degrees. Positive = above horizon, negative = below.</summary>
        public float sunAltitudeDeg;

        /// <summary>Sun azimuth angle in degrees measured clockwise from north (0°–360°).</summary>
        public float sunAzimuthDeg;

        /// <summary>World-space unit vector pointing toward the sun.</summary>
        public Vector3 sunDirection;

        // ── Moon ──────────────────────────────────────────────────────────────────

        /// <summary>Moon altitude angle in degrees. Positive = above horizon, negative = below.</summary>
        public float moonAltitudeDeg;

        /// <summary>Moon azimuth angle in degrees measured clockwise from north (0°–360°).</summary>
        public float moonAzimuthDeg;

        /// <summary>World-space unit vector pointing toward the moon.</summary>
        public Vector3 moonDirection;

        /// <summary>Current lunar phase.</summary>
        public MoonPhase moonPhase;

        /// <summary>Fraction of the moon's visible disk that is illuminated (0 = new moon, 1 = full moon).</summary>
        [Range(0f, 1f)] public float moonIllumination;

        // ── Phase info ────────────────────────────────────────────────────────────

        /// <summary>Current day phase determined by <see cref="sunAltitudeDeg"/>.</summary>
        public DayPhase currentDayPhase;

        /// <summary>True when the sun is above the horizon.</summary>
        public bool isDaytime;

        /// <summary>Local sunrise time expressed as a fractional hour (0–24).</summary>
        public float sunriseTime;

        /// <summary>Local sunset time expressed as a fractional hour (0–24).</summary>
        public float sunsetTime;

        /// <summary>Total number of daylight hours for the current date and location.</summary>
        public float dayLengthHours;
    }

    // ── LightingSnapshot ──────────────────────────────────────────────────────────

    /// <summary>
    /// Complete set of lighting parameters to be applied to the Unity scene by
    /// <see cref="LightingController"/> at a given moment in time.
    /// </summary>
    [Serializable]
    public class LightingSnapshot
    {
        // ── Sun ───────────────────────────────────────────────────────────────────

        /// <summary>Color of the sun directional light.</summary>
        public Color sunColor = Color.white;

        /// <summary>Intensity of the sun directional light (lux equivalent).</summary>
        public float sunIntensity = 1f;

        // ── Moon ──────────────────────────────────────────────────────────────────

        /// <summary>Color of the moon directional light.</summary>
        public Color moonColor = new Color(0.6f, 0.65f, 0.8f);

        /// <summary>Intensity of the moon directional light.</summary>
        public float moonIntensity = 0.05f;

        // ── Ambient ───────────────────────────────────────────────────────────────

        /// <summary>Ambient sky color (top hemisphere).</summary>
        public Color ambientSkyColor = new Color(0.5f, 0.7f, 1f);

        /// <summary>Ambient equator color (horizon band).</summary>
        public Color ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);

        /// <summary>Ambient ground color (bottom hemisphere).</summary>
        public Color ambientGroundColor = new Color(0.2f, 0.2f, 0.15f);

        // ── Fog ───────────────────────────────────────────────────────────────────

        /// <summary>Current fog color.</summary>
        public Color fogColor = new Color(0.7f, 0.8f, 0.9f);

        /// <summary>Exponential fog density (0 = none).</summary>
        public float fogDensity = 0.0002f;

        // ── Skybox ────────────────────────────────────────────────────────────────

        /// <summary>Skybox material exposure value.</summary>
        public float skyboxExposure = 1f;

        /// <summary>Tint color multiplied into the skybox material.</summary>
        public Color skyboxTint = Color.white;

        // ── Shadows ───────────────────────────────────────────────────────────────

        /// <summary>Shadow strength from the primary directional light (0 = no shadows, 1 = full).</summary>
        [Range(0f, 1f)] public float shadowStrength = 1f;

        /// <summary>Color tint applied to shadow areas.</summary>
        public Color shadowColor = new Color(0.1f, 0.1f, 0.15f);

        // ── Stars ─────────────────────────────────────────────────────────────────

        /// <summary>Visibility of the star field (0 = invisible, 1 = fully visible).</summary>
        [Range(0f, 1f)] public float starVisibility;
    }

    // ── TimeOfDayConfig ───────────────────────────────────────────────────────────

    /// <summary>
    /// Runtime configuration for the <see cref="TimeOfDayManager"/>.
    /// Serialized by <see cref="SWEF.Settings.SettingsManager"/> under the
    /// <c>SWEF_TOD_</c> PlayerPrefs key prefix.
    /// </summary>
    [Serializable]
    public class TimeOfDayConfig
    {
        /// <summary>
        /// Simulation time multiplier.
        /// <c>1.0</c> = real-time. <c>60</c> = one game-minute per real-second.
        /// </summary>
        public float timeScale = 1f;

        /// <summary>Starting time of day in fractional hours (0–24). Default 12 noon.</summary>
        [Range(0f, 24f)] public float startingHour = 12f;

        /// <summary>
        /// When true the manager initialises from the device's current UTC clock
        /// and continuously tracks it (ignoring <see cref="timeScale"/>).
        /// </summary>
        public bool useRealWorldTime = true;

        /// <summary>Initial geographic latitude in decimal degrees (−90 to +90).</summary>
        [Range(-90f, 90f)] public float latitude = 48.8f;   // Paris default

        /// <summary>Initial geographic longitude in decimal degrees (−180 to +180).</summary>
        [Range(-180f, 180f)] public float longitude = 2.3f;

        /// <summary>Whether seasonal daylight variation is applied.</summary>
        public bool enableSeasons = true;

        /// <summary>
        /// Force a specific season for testing.
        /// <c>null</c> means the season is computed from the calendar date.
        /// </summary>
        public Season? seasonOverride = null;

        /// <summary>Whether the lunar cycle and moon phase are simulated.</summary>
        public bool enableMoonCycle = true;

        /// <summary>Duration of the golden-hour window on either side of sunrise/sunset, in minutes.</summary>
        public float goldenHourDuration = 30f;

        /// <summary>
        /// How smoothly day-phase lighting transitions blend (0 = instant, 1 = very smooth).
        /// </summary>
        [Range(0f, 1f)] public float transitionSmoothness = 0.8f;

        /// <summary>Seconds between full sun/moon recalculation passes. Smaller = more accurate but more CPU.</summary>
        public float lightingUpdateInterval = 0.1f;
    }

    // ── SeasonalProfile ───────────────────────────────────────────────────────────

    /// <summary>
    /// Aggregate modifiers applied to the base lighting when a particular season is active.
    /// Referenced by <see cref="SeasonalLightingProfile"/> and <see cref="TimeOfDayManager"/>.
    /// </summary>
    [Serializable]
    public class SeasonalProfile
    {
        /// <summary>Season this profile applies to.</summary>
        public Season season;

        /// <summary>
        /// Multiplier for the solar day length.
        /// Values &gt; 1 = longer days (summer); &lt; 1 = shorter days (winter).
        /// </summary>
        public float dayLengthMultiplier = 1f;

        /// <summary>Tint color added to sunlight. Warm for summer, cool for winter.</summary>
        public Color sunColorTint = Color.white;

        /// <summary>Scale factor for <see cref="LightingSnapshot.ambientSkyColor"/> intensity.</summary>
        public float ambientIntensityMultiplier = 1f;

        /// <summary>Scale factor applied to <see cref="LightingSnapshot.fogDensity"/>.</summary>
        public float fogDensityMultiplier = 1f;

        /// <summary>
        /// Kelvin offset from the neutral 6500 K color temperature.
        /// Positive = warmer (summer), negative = cooler (winter/overcast).
        /// </summary>
        public float temperatureColorShift;
    }
}
