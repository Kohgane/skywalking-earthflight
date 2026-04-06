// OceanWaveSimulator.cs — Phase 117: Advanced Ocean & Maritime System
// Multi-octave Gerstner wave simulation.
// Namespace: SWEF.OceanSystem

using System;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Simulates multi-octave Gerstner (trochoidal) ocean waves.
    /// Parametrised by sea state; provides surface height queries used by buoyancy,
    /// water landing, and rendering sub-systems.
    /// </summary>
    public class OceanWaveSimulator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private OceanSystemConfig config;

        [Header("Wave Octaves")]
        [SerializeField] private int octaves = 4;
        [SerializeField] private float baseAmplitude = 0.5f;
        [SerializeField] private float baseFrequency = 0.1f;
        [SerializeField] private float baseSpeed = 1.2f;
        [SerializeField] private float choppiness = 0.8f;

        // ── Private state ─────────────────────────────────────────────────────────

        private float _waveTime;
        private float _windSpeed = 5f;
        private float _windDirection = 270f;

        // ── Gerstner wave parameters (per octave) ─────────────────────────────────

        private struct GerstnerWave
        {
            public Vector2 direction;
            public float   amplitude;
            public float   frequency;
            public float   speed;
            public float   phase;
        }

        private GerstnerWave[] _waves;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (config != null)
            {
                octaves       = config.waveOctaves;
                baseAmplitude = config.swellAmplitude * 0.5f;
                choppiness    = config.choppiness;
            }
            RebuildWaves();
        }

        private void Update()
        {
            _waveTime += Time.deltaTime * (config != null ? config.waveTimeScale : 1f);
        }

        // ── Wave Build ────────────────────────────────────────────────────────────

        private void RebuildWaves()
        {
            _waves = new GerstnerWave[octaves];
            float windRad = _windDirection * Mathf.Deg2Rad;
            var baseDir   = new Vector2(Mathf.Cos(windRad), Mathf.Sin(windRad));

            for (int i = 0; i < octaves; i++)
            {
                float scale    = Mathf.Pow(0.5f, i);
                float angle    = (i % 2 == 0 ? 1f : -1f) * (15f + i * 10f) * Mathf.Deg2Rad;
                float cos      = Mathf.Cos(angle);
                float sin      = Mathf.Sin(angle);
                var   dir      = new Vector2(baseDir.x * cos - baseDir.y * sin,
                                             baseDir.x * sin + baseDir.y * cos).normalized;

                _waves[i] = new GerstnerWave
                {
                    direction = dir,
                    amplitude = baseAmplitude * scale,
                    frequency = baseFrequency / scale,
                    speed     = baseSpeed     * Mathf.Sqrt(1f / scale),
                    phase     = i * 1.3f
                };
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns estimated surface height (Y) at world XZ position.</summary>
        public float GetSurfaceHeight(Vector2 worldXZ)
        {
            if (_waves == null) return 0f;
            float height = 0f;
            foreach (var w in _waves)
            {
                float k   = w.frequency;
                float dot = Vector2.Dot(w.direction, worldXZ);
                float phi = k * dot - w.speed * _waveTime + w.phase;
                height   += w.amplitude * Mathf.Sin(phi);
            }
            return height;
        }

        /// <summary>Returns the Gerstner displacement vector at world XZ position.</summary>
        public Vector3 GetDisplacement(Vector2 worldXZ)
        {
            if (_waves == null) return Vector3.zero;
            float dx = 0f, dy = 0f, dz = 0f;
            foreach (var w in _waves)
            {
                float k   = w.frequency;
                float dot = Vector2.Dot(w.direction, worldXZ);
                float phi = k * dot - w.speed * _waveTime + w.phase;
                float s   = Mathf.Sin(phi);
                float c   = Mathf.Cos(phi);

                dx += -w.direction.x * choppiness * w.amplitude * c;
                dz += -w.direction.y * choppiness * w.amplitude * c;
                dy +=  w.amplitude * s;
            }
            return new Vector3(dx, dy, dz);
        }

        /// <summary>Applies a sea state to scale wave amplitudes accordingly.</summary>
        public void ApplySeaState(SeaState state)
        {
            float heightForState = state switch
            {
                SeaState.Calm     => 0.25f,
                SeaState.Slight   => 0.75f,
                SeaState.Moderate => 1.5f,
                SeaState.Rough    => 3f,
                SeaState.VeryRough=> 5f,
                SeaState.HighSeas => 8f,
                _ => 0.5f
            };
            baseAmplitude = heightForState * 0.5f;
            RebuildWaves();
        }

        /// <summary>Updates wind parameters used to orient waves.</summary>
        public void SetWind(float speedMs, float directionDeg)
        {
            _windSpeed     = speedMs;
            _windDirection = directionDeg;
            baseAmplitude  = Mathf.Lerp(0.1f, 5f, _windSpeed / 30f);
            RebuildWaves();
        }

        /// <summary>Returns a snapshot of current wave conditions.</summary>
        public WaveConditions GetCurrentConditions()
        {
            float swh = 0f;
            foreach (var w in _waves) swh += w.amplitude;
            swh *= 2f; // significant wave height ≈ 2× amplitude sum

            return new WaveConditions
            {
                significantWaveHeight = swh,
                dominantPeriod        = _waves.Length > 0 ? (2f * Mathf.PI / _waves[0].frequency) / _waves[0].speed : 6f,
                waveDirection         = _windDirection,
                windSpeed             = _windSpeed,
                windDirection         = _windDirection,
                seaState              = SeaState.Calm // classified by manager
            };
        }
    }
}
