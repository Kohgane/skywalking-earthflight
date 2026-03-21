using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Minimap
{
    /// <summary>
    /// MonoBehaviour that bridges existing SWEF game systems to <see cref="MinimapManager"/>.
    /// On <c>Start</c> / <c>OnEnable</c> it scans for relevant game objects and registers blips;
    /// it also subscribes to system events to dynamically add/remove blips at runtime.
    /// Moving-entity blip positions are refreshed every <c>Update</c>.
    /// <para>All cross-system references are null-checked — missing systems are skipped gracefully.</para>
    /// </summary>
    public class MinimapBlipProvider : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("Provider Settings")]
        [Tooltip("Prefix applied to all blip IDs registered by this provider.")]
        [SerializeField] private string providerPrefix = "provider";

        [Tooltip("Register a blip for the local player.")]
        [SerializeField] private bool includePlayer = true;

        [Tooltip("Register blips for active tour waypoints.")]
        [SerializeField] private bool includeTourWaypoints = true;

        [Tooltip("Register blips for other multiplayer players.")]
        [SerializeField] private bool includeOtherPlayers = true;

        [Tooltip("Register blips for formation slots.")]
        [SerializeField] private bool includeFormationSlots = true;

        [Tooltip("Register blips for the active ghost replay.")]
        [SerializeField] private bool includeGhostReplay = true;

        [Tooltip("Register blips for active world events.")]
        [SerializeField] private bool includeWorldEvents = true;

        [Tooltip("Register blips for weather zones.")]
        [SerializeField] private bool includeWeatherZones = true;

        [Tooltip("Register blips for points of interest.")]
        [SerializeField] private bool includePointsOfInterest = true;

        // ── Tracked transforms for moving entities ────────────────────────────────
        private readonly Dictionary<string, Transform> _trackedTransforms = new Dictionary<string, Transform>();

        // ── Unity callbacks ────────────────────────────────────────────────────────
        private void OnEnable()
        {
            RegisterAllBlips();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            UnregisterProviderBlips();
        }

        private void Update()
        {
            // Refresh world positions for all tracked moving entities
            foreach (var kvp in _trackedTransforms)
            {
                if (kvp.Value == null) continue;

                var blip = MinimapManager.Instance != null
                    ? MinimapManager.Instance.GetBlip(kvp.Key)
                    : null;

                if (blip != null)
                    blip.worldPosition = kvp.Value.position;
            }
        }

        // ── Registration ──────────────────────────────────────────────────────────
        private void RegisterAllBlips()
        {
            if (MinimapManager.Instance == null) return;

            if (includePlayer)            RegisterPlayerBlip();
            if (includeTourWaypoints)     RegisterTourWaypoints();
            if (includeOtherPlayers)      RegisterOtherPlayers();
            if (includeFormationSlots)    RegisterFormationSlots();
            if (includeGhostReplay)       RegisterGhostReplay();
            if (includeWorldEvents)       RegisterWorldEvents();
            if (includeWeatherZones)      RegisterWeatherZones();
            if (includePointsOfInterest)  RegisterPointsOfInterest();
        }

        // ── Player blip ────────────────────────────────────────────────────────────
        private void RegisterPlayerBlip()
        {
            var fc = FindFirstObjectByType<Flight.FlightController>();
            if (fc == null) return;

            string id = $"{providerPrefix}_player";
            var blip  = new MinimapBlip
            {
                blipId       = id,
                iconType     = MinimapIconType.Player,
                worldPosition = fc.transform.position,
                label        = "You",
                color        = Color.cyan,
                isActive     = true
            };
            MinimapManager.Instance.RegisterBlip(blip);
            _trackedTransforms[id] = fc.transform;
        }

        // ── Tour waypoints ─────────────────────────────────────────────────────────
        private void RegisterTourWaypoints()
        {
#if !SWEF_NO_GUIDED_TOUR
            var nav = FindFirstObjectByType<GuidedTour.WaypointNavigator>();
            if (nav == null) return;

            var waypoints = nav.GetAllWaypoints();
            if (waypoints == null) return;

            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];
                MinimapIconType iconType;
                bool isNext = (i == nav.CurrentWaypointIndex);

                if (isNext)
                    iconType = MinimapIconType.WaypointNext;
                else if (i < nav.CurrentWaypointIndex)
                    iconType = MinimapIconType.WaypointVisited;
                else
                    iconType = MinimapIconType.Waypoint;

                string id = $"{providerPrefix}_wp_{i}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = iconType,
                    worldPosition  = wp.worldPosition,
                    label         = wp.label,
                    color         = isNext ? Color.yellow : Color.white,
                    isActive      = true,
                    isPulsing     = isNext
                };
                MinimapManager.Instance.RegisterBlip(blip);
            }
