using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Specialized controller for underwater and ocean-surface animals.
    ///
    /// <para>Supports whale breaching/spouting/diving, dolphin pod leaping and
    /// bow-riding, fish schooling with bait-ball formation, shark patrol, sea turtle
    /// gentle gliding, and jellyfish passive drift.  Ocean-current influence and
    /// depth-based movement are included.</para>
    /// </summary>
    public class MarineLifeController : MonoBehaviour
    {
        #region Constants

        private const float BreachCooldown      = 45f;   // seconds between whale breaches
        private const float DolphinLeapInterval = 4f;
        private const float SharkCruiseSpeed    = 4f;
        private const float JellyfishDriftSpeed = 0.5f;
        private const float BaitBallRadius      = 25f;
        private const float SurfaceY            = 0f;    // world-space sea level
        private const float SplashParticleLife  = 3f;

        #endregion

        #region Inspector

        [Header("Marine Group Data")]
        [SerializeField] private AnimalGroup groupData = new AnimalGroup();

        [Header("Marine Behavior")]
        [Tooltip("Current high-level marine behavior mode.")]
        [SerializeField] private MarineMode mode = MarineMode.Schooling;

        [Tooltip("Depth below sea level at which this group normally swims.")]
        [SerializeField] private float swimDepth = -10f;

        [Tooltip("Ocean current direction and magnitude.")]
        [SerializeField] private Vector3 currentDirection = new Vector3(0.3f, 0f, 0.1f);

        [Header("Effects")]
        [Tooltip("Particle system instantiated on whale breach. Optional.")]
        [SerializeField] private GameObject splashPrefab;

        #endregion

        #region Enumerations

        /// <summary>High-level marine locomotion mode.</summary>
        public enum MarineMode
        {
            Schooling,
            WhalePatrol,
            DolphinPod,
            SharkCruise,
            TurtleGlide,
            JellyfishDrift,
            CoralReef
        }

        #endregion

        #region Public Properties

        /// <summary>The data object describing this marine group.</summary>
        public AnimalGroup GroupData => groupData;

        #endregion

        #region Private State

        private readonly List<Transform> _members = new List<Transform>();
        private Transform _playerTransform;

        private float _breachTimer;
        private float _dolphinLeapTimer;
        private float _schoolAngle;
        private bool  _isBreaching;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : null;
            StartCoroutine(MarineBehaviorRoutine());
        }

        #endregion

        #region Public API

        /// <summary>Initialises the controller with group data and member transforms.</summary>
        public void Initialise(AnimalGroup data, List<Transform> marineMembers, MarineMode marineMode)
        {
            groupData = data;
            mode      = marineMode;
            _members.Clear();
            _members.AddRange(marineMembers);
        }

        #endregion

        #region Marine Behavior Routine

        private IEnumerator MarineBehaviorRoutine()
        {
            while (true)
            {
                switch (mode)
                {
                    case MarineMode.Schooling:     UpdateSchool();    break;
                    case MarineMode.WhalePatrol:   UpdateWhale();     break;
                    case MarineMode.DolphinPod:    UpdateDolphins();  break;
                    case MarineMode.SharkCruise:   UpdateShark();     break;
                    case MarineMode.TurtleGlide:   UpdateTurtle();    break;
                    case MarineMode.JellyfishDrift:UpdateJellyfish(); break;
                    case MarineMode.CoralReef:                        break; // stationary
                }

                ApplyOceanCurrent();
                yield return new WaitForSeconds(0.05f);
            }
        }

        #endregion

        #region Species-Specific Behaviors

        // ── Fish School ───────────────────────────────────────────────────────────

        private void UpdateSchool()
        {
            _schoolAngle += Time.deltaTime * 0.5f;
            Vector3 center = groupData.centerPosition;
            float   radius = groupData.groupRadius;

            bool baitBall = IsPlayerNearby(100f);

            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i] == null) continue;
                float r   = baitBall ? BaitBallRadius : radius;
                float ang = _schoolAngle + i * (Mathf.PI * 2f / Mathf.Max(1, _members.Count));
                Vector3 target = center + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                target.y = swimDepth + Mathf.Sin(ang * 2f) * 3f;
                _members[i].position = Vector3.Lerp(_members[i].position, target, Time.deltaTime * 2f);
            }

            // Drift center slowly
            groupData.centerPosition += currentDirection * Time.deltaTime;
        }

        // ── Whale ─────────────────────────────────────────────────────────────────

        private void UpdateWhale()
        {
            if (_members.Count == 0) return;
            Transform whale = _members[0];
            if (whale == null) return;

            float speed = groupData.species != null ? groupData.species.baseSpeed : 3f;
            whale.position += whale.forward * speed * Time.deltaTime;
            whale.position = new Vector3(whale.position.x, swimDepth, whale.position.z);

            _breachTimer += Time.deltaTime;
            if (!_isBreaching && _breachTimer >= BreachCooldown)
            {
                _breachTimer = 0f;
                StartCoroutine(WhaleBreach(whale));
            }

            groupData.centerPosition = whale.position;
        }

        private IEnumerator WhaleBreach(Transform whale)
        {
            _isBreaching = true;
            float elapsed = 0f;
            float duration = 4f;
            Vector3 startPos = whale.position;
            Vector3 peakPos  = startPos + Vector3.up * 25f;

            // Rise
            while (elapsed < duration * 0.4f)
            {
                elapsed += Time.deltaTime;
                whale.position = Vector3.Lerp(startPos, peakPos, elapsed / (duration * 0.4f));
                yield return null;
            }

            SpawnSplash(whale.position);

            // Fall
            elapsed = 0f;
            while (elapsed < duration * 0.6f)
            {
                elapsed += Time.deltaTime;
                whale.position = Vector3.Lerp(peakPos, startPos, elapsed / (duration * 0.6f));
                yield return null;
            }

            _isBreaching = false;
        }

        // ── Dolphins ──────────────────────────────────────────────────────────────

        private void UpdateDolphins()
        {
            float speed = groupData.species != null ? groupData.species.baseSpeed : 8f;
            _dolphinLeapTimer += Time.deltaTime;

            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i] == null) continue;
                _members[i].position += _members[i].forward * speed * Time.deltaTime;
                _members[i].position = new Vector3(_members[i].position.x,
                    swimDepth + Mathf.Sin(Time.time * 2f + i) * 3f, _members[i].position.z);
            }

            if (_dolphinLeapTimer >= DolphinLeapInterval)
            {
                _dolphinLeapTimer = 0f;
                if (_members.Count > 0 && _members[0] != null)
                    StartCoroutine(DolphinLeap(_members[Random.Range(0, _members.Count)]));
            }
        }

        private IEnumerator DolphinLeap(Transform dolphin)
        {
            if (dolphin == null) yield break;
            float elapsed = 0f;
            float apex    = SurfaceY + 5f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed;
                float y  = Mathf.Lerp(swimDepth, apex, Mathf.Sin(t * Mathf.PI));
                dolphin.position = new Vector3(dolphin.position.x, y, dolphin.position.z);
                yield return null;
            }
            dolphin.position = new Vector3(dolphin.position.x, swimDepth, dolphin.position.z);
        }

        // ── Shark ─────────────────────────────────────────────────────────────────

        private void UpdateShark()
        {
            if (_members.Count == 0) return;
            Transform shark = _members[0];
            if (shark == null) return;

            shark.position += shark.forward * SharkCruiseSpeed * Time.deltaTime;
            shark.position = new Vector3(shark.position.x, swimDepth, shark.position.z);

            // Slow, wide turns
            shark.Rotate(0f, Time.deltaTime * 5f, 0f);
            groupData.centerPosition = shark.position;
        }

        // ── Sea Turtle ────────────────────────────────────────────────────────────

        private void UpdateTurtle()
        {
            float speed = groupData.species != null ? groupData.species.baseSpeed : 1.5f;
            foreach (var m in _members)
            {
                if (m == null) continue;
                m.position += m.forward * speed * Time.deltaTime;
                m.position  = new Vector3(m.position.x, swimDepth + Mathf.Sin(Time.time * 0.3f),
                    m.position.z);
            }
        }

        // ── Jellyfish ─────────────────────────────────────────────────────────────

        private void UpdateJellyfish()
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i] == null) continue;
                _members[i].position += currentDirection * JellyfishDriftSpeed * Time.deltaTime;
                _members[i].position += Vector3.up * Mathf.Sin(Time.time * 0.5f + i * 0.7f) * 0.01f;
            }
        }

        #endregion

        #region Ocean Current

        private void ApplyOceanCurrent()
        {
            if (mode == MarineMode.CoralReef) return;
            groupData.centerPosition += currentDirection * Time.deltaTime * 0.2f;
        }

        #endregion

        #region Helpers

        private bool IsPlayerNearby(float radius)
        {
            if (_playerTransform == null) return false;
            return Vector3.Distance(groupData.centerPosition, _playerTransform.position) < radius;
        }

        private void SpawnSplash(Vector3 position)
        {
            if (splashPrefab == null) return;
            GameObject splash = Instantiate(splashPrefab, position, Quaternion.identity);
            Destroy(splash, SplashParticleLife);
        }

        #endregion
    }
}
