using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Debug visualization and controls for the wildlife system.
    /// Only active in UNITY_EDITOR or DEVELOPMENT_BUILD configurations.
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class WildlifeDebugOverlay : MonoBehaviour
    {
        #region Inspector

        [Header("Gizmo Colors")]
        [SerializeField] private Color spawnRingColor   = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color despawnRingColor = new Color(1f, 0f, 0f, 0.2f);
        [SerializeField] private Color groupSphereColor  = Color.white;

        [Header("Debug HUD")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private Rect hudRect = new Rect(10, 10, 280, 350);

        #endregion

        #region Private State

        private readonly Queue<string> _eventLog = new Queue<string>();
        private const int MaxLogEntries = 10;
        private float _lastAITickMs;
        private string _forcedSpeciesId = string.Empty;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Register with DebugOverlayController (null-safe)
#if SWEF_DEBUG_AVAILABLE
            var doc = SWEF.DebugOverlay.DebugOverlayController.Instance;
            doc?.RegisterOverlay("Wildlife", this);
#endif
            SubscribeToManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();
        }

        #endregion

        #region Manager Subscriptions

        private void SubscribeToManager()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            mgr.OnGroupSpawned   += g  => LogEvent($"Spawned: {g.species?.speciesId} ({g.memberCount})");
            mgr.OnGroupDespawned += id => LogEvent($"Despawned: {id}");
            mgr.OnBirdStrike     += (s, p) => LogEvent($"BIRD STRIKE: {s?.speciesId} @ {p:F0}");
        }

        private void UnsubscribeFromManager()
        {
            // Event lambda references not cached; manager cleanup on destroy is sufficient
        }

        private void LogEvent(string msg)
        {
            _eventLog.Enqueue($"[{Time.time:F1}] {msg}");
            if (_eventLog.Count > MaxLogEntries)
                _eventLog.Dequeue();
        }

        #endregion

        #region Debug HUD

        private void OnGUI()
        {
            if (!showHUD) return;
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;

            GUILayout.BeginArea(hudRect, GUI.skin.box);
            GUILayout.Label("<b>Wildlife Debug</b>");
            GUILayout.Label($"Groups: {mgr.ActiveGroups.Count} / {mgr.Config.maxActiveGroups}");
            GUILayout.Label($"Individuals: {mgr.TotalIndividuals} / {mgr.Config.maxIndividualsTotal}");

            GUILayout.Space(6);
            GUILayout.Label("<b>Event Log</b>");
            foreach (var e in _eventLog)
                GUILayout.Label(e, GUILayout.MaxWidth(260));

            GUILayout.Space(6);
            GUILayout.Label("<b>Controls</b>");
            _forcedSpeciesId = GUILayout.TextField(_forcedSpeciesId);

            if (GUILayout.Button("Force Spawn"))
                ForceSpawn(_forcedSpeciesId);

            if (GUILayout.Button("Force All Flee"))
                ForceAllFlee();

            if (GUILayout.Button("Clear All Wildlife"))
                WildlifeManager.Instance?.GetComponent<WildlifeSpawnSystem>()?.DespawnAllGroups();

            GUILayout.EndArea();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            DrawRadiusGizmos();
            DrawGroupGizmos();
        }

        private void DrawRadiusGizmos()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr?.Config == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            Gizmos.color = spawnRingColor;
            DrawCircle(cam.transform.position, mgr.Config.spawnRadius);

            Gizmos.color = despawnRingColor;
            DrawCircle(cam.transform.position, mgr.Config.despawnRadius);
        }

        private void DrawGroupGizmos()
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            var cam = Camera.main;

            foreach (var g in mgr.ActiveGroups)
            {
                Gizmos.color = GetCategoryColor(g.species?.category ?? WildlifeCategory.Bird);
                Gizmos.DrawWireSphere(g.centerPosition, 20f);

                if (cam != null)
                {
                    Gizmos.color = GetThreatColor(g.threatLevel);
                    Gizmos.DrawLine(cam.transform.position, g.centerPosition);
                }
            }
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            int segments = 64;
            float step   = 360f / segments;
            Vector3 prev  = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float rad  = i * step * Mathf.Deg2Rad;
                Vector3 cur = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        private static Color GetCategoryColor(WildlifeCategory cat)
        {
            switch (cat)
            {
                case WildlifeCategory.Bird:         return Color.yellow;
                case WildlifeCategory.Raptor:       return Color.red;
                case WildlifeCategory.Seabird:      return Color.white;
                case WildlifeCategory.MarineMammal: return Color.cyan;
                case WildlifeCategory.Fish:         return new Color(0.2f, 0.6f, 1f);
                case WildlifeCategory.LandMammal:   return new Color(0.6f, 0.4f, 0.2f);
                case WildlifeCategory.Insect:       return Color.green;
                case WildlifeCategory.Mythical:     return Color.magenta;
                default:                            return Color.white;
            }
        }

        private static Color GetThreatColor(WildlifeThreatLevel level)
        {
            switch (level)
            {
                case WildlifeThreatLevel.None:     return Color.green;
                case WildlifeThreatLevel.Aware:    return Color.yellow;
                case WildlifeThreatLevel.Alarmed:  return new Color(1f, 0.5f, 0f);
                case WildlifeThreatLevel.Fleeing:  return Color.red;
                case WildlifeThreatLevel.Panicked: return Color.magenta;
                default:                           return Color.white;
            }
        }

        #endregion

        #region Debug Controls

        private void ForceSpawn(string speciesId)
        {
            var mgr = WildlifeManager.Instance;
            if (mgr == null) return;
            var species = mgr.GetSpeciesById(speciesId);
            if (species == null) { Debug.LogWarning($"[WildlifeDebug] Species not found: {speciesId}"); return; }
            var cam = Camera.main;
            Vector3 pos = cam != null ? cam.transform.position + cam.transform.forward * 200f : Vector3.zero;
            mgr.GetComponent<WildlifeSpawnSystem>()?.SpawnGroup(species, pos, species.minGroupSize);
        }

        private void ForceAllFlee()
        {
            foreach (var ctrl in FindObjectsOfType<AnimalGroupController>())
                ctrl.ForceScatter();
        }

        #endregion
    }
#else
    // Stub for non-debug builds
    public class WildlifeDebugOverlay : MonoBehaviour { }
#endif
}
