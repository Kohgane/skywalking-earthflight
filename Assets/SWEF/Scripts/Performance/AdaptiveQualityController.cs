using System;
using UnityEngine;
using SWEF.Core;

namespace SWEF.Performance
{
    /// <summary>
    /// Dynamically adjusts rendering quality based on real-time FPS data from
    /// <see cref="PerformanceProfiler"/> snapshots.
    ///
    /// Lowering: if avg FPS is below <see cref="targetFps"/> for <see cref="lowerSampleCount"/>
    /// consecutive snapshots, the quality is stepped down.
    ///
    /// Raising: if avg FPS exceeds <see cref="targetFps"/> × <see cref="raiseThresholdMultiplier"/>
    /// for <see cref="raiseSampleCount"/> consecutive snapshots AND at least
    /// <see cref="cooldownSeconds"/> have elapsed since the last reduction, the quality is stepped up.
    /// </summary>
    public class AdaptiveQualityController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────
        [Header("Targets")]
        [SerializeField] private float targetFps                 = 30f;
        [SerializeField] private float raiseThresholdMultiplier = 1.5f;
        [SerializeField] private int   lowerSampleCount         = 3;
        [SerializeField] private int   raiseSampleCount         = 5;
        [SerializeField] private float cooldownSeconds          = 30f;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Fired whenever an automatic quality action is taken.</summary>
        public event Action<QualityAction> OnQualityAdjusted;

        // ── Public state ─────────────────────────────────────────────────────────
        /// <summary>When <c>false</c>, no automatic quality changes are made.</summary>
        public bool AutoAdjustEnabled { get; set; } = true;

        /// <summary>
        /// Composite quality score 0–100 derived from the current
        /// <see cref="QualityPresetManager"/> level.
        /// </summary>
        public int CurrentQualityScore
        {
            get
            {
                if (_qpm == null) return 50;
                return _qpm.CurrentQuality switch
                {
                    QualityPresetManager.QualityLevel.Low   => 10,
                    QualityPresetManager.QualityLevel.Medium => 40,
                    QualityPresetManager.QualityLevel.High   => 70,
                    QualityPresetManager.QualityLevel.Ultra  => 100,
                    _                                        => 50,
                };
            }
        }

        // ── Internal state ────────────────────────────────────────────────────────
        private PerformanceProfiler   _profiler;
        private QualityPresetManager  _qpm;

        private int   _lowFpsStreak;
        private int   _highFpsStreak;
        private float _lastLowerTime = float.NegativeInfinity;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            _profiler = PerformanceProfiler.Instance
                ?? FindFirstObjectByType<PerformanceProfiler>();

            _qpm = QualityPresetManager.Instance
                ?? FindFirstObjectByType<QualityPresetManager>();
        }

        private void OnEnable()
        {
            if (_profiler != null)
                _profiler.OnSnapshotTaken += OnSnapshot;
        }

        private void OnDisable()
        {
            if (_profiler != null)
                _profiler.OnSnapshotTaken -= OnSnapshot;
        }

        // ── Internal ─────────────────────────────────────────────────────────────
        private void OnSnapshot(PerformanceSnapshot snap)
        {
            if (!AutoAdjustEnabled) return;
            if (_qpm == null) return;

            float fps   = snap.avgFps;
            float raise = targetFps * raiseThresholdMultiplier;

            if (fps < targetFps)
            {
                _lowFpsStreak++;
                _highFpsStreak = 0;

                if (_lowFpsStreak >= lowerSampleCount)
                {
                    _lowFpsStreak  = 0;
                    _lastLowerTime = Time.realtimeSinceStartup;
                    StepDown();
                }
            }
            else if (fps >= raise)
            {
                _highFpsStreak++;
                _lowFpsStreak = 0;

                float elapsed = Time.realtimeSinceStartup - _lastLowerTime;
                if (_highFpsStreak >= raiseSampleCount && elapsed >= cooldownSeconds)
                {
                    _highFpsStreak = 0;
                    StepUp();
                }
            }
            else
            {
                // FPS in acceptable range — reset counters
                _lowFpsStreak  = 0;
                _highFpsStreak = 0;
            }
        }

        private void StepDown()
        {
            var current = _qpm.CurrentQuality;
            if (current == QualityPresetManager.QualityLevel.Low)
            {
                // Already at minimum; disable post-processing, particles etc.
                ApplyAction(QualityAction.DisablePostProcessing);
                return;
            }

            var next = current switch
            {
                QualityPresetManager.QualityLevel.Ultra  => QualityPresetManager.QualityLevel.High,
                QualityPresetManager.QualityLevel.High   => QualityPresetManager.QualityLevel.Medium,
                _                                        => QualityPresetManager.QualityLevel.Low,
            };

            _qpm.SetQuality(next);
            ApplyAction(QualityAction.ReduceShadows);
            Debug.Log($"[SWEF] AdaptiveQualityController: quality lowered to {next}");
        }

        private void StepUp()
        {
            var current = _qpm.CurrentQuality;
            if (current == QualityPresetManager.QualityLevel.Ultra)
            {
                ApplyAction(QualityAction.IncreaseAll);
                return;
            }

            var next = current switch
            {
                QualityPresetManager.QualityLevel.Low    => QualityPresetManager.QualityLevel.Medium,
                QualityPresetManager.QualityLevel.Medium => QualityPresetManager.QualityLevel.High,
                _                                        => QualityPresetManager.QualityLevel.Ultra,
            };

            _qpm.SetQuality(next);
            ApplyAction(QualityAction.IncreaseAll);
            Debug.Log($"[SWEF] AdaptiveQualityController: quality raised to {next}");
        }

        private void ApplyAction(QualityAction action)
        {
            // Particle system reduction
            if (action == QualityAction.ReduceParticles || action == QualityAction.ReduceShadows)
            {
                var particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
                foreach (var ps in particles)
                {
                    // ParticleSystem.MainModule property setters call into native code via the
                    // cached particle system reference inside the struct, so assignments through
                    // 'main' DO modify the underlying ParticleSystem despite the struct semantics.
                    var main = ps.main;
                    main.maxParticles = Mathf.Max(1, main.maxParticles / 2);
                }
            }

            OnQualityAdjusted?.Invoke(action);
        }
    }

    // ── Enums ─────────────────────────────────────────────────────────────────

    /// <summary>Types of quality actions taken by <see cref="AdaptiveQualityController"/>.</summary>
    public enum QualityAction
    {
        None,
        ReduceShadows,
        ReduceTextures,
        DisablePostProcessing,
        ReduceParticles,
        ReduceTileLOD,
        IncreaseAll,
    }
}
