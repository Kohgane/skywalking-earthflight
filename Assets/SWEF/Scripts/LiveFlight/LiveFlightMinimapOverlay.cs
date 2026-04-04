// LiveFlightMinimapOverlay.cs — SWEF Live Flight Tracking & Real-World Data Overlay (Phase 103)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.LiveFlight
{
    /// <summary>
    /// Renders colour-coded aircraft blips on the minimap canvas.
    /// Each blip shows the aircraft heading as a rotation, and clicking a blip
    /// selects that aircraft in the HUD.
    ///
    /// <para>All minimap-specific references are guarded with
    /// <c>#if SWEF_MINIMAP_AVAILABLE</c> so this component compiles cleanly even
    /// when the Minimap assembly is absent.</para>
    /// </summary>
    public class LiveFlightMinimapOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Configuration")]
        [SerializeField] private LiveFlightConfig config;

        [Header("Minimap Canvas")]
        [SerializeField] private RectTransform minimapRect;
        [SerializeField] private GameObject    blipPrefab;

        [Tooltip("World-space radius (metres) mapped to the minimap radius.")]
        [SerializeField] private float worldRadius = 500_000f;

#if SWEF_MINIMAP_AVAILABLE
        [Header("Minimap Manager (optional)")]
        [SerializeField] private SWEF.Minimap.MinimapManager minimapManager;
#endif

        // ── State ─────────────────────────────────────────────────────────────────
        private readonly List<GameObject>       _blipPool   = new List<GameObject>();
        private readonly List<GameObject>       _activeBlips = new List<GameObject>();
        private List<LiveAircraftInfo>          _lastData   = new List<LiveAircraftInfo>();
        private LiveFlightHUD                   _hudCache;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Start()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived += OnDataReceived;

            _hudCache = FindFirstObjectByType<LiveFlightHUD>();
        }

        private void OnDestroy()
        {
            if (LiveFlightAPIClient.Instance != null)
                LiveFlightAPIClient.Instance.OnAircraftDataReceived -= OnDataReceived;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Removes all aircraft blips from the minimap.</summary>
        public void ClearBlips()
        {
            ReturnAllBlipsToPool();
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private void OnDataReceived(List<LiveAircraftInfo> data)
        {
            _lastData = data;
            RefreshBlips();
        }

        private void RefreshBlips()
        {
            if (minimapRect == null) return;

            ReturnAllBlipsToPool();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mapSize = minimapRect.rect.size;
            float halfW = mapSize.x * 0.5f;
            float halfH = mapSize.y * 0.5f;
            float scale = Mathf.Min(halfW, halfH) / worldRadius;

            Vector3 camPos = cam.transform.position;

            int max = config != null ? config.maxAircraftDisplayed : 100;
            int count = Mathf.Min(_lastData.Count, max);

            for (int i = 0; i < count; i++)
            {
                var info = _lastData[i];

                // Project aircraft relative to camera onto minimap
                Vector3 worldPos = new Vector3(
                    (float)(info.longitude * Mathf.Deg2Rad * 6_371_000.0),
                    info.altitude,
                    (float)(info.latitude  * Mathf.Deg2Rad * 6_371_000.0));

                Vector2 delta = new Vector2(
                    worldPos.x - camPos.x,
                    worldPos.z - camPos.z);

                if (delta.magnitude > worldRadius) continue;

                Vector2 mapPos = delta * scale;

                // Clamp to minimap bounds
                mapPos.x = Mathf.Clamp(mapPos.x, -halfW, halfW);
                mapPos.y = Mathf.Clamp(mapPos.y, -halfH, halfH);

                GameObject blip = GetBlip();
                var rt = blip.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = mapPos;

                // Heading rotation
                blip.transform.localEulerAngles = new Vector3(0f, 0f, -info.heading);

                // Altitude colour
                if (config != null)
                {
                    float t = Mathf.Clamp01(info.altitude / 13000f);
                    Color c = config.altitudeColorGradient.Evaluate(t);
                    var img = blip.GetComponent<Image>();
                    if (img != null) img.color = c;
                }

                // Click handler to select aircraft in HUD
                string icao = info.icao24;
                var btn = blip.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (_hudCache == null) _hudCache = FindFirstObjectByType<LiveFlightHUD>();
                        _hudCache?.SelectAircraft(icao);
                    });
                }

                blip.SetActive(true);
                _activeBlips.Add(blip);
            }
        }

        // ── Pool management ───────────────────────────────────────────────────────

        /// <summary>Total number of blip GameObjects in the inactive pool.</summary>
        public int BlipPoolSize => _blipPool.Count;

        private GameObject GetBlip()
        {
            if (_blipPool.Count > 0)
            {
                var b = _blipPool[_blipPool.Count - 1];
                _blipPool.RemoveAt(_blipPool.Count - 1);
                return b;
            }

            GameObject newBlip = blipPrefab != null
                ? Instantiate(blipPrefab, minimapRect)
                : CreateDefaultBlip();
            return newBlip;
        }

        private void ReturnAllBlipsToPool()
        {
            foreach (var b in _activeBlips)
            {
                if (b == null) continue;
                b.SetActive(false);
                _blipPool.Add(b);
            }
            _activeBlips.Clear();
        }

        private GameObject CreateDefaultBlip()
        {
            var go = new GameObject("AircraftBlip");
            go.transform.SetParent(minimapRect, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(8f, 8f);
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            return go;
        }
    }
}
