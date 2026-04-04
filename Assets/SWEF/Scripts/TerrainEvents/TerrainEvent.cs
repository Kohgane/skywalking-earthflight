// TerrainEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — MonoBehaviour that represents a single live terrain event instance.
    ///
    /// <para>Attached to a dynamically created GameObject by <see cref="TerrainEventManager"/>.
    /// Runs through the <see cref="TerrainEventPhase"/> state machine and raises lifecycle
    /// events that the manager, VFX controller, mission triggers and achievement system consume.</para>
    /// </summary>
    public class TerrainEvent : MonoBehaviour
    {
        // ── Public state ──────────────────────────────────────────────────────────

        /// <summary>Configuration data for this event.</summary>
        public TerrainEventConfig config { get; private set; }

        /// <summary>World-space centre of the event.</summary>
        public Vector3 origin { get; private set; }

        /// <summary>Current lifecycle phase.</summary>
        public TerrainEventPhase phase { get; private set; } = TerrainEventPhase.Dormant;

        /// <summary>Current intensity (0–1 normalised across the phase cycle).</summary>
        public float intensity { get; private set; }

        /// <summary>Current effect radius in metres (grows during active phase).</summary>
        public float currentRadius { get; private set; }

        /// <summary>Elapsed seconds since the event was activated.</summary>
        public float elapsedTime { get; private set; }

        /// <summary>Returns <c>true</c> while the event is in an active phase.</summary>
        public bool isActive => phase >= TerrainEventPhase.Active && phase <= TerrainEventPhase.Subsiding;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when the lifecycle phase changes.</summary>
        public event Action<TerrainEvent> OnPhaseChanged;

        /// <summary>Raised when the event fully ends (Aftermath complete).</summary>
        public event Action<TerrainEvent> OnEventEnded;

        // ── Private ───────────────────────────────────────────────────────────────

        private float _phaseTimer;

        // ── Initialisation ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialise the event. Must be called immediately after AddComponent by the manager.
        /// </summary>
        public virtual void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            config = cfg;
            origin = pos;
            transform.position = pos;
            currentRadius = cfg.effectRadius;
            TransitionTo(TerrainEventPhase.BuildUp);
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            _phaseTimer  += Time.deltaTime;

            UpdatePhase();
            UpdateRadius();
            OnTick();
        }

        // ── Phase State Machine ───────────────────────────────────────────────────

        private void UpdatePhase()
        {
            float phaseDuration = GetCurrentPhaseDuration();
            if (_phaseTimer < phaseDuration) return;

            _phaseTimer = 0f;

            switch (phase)
            {
                case TerrainEventPhase.BuildUp:
                    TransitionTo(TerrainEventPhase.Active);
                    break;
                case TerrainEventPhase.Active:
                    TransitionTo(TerrainEventPhase.Peak);
                    break;
                case TerrainEventPhase.Peak:
                    TransitionTo(TerrainEventPhase.Subsiding);
                    break;
                case TerrainEventPhase.Subsiding:
                    TransitionTo(TerrainEventPhase.Aftermath);
                    break;
                case TerrainEventPhase.Aftermath:
                    EndEvent();
                    break;
            }
        }

        private float GetCurrentPhaseDuration()
        {
            if (config == null) return float.MaxValue;
            switch (phase)
            {
                case TerrainEventPhase.BuildUp:   return config.buildUpDuration;
                case TerrainEventPhase.Active:    return config.peakDuration * 0.5f;
                case TerrainEventPhase.Peak:      return config.peakDuration;
                case TerrainEventPhase.Subsiding: return config.subsidingDuration;
                case TerrainEventPhase.Aftermath: return config.subsidingDuration * 0.5f;
                default:                          return float.MaxValue;
            }
        }

        private void TransitionTo(TerrainEventPhase newPhase)
        {
            phase = newPhase;
            _phaseTimer = 0f;

            // Update normalised intensity
            switch (newPhase)
            {
                case TerrainEventPhase.BuildUp:   intensity = 0.2f; break;
                case TerrainEventPhase.Active:    intensity = 0.6f; break;
                case TerrainEventPhase.Peak:      intensity = 1.0f; break;
                case TerrainEventPhase.Subsiding: intensity = 0.4f; break;
                case TerrainEventPhase.Aftermath: intensity = 0.1f; break;
                default:                          intensity = 0f;   break;
            }

            OnPhaseChanged?.Invoke(this);
            TerrainEventManager.Instance?.NotifyPhaseChanged(this);
            OnPhaseTransition(newPhase);

            Debug.Log($"[SWEF] TerrainEvent '{config?.eventName}': phase → {newPhase}");
        }

        private void UpdateRadius()
        {
            if (config == null) return;
            if (phase == TerrainEventPhase.Active || phase == TerrainEventPhase.Peak)
                currentRadius = Mathf.Min(currentRadius + config.radiusGrowthRate * Time.deltaTime,
                                          config.effectRadius * 2f);
        }

        private void EndEvent()
        {
            phase = TerrainEventPhase.Dormant;
            OnEventEnded?.Invoke(this);
            TerrainEventManager.Instance?.EndEvent(this);
        }

        // ── Virtual Hooks for Sub-types ───────────────────────────────────────────

        /// <summary>Called every Update tick. Override in sub-classes for event-specific behaviour.</summary>
        protected virtual void OnTick() { }

        /// <summary>Called when transitioning to <paramref name="newPhase"/>. Override for event-specific responses.</summary>
        protected virtual void OnPhaseTransition(TerrainEventPhase newPhase) { }

        // ── Public Queries ────────────────────────────────────────────────────────

        /// <summary>Returns <c>true</c> if <paramref name="pos"/> is within the current effect radius.</summary>
        public bool ContainsPosition(Vector3 pos)
        {
            return Vector3.Distance(pos, origin) <= currentRadius;
        }

        /// <summary>
        /// Returns the turbulence contribution at <paramref name="worldPos"/> based on
        /// distance and current intensity.
        /// </summary>
        public float GetTurbulenceAt(Vector3 worldPos)
        {
            if (config == null || !isActive) return 0f;
            float dist = Vector3.Distance(worldPos, origin);
            if (dist > currentRadius) return 0f;
            float falloff = 1f - (dist / currentRadius);
            return config.turbulenceMultiplier * intensity * falloff;
        }

        /// <summary>
        /// Returns visibility reduction (0–1) at <paramref name="worldPos"/>.
        /// </summary>
        public float GetVisibilityReductionAt(Vector3 worldPos)
        {
            if (config == null || !isActive) return 0f;
            float dist = Vector3.Distance(worldPos, origin);
            if (dist > currentRadius) return 0f;
            float falloff = 1f - (dist / currentRadius);
            return config.visibilityReduction * intensity * falloff;
        }
    }
}
