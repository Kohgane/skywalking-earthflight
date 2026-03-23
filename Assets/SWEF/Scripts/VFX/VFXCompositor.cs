// VFXCompositor.cs — SWEF Particle Effects & VFX System
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SWEF.VFX
{
    /// <summary>
    /// Post-processing VFX compositor that manages screen-space effect layering and
    /// smooth transitions between visual states.
    ///
    /// <para>Effects handled: heat shimmer (engine/reentry), speed motion blur,
    /// altitude colour grading, underwater tint. Each effect is registered with a
    /// priority and blend weight. Higher-priority effects take precedence when the
    /// total performance budget is exceeded.</para>
    ///
    /// <para>Integrates with URP's <c>Volume</c> system when
    /// <c>SWEF_URP_POSTPROCESSING_AVAILABLE</c> is defined. When unavailable, effect
    /// weights are still computed and exposed so custom shaders can consume them.</para>
    /// </summary>
    public sealed class VFXCompositor : MonoBehaviour
    {
        // ── Effect Descriptor ─────────────────────────────────────────────────────

        /// <summary>Identifies all supported screen-space compositor effects.</summary>
        public enum ScreenEffect
        {
            /// <summary>Heat shimmer / distortion overlay near engines or during reentry.</summary>
            HeatShimmer,
            /// <summary>Radial motion blur that intensifies with speed.</summary>
            SpeedBlur,
            /// <summary>Altitude-based sky and ambient colour grading.</summary>
            AltitudeColorGrading,
            /// <summary>Blue-green tint and caustic overlay when underwater.</summary>
            UnderwaterTint
        }

        /// <summary>Runtime configuration for a single screen-space effect layer.</summary>
        [Serializable]
        public sealed class EffectLayer
        {
            /// <summary>The screen-space effect this layer represents.</summary>
            public ScreenEffect effect;

            /// <summary>Blend weight 0–1 (0 = inactive, 1 = fully applied).</summary>
            [Range(0f, 1f)] public float weight;

            /// <summary>Target blend weight driving smooth transitions.</summary>
            [Range(0f, 1f)] public float targetWeight;

            /// <summary>Transition speed (weight units per second).</summary>
            [Min(0f)] public float transitionSpeed = 2f;

            /// <summary>Priority — higher values are applied first when budget is constrained.</summary>
            public int priority;

            /// <summary>Whether this effect is currently budgeted and active.</summary>
            [NonSerialized] public bool Budgeted = true;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Performance Budget")]
        [Tooltip("Maximum total weight across all active effects (0–N). Effects are cut by priority when exceeded.")]
        [SerializeField, Min(0f)] private float maxTotalWeight = 3f;

        [Header("Transition")]
        [Tooltip("Default speed (weight units/second) for weight transitions when none is specified per layer.")]
        [SerializeField, Min(0f)] private float defaultTransitionSpeed = 2f;

        [Header("Initial Layers")]
        [Tooltip("Pre-configured effect layers. Additional layers can be added at runtime.")]
        [SerializeField] private List<EffectLayer> layers = new List<EffectLayer>();

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the active set of budgeted effects changes.</summary>
        public event Action OnCompositorChanged;

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure sensible defaults on pre-configured layers
            foreach (var l in layers)
                l.transitionSpeed = Mathf.Max(l.transitionSpeed, defaultTransitionSpeed);
        }

        private void Update()
        {
            UpdateWeights();
            ApplyBudget();
            PushToPostProcessing();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the target blend weight for a screen effect, triggering a smooth transition.
        /// </summary>
        /// <param name="effect">The screen effect to update.</param>
        /// <param name="targetWeight">Desired blend weight 0–1.</param>
        public void SetEffectWeight(ScreenEffect effect, float targetWeight)
        {
            EffectLayer layer = GetOrCreate(effect);
            layer.targetWeight = Mathf.Clamp01(targetWeight);
        }

        /// <summary>Returns the current (smoothed) blend weight of the given effect.</summary>
        /// <param name="effect">The screen effect to query.</param>
        public float GetEffectWeight(ScreenEffect effect)
        {
            foreach (var l in layers)
                if (l.effect == effect) return l.weight;
            return 0f;
        }

        /// <summary>Immediately sets an effect's weight without smoothing.</summary>
        /// <param name="effect">The screen effect to snap.</param>
        /// <param name="weight">Blend weight 0–1.</param>
        public void SnapEffectWeight(ScreenEffect effect, float weight)
        {
            EffectLayer layer = GetOrCreate(effect);
            layer.weight       = Mathf.Clamp01(weight);
            layer.targetWeight = layer.weight;
        }

        /// <summary>Sets the priority of a registered effect layer (higher = evaluated first).</summary>
        /// <param name="effect">The screen effect to reprioritise.</param>
        /// <param name="priority">New priority value.</param>
        public void SetEffectPriority(ScreenEffect effect, int priority)
        {
            EffectLayer layer = GetOrCreate(effect);
            layer.priority = priority;
        }

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private void UpdateWeights()
        {
            foreach (var l in layers)
                l.weight = Mathf.MoveTowards(l.weight, l.targetWeight, l.transitionSpeed * Time.deltaTime);
        }

        private void ApplyBudget()
        {
            // Sort by priority descending
            layers.Sort((a, b) => b.priority.CompareTo(a.priority));

            float remaining = maxTotalWeight;
            bool changed = false;

            foreach (var l in layers)
            {
                bool prev = l.Budgeted;
                if (remaining > 0f && l.weight > 0f)
                {
                    float alloc = Mathf.Min(l.weight, remaining);
                    remaining -= alloc;
                    l.Budgeted = true;
                }
                else
                {
                    l.Budgeted = false;
                }
                if (l.Budgeted != prev) changed = true;
            }

            if (changed) OnCompositorChanged?.Invoke();
        }

        private void PushToPostProcessing()
        {
#if SWEF_URP_POSTPROCESSING_AVAILABLE
            // Example: push weights to a global Volume profile override
            // Requires a Volume with a custom VFXCompositorEffect pass configured.
            // Shader.SetGlobalFloat("_HeatShimmerWeight", GetEffectWeight(ScreenEffect.HeatShimmer));
            // Shader.SetGlobalFloat("_SpeedBlurWeight",   GetEffectWeight(ScreenEffect.SpeedBlur));
            // etc.
#endif
            // Always push as global shader parameters so custom shaders can consume them
            Shader.SetGlobalFloat("_SWEF_HeatShimmerWeight",       GetBudgetedWeight(ScreenEffect.HeatShimmer));
            Shader.SetGlobalFloat("_SWEF_SpeedBlurWeight",          GetBudgetedWeight(ScreenEffect.SpeedBlur));
            Shader.SetGlobalFloat("_SWEF_AltitudeColorGradingWeight", GetBudgetedWeight(ScreenEffect.AltitudeColorGrading));
            Shader.SetGlobalFloat("_SWEF_UnderwaterTintWeight",    GetBudgetedWeight(ScreenEffect.UnderwaterTint));
        }

        private float GetBudgetedWeight(ScreenEffect effect)
        {
            foreach (var l in layers)
                if (l.effect == effect) return l.Budgeted ? l.weight : 0f;
            return 0f;
        }

        private EffectLayer GetOrCreate(ScreenEffect effect)
        {
            foreach (var l in layers)
                if (l.effect == effect) return l;

            var newLayer = new EffectLayer
            {
                effect          = effect,
                weight          = 0f,
                targetWeight    = 0f,
                transitionSpeed = defaultTransitionSpeed,
                priority        = 0
            };
            layers.Add(newLayer);
            return newLayer;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Heat Shimmer Full")]
        private void EditorTestHeatShimmer() => SetEffectWeight(ScreenEffect.HeatShimmer, 1f);

        [ContextMenu("Test Speed Blur Full")]
        private void EditorTestSpeedBlur() => SetEffectWeight(ScreenEffect.SpeedBlur, 1f);

        [ContextMenu("Test Underwater Tint")]
        private void EditorTestUnderwater() => SetEffectWeight(ScreenEffect.UnderwaterTint, 1f);

        [ContextMenu("Clear All Effects")]
        private void EditorClearAll()
        {
            foreach (var l in layers) l.targetWeight = 0f;
        }

        [ContextMenu("Log Effect Weights")]
        private void EditorLogWeights()
        {
            Debug.Log("[VFXCompositor] Current effect weights:");
            foreach (var l in layers)
                Debug.Log($"  {l.effect}: weight={l.weight:F2} target={l.targetWeight:F2} budgeted={l.Budgeted} priority={l.priority}");
        }
#endif
    }
}
