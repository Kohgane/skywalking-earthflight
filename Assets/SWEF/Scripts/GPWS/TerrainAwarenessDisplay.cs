using System.Collections.Generic;
using UnityEngine;

#if SWEF_MINIMAP_AVAILABLE
using SWEF.Minimap;
#endif

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Terrain Awareness and Warning System (TAWS) display.
    ///
    /// <para>Scans terrain ahead of the aircraft via raycasts, classifies cells by
    /// threat level, renders results to a <see cref="RenderTexture"/> for cockpit MFD
    /// integration, and optionally registers danger blips on the minimap
    /// (<c>#if SWEF_MINIMAP_AVAILABLE</c>).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainAwarenessDisplay : MonoBehaviour
    {
        #region Inspector

        [Header("Scan Settings")]
        [Tooltip("Forward-looking scan arc in degrees (total spread).")]
        [SerializeField] private float scanArcDegrees = 120f;

        [Tooltip("Maximum scan distance in metres.")]
        [SerializeField] private float scanDistanceM = 9260f; // ~5 nm

        [Tooltip("Number of radial rays per sweep.")]
        [SerializeField] private int rayCount = 36;

        [Tooltip("Scan interval in seconds.")]
        [SerializeField] private float scanInterval = 1f;

        [Header("Display")]
        [Tooltip("RenderTexture to paint the TAWS terrain display on.")]
        [SerializeField] private RenderTexture displayTexture;

        [Tooltip("Terrain layer mask for raycasts.")]
        [SerializeField] private LayerMask terrainMask = ~0;

        [Header("Threat Thresholds (feet below aircraft)")]
        [SerializeField] private float cautionThresholdFt = 1000f;
        [SerializeField] private float dangerThresholdFt = 500f;

        #endregion

        #region Public Properties

        public TAWSDisplayMode DisplayMode { get; set; } = TAWSDisplayMode.Normal;
        public TerrainScanResult LatestScan { get; private set; }

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private float _nextScanTime;
        private Texture2D _displayBitmap;
        private readonly List<TerrainCell> _cells = new List<TerrainCell>();
        private const float MetresToFeet = 3.28084f;
        private const float MetresToNm = 0.000539957f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            if (displayTexture != null)
            {
                _displayBitmap = new Texture2D(displayTexture.width, displayTexture.height, TextureFormat.RGBA32, false);
                ClearBitmap();
            }
        }

        private void Update()
        {
            if (DisplayMode == TAWSDisplayMode.Off || _mgr == null) return;
            if (Time.time < _nextScanTime) return;

            _nextScanTime = Time.time + scanInterval;
            PerformScan();
            RenderDisplay();
        }

        private void OnDestroy()
        {
            if (_displayBitmap != null)
                Destroy(_displayBitmap);
        }

        #endregion

        #region Scan

        private void PerformScan()
        {
            _cells.Clear();

            Transform acft = transform;
            if (_mgr != null && Camera.main != null)
                acft = Camera.main.transform;

            Vector3 origin = acft.position;
            Vector3 fwd = acft.forward;
            float halfArc = scanArcDegrees * 0.5f;
            float step = scanArcDegrees / Mathf.Max(1, rayCount - 1);

            float maxElev = float.MinValue;
            float minClearance = float.MaxValue;
            float elevSum = 0f;
            int elevCount = 0;
            Vector3 threatDir = Vector3.zero;
            bool hasThreat = false;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = -halfArc + step * i;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * fwd;
                dir = dir.normalized;

                if (!Physics.Raycast(origin, dir, out var hit, scanDistanceM, terrainMask))
                    continue;

                float terrainElev = hit.point.y * MetresToFeet;
                float aircraftFt = origin.y * MetresToFeet;
                float clearance = aircraftFt - terrainElev;
                float distNm = hit.distance * MetresToNm;

                TerrainThreatLevel threat = TerrainThreatLevel.Safe;
                if (clearance < dangerThresholdFt)
                    threat = TerrainThreatLevel.Danger;
                else if (clearance < cautionThresholdFt)
                    threat = TerrainThreatLevel.Caution;
                else if (clearance < cautionThresholdFt * 2f)
                    threat = TerrainThreatLevel.Advisory;

                _cells.Add(new TerrainCell
                {
                    position = hit.point,
                    elevationFt = terrainElev,
                    threatLevel = threat,
                    distanceToAircraftNm = distNm,
                    bearing = angle
                });

                if (terrainElev > maxElev) maxElev = terrainElev;
                if (clearance < minClearance) minClearance = clearance;
                elevSum += terrainElev;
                elevCount++;

                if (threat >= TerrainThreatLevel.Caution)
                {
                    threatDir = dir;
                    hasThreat = true;
                }
            }

            LatestScan = new TerrainScanResult
            {
                cells = _cells.ToArray(),
                maxElevationFt = elevCount > 0 ? maxElev : 0f,
                minClearanceFt = elevCount > 0 ? minClearance : float.MaxValue,
                averageElevationFt = elevCount > 0 ? elevSum / elevCount : 0f,
                threatDirection = threatDir,
                scanTimestamp = Time.time
            };

            if (hasThreat && _mgr != null)
                _mgr.TriggerTerrainAhead(LatestScan);

