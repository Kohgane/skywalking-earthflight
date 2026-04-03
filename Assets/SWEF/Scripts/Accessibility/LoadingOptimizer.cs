// LoadingOptimizer.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// Singleton MonoBehaviour that manages async scene/asset loading,
    /// tile prefetch scheduling, and per-asset priority queuing so the
    /// game remains responsive during loading.
    /// </summary>
    public class LoadingOptimizer : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static LoadingOptimizer Instance { get; private set; }

        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("Settings")]
        [SerializeField, Tooltip("Maximum concurrent async operations.")]
        private int maxConcurrentLoads = 3;

        [SerializeField, Tooltip("Seconds to wait between prefetch batches to avoid hitches.")]
        private float prefetchThrottleSeconds = 0.1f;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private readonly Queue<Func<IEnumerator>> _prefetchQueue = new Queue<Func<IEnumerator>>();
        private int   _activeLoads;
        private bool  _prefetching;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when an async load operation begins; parameter is the asset path.</summary>
        public event Action<string> OnLoadStarted;

        /// <summary>Fired when an async load operation completes; parameter is the asset path.</summary>
        public event Action<string> OnLoadComplete;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads a <see cref="ResourceRequest"/> asynchronously, respecting the
        /// concurrent-load limit, and invokes <paramref name="onComplete"/> when done.
        /// </summary>
        /// <param name="resourcePath">Path passed to <c>Resources.LoadAsync</c>.</param>
        /// <param name="onComplete">Callback invoked with the loaded <see cref="UnityEngine.Object"/>.</param>
        public void LoadAsync(string resourcePath, Action<UnityEngine.Object> onComplete = null)
        {
            StartCoroutine(LoadAsyncRoutine(resourcePath, onComplete));
        }

        /// <summary>
        /// Enqueues a prefetch coroutine function to be run in the background
        /// with throttling to avoid frame hitches.
        /// </summary>
        public void EnqueuePrefetch(Func<IEnumerator> prefetchRoutine)
        {
            if (prefetchRoutine == null) return;
            _prefetchQueue.Enqueue(prefetchRoutine);
            if (!_prefetching)
                StartCoroutine(DrainPrefetchQueue());
        }

        // ── Internals ─────────────────────────────────────────────────────────────

        private IEnumerator LoadAsyncRoutine(string path, Action<UnityEngine.Object> onComplete)
        {
            // Wait for a slot
            while (_activeLoads >= maxConcurrentLoads)
                yield return null;

            _activeLoads++;
            OnLoadStarted?.Invoke(path);

            ResourceRequest req = Resources.LoadAsync(path);
            yield return req;

            _activeLoads--;
            OnLoadComplete?.Invoke(path);
            onComplete?.Invoke(req.asset);
        }

        private IEnumerator DrainPrefetchQueue()
        {
            _prefetching = true;
            while (_prefetchQueue.Count > 0)
            {
                var routine = _prefetchQueue.Dequeue();
                yield return StartCoroutine(routine());
                yield return new WaitForSeconds(prefetchThrottleSeconds);
            }
            _prefetching = false;
        }
    }
}
