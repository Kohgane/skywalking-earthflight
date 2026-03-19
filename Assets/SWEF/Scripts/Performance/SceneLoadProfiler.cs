using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.Performance
{
    /// <summary>
    /// Profiles scene transitions by hooking into <see cref="SceneManager"/> events.
    /// Records timing for unload, load, and activation phases using
    /// <see cref="Time.realtimeSinceStartup"/>.
    /// </summary>
    public class SceneLoadProfiler : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static SceneLoadProfiler Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired after each scene is fully loaded and activated.</summary>
        public event Action<SceneLoadEvent> OnSceneLoadComplete;

        // ── Public state ─────────────────────────────────────────────────────────
        /// <summary>All recorded scene-load events, oldest first.</summary>
        public List<SceneLoadEvent> LoadHistory { get; } = new List<SceneLoadEvent>();

        // ── Internal timing ──────────────────────────────────────────────────────
        private string _pendingSceneName;
        private float  _loadStartTime;
        private float  _unloadEndTime;
        private int    _lastSceneObjectCount;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded   += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded   -= OnSceneLoaded;
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Marks the start of a scene transition (call before loading a new scene).
        /// </summary>
        public void BeginSceneLoad(string targetSceneName)
        {
            _pendingSceneName = targetSceneName;
            _loadStartTime    = Time.realtimeSinceStartup;
        }

        /// <summary>Average load time across all recorded scene loads, in milliseconds.</summary>
        public float GetAverageLoadTime()
        {
            if (LoadHistory.Count == 0) return 0f;
            float sum = 0f;
            foreach (var e in LoadHistory)
                sum += e.totalLoadTimeMs;
            return sum / LoadHistory.Count;
        }

        // ── Scene callbacks ───────────────────────────────────────────────────────
        private void OnSceneUnloaded(Scene scene)
        {
            _unloadEndTime = Time.realtimeSinceStartup;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            float now      = Time.realtimeSinceStartup;
            float total    = now - _loadStartTime;
            float unloadMs = (_loadStartTime > 0f && _unloadEndTime >= _loadStartTime)
                ? (_unloadEndTime - _loadStartTime) * 1000f
                : 0f;
            float activateMs = 0f;
            float loadMs     = total * 1000f - unloadMs - activateMs;

            int objectCount = scene.rootCount;

            var ev = new SceneLoadEvent
            {
                sceneName        = scene.name,
                totalLoadTimeMs  = total * 1000f,
                unloadPreviousMs = unloadMs,
                loadNewMs        = loadMs,
                activateMs       = activateMs,
                objectsLoaded    = objectCount,
                timestamp        = DateTime.Now,
            };

            LoadHistory.Add(ev);
            _loadStartTime = 0f;

            Debug.Log($"[SWEF] SceneLoadProfiler: '{scene.name}' loaded in {total * 1000f:F1} ms ({objectCount} root objects)");
            OnSceneLoadComplete?.Invoke(ev);
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>Record of a single scene-load event.</summary>
    [Serializable]
    public struct SceneLoadEvent
    {
        public string   sceneName;
        public float    totalLoadTimeMs;
        public float    unloadPreviousMs;
        public float    loadNewMs;
        public float    activateMs;
        public int      objectsLoaded;
        public DateTime timestamp;
    }
}
