// TsunamiEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Tsunami wave generation and propagation simulation.
    ///
    /// <para>Tracks wave-front distance, wave height, and coastal inundation state.
    /// The VFX controller uses <see cref="waveFrontDistance"/> to position the ocean wave mesh.</para>
    /// </summary>
    public sealed class TsunamiEvent : TerrainEvent
    {
        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Distance the wave front has propagated from origin in metres.</summary>
        public float waveFrontDistance { get; private set; }

        /// <summary>Current crest height in metres above sea level.</summary>
        public float waveHeight { get; private set; }

        /// <summary>Propagation speed in metres per second.</summary>
        public float waveSpeed { get; private set; }

        /// <summary>Whether the wave has reached the coast (simplified).</summary>
        public bool coastlineReached { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float BaseWaveSpeed     = 200f;   // m/s (open ocean ~800 km/h = 222 m/s)
        private const float ShallowWaterSpeed = 30f;    // m/s near coast
        private const float CoastThreshold    = 0.8f;   // fraction of effectRadius

        // ── Overrides ─────────────────────────────────────────────────────────────

        public override void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            waveFrontDistance = 0f;
            coastlineReached  = false;
            waveSpeed         = BaseWaveSpeed;
            waveHeight        = MapIntensityToHeight(cfg.maxIntensity);
            base.Initialise(cfg, pos);
        }

        protected override void OnTick()
        {
            if (!isActive) return;

            // Decelerate as the wave approaches coast
            float normalised = waveFrontDistance / (currentRadius * CoastThreshold);
            waveSpeed = Mathf.Lerp(BaseWaveSpeed, ShallowWaterSpeed, Mathf.Clamp01(normalised));

            waveFrontDistance += waveSpeed * Time.deltaTime;

            if (!coastlineReached && waveFrontDistance >= currentRadius * CoastThreshold)
            {
                coastlineReached = true;
                Debug.Log($"[SWEF] TsunamiEvent '{config?.eventName}': wave reached coastline (height ~{waveHeight:F0} m).");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static float MapIntensityToHeight(TerrainEventIntensity intensity)
        {
            switch (intensity)
            {
                case TerrainEventIntensity.Trace:    return  1f;
                case TerrainEventIntensity.Minor:    return  3f;
                case TerrainEventIntensity.Moderate: return  7f;
                case TerrainEventIntensity.Strong:   return 15f;
                case TerrainEventIntensity.Major:    return 30f;
                case TerrainEventIntensity.Extreme:  return 60f;
                default:                             return  5f;
            }
        }
    }
}
