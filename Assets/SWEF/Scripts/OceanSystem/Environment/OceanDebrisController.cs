// OceanDebrisController.cs — Phase 117: Advanced Ocean & Maritime System
// Floating debris: icebergs, containers, oil slicks, seaweed, wreckage.
// Namespace: SWEF.OceanSystem

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.OceanSystem
{
    /// <summary>
    /// Phase 117 — Spawns and manages floating ocean debris objects.
    /// Objects ride the wave surface via buoyancy offset queries.
    /// </summary>
    public class OceanDebrisController : MonoBehaviour
    {
        // ── Debris Instance ───────────────────────────────────────────────────────

        [Serializable]
        private class DebrisInstance
        {
            public DebrisType type;
            public Vector3    position;
            public float      driftSpeed;
            public float      driftDirection;
            public GameObject gameObject;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Spawning")]
        [SerializeField] private int   maxDebrisObjects = 30;
        [SerializeField] private float spawnRadius      = 20000f;
        [SerializeField] private float despawnRadius    = 25000f;

        [Header("Prefabs")]
        [SerializeField] private GameObject icebergPrefab;
        [SerializeField] private GameObject containerPrefab;
        [SerializeField] private GameObject oilSlickPrefab;
        [SerializeField] private GameObject seaweedPrefab;

        [Header("Probabilities (0-1)")]
        [SerializeField] private float icebergProbability   = 0.05f;
        [SerializeField] private float containerProbability = 0.25f;
        [SerializeField] private float oilSlickProbability  = 0.20f;
        [SerializeField] private float seaweedProbability   = 0.50f;

        // ── Private state ─────────────────────────────────────────────────────────

        private readonly List<DebrisInstance> _debris = new List<DebrisInstance>();
        private float _spawnTimer;
        private const float SpawnInterval = 5f;

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Raised when new debris is spawned.</summary>
        public event Action<DebrisType, Vector3> OnDebrisSpawned;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                TrySpawnDebris();
            }

            UpdateDebrisPositions();
            DespawnDistantDebris();
        }

        // ── Spawning ──────────────────────────────────────────────────────────────

        private void TrySpawnDebris()
        {
            if (_debris.Count >= maxDebrisObjects) return;

            float r     = DebrisTypeByProbability();
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist  = UnityEngine.Random.Range(spawnRadius * 0.5f, spawnRadius);
            var   pos   = new Vector3(Mathf.Sin(angle) * dist, 0f, Mathf.Cos(angle) * dist);

            var type = SelectDebrisType();
            var inst = new DebrisInstance
            {
                type          = type,
                position      = pos,
                driftSpeed    = UnityEngine.Random.Range(0.1f, 0.5f),
                driftDirection = UnityEngine.Random.Range(0f, 360f)
            };
            _debris.Add(inst);
            OnDebrisSpawned?.Invoke(type, pos);
        }

        private float DebrisTypeByProbability() => UnityEngine.Random.value;

        private DebrisType SelectDebrisType()
        {
            float r = UnityEngine.Random.value;
            if (r < icebergProbability)   return DebrisType.Iceberg;
            if (r < icebergProbability + containerProbability) return DebrisType.Container;
            if (r < icebergProbability + containerProbability + oilSlickProbability) return DebrisType.OilSlick;
            if (r < icebergProbability + containerProbability + oilSlickProbability + seaweedProbability) return DebrisType.SeaweedPatch;
            return DebrisType.Wreckage;
        }

        // ── Position Update ───────────────────────────────────────────────────────

        private void UpdateDebrisPositions()
        {
            var mgr = OceanSystemManager.Instance;
            foreach (var d in _debris)
            {
                // Drift
                float rad = d.driftDirection * Mathf.Deg2Rad;
                d.position += new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * d.driftSpeed * Time.deltaTime;

                // Ride wave surface
                if (mgr != null)
                    d.position.y = mgr.GetSurfaceHeight(new Vector2(d.position.x, d.position.z));

                if (d.gameObject != null)
                    d.gameObject.transform.position = d.position;
            }
        }

        private void DespawnDistantDebris()
        {
            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                if (new Vector2(_debris[i].position.x, _debris[i].position.z).magnitude > despawnRadius)
                {
                    if (_debris[i].gameObject != null) Destroy(_debris[i].gameObject);
                    _debris.RemoveAt(i);
                }
            }
        }
    }
}
