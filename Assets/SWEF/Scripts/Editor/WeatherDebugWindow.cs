#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor window for testing and debugging the SWEF Weather System.
    ///
    /// <para>Open via menu <b>SWEF → Weather Debug</b>.</para>
    ///
    /// <para>Features:</para>
    /// <list type="bullet">
    ///   <item>Force any <see cref="SWEF.Weather.WeatherCondition"/> at runtime.</item>
    ///   <item>Manual transition-progress slider.</item>
    ///   <item>Live readout of all current <see cref="SWEF.Weather.WeatherData"/> fields.</item>
    ///   <item>Quick scenario buttons (clear day, heavy storm, blizzard, dense fog, sandstorm).</item>
    /// </list>
    /// </summary>
    public class WeatherDebugWindow : EditorWindow
    {
        // ── Menu item ─────────────────────────────────────────────────────────────

        /// <summary>Opens the Weather Debug editor window.</summary>
        [MenuItem("SWEF/Weather Debug")]
        public static void ShowWindow()
        {
            var win = GetWindow<WeatherDebugWindow>("Weather Debug");
            win.minSize = new Vector2(340f, 520f);
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private SWEF.Weather.WeatherCondition _selectedCondition = SWEF.Weather.WeatherCondition.Clear;
        private float _transitionOverride = 1f;
        private Vector2 _scroll;

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWEF — Weather Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the Weather Debug window.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawCurrentWeatherReadout();
            EditorGUILayout.Space(8f);
            DrawForceConditionSection();
            EditorGUILayout.Space(8f);
            DrawQuickScenarios();
            EditorGUILayout.Space(8f);
            DrawTransitionSlider();

            EditorGUILayout.EndScrollView();
        }

        // ── Sections ──────────────────────────────────────────────────────────────

        private void DrawCurrentWeatherReadout()
        {
            EditorGUILayout.LabelField("Current Weather (Active)", EditorStyles.boldLabel);

            var sm = SWEF.Weather.WeatherStateManager.Instance;
            if (sm == null)
            {
                EditorGUILayout.HelpBox("WeatherStateManager not found in scene.", MessageType.Warning);
                return;
            }

            var d = sm.ActiveWeather;
            if (d == null)
            {
                EditorGUILayout.LabelField("  No data yet.");
                return;
            }

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Condition",    d.condition.ToString());
            EditorGUILayout.LabelField("Temperature",  $"{d.temperatureCelsius:F1} °C");
            EditorGUILayout.LabelField("Humidity",     $"{d.humidity * 100f:F0} %");
            EditorGUILayout.LabelField("Wind",         $"{d.windSpeedMs:F1} m/s  @  {d.windDirectionDeg:F0}°");
            EditorGUILayout.LabelField("Visibility",   d.visibility >= 1000f
                                                          ? $"{d.visibility / 1000f:F1} km"
                                                          : $"{d.visibility:F0} m");
            EditorGUILayout.LabelField("Cloud Cover",  $"{d.cloudCoverage * 100f:F0} %");
            EditorGUILayout.LabelField("Precipitation",$"{d.precipitationIntensity * 100f:F0} %");
            EditorGUILayout.LabelField("Updated",      d.lastUpdated.ToString("HH:mm:ss") + " UTC");
            EditorGUILayout.LabelField("Altitude",     $"{sm.AltitudeMeters:F0} m");
            EditorGUILayout.LabelField("Transition",   $"{sm.TransitionProgress * 100f:F0} %");
        }

        private void DrawForceConditionSection()
        {
            EditorGUILayout.LabelField("Force Condition", EditorStyles.boldLabel);

            _selectedCondition = (SWEF.Weather.WeatherCondition)
                EditorGUILayout.EnumPopup("Condition", _selectedCondition);

            if (GUILayout.Button("Apply Condition"))
                ForceCondition(_selectedCondition);
        }

        private void DrawQuickScenarios()
        {
            EditorGUILayout.LabelField("Quick Scenarios", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("☀️ Clear Day"))
                ForceScenario(SWEF.Weather.WeatherCondition.Clear, 20f, 0.3f, 2f, 10000f, 0f, 0f);

            if (GUILayout.Button("⛈️ Heavy Storm"))
                ForceScenario(SWEF.Weather.WeatherCondition.Thunderstorm, 12f, 0.9f, 18f, 1500f, 0.95f, 0.9f);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🌨️ Blizzard"))
                ForceScenario(SWEF.Weather.WeatherCondition.HeavySnow, -12f, 0.8f, 22f, 500f, 1f, 0.85f);

            if (GUILayout.Button("🌫️ Dense Fog"))
                ForceScenario(SWEF.Weather.WeatherCondition.DenseFog, 8f, 0.95f, 1f, 120f, 0.9f, 0f);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🏜️ Sandstorm"))
                ForceScenario(SWEF.Weather.WeatherCondition.Sandstorm, 35f, 0.15f, 28f, 300f, 0.5f, 0.6f);

            if (GUILayout.Button("🧊 Hailstorm"))
                ForceScenario(SWEF.Weather.WeatherCondition.Hail, 4f, 0.7f, 15f, 2000f, 0.85f, 0.8f);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTransitionSlider()
        {
            EditorGUILayout.LabelField("Transition Override", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Manually scrub the transition progress between CurrentWeather and TargetWeather.",
                MessageType.None);

            float prev = _transitionOverride;
            _transitionOverride = EditorGUILayout.Slider("Progress", _transitionOverride, 0f, 1f);

            // Reflect slider into WeatherStateManager when changed
            if (!Mathf.Approximately(prev, _transitionOverride))
            {
                var sm = SWEF.Weather.WeatherStateManager.Instance;
                if (sm != null)
                {
                    // We expose TransitionProgress only as a property; use reflection in Editor context
                    typeof(SWEF.Weather.WeatherStateManager)
                        .GetProperty(nameof(SWEF.Weather.WeatherStateManager.TransitionProgress))
                        ?.GetSetMethod(nonPublic: true)
                        ?.Invoke(sm, new object[] { _transitionOverride });
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void ForceCondition(SWEF.Weather.WeatherCondition cond)
        {
            var sm = SWEF.Weather.WeatherStateManager.Instance;
            if (sm == null) { Debug.LogWarning("[SWEF][WeatherDebug] WeatherStateManager not found."); return; }

            var data = SWEF.Weather.WeatherData.CreateClear();
            data.condition = cond;
            sm.SetTargetWeather(data);
            Debug.Log($"[SWEF][WeatherDebug] Forced condition: {cond}");
        }

        private static void ForceScenario(
            SWEF.Weather.WeatherCondition cond,
            float temp, float humidity, float windSpeed,
            float visibility, float cloudCover, float precip)
        {
            var sm = SWEF.Weather.WeatherStateManager.Instance;
            if (sm == null) { Debug.LogWarning("[SWEF][WeatherDebug] WeatherStateManager not found."); return; }

            var data = new SWEF.Weather.WeatherData
            {
                condition              = cond,
                temperatureCelsius     = temp,
                humidity               = humidity,
                windSpeedMs            = windSpeed,
                windDirectionDeg       = UnityEngine.Random.Range(0f, 360f),
                visibility             = visibility,
                cloudCoverage          = cloudCover,
                precipitationIntensity = precip,
                lastUpdated            = DateTime.UtcNow
            };
            sm.SetTargetWeather(data);
            Debug.Log($"[SWEF][WeatherDebug] Applied scenario: {cond}");
        }

        // Refresh the window each editor frame so the readout stays live
        private void Update()
        {
            if (Application.isPlaying)
                Repaint();
        }
    }
}
#endif
