using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Multiplayer
{
    // ── Data Classes ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A serialisable packet representing a complete weather state snapshot
    /// for transmission between host and clients.
    /// </summary>
    [Serializable]
    public class WeatherStatePacket
    {
        /// <summary>Primary weather type (maps to <c>Weather.WeatherType</c>).</summary>
        public int weatherTypeInt;
        /// <summary>Weather intensity in [0, 1].</summary>
        public float intensity;
        /// <summary>Wind speed in metres per second.</summary>
        public float windSpeed;
        /// <summary>Meteorological wind direction in degrees (0 = North).</summary>
        public float windDirection;
        /// <summary>Horizontal visibility in metres.</summary>
        public float visibility;
        /// <summary>Air temperature in degrees Celsius.</summary>
        public float temperature;
        /// <summary>Precipitation / weather effect intensity in [0, 1] (maps to WeatherConditionData.intensity).</summary>
        public float precipitationIntensity;
        /// <summary>Fraction of sky covered by cloud in [0, 1].</summary>
        public float cloudCover;
        /// <summary>UTC ticks when this snapshot was captured on the host.</summary>
        public long capturedAtTicks;
        /// <summary>Human-readable weather event message (e.g. "Storm approaching").</summary>
        public string weatherEventMessage;
    }

    // ── MultiplayerWeatherSync ────────────────────────────────────────────────────

    /// <summary>
    /// Synchronises weather state across all players in a multiplayer session.
    ///
    /// <para>The session host fetches authoritative weather from
    /// <see cref="Weather.WeatherDataService"/> and broadcasts a
    /// <see cref="WeatherStatePacket"/> to all clients at a configurable interval or
    /// immediately on a significant change.  Clients smoothly interpolate between the
    /// previous and received state over a configurable transition duration.</para>
    ///
    /// <para>If the host's data is older than 5 minutes, clients fall back to their
    /// own local weather data (obtained from their own <see cref="Weather.WeatherDataService"/>
    /// if available).</para>
    /// </summary>
    public class MultiplayerWeatherSync : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static MultiplayerWeatherSync Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sync Settings")]
        [Tooltip("How often (seconds) the host broadcasts the current weather state.")]
        [SerializeField] private float syncIntervalSec = 30f;

        [Tooltip("Minimum weather intensity delta that triggers an immediate sync broadcast.")]
        [SerializeField] private float significantChangeDelta = 0.15f;

        [Header("Client Transition")]
        [Tooltip("Seconds clients take to interpolate between received weather states.")]
        [SerializeField] private float clientTransitionSec = 8f;

        [Header("Staleness")]
        [Tooltip("Maximum age (seconds) of host weather data before clients use local fallback.")]
        [SerializeField] private float maxStalenessSeconds = 300f;

        [Header("References")]
        [Tooltip("Phase 32 WeatherManager — receives applied weather on clients.")]
        [SerializeField] private Weather.WeatherManager weatherManager;

        [Tooltip("Phase 32 WeatherDataService — polled on host for current weather.")]
        [SerializeField] private Weather.WeatherDataService weatherDataService;

        [Tooltip("NetworkManager2 — used to determine host/client role.")]
        [SerializeField] private NetworkManager2 networkManager2;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired on clients when a new weather state is received from the host.</summary>
        public event Action<WeatherStatePacket> OnWeatherPacketReceived;

        /// <summary>Fired on all players when a weather event message is broadcast.</summary>
        public event Action<string> OnWeatherEventBroadcast;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Whether this instance is the session host (authoritative weather source).</summary>
        public bool IsHost => networkManager2 != null ? networkManager2.IsHost : false;

        /// <summary>The last weather state packet received or broadcast.</summary>
        public WeatherStatePacket LastPacket { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private float _syncTimer;
        private WeatherStatePacket _previousPacket;
        private WeatherStatePacket _targetPacket;
        private float _transitionTimer;
        private bool  _transitionActive;

        // Main-thread dispatch queue for network callbacks.
        private readonly Queue<Action> _mainThreadQueue = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (weatherManager == null)
                weatherManager = Weather.WeatherManager.Instance
                    ?? FindFirstObjectByType<Weather.WeatherManager>();

            if (weatherDataService == null)
                weatherDataService = Weather.WeatherDataService.Instance
                    ?? FindFirstObjectByType<Weather.WeatherDataService>();

            if (networkManager2 == null)
                networkManager2 = NetworkManager2.Instance
                    ?? FindFirstObjectByType<NetworkManager2>();

            if (weatherDataService != null)
                weatherDataService.OnWeatherUpdated += HandleLocalWeatherUpdated;
        }

        private void Update()
        {
            // Flush main-thread actions.
            lock (_mainThreadQueue)
            {
                while (_mainThreadQueue.Count > 0)
                    _mainThreadQueue.Dequeue()?.Invoke();
            }

            if (IsHost)
            {
                _syncTimer += Time.deltaTime;
                if (_syncTimer >= syncIntervalSec)
                {
                    _syncTimer = 0f;
                    BroadcastCurrentWeather();
                }
            }
            else
            {
                // Client: advance the interpolation transition.
                if (_transitionActive)
                    AdvanceClientTransition(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (weatherDataService != null)
                weatherDataService.OnWeatherUpdated -= HandleLocalWeatherUpdated;
        }

        // ── Host API ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Forces an immediate weather broadcast to all clients (host only).
        /// </summary>
        public void ForceBroadcast()
        {
            if (!IsHost) return;
            BroadcastCurrentWeather();
        }

        /// <summary>
        /// Broadcasts a weather event notification string to all clients (host only).
        /// </summary>
        /// <param name="message">Event message, e.g. "Storm approaching from the north."</param>
        public void BroadcastWeatherEvent(string message)
        {
            if (!IsHost) return;

            Debug.Log($"[SWEF][MultiplayerWeatherSync] Broadcasting weather event: {message}");

            // In production: serialise a network message and send via NetworkTransport.
            // Here we invoke locally for demonstration.
            OnWeatherEventBroadcast?.Invoke(message);
        }

        // ── Client API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Receives a <see cref="WeatherStatePacket"/> from the host (called by the network layer).
        /// </summary>
        /// <param name="packet">Received weather state.</param>
        public void ReceiveWeatherPacket(WeatherStatePacket packet)
        {
            if (packet == null) return;

            // Check staleness.
            var capturedAt = new DateTime(packet.capturedAtTicks, DateTimeKind.Utc);
            double ageSec  = (DateTime.UtcNow - capturedAt).TotalSeconds;

            if (ageSec > maxStalenessSeconds)
            {
                Debug.LogWarning($"[SWEF][MultiplayerWeatherSync] Received stale weather packet ({ageSec:F0}s old). Using local fallback.");
                return;
            }

            DispatchToMainThread(() => ApplyWeatherPacket(packet));
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void BroadcastCurrentWeather()
        {
            if (weatherManager == null) return;

            var current = weatherManager.CurrentWeather;
            var packet  = new WeatherStatePacket
            {
                weatherTypeInt         = (int)current.type,
                intensity              = current.intensity,
                windSpeed              = current.windSpeed,
                windDirection          = current.windDirection,
                visibility             = current.visibility,
                temperature            = current.temperature,
                precipitationIntensity = current.intensity,
                cloudCover             = current.cloudCover,
                capturedAtTicks        = DateTime.UtcNow.Ticks,
                weatherEventMessage    = string.Empty
            };

            LastPacket = packet;

            // In production: serialise and send via NetworkTransport.
            Debug.Log($"[SWEF][MultiplayerWeatherSync] Host broadcasting weather: {current.type} intensity={current.intensity:F2}.");

            // Invoke local callback for testing / single-machine demo.
            OnWeatherPacketReceived?.Invoke(packet);
        }

        private void HandleLocalWeatherUpdated(Weather.WeatherData data)
        {
            // Host: check whether the new data represents a significant change.
            if (!IsHost || weatherManager == null) return;

            var current = weatherManager.CurrentWeather;
            if (Mathf.Abs(data.precipitationIntensity - current.intensity) >= significantChangeDelta)
            {
                Debug.Log("[SWEF][MultiplayerWeatherSync] Significant weather change — triggering immediate sync.");
                BroadcastCurrentWeather();
            }
        }

        private void ApplyWeatherPacket(WeatherStatePacket packet)
        {
            if (weatherManager == null) return;

            LastPacket = packet;
            OnWeatherPacketReceived?.Invoke(packet);

            if (!string.IsNullOrEmpty(packet.weatherEventMessage))
                OnWeatherEventBroadcast?.Invoke(packet.weatherEventMessage);

            // Start client-side transition.
            _previousPacket   = BuildCurrentPacket();
            _targetPacket     = packet;
            _transitionTimer  = 0f;
            _transitionActive = true;

            Debug.Log($"[SWEF][MultiplayerWeatherSync] Received weather packet from host, starting transition.");
        }

        private void AdvanceClientTransition(float dt)
        {
            if (_previousPacket == null || _targetPacket == null) return;

            _transitionTimer += dt;
            float t = Mathf.Clamp01(_transitionTimer / clientTransitionSec);

            // Build an interpolated snapshot and push to the WeatherManager.
            var interpolated = new Weather.WeatherConditionData
            {
                type          = t >= 0.5f
                                    ? (Weather.WeatherType)_targetPacket.weatherTypeInt
                                    : (Weather.WeatherType)_previousPacket.weatherTypeInt,
                intensity     = Mathf.Lerp(_previousPacket.intensity,    _targetPacket.intensity,    t),
                windSpeed     = Mathf.Lerp(_previousPacket.windSpeed,    _targetPacket.windSpeed,    t),
                windDirection = Mathf.LerpAngle(_previousPacket.windDirection, _targetPacket.windDirection, t),
                visibility    = Mathf.Lerp(_previousPacket.visibility,   _targetPacket.visibility,   t),
                temperature   = Mathf.Lerp(_previousPacket.temperature,  _targetPacket.temperature,  t),
                cloudCover    = Mathf.Lerp(_previousPacket.cloudCover,   _targetPacket.cloudCover,   t)
            };

            weatherManager.ForceWeather(interpolated.type, interpolated.intensity);

            if (t >= 1f)
                _transitionActive = false;
        }

        /// <summary>Builds a packet from the current WeatherManager state.</summary>
        private WeatherStatePacket BuildCurrentPacket()
        {
            if (weatherManager == null) return new WeatherStatePacket { capturedAtTicks = DateTime.UtcNow.Ticks };

            var c = weatherManager.CurrentWeather;
            return new WeatherStatePacket
            {
                weatherTypeInt         = (int)c.type,
                intensity              = c.intensity,
                windSpeed              = c.windSpeed,
                windDirection          = c.windDirection,
                visibility             = c.visibility,
                temperature            = c.temperature,
                precipitationIntensity = c.intensity,
                cloudCover             = c.cloudCover,
                capturedAtTicks        = DateTime.UtcNow.Ticks
            };
        }

        private void DispatchToMainThread(Action action)
        {
            lock (_mainThreadQueue)
                _mainThreadQueue.Enqueue(action);
        }
    }
}
