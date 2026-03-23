using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.CityGen
{
    /// <summary>
    /// Phase 52 — Manages city lighting for the day/night cycle.
    ///
    /// <para>Integrates with the TimeOfDay system: window emissive materials are
    /// enabled at dusk and disabled at dawn.  Street lights can be real
    /// <see cref="Light"/> components (nearby) or emissive mesh dots (distant).</para>
    ///
    /// <para>Performance: only buildings within <see cref="detailLightRadius"/> receive
    /// per-window light toggling; buildings beyond that distance use a single
    /// emissive material property change per renderer.</para>
    /// </summary>
    public class CityLightingController : MonoBehaviour
    {
        #region Constants

        private const float DuskHour  = 18f;   // 6 PM
        private const float DawnHour  = 6f;    // 6 AM
        private const float FlickerInterval = 0.5f;

        #endregion

        #region Inspector

        [Header("Window Materials")]
        [Tooltip("Material applied to windows during night hours (emissive variant).")]
        [SerializeField] private Material windowNightMaterial;

        [Tooltip("Material applied to windows during day hours.")]
        [SerializeField] private Material windowDayMaterial;

        [Header("Street Lights")]
        [Tooltip("Prefab for a street light point light (used within detail radius).")]
        [SerializeField] private GameObject streetLightPrefab;

        [Tooltip("Warm (residential) street light color.")]
        [SerializeField] private Color warmLightColor  = new Color(1f, 0.85f, 0.5f);

        [Tooltip("Cool (commercial) street light color.")]
        [SerializeField] private Color coolLightColor  = new Color(0.8f, 0.9f, 1f);

        [Header("Ranges")]
        [Tooltip("Buildings inside this radius get full per-window lighting.")]
        [SerializeField] private float detailLightRadius = 300f;

        [Tooltip("City glow halo visibility distance.")]
        [SerializeField] private float glowRadius = 2000f;

        [Header("Animation")]
        [Tooltip("Whether windows randomly flicker on/off for a lived-in feel.")]
        [SerializeField] private bool enableWindowFlicker = true;

        [Tooltip("Maximum number of windows that may flicker per second.")]
        [SerializeField] private int flickerBudget = 20;

        #endregion

        #region Private State

        // Renderers whose window state we manage.
        private readonly List<Renderer> _windowRenderers = new List<Renderer>();
        // Street lights spawned for road segments.
        private readonly List<GameObject> _streetLights  = new List<GameObject>();

        private bool _isNight = false;
        private Coroutine _flickerCoroutine;
        private Camera _camera;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _camera = Camera.main;
            _flickerCoroutine = StartCoroutine(FlickerLoop());
        }

        private void Update()
        {
            UpdateTimeOfDay();
        }

        private void OnDestroy()
        {
            if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Registers all window renderers found under <paramref name="settlementRoot"/>
        /// for lighting management.
        /// </summary>
        public void RegisterSettlement(Transform settlementRoot)
        {
            if (settlementRoot == null) return;
            var renderers = settlementRoot.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                if (r.gameObject.name.Contains("Window") || r.gameObject.name.Contains("LOD0"))
                    _windowRenderers.Add(r);
        }

        /// <summary>
        /// Spawns street lights along road <paramref name="segments"/> within a
        /// given parent transform.
        /// </summary>
        public void PlaceStreetLights(IEnumerable<RoadSegment> segments, Transform parent)
        {
            if (streetLightPrefab == null) return;
            const float Spacing = 40f;

            foreach (var seg in segments)
            {
                if (seg.roadType == RoadType.Alley || seg.roadType == RoadType.Pedestrian)
                    continue;

                float length = Vector3.Distance(seg.start, seg.end);
                int count    = Mathf.Max(1, Mathf.RoundToInt(length / Spacing));
                Vector3 dir  = (seg.end - seg.start) / (count + 1);
                Vector3 perp = Vector3.Cross(Vector3.up, dir.normalized) * (seg.width * 0.6f);

                for (int i = 1; i <= count; i++)
                {
                    Vector3 pos = seg.start + dir * i + perp;
                    var go = Instantiate(streetLightPrefab, pos, Quaternion.identity, parent);
                    go.name = "StreetLight";
                    _streetLights.Add(go);
                }
            }
        }

        #endregion

        #region Time Of Day

        private void UpdateTimeOfDay()
        {
            float hour = GetCurrentHour();
            bool shouldBeNight = (hour >= DuskHour || hour < DawnHour);

            if (shouldBeNight == _isNight) return;
            _isNight = shouldBeNight;
            ApplyLightingState(_isNight);
        }

        private void ApplyLightingState(bool isNight)
        {
            if (_camera == null) return;
            Vector3 camPos = _camera.transform.position;
            var propertyBlock = new MaterialPropertyBlock();

            foreach (var r in _windowRenderers)
            {
                if (r == null) continue;
                float dist = (r.transform.position - camPos).magnitude;

                if (dist < detailLightRadius)
                {
                    // Full per-renderer material swap (instance material to avoid shared mutation).
                    r.material = isNight ? windowNightMaterial : windowDayMaterial;
                }
                else
                {
                    // Use MaterialPropertyBlock for emissive toggle — avoids shared material mutation.
                    r.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_EmissionColor", isNight ? warmLightColor : Color.black);
                    r.SetPropertyBlock(propertyBlock);
                }
            }

            // Toggle street light GameObjects.
            foreach (var sl in _streetLights)
            {
                if (sl == null) continue;
                var light = sl.GetComponentInChildren<Light>();
                if (light != null) light.enabled = isNight;
            }
        }

        private static float GetCurrentHour()
        {
#if SWEF_TIMEOFDAY_AVAILABLE
            var tod = TimeOfDay.TimeOfDayManager.Instance;
            if (tod != null) return tod.CurrentHour;
#endif
            // Fallback: use real wall-clock time.
            return (float)System.DateTime.Now.TimeOfDay.TotalHours;
        }

        #endregion

        #region Window Flicker

        private IEnumerator FlickerLoop()
        {
            var block = new MaterialPropertyBlock();
            while (true)
            {
                yield return new WaitForSeconds(FlickerInterval);
                if (!enableWindowFlicker || !_isNight || _windowRenderers.Count == 0) continue;

                int budget = Mathf.Min(flickerBudget, _windowRenderers.Count);
                for (int i = 0; i < budget; i++)
                {
                    int  idx = Random.Range(0, _windowRenderers.Count);
                    var  r   = _windowRenderers[idx];
                    if (r == null) continue;

                    bool on = Random.value > 0.3f;
                    r.GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", on ? warmLightColor : Color.black);
                    r.SetPropertyBlock(block);
                }
            }
        }

        #endregion
    }
}
