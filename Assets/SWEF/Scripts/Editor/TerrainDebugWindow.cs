#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SWEF.Terrain;
using SWEF.LOD;

namespace SWEF.Editor
{
    /// <summary>
    /// Custom EditorWindow for inspecting and tuning the Phase 27 terrain system.
    /// Open via <b>SWEF → Terrain Debug</b>.
    /// </summary>
    public class TerrainDebugWindow : EditorWindow
    {
        // ── Menu item ────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Terrain Debug")]
        public static void ShowWindow() => GetWindow<TerrainDebugWindow>("SWEF Terrain Debug");

        // ── State ────────────────────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool    _showLODSliders = true;
        private bool    _showChunkGrid  = true;

        // Cached LOD threshold values for the slider UI
        private float _lod0 = 500f;
        private float _lod1 = 2000f;
        private float _lod2 = 8000f;
        private float _lod3 = 20000f;

        // ── Cached scene references ───────────────────────────────────────────────
        private ProceduralTerrainGenerator _cachedGen;
        private LODManager                 _cachedLOD;
        private TerrainChunkPool           _cachedPool;
        private CesiumTerrainBridge        _cachedBridge;

        private void RefreshCachedRefs()
        {
            _cachedGen    = FindFirstObjectByType<ProceduralTerrainGenerator>();
            _cachedLOD    = FindFirstObjectByType<LODManager>();
            _cachedPool   = FindFirstObjectByType<TerrainChunkPool>();
            _cachedBridge = FindFirstObjectByType<CesiumTerrainBridge>();
        }

        // ── GUI ──────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTitle();
            DrawStats();
            EditorGUILayout.Space(4);
            DrawCesiumStatus();
            EditorGUILayout.Space(4);
            DrawLODThresholds();
            EditorGUILayout.Space(4);
            DrawChunkGrid();
            EditorGUILayout.Space(4);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("SWEF — Terrain & LOD Debug", titleStyle);
            EditorGUILayout.LabelField("Phase 27", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Separator();
        }

        private void DrawStats()
        {
            EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Active Chunks",   _cachedGen  != null ? _cachedGen.ActiveChunkCount.ToString()   : "—");
                EditorGUILayout.LabelField("Pending Chunks",  _cachedGen  != null ? _cachedGen.PendingChunkCount.ToString()  : "—");
                EditorGUILayout.LabelField("Pooled Chunks",   _cachedPool != null ? _cachedPool.AvailableCount.ToString()    : "—");
                EditorGUILayout.LabelField("Pool Total",      _cachedPool != null ? _cachedPool.TotalCreated.ToString()      : "—");
                EditorGUILayout.LabelField("Culled Chunks",   _cachedLOD  != null ? _cachedLOD.CulledChunks.ToString()       : "—");

                if (_cachedLOD != null)
                {
                    float memMB = _cachedLOD.MemoryEstimateBytes / (1024f * 1024f);
                    EditorGUILayout.LabelField("Est. Terrain Mem", $"{memMB:F1} MB");
                }
            }
        }

        private void DrawCesiumStatus()
        {
            EditorGUILayout.LabelField("Cesium Bridge", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                if (_cachedBridge == null)
                {
                    EditorGUILayout.HelpBox("CesiumTerrainBridge not found in scene.", MessageType.Info);
                    return;
                }

#if CESIUM_FOR_UNITY
                EditorGUILayout.LabelField("Cesium SDK", "Present ✓");
#else
                EditorGUILayout.LabelField("Cesium SDK", "Not found — procedural fallback active");
#endif
                EditorGUILayout.LabelField("Bridge GameObject", _cachedBridge.gameObject.name);
            }
        }

