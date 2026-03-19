using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Offline
{
    /// <summary>
    /// Manages graceful degradation when the device is offline.
    ///
    /// <para>Subscribes to <see cref="OfflineManager"/> events and
    /// disables/re-enables dependent services accordingly.  Operations that
    /// cannot run offline are queued as <see cref="System.Action"/> delegates
    /// and flushed when connectivity is restored.</para>
    ///
    /// <para>Maximum deferred-operation queue size: <see cref="MaxDeferredOps"/>.
    /// When the queue is full, the oldest entry is silently dropped.</para>
    /// </summary>
    public class OfflineFallbackController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static OfflineFallbackController Instance { get; private set; }

        // ── Constants ─────────────────────────────────────────────────────────────
        /// <summary>Maximum number of deferred operations retained while offline.</summary>
        public const int MaxDeferredOps = 1000;

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("OfflineHUD to show/hide the offline indicator.  Auto-found if null.")]
        [SerializeField] private OfflineHUD offlineHUD;

        // ── Properties ────────────────────────────────────────────────────────────
        /// <summary>Number of operations currently queued for deferred execution.</summary>
        public int DeferredOpCount => _deferredOps.Count;

        // ── Internal ──────────────────────────────────────────────────────────────
        private readonly Queue<System.Action> _deferredOps = new Queue<System.Action>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

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

        private void Start()
        {
            if (offlineHUD == null)
                offlineHUD = FindFirstObjectByType<OfflineHUD>();

            if (OfflineManager.Instance != null)
            {
                OfflineManager.Instance.OnOfflineModeEntered  += HandleOfflineModeEntered;
                OfflineManager.Instance.OnOnlineModeRestored  += HandleOnlineModeRestored;

                // Apply correct initial state
                if (OfflineManager.Instance.IsOffline)
                    HandleOfflineModeEntered();
            }
            else
            {
                Debug.LogWarning("[SWEF] OfflineFallbackController: OfflineManager not found.");
            }
        }

        private void OnDestroy()
        {
            if (OfflineManager.Instance != null)
            {
                OfflineManager.Instance.OnOfflineModeEntered -= HandleOfflineModeEntered;
                OfflineManager.Instance.OnOnlineModeRestored -= HandleOnlineModeRestored;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Enqueues an operation to be executed when connectivity is restored.
        /// If the queue is at capacity, the oldest item is dropped.
        /// </summary>
        /// <param name="operation">Action to defer.</param>
        public void EnqueueDeferredOperation(System.Action operation)
        {
            if (operation == null) return;

            if (_deferredOps.Count >= MaxDeferredOps)
            {
                _deferredOps.Dequeue(); // drop oldest
                Debug.LogWarning("[SWEF] OfflineFallbackController: deferred queue full — oldest op dropped.");
            }
            _deferredOps.Enqueue(operation);
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void HandleOfflineModeEntered()
        {
            Debug.Log("[SWEF] OfflineFallbackController: offline — disabling network-dependent services.");

            // Disable weather API
            Weather.WeatherDataService.Instance?.SetFallbackMode(true);

            // Disable multiplayer sync
            var roomManager = FindFirstObjectByType<Multiplayer.RoomManager>();
            if (roomManager != null) roomManager.enabled = false;

            // Notify telemetry pipeline to queue locally
            var dispatcher = Analytics.TelemetryDispatcher.Instance != null
                ? Analytics.TelemetryDispatcher.Instance
                : FindFirstObjectByType<Analytics.TelemetryDispatcher>();
            if (dispatcher != null) dispatcher.SetOfflineMode(true);

            // Show HUD offline indicator
            offlineHUD?.ShowOfflineIndicator();
        }

        private void HandleOnlineModeRestored()
        {
            Debug.Log("[SWEF] OfflineFallbackController: online — re-enabling services.");

            // Re-enable weather
            Weather.WeatherDataService.Instance?.SetFallbackMode(false);

            // Re-enable multiplayer sync
            var roomManager = FindFirstObjectByType<Multiplayer.RoomManager>();
            if (roomManager != null) roomManager.enabled = true;

            // Re-enable telemetry upload and flush queued events
            var dispatcher = Analytics.TelemetryDispatcher.Instance != null
                ? Analytics.TelemetryDispatcher.Instance
                : FindFirstObjectByType<Analytics.TelemetryDispatcher>();
            if (dispatcher != null)
            {
                dispatcher.SetOfflineMode(false);
                dispatcher.FlushQueue();
            }

            // Flush deferred operations
            FlushDeferredOps();

            // Hide HUD offline indicator
            offlineHUD?.HideOfflineIndicator();
        }

        private void FlushDeferredOps()
        {
            int count = _deferredOps.Count;
            while (_deferredOps.Count > 0)
            {
                try
                {
                    _deferredOps.Dequeue()?.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SWEF] OfflineFallbackController: deferred op threw: {ex.Message}");
                }
            }
            if (count > 0)
                Debug.Log($"[SWEF] OfflineFallbackController: flushed {count} deferred op(s).");
        }
    }
}
