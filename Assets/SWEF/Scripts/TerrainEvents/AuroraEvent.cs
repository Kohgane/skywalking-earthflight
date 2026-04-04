// AuroraEvent.cs — SWEF Dynamic Terrain Events & Geological Phenomena (Phase 105)
using UnityEngine;

namespace SWEF.TerrainEvents
{
    /// <summary>
    /// Phase 105 — Enhanced aurora borealis / australis rendering event.
    ///
    /// <para>Drives colour cycling, curtain animation parameters, and exposes the current
    /// aurora coverage angle for use by <see cref="TerrainEventVFXController"/>.</para>
    /// </summary>
    public sealed class AuroraEvent : TerrainEvent
    {
        // ── State ─────────────────────────────────────────────────────────────────

        /// <summary>Current dominant colour of the aurora curtains (interpolated over time).</summary>
        public Color currentColor { get; private set; } = Color.green;

        /// <summary>Normalised animation phase (0–1) driving curtain ripple speed.</summary>
        public float curtainAnimationPhase { get; private set; }

        /// <summary>Angular coverage of the aurora arc in degrees (0–360).</summary>
        public float coverageDegrees { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────

        private const float ColorCycleSpeed   = 0.05f;
        private const float AnimationSpeed    = 0.3f;
        private const float MaxCoverage       = 180f;

        private static readonly Color[] AuroraColors =
        {
            new Color(0.0f, 1.0f, 0.5f),  // green
            new Color(0.5f, 0.2f, 1.0f),  // purple
            new Color(0.0f, 0.8f, 1.0f),  // cyan
            new Color(1.0f, 0.4f, 0.2f),  // pink-red
        };

        private float _colorTimer;
        private int   _colorIndex;

        // ── Overrides ─────────────────────────────────────────────────────────────

        public override void Initialise(TerrainEventConfig cfg, Vector3 pos)
        {
            _colorTimer  = 0f;
            _colorIndex  = 0;
            coverageDegrees = 30f;
            base.Initialise(cfg, pos);
        }

        protected override void OnTick()
        {
            _colorTimer += Time.deltaTime * ColorCycleSpeed;
            if (_colorTimer >= 1f)
            {
                _colorTimer = 0f;
                _colorIndex = (_colorIndex + 1) % AuroraColors.Length;
            }

            Color next = AuroraColors[(_colorIndex + 1) % AuroraColors.Length];
            currentColor = Color.Lerp(AuroraColors[_colorIndex], next, _colorTimer);

            curtainAnimationPhase = Mathf.Repeat(curtainAnimationPhase + AnimationSpeed * intensity * Time.deltaTime, 1f);

            if (isActive)
                coverageDegrees = Mathf.Min(coverageDegrees + 10f * intensity * Time.deltaTime, MaxCoverage);
        }

        /// <summary>
        /// Returns <c>true</c> if the player at <paramref name="altitude"/> is within an
        /// altitude band suitable for aurora viewing.
        /// </summary>
        public bool IsOptimalViewingAltitude(float altitude)
        {
            // Auroras are best visible between 5 000 m and 30 000 m
            return altitude >= 5000f && altitude <= 30000f;
        }
    }
}
