using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Wildlife
{
    /// <summary>
    /// Phase 53 — Specialized controller for bird flocks.
    ///
    /// <para>Handles V-formation flying with dynamic leader switching, soaring
    /// thermals, diving, murmuration effects for large starling-like flocks,
    /// altitude zone maintenance, and seasonal migration.</para>
    /// </summary>
    public class BirdFlockController : MonoBehaviour
    {
        #region Constants

        private const float ThermalOrbitRadius     = 150f;
        private const float ThermalOrbitSpeed      = 0.4f;   // rad/s
        private const float VFormationSpacing      = 5f;     // world units between birds
        private const float VFormationAngle        = 35f;    // degrees
        private const float LeaderSwitchInterval   = 15f;    // seconds
        private const float ScatterRadius          = 30f;    // player collision radius
        private const float MurmurationTwistSpeed  = 2f;

        #endregion

        #region Inspector

        [Header("Flock Configuration")]
        [SerializeField] private AnimalGroup flockData = new AnimalGroup();

        [Tooltip("Member transforms of this flock.")]
        [SerializeField] private List<Transform> members = new List<Transform>();

        [Tooltip("Whether this flock is currently in murmuration mode.")]
        [SerializeField] private bool murmurationMode = false;

        [Tooltip("Target altitude for soaring behavior.")]
        [SerializeField] private float soaringAltitude = 600f;

        [Tooltip("Altitude below which the flock stays (low-altitude birds).")]
        [SerializeField] private float maxFlightAltitude = 200f;

        [Header("Seasonal Migration")]
        [Tooltip("Direction of spring migration (normalized).")]
        [SerializeField] private Vector3 springMigrationDir = Vector3.forward;

        [Tooltip("Whether this flock is currently migrating.")]
        [SerializeField] private bool isMigrating = false;

        #endregion

        #region Public Properties

        /// <summary>Index of the current leader in the <see cref="members"/> list.</summary>
        public int LeaderIndex { get; private set; }

        /// <summary>Whether the flock is performing a thermal soar.</summary>
        public bool IsSoaring { get; private set; }

        #endregion

        #region Private State

        private Transform _playerTransform;
        private float     _leaderTimer;
        private float     _thermalAngle;
        private float     _murmurationPhase;
        private Vector3   _migrationDir;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _playerTransform = Camera.main != null ? Camera.main.transform : null;
            _migrationDir    = springMigrationDir.normalized;

            if (murmurationMode && members.Count >= 50)
                StartCoroutine(MurmurationRoutine());
            else
                StartCoroutine(FlockRoutine());
        }

        private void Update()
        {
            CheckPlayerScatter();
        }

        #endregion

        #region Public API

        /// <summary>Initialises the flock with data and member transforms.</summary>
        public void Initialise(AnimalGroup data, List<Transform> flockMembers, bool enableMurmuration = false)
        {
            flockData       = data;
            murmurationMode = enableMurmuration;
            members.Clear();
            members.AddRange(flockMembers);
        }

        /// <summary>Triggers seasonal migration in the given direction.</summary>
        public void BeginMigration(Vector3 direction)
        {
            _migrationDir = direction.normalized;
            isMigrating   = true;
            flockData.currentBehavior = AnimalBehavior.Migrating;
        }

        /// <summary>Ends migration and returns to normal patrol behavior.</summary>
        public void EndMigration()
        {
            isMigrating = false;
            flockData.currentBehavior = AnimalBehavior.Flying;
        }

        #endregion

        #region Flock Routine

        private IEnumerator FlockRoutine()
        {
            while (true)
            {
                _leaderTimer += BehaviorTickTime();

                if (_leaderTimer >= LeaderSwitchInterval)
                {
                    _leaderTimer = 0f;
                    SwitchLeader();
                }

                if (isMigrating)
                    FlyMigration();
                else if (IsSoaring)
                    FlyThermal();
                else
                    FlyVFormation();

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void FlyVFormation()
        {
            if (members.Count == 0) return;

            Transform leader = LeaderIndex < members.Count ? members[LeaderIndex] : members[0];
            if (leader == null) return;

            // Move leader forward
            float speed = flockData.species != null ? flockData.species.baseSpeed : 10f;
            leader.position += leader.forward * speed * 0.1f;
            leader.position = ClampToAltitude(leader.position);

            // Position followers in V-shape
            for (int i = 0; i < members.Count; i++)
            {
                if (i == LeaderIndex || members[i] == null) continue;

                int side = (i % 2 == 0) ? 1 : -1;
                int rank = (i + 1) / 2;
                Vector3 offset = Quaternion.Euler(0, side * VFormationAngle, 0)
                    * (-leader.forward * VFormationSpacing * rank)
                    + Vector3.right * (side * VFormationSpacing * 0.5f * rank);

                members[i].position = Vector3.Lerp(members[i].position,
                    leader.position + offset, Time.deltaTime * 2f);

                if (leader.forward != Vector3.zero)
                    members[i].rotation = Quaternion.Slerp(members[i].rotation,
                        Quaternion.LookRotation(leader.forward), Time.deltaTime * 3f);
            }

            flockData.centerPosition = leader.position;
        }

        private void FlyThermal()
        {
            _thermalAngle += ThermalOrbitSpeed * Time.deltaTime;
            Vector3 center = flockData.centerPosition;

            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] == null) continue;
                float angle = _thermalAngle + i * (Mathf.PI * 2f / Mathf.Max(1, members.Count));
                Vector3 target = center + new Vector3(
                    Mathf.Cos(angle) * ThermalOrbitRadius,
                    Mathf.Sin(angle * 0.1f) * 20f,
                    Mathf.Sin(angle) * ThermalOrbitRadius);
                members[i].position = Vector3.Lerp(members[i].position, target, Time.deltaTime * 2f);
            }
        }

        private void FlyMigration()
        {
            if (members.Count == 0) return;
            float speed = flockData.species != null ? flockData.species.baseSpeed : 12f;
            foreach (var m in members)
            {
                if (m == null) continue;
                m.position += _migrationDir * speed * 0.1f;
                m.position  = ClampToAltitude(m.position);
            }
            flockData.centerPosition += _migrationDir * speed * 0.1f;
        }

        #endregion

        #region Murmuration

        private IEnumerator MurmurationRoutine()
        {
            while (true)
            {
                _murmurationPhase += MurmurationTwistSpeed * Time.deltaTime;
                Vector3 center = flockData.centerPosition;

                for (int i = 0; i < members.Count; i++)
                {
                    if (members[i] == null) continue;
                    float t     = (float)i / members.Count;
                    float angle = _murmurationPhase + t * Mathf.PI * 8f;
                    Vector3 wave = new Vector3(
                        Mathf.Sin(angle) * 80f,
                        Mathf.Cos(angle * 0.7f) * 30f,
                        Mathf.Cos(angle) * 80f);
                    members[i].position = Vector3.Lerp(members[i].position, center + wave, Time.deltaTime * 3f);
                }

                // Drift the cloud center slowly
                center += new Vector3(Mathf.Sin(_murmurationPhase * 0.1f), 0f, Mathf.Cos(_murmurationPhase * 0.1f));
                flockData.centerPosition = center;

                yield return null;
            }
        }

        #endregion

        #region Player Scatter

        private void CheckPlayerScatter()
        {
            if (_playerTransform == null) return;
            if (Vector3.Distance(transform.position, _playerTransform.position) < ScatterRadius)
                ScatterFlock();
        }

        private void ScatterFlock()
        {
            foreach (var m in members)
            {
                if (m == null) continue;
                Vector3 away = (m.position - _playerTransform.position).normalized;
                m.position += away * (flockData.species != null ? flockData.species.baseSpeed : 10f)
                              * Time.deltaTime * 5f;
            }
        }

        #endregion

        #region Helpers

        private void SwitchLeader()
        {
            if (members.Count <= 1) return;
            LeaderIndex = (LeaderIndex + 1) % members.Count;
        }

        private Vector3 ClampToAltitude(Vector3 pos)
        {
            if (IsSoaring)
                pos.y = Mathf.Lerp(pos.y, soaringAltitude, Time.deltaTime * 0.5f);
            else
                pos.y = Mathf.Clamp(pos.y, 10f, maxFlightAltitude);
            return pos;
        }

        private static float BehaviorTickTime() => 0.1f;

        #endregion
    }
}