#if SWEF_MINIMAP_AVAILABLE
            RegisterMinimapBlips();
#endif
        }

        #endregion

        #region Render

        private void RenderDisplay()
        {
            if (_displayBitmap == null || displayTexture == null) return;

            ClearBitmap();

            int cx = _displayBitmap.width / 2;
            int cy = _displayBitmap.height / 2;
            float scale = cx / (scanDistanceM * MetresToNm);

            foreach (var cell in _cells)
            {
                float nx = Mathf.Sin(cell.bearing * Mathf.Deg2Rad) * cell.distanceToAircraftNm * scale;
                float ny = Mathf.Cos(cell.bearing * Mathf.Deg2Rad) * cell.distanceToAircraftNm * scale;

                int px = cx + Mathf.RoundToInt(nx);
                int py = cy + Mathf.RoundToInt(ny);

                if (px < 0 || px >= _displayBitmap.width || py < 0 || py >= _displayBitmap.height)
                    continue;

                Color col = GetThreatColor(cell.threatLevel);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int sx = px + dx;
                        int sy = py + dy;
                        if (sx >= 0 && sx < _displayBitmap.width && sy >= 0 && sy < _displayBitmap.height)
                            _displayBitmap.SetPixel(sx, sy, col);
                    }
            }

            _displayBitmap.Apply();
            Graphics.Blit(_displayBitmap, displayTexture);
        }

        private void ClearBitmap()
        {
            if (_displayBitmap == null) return;
            var fill = new Color[_displayBitmap.width * _displayBitmap.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0f, 0.05f, 0f, 1f);
            _displayBitmap.SetPixels(fill);
        }

        private static Color GetThreatColor(TerrainThreatLevel level) => level switch
        {
            TerrainThreatLevel.Danger   => Color.red,
            TerrainThreatLevel.Caution  => Color.yellow,
            TerrainThreatLevel.Advisory => new Color(0f, 0.8f, 0f),
            _                           => new Color(0f, 0.4f, 0f)
        };

        #endregion

        #region Minimap Integration

#if SWEF_MINIMAP_AVAILABLE
        private void RegisterMinimapBlips()
        {
            var mm = MinimapManager.Instance;
            if (mm == null || LatestScan == null) return;

            foreach (var cell in LatestScan.cells)
            {
                if (cell.threatLevel < TerrainThreatLevel.Caution) continue;
                var blip = new MinimapBlip
                {
                    blipId = $"gpws_terrain_{cell.bearing:F0}_{cell.distanceToAircraftNm:F1}",
                    position = cell.position,
                    blipType = MinimapBlipType.DangerZone,
                    isActive = true,
                    label = $"TERRAIN {cell.elevationFt:F0}ft"
                };
                mm.RegisterBlip(blip);
            }
        }
#endif

        #endregion
    }
}
