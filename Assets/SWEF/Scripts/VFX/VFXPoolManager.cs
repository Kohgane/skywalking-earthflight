// VFXPoolManager.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Singleton object pool that manages the lifecycle of all VFX instances in the scene.
    /// Survives scene transitions via <c>DontDestroyOnLoad</c>.
    ///
    /// <para>Each <see cref="VFXProfile"/> drives a per-type pool that can be pre-warmed at
    /// startup. Spawn requests are fulfilled via <see cref="Spawn"/>, which returns a
    /// <see cref="VFXInstanceHandle"/>. Reclaiming an instance is done through
    /// <see cref="Despawn"/> or automatically when the effect's lifetime expires.</para>
    ///
    /// <para>A global active-particle budget (default 50 000) is enforced. When the
    /// budget is exceeded or a pool is exhausted the <see cref="OnPoolExhausted"/> event
    /// is raised.</para>
    /// </summary>
    public sealed class VFXPoolManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────────

        /// <summary>Gets the active singleton instance, or <c>null</c> if not yet initialised.</summary>
        public static VFXPoolManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Profiles")]
        [Tooltip("All VFX profiles that will have pools pre-warmed at startup.")]
        [SerializeField] private List<VFXProfile> profiles = new List<VFXProfile>();

        [Header("Budget")]
        [Tooltip("Maximum total active particles across all pools (default 50 000).")]
        [SerializeField, Min(1000)] private int maxGlobalParticles = 50_000;

        [Tooltip("Interval in seconds between automatic expired-instance sweeps.")]
        [SerializeField, Min(0.1f)] private float sweepInterval = 1f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when a spawn request cannot be fulfilled because the per-type pool is
        /// exhausted (and the profile policy is <see cref="PoolExhaustPolicy.DropRequest"/>),
        /// or when the global particle budget would be exceeded.
        /// </summary>
        public event Action<VFXType> OnPoolExhausted;

        // ── Private State ─────────────────────────────────────────────────────────

        private readonly Dictionary<VFXType, Queue<PoolEntry>>   _free    = new Dictionary<VFXType, Queue<PoolEntry>>();
        private readonly Dictionary<int,     ActiveEntry>         _active  = new Dictionary<int, ActiveEntry>();
        private readonly Dictionary<VFXType, VFXProfile>          _profileMap = new Dictionary<VFXType, VFXProfile>();

        private int  _nextId   = 1;
        private int  _particleBudgetUsed;
        private float _nextSweepTime;

        // ── Inner Types ───────────────────────────────────────────────────────────

        private sealed class PoolEntry
        {
            public GameObject    GameObject;
            public ParticleSystem Ps;
        }

        private sealed class ActiveEntry
        {
            public VFXInstanceHandle Handle;
            public PoolEntry         Entry;
            public float             ExpireTime;   // Time.time when the instance expires; ≤ 0 = looping
            public int               ParticleCount;
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildProfileMap();
            WarmPools();
        }

        private void Update()
        {
            if (Time.time >= _nextSweepTime)
            {
                _nextSweepTime = Time.time + sweepInterval;
                SweepExpiredInstances();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns a VFX effect described by <paramref name="request"/>.
        /// Returns a <see cref="VFXInstanceHandle"/> that can be used to despawn the
        /// instance early. Returns <see cref="VFXInstanceHandle.Invalid"/> if the
        /// request was dropped due to pool exhaustion or budget constraints.
        /// </summary>
        /// <param name="request">Complete spawn request including type, position, and optional overrides.</param>
        public VFXInstanceHandle Spawn(VFXSpawnRequest request)
        {
            if (!_profileMap.TryGetValue(request.type, out VFXProfile profile))
            {
                Debug.LogWarning($"[VFXPoolManager] No profile registered for {request.type}.");
                return VFXInstanceHandle.Invalid;
            }

            // Global particle budget check
            int estimatedCount = EstimateParticleCount(profile);
            if (_particleBudgetUsed + estimatedCount > maxGlobalParticles)
            {
                OnPoolExhausted?.Invoke(request.type);
                return VFXInstanceHandle.Invalid;
            }

            // Per-type active cap
            if (profile.MaxInstances > 0 && CountActive(request.type) >= profile.MaxInstances)
            {
                return HandleExhausted(request.type, profile, request);
            }

            PoolEntry entry = AcquireEntry(request.type, profile);
            if (entry == null) return VFXInstanceHandle.Invalid;

            return ActivateEntry(entry, request, profile);
        }

        /// <summary>
        /// Manually reclaims the VFX instance identified by <paramref name="handle"/>.
        /// Safe to call with an invalid handle.
        /// </summary>
        /// <param name="handle">Handle returned by <see cref="Spawn"/>.</param>
        public void Despawn(VFXInstanceHandle handle)
        {
            if (!handle.IsValid) return;
            if (_active.TryGetValue(handle.Id, out ActiveEntry active))
                RecycleEntry(handle.Id, active);
        }

        /// <summary>Returns the total number of currently active VFX instances.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>Returns the number of active instances of a specific VFX type.</summary>
        /// <param name="type">Type to query.</param>
        public int CountActive(VFXType type)
        {
            int count = 0;
            foreach (var kv in _active)
                if (kv.Value.Handle.Type == type) count++;
            return count;
        }

        /// <summary>Returns the current global active-particle estimate.</summary>
        public int ParticleBudgetUsed => _particleBudgetUsed;

        /// <summary>Maximum allowed total particles across all active effects.</summary>
        public int MaxGlobalParticles => maxGlobalParticles;

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private void BuildProfileMap()
        {
            foreach (VFXProfile p in profiles)
                if (p != null) _profileMap[p.vfxType] = p;
        }

        private void WarmPools()
        {
            foreach (VFXProfile p in profiles)
            {
                if (p == null || p.prefab == null) continue;
                EnsureQueue(p.vfxType);
                for (int i = 0; i < p.WarmCount; i++)
                    _free[p.vfxType].Enqueue(CreateEntry(p));
            }
        }

        private void EnsureQueue(VFXType type)
        {
            if (!_free.ContainsKey(type))
                _free[type] = new Queue<PoolEntry>();
        }

        private PoolEntry CreateEntry(VFXProfile profile)
        {
            if (profile.prefab == null) return null;
            GameObject go = Instantiate(profile.prefab, transform);
            go.SetActive(false);
            go.layer = profile.RenderLayer;
            return new PoolEntry { GameObject = go, Ps = go.GetComponentInChildren<ParticleSystem>() };
        }

        private PoolEntry AcquireEntry(VFXType type, VFXProfile profile)
        {
            EnsureQueue(type);
            if (_free[type].Count > 0)
                return _free[type].Dequeue();

            if (profile.prefab != null)
                return CreateEntry(profile);

            return null;
        }

        /// <summary>
        /// Configures <paramref name="entry"/> according to <paramref name="request"/> and registers
        /// it as an active instance. Does not perform any cap or budget checks — callers are
        /// responsible for those validations before invoking this method.
        /// </summary>
        private VFXInstanceHandle ActivateEntry(PoolEntry entry, VFXSpawnRequest request, VFXProfile profile)
        {
            int estimatedCount = EstimateParticleCount(profile);

            entry.GameObject.transform.position   = request.position;
            entry.GameObject.transform.rotation   = request.rotation;
            entry.GameObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, request.scale);
            if (request.parent != null)
                entry.GameObject.transform.SetParent(request.parent, worldPositionStays: true);
            else
                entry.GameObject.transform.SetParent(transform, worldPositionStays: false);

            entry.GameObject.SetActive(true);
            entry.Ps?.Play(withChildren: true);

            float duration = request.durationOverride > 0f ? request.durationOverride : profile.Lifetime;
            float expiry   = profile.loop ? -1f : Time.time + duration;

            var handle = new VFXInstanceHandle(_nextId++, request.type, request.position, Time.time);
            _active[handle.Id] = new ActiveEntry
            {
                Handle        = handle,
                Entry         = entry,
                ExpireTime    = expiry,
                ParticleCount = estimatedCount
            };
            _particleBudgetUsed += estimatedCount;
            return handle;
        }

        private VFXInstanceHandle HandleExhausted(VFXType type, VFXProfile profile, VFXSpawnRequest request)
        {
            switch (profile.exhaustPolicy)
            {
                case PoolExhaustPolicy.RecycleOldest:
                {
                    int oldestId = FindOldestActive(type);
                    if (oldestId != 0 && _active.TryGetValue(oldestId, out ActiveEntry old))
                    {
                        RecycleEntry(oldestId, old);
                        // After recycling, attempt a single direct acquisition — do not recurse through Spawn
                        PoolEntry entry = AcquireEntry(type, profile);
                        if (entry == null) return VFXInstanceHandle.Invalid;
                        return ActivateEntry(entry, request, profile);
                    }
                    OnPoolExhausted?.Invoke(type);
                    return VFXInstanceHandle.Invalid;
                }
                case PoolExhaustPolicy.Grow:
                {
                    // Directly create a new entry and activate it, bypassing the instance cap
                    PoolEntry entry = CreateEntry(profile);
                    if (entry == null) return VFXInstanceHandle.Invalid;
                    return ActivateEntry(entry, request, profile);
                }
                default:
                    OnPoolExhausted?.Invoke(type);
                    return VFXInstanceHandle.Invalid;
            }
        }

        private int FindOldestActive(VFXType type)
        {
            int   oldestId   = 0;
            float oldestTime = float.MaxValue;
            foreach (var kv in _active)
            {
                if (kv.Value.Handle.Type == type && kv.Value.Handle.SpawnTime < oldestTime)
                {
                    oldestTime = kv.Value.Handle.SpawnTime;
                    oldestId   = kv.Key;
                }
            }
            return oldestId;
        }

        private void RecycleEntry(int id, ActiveEntry active)
        {
            _particleBudgetUsed = Mathf.Max(0, _particleBudgetUsed - active.ParticleCount);
            active.Entry.Ps?.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
            active.Entry.GameObject.SetActive(false);
            active.Entry.GameObject.transform.SetParent(transform, worldPositionStays: false);
            _active.Remove(id);
            EnsureQueue(active.Handle.Type);
            _free[active.Handle.Type].Enqueue(active.Entry);
        }

        private void SweepExpiredInstances()
        {
            float now = Time.time;
            var toReclaim = new List<int>();
            foreach (var kv in _active)
                if (kv.Value.ExpireTime > 0f && now >= kv.Value.ExpireTime)
                    toReclaim.Add(kv.Key);

            foreach (int id in toReclaim)
                if (_active.TryGetValue(id, out ActiveEntry ae))
                    RecycleEntry(id, ae);
        }

        private static int EstimateParticleCount(VFXProfile profile)
        {
            // Rough estimate: prefab's max particle count, or 100 if not available
            return 100;
        }

#if UNITY_EDITOR
        [ContextMenu("Log Pool Stats")]
        private void EditorLogStats()
        {
            Debug.Log($"[VFXPoolManager] Active: {_active.Count} | Budget used: {_particleBudgetUsed}/{maxGlobalParticles}");
            foreach (var kv in _free)
                Debug.Log($"  Free pool [{kv.Key}]: {kv.Value.Count} entries");
        }
#endif
    }
}
