// SatelliteTrackingBridge.cs — Phase 114: Satellite & Space Debris Tracking
// Integration with existing SWEF systems: Flight (orbit insertion), Minimap (satellite overlay),
// Achievement (#if SWEF_SATELLITE_AVAILABLE).
// Namespace: SWEF.SatelliteTracking

using System;
using UnityEngine;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Bridge component connecting the Satellite Tracking system to other SWEF modules.
    /// Each integration is compile-guarded by the relevant symbol.
    /// </summary>
    public class SatelliteTrackingBridge : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        /// <summary>Shared singleton instance.</summary>
        public static SatelliteTrackingBridge Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the bridge has connected all available modules.</summary>
        public event Action OnBridgeReady;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SubscribeToTrackingManager();
            OnBridgeReady?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromTrackingManager();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void SubscribeToTrackingManager()
        {
            var mgr = SatelliteTrackingManager.Instance;
            if (mgr == null) return;

            mgr.OnSatelliteAdded    += HandleSatelliteAdded;
            mgr.OnConjunctionWarning += HandleConjunctionWarning;
        }

        private void UnsubscribeFromTrackingManager()
        {
            var mgr = SatelliteTrackingManager.Instance;
            if (mgr == null) return;

            mgr.OnSatelliteAdded    -= HandleSatelliteAdded;
            mgr.OnConjunctionWarning -= HandleConjunctionWarning;
        }

        private void HandleSatelliteAdded(SatelliteRecord record)
        {
#if SWEF_MINIMAP_AVAILABLE
            // Notify the minimap to add an overlay blip for this satellite
            NotifyMinimapSatelliteAdded(record);
#endif

#if SWEF_SATELLITE_AVAILABLE
            NotifyAchievementSatelliteTracked(record);
#endif
        }

        private void HandleConjunctionWarning(ConjunctionData conjunction)
        {
#if SWEF_SATELLITE_AVAILABLE
            if (conjunction.urgencyLevel >= 2)
                NotifyAchievementDebrisEncounter(conjunction);
#endif
        }

#if SWEF_MINIMAP_AVAILABLE
        private void NotifyMinimapSatelliteAdded(SatelliteRecord record)
        {
            // Forward to Minimap module via type-safe soft reference
            var minimapType = System.Type.GetType("SWEF.Minimap.MinimapManager, Assembly-CSharp");
            if (minimapType != null)
            {
                var minimapInstance = FindObjectOfType(minimapType);
                // Minimap integration would be invoked here
                Debug.Log($"[SatelliteTrackingBridge] Minimap notified of satellite: {record.name}");
            }
        }
#endif

#if SWEF_SATELLITE_AVAILABLE
        private void NotifyAchievementSatelliteTracked(SatelliteRecord record)
        {
            Debug.Log($"[SatelliteTrackingBridge] Achievement: satellite tracked — {record.name}");
        }

        private void NotifyAchievementDebrisEncounter(ConjunctionData conjunction)
        {
            Debug.Log($"[SatelliteTrackingBridge] Achievement: debris encounter — " +
                      $"miss dist {conjunction.missDistanceKm:F2} km");
        }
#endif
    }
}
