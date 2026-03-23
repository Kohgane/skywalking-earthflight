// VFXLODController.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.VFX
{
    /// <summary>
    /// Per-instance LOD controller that transitions active VFX instances through four
    /// quality levels (<see cref="VFXLODLevel"/>) based on camera distance.
    ///
    /// <para>Hysteresis bands prevent rapid LOD thrashing: an instance must be outside
    /// both the upgrade <em>and</em> downgrade thresholds before a transition occurs.</para>
    ///
    /// <para>When <c>SWEF_PERFORMANCE_AVAILABLE</c> is defined the controller queries
    /// <c>PerformanceProfiler.Instance.QualityScale</c> to adaptively bias LOD distances.</para>
    /// </summary>
    public sealed class VFXLODController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Camera")]
        [Tooltip("Reference camera used for distance calculations. Falls back to Camera.main if null.")]
        [SerializeField] private Camera referenceCamera;

        [Header("Hysteresis")]
        [Tooltip("Extra distance (metres) beyond a threshold required before upgrading LOD (moving closer).")]
        [SerializeField, Min(0f)] private float upgradeHysteresis = 10f;

        [Tooltip("Extra distance (metres) beyond a threshold required before downgrading LOD (moving further).")]
        [SerializeField, Min(0f)] private float downgradeHysteresis = 20f;

        [Header("Evaluation")]
        [Tooltip("Interval in seconds between LOD evaluation passes.")]
        [SerializeField, Min(0.05f)] private float evaluationInterval = 0.1f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when an instance transitions to a new LOD level.
        /// Parameters: instance GameObject, old level, new level.
        /// </summary>
        public event Action<GameObject, VFXLODLevel, VFXLODLevel> OnLODChanged;

        // ── Private State ─────────────────────────────────────────────────────────

        private sealed class TrackedInstance
        {
            public GameObject    GameObject;
            public ParticleSystem[] Systems;
            public VFXProfile    Profile;
            public VFXLODLevel   CurrentLevel;
            public int[]         OriginalMaxParticles;
        }

        private readonly List<TrackedInstance> _tracked = new List<TrackedInstance>();
        private float _nextEvalTime;
        private Camera _cam;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _cam = referenceCamera != null ? referenceCamera : Camera.main;
        }

        private void Update()
        {
            if (Time.time < _nextEvalTime) return;
            _nextEvalTime = Time.time + evaluationInterval;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float qualityScale = GetQualityScale();
            Vector3 camPos = _cam.transform.position;

            foreach (TrackedInstance inst in _tracked)
            {
                if (inst.GameObject == null) continue;
                float dist = Vector3.Distance(inst.GameObject.transform.position, camPos);
                EvaluateLOD(inst, dist, qualityScale);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins tracking <paramref name="go"/> for LOD management using the supplied profile.
        /// </summary>
        /// <param name="go">VFX instance GameObject to track.</param>
        /// <param name="profile">VFX profile that contains LOD distance and scale data.</param>
        public void Track(GameObject go, VFXProfile profile)
        {
            if (go == null || profile == null) return;

            ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            int[] origMax = new int[systems.Length];
            for (int i = 0; i < systems.Length; i++)
                origMax[i] = systems[i].main.maxParticles;

            _tracked.Add(new TrackedInstance
            {
                GameObject          = go,
                Systems             = systems,
                Profile             = profile,
                CurrentLevel        = VFXLODLevel.Full,
                OriginalMaxParticles = origMax
            });
        }

        /// <summary>Stops tracking the given GameObject and removes it from the LOD evaluation list.</summary>
        /// <param name="go">VFX instance to stop tracking.</param>
        public void Untrack(GameObject go)
        {
            _tracked.RemoveAll(t => t.GameObject == go);
        }

        /// <summary>Returns the current LOD level of a tracked instance, or <see cref="VFXLODLevel.Full"/> if not tracked.</summary>
        /// <param name="go">Instance to query.</param>
        public VFXLODLevel GetCurrentLOD(GameObject go)
        {
            foreach (var inst in _tracked)
                if (inst.GameObject == go) return inst.CurrentLevel;
            return VFXLODLevel.Full;
        }

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private void EvaluateLOD(TrackedInstance inst, float distance, float qualityScale)
        {
            Vector3 thresholds = inst.Profile.lodDistances / qualityScale;

            VFXLODLevel desired = ComputeDesiredLevel(distance, thresholds);

            // Apply hysteresis
            if (desired < inst.CurrentLevel)
            {
                // Upgrading (closer) — require distance to be inside threshold minus hysteresis
                float upgradeThreshold = GetThreshold(thresholds, (int)inst.CurrentLevel - 1) - upgradeHysteresis;
                if (distance > upgradeThreshold) desired = inst.CurrentLevel;
            }
            else if (desired > inst.CurrentLevel)
            {
                // Downgrading (further) — require distance to be outside threshold plus hysteresis
                float downgradeThreshold = GetThreshold(thresholds, (int)inst.CurrentLevel) + downgradeHysteresis;
                if (distance < downgradeThreshold) desired = inst.CurrentLevel;
            }

            if (desired == inst.CurrentLevel) return;

            VFXLODLevel previous = inst.CurrentLevel;
            inst.CurrentLevel = desired;
            ApplyLOD(inst, desired);
            OnLODChanged?.Invoke(inst.GameObject, previous, desired);
        }

        private static VFXLODLevel ComputeDesiredLevel(float distance, Vector3 thresholds)
        {
            if (distance <= thresholds.x) return VFXLODLevel.Full;
            if (distance <= thresholds.y) return VFXLODLevel.Reduced;
            if (distance <= thresholds.z) return VFXLODLevel.Minimal;
            return VFXLODLevel.Billboard;
        }

        private static float GetThreshold(Vector3 thresholds, int level)
        {
            return level switch
            {
                0 => thresholds.x,
                1 => thresholds.y,
                2 => thresholds.z,
                _ => float.MaxValue
            };
        }

        private void ApplyLOD(TrackedInstance inst, VFXLODLevel level)
        {
            float scale = inst.Profile.GetParticleScale(level);
            bool  active = level != VFXLODLevel.Billboard;

            for (int i = 0; i < inst.Systems.Length; i++)
            {
                ParticleSystem ps = inst.Systems[i];
                if (ps == null) continue;

                var main = ps.main;
                if (active)
                    main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(inst.OriginalMaxParticles[i] * scale));
                else
                    main.maxParticles = 0;
            }
        }

        private static float GetQualityScale()
        {
#if SWEF_PERFORMANCE_AVAILABLE
            if (SWEF.Performance.PerformanceProfiler.Instance != null)
                return Mathf.Clamp(SWEF.Performance.PerformanceProfiler.Instance.QualityScale, 0.25f, 2f);
#endif
            return 1f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            foreach (var inst in _tracked)
            {
                if (inst.GameObject == null) continue;
                Gizmos.color = inst.CurrentLevel switch
                {
                    VFXLODLevel.Full      => Color.green,
                    VFXLODLevel.Reduced   => Color.yellow,
                    VFXLODLevel.Minimal   => new Color(1f, 0.5f, 0f),
                    _                     => Color.red
                };
                Gizmos.DrawWireSphere(inst.GameObject.transform.position, 2f);
            }
        }
#endif
    }
}