#endif
        }

        // ── Other players ──────────────────────────────────────────────────────────
        private void RegisterOtherPlayers()
        {
#if !SWEF_NO_MULTIPLAYER
            var syncSystem = FindFirstObjectByType<Multiplayer.PlayerSyncSystem>();
            if (syncSystem == null) return;

            var players = syncSystem.GetRemotePlayers();
            if (players == null) return;

            foreach (var player in players)
            {
                if (player == null) continue;
                string id = $"{providerPrefix}_oplayer_{player.PlayerId}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = MinimapIconType.OtherPlayer,
                    worldPosition  = player.transform.position,
                    label         = player.DisplayName,
                    color         = new Color(0.2f, 0.8f, 0.2f),
                    isActive      = true
                };
                MinimapManager.Instance.RegisterBlip(blip);
                _trackedTransforms[id] = player.transform;
            }
#endif
        }

        // ── Formation slots ────────────────────────────────────────────────────────
        private void RegisterFormationSlots()
        {
#if !SWEF_NO_MULTIPLAYER
            var formMgr = FindFirstObjectByType<Multiplayer.FormationFlyingManager>();
            if (formMgr == null) return;

            var slots = formMgr.GetFormationSlotPositions();
            if (slots == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                string id = $"{providerPrefix}_fslot_{i}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = MinimapIconType.FormationSlot,
                    worldPosition  = slots[i],
                    label         = $"Slot {i + 1}",
                    color         = new Color(0.4f, 0.8f, 1f),
                    isActive      = true
                };
                MinimapManager.Instance.RegisterBlip(blip);
            }
#endif
        }

        // ── Ghost replay ───────────────────────────────────────────────────────────
        private void RegisterGhostReplay()
        {
#if !SWEF_NO_REPLAY
            var ghost = FindFirstObjectByType<Replay.GhostRacer>();
            if (ghost == null) return;

            string id = $"{providerPrefix}_ghost";
            var blip  = new MinimapBlip
            {
                blipId        = id,
                iconType      = MinimapIconType.GhostReplay,
                worldPosition  = ghost.transform.position,
                label         = "Ghost",
                color         = new Color(0.7f, 0.4f, 1f),
                isActive      = true,
                isPulsing     = false
            };
            MinimapManager.Instance.RegisterBlip(blip);
            _trackedTransforms[id] = ghost.transform;
#endif
        }

        // ── World events ───────────────────────────────────────────────────────────
        private void RegisterWorldEvents()
        {
#if !SWEF_NO_EVENTS
            var eventMgr = FindFirstObjectByType<Events.EventScheduler>();
            if (eventMgr == null) return;

            var activeEvents = eventMgr.GetActiveEventInstances();
            if (activeEvents == null) return;

            foreach (var evt in activeEvents)
            {
                if (evt == null) continue;
                string id = $"{providerPrefix}_event_{evt.instanceId}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = MinimapIconType.WorldEvent,
                    worldPosition  = evt.worldPosition,
                    label         = evt.displayName,
                    color         = new Color(1f, 0.6f, 0f),
                    isActive      = true,
                    isPulsing     = true
                };
                MinimapManager.Instance.RegisterBlip(blip);
            }
