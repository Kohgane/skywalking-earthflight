// FlightContextAnalyzer.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using UnityEngine;
using SWEF.Flight;
using SWEF.Biome;
using SWEF.Damage;
using SWEF.Emergency;
using SWEF.TimeOfDay;
using SWEF.Weather;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Samples all relevant game systems each tick and builds a
    /// <see cref="FlightMusicContext"/> snapshot for the adaptive music pipeline.
    ///
    /// <para>Every integration point is null-safe; missing systems use sensible defaults
    /// so the music system degrades gracefully when not all phases are present.</para>
    /// </summary>
    public class FlightContextAnalyzer : MonoBehaviour
    {
        // ── Inspector (optional explicit refs) ────────────────────────────────────
        [Header("Optional References (auto-found if null)")]
        [SerializeField] private FlightController        flightController;
        [SerializeField] private AltitudeController      altitudeController;
        [SerializeField] private FlightPhysicsIntegrator flightPhysicsIntegrator;
        [SerializeField] private WeatherManager          weatherManager;
        [SerializeField] private TimeOfDayManager        timeOfDayManager;
        [SerializeField] private EmergencyManager        emergencyManager;
        [SerializeField] private DamageModel             damageModel;

        // ── State ─────────────────────────────────────────────────────────────────
        private FlightMusicContext _context = FlightMusicContext.Default();

        /// <summary>Time (seconds) after a mission completion to keep the Triumphant flag set.</summary>
        private const float MissionCompletedWindow = 10f;
        private float _missionCompletedTimer;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Auto-find references
            if (flightController        == null)
                flightController        = FindFirstObjectByType<FlightController>();
            if (altitudeController      == null)
                altitudeController      = FindFirstObjectByType<AltitudeController>();
            if (flightPhysicsIntegrator == null)
                flightPhysicsIntegrator = FindFirstObjectByType<FlightPhysicsIntegrator>();
            if (weatherManager          == null)
                weatherManager          = FindFirstObjectByType<WeatherManager>();
            if (timeOfDayManager        == null)
                timeOfDayManager        = FindFirstObjectByType<TimeOfDayManager>();
            if (emergencyManager        == null)
                emergencyManager        = FindFirstObjectByType<EmergencyManager>();
            if (damageModel             == null)
                damageModel             = FindFirstObjectByType<DamageModel>();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the most recently built context snapshot.</summary>
        public FlightMusicContext Context => _context;

        /// <summary>Rebuilds the <see cref="FlightMusicContext"/> from the current game state.</summary>
        public FlightMusicContext BuildContext()
        {
            FlightMusicContext ctx = FlightMusicContext.Default();

            // ── Flight ────────────────────────────────────────────────────────────
            if (flightController != null)
            {
                ctx.speed    = flightController.CurrentSpeedMps;
                ctx.isFlying = flightController.IsFlying;
            }

            if (altitudeController != null)
            {
                ctx.altitude  = altitudeController.CurrentAltitudeMeters;
                ctx.isInSpace = ctx.altitude > 100_000f;
                ctx.isLanding = ctx.altitude < 500f && ctx.isFlying;
            }

            // FlightPhysicsIntegrator does not expose GForce/IsStalling as direct public
            // properties; values remain at defaults (gForce=1, stallWarning=false).
            // TODO: Wire ctx.gForce and ctx.stallWarning once FlightPhysicsIntegrator exposes them.

            // ── Weather ───────────────────────────────────────────────────────────
            if (weatherManager != null)
            {
                WeatherConditionData weather = weatherManager.CurrentWeather;
                if (weather != null)
                {
                    ctx.weatherIntensity = weather.intensity;
                    ctx.inStorm          = weather.intensity > 0.7f;
                }
            }

            // ── Time of Day ───────────────────────────────────────────────────────
            if (timeOfDayManager != null)
            {
                ctx.timeOfDay = timeOfDayManager.CurrentHour;
                SunMoonState sunState = timeOfDayManager.GetSunMoonState();
                if (sunState != null)
                    ctx.sunAltitudeDeg = sunState.sunAltitudeDeg;
            }

            // ── Biome ─────────────────────────────────────────────────────────────
            if (flightController != null)
            {
                Vector3 pos     = flightController.transform.position;
                BiomeType biome = BiomeClassifier.ClassifyBiome(pos.z, pos.x, ctx.altitude);
                ctx.biomeType   = biome.ToString();
            }

            // ── Emergency ─────────────────────────────────────────────────────────
            if (emergencyManager != null)
                ctx.hasActiveEmergency = emergencyManager.ActiveEmergencies.Count > 0;

            // ── Damage ────────────────────────────────────────────────────────────
            if (damageModel != null)
                ctx.damageLevel = 1f - Mathf.Clamp01(damageModel.GetOverallHealth() / 100f);

            // ── Danger ────────────────────────────────────────────────────────────
            ctx.dangerLevel = CalculateDangerLevel(ctx);

            // ── Mission completion window ─────────────────────────────────────────
            if (_missionCompletedTimer > 0f)
            {
                _missionCompletedTimer -= Time.deltaTime;
                ctx.missionJustCompleted = true;
            }

            _context = ctx;
            return ctx;
        }

        /// <summary>Call this when a mission is successfully completed.</summary>
        public void NotifyMissionCompleted()
        {
            _missionCompletedTimer = MissionCompletedWindow;
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private static float CalculateDangerLevel(FlightMusicContext ctx)
        {
            float danger = 0f;

            if (ctx.hasActiveEmergency)
                danger = Mathf.Max(danger, 0.8f);

            danger = Mathf.Max(danger, ctx.damageLevel);

            if (ctx.stallWarning)
                danger = Mathf.Max(danger, 0.6f);

            if (ctx.weatherIntensity > 0.7f)
                danger = Mathf.Max(danger, ctx.weatherIntensity * 0.8f);

            return Mathf.Clamp01(danger);
        }
    }
}
