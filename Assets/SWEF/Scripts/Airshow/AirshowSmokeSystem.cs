// Phase 73 — Flight Formation Display & Airshow System
// Assets/SWEF/Scripts/Airshow/AirshowSmokeSystem.cs
using System.Collections.Generic;
using UnityEngine;
using SWEF.Contrail;
using SWEF.Weather;

namespace SWEF.Airshow
{
    /// <summary>
    /// Manages colored smoke trails for all airshow performers.
    /// Pools particle systems per performer, applies wind drift, and
    /// integrates with <see cref="ContrailManager"/> for the rendering pipeline.
    /// </summary>
    public class AirshowSmokeSystem : MonoBehaviour
    {
        #region Inspector
        [Header("References")]
        [Tooltip("Template ParticleSystem used for pooled smoke trails.")]
        [SerializeField] private ParticleSystem smokeTemplate;

        [Header("Configuration")]
        [SerializeField] private float baseParticleSize = 2f;
        [SerializeField] private float speedWidthScale = 0.02f;   // additional width per m/s
        #endregion

        #region Public State
        /// <summary>Number of currently active smoke trails.</summary>
        public int ActiveTrailCount => _activeTrails.Count;
        #endregion

        #region Private
        private struct SmokeTrailData
        {
            public ParticleSystem particles;
            public Color color;
            public bool active;
        }

        private readonly Dictionary<int, SmokeTrailData> _activeTrails =
            new Dictionary<int, SmokeTrailData>();

        private AirshowConfig _config;
        #endregion

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _config = AirshowManager.Instance != null
                ? AirshowManager.Instance.Config
                : new AirshowConfig();
        }

        private void Update()
        {
            ApplyWindDrift();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Enables and colorizes the smoke trail for a performer slot.</summary>
        public void EnableSmoke(int performerSlot, SmokeColor color)
        {
            Color c = SmokeColorToColor(color);
            if (_activeTrails.TryGetValue(performerSlot, out SmokeTrailData data))
            {
                data.color  = c;
                data.active = true;
                ApplyColorToParticles(data.particles, c);
                data.particles.Play();
                _activeTrails[performerSlot] = data;
            }
            else
            {
                ParticleSystem ps = SpawnSmokeParticles(c);
                _activeTrails[performerSlot] = new SmokeTrailData
                {
                    particles = ps,
                    color     = c,
                    active    = true
                };
            }
        }

        /// <summary>Disables the smoke trail for a performer slot.</summary>
        public void DisableSmoke(int performerSlot)
        {
            if (!_activeTrails.TryGetValue(performerSlot, out SmokeTrailData data)) return;
            data.active = false;
            data.particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _activeTrails[performerSlot] = data;
        }

        /// <summary>Sets all active smoke trails to the same color simultaneously.</summary>
        public void SetAllSmokeColor(SmokeColor color)
        {
            Color c = SmokeColorToColor(color);
            var keys = new List<int>(_activeTrails.Keys);
            foreach (int slot in keys)
            {
                SmokeTrailData data = _activeTrails[slot];
                data.color = c;
                ApplyColorToParticles(data.particles, c);
                _activeTrails[slot] = data;
            }
        }

        /// <summary>Emits a short burst of extra smoke for emphasis on a performer's trail.</summary>
        public void PulseSmoke(int slot)
        {
            if (!_activeTrails.TryGetValue(slot, out SmokeTrailData data)) return;
            if (data.particles == null) return;
            data.particles.Emit(50);
        }

        /// <summary>Fades out and clears all active smoke trails.</summary>
        public void ClearAllSmoke()
        {
            foreach (SmokeTrailData data in _activeTrails.Values)
            {
                if (data.particles != null)
                    data.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _activeTrails.Clear();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private ParticleSystem SpawnSmokeParticles(Color color)
        {
            ParticleSystem ps;
            if (smokeTemplate != null)
            {
                ps = Instantiate(smokeTemplate, transform);
            }
            else
            {
                var go = new GameObject("SmokeTrail");
                go.transform.SetParent(transform);
                ps = go.AddComponent<ParticleSystem>();
            }

            ApplyColorToParticles(ps, color);
            ConfigureLifetime(ps);
            ps.Play();
            return ps;
        }

        private void ConfigureLifetime(ParticleSystem ps)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = _config.smokeTrailLifetime;
            main.startSize     = baseParticleSize * _config.smokeDensity;
        }

        private static void ApplyColorToParticles(ParticleSystem ps, Color color)
        {
            if (ps == null) return;
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
        }

        private void ApplyWindDrift()
        {
            if (_activeTrails.Count == 0) return;

            Vector3 windVelocity = Vector3.zero;
            WeatherManager wm = WeatherManager.Instance;
            if (wm != null)
                windVelocity = wm.CurrentWind.direction * wm.CurrentWind.speed;

            foreach (SmokeTrailData data in _activeTrails.Values)
            {
                if (data.particles == null || !data.active) continue;
                ParticleSystem.VelocityOverLifetimeModule vel = data.particles.velocityOverLifetime;
                vel.enabled = true;
                vel.x = windVelocity.x * 0.1f;
                vel.y = windVelocity.y * 0.1f;
                vel.z = windVelocity.z * 0.1f;
            }
        }

        private static Color SmokeColorToColor(SmokeColor sc)
        {
            return sc switch
            {
                SmokeColor.Red    => Color.red,
                SmokeColor.Blue   => Color.blue,
                SmokeColor.Green  => Color.green,
                SmokeColor.Yellow => Color.yellow,
                SmokeColor.Orange => new Color(1f, 0.5f, 0f),
                SmokeColor.Purple => new Color(0.5f, 0f, 0.5f),
                SmokeColor.Pink   => new Color(1f, 0.41f, 0.71f),
                SmokeColor.Black  => Color.black,
                _                 => Color.white
            };
        }
    }
}