        private void DrawLODThresholds()
        {
            _showLODSliders = EditorGUILayout.Foldout(_showLODSliders, "LOD Distance Thresholds", true);
            if (!_showLODSliders) return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (_cachedLOD != null)
                {
                    float[] dist = _cachedLOD.LODDistances;
                    if (dist != null && dist.Length >= 4)
                    {
                        _lod0 = dist[0]; _lod1 = dist[1]; _lod2 = dist[2]; _lod3 = dist[3];
                    }
                }

                _lod0 = EditorGUILayout.Slider("Full → Half (m)",    _lod0, 100f,  5000f);
                _lod1 = EditorGUILayout.Slider("Half → Quarter (m)",  _lod1, 500f,  10000f);
                _lod2 = EditorGUILayout.Slider("Quarter → Min (m)",   _lod2, 1000f, 30000f);
                _lod3 = EditorGUILayout.Slider("Min → Culled (m)",    _lod3, 5000f, 50000f);

                if (_cachedLOD != null && GUILayout.Button("Apply Thresholds"))
                {
                    float[] dist = _cachedLOD.LODDistances;
                    if (dist != null && dist.Length >= 4)
                    {
                        dist[0] = _lod0; dist[1] = _lod1; dist[2] = _lod2; dist[3] = _lod3;
                    }
                    Debug.Log($"[SWEF] LOD thresholds updated: {_lod0}, {_lod1}, {_lod2}, {_lod3}");
                }
            }
        }

        private void DrawChunkGrid()
        {
            _showChunkGrid = EditorGUILayout.Foldout(_showChunkGrid, "Chunk LOD Grid", true);
            if (!_showChunkGrid) return;

            if (_cachedGen == null)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.HelpBox("ProceduralTerrainGenerator not found in scene.", MessageType.Info);
                return;
            }

            // Show coloured legend
            EditorGUILayout.BeginHorizontal();
            DrawColorSwatch(Color.green,   "Full");
            DrawColorSwatch(Color.yellow,  "Half");
            DrawColorSwatch(new Color(1f, 0.5f, 0f), "Quarter");
            DrawColorSwatch(Color.red,     "Minimal");
            DrawColorSwatch(Color.grey,    "Culled");
            EditorGUILayout.EndHorizontal();

            // Iterate terrain chunks in scene
            var chunks = FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None);
            if (chunks.Length == 0)
            {
                EditorGUILayout.LabelField("No active chunks.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField($"{chunks.Length} chunk(s) in scene:");
            foreach (var chunk in chunks)
            {
                Color lodColor = chunk.CurrentLOD switch
                {
                    TerrainLODLevel.Full    => Color.green,
                    TerrainLODLevel.Half    => Color.yellow,
                    TerrainLODLevel.Quarter => new Color(1f, 0.5f, 0f),
                    TerrainLODLevel.Minimal => Color.red,
                    _                       => Color.grey
                };
                GUI.color = lodColor;
                EditorGUILayout.LabelField(
                    $"  {chunk.ChunkCoord} — {chunk.CurrentLOD} — {chunk.DistanceToPlayer:F0} m",
                    EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                bool genActive = _cachedGen != null && _cachedGen.gameObject.activeSelf;

                if (GUILayout.Button(genActive ? "Disable Procedural Terrain" : "Enable Procedural Terrain"))
                {
                    if (_cachedGen != null)
                    {
                        _cachedGen.gameObject.SetActive(!genActive);
                        Debug.Log($"[SWEF] Procedural terrain: {(!genActive ? "ON" : "OFF")}");
                    }
                }

                if (GUILayout.Button("Force LOD Update"))
                {
                    _cachedLOD?.UpdateAllLODs();
                    Debug.Log("[SWEF] Forced LOD update.");
                }

                if (GUILayout.Button("Refresh (Repaint)"))
                {
                    RefreshCachedRefs();
                    Repaint();
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private void DrawColorSwatch(Color color, string label)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUILayout.Label("■", EditorStyles.miniLabel, GUILayout.Width(16));
            GUI.color = prev;
            GUILayout.Label(label, EditorStyles.miniLabel);
        }

        private void OnEnable()
        {
            RefreshCachedRefs();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Re-cache refs when entering or exiting play mode
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                RefreshCachedRefs();
        }

        private double _lastRepaintTime;
        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRepaintTime > 0.5)
            {
                _lastRepaintTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }
}
#endif
