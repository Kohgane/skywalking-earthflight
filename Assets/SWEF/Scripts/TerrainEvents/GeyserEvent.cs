// GeyserEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using System.Collections;
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Geyser eruption cycle simulation.
    ///
    /// <para>Geysers cycle through repeated eruption bursts rather than a single lifecycle
    /// pass.  Each burst drives the water column height, steam plume, and provides an
    /// updraft for the player aircraft if overhead.</para>
    /// </summary>
    public sealed class GeyserEvent : TerrainEvent
    {
        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Current height of the water column in metres.</summary>
        public float waterColumnHeight { get; private set; }

        /// <summary>Number of completed eruption bursts.</summary>
        public int burstCount { get; private set; }

        /// <summary>Whether an eruption burst is in progress.</summary>
        public bool isBursting { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float BurstInterval   = 60f;   // seconds between bursts
        private const float BurstDuration   = 8f;    // seconds per burst
        private const float MaxWaterColumn  = 60f;   // metres
        private const float ColumnRiseRate  = 20f;   // m/s
        private const float ColumnFallRate  = 10f;   // m/s

        private Coroutine _burstCoroutine;

        // ── Overrides ─────────────────────────────────────────────────────────────

        public override void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            waterColumnHeight = 0f;
            burstCount        = 0;
            isBursting        = false;
            base.Initialise(cfg, pos);
        }

        protected override void OnPhaseTransition(TerrainEventPhase newPhase)
        {
            if (newPhase == TerrainEventPhase.Active && _burstCoroutine == null)
                _burstCoroutine = StartCoroutine(BurstCycle());
        }

        protected override void OnTick()
        {
            if (isBursting)
                waterColumnHeight = Mathf.Min(waterColumnHeight + ColumnRiseRate  * Time.deltaTime, MaxWaterColumn * intensity);
            else
                waterColumnHeight = Mathf.Max(waterColumnHeight - ColumnFallRate * Time.deltaTime, 0f);
        }

        // ── Burst Cycle ───────────────────────────────────────────────────────────

        private IEnumerator BurstCycle()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(BurstInterval + UnityEngine.Random.Range(-10f, 10f));

                if (!isActive) break;

                isBursting = true;
                burstCount++;
                Debug.Log($"[SWEF] GeyserEvent '{config?.eventName}': burst #{burstCount} starting.");
                yield return new WaitForSeconds(BurstDuration);
                isBursting = false;
            }

            _burstCoroutine = null;
        }

        /// <summary>
        /// Returns the updraft force (upward Newtons) at <paramref name="worldPos"/> during a burst.
        /// </summary>
        public float GetUpdraftAt(Vector3 worldPos)
        {
            if (!isBursting || config == null) return 0f;
            float horizontalDist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(origin.x,   origin.z));
            float cone = currentRadius * 0.15f; // narrow steam column
            if (horizontalDist > cone) return 0f;
            return config.thermalStrength * intensity * (1f - horizontalDist / cone);
        }
    }
}
