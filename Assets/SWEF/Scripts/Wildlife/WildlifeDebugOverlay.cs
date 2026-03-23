using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Debug and tuning overlay for the wildlife system.
    ///
    /// <para>Renders an on-screen HUD showing active animal counts, species breakdown,
    /// spawn zones, and per-group behavior/LOD state.  Also draws editor Gizmos for
    /// spawn/despawn radii and chunk boundaries.</para>
    ///
    /// <para>Only compiled in Unity Editor and development builds.</para>
    /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class WildlifeDebugOverlay : MonoBehaviour
    {
        #region Inspector

        [Header("Overlay Visibility")]
        [Tooltip("Toggle the on-screen debug overlay.")]
        [SerializeField] private bool showOverlay = true;

        [Tooltip("Show gizmos for spawn/despawn radii and chunk grid.")]
        [SerializeField] private bool showGizmos = true;

        [Header("Category Toggles")]
        [SerializeField] private bool showBirds       = true;
        [SerializeField] private bool showMarineLife  = true;
        [SerializeField] private bool showLandAnimals = true;
        [SerializeField] private bool showInsects     = true;

        [Header("References")]
        [Tooltip("WildlifeManager to inspect. Resolved at runtime if null.")]
        [SerializeField] private WildlifeManager wildlifeManager;

        [Tooltip("WildlifeSpawnSystem for chunk info. Resolved at runtime if null.")]
        [SerializeField] private WildlifeSpawnSystem spawnSystem;

        #endregion

        #region Constants

        private const int   OverlayWidth   = 420;
        private const int   OverlayHeight  = 320;
        private const int   OverlayPadding = 10;
        private const float ChunkSize      = 1000f;

        #endregion

        #region Private State

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private bool     _stylesInitialised;

        private readonly Dictionary<string, int> _speciesBreakdown = new Dictionary<string, int>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (wildlifeManager == null)
                wildlifeManager = FindFirstObjectByType<WildlifeManager>();
            if (spawnSystem == null)
                spawnSystem = FindFirstObjectByType<WildlifeSpawnSystem>();
        }

        private void OnGUI()
        {
            if (!showOverlay) return;
            InitStyles();
            DrawOverlay();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            DrawSpawnGizmos();
        }

        #endregion

        #region Overlay Drawing

        private void DrawOverlay()
        {
            if (wildlifeManager == null) return;

            Rect rect = new Rect(OverlayPadding, OverlayPadding, OverlayWidth, OverlayHeight);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16));

            GUILayout.Label("🦁 Wildlife Debug", _headerStyle);
            GUILayout.Space(4);

            int total = wildlifeManager.TotalVisibleAnimals;
            int max   = wildlifeManager.Settings.maxAnimalsVisible;
            GUILayout.Label($"Active Animals : {total} / {max}", _labelStyle);
            GUILayout.Label($"Active Groups  : {wildlifeManager.ActiveGroups.Count}", _labelStyle);
            GUILayout.Label($"Discoveries    : {wildlifeManager.DiscoveredSpecies.Count}", _labelStyle);

            GUILayout.Space(6);
            GUILayout.Label("── Species Breakdown ──", _labelStyle);

            BuildBreakdown();
            foreach (var kv in _speciesBreakdown)
            {
                GUILayout.Label($"  {kv.Key}: {kv.Value}", _labelStyle);
            }

            GUILayout.Space(6);
            GUILayout.Label("── Active Groups ──", _labelStyle);
            int shown = 0;
            foreach (var g in wildlifeManager.ActiveGroups)
            {
                if (shown >= 5) { GUILayout.Label("  ...", _labelStyle); break; }
                if (g.species == null) continue;
                if (!PassesCategoryFilter(g.species)) continue;
                GUILayout.Label(
                    $"  {g.species.speciesName} ×{g.memberCount}" +
                    $" | {g.currentBehavior} | pos({g.centerPosition.x:F0},{g.centerPosition.z:F0})",
                    _labelStyle);
                shown++;
            }

            GUILayout.EndArea();
        }

        private void BuildBreakdown()
        {
            _speciesBreakdown.Clear();
            if (wildlifeManager == null) return;
            foreach (var g in wildlifeManager.ActiveGroups)
            {
                if (g.species == null) continue;
                string key = g.species.speciesName;
                _speciesBreakdown.TryGetValue(key, out int cnt);
                _speciesBreakdown[key] = cnt + g.memberCount;
            }
        }

        private bool PassesCategoryFilter(AnimalSpecies s)
        {
            if (s.kingdom == AnimalKingdom.Bird   && !showBirds)       return false;
            if (s.kingdom == AnimalKingdom.Insect && !showInsects)     return false;
            if (s.kingdom == AnimalKingdom.Fish   && !showMarineLife)  return false;
            if (!s.flightCapable && !s.swimCapable && !showLandAnimals) return false;
            return true;
        }

        #endregion

        #region Gizmos

        private void DrawSpawnGizmos()
        {
            if (wildlifeManager == null) return;
            Transform player = Camera.main != null ? Camera.main.transform : null;
            if (player == null) return;

            Vector3 pos = player.position;
            float spawnR   = wildlifeManager.Settings.spawnRadius;
            float despawnR = wildlifeManager.Settings.despawnRadius;

            // Spawn radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            DrawCircleGizmo(pos, spawnR);

            // Despawn radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            DrawCircleGizmo(pos, despawnR);

            // Chunk grid (3×3 around player)
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            int cx = Mathf.FloorToInt(pos.x / ChunkSize);
            int cz = Mathf.FloorToInt(pos.z / ChunkSize);
            for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                float ox = (cx + dx) * ChunkSize;
                float oz = (cz + dz) * ChunkSize;
                Gizmos.DrawWireCube(
                    new Vector3(ox + ChunkSize * 0.5f, pos.y, oz + ChunkSize * 0.5f),
                    new Vector3(ChunkSize, 10f, ChunkSize));
            }

            // Active group centers
            Gizmos.color = Color.cyan;
            foreach (var g in wildlifeManager.ActiveGroups)
                Gizmos.DrawSphere(g.centerPosition, g.groupRadius * 0.5f);
        }

        private static void DrawCircleGizmo(Vector3 center, float radius)
        {
            const int segments = 32;
            float step = Mathf.PI * 2f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step, a1 = (i + 1) * step;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius),
                    center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius));
            }
        }

        #endregion

        #region GUI Styles

        private void InitStyles()
        {
            if (_stylesInitialised) return;
            _stylesInitialised = true;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.75f)) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal   = { textColor = Color.white }
            };

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontSize   = 13,
                fontStyle  = FontStyle.Bold,
                normal     = { textColor = new Color(0.4f, 1f, 0.4f) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        #endregion
    }
#else
    /// <summary>Stub — WildlifeDebugOverlay is only active in Editor / development builds.</summary>
    public class WildlifeDebugOverlay : UnityEngine.MonoBehaviour { }
#endif
}
