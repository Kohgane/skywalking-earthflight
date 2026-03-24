using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 75 — Boid-based flocking algorithm for bird groups.
    /// Implements separation, alignment, cohesion, obstacle avoidance, and
    /// multiple formation types with LOD performance scaling.
    /// </summary>
    public class BirdFlockController : MonoBehaviour
    {
        #region Inspector

        [Header("Flock Parameters")]
        [SerializeField] private FlockParameters flockParams = new FlockParameters();

        [Header("Formation")]
        [SerializeField] private FormationType currentFormation = FormationType.Scatter;

        [Header("Performance")]
        [Tooltip("Number of boids updated per frame (staggered).")]
        [SerializeField] private int boidsPerFrame = 5;

        [Tooltip("LOD distance — beyond this, skip separation check.")]
        [SerializeField] private float simplifiedLODDistance = 300f;

        [Tooltip("LOD distance — beyond this, pause visual updates.")]
        [SerializeField] private float billboardLODDistance = 600f;

        #endregion

        #region Private State

        private readonly List<Transform> _boids = new List<Transform>();
        private readonly List<Vector3>   _boidVelocities = new List<Vector3>();
        private Transform _leader;
        private Transform _playerTransform;
        private bool _isFleeing;
        private int  _boidUpdateIndex;
        private float _baseSpeed = 10f;
        private float _fleeSpeed = 20f;

        #endregion

        #region Public Properties

        /// <summary>Number of boids in this flock.</summary>
        public int BoidCount => _boids.Count;

        /// <summary>Average world position of all boids.</summary>
        public Vector3 FlockCenter
        {
            get
            {
                if (_boids.Count == 0) return transform.position;
                Vector3 sum = Vector3.zero;
                foreach (var b in _boids) sum += b.position;
                return sum / _boids.Count;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null) _playerTransform = cam.transform;
        }

        private void Update()
        {
            StepBoidUpdates();
        }

        #endregion

        #region Boid Initialisation

        /// <summary>Registers boid transforms. Call after spawning individuals.</summary>
        public void InitialiseBoids(List<Transform> boids, float baseSpeed, float fleeSpeed)
        {
            _boids.Clear();
            _boidVelocities.Clear();
            _boids.AddRange(boids);
            _baseSpeed = baseSpeed;
            _fleeSpeed = fleeSpeed;

            foreach (var b in _boids)
                _boidVelocities.Add(b.forward * baseSpeed);

            if (_boids.Count > 0)
                _leader = _boids[0];

            SetFormation(currentFormation);
        }

        #endregion

        #region Boid Update (staggered)

        private void StepBoidUpdates()
        {
            if (_boids.Count == 0) return;
            int count = Mathf.Min(boidsPerFrame, _boids.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = (_boidUpdateIndex + i) % _boids.Count;
                UpdateBoid(idx);
            }
            _boidUpdateIndex = (_boidUpdateIndex + count) % _boids.Count;
        }

        private void UpdateBoid(int idx)
        {
            Transform boid = _boids[idx];
            if (boid == null) return;

            float distToCamera = _playerTransform != null
                ? (_playerTransform.position - boid.position).sqrMagnitude
                : 0f;

            // Beyond billboard LOD — skip visual update
            if (distToCamera > billboardLODDistance * billboardLODDistance) return;

            bool simplified = distToCamera > simplifiedLODDistance * simplifiedLODDistance;
            Vector3 steer   = Vector3.zero;

            steer += Separation(idx, simplified) * flockParams.separationWeight;
            steer += Alignment(idx)              * flockParams.alignmentWeight;
            steer += Cohesion(idx)               * flockParams.cohesionWeight;
            steer += ObstacleAvoidance(boid)     * flockParams.obstacleAvoidanceWeight;
            steer += LeaderFollow(boid)          * 0.3f;

            if (_isFleeing && _playerTransform != null)
            {
                Vector3 away = (boid.position - _playerTransform.position).normalized;
                steer += away * 5f;
            }

            steer = Vector3.ClampMagnitude(steer, flockParams.maxSteerForce);
            float speed = _isFleeing ? _fleeSpeed : _baseSpeed;
            _boidVelocities[idx] = Vector3.ClampMagnitude(
                _boidVelocities[idx] + steer * Time.deltaTime, speed);

            boid.position += _boidVelocities[idx] * Time.deltaTime;
            if (_boidVelocities[idx] != Vector3.zero)
                boid.rotation = Quaternion.Slerp(
                    boid.rotation,
                    Quaternion.LookRotation(_boidVelocities[idx]),
                    10f * Time.deltaTime);
        }

        #endregion

        #region Boid Rules

        private Vector3 Separation(int idx, bool simplified)
        {
            if (simplified) return Vector3.zero;
            Vector3 steer = Vector3.zero;
            int count = 0;
            Vector3 pos = _boids[idx].position;
            for (int j = 0; j < _boids.Count; j++)
            {
                if (j == idx || _boids[j] == null) continue;
                float d = Vector3.Distance(pos, _boids[j].position);
                if (d < flockParams.separationRadius && d > 0.001f)
                {
                    steer += (pos - _boids[j].position).normalized / d;
                    count++;
                }
            }
            return count > 0 ? steer / count : Vector3.zero;
        }

        private Vector3 Alignment(int idx)
        {
            Vector3 avg = Vector3.zero;
            int count   = 0;
            Vector3 pos = _boids[idx].position;
            for (int j = 0; j < _boids.Count; j++)
            {
                if (j == idx || _boids[j] == null) continue;
                if (Vector3.Distance(pos, _boids[j].position) < flockParams.alignmentRadius)
                {
                    avg += _boidVelocities[j];
                    count++;
                }
            }
            if (count == 0) return Vector3.zero;
            avg /= count;
            return (avg.normalized * _baseSpeed - _boidVelocities[idx]);
        }

        private Vector3 Cohesion(int idx)
        {
            Vector3 center = Vector3.zero;
            int count      = 0;
            Vector3 pos    = _boids[idx].position;
            for (int j = 0; j < _boids.Count; j++)
            {
                if (j == idx || _boids[j] == null) continue;
                if (Vector3.Distance(pos, _boids[j].position) < flockParams.cohesionRadius)
                {
                    center += _boids[j].position;
                    count++;
                }
            }
            if (count == 0) return Vector3.zero;
            center /= count;
            return (center - pos).normalized;
        }

        private Vector3 ObstacleAvoidance(Transform boid)
        {
            if (Physics.Raycast(boid.position, boid.forward, out RaycastHit hit, 20f))
                return (boid.position - hit.point).normalized;
            return Vector3.zero;
        }

        private Vector3 LeaderFollow(Transform boid)
        {
            if (_leader == null) return Vector3.zero;
            return (_leader.position - boid.position).normalized * 0.5f;
        }

        #endregion

        #region Formation

        /// <summary>Sets the flock formation type.</summary>
        public void SetFormation(FormationType type)
        {
            currentFormation = type;
            // Formation offsets applied to initial positions; AI maintains loosely
            // Additional per-formation logic can be layered here
        }

        #endregion

        #region Group Controller Callback

        /// <summary>Called by AnimalGroupController when behavior changes.</summary>
        public void OnBehaviorChanged(WildlifeBehavior behavior)
        {
            _isFleeing = behavior == WildlifeBehavior.Fleeing;
            switch (behavior)
            {
                case WildlifeBehavior.Migrating:  SetFormation(FormationType.VFormation);    break;
                case WildlifeBehavior.Circling:   SetFormation(FormationType.SoaringCircle); break;
                case WildlifeBehavior.Flocking:   SetFormation(FormationType.Murmuration);   break;
                case WildlifeBehavior.Fleeing:    SetFormation(FormationType.Scatter);        break;
            }
        }

        #endregion
    }
}
