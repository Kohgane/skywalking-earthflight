// EarthquakeEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System.Collections;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Earthquake simulation.
    ///
    /// <para>Drives ground-shaking camera feedback, terrain displacement, and schedules
    /// aftershocks at diminishing intensity after the peak phase.</para>
    /// </summary>
    public sealed class EarthquakeEvent : TerrainEvent
    {
        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Richter-scale magnitude (1–9) derived from config intensity.</summary>
        public float magnitude { get; private set; }

        /// <summary>Number of aftershocks that have occurred.</summary>
        public int aftershockCount { get; private set; }

        /// <summary>Whether the main shock has been triggered.</summary>
        public bool mainShockTriggered { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────

        private const int    MaxAftershocks      = 5;
        private const float  AftershockInterval  = 30f;
        private const float  AftershockDecay     = 0.6f;

        // ── Overrides ─────────────────────────────────────────────────────────────

        public override void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            magnitude          = MapIntensityToMagnitude(cfg.maxIntensity);
            aftershockCount    = 0;
            mainShockTriggered = false;
            base.Initialise(cfg, pos);
        }

        protected override void OnPhaseTransition(TerrainEventPhase newPhase)
        {
            if (newPhase == TerrainEventPhase.Peak && !mainShockTriggered)
            {
                mainShockTriggered = true;
                Debug.Log($"[SWEF] EarthquakeEvent '{config?.eventName}': main shock M{magnitude:F1}.");

                if (config != null && config.deformsTerrain)
                    TerrainDeformationSystem.Instance?.ApplyDeformation(origin, config.effectRadius, config.maxDeformationAmount * 0.5f);

                StartCoroutine(ScheduleAftershocks());
            }
        }

        private IEnumerator ScheduleAftershocks()
        {
            float currentMagnitude = magnitude * AftershockDecay;
            for (int i = 0; i < MaxAftershocks && currentMagnitude >= 1f; i++)
            {
                yield return new WaitForSeconds(AftershockInterval + UnityEngine.Random.Range(-10f, 10f));
                aftershockCount++;
                Debug.Log($"[SWEF] EarthquakeEvent: aftershock #{aftershockCount} M{currentMagnitude:F1}.");
                currentMagnitude *= AftershockDecay;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static float MapIntensityToMagnitude(TerrainEventIntensity intensity)
        {
            switch (intensity)
            {
                case TerrainEventIntensity.Trace:    return 2.0f;
                case TerrainEventIntensity.Minor:    return 3.5f;
                case TerrainEventIntensity.Moderate: return 5.0f;
                case TerrainEventIntensity.Strong:   return 6.0f;
                case TerrainEventIntensity.Major:    return 7.0f;
                case TerrainEventIntensity.Extreme:  return 8.5f;
                default:                             return 4.0f;
            }
        }
    }
}
