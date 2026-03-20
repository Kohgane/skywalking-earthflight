using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SWEF.Core;

namespace SWEF.Weather
{
    /// <summary>
    /// Fetches real-time weather data from OpenWeatherMap (free tier) based on the
    /// player's current geographic coordinates.
    ///
    /// <para><b>API Key:</b> Set <c>apiKey</c> in the Inspector. The field is marked
    /// <c>[SerializeField]</c> so it can be configured per-project without committing
    /// a key to source control. Use Unity Cloud Config or a StreamingAssets config file
    /// for production deployments.</para>
    ///
    /// <para>Fallback: when offline or when the API returns an error, a Clear default
    /// condition is emitted and a warning is logged via <see cref="ErrorHandler"/> if
    /// available.</para>
    /// </summary>
    public class WeatherAPIClient : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherAPIClient Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("OpenWeatherMap")]
        [Tooltip("OpenWeatherMap API key. Never hard-code a real key in source files — " +
                 "configure this via Inspector override or Unity Cloud Config at runtime.")]
        [SerializeField] private string apiKey = "";

        [Tooltip("Seconds between automatic weather refreshes. Default: 300 (5 minutes).")]
        [SerializeField] private float updateInterval = 300f;

        [Tooltip("Minimum distance (metres) the player must travel before triggering " +
                 "an early re-fetch. Prevents redundant requests near the same location.")]
        [SerializeField] private float maxRequestRadius = 50000f;

        [Tooltip("HTTP request timeout in seconds.")]
        [SerializeField] private float requestTimeoutSeconds = 10f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever a new weather snapshot is available.</summary>
        public event Action<WeatherConditionData> OnWeatherUpdated;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The most recently received (or fallback) weather forecast.</summary>
        public WeatherForecast LastForecast { get; private set; }

        /// <summary><c>true</c> while a network request is in flight.</summary>
        public bool IsFetching { get; private set; }

        // ── Internal ──────────────────────────────────────────────────────────────
        private float  _pollTimer;
        private double _lastFetchLat;
        private double _lastFetchLon;
        private bool   _hasFetchedOnce;

        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            LastForecast = new WeatherForecast
            {
                current        = WeatherConditionData.CreateClear(),
                hourly         = new WeatherConditionData[0],
                lastUpdateUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= updateInterval)
            {
                _pollTimer = 0f;
                double lat = SWEFSession.Lat;
                double lon = SWEFSession.Lon;
                FetchWeather(lat, lon);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately starts a weather fetch for the given coordinates.
        /// Safe to call from outside; ignores duplicate in-flight requests.
        /// </summary>
        public void FetchWeather(double lat, double lon)
        {
            if (IsFetching) return;

            // Skip if still within maxRequestRadius of last fetch
            if (_hasFetchedOnce)
            {
                // Haversine approximation to compute great-circle distance in metres
                const double R  = 6_371_000.0;
                double dlat  = (lat - _lastFetchLat) * System.Math.PI / 180.0;
                double dlon  = (lon - _lastFetchLon) * System.Math.PI / 180.0;
                double a     = System.Math.Sin(dlat / 2) * System.Math.Sin(dlat / 2) +
                               System.Math.Cos(_lastFetchLat * System.Math.PI / 180.0) *
                               System.Math.Cos(lat * System.Math.PI / 180.0) *
                               System.Math.Sin(dlon / 2) * System.Math.Sin(dlon / 2);
                double distM = 2.0 * R * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1.0 - a));
                if (distM < maxRequestRadius) return;
            }

            StartCoroutine(FetchCoroutine(lat, lon));
        }

        // ── Coroutines ────────────────────────────────────────────────────────────

        private IEnumerator FetchCoroutine(double lat, double lon)
        {
            IsFetching = true;

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[SWEF][WeatherAPIClient] No API key set — using clear weather fallback.");
                EmitFallback();
                IsFetching = false;
                yield break;
            }

            string url = $"{BaseUrl}?lat={lat:F6}&lon={lon:F6}&units=metric&appid={apiKey}";

            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SWEF][WeatherAPIClient] Request failed: {req.error}");
                ErrorHandler.ShowNetworkError($"Weather API: {req.error}");
                EmitFallback();
            }
            else
            {
                var condition = ParseResponse(req.downloadHandler.text);
                var forecast  = new WeatherForecast
                {
                    current        = condition,
                    hourly         = new WeatherConditionData[0],
                    latitude       = lat,
                    longitude      = lon,
                    lastUpdateUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                LastForecast    = forecast;
                _lastFetchLat   = lat;
                _lastFetchLon   = lon;
                _hasFetchedOnce = true;

                OnWeatherUpdated?.Invoke(condition);
                Debug.Log($"[SWEF][WeatherAPIClient] Weather updated: {condition.type} ({condition.description})");
            }

            IsFetching = false;
        }

        // ── Parsing ───────────────────────────────────────────────────────────────

        /// <summary>Parses the OpenWeatherMap JSON response into a <see cref="WeatherConditionData"/>.</summary>
        private WeatherConditionData ParseResponse(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<OWMResponseWrapper>(json);
                if (wrapper == null) return WeatherConditionData.CreateClear();

                var cond = new WeatherConditionData
                {
                    temperature   = wrapper.main.temp,
                    humidity      = wrapper.main.humidity / 100f,
                    pressure      = wrapper.main.pressure,
                    windSpeed     = wrapper.wind.speed,
                    windDirection = wrapper.wind.deg,
                    visibility    = Mathf.Clamp(wrapper.visibility, 100f, 50000f),
                    cloudCover    = wrapper.clouds.all / 100f,
                    description   = (wrapper.weather != null && wrapper.weather.Length > 0)
                                       ? wrapper.weather[0].description
                                       : "Unknown"
                };

                int owmId = (wrapper.weather != null && wrapper.weather.Length > 0)
                    ? wrapper.weather[0].id
                    : 800;

                cond.type      = MapOWMIdToWeatherType(owmId, cond.cloudCover);
                cond.intensity = ComputeIntensity(cond);

                return cond;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF][WeatherAPIClient] JSON parse error: {ex.Message}");
                return WeatherConditionData.CreateClear();
            }
        }

        private static WeatherType MapOWMIdToWeatherType(int id, float cloudCover)
        {
            // OpenWeatherMap weather condition codes:
            // https://openweathermap.org/weather-conditions
            if      (id >= 200 && id < 300) return WeatherType.Thunderstorm;
            if      (id >= 300 && id < 400) return WeatherType.Drizzle;
            if      (id == 500)             return WeatherType.Rain;
            if      (id == 501)             return WeatherType.Rain;
            if      (id >= 502 && id < 505) return WeatherType.HeavyRain;
            if      (id == 511)             return WeatherType.Sleet;
            if      (id >= 520 && id < 532) return WeatherType.Rain;
            if      (id >= 600 && id < 602) return WeatherType.Snow;
            if      (id == 602)             return WeatherType.HeavySnow;
            if      (id >= 603 && id < 622) return WeatherType.Snow;
            if      (id == 611 || id == 612 || id == 613) return WeatherType.Sleet;
            if      (id == 615 || id == 616) return WeatherType.Sleet;
            if      (id == 701)             return WeatherType.Mist;
            if      (id == 711 || id == 721 || id == 731 || id == 751 || id == 761) return WeatherType.Sandstorm;
            if      (id == 741)             return cloudCover > 0.7f ? WeatherType.DenseFog : WeatherType.Fog;
            if      (id == 762)             return WeatherType.Sandstorm;
            if      (id == 771)             return WeatherType.HeavyRain;
            if      (id == 781)             return WeatherType.Thunderstorm;
            if      (id == 800)             return WeatherType.Clear;
            if      (id == 801)             return WeatherType.Cloudy;
            if      (id == 802)             return WeatherType.Cloudy;
            if      (id == 803)             return WeatherType.Overcast;
            if      (id == 804)             return WeatherType.Overcast;
            if      (id == 906)             return WeatherType.Hail;
            return WeatherType.Clear;
        }

        private static float ComputeIntensity(WeatherConditionData c)
        {
            return c.type switch
            {
                WeatherType.Thunderstorm => 0.8f + c.cloudCover * 0.2f,
                WeatherType.HeavyRain    => 0.6f + (1f - c.visibility / 50000f) * 0.4f,
                WeatherType.HeavySnow    => 0.6f,
                WeatherType.DenseFog     => 0.9f,
                WeatherType.Sandstorm    => 0.7f,
                WeatherType.Hail         => 0.75f,
                WeatherType.Rain         => 0.4f,
                WeatherType.Snow         => 0.35f,
                WeatherType.Fog          => 0.5f,
                WeatherType.Overcast     => c.cloudCover,
                WeatherType.Cloudy       => c.cloudCover * 0.6f,
                _                        => 0f
            };
        }

        private void EmitFallback()
        {
            var clear = WeatherConditionData.CreateClear();
            OnWeatherUpdated?.Invoke(clear);
        }

        // ── JSON wrapper classes ──────────────────────────────────────────────────

        // These private serializable classes mirror the OpenWeatherMap JSON structure
        // and are compatible with Unity's JsonUtility (no Newtonsoft required).

        [Serializable]
        private class OWMResponseWrapper
        {
            public OWMWeatherEntry[] weather;
            public OWMMain           main;
            public int               visibility;
            public OWMWind           wind;
            public OWMClouds         clouds;
            public string            name;
            public long              dt;
        }

        [Serializable]
        private class OWMWeatherEntry
        {
            public int    id;
            public string main;
            public string description;
            public string icon;
        }

        [Serializable]
        private class OWMMain
        {
            public float temp;
            public float feels_like;
            public float temp_min;
            public float temp_max;
            public float pressure;
            public float humidity;
        }

        [Serializable]
        private class OWMWind
        {
            public float speed;
            public float deg;
            public float gust;
        }

        [Serializable]
        private class OWMClouds
        {
            public float all;
        }
    }
}
