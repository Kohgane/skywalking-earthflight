// AltitudeEffectsManager.cs — SWEF Satellite View & Orbital Camera System
using UnityEngine;

namespace SWEF.OrbitalCamera
{
    /// <summary>
    /// Drives progressive atmosphere effects — sky colour, star visibility,
    /// sun glare, and sound dampening — based on the current camera altitude.
    /// </summary>
    public class AltitudeEffectsManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Sky Colour Gradient")]
        [Tooltip("Sky colour at ground level (0 km).")]
        [SerializeField] private Color skyColorGround = new Color(0.40f, 0.65f, 1.00f);

        [Tooltip("Sky colour at the top of the atmosphere (~100 km).")]
        [SerializeField] private Color skyColorHighAtmosphere = new Color(0.05f, 0.05f, 0.25f);

        [Tooltip("Sky colour in orbit / space (effectively black).")]
        [SerializeField] private Color skyColorSpace = Color.black;

        [Header("Atmosphere Thickness")]
        [Tooltip("Altitude (km) where the atmosphere is considered to end (Kármán line ≈ 100 km).")]
        [SerializeField] private float atmosphereTopKm = 100f;

        [Header("Stars")]
        [Tooltip("Altitude (km) above which stars start to become visible.")]
        [SerializeField] private float starFadeInStartKm = 50f;

        [Tooltip("Altitude (km) at which stars are fully visible.")]
        [SerializeField] private float starFadeInEndKm = 120f;

        [Tooltip("Renderer or particle system that represents the star field. Opacity is driven via a Material colour alpha.")]
        [SerializeField] private Renderer starFieldRenderer;

        [Header("Sun Glare")]
        [Tooltip("Light component representing the Sun (glare intensity driven via its intensity).")]
        [SerializeField] private Light sunLight;

        [Tooltip("Sun light intensity at ground level.")]
        [SerializeField] private float sunIntensityGround = 1.0f;

        [Tooltip("Sun light intensity in orbit (no atmosphere filtering).")]
        [SerializeField] private float sunIntensityOrbit = 1.6f;

        [Header("Audio Dampening")]
        [Tooltip("AudioSource whose volume is reduced with altitude (e.g. wind / ambient).")]
        [SerializeField] private AudioSource ambientAudioSource;

        [Tooltip("Altitude (km) above which audio begins to fade out.")]
        [SerializeField] private float audioFadeStartKm = 20f;

        [Tooltip("Altitude (km) at which audio reaches zero (space is silent).")]
        [SerializeField] private float audioFadeEndKm = 100f;

        #endregion

        #region Private

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private float _baseAudioVolume = 1f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (ambientAudioSource != null)
                _baseAudioVolume = ambientAudioSource.volume;
        }

        private void LateUpdate()
        {
            var ctrl = OrbitalCameraController.Instance;
            if (ctrl != null)
                UpdateEffectsForAltitude(ctrl.GetCurrentAltitudeKm());
        }

        #endregion

        #region Public API

        /// <summary>
        /// Applies all altitude-driven effects (sky colour, stars, sun glare, audio).
        /// Can be called manually or is invoked automatically every LateUpdate.
        /// </summary>
        /// <param name="altitudeKm">Current altitude in kilometres.</param>
        public void UpdateEffectsForAltitude(float altitudeKm)
        {
            RenderSettings.skybox?.SetColor(ColorId, GetSkyColorAtAltitude(altitudeKm));

            UpdateStarVisibility(altitudeKm);
            UpdateSunGlare(altitudeKm);
            UpdateAudioDampening(altitudeKm);
        }

        /// <summary>
        /// Returns the interpolated sky colour for a given altitude.
        /// Blends from ground blue → dark blue → black.
        /// </summary>
        /// <param name="altitudeKm">Altitude in kilometres.</param>
        public Color GetSkyColorAtAltitude(float altitudeKm)
        {
            if (altitudeKm <= 0f) return skyColorGround;

            var halfTop = atmosphereTopKm * 0.5f;
            if (altitudeKm < halfTop)
            {
                var t = altitudeKm / halfTop;
                return Color.Lerp(skyColorGround, skyColorHighAtmosphere, t);
            }
            else
            {
                var t = (altitudeKm - halfTop) / halfTop;
                return Color.Lerp(skyColorHighAtmosphere, skyColorSpace, Mathf.Clamp01(t));
            }
        }

        /// <summary>
        /// Returns the normalised atmosphere density at the given altitude
        /// using an exponential scale-height model (H ≈ 8.5 km).
        /// </summary>
        /// <param name="altitudeKm">Altitude in kilometres.</param>
        public float GetAtmosphereDensityAtAltitude(float altitudeKm)
        {
            const float scaleHeightKm = 8.5f;
            return Mathf.Exp(-altitudeKm / scaleHeightKm);
        }

        #endregion

        #region Private Helpers

        private void UpdateStarVisibility(float altKm)
        {
            if (starFieldRenderer == null) return;
            var alpha = Mathf.InverseLerp(starFadeInStartKm, starFadeInEndKm, altKm);
            var col   = starFieldRenderer.material.color;
            col.a     = alpha;
            starFieldRenderer.material.color = col;
        }

        private void UpdateSunGlare(float altKm)
        {
            if (sunLight == null) return;
            var density = GetAtmosphereDensityAtAltitude(altKm);
            // More atmosphere → less direct glare on camera (but brighter ambient diffuse);
            // we simply lerp intensity with inverse density for a simple approximation.
            sunLight.intensity = Mathf.Lerp(sunIntensityOrbit, sunIntensityGround, density);
        }

        private void UpdateAudioDampening(float altKm)
        {
            if (ambientAudioSource == null) return;
            var t = Mathf.InverseLerp(audioFadeStartKm, audioFadeEndKm, altKm);
            ambientAudioSource.volume = Mathf.Lerp(_baseAudioVolume, 0f, t);
        }

        #endregion
    }
}
