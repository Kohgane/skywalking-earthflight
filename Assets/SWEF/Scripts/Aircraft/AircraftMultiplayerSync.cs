using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SWEF.Multiplayer;

namespace SWEF.Aircraft
{
    /// <summary>
    /// Synchronises the local player's aircraft customisation loadout to all
    /// connected peers.  When the active loadout changes, the loadout is
    /// serialised to a compact pipe-delimited string and sent via
    /// <see cref="NetworkManager2"/>.  Incoming loadout strings from remote
    /// players are applied to their aircraft <see cref="AircraftVisualController"/>
    /// instances.
    /// </summary>
    public class AircraftMultiplayerSync : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private AircraftCustomizationManager customizationManager;

        /// <summary>
        /// Map of remote player IDs to their aircraft VisualController.
        /// Populate at runtime when remote player models are instantiated.
        /// </summary>
        [SerializeField] private List<RemotePlayerEntry> remotePlayerVisuals =
            new List<RemotePlayerEntry>();

        // ── Types ─────────────────────────────────────────────────────────────────

        /// <summary>Links a player ID to its runtime visual controller.</summary>
        [Serializable]
        public class RemotePlayerEntry
        {
            /// <summary>Network player ID.</summary>
            public string playerId;
            /// <summary>Visual controller on the remote player's aircraft model.</summary>
            public AircraftVisualController visualController;
        }

        // ── Runtime state ─────────────────────────────────────────────────────────

        private NetworkManager2 _network;
        private Dictionary<string, AircraftVisualController> _remoteControllers =
            new Dictionary<string, AircraftVisualController>();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            if (customizationManager == null)
                customizationManager = AircraftCustomizationManager.Instance;

            _network = FindObjectOfType<NetworkManager2>();

            if (customizationManager != null)
                customizationManager.OnLoadoutChanged += OnLoadoutChanged;

            // Pre-populate dictionary from Inspector list.
            foreach (var entry in remotePlayerVisuals)
            {
                if (!string.IsNullOrEmpty(entry.playerId) && entry.visualController != null)
                    _remoteControllers[entry.playerId] = entry.visualController;
            }
        }

        private void OnDestroy()
        {
            if (customizationManager != null)
                customizationManager.OnLoadoutChanged -= OnLoadoutChanged;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises the given loadout to a compact string.
        /// Format: <c>body:skinId|wings:skinId|engine:skinId|...</c>
        /// </summary>
        public string SerializeLoadout(AircraftLoadout loadout)
        {
            if (loadout == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append("body:")    .Append(loadout.bodySkinId    ?? string.Empty).Append('|');
            sb.Append("wings:")   .Append(loadout.wingsSkinId   ?? string.Empty).Append('|');
            sb.Append("engine:")  .Append(loadout.engineSkinId  ?? string.Empty).Append('|');
            sb.Append("cockpit:") .Append(loadout.cockpitSkinId ?? string.Empty).Append('|');
            sb.Append("trail:")   .Append(loadout.trailSkinId   ?? string.Empty).Append('|');
            sb.Append("decal:")   .Append(loadout.decalSkinId   ?? string.Empty).Append('|');
            sb.Append("particle:").Append(loadout.particleSkinId ?? string.Empty).Append('|');
            sb.Append("aura:")    .Append(loadout.auraSkinId    ?? string.Empty);
            return sb.ToString();
        }

        /// <summary>
        /// Deserialises a compact loadout string (produced by
        /// <see cref="SerializeLoadout"/>) back into an
        /// <see cref="AircraftLoadout"/>.
        /// </summary>
        public AircraftLoadout DeserializeLoadout(string data)
        {
            var loadout = new AircraftLoadout();
            if (string.IsNullOrEmpty(data)) return loadout;

            foreach (var token in data.Split('|'))
            {
                int colon = token.IndexOf(':');
                if (colon < 0) continue;
                string key = token.Substring(0, colon).Trim().ToLowerInvariant();
                string val = token.Substring(colon + 1).Trim();

                switch (key)
                {
                    case "body":     loadout.bodySkinId     = val; break;
                    case "wings":    loadout.wingsSkinId    = val; break;
                    case "engine":   loadout.engineSkinId   = val; break;
                    case "cockpit":  loadout.cockpitSkinId  = val; break;
                    case "trail":    loadout.trailSkinId    = val; break;
                    case "decal":    loadout.decalSkinId    = val; break;
                    case "particle": loadout.particleSkinId = val; break;
                    case "aura":     loadout.auraSkinId     = val; break;
                }
            }
            return loadout;
        }

        /// <summary>
        /// Serialises the currently active loadout and sends it to all peers via
        /// <see cref="NetworkManager2"/>.
        /// </summary>
        public void BroadcastLoadout()
        {
            if (customizationManager == null || customizationManager.ActiveLoadout == null) return;

            string payload = SerializeLoadout(customizationManager.ActiveLoadout);

            if (_network == null)
            {
                Debug.LogWarning("[AircraftMultiplayerSync] NetworkManager2 not found; cannot broadcast loadout.");
                return;
            }

            // In production: serialise and send via NetworkTransport.
            Debug.Log($"[AircraftMultiplayerSync] Broadcasting loadout to all players: {payload}");
        }

        /// <summary>
        /// Called by the network transport when a remote player sends their loadout.
        /// Parses the data and applies it to the relevant visual controller.
        /// </summary>
        public void OnRemoteLoadoutReceived(string playerId, string loadoutData)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(loadoutData)) return;

            var loadout = DeserializeLoadout(loadoutData);
            ApplyRemoteLoadout(playerId, loadout);
        }

        /// <summary>
        /// Applies <paramref name="loadout"/> to the remote player's aircraft model.
        /// </summary>
        public void ApplyRemoteLoadout(string playerId, AircraftLoadout loadout)
        {
            if (!_remoteControllers.TryGetValue(playerId, out var vc) || vc == null)
            {
                Debug.LogWarning($"[AircraftMultiplayerSync] No VisualController registered for player '{playerId}'.");
                return;
            }
            vc.ApplyLoadout(loadout);
        }

        /// <summary>
        /// Registers a remote player's visual controller so their customisation
        /// can be applied when loadout data arrives.
        /// </summary>
        public void RegisterRemotePlayer(string playerId, AircraftVisualController vc)
        {
            if (string.IsNullOrEmpty(playerId) || vc == null) return;
            _remoteControllers[playerId] = vc;
        }

        /// <summary>Removes a remote player's registration.</summary>
        public void UnregisterRemotePlayer(string playerId)
        {
            _remoteControllers.Remove(playerId);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnLoadoutChanged(AircraftLoadout _) => BroadcastLoadout();
    }
}
