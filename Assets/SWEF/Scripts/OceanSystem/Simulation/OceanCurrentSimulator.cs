// OceanCurrentSimulator.cs — Phase 117: Advanced Ocean & Maritime System
// Surface currents, rip currents affecting seaplane taxiing and water landings.
// Namespace: SWEF.OceanSystem

using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates ocean surface currents using layered Perlin noise fields.
    /// Rip currents are injected near designated coastal zones.
    /// Provides current velocity queries used by <see cref="WaterPhysicsController"/>
    /// and <see cref="WaterLandingController"/>.
    /// </summary>
    public class OceanCurrentSimulator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Current Field")]
        [SerializeField] private float noiseScale = 0.0002f;
        [SerializeField] private float noiseTimeScale = 0.02f;
        [SerializeField] private float currentSmoothing = 0.5f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _noiseTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (config == null || !config.enableCurrents) return;
            _noiseTime += Time.deltaTime * noiseTimeScale;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the surface current velocity vector (XZ plane) at
        /// <paramref name="worldPosition"/>.
        /// </summary>
        public Vector2 GetCurrentVelocity(Vector3 worldPosition)
        {
            if (config == null || !config.enableCurrents) return Vector2.zero;

            float nx = Mathf.PerlinNoise(worldPosition.x * noiseScale + _noiseTime,
                                         worldPosition.z * noiseScale);
            float nz = Mathf.PerlinNoise(worldPosition.x * noiseScale,
                                         worldPosition.z * noiseScale + _noiseTime + 100f);

            // Map 0..1 → −1..+1
            float vx = (nx - 0.5f) * 2f * config.maxCurrentSpeed;
            float vz = (nz - 0.5f) * 2f * config.maxCurrentSpeed;

            return new Vector2(vx, vz);
        }

        /// <summary>
        /// Simulates a rip current pulling objects away from shore at the given position.
        /// Returns the rip current velocity if within <paramref name="ripRadius"/> of
        /// <paramref name="ripCentre"/>; otherwise <see cref="Vector2.zero"/>.
        /// </summary>
        public Vector2 GetRipCurrentVelocity(Vector3 worldPosition, Vector3 ripCentre, float ripRadius, float ripSpeed)
        {
            var flat = new Vector2(worldPosition.x - ripCentre.x, worldPosition.z - ripCentre.z);
            float dist = flat.magnitude;
            if (dist > ripRadius || dist < 0.001f) return Vector2.zero;

            float strength = (1f - dist / ripRadius) * ripSpeed;
            return flat.normalized * strength;
        }
    }
}