#endif
        }

        // ── Weather zones ──────────────────────────────────────────────────────────
        private void RegisterWeatherZones()
        {
#if !SWEF_NO_WEATHER
            var weatherMgr = FindFirstObjectByType<Weather.WeatherManager>();
            if (weatherMgr == null) return;

            var zones = weatherMgr.GetActiveWeatherZones();
            if (zones == null) return;

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                string id = $"{providerPrefix}_wzone_{i}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = MinimapIconType.WeatherZone,
                    worldPosition  = zone.center,
                    label         = zone.zoneName,
                    color         = new Color(0.4f, 0.7f, 1f),
                    isActive      = true
                };
                MinimapManager.Instance.RegisterBlip(blip);
            }
#endif
        }

        // ── Points of interest ─────────────────────────────────────────────────────
        private void RegisterPointsOfInterest()
        {
            // POIs are typically static world objects tagged "SWEF_POI"
            var pois = GameObject.FindGameObjectsWithTag("SWEF_POI");
            if (pois == null) return;

            for (int i = 0; i < pois.Length; i++)
            {
                if (pois[i] == null) continue;
                string id = $"{providerPrefix}_poi_{pois[i].GetInstanceID()}";
                var blip  = new MinimapBlip
                {
                    blipId        = id,
                    iconType      = MinimapIconType.PointOfInterest,
                    worldPosition  = pois[i].transform.position,
                    label         = pois[i].name,
                    color         = Color.white,
                    isActive      = true
                };
                MinimapManager.Instance.RegisterBlip(blip);
            }
        }

        // ── Event subscriptions ────────────────────────────────────────────────────
        private void SubscribeToEvents()
        {
            // Multiplayer player connected/disconnected
#if !SWEF_NO_MULTIPLAYER
            var syncSystem = FindFirstObjectByType<Multiplayer.PlayerSyncSystem>();
            if (syncSystem != null)
            {
                syncSystem.OnRemotePlayerConnected    += OnRemotePlayerConnected;
                syncSystem.OnRemotePlayerDisconnected += OnRemotePlayerDisconnected;
            }
#endif
        }

        private void UnsubscribeFromEvents()
        {
#if !SWEF_NO_MULTIPLAYER
            var syncSystem = FindFirstObjectByType<Multiplayer.PlayerSyncSystem>();
            if (syncSystem != null)
            {
                syncSystem.OnRemotePlayerConnected    -= OnRemotePlayerConnected;
                syncSystem.OnRemotePlayerDisconnected -= OnRemotePlayerDisconnected;
            }
#endif
        }

#if !SWEF_NO_MULTIPLAYER
        private void OnRemotePlayerConnected(Multiplayer.RemotePlayerEntry player)
        {
            if (player == null || MinimapManager.Instance == null) return;
            string id = $"{providerPrefix}_oplayer_{player.PlayerId}";
            var blip  = new MinimapBlip
            {
                blipId        = id,
                iconType      = MinimapIconType.OtherPlayer,
                worldPosition  = player.transform.position,
                label         = player.DisplayName,
                color         = new Color(0.2f, 0.8f, 0.2f),
                isActive      = true
            };
            MinimapManager.Instance.RegisterBlip(blip);
            _trackedTransforms[id] = player.transform;
        }

        private void OnRemotePlayerDisconnected(string playerId)
        {
            if (MinimapManager.Instance == null) return;
            string id = $"{providerPrefix}_oplayer_{playerId}";
            MinimapManager.Instance.UnregisterBlip(id);
            _trackedTransforms.Remove(id);
        }
#endif

        // ── Cleanup ────────────────────────────────────────────────────────────────
        private void UnregisterProviderBlips()
        {
            if (MinimapManager.Instance == null) return;

            var toRemove = new List<string>(_trackedTransforms.Keys);
            foreach (var id in toRemove)
                MinimapManager.Instance.UnregisterBlip(id);

            _trackedTransforms.Clear();
        }
    }
}
