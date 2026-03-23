// EscortMission.cs — SWEF Flight Formation & Wingman AI System (Phase 63)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Formation
{
    /// <summary>
    /// Describes the lifecycle status of an <see cref="EscortMission"/>.
    /// </summary>
    public enum MissionStatus
    {
        /// <summary>Mission has not yet been started.</summary>
        NotStarted,

        /// <summary>Mission is currently active.</summary>
        InProgress,

        /// <summary>All objectives were met; mission ended with success.</summary>
        Succeeded,

        /// <summary>A failure condition was triggered; mission has ended.</summary>
        Failed,
    }

    /// <summary>
    /// MonoBehaviour that manages an escort-type mission where the player
    /// and wingmen protect a <see cref="escortTarget"/> from threats.
    /// <para>
    /// Every frame it scans for colliders inside <see cref="escortRadius"/>;
    /// any new threat that enters causes a <see cref="WingmanAI"/> to be
    /// commanded to intercept via <see cref="WingmanAI.CommandAttack"/>.
    /// </para>
    /// </summary>
    public sealed class EscortMission : MonoBehaviour
    {
        #region Events

        /// <summary>Raised when the mission transitions to <see cref="MissionStatus.InProgress"/>.</summary>
        public event Action OnMissionStart;

        /// <summary>
        /// Raised when the mission ends.
        /// The <see langword="bool"/> parameter is <see langword="true"/> on success,
        /// <see langword="false"/> on failure.
        /// </summary>
        public event Action<bool> OnMissionEnd;

        /// <summary>
        /// Raised every frame while the mission is in progress, passing the
        /// remaining time in seconds.
        /// </summary>
        public event Action<float> OnTimeUpdate;

        #endregion

        #region Inspector

        [Header("Target")]
        [Tooltip("The VIP, cargo, or asset the escort mission is protecting.")]
        [SerializeField] private Transform _escortTarget;

        [Header("Mission Parameters")]
        [Tooltip("Radius in metres around the escort target within which threats are detected.")]
        [SerializeField] private float escortRadius = 100f;

        [Tooltip("Total mission time limit in seconds (default 5 minutes).")]
        [SerializeField] private float missionTimeLimit = 300f;

        [Tooltip("Number of hits the escort target may absorb before the mission fails.")]
        [SerializeField] private int maxAllowedDamage = 3;

        [Header("Threat Detection")]
        [Tooltip("Layer mask used when scanning for threats inside the escort radius.")]
        [SerializeField] private LayerMask threatLayerMask;

        #endregion

        #region Runtime State

        /// <summary>Current lifecycle status of this mission.</summary>
        public MissionStatus status { get; private set; } = MissionStatus.NotStarted;

        /// <summary>Remaining time in seconds (valid while <see cref="status"/> is InProgress).</summary>
        public float RemainingTime { get; private set; }

        /// <summary>Number of hits the escort target has received so far.</summary>
        public int DamageReceived { get; private set; }

        /// <summary>The VIP, cargo, or asset the escort mission is protecting.</summary>
        public Transform escortTarget
        {
            get => _escortTarget;
            set => _escortTarget = value;
        }

        private readonly HashSet<Collider> _knownThreats = new HashSet<Collider>();

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (status != MissionStatus.InProgress) return;

            RemainingTime -= Time.deltaTime;
            OnTimeUpdate?.Invoke(RemainingTime);

            if (RemainingTime <= 0f)
            {
                CompleteMission();
                return;
            }

            ScanForThreats();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Starts the escort mission, resets counters, and transitions to
        /// <see cref="MissionStatus.InProgress"/>.
        /// Does nothing if the mission is already in progress or finished.
        /// </summary>
        public void StartMission()
        {
            if (status != MissionStatus.NotStarted) return;

            RemainingTime  = missionTimeLimit;
            DamageReceived = 0;
            _knownThreats.Clear();
            status = MissionStatus.InProgress;

            // Command all wingmen into escort mode.
            if (FormationManager.Instance != null && escortTarget != null)
            {
                foreach (WingmanAI w in FormationManager.Instance.wingmen)
                    w.CommandEscort(escortTarget);
            }

            OnMissionStart?.Invoke();
        }

        /// <summary>
        /// Marks the mission as <see cref="MissionStatus.Succeeded"/> and fires
        /// <see cref="OnMissionEnd"/> with <see langword="true"/>.
        /// </summary>
        public void CompleteMission()
        {
            if (status != MissionStatus.InProgress) return;
            status = MissionStatus.Succeeded;
            RecallWingmen();
            OnMissionEnd?.Invoke(true);
        }

        /// <summary>
        /// Marks the mission as <see cref="MissionStatus.Failed"/> and fires
        /// <see cref="OnMissionEnd"/> with <see langword="false"/>.
        /// </summary>
        public void FailMission()
        {
            if (status != MissionStatus.InProgress) return;
            status = MissionStatus.Failed;
            RecallWingmen();
            OnMissionEnd?.Invoke(false);
        }

        /// <summary>
        /// Registers a hit on the escort target.  If hits exceed
        /// <see cref="maxAllowedDamage"/> the mission fails automatically.
        /// </summary>
        public void RegisterHit()
        {
            if (status != MissionStatus.InProgress) return;
            DamageReceived++;
            if (DamageReceived >= maxAllowedDamage)
                FailMission();
        }

        #endregion

        #region Private Helpers

        private void ScanForThreats()
        {
            if (escortTarget == null) return;

            Collider[] hits = Physics.OverlapSphere(
                escortTarget.position, escortRadius, threatLayerMask);

            FormationManager mgr = FormationManager.Instance;

            foreach (Collider c in hits)
            {
                if (_knownThreats.Contains(c)) continue;
                _knownThreats.Add(c);

                // Assign the first available wingman to intercept.
                if (mgr != null)
                {
                    foreach (WingmanAI w in mgr.wingmen)
                    {
                        if (w.currentState == WingmanState.Following ||
                            w.currentState == WingmanState.Forming)
                        {
                            w.CommandAttack(c.transform);
                            break;
                        }
                    }
                }
            }

            // Prune threats that have left the radius.
            _knownThreats.RemoveWhere(c =>
                c == null ||
                Vector3.Distance(c.transform.position, escortTarget.position) > escortRadius * 1.2f);
        }

        private void RecallWingmen()
        {
            FormationManager mgr = FormationManager.Instance;
            if (mgr == null) return;
            foreach (WingmanAI w in mgr.wingmen)
                w.CommandReturn();
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (escortTarget == null) return;
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.2f);
            UnityEditor.Handles.DrawSolidDisc(escortTarget.position, Vector3.up, escortRadius);
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(escortTarget.position, Vector3.up, escortRadius);
        }
#endif

        #endregion
    }
}
