// VolcanicEruptionEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Volcanic eruption simulation.
    ///
    /// <para>Adds lava flow tracking, ash cloud expansion, and optional terrain deformation
    /// on top of the base <see cref="TerrainEvent"/> lifecycle.</para>
    /// </summary>
    public sealed class VolcanicEruptionEvent : TerrainEvent
    {
        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Current lava flow radius in metres.</summary>
        public float lavaFlowRadius { get; private set; }

        /// <summary>Ash cloud altitude ceiling in metres above the eruption origin.</summary>
        public float ashCloudCeiling { get; private set; }

        /// <summary>Whether the caldera has collapsed (major eruption trigger).</summary>
        public bool calderaCollapsed { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float LavaFlowGrowthRate    = 3f;
        private const float AshCloudGrowthRate    = 50f;
        private const float InitialAshCeiling     = 500f;
        private const float MaxAshCeiling         = 20000f;

        // ── Overrides ─────────────────────────────────────────────────────────────

        public override void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            lavaFlowRadius = 0f;
            ashCloudCeiling = InitialAshCeiling;
            calderaCollapsed = false;
            base.Initialise(cfg, pos);
        }

        protected override void OnTick()
        {
            if (!isActive) return;

            lavaFlowRadius  = Mathf.Min(lavaFlowRadius  + LavaFlowGrowthRate * intensity * Time.deltaTime, currentRadius * 0.6f);
            ashCloudCeiling = Mathf.Min(ashCloudCeiling + AshCloudGrowthRate * intensity * Time.deltaTime, MaxAshCeiling);
        }

        protected override void OnPhaseTransition(TerrainEventPhase newPhase)
        {
            if (newPhase == TerrainEventPhase.Peak && !calderaCollapsed)
            {
                calderaCollapsed = true;
                Debug.Log($"[SWEF] VolcanicEruptionEvent '{config?.eventName}': caldera collapse triggered.");

                if (config != null && config.deformsTerrain)
                    TerrainDeformationSystem.Instance?.ApplyDeformation(origin, config.effectRadius * 0.2f, -config.maxDeformationAmount);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="worldPos"/> is inside the lava exclusion zone.
        /// </summary>
        public bool IsInLavaZone(Vector3 worldPos)
        {
            return isActive && Vector3.Distance(worldPos, origin) <= lavaFlowRadius;
        }
    }
}
