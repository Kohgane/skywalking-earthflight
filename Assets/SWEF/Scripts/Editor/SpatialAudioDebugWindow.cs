#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SWEF.Audio;

namespace SWEF.Editor
{
    /// <summary>
    /// Custom EditorWindow for debugging the Phase 28 Spatial Audio Engine.
    /// Open via <b>SWEF → Spatial Audio Debug</b>.
    /// </summary>
    public class SpatialAudioDebugWindow : EditorWindow
    {
        // ── Menu ─────────────────────────────────────────────────────────────────
        [MenuItem("SWEF/Spatial Audio Debug")]
        public static void ShowWindow() => GetWindow<SpatialAudioDebugWindow>("SWEF Spatial Audio Debug");

        // ── State ────────────────────────────────────────────────────────────────
        private Vector2 _scroll;
        private bool    _showSoundscapeLayers = true;
        private bool    _showMusicLayers      = true;
        private bool    _showWindStats        = true;

        // ── Cached scene refs ─────────────────────────────────────────────────────
        private SpatialAudioManager          _spatialMgr;
        private AltitudeSoundscapeController _soundscape;
        private DopplerEffectController      _doppler;
        private WindAudioGenerator           _wind;
        private SonicBoomController          _sonicBoom;
        private MusicLayerSystem             _music;

        private void OnEnable() => RefreshRefs();

        private void RefreshRefs()
        {
            _spatialMgr = FindFirstObjectByType<SpatialAudioManager>();
            _soundscape = FindFirstObjectByType<AltitudeSoundscapeController>();
            _doppler    = FindFirstObjectByType<DopplerEffectController>();
            _wind       = FindFirstObjectByType<WindAudioGenerator>();
            _sonicBoom  = FindFirstObjectByType<SonicBoomController>();
            _music      = FindFirstObjectByType<MusicLayerSystem>();
        }

        // ── GUI ───────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTitle();
            EditorGUILayout.Space(4);
            DrawSourceStats();
            EditorGUILayout.Space(4);
            DrawSoundscapeLayers();
            EditorGUILayout.Space(4);
            DrawDopplerStats();
            EditorGUILayout.Space(4);
            DrawWindStats();
            EditorGUILayout.Space(4);
            DrawMachDisplay();
            EditorGUILayout.Space(4);
            DrawMusicLayers();
            EditorGUILayout.Space(8);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("SWEF — Spatial Audio Debug", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Phase 28: Spatial Audio Engine & 3D Sound System",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            if (GUILayout.Button("Refresh Scene References", GUILayout.Height(22)))
                RefreshRefs();
        }

        private void DrawSourceStats()
        {
            EditorGUILayout.LabelField("Audio Source Pool", EditorStyles.boldLabel);
            if (_spatialMgr == null)
            {
                EditorGUILayout.HelpBox("SpatialAudioManager not found in scene.", MessageType.Info);
                return;
            }

            int total  = _spatialMgr.GetComponentsInChildren<AudioSource>(true).Length;
            int active = 0;
            foreach (var src in _spatialMgr.GetComponentsInChildren<AudioSource>(true))
                if (src.isPlaying) active++;

            EditorGUILayout.LabelField($"Total pooled sources: {total}");
            EditorGUILayout.LabelField($"Active (playing):     {active}");
        }

        private void DrawSoundscapeLayers()
        {
            _showSoundscapeLayers = EditorGUILayout.Foldout(_showSoundscapeLayers, "Soundscape Layers");
            if (!_showSoundscapeLayers) return;

            if (_soundscape == null)
            {
                EditorGUILayout.HelpBox("AltitudeSoundscapeController not found.", MessageType.Info);
                return;
            }

            string[] layerNames = { "City (0–500m)", "Wind (500–5k)", "High Wind (5–20k)",
                                    "Thin Atm (20–80k)", "Near-Space (80–120k)", "Space (120k+)" };
            for (int i = 0; i < layerNames.Length; i++)
            {
                float vol = _soundscape.GetLayerVolume(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(layerNames[i], GUILayout.Width(180));
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(16));
                EditorGUI.DrawRect(new Rect(r.x, r.y + 2, r.width * vol, r.height - 4), Color.cyan);
                EditorGUI.DrawRect(new Rect(r.x + r.width * vol, r.y + 2,
                    r.width * (1f - vol), r.height - 4), new Color(0.2f, 0.2f, 0.2f));
                EditorGUILayout.LabelField($"{vol:P0}", GUILayout.Width(45));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDopplerStats()
        {
            EditorGUILayout.LabelField("Doppler Effect", EditorStyles.boldLabel);
            if (_doppler == null)
            {
                EditorGUILayout.HelpBox("DopplerEffectController not found.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField($"Enabled:   {_doppler.IsEnabled}");
            EditorGUILayout.LabelField($"Intensity: {_doppler.DopplerIntensity:F2}");
        }

        private void DrawWindStats()
        {
            _showWindStats = EditorGUILayout.Foldout(_showWindStats, "Wind Generator");
            if (!_showWindStats) return;

            if (_wind == null)
            {
                EditorGUILayout.HelpBox("WindAudioGenerator not found.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField($"Volume:       {_wind.CurrentVolume:F3}");
            EditorGUILayout.LabelField($"Cutoff (Hz):  {_wind.CurrentCutoffFreq:F0}");
            EditorGUILayout.LabelField($"Turbulence:   {_wind.TurbulenceLevel:F3}");
        }

        private void DrawMachDisplay()
        {
            EditorGUILayout.LabelField("Mach Number", EditorStyles.boldLabel);
            if (_sonicBoom == null)
            {
                EditorGUILayout.HelpBox("SonicBoomController not found.", MessageType.Info);
                return;
            }
            float mach = _sonicBoom.CurrentMach;
            Color color = mach >= 1f ? Color.red : (mach >= 0.8f ? Color.yellow : Color.green);
            var prev = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"Mach {mach:F3}", EditorStyles.boldLabel);
            GUI.color = prev;
        }

        private void DrawMusicLayers()
        {
            _showMusicLayers = EditorGUILayout.Foldout(_showMusicLayers, "Music Layers");
            if (!_showMusicLayers) return;

            if (_music == null)
            {
                EditorGUILayout.HelpBox("MusicLayerSystem not found.", MessageType.Info);
                return;
            }

            var layerTypes = (MusicLayerSystem.MusicLayerType[])
                System.Enum.GetValues(typeof(MusicLayerSystem.MusicLayerType));

            foreach (var lt in layerTypes)
            {
                float vol = _music.GetLayerCurrentVolume(lt);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(lt.ToString(), GUILayout.Width(80));
                float newVol = EditorGUILayout.Slider(vol, 0f, 1f);
                if (!Mathf.Approximately(newVol, vol))
                    _music.SetLayerTargetVolume(lt, newVol);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Play Teleport Stinger"))
                _music?.PlayTeleportStinger();

            EditorGUILayout.HelpBox(
                "Click in the Scene view to test spatial sounds (coming soon).",
                MessageType.None);
        }

        // Repaint every half-second in play mode
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }
    }
}
#endif
