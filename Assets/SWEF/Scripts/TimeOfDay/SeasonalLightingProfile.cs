using UnityEngine;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// ScriptableObject that encodes season-specific lighting curves and color gradients.
    /// One asset per season is created (Spring, Summer, Autumn, Winter).
    /// <para>
    /// The <see cref="Evaluate"/> method converts the current sun altitude and hour of day
    /// into a fully populated <see cref="LightingSnapshot"/> driven entirely by
    /// designer-authored curves and gradients.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "SWEF/TimeOfDay/SeasonalLightingProfile", fileName = "SeasonalLightingProfile")]
    public class SeasonalLightingProfile : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Identity")]
        [Tooltip("Season this profile applies to.")]
        public Season season;

        [Header("Sun")]
        [Tooltip("Sun color sampled by normalised sun altitude (0 = −18°, 1 = +90°).")]
        public Gradient sunColorGradient;

        [Tooltip("Sun intensity curve. X = normalised sun altitude (0–1). Y = intensity multiplier.")]
        public AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1.2f);

        [Header("Ambient")]
        [Tooltip("Ambient sky color gradient keyed to normalised sun altitude.")]
        public Gradient ambientColorGradient;

        [Header("Fog")]
        [Tooltip("Fog color gradient keyed to normalised sun altitude.")]
        public Gradient fogColorGradient;

        [Header("Shadows")]
        [Tooltip("Shadow strength curve. X = normalised sun altitude. Y = shadow strength (0–1).")]
        public AnimationCurve shadowStrengthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Skybox")]
        [Tooltip("Skybox exposure curve. X = normalised sun altitude. Y = exposure.")]
        public AnimationCurve skyboxExposureCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 1f);

        [Header("Stars")]
        [Tooltip("Star visibility curve. X = normalised sun altitude (0 = night, 1 = day). Y = alpha.")]
        public AnimationCurve starVisibilityCurve;

        [Header("Clouds")]
        [Tooltip("Cloud color tint gradient keyed to hour of day (0 = midnight, 0.5 = noon, 1 = midnight).")]
        public Gradient cloudColorTint;

        [Header("Color Temperature")]
        [Tooltip("Color temperature curve (Kelvin). X = hour of day (0–24 mapped to 0–1). Y = Kelvin.")]
        public AnimationCurve temperatureKelvin = AnimationCurve.Constant(0f, 1f, 6500f);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates all curves and gradients for the given solar conditions and returns
        /// a populated <see cref="LightingSnapshot"/>.
        /// </summary>
        /// <param name="sunAltitude">Sun altitude in degrees (−18 to +90).</param>
        /// <param name="hourOfDay">Current hour of day (0–24).</param>
        public LightingSnapshot Evaluate(float sunAltitude, float hourOfDay)
        {
            float t    = Mathf.InverseLerp(-18f, 90f, sunAltitude);
            float hourT = Mathf.InverseLerp(0f, 24f, hourOfDay);

            var snap = new LightingSnapshot();

            snap.sunColor        = sunColorGradient    != null ? sunColorGradient.Evaluate(t)    : Color.white;
            snap.sunIntensity    = sunIntensityCurve   != null ? sunIntensityCurve.Evaluate(t)   : Mathf.Clamp01(t);
            snap.ambientSkyColor = ambientColorGradient!= null ? ambientColorGradient.Evaluate(t): Color.grey;
            snap.fogColor        = fogColorGradient    != null ? fogColorGradient.Evaluate(t)    : Color.grey;
            snap.shadowStrength  = shadowStrengthCurve != null ? shadowStrengthCurve.Evaluate(t) : Mathf.Clamp01(t);
            snap.skyboxExposure  = skyboxExposureCurve != null ? skyboxExposureCurve.Evaluate(t) : t;
            snap.starVisibility  = starVisibilityCurve != null ? starVisibilityCurve.Evaluate(t) : Mathf.Clamp01(1f - t * 2f);

            // Apply color temperature tint to sun color
            if (temperatureKelvin != null)
            {
                float kelvin = temperatureKelvin.Evaluate(hourT);
                snap.sunColor *= KelvinToColor(kelvin);
            }

            return snap;
        }

        // ── Default profile factory ───────────────────────────────────────────────

        /// <summary>
        /// Creates a sensible default profile for <paramref name="s"/>.
        /// Used when no ScriptableObject asset has been assigned.
        /// </summary>
        public static SeasonalLightingProfile CreateDefault(Season s)
        {
            var profile = CreateInstance<SeasonalLightingProfile>();
            profile.season = s;

            // Default gradients — keyed from night (0) to full day (1)
            profile.sunColorGradient = BuildDefaultSunGradient(s);
            profile.ambientColorGradient = BuildDefaultAmbientGradient(s);
            profile.fogColorGradient = BuildDefaultFogGradient(s);
            profile.starVisibilityCurve = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.3f, 0f), new Keyframe(1f, 0f));

            return profile;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>Converts a color temperature in Kelvin to an approximate RGB tint color.</summary>
        private static Color KelvinToColor(float kelvin)
        {
            kelvin = Mathf.Clamp(kelvin, 1000f, 40000f) / 100f;
            float r, g, b;

            // Red channel
            r = kelvin <= 66f ? 1f : Mathf.Clamp01(329.698727446f * Mathf.Pow(kelvin - 60f, -0.1332047592f) / 255f);

            // Green channel
            if (kelvin <= 66f)
                g = Mathf.Clamp01((99.4708025861f * Mathf.Log(kelvin) - 161.1195681661f) / 255f);
            else
                g = Mathf.Clamp01(288.1221695283f * Mathf.Pow(kelvin - 60f, -0.0755148492f) / 255f);

            // Blue channel
            if (kelvin >= 66f)
                b = 1f;
            else if (kelvin <= 19f)
                b = 0f;
            else
                b = Mathf.Clamp01((138.5177312231f * Mathf.Log(kelvin - 10f) - 305.0447927307f) / 255f);

            return new Color(r, g, b);
        }

        private static Gradient BuildDefaultSunGradient(Season s)
        {
            var g = new Gradient();
            // Warm for summer, cool for winter
            Color midday = s == Season.Summer ? new Color(1f, 0.97f, 0.90f)
                         : s == Season.Winter ? new Color(0.85f, 0.90f, 1.0f)
                         : new Color(1f, 0.95f, 0.88f);
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.5f, 0.1f), 0.28f),
                    new GradientColorKey(midday,                        0.6f),
                    new GradientColorKey(midday,                        1.0f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private static Gradient BuildDefaultAmbientGradient(Season s)
        {
            var g = new Gradient();
            Color dayAmbient = s == Season.Summer ? new Color(0.5f, 0.65f, 0.95f)
                             : s == Season.Winter ? new Color(0.45f, 0.55f, 0.75f)
                             : new Color(0.5f, 0.65f, 0.90f);
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.10f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.25f, 0.4f),   0.25f),
                    new GradientColorKey(dayAmbient,                      0.6f),
                    new GradientColorKey(dayAmbient,                      1.0f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
                });
            return g;
        }

        private static Gradient BuildDefaultFogGradient(Season s)
        {
            var g = new Gradient();
            Color dayFog = s == Season.Autumn ? new Color(0.65f, 0.70f, 0.75f)
                         : s == Season.Winter ? new Color(0.70f, 0.72f, 0.78f)
                         : new Color(0.68f, 0.78f, 0.92f);
            g.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.40f, 0.35f), 0.28f),
                    new GradientColorKey(dayFog,                          0.55f),
                    new GradientColorKey(dayFog,                          1.0f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f)
                });
            return g;
        }
    }
}
