#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SWEF.Editor
{
    /// <summary>
    /// Unity Editor window for testing and debugging the SWEF Multiplayer System.
    ///
    /// <para>Open via menu <b>SWEF → Multiplayer Debug</b>.</para>
    ///
    /// <para>Features:</para>
    /// <list type="bullet">
    ///   <item>Live connection status and room info.</item>
    ///   <item>Connected player list with sync statistics.</item>
    ///   <item>Buttons: Force Disconnect, Simulate Lag, Spawn Fake Player, Clear All Remote Players.</item>
    ///   <item>Bandwidth readout (bytes/sec sent and received).</item>
    /// </list>
    /// </summary>
    public class MultiplayerDebugWindow : EditorWindow
    {
        // ── Menu Item ─────────────────────────────────────────────────────────────

        /// <summary>Opens the Multiplayer Debug editor window.</summary>
        [MenuItem("SWEF/Multiplayer Debug")]
        public static void ShowWindow()
        {
            var win = GetWindow<MultiplayerDebugWindow>("Multiplayer Debug");
            win.minSize = new Vector2(360f, 540f);
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private Vector2 _scroll;
        private float   _simulatedLagMs;
        private string  _fakePlayerName = "TestPilot";
        private int     _fakePlayerColor;

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWEF — Multiplayer Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use the Multiplayer Debug window.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawConnectionStatus();
            EditorGUILayout.Space(8f);
            DrawRoomInfo();
            EditorGUILayout.Space(8f);
            DrawPlayerList();
            EditorGUILayout.Space(8f);
            DrawBandwidth();
            EditorGUILayout.Space(8f);
            DrawDebugActions();

            EditorGUILayout.EndScrollView();
        }

        // ── Sections ──────────────────────────────────────────────────────────────

        private void DrawConnectionStatus()
        {
            EditorGUILayout.LabelField("Connection Status", EditorStyles.boldLabel);

            var manager = Object.FindFirstObjectByType<SWEF.Multiplayer.MultiplayerManager>();

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            if (manager == null)
            {
                EditorGUILayout.HelpBox("MultiplayerManager not found in scene.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Local Player ID", manager.LocalPlayerId);
            EditorGUILayout.LabelField("Is In Room",      manager.IsInRoom.ToString());
            EditorGUILayout.LabelField("Is Host",         manager.IsHost.ToString());
            EditorGUILayout.LabelField("Remote Players",  manager.RemotePlayerCount.ToString());
        }

        private void DrawRoomInfo()
        {
            EditorGUILayout.LabelField("Room Info", EditorStyles.boldLabel);

            var rm = SWEF.Multiplayer.RoomManager.Instance;
            if (rm == null)
            {
                EditorGUILayout.HelpBox("RoomManager not found.", MessageType.Warning);
                return;
            }

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            if (!rm.IsInRoom)
            {
                EditorGUILayout.LabelField("Not in a room.");
                return;
            }

            var room = rm.CurrentRoom;
            EditorGUILayout.LabelField("Room Name",   room.roomName);
            EditorGUILayout.LabelField("Room ID",     room.roomId.Substring(0, Mathf.Min(room.roomId.Length, 16)) + "…");
            EditorGUILayout.LabelField("Players",     $"{room.playerCount} / {room.maxPlayers}");
            EditorGUILayout.LabelField("Region",      room.region);
            EditorGUILayout.LabelField("Public",      room.isPublic.ToString());
            EditorGUILayout.LabelField("Created",     room.createdAt.ToString("HH:mm:ss") + " UTC");
        }

        private void DrawPlayerList()
        {
            EditorGUILayout.LabelField("Players in Room", EditorStyles.boldLabel);

            var rm = SWEF.Multiplayer.RoomManager.Instance;
            if (rm == null || !rm.IsInRoom)
            {
                EditorGUILayout.LabelField("  (not in a room)");
                return;
            }

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            foreach (var info in rm.PlayersInRoom)
            {
                string hostMark  = info.isHost  ? " [HOST]" : "";
                string readyMark = info.isReady ? " ✅" : "";
                EditorGUILayout.LabelField($"  {info.playerName}{hostMark}{readyMark}",
                    $"Color#{info.avatarIndex}  ID:{info.playerId}");
            }
        }

        private void DrawBandwidth()
        {
            EditorGUILayout.LabelField("Bandwidth", EditorStyles.boldLabel);

            var syncCtrl = Object.FindFirstObjectByType<SWEF.Multiplayer.PlayerSyncController>();
            if (syncCtrl == null)
            {
                EditorGUILayout.HelpBox("PlayerSyncController not found.", MessageType.Warning);
                return;
            }

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Bytes/s Sent",     $"{syncCtrl.BytesPerSecondSent:F0} B/s");
            EditorGUILayout.LabelField("Bytes/s Received", $"{syncCtrl.BytesPerSecondReceived:F0} B/s");
        }

        private void DrawDebugActions()
        {
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Force Disconnect"))
                ForceDisconnect();

            if (GUILayout.Button("Clear Remote Players"))
                ClearRemotePlayers();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Simulate Lag", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("50 ms"))  SetSimulatedLag(50f);
            if (GUILayout.Button("100 ms")) SetSimulatedLag(100f);
            if (GUILayout.Button("200 ms")) SetSimulatedLag(200f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Spawn Fake Player", EditorStyles.boldLabel);
            _fakePlayerName  = EditorGUILayout.TextField("Name",  _fakePlayerName);
            _fakePlayerColor = EditorGUILayout.IntSlider("Color", _fakePlayerColor, 0, 7);
            if (GUILayout.Button("Spawn Fake Player"))
                SpawnFakePlayer();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void ForceDisconnect()
        {
            var manager = Object.FindFirstObjectByType<SWEF.Multiplayer.MultiplayerManager>();
            if (manager == null) { Debug.LogWarning("[SWEF][MultiplayerDebug] MultiplayerManager not found."); return; }
            manager.ShutdownMultiplayer();
            Debug.Log("[SWEF][MultiplayerDebug] Force disconnect triggered.");
        }

        private static void ClearRemotePlayers()
        {
            var manager = Object.FindFirstObjectByType<SWEF.Multiplayer.MultiplayerManager>();
            manager?.RemoveAllPlayers();
            Debug.Log("[SWEF][MultiplayerDebug] Cleared all remote players.");
        }

        private static void SetSimulatedLag(float ms)
        {
            var transport = Object.FindFirstObjectByType<SWEF.Multiplayer.FallbackLocalTransport>();
            if (transport != null)
            {
                typeof(SWEF.Multiplayer.FallbackLocalTransport)
                    .GetField("simulatedLatencyMs",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(transport, ms);
                Debug.Log($"[SWEF][MultiplayerDebug] Simulated lag set to {ms} ms.");
            }
            else
            {
                Debug.LogWarning("[SWEF][MultiplayerDebug] FallbackLocalTransport not found in scene.");
            }
        }

        private void SpawnFakePlayer()
        {
            var rm = SWEF.Multiplayer.RoomManager.Instance;
            if (rm == null) { Debug.LogWarning("[SWEF][MultiplayerDebug] RoomManager not found."); return; }
            if (!rm.IsInRoom) { Debug.LogWarning("[SWEF][MultiplayerDebug] Not in a room."); return; }

            var info = new SWEF.Multiplayer.PlayerInfo
            {
                playerId    = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                playerName  = string.IsNullOrWhiteSpace(_fakePlayerName) ? "FakePilot" : _fakePlayerName,
                avatarIndex = _fakePlayerColor,
                isHost      = false,
                isReady     = false,
                joinedAt    = System.DateTime.UtcNow
            };

            rm.SimulatePlayerJoin(info);
            Debug.Log($"[SWEF][MultiplayerDebug] Spawned fake player '{info.playerName}'.");
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Application.isPlaying)
                Repaint();
        }
    }
}
#endif
