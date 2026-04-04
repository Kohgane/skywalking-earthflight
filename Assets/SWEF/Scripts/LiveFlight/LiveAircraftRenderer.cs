// LiveAircraftRenderer.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Singleton MonoBehaviour that maintains an object pool of aircraft marker
    /// GameObjects and updates their positions / colours each time the API client
    /// delivers fresh data.
    ///
    /// <para>When compiled with <c>SWEF_TERRAIN_AVAILABLE</c> the renderer integrates
    /// with <see cref="SWEF.Terrain.CesiumTerrainBridge"/> for georeference-aware
    /// positioning.</para>
    /// </summary>
    public class LiveAircraftRenderer : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static LiveAircraftRenderer Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private LiveFlightConfig config;

        [Header("Prefabs")]
        [SerializeField] private GameObject aircraftMarkerPrefab;
        [SerializeField] private GameObject labelPrefab;

        [Header("LOD")]
        [Tooltip("Labels are hidden beyond this world-space distance from the camera.")]
        [SerializeField] private float labelHideDistance = 50000f;

        [Tooltip("Full geometry is replaced by a simple dot beyond this distance.")]
        [SerializeField] private float lodDistance = 200000f;

        // ── Pool ──────────────────────────────────────────────────────────────────
        private readonly List<GameObject>         _pool       = new List<GameObject>();
        private readonly List<GameObject>         _active     = new List<GameObject>();
        private readonly List<LiveAircraftInfo>   _current    = new List<LiveAircraftInfo>();
        private readonly List<LiveAircraftInfo>   _previous   = new List<LiveAircraftInfo>();
        private float _lerpTimer;
        private float _lerpDuration;

        // ── Visibility ────────────────────────────────────────────────────────────
        private bool _visible = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived += ShowAircraft;

#if SWEF_TERRAIN_AVAILABLE
            // Terrain bridge integration: no extra calls needed; positions are
            // converted inside WorldPositionForAircraft() below.
            Debug.Log("[LiveAircraftRenderer] SWEF_TERRAIN_AVAILABLE: Cesium georeference active.");
