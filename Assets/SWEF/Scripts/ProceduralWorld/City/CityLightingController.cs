// CityLightingController.cs — Phase 113: Procedural City & Airport Generation
// Dynamic city lighting: windows light up at night, street lights, traffic lights, neon signs.
// Namespace: SWEF.ProceduralWorld

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.ProceduralWorld
{
    /// <summary>
    /// Controls dynamic lighting for a procedural city.
    /// Window lights activate at dusk, street lights follow a day/night cycle,
    /// and neon signs pulse in commercial zones.
    /// </summary>
    public class CityLightingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Day / Night Thresholds [0..1]")]
        [Tooltip("Sun intensity below which city lights turn on.")]
        [SerializeField] private float nightThreshold = 0.2f;

        [Tooltip("Sun intensity below which full night mode activates.")]
        [SerializeField] private float fullNightThreshold = 0.05f;

        [Header("Light Prefabs")]
        [SerializeField] private GameObject streetLightPrefab;
        [SerializeField] private GameObject trafficLightPrefab;

        [Header("Window Emission")]
        [SerializeField] private Color windowNightColor = new Color(1f, 0.95f, 0.7f);
        [SerializeField] private Color windowDayColor = Color.black;

        // ── Public state ──────────────────────────────────────────────────────────
        /// <summary>Whether city lights are currently active (night mode).</summary>
        public bool IsNightMode { get; private set; }

        // ── Private state ─────────────────────────────────────────────────────────
        private readonly List<Light> _streetLights = new List<Light>();
        private readonly List<Renderer> _windowRenderers = new List<Renderer>();
        private float _sunIntensity = 1f;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            UpdateSunIntensity();
            bool shouldBeNight = _sunIntensity < nightThreshold;
            if (shouldBeNight != IsNightMode)
            {
                IsNightMode = shouldBeNight;
                ApplyLightingMode();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Registers a building renderer for window emission control.</summary>
        public void RegisterWindowRenderer(Renderer r)
        {
            if (r != null && !_windowRenderers.Contains(r))
                _windowRenderers.Add(r);
        }

        /// <summary>Spawns a street light at the given world position.</summary>
        public Light SpawnStreetLight(Vector3 position, Transform parent)
        {
            if (streetLightPrefab == null) return null;
            var go = Instantiate(streetLightPrefab, position, Quaternion.identity, parent);
            var light = go.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.enabled = IsNightMode;
                _streetLights.Add(light);
            }
            return light;
        }

        /// <summary>Forces a lighting mode update immediately.</summary>
        public void ForceRefresh(bool nightMode)
        {
            IsNightMode = nightMode;
            ApplyLightingMode();
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void UpdateSunIntensity()
        {
            // Attempt to read from directional light; fall back to constant
            var sun = RenderSettings.sun;
            _sunIntensity = sun != null ? sun.intensity : 1f;
        }

        private void ApplyLightingMode()
        {
            // Street lights
            foreach (var light in _streetLights)
                if (light != null) light.enabled = IsNightMode;

            // Window emission
            Color emissionColor = IsNightMode ? windowNightColor : windowDayColor;
            foreach (var r in _windowRenderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        if (IsNightMode)
                            mat.EnableKeyword("_EMISSION");
                        else
                            mat.DisableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColor);
                    }
                }
            }
        }
    }
}
