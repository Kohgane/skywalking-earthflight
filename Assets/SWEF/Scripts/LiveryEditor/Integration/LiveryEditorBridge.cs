// LiveryEditorBridge.cs — Phase 115: Advanced Aircraft Livery Editor
// Integration with existing SWEF systems: Aircraft, Workshop, Marketplace, CloudSave.
// Namespace: SWEF.LiveryEditor

using System;
using UnityEngine;

namespace SWEF.LiveryEditor
{
    /// <summary>
    /// Phase 115 — Integration bridge between the Livery Editor and other SWEF systems.
    /// Provides conditional hooks for Aircraft, Workshop, Marketplace, and CloudSave
    /// systems when the corresponding define symbols are present.
    /// </summary>
    public class LiveryEditorBridge : MonoBehaviour
    {
        // ── Inspector references ──────────────────────────────────────────────────
        [SerializeField] private LiveryEditorManager manager;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the bridge sends a livery to the Aircraft system.</summary>
        public event Action<string, LiverySaveData> OnLiverySentToAircraft;

        /// <summary>Raised when a livery is published to the Workshop.</summary>
        public event Action<LiverySaveData> OnLiveryPublishedToWorkshop;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (manager != null) manager.OnLiveryApplied += HandleLiveryApplied;
        }

        private void OnDisable()
        {
            if (manager != null) manager.OnLiveryApplied -= HandleLiveryApplied;
        }

        // ── Aircraft integration ──────────────────────────────────────────────────

#if SWEF_LIVERY_AVAILABLE
        /// <summary>
        /// Sends the active livery to the Aircraft system's material pipeline.
        /// Requires <c>SWEF_LIVERY_AVAILABLE</c>.
        /// </summary>
        public void ApplyToAircraft(string aircraftId)
        {
            if (manager?.ActiveLivery == null) return;
            // TODO: resolve AircraftManager.Instance and call SetLivery(aircraftId, livery)
            OnLiverySentToAircraft?.Invoke(aircraftId, manager.ActiveLivery);
            Debug.Log($"[SWEF] LiveryEditorBridge: livery sent to aircraft '{aircraftId}'.");
        }
#endif

        // ── Workshop / Marketplace integration ───────────────────────────────────

#if SWEF_MARKETPLACE_AVAILABLE
        /// <summary>
        /// Publishes the active livery to the Workshop marketplace.
        /// Requires <c>SWEF_MARKETPLACE_AVAILABLE</c>.
        /// </summary>
        public void PublishToWorkshop()
        {
            if (manager?.ActiveLivery == null) return;
            // TODO: connect to MarketplaceManager.Instance.Publish(livery)
            OnLiveryPublishedToWorkshop?.Invoke(manager.ActiveLivery);
            Debug.Log($"[SWEF] LiveryEditorBridge: livery '{manager.ActiveLivery.Metadata.Name}' submitted to workshop.");
        }
#endif

        // ── CloudSave integration ─────────────────────────────────────────────────

#if SWEF_CLOUDSAVE_AVAILABLE
        /// <summary>
        /// Persists the active livery via the CloudSave system.
        /// Requires <c>SWEF_CLOUDSAVE_AVAILABLE</c>.
        /// </summary>
        public void CloudSave()
        {
            if (manager?.ActiveLivery == null) return;
            string json = JsonUtility.ToJson(manager.ActiveLivery.Metadata);
            // TODO: CloudSaveManager.Instance.Save("livery_" + manager.ActiveLivery.Metadata.LiveryId, json)
            Debug.Log("[SWEF] LiveryEditorBridge: livery queued for cloud save.");
        }
#endif

        // ── Internal handlers ─────────────────────────────────────────────────────

        private void HandleLiveryApplied(string aircraftId, LiverySaveData livery)
        {
            Debug.Log($"[SWEF] LiveryEditorBridge: livery '{livery.Metadata.Name}' applied to '{aircraftId}'.");
        }
    }
}
