using UnityEngine;
using SWEF.Flight;
using SWEF.Util;

namespace SWEF.Atmosphere
{
    [System.Serializable]
    public struct AtmosphereLayer
    {
        public float altitudeMeters;
        public float fogDensity;
        public Color skyColor;
        [Range(0f, 1f)] public float skyboxBlend;
        [Range(0f, 2f)] public float sunIntensity;
    }

    /// <summary>
    /// Blends fog, sky color, skybox material, and sun intensity based on current altitude.
    /// Reads altitude from AltitudeController. Layers are designer-tweakable in Inspector.
    /// </summary>
    public class AtmosphereController : MonoBehaviour
    {
        [SerializeField] private AltitudeController altitudeSource;
        [SerializeField] private Light sunLight;
        [SerializeField] private Material skyboxMaterial;

        [Header("Transition")]
        [SerializeField] private float transitionSmoothing = 3f;

        [Header("Layers (sorted by altitude, low to high)")]
        [SerializeField] private AtmosphereLayer[] layers = new AtmosphereLayer[]
        {
            new AtmosphereLayer { altitudeMeters = 0,       fogDensity = 0.0008f, skyColor = new Color(0.53f, 0.81f, 0.92f), skyboxBlend = 0f,   sunIntensity = 1.0f },
            new AtmosphereLayer { altitudeMeters = 2000,    fogDensity = 0.0008f, skyColor = new Color(0.53f, 0.81f, 0.92f), skyboxBlend = 0f,   sunIntensity = 1.0f },
            new AtmosphereLayer { altitudeMeters = 20000,   fogDensity = 0.0001f, skyColor = new Color(0.20f, 0.40f, 0.80f), skyboxBlend = 0.3f, sunIntensity = 1.0f },
            new AtmosphereLayer { altitudeMeters = 80000,   fogDensity = 0.00001f,skyColor = new Color(0.05f, 0.10f, 0.30f), skyboxBlend = 0.7f, sunIntensity = 0.9f },
            new AtmosphereLayer { altitudeMeters = 120000,  fogDensity = 0f,      skyColor = new Color(0.01f, 0.01f, 0.05f), skyboxBlend = 1.0f, sunIntensity = 0.7f },
            new AtmosphereLayer { altitudeMeters = 200000,  fogDensity = 0f,      skyColor = Color.black,                     skyboxBlend = 1.0f, sunIntensity = 0.6f },
        };

        private float _currentFog;
        private Color _currentSkyColor;
        private float _currentBlend;
        private float _currentSun;

        private void Start()
        {
            if (altitudeSource == null)
                altitudeSource = FindFirstObjectByType<AltitudeController>();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            if (layers.Length > 0)
            {
                _currentFog = layers[0].fogDensity;
                _currentSkyColor = layers[0].skyColor;
                _currentBlend = layers[0].skyboxBlend;
                _currentSun = layers[0].sunIntensity;
            }
        }

        private void Update()
        {
            if (altitudeSource == null || layers.Length == 0) return;

            float alt = altitudeSource.CurrentAltitudeMeters;
            float dt = Time.deltaTime;

            AtmosphereLayer lower = layers[0];
            AtmosphereLayer upper = layers[layers.Length - 1];
            float t = 1f;

            for (int i = 0; i < layers.Length - 1; i++)
            {
                if (alt >= layers[i].altitudeMeters && alt < layers[i + 1].altitudeMeters)
                {
                    lower = layers[i];
                    upper = layers[i + 1];
                    float range = upper.altitudeMeters - lower.altitudeMeters;
                    t = range > 0 ? (alt - lower.altitudeMeters) / range : 0f;
                    break;
                }
            }

            if (alt >= layers[layers.Length - 1].altitudeMeters)
            {
                lower = layers[layers.Length - 1];
                upper = layers[layers.Length - 1];
                t = 1f;
            }

            float targetFog = Mathf.Lerp(lower.fogDensity, upper.fogDensity, t);
            Color targetSky = Color.Lerp(lower.skyColor, upper.skyColor, t);
            float targetBlend = Mathf.Lerp(lower.skyboxBlend, upper.skyboxBlend, t);
            float targetSun = Mathf.Lerp(lower.sunIntensity, upper.sunIntensity, t);

            _currentFog = ExpSmoothing.ExpLerp(_currentFog, targetFog, transitionSmoothing, dt);
            _currentSkyColor = Color.Lerp(_currentSkyColor, targetSky, 1f - Mathf.Exp(-transitionSmoothing * dt));
            _currentBlend = ExpSmoothing.ExpLerp(_currentBlend, targetBlend, transitionSmoothing, dt);
            _currentSun = ExpSmoothing.ExpLerp(_currentSun, targetSun, transitionSmoothing, dt);

            RenderSettings.fogDensity = _currentFog;
            RenderSettings.ambientSkyColor = _currentSkyColor;

            if (skyboxMaterial != null)
                skyboxMaterial.SetFloat("_Blend", _currentBlend);

            if (sunLight != null)
                sunLight.intensity = _currentSun;
        }
    }
}
