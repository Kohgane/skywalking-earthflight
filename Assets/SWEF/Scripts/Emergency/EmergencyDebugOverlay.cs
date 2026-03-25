using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Emergency
{
    /// <summary>
    /// Phase 76 — Debug-only overlay providing Gizmos for landing sites, glide range
    /// and rescue paths, plus an in-game HUD panel with trigger / escalate / resolve /
    /// dispatch / toggle / reset controls.
    /// </summary>
    public class EmergencyDebugOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        #region Inspector

        [Header("References")]
        [SerializeField] private EmergencyLandingController landingController;
        [SerializeField] private RescueSimulationController rescueController;

        [Header("Gizmo Colors")]
        [SerializeField] private Color landingSiteColor = new Color(0f, 1f, 0f, 0.6f);
        [SerializeField] private Color glideRangeColor  = new Color(0f, 0.8f, 1f, 0.3f);
        [SerializeField] private Color rescuePathColor  = new Color(1f, 0.5f, 0f, 0.8f);
        [SerializeField] private Color criticalColor    = new Color(1f, 0f, 0f, 0.7f);

        [Header("HUD")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private int  hudFontSize = 14;

        #endregion

        #region Private State

        private readonly List<string> _eventLog = new List<string>(20);
        private Vector2 _scrollPos;
        private int _scenarioDropdownIndex;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            var mgr = EmergencyManager.Instance;
            if (mgr == null) return;
            mgr.OnEmergencyTriggered    += em => Log($"TRIGGERED {em.scenario.scenarioId} [{em.emergencyId}]");
            mgr.OnEmergencyEscalated    += (em, prev) => Log($"ESCALATED {em.emergencyId}: {prev}→{em.currentSeverity}");
            mgr.OnEmergencyPhaseChanged += (em, ph) => Log($"PHASE {em.emergencyId}: {ph}");
            mgr.OnEmergencyResolved     += res => Log($"RESOLVED {res.emergencyId} success={res.wasSuccessful} score={res.score:F0}");
            mgr.OnDistressCallMade      += (em, ct) => Log($"DISTRESS {em.emergencyId}: {ct}");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            DrawLandingSiteGizmos();
            DrawGlideRangeGizmo();
            DrawRescuePathGizmos();
        }

        private void DrawLandingSiteGizmos()
        {
            if (landingController == null) return;
            // Landing sites are serialized inside the controller; draw a cube at this object's position
            // as a placeholder indicator when no runtime data is available.
            Gizmos.color = landingSiteColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 5f, new Vector3(50f, 2f, 100f));
        }

        private void DrawGlideRangeGizmo()
        {
            if (landingController == null) return;
            float range = landingController.ComputeGlideRange(transform.position.y);
            Gizmos.color = glideRangeColor;
            DrawWireCircleXZ(transform.position, range, 36);
        }

        private void DrawRescuePathGizmos()
        {
            if (rescueController == null) return;
            Gizmos.color = rescuePathColor;
            foreach (var unit in rescueController.ActiveUnits)
            {
                if (!unit.hasArrived)
                    Gizmos.DrawLine(unit.position, unit.targetPosition);
                Gizmos.DrawSphere(unit.position, 20f);
            }
        }

        private static void DrawWireCircleXZ(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step * Mathf.Deg2Rad;
                float a1 = (i + 1) * step * Mathf.Deg2Rad;
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                Gizmos.DrawLine(p0, p1);
            }
        }

        #endregion

        #region In-Game HUD

        private void OnGUI()
        {
            if (!showHUD) return;

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize  = hudFontSize,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.white;

            float w = 340f, h = 420f;
            GUILayout.BeginArea(new Rect(Screen.width - w - 10f, 10f, w, h), GUI.skin.box);
            GUILayout.Label("<b>[EMERGENCY DEBUG]</b>", style);
            GUILayout.Space(4f);

            DrawManagerStatus();
            GUILayout.Space(4f);
            DrawControls();
            GUILayout.Space(4f);
            DrawEventLog();

            GUILayout.EndArea();
        }

        private void DrawManagerStatus()
        {
            var mgr = EmergencyManager.Instance;
            if (mgr == null) { GUILayout.Label("EmergencyManager: NULL"); return; }
            GUILayout.Label($"Active emergencies: {mgr.ActiveEmergencies.Count}/{mgr.Config.maxSimultaneousEmergencies}");
            foreach (var em in mgr.ActiveEmergencies)
                GUILayout.Label($"  [{em.emergencyId}] {em.scenario.type} | {em.currentSeverity} | {em.currentPhase} | t={em.elapsedTime:F0}s");
        }

        private void DrawControls()
        {
            var mgr = EmergencyManager.Instance;
            if (mgr == null) return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Engine Failure"))
                mgr.TriggerEmergencyByType(EmergencyType.EngineFailure, transform.position, transform.position.y);
            if (GUILayout.Button("Trigger Fire"))
                mgr.TriggerEmergencyByType(EmergencyType.FireOnboard, transform.position, transform.position.y);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trigger Depress."))
                mgr.TriggerEmergencyByType(EmergencyType.Depressurization, transform.position, transform.position.y);
            if (GUILayout.Button("Trigger Dual Eng."))
                mgr.TriggerEmergencyByType(EmergencyType.DualEngineFailure, transform.position, transform.position.y);
            GUILayout.EndHorizontal();

            if (mgr.ActiveEmergencies.Count > 0)
            {
                var first = mgr.ActiveEmergencies[0];
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Resolve (success)"))
                    mgr.ResolveEmergency(first, true);
                if (GUILayout.Button("Resolve (fail)"))
                    mgr.ResolveEmergency(first, false);
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Clear All"))
            {
                for (int i = mgr.ActiveEmergencies.Count - 1; i >= 0; i--)
                    mgr.ResolveEmergency(mgr.ActiveEmergencies[i], false);
            }
        }

        private void DrawEventLog()
        {
            GUILayout.Label($"<b>Event Log</b> (last {_eventLog.Count}):");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(100f));
            foreach (var entry in _eventLog)
                GUILayout.Label(entry);
            GUILayout.EndScrollView();
        }

        #endregion

        #region Helpers

        private void Log(string message)
        {
            _eventLog.Insert(0, message);
            if (_eventLog.Count > 20)
                _eventLog.RemoveAt(_eventLog.Count - 1);
        }

        #endregion

#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
    }
}
