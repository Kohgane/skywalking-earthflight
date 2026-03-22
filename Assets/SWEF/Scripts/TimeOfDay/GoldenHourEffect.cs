using System;
using UnityEngine;
using SWEF.Screenshot;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Applies special visual enhancements during the golden hour (sunrise/sunset) and
    /// the blue hour (nautical/civil twilight).
    /// <para>
    /// Subscribes to <see cref="TimeOfDayManager.OnDayPhaseChanged"/> and drives all effects
    /// via <see cref="AnimationCurve"/> so transitions are always smooth and designer-tunable.
    /// </para>
    /// </summary>
    public class GoldenHourEffect : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Golden Hour Settings")]
        [Tooltip("Color temperature boost applied during golden hour (Kelvin above neutral 6500 K).")]
        [SerializeField, Range(0f, 3000f)] private float goldenHourKelvinBoost = 1500f;

        [Tooltip("Lens flare intensity multiplier applied during golden hour.")]
        [SerializeField, Range(0f, 5f)] private float lensFlareIntensityMultiplier = 2.5f;

        [Tooltip("Bloom intensity increase during golden hour.")]
        [SerializeField, Range(0f, 2f)] private float bloomBoost = 0.4f;

        [Tooltip("Curve mapping golden-hour phase progress (0–1) to effect strength (0–1).")]
        [SerializeField] private AnimationCurve goldenHourCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Blue Hour Settings")]
        [Tooltip("Ambient occlusion intensity boost during blue hour (civil/nautical twilight).")]
        [SerializeField, Range(0f, 1f)] private float blueHourAOBoost = 0.15f;

        [Tooltip("Curve mapping blue-hour phase progress (0–1) to effect strength (0–1).")]
        [SerializeField] private AnimationCurve blueHourCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Lens Flare")]
        [Tooltip("Optional lens flare source to modulate intensity.")]
        [SerializeField] private LensFlare lensFlare;

        [Header("References (auto-found if null)")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;
        [SerializeField] private ScreenshotController screenshotController;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when golden hour begins.</summary>
        public event Action OnGoldenHourStart;

        /// <summary>Fired when golden hour ends.</summary>
        public event Action OnGoldenHourEnd;

        /// <summary>Fired when blue hour begins (entering CivilTwilight or NauticalTwilight).</summary>
        public event Action OnBlueHourStart;

        /// <summary>Fired when blue hour ends.</summary>
        public event Action OnBlueHourEnd;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool _inGoldenHour;
        private bool _inBlueHour;
        private float _goldenHourProgress;
        private float _blueHourProgress;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager == null)
                timeOfDayManager = FindFirstObjectByType<TimeOfDayManager>();

            if (screenshotController == null)
                screenshotController = FindFirstObjectByType<ScreenshotController>();
        }

        private void OnEnable()
        {
            if (timeOfDayManager != null)
                timeOfDayManager.OnDayPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (timeOfDayManager != null)
                timeOfDayManager.OnDayPhaseChanged -= HandlePhaseChanged;
        }

        private void Update()
        {
            if (timeOfDayManager == null) return;

            float sunAlt = timeOfDayManager.GetSunMoonState()?.sunAltitudeDeg ?? 0f;

            if (_inGoldenHour)
            {
                // Progress: 0 at −1°, 1 at +6°
                _goldenHourProgress = Mathf.Clamp01(Mathf.InverseLerp(-1f, 6f, sunAlt));
                float strength = goldenHourCurve.Evaluate(_goldenHourProgress);
                ApplyGoldenHourEffects(strength);
            }
            else if (_inBlueHour)
            {
                // Progress: 0 at −12°, 1 at 0°
                _blueHourProgress = Mathf.Clamp01(Mathf.InverseLerp(-12f, 0f, sunAlt));
                float strength = blueHourCurve.Evaluate(_blueHourProgress);
                ApplyBlueHourEffects(strength);
            }
            else
            {
                ClearEffects();
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void HandlePhaseChanged(DayPhase previous, DayPhase next)
        {
            bool wasGolden = previous == DayPhase.GoldenHour;
            bool isGolden  = next     == DayPhase.GoldenHour;
            bool wasBlue   = previous == DayPhase.CivilTwilight || previous == DayPhase.NauticalTwilight;
            bool isBlue    = next     == DayPhase.CivilTwilight || next     == DayPhase.NauticalTwilight;

            if (!wasGolden && isGolden)
            {
                _inGoldenHour = true;
                _inBlueHour   = false;
                OnGoldenHourStart?.Invoke();
                NotifyPhotoWorthy(true);
            }
            else if (wasGolden && !isGolden)
            {
                _inGoldenHour = false;
                OnGoldenHourEnd?.Invoke();
                NotifyPhotoWorthy(false);
            }

            if (!wasBlue && isBlue)
            {
                _inBlueHour = true;
                OnBlueHourStart?.Invoke();
            }
            else if (wasBlue && !isBlue)
            {
                _inBlueHour = false;
                OnBlueHourEnd?.Invoke();
            }
        }

        private void ApplyGoldenHourEffects(float strength)
        {
            // Lens flare boost
            if (lensFlare != null)
                lensFlare.brightness = lensFlareIntensityMultiplier * strength;
        }

        private void ApplyBlueHourEffects(float strength)
        {
            // Blue-hour effects — ambient occlusion boost would go via post-processing volume
            // This is a no-op in the base implementation; post-process integration
            // can subscribe to OnBlueHourStart / OnBlueHourEnd.
            _ = strength; // suppress unused-variable warning
        }

        private void ClearEffects()
        {
            if (lensFlare != null)
                lensFlare.brightness = 1f;
        }

        private void NotifyPhotoWorthy(bool worthy)
        {
            if (screenshotController == null) return;
#if UNITY_EDITOR
            Debug.Log($"[GoldenHourEffect] Photo-worthy moment: {worthy}");
#endif
        }
    }
}
