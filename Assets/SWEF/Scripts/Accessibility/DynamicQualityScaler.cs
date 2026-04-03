// DynamicQualityScaler.cs — SWEF Accessibility & Platform Optimization (Phase 93)
using System.Collections;
using UnityEngine;

namespace SWEF.Accessibility
{
    /// <summary>
    /// MonoBehaviour that monitors frame rate and automatically steps the
    /// quality tier up or down to maintain the target FPS.
    ///
    /// <para>Uses hysteresis bands to prevent rapid oscillation:
    /// quality is lowered only after <see cref="downgradeSustainSeconds"/> consecutive
    /// low-FPS frames, and raised only after <see cref="upgradeSustainSeconds"/> consecutive
    /// high-FPS frames.</para>
    ///
    /// <para>Works with <see cref="PlatformOptimizer"/> to apply tier changes.</para>
    /// </summary>
    public class DynamicQualityScaler : MonoBehaviour
    {
        // ── Serialised fields ─────────────────────────────────────────────────────
        [Header("FPS Targets")]
        [SerializeField, Tooltip("Minimum acceptable average FPS before downgrading quality.")]
        private float minAcceptableFps = 28f;

        [SerializeField, Tooltip("FPS threshold above which quality may be upgraded.")]
        private float upgradeThresholdFps = 58f;

        [Header("Hysteresis")]
        [SerializeField, Tooltip("Seconds of sustained low FPS before a downgrade is triggered.")]
        private float downgradeSustainSeconds = 3f;

        [SerializeField, Tooltip("Seconds of sustained high FPS before an upgrade is considered.")]
        private float upgradeSustainSeconds = 10f;

        [SerializeField, Tooltip("Minimum seconds between any tier changes (cooldown).")]
        private float changeCooldownSeconds = 5f;

        [Header("Control")]
        [SerializeField, Tooltip("Enable automatic quality scaling.")]
        private bool enabled = true;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private float _lowFpsTimer;
        private float _highFpsTimer;
        private float _cooldownTimer;
        private float _smoothedFps;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()  { StartCoroutine(ScaleLoop()); }
        private void OnDisable() { StopAllCoroutines(); }

        private void Update()
        {
            // Smooth FPS with an exponential moving average (α ≈ 0.05 per frame)
            float dt = Time.unscaledDeltaTime;
            _smoothedFps = _smoothedFps == 0f
                ? (dt > 0f ? 1f / dt : 60f)
                : Mathf.Lerp(_smoothedFps, dt > 0f ? 1f / dt : _smoothedFps, 0.05f);
        }

        // ── Scaling loop ──────────────────────────────────────────────────────────

        private IEnumerator ScaleLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                if (!enabled || PlatformOptimizer.Instance == null) continue;

                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - 0.5f);
                if (_cooldownTimer > 0f) continue;

                float fps = _smoothedFps;
                QualityTier current = PlatformOptimizer.Instance.ActiveTier;

                // Accumulate timers
                if (fps < minAcceptableFps)
                {
                    _lowFpsTimer  += 0.5f;
                    _highFpsTimer  = 0f;
                }
                else if (fps >= upgradeThresholdFps)
                {
                    _highFpsTimer += 0.5f;
                    _lowFpsTimer   = 0f;
                }
                else
                {
                    _lowFpsTimer  = 0f;
                    _highFpsTimer = 0f;
                }

                // Downgrade?
                if (_lowFpsTimer >= downgradeSustainSeconds && current < QualityTier.Potato)
                {
                    QualityTier next = current + 1;
                    Debug.LogWarning($"[SWEF] Accessibility: DynamicQualityScaler — FPS {fps:F1} low, downgrading to {next}.");
                    PlatformOptimizer.Instance.ApplyTier(next);
                    _lowFpsTimer   = 0f;
                    _cooldownTimer = changeCooldownSeconds;
                }
                // Upgrade?
                else if (_highFpsTimer >= upgradeSustainSeconds && current > QualityTier.Ultra)
                {
                    QualityTier next = current - 1;
                    Debug.Log($"[SWEF] Accessibility: DynamicQualityScaler — FPS {fps:F1} stable, upgrading to {next}.");
                    PlatformOptimizer.Instance.ApplyTier(next);
                    _highFpsTimer  = 0f;
                    _cooldownTimer = changeCooldownSeconds;
                }
            }
        }
    }
}
