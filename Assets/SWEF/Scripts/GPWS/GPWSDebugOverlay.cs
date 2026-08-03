#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace SWEF.GPWS
{
    /// <summary>
    /// Phase 121 — Editor/development debug overlay for the GPWS subsystem.
    ///
    /// <para>Draws terrain scan rays, threat-zone boundaries, and alert trigger
    /// markers as scene-view gizmos and optional runtime GUI overlay.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class GPWSDebugOverlay : MonoBehaviour
    {
        #region Inspector

        [Header("Gizmo Settings")]
        [SerializeField] private bool drawTerrainRays = true;
        [SerializeField] private bool drawThreatZones = true;
        [SerializeField] private bool drawAltitudeRing = true;
        [SerializeField] private float gizmoAlpha = 0.6f;

        [Header("Runtime GUI")]
        [SerializeField] private bool showRuntimeGUI = true;

        #endregion

        #region Private State

        private GPWSManager _mgr;
        private TerrainAwarenessDisplay _taws;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _mgr = GPWSManager.Instance;
            _taws = FindFirstObjectByType<TerrainAwarenessDisplay>();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (_mgr == null) return;

            if (drawTerrainRays) DrawTerrainScanRays();
            if (drawThreatZones) DrawThreatZones();
            if (drawAltitudeRing) DrawAltitudeRing();
        }

        private void OnGUI()
        {
            if (!showRuntimeGUI || _mgr == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("<b>GPWS Debug</b>");
            GUILayout.Label($"AGL: {_mgr.AircraftAltitudeAglFt:F0} ft");
            GUILayout.Label($"VS: {_mgr.VerticalSpeedFpm:F0} fpm");
            GUILayout.Label($"IAS: {_mgr.AirspeedKnots:F0} kt");
            GUILayout.Label($"Gear: {(_mgr.GearDown ? "DOWN" : "UP")}");
            GUILayout.Label($"Terrain: {_mgr.TerrainAltitudeFt:F0} ft");
            GUILayout.Label($"Enabled: {_mgr.IsEnabled}");

            GUILayout.Space(8);
            GUILayout.Label($"<b>Active Alerts ({_mgr.ActiveAlerts.Count})</b>");
            foreach (var a in _mgr.ActiveAlerts)
                GUILayout.Label($"  [{a.level}] {a.mode} — {a.messageKey}");

            var ws = FindFirstObjectByType<WindshearDetector>();
            if (ws != null)
            {
                GUILayout.Space(8);
                GUILayout.Label($"Windshear: {(ws.WindshearActive ? "ACTIVE" : "clear")}");
                GUILayout.Label($"Microburst: {(ws.MicroburstDetected ? "YES" : "no")}");
                GUILayout.Label($"Shear: {ws.CurrentShearKnots:F1} kt");
            }

            GUILayout.EndArea();
        }

        #endregion

        #region Gizmo Drawing

        private void DrawTerrainScanRays()
        {
            if (_taws == null || _taws.LatestScan == null) return;

            foreach (var cell in _taws.LatestScan.cells)
            {
                Color col = cell.threatLevel switch
                {
                    TerrainThreatLevel.Danger   => new Color(1f, 0f, 0f, gizmoAlpha),
                    TerrainThreatLevel.Caution  => new Color(1f, 1f, 0f, gizmoAlpha),
                    TerrainThreatLevel.Advisory => new Color(0f, 1f, 0f, gizmoAlpha),
                    _                           => new Color(0f, 0.5f, 0f, gizmoAlpha * 0.5f)
                };

                Gizmos.color = col;
                Gizmos.DrawLine(transform.position, cell.position);
                Gizmos.DrawSphere(cell.position, 20f);
            }
        }

        private void DrawThreatZones()
        {
            if (_taws == null || _taws.LatestScan == null) return;

            foreach (var cell in _taws.LatestScan.cells)
            {
                if (cell.threatLevel < TerrainThreatLevel.Caution) continue;

                Color col = cell.threatLevel == TerrainThreatLevel.Danger
                    ? new Color(1f, 0f, 0f, gizmoAlpha * 0.3f)
                    : new Color(1f, 1f, 0f, gizmoAlpha * 0.3f);

                Gizmos.color = col;
                Gizmos.DrawCube(cell.position, new Vector3(100f, 50f, 100f));
            }
        }

        private void DrawAltitudeRing()
        {
            float aglM = _mgr.AircraftAltitudeAglFt / 3.28084f;
            Gizmos.color = new Color(0f, 1f, 1f, gizmoAlpha * 0.5f);
            Gizmos.DrawWireSphere(transform.position, aglM);
        }

        #endregion
    }
}
#endif
