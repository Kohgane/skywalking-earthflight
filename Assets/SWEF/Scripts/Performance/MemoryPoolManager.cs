using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Performance
{
    /// <summary>
    /// Generic object pooling system to reduce GC pressure.
    /// Use <see cref="ObjectPool{T}"/> to create pools, then register them with
    /// <see cref="MemoryPoolManager"/> for centralised monitoring.
    /// </summary>
    public class MemoryPoolManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static MemoryPoolManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever a pool is registered, cleared, or shrunk.</summary>
        public event Action OnPoolsChanged;

        // ── Internal registry ────────────────────────────────────────────────────
        private readonly Dictionary<string, IPool> _pools = new Dictionary<string, IPool>();

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

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>Registers an <see cref="ObjectPool{T}"/> for monitoring.</summary>
        public void RegisterPool<T>(string name, ObjectPool<T> pool) where T : Component
        {
            _pools[name] = pool;
            OnPoolsChanged?.Invoke();
        }

        /// <summary>
        /// Returns a dictionary of pool name → (active, pooled, total) tuples
        /// suitable for displaying in a debug UI.
        /// </summary>
        public Dictionary<string, (int active, int pooled, int total)> GetPoolStats()
        {
            var stats = new Dictionary<string, (int, int, int)>(_pools.Count);
            foreach (var kv in _pools)
                stats[kv.Key] = (kv.Value.ActiveCount, kv.Value.PooledCount, kv.Value.TotalCount);
            return stats;
        }

        /// <summary>
        /// Calls <c>Shrink</c> on every registered pool, releasing excess objects
        /// beyond their initial sizes.
        /// </summary>
        public void ShrinkAll()
        {
            foreach (var pool in _pools.Values)
                pool.Shrink();
            OnPoolsChanged?.Invoke();
        }
    }

    // ── Internal interface for non-generic tracking ──────────────────────────────
    internal interface IPool
    {
        int ActiveCount { get; }
        int PooledCount { get; }
        int TotalCount  { get; }
        void Shrink();
    }

    // ── Generic pool ─────────────────────────────────────────────────────────────

    /// <summary>
    /// A simple Component object pool that pre-warms instances, reuses inactive
    /// objects, and enforces a maximum size cap.
    /// </summary>
    /// <typeparam name="T">A <see cref="Component"/> type to pool.</typeparam>
    public class ObjectPool<T> : IPool where T : Component
    {
        private readonly T         _prefab;
        private readonly int       _initialSize;
        private readonly int       _maxSize;
        private readonly Transform _parent;

        private readonly List<T> _inactive = new List<T>();
        private readonly List<T> _active   = new List<T>();

        /// <summary>Number of currently active (checked-out) instances.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Number of instances currently sitting idle in the pool.</summary>
        public int PooledCount => _inactive.Count;

        /// <summary>Total number of instances that have been created.</summary>
        public int TotalCount  => _active.Count + _inactive.Count;

        /// <summary>
        /// Creates a pool.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate when the pool needs a new instance.</param>
        /// <param name="initialSize">Number of objects to pre-warm on construction.</param>
        /// <param name="maxSize">Hard cap on total instances that may exist at once.</param>
        /// <param name="parent">Optional parent transform for pooled instances.</param>
        public ObjectPool(T prefab, int initialSize, int maxSize, Transform parent = null)
        {
            _prefab      = prefab;
            _initialSize = initialSize;
            _maxSize     = maxSize;
            _parent      = parent;

            PreWarm(initialSize);
        }

        /// <summary>
        /// Pre-instantiates up to <paramref name="count"/> objects and stores them
        /// as inactive.
        /// </summary>
        public void PreWarm(int count)
        {
            int toCreate = Mathf.Min(count, _maxSize - TotalCount);
            for (int i = 0; i < toCreate; i++)
            {
                T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
                obj.gameObject.SetActive(false);
                _inactive.Add(obj);
            }
        }

        /// <summary>
        /// Returns an inactive pooled object, activating it. If the pool is empty
        /// and the total count is below <c>maxSize</c>, a new instance is created.
        /// Returns <c>null</c> when the cap is reached.
        /// </summary>
        public T Get()
        {
            T obj;
            if (_inactive.Count > 0)
            {
                int last = _inactive.Count - 1;
                obj = _inactive[last];
                _inactive.RemoveAt(last);
            }
            else if (TotalCount < _maxSize)
            {
                obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            }
            else
            {
                Debug.LogWarning($"[SWEF] ObjectPool<{typeof(T).Name}>: pool at max capacity ({_maxSize}).");
                return null;
            }

            obj.gameObject.SetActive(true);
            _active.Add(obj);
            return obj;
        }

        /// <summary>
        /// Returns an active object back to the pool, deactivating it.
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            obj.gameObject.SetActive(false);
            _active.Remove(obj);
            _inactive.Add(obj);

            if (_parent != null)
                obj.transform.SetParent(_parent, false);
        }

        /// <summary>
        /// Releases pooled (inactive) objects beyond the initial size, destroying them.
        /// </summary>
        public void Shrink()
        {
            while (_inactive.Count > _initialSize)
            {
                int last = _inactive.Count - 1;
                T   obj  = _inactive[last];
                _inactive.RemoveAt(last);
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        /// <summary>Destroys all pooled (inactive) instances.</summary>
        public void Clear()
        {
            foreach (T obj in _inactive)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
            _inactive.Clear();
        }
    }
}
