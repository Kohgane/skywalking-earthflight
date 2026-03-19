using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.Weather
{
    /// <summary>
    /// Fetches real-time weather data from an external API based on the player's
    /// geolocation (latitude/longitude).  Falls back to procedurally generated
    /// weather patterns when offline or when no API key is configured.
    ///
    /// <para><b>API Setup:</b> This service is designed for the Open-Meteo free API
    /// (https://open-meteo.com/) which requires <em>no API key</em> for basic use.
    /// If you switch to a paid provider, populate <c>apiKey</c> in the Inspector.
    /// <b>Never commit a real API key to source control.</b></para>
    ///
    /// <para>Attach to a persistent GameObject (e.g. the WorldBootstrap object).
    /// The service auto-starts polling when enabled.</para>
    /// </summary>
    public class WeatherDataService : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static WeatherDataService Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("API Configuration")]
        [Tooltip("Base URL for the weather REST endpoint.  " +
                 "Default: Open-Meteo (no key required).  " +
                 "IMPORTANT: Do NOT commit a real API key to source control.")]
        [SerializeField] private string apiBaseUrl =
            "https://api.open-meteo.com/v1/forecast";

        [Tooltip("Optional API key for authenticated weather providers.  " +
                 "Leave blank to use the keyless Open-Meteo endpoint.  " +
                 "Configure via environment variable or Unity Cloud Config — " +
                 "never hard-code a real key here.")]
        [SerializeField] private string apiKey = "";

        [Header("Polling")]
        [Tooltip("How often (in seconds) to refresh weather data from the API.  Default: 300 s = 5 min.")]
        [SerializeField] private float pollIntervalSeconds = 300f;

        [Tooltip("Timeout for each HTTP request in seconds.")]
        [SerializeField] private float requestTimeoutSeconds = 10f;

        [Header("Offline / Fallback")]
        [Tooltip("When true, always use procedural weather instead of the API.")]
        [SerializeField] private bool offlineMode = false;

        [Tooltip("How quickly procedural weather cycles through patterns (seconds).")]
        [SerializeField] private float proceduralChangePeriod = 180f;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised whenever fresh weather data is available (API or procedural).</summary>
        public event Action<WeatherData> OnWeatherUpdated;

        /// <summary>
        /// Raised at the start of a weather transition, providing both the current
        /// and upcoming <see cref="WeatherData"/> snapshots.
        /// </summary>
        public event Action<WeatherData, WeatherData> OnWeatherTransitionStart;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>The most recently fetched or generated weather data.</summary>
        public WeatherData CurrentData { get; private set; }

        /// <summary><c>true</c> while a network request is in flight.</summary>
        public bool IsFetching { get; private set; }

        // ── Internal ──────────────────────────────────────────────────────────────
        private float _pollTimer;
        private float _proceduralTimer;
        private Coroutine _fetchCoroutine;

        // Geolocation source — set by WeatherStateManager or any external caller.
        private double _latitude  = 37.5665;   // Default: Seoul
        private double _longitude = 126.9780;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentData = WeatherData.CreateClear();
        }

        private void Start()
        {
            // Initial fetch on start
            RefreshWeather();
        }

        private void Update()
        {
            _pollTimer += Time.deltaTime;
            if (_pollTimer >= pollIntervalSeconds)
            {
                _pollTimer = 0f;
                RefreshWeather();
            }

            if (offlineMode)
            {
                _proceduralTimer += Time.deltaTime;
                if (_proceduralTimer >= proceduralChangePeriod)
                {
                    _proceduralTimer = 0f;
                    GenerateAndPublishProcedural();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the geolocation used for weather queries.
        /// Call this whenever the player's position changes significantly.
        /// </summary>
        /// <param name="lat">Latitude in degrees.</param>
        /// <param name="lon">Longitude in degrees.</param>
        public void SetLocation(double lat, double lon)
        {
            _latitude  = lat;
            _longitude = lon;
        }

        /// <summary>
        /// Enables or disables fallback (procedural) mode for offline operation.
        /// When <paramref name="fallback"/> is <c>true</c>, no network requests
        /// are made and procedural weather is used instead.
        /// </summary>
        /// <param name="fallback"><c>true</c> to enter offline/fallback mode.</param>
        public void SetFallbackMode(bool fallback)
        {
            offlineMode = fallback;
            Debug.Log($"[SWEF][Weather] Fallback mode: {fallback}");
            if (fallback)
                GenerateAndPublishProcedural();
        }

        /// <summary>
        /// Forces an immediate weather data refresh regardless of the poll timer.
        /// </summary>
        public void RefreshWeather()
        {
            if (offlineMode)
            {
                GenerateAndPublishProcedural();
                return;
            }

            if (_fetchCoroutine != null)
                StopCoroutine(_fetchCoroutine);

            _fetchCoroutine = StartCoroutine(FetchWeatherCoroutine());
        }

        // ── Network fetch ─────────────────────────────────────────────────────────

        private IEnumerator FetchWeatherCoroutine()
        {
            IsFetching = true;

            // Open-Meteo: free, no API key, returns JSON with WMO weather code
            string url = BuildRequestUrl();
            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.CeilToInt(requestTimeoutSeconds);

            yield return req.SendWebRequest();
            IsFetching = false;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[SWEF][Weather] API request failed: {req.error}. Using procedural fallback.");
                GenerateAndPublishProcedural();
                yield break;
            }

            WeatherData parsed = ParseOpenMeteoResponse(req.downloadHandler.text);
            if (parsed == null)
            {
                Debug.LogWarning("[SWEF][Weather] Failed to parse API response. Using procedural fallback.");
                GenerateAndPublishProcedural();
                yield break;
            }

            PublishNewData(parsed);
        }

        private string BuildRequestUrl()
        {
            // Open-Meteo API — fetches current weather + wind
            string url = $"{apiBaseUrl}?latitude={_latitude:F4}&longitude={_longitude:F4}" +
                         "&current=temperature_2m,relative_humidity_2m,wind_speed_10m," +
                         "wind_direction_10m,precipitation,cloud_cover,visibility," +
                         "weather_code&wind_speed_unit=ms";

            if (!string.IsNullOrEmpty(apiKey))
                url += $"&apikey={apiKey}";

            return url;
        }

        /// <summary>
        /// Parses an Open-Meteo JSON response into a <see cref="WeatherData"/> instance.
        /// Handles only the subset of fields relevant to SWEF.
        /// </summary>
        private WeatherData ParseOpenMeteoResponse(string json)
        {
            try
            {
                var root = JsonUtility.FromJson<OpenMeteoRoot>(json);
                if (root?.current == null) return null;

                var c = root.current;
                var data = new WeatherData
                {
                    temperatureCelsius   = c.temperature_2m,
                    humidity             = Mathf.Clamp01(c.relative_humidity_2m / 100f),
                    windSpeedMs          = c.wind_speed_10m,
                    windDirectionDeg     = c.wind_direction_10m,
                    visibility           = Mathf.Max(0f, c.visibility),
                    cloudCoverage        = Mathf.Clamp01(c.cloud_cover / 100f),
                    precipitationIntensity = Mathf.Clamp01(c.precipitation / 20f), // 20 mm/h = 1.0
                    lastUpdated          = DateTime.UtcNow,
                    condition            = WmoCodeToCondition(c.weather_code, c.precipitation)
                };

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SWEF][Weather] JSON parse error: {e.Message}");
                return null;
            }
        }

        // ── Procedural generation ─────────────────────────────────────────────────

        private void GenerateAndPublishProcedural()
        {
            WeatherData generated = GenerateProceduralWeather();
            PublishNewData(generated);
        }

        private WeatherData GenerateProceduralWeather()
        {
            // Cycle through weather patterns using a seeded random based on time
            float t = Time.time;
            UnityEngine.Random.State prevState = UnityEngine.Random.state;
            UnityEngine.Random.InitState((int)(t / proceduralChangePeriod));

            float roll = UnityEngine.Random.value;
            WeatherCondition cond;
            if      (roll < 0.35f) cond = WeatherCondition.Clear;
            else if (roll < 0.55f) cond = WeatherCondition.Cloudy;
            else if (roll < 0.65f) cond = WeatherCondition.Overcast;
            else if (roll < 0.73f) cond = WeatherCondition.Rain;
            else if (roll < 0.79f) cond = WeatherCondition.HeavyRain;
            else if (roll < 0.83f) cond = WeatherCondition.Snow;
            else if (roll < 0.86f) cond = WeatherCondition.HeavySnow;
            else if (roll < 0.88f) cond = WeatherCondition.Fog;
            else if (roll < 0.90f) cond = WeatherCondition.DenseFog;
            else if (roll < 0.93f) cond = WeatherCondition.Thunderstorm;
            else if (roll < 0.95f) cond = WeatherCondition.Hail;
            else if (roll < 0.97f) cond = WeatherCondition.Sandstorm;
            else                   cond = WeatherCondition.Windy;

            float precip = (cond == WeatherCondition.Rain || cond == WeatherCondition.HeavyRain ||
                            cond == WeatherCondition.Snow  || cond == WeatherCondition.HeavySnow ||
                            cond == WeatherCondition.Thunderstorm || cond == WeatherCondition.Hail)
                ? UnityEngine.Random.Range(0.2f, 1.0f) : 0f;

            float cloudCov = cond == WeatherCondition.Clear ? UnityEngine.Random.Range(0f, 0.2f)
                           : cond == WeatherCondition.Cloudy ? UnityEngine.Random.Range(0.3f, 0.6f)
                           : UnityEngine.Random.Range(0.6f, 1.0f);

            var data = new WeatherData
            {
                condition              = cond,
                temperatureCelsius     = UnityEngine.Random.Range(-5f, 35f),
                humidity               = UnityEngine.Random.Range(0.2f, 0.95f),
                windSpeedMs            = UnityEngine.Random.Range(0f, 20f),
                windDirectionDeg       = UnityEngine.Random.Range(0f, 360f),
                visibility             = cond == WeatherCondition.DenseFog ? UnityEngine.Random.Range(50f, 300f)
                                       : cond == WeatherCondition.Fog       ? UnityEngine.Random.Range(300f, 2000f)
                                       : cond == WeatherCondition.Sandstorm ? UnityEngine.Random.Range(100f, 1000f)
                                       : UnityEngine.Random.Range(3000f, 10000f),
                cloudCoverage          = cloudCov,
                precipitationIntensity = precip,
                lastUpdated            = DateTime.UtcNow
            };

            UnityEngine.Random.state = prevState;
            return data;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void PublishNewData(WeatherData newData)
        {
            WeatherData previous = CurrentData;
            CurrentData = newData;

            if (previous.condition != newData.condition)
                OnWeatherTransitionStart?.Invoke(previous, newData);

            OnWeatherUpdated?.Invoke(newData);
        }

        /// <summary>
        /// Maps WMO weather interpretation codes to <see cref="WeatherCondition"/>.
        /// Reference: https://open-meteo.com/en/docs#weathervariables
        /// </summary>
        private static WeatherCondition WmoCodeToCondition(int code, float precip)
        {
            return code switch
            {
                0       => WeatherCondition.Clear,
                1       => WeatherCondition.Clear,
                2       => WeatherCondition.Cloudy,
                3       => WeatherCondition.Overcast,
                45      => WeatherCondition.Fog,
                48      => WeatherCondition.DenseFog,
                51 or 53 => WeatherCondition.Rain,
                55      => WeatherCondition.Rain,
                56 or 57 => WeatherCondition.Rain,
                61 or 63 => WeatherCondition.Rain,
                65      => WeatherCondition.HeavyRain,
                66 or 67 => WeatherCondition.HeavyRain,
                71 or 73 => WeatherCondition.Snow,
                75      => WeatherCondition.HeavySnow,
                77      => WeatherCondition.Snow,
                80 or 81 => WeatherCondition.Rain,
                82      => WeatherCondition.HeavyRain,
                85 or 86 => WeatherCondition.HeavySnow,
                95      => WeatherCondition.Thunderstorm,
                96 or 99 => WeatherCondition.Hail,
                _       => precip > 0.5f ? WeatherCondition.Rain : WeatherCondition.Cloudy
            };
        }

        // ── JSON model (minimal, matches Open-Meteo schema) ───────────────────────

        [Serializable]
        private class OpenMeteoRoot
        {
            public OpenMeteoCurrent current;
        }

        [Serializable]
        private class OpenMeteoCurrent
        {
            public float temperature_2m;
            public float relative_humidity_2m;
            public float wind_speed_10m;
            public float wind_direction_10m;
            public float precipitation;
            public float cloud_cover;
            public float visibility;
            public int   weather_code;
        }
    }
}