#endif
        }

        private void OnDestroy()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived -= ShowAircraft;
        }

        private void Update()
        {
            if (!_visible || _current.Count == 0) return;
            if (config == null) return;

            _lerpTimer += Time.deltaTime;
            float t = _lerpDuration > 0f
                ? Mathf.Clamp01(_lerpTimer / _lerpDuration)
                : 1f;

            Camera cam = Camera.main;

            for (int i = 0; i < _active.Count && i < _current.Count; i++)
            {
                var cur  = _current[i];
                var go   = _active[i];
                if (go == null) continue;

                // Interpolate between previous and current position.
                Vector3 prevPos = i < _previous.Count
                    ? WorldPositionForAircraft(_previous[i])
                    : WorldPositionForAircraft(cur);
                Vector3 curPos  = WorldPositionForAircraft(cur);
                go.transform.position = Vector3.Lerp(prevPos, curPos, t);

                // Heading rotation (Y-axis)
                go.transform.rotation = Quaternion.Euler(0f, cur.heading, 0f);

                // Altitude colour
                ApplyAltitudeColor(go, cur.altitude);

                // LOD — label visibility
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, go.transform.position);
                    SetLabelVisible(go, _visible && config.showLabels && dist < labelHideDistance);
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Updates the renderer with the latest aircraft list.</summary>
        public void ShowAircraft(List<LiveAircraftInfo> aircraft)
        {
            if (!_visible) return;
            if (config == null) return;

            // Store previous positions for interpolation.
            _previous.Clear();
            _previous.AddRange(_current);
            _current.Clear();

            int displayCount = Mathf.Min(aircraft.Count, config.maxAircraftDisplayed);

            // Recycle or expand pool.
            while (_pool.Count + _active.Count < displayCount)
                GrowPool(8);

            // Return all active markers to pool.
            ReturnAllToPool();

            // Activate one marker per aircraft.
            for (int i = 0; i < displayCount; i++)
            {
                var info = aircraft[i];
                _current.Add(info);

                GameObject go = GetFromPool();
                go.transform.position = WorldPositionForAircraft(info);
                go.transform.rotation = Quaternion.Euler(0f, info.heading, 0f);
                go.transform.localScale = Vector3.one * (config?.iconScale ?? 1f);
                go.SetActive(true);
                _active.Add(go);

                UpdateLabel(go, info);
                ApplyAltitudeColor(go, info.altitude);
            }

            _lerpTimer    = 0f;
            _lerpDuration = config.pollIntervalSeconds;
        }

        /// <summary>Hides all active aircraft markers and returns them to the pool.</summary>
        public void HideAll()
        {
            ReturnAllToPool();
            _current.Clear();
            _previous.Clear();
        }

        /// <summary>Toggles overall visibility without destroying pooled objects.</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            foreach (var go in _active)
                if (go != null) go.SetActive(visible);
        }

        // ── Pool management ───────────────────────────────────────────────────────

        /// <summary>Returns the total number of pooled (inactive) marker objects.</summary>
        public int PoolSize => _pool.Count;

        private void GrowPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = aircraftMarkerPrefab != null
                    ? Instantiate(aircraftMarkerPrefab, transform)
                    : CreateDefaultMarker();
                go.SetActive(false);
                _pool.Add(go);
            }
        }

        private GameObject GetFromPool()
        {
            if (_pool.Count == 0) GrowPool(1);
            var go = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            return go;
        }

        private void ReturnAllToPool()
        {
            foreach (var go in _active)
            {
                if (go == null) continue;
                go.SetActive(false);
                _pool.Add(go);
            }
            _active.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts aircraft lat/lon/alt to a Unity world-space position.
        /// When <c>SWEF_TERRAIN_AVAILABLE</c> the Cesium georeference is used;
        /// otherwise a simple equirectangular approximation is applied.
        /// </summary>
        private static Vector3 WorldPositionForAircraft(LiveAircraftInfo info)
        {
#if SWEF_TERRAIN_AVAILABLE
            var bridge = SWEF.Terrain.CesiumTerrainBridge.Instance;
            if (bridge != null)
                return bridge.GeodeticToUnity(info.latitude, info.longitude, info.altitude);
#endif
            // Fallback: equirectangular projection (useful for Editor testing).
            const double EarthRadius = 6_371_000.0;
            float x = (float)(info.longitude * Mathf.Deg2Rad * EarthRadius);
            float z = (float)(info.latitude  * Mathf.Deg2Rad * EarthRadius);
            float y = info.altitude;
            return new Vector3(x, y, z);
        }

        private void ApplyAltitudeColor(GameObject go, float altitude)
        {
            if (config == null) return;
            float maxAlt = 13000f; // cruise ceiling ~FL430
            float t = Mathf.Clamp01(altitude / maxAlt);
            Color c = config.altitudeColorGradient.Evaluate(t);

            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = c;
        }

        private void SetLabelVisible(GameObject go, bool visible)
        {
            var label = go.transform.Find("Label");
            if (label != null) label.gameObject.SetActive(visible);
        }

        private void UpdateLabel(GameObject go, LiveAircraftInfo info)
        {
            if (!config.showLabels) return;
            var label = go.transform.Find("Label");
            if (label == null) return;

            var tm = label.GetComponent<TMPro.TextMeshPro>();
            if (tm != null)
                tm.text = $"{info.callsign}\n{info.altitude:F0} m\n{info.velocity:F0} m/s";
        }

        private GameObject CreateDefaultMarker()
        {
            var go   = new GameObject("AircraftMarker");
            go.transform.SetParent(transform, false);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(go.transform, false);
            cube.transform.localScale = Vector3.one * 500f;
            return go;
        }
    }
}
