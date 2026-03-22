using System;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.TimeOfDay
{
    /// <summary>
    /// Synchronises the time-of-day simulation across all players in a multiplayer session.
    /// <para>
    /// The room <em>host</em> is the authoritative time source.  Non-host clients apply
    /// gentle drift corrections (never snapping) to keep their clocks in sync.
    /// On host migration the new host immediately broadcasts the current time.
    /// </para>
    /// Sync packet is sent every <see cref="syncIntervalSeconds"/> seconds;
    /// local interpolation fills the gaps.
    /// </summary>
    public class TimeOfDayMultiplayerSync : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Sync Settings")]
        [Tooltip("Seconds between full time sync broadcasts. Lower = more accurate but more bandwidth.")]
        [SerializeField, Range(1f, 30f)] private float syncIntervalSeconds = 5f;

        [Tooltip("Maximum drift correction speed (simulated hours per real second) for gentle catch-up.")]
        [SerializeField, Range(0.001f, 0.5f)] private float maxDriftCorrectionSpeed = 0.05f;

        [Tooltip("Drift threshold in simulated seconds — corrections below this are ignored.")]
        [SerializeField, Range(0f, 10f)] private float driftIgnoreThresholdSeconds = 2f;

        [Header("References (auto-found if null)")]
        [SerializeField] private TimeOfDayManager timeOfDayManager;
        [SerializeField] private NetworkManager2  networkManager;

        // ── State ─────────────────────────────────────────────────────────────────
        private float _syncTimer;
        private float _targetHour;    // received from host, to interpolate toward
        private bool  _hasTarget;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (timeOfDayManager == null)
                timeOfDayManager = FindFirstObjectByType<TimeOfDayManager>();

            if (networkManager == null)
                networkManager = FindFirstObjectByType<NetworkManager2>();
        }

        private void OnEnable()
        {
            if (networkManager == null) return;
            networkManager.OnLobbyJoined    += OnJoinedLobby;
            networkManager.OnHostMigrated   += OnHostMigrated;
        }

        private void OnDisable()
        {
            if (networkManager == null) return;
            networkManager.OnLobbyJoined    -= OnJoinedLobby;
            networkManager.OnHostMigrated   -= OnHostMigrated;
        }

        private void Update()
        {
            if (timeOfDayManager == null || networkManager == null) return;

            bool isHost = networkManager.IsHost;

            if (isHost)
            {
                _syncTimer += Time.deltaTime;
                if (_syncTimer >= syncIntervalSeconds)
                {
                    _syncTimer = 0f;
                    BroadcastTime();
                }
            }
            else if (_hasTarget)
            {
                ApplyDriftCorrection();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when a sync packet is received from the host.
        /// In a real implementation this would be wired to the network RPC layer.
        /// </summary>
        /// <param name="hostHour">Fractional hour (0–24) reported by the host.</param>
        /// <param name="timeScale">Host time scale.</param>
        /// <param name="seasonOverride">Host season override (or <c>-1</c> for none).</param>
        public void ReceiveSyncPacket(float hostHour, float timeScale, int seasonOverride)
        {
            if (networkManager != null && networkManager.IsHost) return; // hosts ignore their own packets

            _targetHour = hostHour;
            _hasTarget  = true;
            timeOfDayManager?.SetTimeScale(timeScale);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void BroadcastTime()
        {
            if (timeOfDayManager == null) return;
            float hour = timeOfDayManager.CurrentHour;
            // In a real SWEF networking layer this would call a network RPC.
            // Here we log the intent for integration.
#if UNITY_EDITOR
            Debug.Log($"[TimeOfDayMultiplayerSync] Host broadcasting time: {hour:F2}h");
#endif
        }

        private void ApplyDriftCorrection()
        {
            if (timeOfDayManager == null) return;

            float currentHour = timeOfDayManager.CurrentHour;
            float diff        = _targetHour - currentHour;

            // Wrap around midnight
            if (diff > 12f)  diff -= 24f;
            if (diff < -12f) diff += 24f;

            float diffSeconds = diff * 3600f;
            if (Mathf.Abs(diffSeconds) < driftIgnoreThresholdSeconds) return;

            // Apply a gentle step toward the target
            float correction = Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), maxDriftCorrectionSpeed * Time.deltaTime);
            timeOfDayManager.SetTime(currentHour + correction);
        }

        private void OnJoinedLobby(LobbyInfo lobby)
        {
            // When joining, request a time sync from the host
            _hasTarget = false;
#if UNITY_EDITOR
            Debug.Log("[TimeOfDayMultiplayerSync] Joined lobby — awaiting host time sync.");
#endif
        }

        private void OnHostMigrated(string newHostId)
        {
            // If we became the new host, broadcast our time immediately
            if (networkManager != null && networkManager.IsHost)
            {
                BroadcastTime();
            }
        }
    }
}
