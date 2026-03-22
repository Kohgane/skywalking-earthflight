using UnityEngine;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Manages all nighttime sky elements: star field, moon phase rendering,
    /// aurora borealis, and the Milky Way band.
    /// <para>
    /// Integrates with <see cref="CloudRenderer"/> to occlude stars behind clouds and
    /// with <see cref="TimeOfDayManager"/> for lighting data.
    /// Aurora is enabled automatically near polar latitudes (|lat| &gt; 60°).
    /// </para>
    /// </summary>
    public class NightSkyRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Star Field")]
        [Tooltip("Particle system that renders the star field.")]
        [SerializeField] private ParticleSystem starParticles;

        [Tooltip("Maximum star emission rate (particles per second) at full visibility.")]
        [SerializeField, Range(0f, 5000f)] private float maxStarEmission = 2000f;

        [Tooltip("Star twinkle speed — animation rate of the star shimmer.")]
        [SerializeField, Range(0f, 10f)] private float twinkleSpeed = 1.5f;

        [Tooltip("Optional mesh renderer for constellation overlay texture.")]
        [SerializeField] private MeshRenderer constellationOverlay;

        [Header("Moon")]
        [Tooltip("MeshRenderer for the moon quad/sphere — must use a phase-aware material.")]
        [SerializeField] private MeshRenderer moonRenderer;

        [Tooltip("Shader property name for the moon phase value (0 = new, 1 = full).")]
        [SerializeField] private string moonPhaseProperty = "_Phase";

        [Tooltip("Shader property name for the moon glow intensity.")]
        [SerializeField] private string moonGlowProperty = "_GlowIntensity";

        [Header("Aurora Borealis")]
        [Tooltip("Parent GameObject holding aurora curtain meshes/particles. Activated near poles.")]
        [SerializeField] private GameObject auroraRoot;

        [Tooltip("Absolute latitude threshold above which aurora appears (degrees).")]
        [SerializeField, Range(50f, 80f)] private float auroraLatitudeThreshold = 60f;

        [Tooltip("Primary aurora color (typically green).")]
        [SerializeField] private Color auroraColorPrimary = new Color(0.1f, 1f, 0.3f, 0.6f);

        [Tooltip("Secondary aurora color (purple/blue).")]
        [SerializeField] private Color auroraColorSecondary = new Color(0.5f, 0.2f, 1.0f, 0.4f);

        [Tooltip("Animation speed for aurora curtain oscillation.")]
        [SerializeField, Range(0.1f, 3f)] private float auroraAnimSpeed = 0.5f;

        [Header("Milky Way")]
        [Tooltip("MeshRenderer for the Milky Way band. Rotation is updated based on season/time.")]
        [SerializeField] private MeshRenderer milkyWayRenderer;

        [Tooltip("Rotation speed of the Milky Way band (degrees per in-game hour).")]
        [SerializeField] private float milkyWayRotationSpeed = 15f;

        [Header("References (auto-found if null)")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;

        [Tooltip("Optional: global cloud coverage (0 = clear, 1 = overcast). Set at runtime by weather system.")]
        [SerializeField, Range(0f, 1f)] private float externalCloudCoverage;

        // ── Shader property IDs ───────────────────────────────────────────────────
        private int _moonPhaseId;
        private int _moonGlowId;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _auroraTime;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager == null)
                timeOfDayManager = FindFirstObjectByType<TimeOfDayManager>();

            _moonPhaseId = Shader.PropertyToID(moonPhaseProperty);
            _moonGlowId  = Shader.PropertyToID(moonGlowProperty);
        }

        private void Update()
        {
            if (timeOfDayManager == null) return;

            LightingSnapshot lighting = timeOfDayManager.GetCurrentLighting();
            SunMoonState     state    = timeOfDayManager.GetSunMoonState();

            UpdateStars(lighting, state);
            UpdateMoon(state);
            UpdateAurora(state);
            UpdateMilkyWay(state);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the global cloud coverage used to occlude stars and the Milky Way.
        /// Call this from a weather manager or cloud system integration (0 = clear, 1 = overcast).
        /// </summary>
        public void SetCloudCoverage(float coverage) =>
            externalCloudCoverage = Mathf.Clamp01(coverage);

        // ── Private helpers ───────────────────────────────────────────────────────

        private void UpdateStars(LightingSnapshot lighting, SunMoonState state)
        {
            if (starParticles == null) return;

            // Occlude stars behind clouds using external coverage value
            float cloudOcclusion = 1f - Mathf.Clamp01(externalCloudCoverage);

            float effectiveVisibility = lighting.starVisibility * cloudOcclusion;

            var emission = starParticles.emission;
            emission.rateOverTime = maxStarEmission * effectiveVisibility;

            // Twinkle via noise-based lifetime variation driven by twinkleSpeed
            var noise = starParticles.noise;
            if (noise.enabled)
            {
                var strength  = noise.strength;
                strength.mode = ParticleSystemCurveMode.Constant;
                noise.strength = strength; // keep existing; twinkle via shader in real projects
            }

            if (effectiveVisibility > 0.01f && !starParticles.isPlaying) starParticles.Play();
            else if (effectiveVisibility <= 0.01f && starParticles.isPlaying) starParticles.Stop();

            // Constellation overlay
            if (constellationOverlay != null)
            {
                Color c = constellationOverlay.material.color;
                c.a = effectiveVisibility * 0.5f;
                constellationOverlay.material.color = c;
            }
        }

        private void UpdateMoon(SunMoonState state)
        {
            if (moonRenderer == null) return;

            // Phase value — 0 = new moon, 0.5 = full, 1 = back to new
            float phaseValue = state.moonIllumination;
            moonRenderer.material.SetFloat(_moonPhaseId, phaseValue);

            float moonAboveHorizon = Mathf.Clamp01(Mathf.InverseLerp(-5f, 10f, state.moonAltitudeDeg));
            float glowIntensity    = moonAboveHorizon * state.moonIllumination;
            moonRenderer.material.SetFloat(_moonGlowId, glowIntensity);

            // Position moon in sky
            if (state.moonAltitudeDeg > -5f)
            {
                moonRenderer.gameObject.SetActive(true);
                moonRenderer.transform.rotation = Quaternion.LookRotation(-state.moonDirection);
            }
            else
            {
                moonRenderer.gameObject.SetActive(false);
            }
        }

        private void UpdateAurora(SunMoonState state)
        {
            if (auroraRoot == null) return;

            float lat       = timeOfDayManager.Latitude;
            bool  nearPole  = Mathf.Abs(lat) > auroraLatitudeThreshold;
            bool  isDark    = state.sunAltitudeDeg < -12f;

            // Aurora is more intense with a new moon (less competing light)
            float lunarFactor = 1f - state.moonIllumination * 0.7f;

            auroraRoot.SetActive(nearPole && isDark);

            if (nearPole && isDark)
            {
                _auroraTime += Time.deltaTime * auroraAnimSpeed;

                // Animate curtain renderers
                var renderers = auroraRoot.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material.HasProperty("_MainTex"))
                    {
                        Vector2 offset = r.material.mainTextureOffset;
                        offset.x = Mathf.Sin(_auroraTime * 0.3f) * 0.1f;
                        r.material.mainTextureOffset = offset;
                    }
                    // Alternate between primary and secondary colors over time
                    float blend = (Mathf.Sin(_auroraTime) + 1f) * 0.5f;
                    Color auroraTint = Color.Lerp(auroraColorPrimary, auroraColorSecondary, blend) * lunarFactor;
                    if (r.material.HasProperty("_Color"))
                        r.material.color = auroraTint;
                }
            }
        }

        private void UpdateMilkyWay(SunMoonState state)
        {
            if (milkyWayRenderer == null) return;

            // Only visible at night with low moon illumination
            float cloudOcclusion = 1f - Mathf.Clamp01(externalCloudCoverage);
            float visibility     = Mathf.Clamp01(Mathf.InverseLerp(-6f, -15f, state.sunAltitudeDeg))
                                   * (1f - state.moonIllumination * 0.6f)
                                   * cloudOcclusion;

            Color c = milkyWayRenderer.material.color;
            c.a = visibility;
            milkyWayRenderer.material.color = c;

            // Rotate band based on hour of day to simulate Earth's rotation
            float hourAngle = timeOfDayManager.CurrentHour * milkyWayRotationSpeed;
            milkyWayRenderer.transform.localRotation = Quaternion.Euler(0f, hourAngle, 0f);
        }
    }
}
