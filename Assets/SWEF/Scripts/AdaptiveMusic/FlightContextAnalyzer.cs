// FlightContextAnalyzer.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// MonoBehaviour that builds a <see cref="FlightMusicContext"/> each tick by
    /// sampling relevant game systems.  All integrations are null-safe with
    /// sensible defaults so the system works even when optional subsystems are absent.
    /// </summary>
    public class FlightContextAnalyzer : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Update Rate")]
        [Tooltip("How often (seconds) the flight context is refreshed.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _updateInterval = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────

        private FlightMusicContext _context = FlightMusicContext.Default();
        private float              _nextUpdateTime;
        private float              _missionCompleteTimer;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns the most recently computed <see cref="FlightMusicContext"/>.</summary>
        public FlightMusicContext Context => _context;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Time.time < _nextUpdateTime) return;
            _nextUpdateTime = Time.time + _updateInterval;
            RefreshContext();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void RefreshContext()
        {
            var ctx = FlightMusicContext.Default();

            SampleFlightController(ref ctx);
            SampleWeather(ref ctx);
            SampleTimeOfDay(ref ctx);
            SampleBiome(ref ctx);
            SampleMission(ref ctx);
            SampleDamage(ref ctx);
            SampleEmergency(ref ctx);

            _context = ctx;
        }

        private static void SampleFlightController(ref FlightMusicContext ctx)
        {
#if SWEF_FLIGHT_AVAILABLE
            var fc = SWEF.Flight.FlightController.Instance;
            if (fc != null)
            {
                ctx.speed     = fc.Airspeed;
                ctx.isLanding = fc.IsLanding;
            }

            var ac = SWEF.Flight.AltitudeController.Instance;
            if (ac != null)
            {
                ctx.altitude  = ac.AltitudeMetres;
                ctx.isInSpace = ac.AltitudeMetres >= 100_000f;
            }

            var fp = SWEF.Flight.FlightPhysicsIntegrator.Instance;
            if (fp != null)
            {
                ctx.gForce      = fp.GForce;
                ctx.stallWarning = fp.IsStalling;
            }
#endif
        }

        private static void SampleWeather(ref FlightMusicContext ctx)
        {
#if SWEF_WEATHER_AVAILABLE
            var wm = SWEF.Weather.WeatherManager.Instance;
            if (wm != null)
                ctx.weatherIntensity = wm.CurrentIntensity;
#endif
        }

        private static void SampleTimeOfDay(ref FlightMusicContext ctx)
        {
#if SWEF_TIMEOFDAY_AVAILABLE
            var tod = SWEF.TimeOfDay.TimeOfDayManager.Instance;
            if (tod != null)
            {
                ctx.timeOfDay      = tod.HourOfDay;
                ctx.sunAltitudeDeg = tod.SunAltitudeDegrees;
            }
#endif
        }

        private static void SampleBiome(ref FlightMusicContext ctx)
        {
#if SWEF_BIOME_AVAILABLE
            var bc = SWEF.Biome.BiomeClassifier.Instance;
            if (bc != null)
                ctx.biomeType = bc.CurrentBiomeId;
#endif
        }

        private static void SampleMission(ref FlightMusicContext ctx)
        {
#if SWEF_PASSENGERCARGO_AVAILABLE
            var mm = SWEF.PassengerCargo.TransportMissionManager.Instance;
            if (mm != null)
            {
                ctx.isInMission          = mm.HasActiveMission;
                ctx.missionJustCompleted = mm.WasJustCompleted;
            }
#endif
        }

        private static void SampleDamage(ref FlightMusicContext ctx)
        {
#if SWEF_DAMAGE_AVAILABLE
            var dm = SWEF.Damage.DamageModel.Instance;
            if (dm != null)
                ctx.damageLevel = dm.NormalizedDamage;
#endif
        }

        private static void SampleEmergency(ref FlightMusicContext ctx)
        {
#if SWEF_EMERGENCY_AVAILABLE
            var em = SWEF.Emergency.EmergencyManager.Instance;
            if (em != null)
            {
                bool hasEmergency = em.HasActiveEmergency;
                if (hasEmergency)
                    ctx.dangerLevel = Mathf.Max(ctx.dangerLevel, 1f);
                ctx.isInCombatZone = em.IsInCombatZone;
            }
#endif
        }
    }
}
