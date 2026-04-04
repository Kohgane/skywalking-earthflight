// LiveFlightAPIClient.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Singleton MonoBehaviour responsible for fetching live aircraft data from the
    /// configured REST API (OpenSky, ADS-B Exchange) or generating mock data in the
    /// Unity Editor.  Handles rate limiting, error recovery and exponential back-off.
    /// </summary>
    public class LiveFlightAPIClient : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static LiveFlightAPIClient Instance { get; private set; }

        // ── Configuration ─────────────────────────────────────────────────────────
        [SerializeField] private LiveFlightConfig config;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised each time a fresh batch of aircraft state-vectors is available.</summary>
        public event Action<List<LiveAircraftInfo>> OnAircraftDataReceived;

        /// <summary>Raised when a fetch attempt fails after all retries are exhausted.</summary>
        public event Action<string> OnFetchError;

        // ── Internal state ────────────────────────────────────────────────────────
        private float _pollTimer;
        private bool  _isFetching;
        private int   _consecutiveErrors;

        private const int   MaxRetries       = 4;
        private const float BaseRetryDelay   = 2f;    // seconds
        private const float MaxRetryDelay    = 60f;   // seconds

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
        }

        private void Update()
        {
            if (config == null || _isFetching) return;

            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= config.pollIntervalSeconds)
            {
                _pollTimer = 0f;
                StartCoroutine(PollData());
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Immediately triggers a fetch for the given bounding box.
        /// The result is broadcast via <see cref="OnAircraftDataReceived"/>.
        /// </summary>
        public void FetchNow(double minLat, double minLon, double maxLat, double maxLon)
        {
            if (_isFetching) return;
            StartCoroutine(FetchAircraftInArea(minLat, minLon, maxLat, maxLon));
        }

        /// <summary>
        /// Coroutine: queries the configured API for all aircraft inside the bounding
        /// box and raises <see cref="OnAircraftDataReceived"/> on success.
        /// </summary>
        public IEnumerator FetchAircraftInArea(
            double minLat, double minLon, double maxLat, double maxLon)
        {
            if (config == null) yield break;

            _isFetching = true;

            if (config.apiProvider == LiveFlightDataSource.Mock)
            {
                // No network needed — generate synthetic data instantly.
                var center = new Vector3(
                    (float)((minLon + maxLon) * 0.5),
                    0f,
                    (float)((minLat + maxLat) * 0.5));
                var list = GenerateMockData(center, config.maxAircraftDisplayed);
                OnAircraftDataReceived?.Invoke(list);
                _consecutiveErrors = 0;
                _isFetching = false;
                yield break;
            }

            string url = BuildUrl(minLat, minLon, maxLat, maxLon);
            yield return StartCoroutine(FetchWithRetry(url));

            _isFetching = false;
        }

        // ── Parsing ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses the OpenSky Network <em>GET /states/all</em> JSON response into a
        /// list of <see cref="LiveAircraftInfo"/> structs.
        /// </summary>
        public List<LiveAircraftInfo> ParseOpenSkyResponse(string json)
        {
            var result = new List<LiveAircraftInfo>();
            if (string.IsNullOrEmpty(json)) return result;

            // OpenSky response: {"time":…,"states":[[…],…]}
            // State vector array indices (from OpenSky docs):
            //  0  icao24        string
            //  1  callsign      string
            //  2  origin_country string
            //  3  time_position  int / null
            //  4  last_contact   int
            //  5  longitude      float / null
            //  6  latitude       float / null
            //  7  baro_altitude  float / null
            //  8  on_ground      bool
            //  9  velocity       float / null
            // 10  true_track     float / null
            // 11  vertical_rate  float / null
            // 12  sensors        [int] / null
            // 13  geo_altitude   float / null
            // 14  squawk         string / null
            // 15  spi            bool
            // 16  position_source int

            int statesStart = json.IndexOf("\"states\"", StringComparison.Ordinal);
            if (statesStart < 0) return result;

            int arrayStart = json.IndexOf('[', statesStart + 8);
            if (arrayStart < 0) return result;

            // Walk the top-level states array, extracting one inner array per aircraft.
            int depth = 0;
            int innerStart = -1;

            for (int i = arrayStart; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '[')
                {
                    depth++;
                    if (depth == 2) innerStart = i;
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1 && innerStart >= 0)
                    {
                        string row = json.Substring(innerStart, i - innerStart + 1);
                        var info = ParseOpenSkyRow(row);
                        if (!string.IsNullOrEmpty(info.icao24))
                            result.Add(info);
                        innerStart = -1;
                    }
                    if (depth == 0) break;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses an ADS-B Exchange <em>JSON</em> response into a list of
        /// <see cref="LiveAircraftInfo"/> structs.
        /// </summary>
        public List<LiveAircraftInfo> ParseADSBResponse(string json)
        {
            var result = new List<LiveAircraftInfo>();
            if (string.IsNullOrEmpty(json)) return result;

            // ADS-B Exchange v2 format: {"ac":[{…},…]}
            int acStart = json.IndexOf("\"ac\"", StringComparison.Ordinal);
            if (acStart < 0) return result;

            int arrayStart = json.IndexOf('[', acStart + 4);
            if (arrayStart < 0) return result;

            int depth = 0;
            int objStart = -1;

            for (int i = arrayStart; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '{')
                {
                    depth++;
                    if (depth == 1) objStart = i;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objStart >= 0)
                    {
                        string obj = json.Substring(objStart, i - objStart + 1);
                        var info = ParseADSBObject(obj);
                        if (!string.IsNullOrEmpty(info.icao24))
                            result.Add(info);
                        objStart = -1;
                    }
                }
                else if (c == ']' && depth == 0) break;
            }

            return result;
        }

        /// <summary>
        /// Generates <paramref name="count"/> synthetic aircraft positions around
        /// <paramref name="center"/> for Editor/offline testing.
        /// </summary>
        public List<LiveAircraftInfo> GenerateMockData(Vector3 center, int count)
        {
            var list = new List<LiveAircraftInfo>(count);
            var rng  = new System.Random(42); // deterministic seed for reproducibility

            string[] types      = { "B737", "A320", "B777", "A380", "E175", "CRJ9", "B787", "A350" };
            string[] countries  = { "United States", "Germany", "France", "Japan", "United Kingdom", "Canada" };
            string[] callPfx    = { "UAL", "DLH", "AFR", "JAL", "BAW", "ACA", "SWA", "DAL" };
            long     now        = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            for (int i = 0; i < count; i++)
            {
                float latOffset = (float)(rng.NextDouble() * 10.0 - 5.0);
                float lonOffset = (float)(rng.NextDouble() * 10.0 - 5.0);

                list.Add(new LiveAircraftInfo
                {
                    icao24        = $"mock{i:x4}",
                    callsign      = $"{callPfx[rng.Next(callPfx.Length)]}{rng.Next(100, 9999)}",
                    latitude      = center.z + latOffset,
                    longitude     = center.x + lonOffset,
                    altitude      = (float)(rng.NextDouble() * 12000.0 + 1000.0),
                    velocity      = (float)(rng.NextDouble() * 250.0 + 100.0),
                    heading       = (float)(rng.NextDouble() * 360.0),
                    verticalRate  = (float)(rng.NextDouble() * 20.0 - 10.0),
                    onGround      = false,
                    lastUpdate    = now,
                    originCountry = countries[rng.Next(countries.Length)],
                    aircraftType  = types[rng.Next(types.Length)]
                });
            }

            return list;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private IEnumerator PollData()
        {
            // Use a wide global bounding box by default; real use-cases should
            // restrict this based on the player camera position.
            yield return StartCoroutine(
                FetchAircraftInArea(-90, -180, 90, 180));
        }

        private IEnumerator FetchWithRetry(string url)
        {
            int   attempt = 0;
            float delay   = BaseRetryDelay;

            while (attempt <= MaxRetries)
            {
                if (attempt > 0)
                    yield return new WaitForSecondsRealtime(delay);

                bool success  = false;
                string body   = null;
                string error  = null;

                yield return StartCoroutine(GetRequest(url, (ok, b, e) =>
                {
                    success = ok;
                    body    = b;
                    error   = e;
                }));

                if (success)
                {
                    _consecutiveErrors = 0;
                    var list = config.apiProvider == LiveFlightDataSource.ADS_B_Exchange
                        ? ParseADSBResponse(body)
                        : ParseOpenSkyResponse(body);
                    OnAircraftDataReceived?.Invoke(list);
                    yield break;
                }

                attempt++;
                _consecutiveErrors++;
                delay = Mathf.Min(delay * 2f, MaxRetryDelay);
                Debug.LogWarning($"[LiveFlightAPIClient] Fetch attempt {attempt} failed: {error}");
            }

            string msg = $"Fetch failed after {MaxRetries} retries for provider {config.apiProvider}.";
            Debug.LogError($"[LiveFlightAPIClient] {msg}");
            OnFetchError?.Invoke(msg);
            LiveFlightAnalytics.TrackAPIError(config.apiProvider.ToString(), msg);
        }

        private IEnumerator GetRequest(string url, Action<bool, string, string> callback)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 15;

            if (!string.IsNullOrEmpty(config.apiKey))
                req.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            callback(ok,
                ok ? req.downloadHandler.text : null,
                ok ? null : req.error);
        }

        private string BuildUrl(double minLat, double minLon, double maxLat, double maxLon)
        {
            string inv = System.Globalization.CultureInfo.InvariantCulture.ToString();
            switch (config.apiProvider)
            {
                case LiveFlightDataSource.ADS_B_Exchange:
                    return $"{config.apiUrl}/v2/lat/{minLat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}" +
                           $"/lon/{minLon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}" +
                           $"/dist/{(config.displayRadiusKm).ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}/json/";

                default: // OpenSky
                    return $"{config.apiUrl}/states/all" +
                           $"?lamin={minLat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}" +
                           $"&lomin={minLon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}" +
                           $"&lamax={maxLat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}" +
                           $"&lomax={maxLon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        // ── OpenSky row parser ────────────────────────────────────────────────────

        private static LiveAircraftInfo ParseOpenSkyRow(string row)
        {
            // row looks like: ["abc123","UAL123","United States",1700000000,1700000001,-122.3,37.8,10000.0,false,250.0,180.0,2.5,null,10050.0,null,false,0]
            string[] tokens = SplitJsonArray(row);
            if (tokens.Length < 11)
                return default;

            return new LiveAircraftInfo
            {
                icao24        = StripQuotes(tokens[0]),
                callsign      = StripQuotes(tokens[1]).Trim(),
                originCountry = tokens.Length > 2  ? StripQuotes(tokens[2])              : "",
                lastUpdate    = tokens.Length > 4  ? ParseLong(tokens[4])                : 0L,
                longitude     = tokens.Length > 5  ? ParseDouble(tokens[5])              : 0.0,
                latitude      = tokens.Length > 6  ? ParseDouble(tokens[6])              : 0.0,
                altitude      = tokens.Length > 7  ? ParseFloat(tokens[7])               : 0f,
                onGround      = tokens.Length > 8  ? ParseBool(tokens[8])                : false,
                velocity      = tokens.Length > 9  ? ParseFloat(tokens[9])               : 0f,
                heading       = tokens.Length > 10 ? ParseFloat(tokens[10])              : 0f,
                verticalRate  = tokens.Length > 11 ? ParseFloat(tokens[11])              : 0f,
                aircraftType  = ""
            };
        }

        // ── ADS-B Exchange object parser ──────────────────────────────────────────

        private static LiveAircraftInfo ParseADSBObject(string obj)
        {
            return new LiveAircraftInfo
            {
                icao24        = GetJsonString(obj, "hex"),
                callsign      = GetJsonString(obj, "flight").Trim(),
                originCountry = "",
                latitude      = ParseDouble(GetJsonString(obj, "lat")),
                longitude     = ParseDouble(GetJsonString(obj, "lon")),
                altitude      = ParseFloat(GetJsonString(obj, "alt_baro")),
                velocity      = ParseFloat(GetJsonString(obj, "gs")),
                heading       = ParseFloat(GetJsonString(obj, "track")),
                verticalRate  = ParseFloat(GetJsonString(obj, "baro_rate")),
                onGround      = GetJsonString(obj, "alt_baro") == "ground",
                lastUpdate    = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                aircraftType  = GetJsonString(obj, "t")
            };
        }

        // ── Minimal JSON helpers ──────────────────────────────────────────────────

        private static string[] SplitJsonArray(string array)
        {
            // Strip outer brackets
            string inner = array.Trim();
            if (inner.StartsWith("[")) inner = inner.Substring(1);
            if (inner.EndsWith("]"))   inner = inner.Substring(0, inner.Length - 1);

            var tokens = new List<string>();
            int i = 0;
            while (i < inner.Length)
            {
                char c = inner[i];
                if (c == '"')
                {
                    int end = inner.IndexOf('"', i + 1);
                    while (end > 0 && inner[end - 1] == '\\')
                        end = inner.IndexOf('"', end + 1);
                    if (end < 0) end = inner.Length - 1;
                    tokens.Add(inner.Substring(i, end - i + 1));
                    i = end + 1;
                    // skip comma
                    while (i < inner.Length && (inner[i] == ',' || inner[i] == ' ')) i++;
                }
                else if (c == '[' || c == '{')
                {
                    // nested — skip
                    int d = 1; int j = i + 1;
                    char open  = c;
                    char close = c == '[' ? ']' : '}';
                    while (j < inner.Length && d > 0)
                    {
                        if (inner[j] == open)  d++;
                        if (inner[j] == close) d--;
                        j++;
                    }
                    tokens.Add(inner.Substring(i, j - i));
                    i = j;
                    while (i < inner.Length && (inner[i] == ',' || inner[i] == ' ')) i++;
                }
                else
                {
                    int comma = inner.IndexOf(',', i);
                    if (comma < 0) comma = inner.Length;
                    tokens.Add(inner.Substring(i, comma - i).Trim());
                    i = comma + 1;
                }
            }
            return tokens.ToArray();
        }

        private static string GetJsonString(string json, string key)
        {
            string search = $"\"{key}\"";
            int k = json.IndexOf(search, StringComparison.Ordinal);
            if (k < 0) return "";
            int colon = json.IndexOf(':', k + search.Length);
            if (colon < 0) return "";
            int vs = colon + 1;
            while (vs < json.Length && json[vs] == ' ') vs++;
            if (vs >= json.Length) return "";

            if (json[vs] == '"')
            {
                int end = json.IndexOf('"', vs + 1);
                return end < 0 ? "" : json.Substring(vs + 1, end - vs - 1);
            }
            else
            {
                int end = vs;
                while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
                return json.Substring(vs, end - vs).Trim();
            }
        }

        private static string StripQuotes(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2);
            if (s == "null") return "";
            return s;
        }

        private static float  ParseFloat(string s)  { float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v);  return v; }
        private static double ParseDouble(string s) { double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v); return v; }
        private static long   ParseLong(string s)   { long.TryParse(s, out long v);   return v; }
        private static bool   ParseBool(string s)   => s.Trim() == "true";
    }
}
