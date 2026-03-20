using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Enumerates all weather types supported by Phase 32 weather system.
    /// </summary>
    public enum WeatherType
    {
        Clear,
        Cloudy,
        Overcast,
        Rain,
        HeavyRain,
        Snow,
        HeavySnow,
        Fog,
        DenseFog,
        Thunderstorm,
        Hail,
        Sandstorm,
        Drizzle,
        Sleet,
        Mist
    }

    /// <summary>
    /// Snapshot of weather state at a given location and time.
    /// Produced by <see cref="WeatherAPIClient"/> and consumed by all Phase 32 weather sub-systems.
    /// </summary>
    [System.Serializable]
    public struct WeatherConditionData
    {
        /// <summary>Primary weather type.</summary>
        public WeatherType type;

        /// <summary>Intensity of weather effect (0 = minimal, 1 = maximum).</summary>
        [Range(0f, 1f)]
        public float intensity;

        /// <summary>Air temperature in degrees Celsius.</summary>
        public float temperature;

        /// <summary>Relative humidity (0 = dry, 1 = fully saturated).</summary>
        [Range(0f, 1f)]
        public float humidity;

        /// <summary>Horizontal visibility in metres (100–50000).</summary>
        public float visibility;

        /// <summary>Sustained wind speed in metres per second.</summary>
        public float windSpeed;

        /// <summary>Wind direction in meteorological degrees (0 = North, 90 = East).</summary>
        [Range(0f, 360f)]
        public float windDirection;

        /// <summary>Fraction of sky covered by cloud (0 = clear, 1 = overcast).</summary>
        [Range(0f, 1f)]
        public float cloudCover;

        /// <summary>Atmospheric pressure in hPa.</summary>
        public float pressure;

        /// <summary>Human-readable weather description.</summary>
        public string description;

        /// <summary>Returns a sensible clear-sky default.</summary>
        public static WeatherConditionData CreateClear() => new WeatherConditionData
        {
            type          = WeatherType.Clear,
            intensity     = 0f,
            temperature   = 20f,
            humidity      = 0.4f,
            visibility    = 50000f,
            windSpeed     = 2f,
            windDirection = 0f,
            cloudCover    = 0f,
            pressure      = 1013f,
            description   = "Clear"
        };

        /// <summary>
        /// Linearly interpolates between two <see cref="WeatherConditionData"/> snapshots.
        /// Non-numeric fields (type, description) are taken from <paramref name="b"/> when
        /// <paramref name="t"/> >= 0.5.
        /// </summary>
        public static WeatherConditionData Lerp(WeatherConditionData a, WeatherConditionData b, float t)
        {
            t = Mathf.Clamp01(t);
            return new WeatherConditionData
            {
                type          = t >= 0.5f ? b.type : a.type,
                intensity     = Mathf.Lerp(a.intensity,     b.intensity,     t),
                temperature   = Mathf.Lerp(a.temperature,   b.temperature,   t),
                humidity      = Mathf.Lerp(a.humidity,       b.humidity,       t),
                visibility    = Mathf.Lerp(a.visibility,     b.visibility,     t),
                windSpeed     = Mathf.Lerp(a.windSpeed,      b.windSpeed,      t),
                windDirection = Mathf.LerpAngle(a.windDirection, b.windDirection, t),
                cloudCover    = Mathf.Lerp(a.cloudCover,     b.cloudCover,     t),
                pressure      = Mathf.Lerp(a.pressure,       b.pressure,       t),
                description   = t >= 0.5f ? b.description : a.description
            };
        }
    }

    /// <summary>
    /// Detailed wind state including gusts and turbulence, consumed by <see cref="WindSystem"/>.
    /// </summary>
    [System.Serializable]
    public struct WindData
    {
        /// <summary>Normalised world-space wind direction vector.</summary>
        public Vector3 direction;

        /// <summary>Sustained wind speed in metres per second.</summary>
        public float speed;

        /// <summary>Peak gust speed in metres per second.</summary>
        public float gustSpeed;

        /// <summary>Gust frequency in Hz (oscillations per second).</summary>
        public float gustFrequency;

        /// <summary>Turbulence intensity (0 = calm, 1 = violent).</summary>
        [Range(0f, 1f)]
        public float turbulenceIntensity;

        /// <summary>Creates a default calm-wind snapshot.</summary>
        public static WindData CreateCalm() => new WindData
        {
            direction          = Vector3.forward,
            speed              = 0f,
            gustSpeed          = 0f,
            gustFrequency      = 0f,
            turbulenceIntensity = 0f
        };

        /// <summary>
        /// Builds a <see cref="WindData"/> from a speed (m/s) and meteorological direction (degrees).
        /// </summary>
        public static WindData FromSpeedAndDirection(float speedMs, float directionDeg, float gustMultiplier = 1.5f)
        {
            float rad = directionDeg * Mathf.Deg2Rad;
            // Meteorological convention: direction the wind is coming FROM.
            var dir = new Vector3(-Mathf.Sin(rad), 0f, -Mathf.Cos(rad)).normalized;
            return new WindData
            {
                direction          = dir,
                speed              = speedMs,
                gustSpeed          = speedMs * gustMultiplier,
                gustFrequency      = 0.3f,
                turbulenceIntensity = Mathf.Clamp01(speedMs / 30f)
            };
        }
    }

    /// <summary>
    /// Bundles current and hourly forecast weather data for a geographic location.
    /// Populated by <see cref="WeatherAPIClient"/>.
    /// </summary>
    [System.Serializable]
    public struct WeatherForecast
    {
        /// <summary>Current weather conditions.</summary>
        public WeatherConditionData current;

        /// <summary>Array of hourly forecast snapshots (next 24 hours).</summary>
        public WeatherConditionData[] hourly;

        /// <summary>Geographic latitude of the forecast origin.</summary>
        public double latitude;

        /// <summary>Geographic longitude of the forecast origin.</summary>
        public double longitude;

        /// <summary>Unix timestamp (UTC seconds) when this forecast was last fetched.</summary>
        public long lastUpdateUnix;
    }
}
