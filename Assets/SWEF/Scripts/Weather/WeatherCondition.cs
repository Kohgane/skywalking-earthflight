using System;
using UnityEngine;

namespace SWEF.Weather
{
    /// <summary>
    /// Enumerates all possible weather conditions in SWEF.
    /// </summary>
    public enum WeatherCondition
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
        Windy
    }

    /// <summary>
    /// Snapshot of real-time or procedural weather data at the player's location.
    /// Populated by <see cref="WeatherDataService"/> and consumed by all Weather sub-systems.
    /// </summary>
    [Serializable]
    public class WeatherData
    {
        /// <summary>Primary weather condition descriptor.</summary>
        public WeatherCondition condition;

        /// <summary>Air temperature in degrees Celsius.</summary>
        public float temperatureCelsius;

        /// <summary>Relative humidity (0 = dry, 1 = fully saturated).</summary>
        [Range(0f, 1f)]
        public float humidity;

        /// <summary>Sustained wind speed in meters per second.</summary>
        public float windSpeedMs;

        /// <summary>Wind direction in meteorological degrees (0 = North, 90 = East, 180 = South, 270 = West).</summary>
        [Range(0f, 360f)]
        public float windDirectionDeg;

        /// <summary>Horizontal visibility in meters (e.g. 10,000 = 10 km clear).</summary>
        public float visibility;

        /// <summary>Fraction of sky covered by cloud (0 = clear, 1 = overcast).</summary>
        [Range(0f, 1f)]
        public float cloudCoverage;

        /// <summary>Precipitation intensity (0 = none, 1 = heaviest).</summary>
        [Range(0f, 1f)]
        public float precipitationIntensity;

        /// <summary>UTC timestamp of when this data was last fetched or generated.</summary>
        public DateTime lastUpdated;

        /// <summary>Returns a wind force vector in world-space (XZ plane, Y = 0).</summary>
        public Vector3 WindVector
        {
            get
            {
                float rad = windDirectionDeg * Mathf.Deg2Rad;
                // Meteorological convention: direction *from* which wind blows.
                return new Vector3(-Mathf.Sin(rad), 0f, -Mathf.Cos(rad)) * windSpeedMs;
            }
        }

        /// <summary>Creates a default clear-weather data instance.</summary>
        public static WeatherData CreateClear() => new WeatherData
        {
            condition            = WeatherCondition.Clear,
            temperatureCelsius   = 20f,
            humidity             = 0.4f,
            windSpeedMs          = 2f,
            windDirectionDeg     = 0f,
            visibility           = 10000f,
            cloudCoverage        = 0f,
            precipitationIntensity = 0f,
            lastUpdated          = DateTime.UtcNow
        };
    }
}
