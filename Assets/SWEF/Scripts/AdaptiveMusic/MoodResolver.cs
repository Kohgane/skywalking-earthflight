// MoodResolver.cs — SWEF Dynamic Soundtrack & Adaptive Music System
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Static utility that converts a <see cref="FlightMusicContext"/> into a
    /// <see cref="MusicMood"/> and an intensity value (0–1) using a
    /// priority-ordered rule set.
    /// </summary>
    public static class MoodResolver
    {
        // ── Constants ──────────────────────────────────────────────────────────

        private const float SpaceAltitudeThreshold   = 100_000f; // 100 km
        private const float Mach08SpeedMs            = 272f;     // ~0.8 Mach at sea level (m/s)
        private const float DangerDamageThreshold    = 0.60f;
        private const float WeatherTenseThreshold    = 0.70f;
        private const float GForceTenseThreshold     = 3.0f;
        private const float GoldenHourSunMin         = 0f;
        private const float GoldenHourSunMax         = 6f;

        // ── Mood Resolution ────────────────────────────────────────────────────

        /// <summary>
        /// Determines the target <see cref="MusicMood"/> from the supplied context
        /// using priority-ordered rules (highest priority first).
        /// </summary>
        public static MusicMood ResolveMood(FlightMusicContext ctx)
        {
            // Priority 1 — Emergency / heavy damage
            if (ctx.isInCombatZone || ctx.dangerLevel >= 1f || ctx.damageLevel >= DangerDamageThreshold)
                return MusicMood.Danger;

            // Priority 2 — High-G or stall
            if (ctx.gForce >= GForceTenseThreshold || ctx.stallWarning)
                return MusicMood.Tense;

            // Priority 3 — Storm / severe weather
            if (ctx.weatherIntensity >= WeatherTenseThreshold)
                return MusicMood.Tense;

            // Priority 4 — Space (above Kármán line)
            if (ctx.isInSpace || ctx.altitude >= SpaceAltitudeThreshold)
                return MusicMood.Epic;

            // Priority 5 — Mission just completed
            if (ctx.missionJustCompleted)
                return MusicMood.Triumphant;

            // Priority 6 — Golden hour (sun 0–6° above horizon)
            if (ctx.sunAltitudeDeg >= GoldenHourSunMin && ctx.sunAltitudeDeg <= GoldenHourSunMax)
                return MusicMood.Serene;

            // Priority 7 — Night + clear sky
            bool isNight = ctx.timeOfDay >= 20f || ctx.timeOfDay < 5f;
            if (isNight && ctx.weatherIntensity < 0.2f)
                return MusicMood.Mysterious;

            // Priority 8 — High speed (> ~Mach 0.8)
            if (ctx.speed >= Mach08SpeedMs)
                return MusicMood.Adventurous;

            // Priority 9 — Stable cruise
            float stableGForce   = Mathf.Abs(ctx.gForce - 1f);
            bool  isCruiseAlt    = ctx.altitude >= 500f;
            bool  isCruiseSpeed  = ctx.speed    >= 50f;
            if (isCruiseAlt && isCruiseSpeed && stableGForce < 0.5f && ctx.weatherIntensity < 0.3f)
                return MusicMood.Cruising;

            // Priority 10 — Low altitude, calm, slow
            return MusicMood.Peaceful;
        }

        // ── Intensity Resolution ───────────────────────────────────────────────

        /// <summary>
        /// Returns an intensity value (0–1) representing how extreme the current
        /// <paramref name="ctx"/> is within the context of <paramref name="mood"/>.
        /// </summary>
        public static float ResolveIntensity(FlightMusicContext ctx, MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Danger:
                {
                    float combatScore   = ctx.isInCombatZone ? 0.4f : 0f;
                    float dangerScore   = ctx.dangerLevel * 0.3f;
                    float damageScore   = ctx.damageLevel * 0.3f;
                    return Mathf.Clamp01(combatScore + dangerScore + damageScore);
                }

                case MusicMood.Tense:
                {
                    float gScore        = Mathf.InverseLerp(1f, 6f, ctx.gForce);
                    float stallScore    = ctx.stallWarning ? 0.4f : 0f;
                    float weatherScore  = Mathf.InverseLerp(0.5f, 1f, ctx.weatherIntensity);
                    return Mathf.Clamp01(Mathf.Max(gScore, stallScore, weatherScore));
                }

                case MusicMood.Epic:
                {
                    float altScore = Mathf.InverseLerp(SpaceAltitudeThreshold, 400_000f, ctx.altitude);
                    float spaceScore = ctx.isInSpace ? 0.6f : 0f;
                    return Mathf.Clamp01(Mathf.Max(altScore, spaceScore));
                }

                case MusicMood.Adventurous:
                {
                    return Mathf.Clamp01(Mathf.InverseLerp(Mach08SpeedMs, 600f, ctx.speed));
                }

                case MusicMood.Triumphant:
                    return 1f;

                case MusicMood.Serene:
                {
                    float sunScore = 1f - Mathf.InverseLerp(GoldenHourSunMin, GoldenHourSunMax, ctx.sunAltitudeDeg);
                    return Mathf.Clamp01(0.4f + sunScore * 0.6f);
                }

                case MusicMood.Mysterious:
                {
                    float nightDepth = ctx.timeOfDay >= 20f
                        ? Mathf.InverseLerp(20f, 24f, ctx.timeOfDay)
                        : Mathf.InverseLerp(5f, 0f, ctx.timeOfDay);
                    return Mathf.Clamp01(0.3f + nightDepth * 0.7f);
                }

                case MusicMood.Cruising:
                {
                    float speedScore = Mathf.InverseLerp(50f, Mach08SpeedMs, ctx.speed);
                    float altScore   = Mathf.InverseLerp(500f, 12_000f, ctx.altitude);
                    return Mathf.Clamp01((speedScore + altScore) * 0.5f);
                }

                case MusicMood.Peaceful:
                default:
                {
                    float calmScore = 1f - ctx.weatherIntensity;
                    float slowScore = Mathf.InverseLerp(0f, 50f, ctx.speed);
                    return Mathf.Clamp01((calmScore + (1f - slowScore)) * 0.5f);
                }
            }
        }
    }
}
