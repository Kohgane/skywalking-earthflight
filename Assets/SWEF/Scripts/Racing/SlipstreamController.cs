// SlipstreamController.cs — SWEF Boost & Drift Mechanics System (Phase 62)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Racing
{
    /// <summary>
    /// Phase 62 — Singleton that detects when the local player is drafting in the
    /// slipstream of another player and charges a graduated boost reward.
    ///
    /// <para>A cone-shaped detection zone is evaluated each frame against remote
    /// player positions supplied by <c>PlayerSyncSystem</c>. While inside the zone
    /// a charge timer advances through four graduated thresholds (25 / 50 / 75 / 100 %).
    /// A full charge or zone exit grants a <see cref="BoostConfig"/> reward via
    /// <see cref="BoostController"/>.</para>
    ///
    /// <para>Attach to the same persistent GameObject as <see cref="BoostController"/>.</para>
    /// </summary>
    public class SlipstreamController : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static SlipstreamController Instance { get; private set; }

        #endregion

        #region Constants

        private static readonly float[] ChargeThresholds = { 0.25f, 0.50f, 0.75f, 1.00f };

        #endregion

        #region Inspector

        [Header("Configuration")]
        [Tooltip("Slipstream detection config (range, cone angle, charge time, reward).")]
        [SerializeField] private SlipstreamConfig config;

        [Header("Player Transform")]
        [Tooltip("Transform of the local player used as the detection cone origin.")]
        [SerializeField] private Transform localPlayerTransform;

        #endregion

        #region Events

        /// <summary>Fired when the player enters another player's slipstream cone.</summary>
        public event Action<string> OnSlipstreamEnter;

        /// <summary>
        /// Fired at each charge threshold crossing (25 %, 50 %, 75 %, 100 %).
        /// Passes the current normalised charge (0–1).
        /// </summary>
        public event Action<float> OnSlipstreamCharged;

        /// <summary>Fired when the player exits the slipstream zone.</summary>
        public event Action OnSlipstreamExit;

        #endregion

        #region Public Properties

        /// <summary>Whether the player is currently inside a slipstream cone.</summary>
        public bool IsInSlipstream { get; private set; }

        /// <summary>Normalised slipstream charge 0–1.</summary>
        public float ChargeNormalized { get; private set; }

        /// <summary>ID of the player whose slipstream is currently being drafted.</summary>
        public string CurrentLeadPlayerId { get; private set; }

        #endregion

        #region Private State

        private float _chargeTimer;
        private int   _lastThresholdIndex = -1;

        // Remote player positions provided externally (e.g. from PlayerSyncSystem).
        private readonly Dictionary<string, (Vector3 position, Vector3 forward)> _remotePlayerData
            = new Dictionary<string, (Vector3, Vector3)>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (localPlayerTransform == null || config == null) return;
            EvaluateSlipstream(Time.deltaTime);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Updates the known position and forward direction of a remote player.
        /// Call this from <c>PlayerSyncSystem</c> each frame for each tracked player.
        /// </summary>
        /// <param name="playerId">Unique player identifier.</param>
        /// <param name="worldPosition">Remote player's current world position.</param>
        /// <param name="worldForward">Remote player's current forward direction (normalised).</param>
        public void UpdateRemotePlayer(string playerId, Vector3 worldPosition, Vector3 worldForward)
        {
            _remotePlayerData[playerId] = (worldPosition, worldForward.normalized);
        }

        /// <summary>
        /// Removes a remote player from tracking (on disconnect or out-of-range culling).
        /// </summary>
        /// <param name="playerId">Unique player identifier to remove.</param>
        public void RemoveRemotePlayer(string playerId)
        {
            _remotePlayerData.Remove(playerId);
        }

        #endregion

        #region Private Helpers

        private void EvaluateSlipstream(float dt)
        {
            bool foundSlipstream = false;

            foreach (var kvp in _remotePlayerData)
            {
                (Vector3 remotePos, Vector3 remoteForward) = kvp.Value;

                // Vector from remote player back to local player.
                Vector3 toLocal = localPlayerTransform.position - remotePos;
                float   dist    = toLocal.magnitude;

                if (dist > config.detectionRange) continue;

                // Check cone: the local player must be inside the cone directly behind
                // the remote player. "Behind" means toLocal points in the same direction as
                // remoteForward (both heading away from the remote player's rear).
                float cosAngle    = Vector3.Dot(remoteForward, toLocal.normalized);
                float cosThreshold = Mathf.Cos(config.coneAngleDegrees * Mathf.Deg2Rad);
                if (cosAngle <= cosThreshold) // outside cone
                    continue;

                foundSlipstream = true;

                if (!IsInSlipstream)
                {
                    IsInSlipstream      = true;
                    CurrentLeadPlayerId = kvp.Key;
                    _chargeTimer        = 0f;
                    _lastThresholdIndex = -1;
                    OnSlipstreamEnter?.Invoke(kvp.Key);
                }

                _chargeTimer    += dt;
                ChargeNormalized = Mathf.Clamp01(config.chargeTime > 0f
                    ? _chargeTimer / config.chargeTime
                    : 1f);

                NotifyThresholdCrossings();

                if (ChargeNormalized >= 1f)
                {
                    GrantBoost(full: true);
                    foundSlipstream = false; // force exit state reset
                    break;
                }

                break; // Only draft the closest / first detected leader.
            }

            if (!foundSlipstream && IsInSlipstream)
                ExitSlipstream();
        }

        private void NotifyThresholdCrossings()
        {
            for (int i = _lastThresholdIndex + 1; i < ChargeThresholds.Length; i++)
            {
                if (ChargeNormalized >= ChargeThresholds[i])
                {
                    _lastThresholdIndex = i;
                    OnSlipstreamCharged?.Invoke(ChargeThresholds[i]);
                }
            }
        }

        private void ExitSlipstream()
        {
            if (ChargeNormalized > 0f)
                GrantBoost(full: false);

            IsInSlipstream      = false;
            CurrentLeadPlayerId = string.Empty;
            ChargeNormalized    = 0f;
            _chargeTimer        = 0f;
            _lastThresholdIndex = -1;
            OnSlipstreamExit?.Invoke();
        }

        private void GrantBoost(bool full)
        {
            if (BoostController.Instance == null || config == null) return;
            BoostConfig reward = full ? config.boostReward : config.partialBoostReward;
            if (reward != null)
                BoostController.Instance.ApplyBoost(reward);
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (localPlayerTransform == null || config == null) return;

            Gizmos.color = IsInSlipstream ? Color.green : Color.cyan;
            // Draw detection cone as a simple sphere arc approximation.
            foreach (var kvp in _remotePlayerData)
            {
                Vector3 remotePos     = kvp.Value.position;
                Vector3 remoteForward = kvp.Value.forward;

                // Draw cone lines.
                float halfAngleRad = config.coneAngleDegrees * Mathf.Deg2Rad;
                Vector3 coneLeft  = Quaternion.AngleAxis(-config.coneAngleDegrees, Vector3.up) * (-remoteForward);
                Vector3 coneRight = Quaternion.AngleAxis( config.coneAngleDegrees, Vector3.up) * (-remoteForward);
                Gizmos.DrawRay(remotePos, coneLeft  * config.detectionRange);
                Gizmos.DrawRay(remotePos, coneRight * config.detectionRange);
                Gizmos.DrawWireSphere(remotePos, config.detectionRange * 0.05f);
            }
        }
#endif

        #endregion
    }
}
