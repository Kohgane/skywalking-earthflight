using System;
using System.Reflection;
using UnityEngine;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton quality preset manager.
    /// Exposes four presets (Low / Medium / High / Ultra) that tune Unity's
    /// built-in quality settings alongside Cesium tile LOD, shadow distance,
    /// and target frame rate. The chosen preset is persisted in PlayerPrefs.
    /// </summary>
    public class QualityPresetManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static QualityPresetManager Instance { get; private set; }

        // ── Quality levels ───────────────────────────────────────────────────────
        /// <summary>Available quality presets.</summary>
        public enum QualityLevel { Low, Medium, High, Ultra }

        private const string PrefsKey = "SWEF_QualityLevel";

        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Cesium Tileset (auto-found if null)")]
        [SerializeField] private Component tileset;

        // ── Events / Properties ──────────────────────────────────────────────────
        /// <summary>Fired whenever the quality preset changes.</summary>
        public event Action<QualityLevel> OnQualityChanged;

        /// <summary>The currently active quality preset.</summary>
        public QualityLevel CurrentQuality { get; private set; } = QualityLevel.Medium;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoFindTileset();
            LoadSavedQuality();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Applies the given quality preset and persists the choice.
        /// Adjusts Unity quality level, Cesium SSE, shadow settings, and target frame rate.
        /// </summary>
        public void SetQuality(QualityLevel level)
        {
            CurrentQuality = level;
            PlayerPrefs.SetInt(PrefsKey, (int)level);
            PlayerPrefs.Save();

            ApplyQuality(level);
            OnQualityChanged?.Invoke(level);
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void AutoFindTileset()
        {
            if (tileset != null) return;

            // Try to find a Cesium3DTileset via reflection to avoid a hard
            // compile-time dependency on the Cesium for Unity package.
            foreach (var comp in FindObjectsByType<Component>(FindObjectsSortMode.None))
            {
                if (comp.GetType().Name == "Cesium3DTileset")
                {
                    tileset = comp;
                    break;
                }
            }
        }

        private void LoadSavedQuality()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                int saved = PlayerPrefs.GetInt(PrefsKey);
                SetQuality((QualityLevel)saved);
            }
            else
            {
                SetQuality(QualityLevel.Medium);
            }
        }

        private void ApplyQuality(QualityLevel level)
        {
            switch (level)
            {
                case QualityLevel.Low:
                    QualitySettings.SetQualityLevel(0, true);
                    SetTilesetSSE(24f);
                    QualitySettings.shadowDistance   = 0f;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    Application.targetFrameRate      = 30;
                    break;

                case QualityLevel.Medium:
                    QualitySettings.SetQualityLevel(1, true);
                    SetTilesetSSE(12f);
                    QualitySettings.shadowDistance   = 50f;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    Application.targetFrameRate      = 30;
                    break;

                case QualityLevel.High:
                    QualitySettings.SetQualityLevel(2, true);
                    SetTilesetSSE(8f);
                    QualitySettings.shadowDistance   = 100f;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    Application.targetFrameRate      = 60;
                    break;

                case QualityLevel.Ultra:
                    QualitySettings.SetQualityLevel(3, true);
                    SetTilesetSSE(2f);
                    QualitySettings.shadowDistance   = 200f;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    Application.targetFrameRate      = 60;
                    break;
            }

            Debug.Log($"[SWEF] QualityPresetManager: quality set to {level}");
        }

        /// <summary>
        /// Sets <c>maximumScreenSpaceError</c> on the Cesium tileset component via
        /// reflection so there is no hard compile-time dependency on the package.
        /// </summary>
        private void SetTilesetSSE(float value)
        {
            if (tileset == null) return;

            Type t = tileset.GetType();

            PropertyInfo prop = t.GetProperty("maximumScreenSpaceError",
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(tileset, value);
                return;
            }

            FieldInfo field = t.GetField("maximumScreenSpaceError",
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(tileset, value);
                return;
            }

            Debug.LogWarning("[SWEF] QualityPresetManager: 'maximumScreenSpaceError' not found on tileset component.");
        }
    }
}
