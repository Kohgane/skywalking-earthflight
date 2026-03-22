using System;
using UnityEngine;
using SWEF.Flight;
using SWEF.Settings;
using SWEF.Atmosphere;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Central singleton that drives the Dynamic Time-of-Day system.
    /// Survives scene loads via <c>DontDestroyOnLoad</c>.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Advances simulated time each frame (or syncs to real-world UTC).</item>
    ///   <item>Tracks the player's geographic position (lat/lon) from <see cref="FlightController"/>.</item>
    ///   <item>Recalculates sun/moon state via <see cref="SolarCalculator"/>.</item>
    ///   <item>Fires phase-change events consumed by other subsystems.</item>
    ///   <item>Persists user preferences to <see cref="SettingsManager"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TimeOfDayManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static TimeOfDayManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [Tooltip("Runtime configuration for the time-of-day system.")]
        [SerializeField] private TimeOfDayConfig config = new TimeOfDayConfig();

        [Header("References (auto-found if null)")]
        [Tooltip("FlightController for reading the player's position.")]
        [SerializeField] private FlightController flightController;

        [Tooltip("AtmosphereController for sky/fog integration. Reserved for future atmospheric scattering integration.")]
        [SerializeField] private AtmosphereController atmosphereController;

        [Tooltip("SettingsManager for persisting time preferences.")]
        [SerializeField] private SettingsManager settingsManager;

        // ── PlayerPrefs keys ─────────────────────────────────────────────────────
        private const string KeyPrefix    = "SWEF_TOD_";
        private const string KeyTimeScale = KeyPrefix + "TimeScale";
        private const string KeyUseReal   = KeyPrefix + "UseRealTime";
        private const string KeyStartHour = KeyPrefix + "StartHour";

        // ── State ─────────────────────────────────────────────────────────────────
        private DateTime   _currentUtc;
        private bool       _paused;
        private float      _accumulator;   // sub-second leftover for simulation mode
        private DayPhase   _lastDayPhase   = DayPhase.Day;
        private Season     _lastSeason     = Season.Spring;
        private int        _lastHour       = -1;
        private float      _currentLat;
        private float      _currentLon;
        private float      _lightingTimer;

        private SunMoonState    _sunMoonState    = new SunMoonState();
        private LightingSnapshot _currentLighting = new LightingSnapshot();

        // ── Public properties ────────────────────────────────────────────────────

        /// <summary>Current simulated UTC date and time.</summary>
        public DateTime CurrentDateTime => _currentUtc;

        /// <summary>Current time expressed as a fractional hour (0–24).</summary>
        public float CurrentHour => (float)(_currentUtc.Hour + _currentUtc.Minute / 60.0 + _currentUtc.Second / 3600.0);

        /// <summary>Current meteorological season.</summary>
        public Season CurrentSeason { get; private set; } = Season.Spring;

        /// <summary>Current day phase based on the sun's altitude.</summary>
        public DayPhase CurrentDayPhase => _sunMoonState.currentDayPhase;

        /// <summary>Current geographic latitude of the player.</summary>
        public float Latitude => _currentLat;

        /// <summary>Current geographic longitude of the player.</summary>
        public float Longitude => _currentLon;

        /// <summary>Whether time advancement is currently paused.</summary>
        public bool IsPaused => _paused;

        /// <summary>Current simulation time scale (1 = real-time, 60 = one minute per second).</summary>
        public float CurrentTimeScale => config.timeScale;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when the day phase transitions (e.g. Day → GoldenHour).</summary>
        public event Action<DayPhase, DayPhase> OnDayPhaseChanged;

        /// <summary>Fired once each simulated sunrise.</summary>
        public event Action OnSunrise;

        /// <summary>Fired once each simulated sunset.</summary>
        public event Action OnSunset;

        /// <summary>Fired when the calendar season changes.</summary>
        public event Action<Season> OnSeasonChanged;

        /// <summary>Fired when the simulated hour ticks over (integer hour boundary).</summary>
        public event Action<int> OnHourChanged;

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

            LoadPreferences();
            Initialise();
        }

        private void Update()
        {
            if (_paused) return;

            AdvanceTime();
            TrackPlayerPosition();

            _lightingTimer += Time.deltaTime;
            if (_lightingTimer >= config.lightingUpdateInterval)
            {
                _lightingTimer = 0f;
                RecalculateSunMoon();
                FireEvents();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Sets the simulated hour of day (0–24), keeping the current date.</summary>
        public void SetTime(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            int h = (int)hour;
            int m = (int)((hour - h) * 60f);
            int s = (int)(((hour - h) * 60f - m) * 60f);
            _currentUtc = new DateTime(_currentUtc.Year, _currentUtc.Month, _currentUtc.Day, h, m, s, DateTimeKind.Utc);
        }

        /// <summary>Replaces the current simulated date and time.</summary>
        public void SetDate(DateTime utcDateTime) => _currentUtc = utcDateTime;

        /// <summary>
        /// Changes the simulation time scale.
        /// <c>1.0</c> = real-time. <c>60</c> = one game-minute per real-second.
        /// Setting this automatically disables <see cref="TimeOfDayConfig.useRealWorldTime"/>.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            config.timeScale      = Mathf.Max(0f, scale);
            config.useRealWorldTime = false;
            SavePreferences();
        }

        /// <summary>Returns a snapshot of the current sun and moon positional data.</summary>
        public SunMoonState GetSunMoonState() => _sunMoonState;

        /// <summary>Returns the most recently computed lighting snapshot.</summary>
        public LightingSnapshot GetCurrentLighting() => _currentLighting;

        /// <summary>Skips forward by <paramref name="hours"/> hours of simulation time.</summary>
        public void FastForward(float hours)
        {
            _currentUtc = _currentUtc.AddHours(hours);
            RecalculateSunMoon();
        }

        /// <summary>Rewinds by <paramref name="hours"/> hours of simulation time.</summary>
        public void Rewind(float hours) => FastForward(-hours);

        /// <summary>Suspends time advancement.</summary>
        public void PauseTime()  => _paused = true;

        /// <summary>Resumes time advancement.</summary>
        public void ResumeTime() => _paused = false;

        // ── Private helpers ───────────────────────────────────────────────────────

        private void Initialise()
        {
            _currentLat = config.latitude;
            _currentLon = config.longitude;

            if (config.useRealWorldTime)
            {
                _currentUtc = DateTime.UtcNow;
            }
            else
            {
                var today = DateTime.UtcNow;
                int h = (int)config.startingHour;
                int m = (int)((config.startingHour - h) * 60f);
                _currentUtc = new DateTime(today.Year, today.Month, today.Day, h, m, 0, DateTimeKind.Utc);
            }

            RecalculateSunMoon();

            _lastDayPhase = _sunMoonState.currentDayPhase;
            _lastSeason   = CurrentSeason;
            _lastHour     = (int)CurrentHour;
        }

        private void AdvanceTime()
        {
            if (config.useRealWorldTime)
            {
                _currentUtc = DateTime.UtcNow;
            }
            else
            {
                double secondsToAdd = Time.deltaTime * config.timeScale;
                _currentUtc = _currentUtc.AddSeconds(secondsToAdd);
            }
        }

        private void TrackPlayerPosition()
        {
            if (flightController == null)
            {
                flightController = FindFirstObjectByType<FlightController>();
                if (flightController == null) return;
            }

            // Convert Unity world-space position to approximate geographic coordinates.
            // Convention used across SWEF: 1 Unity unit ≈ 1 metre.
            // Latitude offset: north/south from reference point.
            // Longitude offset: east/west from reference point.
            Vector3 pos = flightController.transform.position;
            _currentLat = Mathf.Clamp(config.latitude  + pos.z * 0.000009f, -90f,  90f);
            _currentLon = config.longitude + pos.x * 0.000009f;
            _currentLon = (_currentLon + 180f) % 360f - 180f; // wrap to −180..+180
        }

        private void RecalculateSunMoon()
        {
            // ── Sun ───────────────────────────────────────────────────────────────
            var (sunAlt, sunAz) = SolarCalculator.CalculateSunPosition(_currentUtc, _currentLat, _currentLon);

            _sunMoonState.sunAltitudeDeg  = sunAlt;
            _sunMoonState.sunAzimuthDeg   = sunAz;
            _sunMoonState.sunDirection    = AltAzToDirection(sunAlt, sunAz);
            _sunMoonState.currentDayPhase = SolarCalculator.GetDayPhase(sunAlt);
            _sunMoonState.isDaytime       = sunAlt > 0f;
            _sunMoonState.sunriseTime     = SolarCalculator.CalculateSunrise(_currentUtc, _currentLat, _currentLon);
            _sunMoonState.sunsetTime      = SolarCalculator.CalculateSunset(_currentUtc, _currentLat, _currentLon);
            _sunMoonState.dayLengthHours  = SolarCalculator.CalculateDayLength(_currentUtc, _currentLat, _currentLon);

            // ── Moon ──────────────────────────────────────────────────────────────
            if (config.enableMoonCycle)
            {
                var (moonAlt, moonAz) = SolarCalculator.CalculateMoonPosition(_currentUtc, _currentLat, _currentLon);
                _sunMoonState.moonAltitudeDeg  = moonAlt;
                _sunMoonState.moonAzimuthDeg   = moonAz;
                _sunMoonState.moonDirection    = AltAzToDirection(moonAlt, moonAz);
                _sunMoonState.moonPhase        = SolarCalculator.GetMoonPhase(_currentUtc);
                _sunMoonState.moonIllumination = SolarCalculator.GetMoonIllumination(_currentUtc);
            }

            // ── Season ────────────────────────────────────────────────────────────
            CurrentSeason = config.seasonOverride.HasValue
                ? config.seasonOverride.Value
                : SolarCalculator.GetSeason(_currentUtc, _currentLat);

            // ── Lighting snapshot ─────────────────────────────────────────────────
            BuildLightingSnapshot();
        }

        private void FireEvents()
        {
            // Day phase change
            DayPhase newPhase = _sunMoonState.currentDayPhase;
            if (newPhase != _lastDayPhase)
            {
                DayPhase prev = _lastDayPhase;
                _lastDayPhase = newPhase;
                OnDayPhaseChanged?.Invoke(prev, newPhase);

                // Specific sunrise / sunset events
                bool wasNight = prev == DayPhase.Night || prev == DayPhase.AstronomicalTwilight;
                bool isDay    = newPhase == DayPhase.CivilTwilight || newPhase == DayPhase.GoldenHour || newPhase == DayPhase.Day;
                if (wasNight && isDay) OnSunrise?.Invoke();

                bool wasDay   = prev == DayPhase.Day || prev == DayPhase.GoldenHour || prev == DayPhase.CivilTwilight;
                bool isNight  = newPhase == DayPhase.Night || newPhase == DayPhase.AstronomicalTwilight;
                if (wasDay && isNight) OnSunset?.Invoke();
            }

            // Season change
            if (CurrentSeason != _lastSeason)
            {
                _lastSeason = CurrentSeason;
                OnSeasonChanged?.Invoke(CurrentSeason);
            }

            // Hour tick
            int hour = (int)CurrentHour;
            if (hour != _lastHour)
            {
                _lastHour = hour;
                OnHourChanged?.Invoke(hour);
            }
        }

        private void BuildLightingSnapshot()
        {
            float alt   = _sunMoonState.sunAltitudeDeg;
            float t01   = Mathf.InverseLerp(-18f, 90f, alt); // normalised sun altitude

            // Sun color — warm orange near horizon, white at zenith
            Color sunHorizon = new Color(1.0f, 0.55f, 0.2f);
            Color sunDay     = new Color(1.0f, 0.95f, 0.85f);
            _currentLighting.sunColor     = Color.Lerp(sunHorizon, sunDay, Mathf.Clamp01(t01 * 2f));
            _currentLighting.sunIntensity = Mathf.Clamp01(Mathf.InverseLerp(-6f, 15f, alt)) * 1.2f;

            // Moon
            _currentLighting.moonIntensity = _sunMoonState.isDaytime ? 0f :
                Mathf.Clamp01(Mathf.InverseLerp(-5f, 15f, _sunMoonState.moonAltitudeDeg))
                    * _sunMoonState.moonIllumination * 0.15f;

            // Ambient
            Color nightAmbient  = new Color(0.05f, 0.05f, 0.12f);
            Color dawnAmbient   = new Color(0.55f, 0.35f, 0.30f);
            Color dayAmbient    = new Color(0.55f, 0.65f, 0.90f);
            float ambT          = Mathf.Clamp01(Mathf.InverseLerp(-10f, 20f, alt));
            _currentLighting.ambientSkyColor     = Color.Lerp(nightAmbient, Color.Lerp(dawnAmbient, dayAmbient, ambT), ambT);
            _currentLighting.ambientEquatorColor = Color.Lerp(new Color(0.05f, 0.05f, 0.1f), new Color(0.5f, 0.55f, 0.65f), ambT);
            _currentLighting.ambientGroundColor  = Color.Lerp(new Color(0.02f, 0.02f, 0.02f), new Color(0.22f, 0.20f, 0.15f), ambT);

            // Fog
            _currentLighting.fogColor   = Color.Lerp(new Color(0.1f, 0.1f, 0.2f), new Color(0.7f, 0.78f, 0.9f), Mathf.Clamp01(ambT * 1.3f));
            _currentLighting.fogDensity = Mathf.Lerp(0.0005f, 0.0002f, ambT);

            // Skybox
            _currentLighting.skyboxExposure = Mathf.Lerp(0.05f, 1.0f, Mathf.Clamp01(ambT * 1.2f));
            _currentLighting.skyboxTint     = Color.Lerp(new Color(0.1f, 0.1f, 0.3f), Color.white, ambT);

            // Shadows
            _currentLighting.shadowStrength = Mathf.Lerp(0f, 1f, Mathf.Clamp01(_currentLighting.sunIntensity * 1.5f));
            _currentLighting.shadowColor    = Color.Lerp(new Color(0.15f, 0.15f, 0.25f), new Color(0.1f, 0.1f, 0.14f), ambT);

            // Stars visible at night
            _currentLighting.starVisibility = Mathf.Clamp01(Mathf.InverseLerp(-6f, -18f, alt));
        }

        private static Vector3 AltAzToDirection(float altDeg, float azDeg)
        {
            float altRad = altDeg * Mathf.Deg2Rad;
            float azRad  = azDeg  * Mathf.Deg2Rad;
            // Convert spherical → Cartesian (Y-up, Z-north convention)
            float cosAlt = Mathf.Cos(altRad);
            return new Vector3(
                cosAlt * Mathf.Sin(azRad),
                Mathf.Sin(altRad),
                cosAlt * Mathf.Cos(azRad)
            ).normalized;
        }

        private void LoadPreferences()
        {
            if (PlayerPrefs.HasKey(KeyTimeScale))
                config.timeScale = PlayerPrefs.GetFloat(KeyTimeScale);
            if (PlayerPrefs.HasKey(KeyUseReal))
                config.useRealWorldTime = PlayerPrefs.GetInt(KeyUseReal) == 1;
            if (PlayerPrefs.HasKey(KeyStartHour))
                config.startingHour = PlayerPrefs.GetFloat(KeyStartHour);
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetFloat(KeyTimeScale, config.timeScale);
            PlayerPrefs.SetInt(KeyUseReal, config.useRealWorldTime ? 1 : 0);
            PlayerPrefs.SetFloat(KeyStartHour, config.startingHour);
            PlayerPrefs.Save();
        }

        // ── Editor gizmos ────────────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            // Sun direction — yellow
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawLine(origin, origin + _sunMoonState.sunDirection * 10f);
            // Moon direction — cyan
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawLine(origin, origin + _sunMoonState.moonDirection * 10f);
        }
#endif
    }
}
