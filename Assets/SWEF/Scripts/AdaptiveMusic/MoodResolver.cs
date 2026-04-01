// MoodResolver.cs — SWEF Dynamic Soundtrack & Adaptive Music System (Phase 83)
using UnityEngine;

namespace SWEF.AdaptiveMusic
{
    /// <summary>
    /// Static utility that converts a <see cref="FlightMusicContext"/> into a
    /// <see cref="MusicMood"/> and a normalised intensity value (0–1).
    ///
    /// <para>Rules are evaluated in strict priority order (highest first).
    /// The first matching rule wins.</para>
    /// </summary>
    public static class MoodResolver
    {
        // ── Constants ────────────────────────────────────────────────────────────

        /// <summary>Mach 0.8 in m/s (approximate, at sea level / standard conditions).</summary>
        private const float Mach08Ms = 272f;   // ≈ 0.8 × 340 m/s

        /// <summary>Altitude above which the aircraft is considered in space (metres).</summary>
        private const float SpaceAltitude = 100_000f;

        /// <summary>Weather intensity threshold above which it is considered a storm.</summary>
        private const float StormThreshold = 0.7f;

        /// <summary>Damage proportion above which Danger mood is triggered.</summary>
        private const float DangerDamageThreshold = 0.6f;

        /// <summary>G-force threshold above which Tense mood is triggered.</summary>
        private const float TenseGForce = 3f;

        /// <summary>Sun altitude range (degrees) considered "golden hour".</summary>
        private const float GoldenHourLow  = 0f;
        private const float GoldenHourHigh = 6f;

        /// <summary>Hour range considered "night" for Mysterious mood.</summary>
        private const float NightHourStart = 21f;
        private const float NightHourEnd   = 5f;  // wraps around midnight

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the most appropriate <see cref="MusicMood"/> for the given flight context.
        /// Priorities are evaluated top-down; the first matching rule wins.
        /// </summary>
        public static MusicMood ResolveMood(FlightMusicContext ctx)
        {
            // Priority 1: Active emergency OR critical damage
            if (ctx.hasActiveEmergency || ctx.damageLevel > DangerDamageThreshold)
                return MusicMood.Danger;

            // Priority 2: High-G or stall warning
            if (ctx.gForce > TenseGForce || ctx.stallWarning)
                return MusicMood.Tense;

            // Priority 3: In storm
            if (ctx.inStorm || ctx.weatherIntensity > StormThreshold)
                return MusicMood.Tense;

            // Priority 4: Space (above Kármán line)
            if (ctx.isInSpace || ctx.altitude > SpaceAltitude)
                return MusicMood.Epic;

            // Priority 5: Mission just completed
            if (ctx.missionJustCompleted)
                return MusicMood.Triumphant;

            // Priority 6: Golden hour (sun 0–6° above horizon)
            if (ctx.sunAltitudeDeg >= GoldenHourLow && ctx.sunAltitudeDeg <= GoldenHourHigh)
                return MusicMood.Serene;

            // Priority 7: Night + clear weather
            if (IsNight(ctx.timeOfDay) && ctx.weatherIntensity < 0.3f)
                return MusicMood.Mysterious;

            // Priority 8: High speed (> Mach 0.8)
            if (ctx.speed > Mach08Ms)
                return MusicMood.Adventurous;

            // Priority 9: Smooth cruise
            if (ctx.isFlying && ctx.speed > 50f && ctx.gForce is > 0.8f and < 1.5f)
                return MusicMood.Cruising;

            // Priority 10: Low altitude + calm + low speed
            if (ctx.altitude < 300f && ctx.weatherIntensity < 0.2f && ctx.speed < 50f)
                return MusicMood.Peaceful;

            // Default fallback
            return MusicMood.Peaceful;
        }

        /// <summary>
        /// Returns a normalised intensity value (0–1) that reflects how extreme the
        /// current context is within the resolved mood.
        /// </summary>
        public static float ResolveIntensity(FlightMusicContext ctx, MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Danger:
                    // Intensity = max of danger signals
                    return Mathf.Clamp01(Mathf.Max(
                        ctx.damageLevel,
                        ctx.hasActiveEmergency ? 0.8f : 0f,
                        Remap(ctx.dangerLevel, 0f, 1f, 0f, 1f)));

                case MusicMood.Tense:
                    float gIntensity    = Remap(ctx.gForce, 1f, 6f, 0f, 1f);
                    float wxIntensity   = Remap(ctx.weatherIntensity, 0.5f, 1f, 0f, 1f);
                    return Mathf.Clamp01(Mathf.Max(gIntensity, wxIntensity,
                        ctx.stallWarning ? 0.7f : 0f));

                case MusicMood.Epic:
                    return Mathf.Clamp01(Remap(ctx.altitude, SpaceAltitude, SpaceAltitude * 3f, 0.3f, 1f));

                case MusicMood.Triumphant:
                    return 0.9f;

                case MusicMood.Serene:
                    // Softer when sun is closer to horizon
                    return Mathf.Clamp01(1f - Remap(ctx.sunAltitudeDeg, GoldenHourLow, GoldenHourHigh, 0f, 1f));

                case MusicMood.Mysterious:
                    return Mathf.Clamp01(Remap(ctx.altitude, 0f, 5000f, 0.3f, 0.8f));

                case MusicMood.Adventurous:
                    return Mathf.Clamp01(Remap(ctx.speed, Mach08Ms, Mach08Ms * 2f, 0.4f, 1f));

                case MusicMood.Cruising:
                    return Mathf.Clamp01(Remap(ctx.speed, 50f, 250f, 0.2f, 0.7f));

                case MusicMood.Peaceful:
                default:
                    // Intensity inversely related to speed and weather
                    float calm = 1f - Mathf.Clamp01(ctx.weatherIntensity)
                                    - Remap(ctx.speed, 0f, 50f, 0f, 0.3f);
                    return Mathf.Clamp01(calm * 0.5f + 0.1f);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool IsNight(float hour)
        {
            // Night is 21:00–05:00 (wraps around midnight)
            return hour >= NightHourStart || hour < NightHourEnd;
        }

        /// <summary>Linear remap from [inLow, inHigh] to [outLow, outHigh], unclamped.</summary>
        private static float Remap(float value, float inLow, float inHigh, float outLow, float outHigh)
        {
            if (Mathf.Approximately(inHigh, inLow))
                return outLow;
            return outLow + (value - inLow) / (inHigh - inLow) * (outHigh - outLow);
        }
    }
}
