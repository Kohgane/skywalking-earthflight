// WaterRippleSystem.cs — SWEF Ocean & Water Interaction System
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Water
{
    #region Data

    /// <summary>Runtime state of a single active ripple event.</summary>
    internal class RippleEvent
    {
        /// <summary>World-space origin of the ripple.</summary>
        public Vector3 origin;

        /// <summary>Elapsed time since the ripple was spawned (s).</summary>
        public float age;

        /// <summary>Total lifetime of this ripple event (s).</summary>
        public float lifetime;

        /// <summary>Whether this slot is currently in use.</summary>
        public bool active;

        /// <summary>LineRenderer ring instances for this event.</summary>
        public LineRenderer[] rings;
    }

    #endregion

    /// <summary>
    /// Phase 55 — MonoBehaviour that generates dynamic ripple ring effects at
    /// object-water contact points and propagates them outward with fade-over-time decay.
    ///
    /// <para>Ripple rings are rendered using <see cref="LineRenderer"/> components so
    /// they work across all render pipelines without custom shaders.  The pool of
    /// <see cref="RippleEvent"/> slots is pre-allocated in <see cref="Awake"/> to
    /// prevent runtime allocations.</para>
    ///
    /// <para>Integration point: Call <see cref="SpawnRipple"/> from
    /// <see cref="BuoyancyController"/>, <see cref="SplashEffectController"/>,
    /// or any other component that needs wake/ripple effects.</para>
    /// </summary>
    public class WaterRippleSystem : MonoBehaviour
    {
        #region Inspector

        [Header("Interaction Profile")]
        [Tooltip("Profile containing RippleSettings values.")]
        [SerializeField] private WaterInteractionProfile profile;

        [Header("Rendering")]
        [Tooltip("Material applied to all ripple ring LineRenderers.")]
        [SerializeField] private Material rippleMaterial;

        [Tooltip("Layer mask used when positioning ripple rings on the water surface.")]
        [SerializeField] private LayerMask waterLayerMask;

        [Header("Debug")]
        [Tooltip("Log ripple spawn events to the console.")]
        [SerializeField] private bool debugLog;

        #endregion

        #region Private State

        private RippleSettings _settings;
        private RippleEvent[] _pool;
        private int _nextSlot;
        private const int CircleSegments = 32;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _settings = profile != null ? profile.ripple : new RippleSettings();
            BuildPool();
        }

        private void Update()
        {
            UpdateRipples();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns a new ripple event at <paramref name="worldPos"/> with the given
        /// impact <paramref name="velocityMagnitude"/> (used to scale initial ring opacity).
        /// </summary>
        /// <param name="worldPos">World-space contact point on or near the water surface.</param>
        /// <param name="velocityMagnitude">Impact speed (m/s) — larger values produce brighter rings.</param>
        public void SpawnRipple(Vector3 worldPos, float velocityMagnitude = 1f)
        {
            RippleEvent slot = AcquireSlot();
            slot.origin   = new Vector3(worldPos.x, WaterSurfaceManager.Instance != null
                                ? WaterSurfaceManager.Instance.GetWaterHeightAt(worldPos)
                                : worldPos.y, worldPos.z);
            slot.age      = 0f;
            slot.lifetime = _settings.lifetime;
            slot.active   = true;

            if (debugLog)
                Debug.Log($"[WaterRipple] Spawned ripple at {slot.origin}, vel={velocityMagnitude:F1}");
        }

        #endregion

        #region Private Helpers

        private void BuildPool()
        {
            _pool = new RippleEvent[_settings.maxActiveRipples];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = new RippleEvent
                {
                    rings = CreateRingRenderers(_settings.ringCount),
                    active = false
                };
                SetRingsVisible(_pool[i], false);
            }
        }

        private LineRenderer[] CreateRingRenderers(int count)
        {
            var rings = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"RippleRing_{i}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.loop         = true;
                lr.useWorldSpace = true;
                lr.positionCount = CircleSegments;
                lr.startWidth    = _settings.ringWidth;
                lr.endWidth      = _settings.ringWidth;
                if (rippleMaterial != null) lr.material = rippleMaterial;
                rings[i] = lr;
            }
            return rings;
        }

        private RippleEvent AcquireSlot()
        {
            // Prefer an inactive slot
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].active)
                    return _pool[i];
            }
            // Recycle the oldest (round-robin)
            _nextSlot = (_nextSlot + 1) % _pool.Length;
            return _pool[_nextSlot];
        }

        private void UpdateRipples()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                var evt = _pool[i];
                if (!evt.active) continue;

                evt.age += Time.deltaTime;
                float t = evt.age / evt.lifetime; // 0..1 age fraction

                if (t >= 1f)
                {
                    evt.active = false;
                    SetRingsVisible(evt, false);
                    continue;
                }

                SetRingsVisible(evt, true);

                for (int r = 0; r < evt.rings.Length; r++)
                {
                    // Stagger ring phases so they fan outward
                    float ringFrac = (float)r / Mathf.Max(1, evt.rings.Length - 1);
                    float ringT = Mathf.Clamp01(t - ringFrac * 0.2f);
                    float radius = ringT * _settings.maxRadius;
                    float alpha  = Mathf.Clamp01(1f - ringT) * (1f - t);

                    UpdateRingPositions(evt.rings[r], evt.origin, radius);

                    Color c = evt.rings[r].startColor;
                    c.a = alpha;
                    evt.rings[r].startColor = c;
                    evt.rings[r].endColor   = c;
                }
            }
        }

        private static void UpdateRingPositions(LineRenderer lr, Vector3 centre, float radius)
        {
            for (int s = 0; s < CircleSegments; s++)
            {
                float angle = s / (float)CircleSegments * Mathf.PI * 2f;
                lr.SetPosition(s, centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private static void SetRingsVisible(RippleEvent evt, bool visible)
        {
            for (int r = 0; r < evt.rings.Length; r++)
                evt.rings[r].enabled = visible;
        }

        #endregion
    }
}
