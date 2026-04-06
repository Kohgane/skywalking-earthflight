// SpaceEnvironmentRenderer.cs — Phase 114: Satellite & Space Debris Tracking
// Space environment: Earth limb glow, star field, sun flare, atmospheric scattering from orbit.
// Namespace: SWEF.SatelliteTracking

using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Manages the visual space environment when the camera is in or near orbital altitude:
    /// Earth limb atmospheric glow, star field brightness, sun corona/flare, and
    /// thin atmospheric scattering on the Earth limb.
    /// </summary>
    public class SpaceEnvironmentRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Earth Limb Glow")]
        [Tooltip("Material applied to the Earth atmosphere shell mesh.")]
        [SerializeField] private Material atmosphereShellMaterial;

        [Tooltip("Altitude (km) at which the atmosphere glow fully fades in.")]
        [SerializeField] private float atmosphereFadeAltKm = 100f;

        [Tooltip("Atmosphere limb glow colour.")]
        [SerializeField] private Color limbGlowColor = new Color(0.4f, 0.7f, 1f, 0.6f);

        [Header("Star Field")]
        [Tooltip("ParticleSystem used for the procedural star field.")]
        [SerializeField] private ParticleSystem starFieldParticles;

        [Tooltip("Maximum star field brightness (alpha) when fully in space.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxStarBrightness = 1f;

        [Tooltip("Altitude (km) above which stars reach full brightness.")]
        [SerializeField] private float starFullBrightnessAltKm = 80f;

        [Header("Sun Flare")]
        [Tooltip("Directional light representing the Sun (for flare/shadow).")]
        [SerializeField] private Light sunLight;

        [Tooltip("LensFlare component on the Sun light.")]
        [SerializeField] private Behaviour sunLensFlare;

        [Header("Camera Altitude")]
        [Tooltip("Camera whose altitude drives the environment transitions.")]
        [SerializeField] private Camera trackingCamera;

        [Tooltip("Earth radius in world units for altitude calculation.")]
        [SerializeField] private float earthRadiusWU = 637.1f;

        [Tooltip("Kilometres per world unit.")]
        [SerializeField] private float kmPerWorldUnit = 10f;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            if (trackingCamera == null) trackingCamera = Camera.main;
            if (trackingCamera == null) return;

            float altKm = CalculateCameraAltitudeKm();
            UpdateAtmosphereShell(altKm);
            UpdateStarField(altKm);
            UpdateSunFlare(altKm);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private float CalculateCameraAltitudeKm()
        {
            float distFromCentre = trackingCamera.transform.position.magnitude;
            return (distFromCentre - earthRadiusWU) * kmPerWorldUnit;
        }

        private void UpdateAtmosphereShell(float altKm)
        {
            if (atmosphereShellMaterial == null) return;
            float t     = Mathf.Clamp01(altKm / atmosphereFadeAltKm);
            var color   = limbGlowColor;
            color.a     = Mathf.Lerp(0f, limbGlowColor.a, 1f - t * 0.5f);
            atmosphereShellMaterial.SetColor("_TintColor", color);
        }

        private void UpdateStarField(float altKm)
        {
            if (starFieldParticles == null) return;
            float brightness = Mathf.Clamp01(altKm / starFullBrightnessAltKm) * maxStarBrightness;
            var main = starFieldParticles.main;
            main.startColor = new Color(1f, 1f, 1f, brightness);
        }

        private void UpdateSunFlare(float altKm)
        {
            if (sunLensFlare == null) return;
            // Flare is more visible above atmosphere
            sunLensFlare.enabled = altKm > 80f;
        }
    }
}
