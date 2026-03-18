using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Settings
{
    /// <summary>
    /// PlayerPrefs-backed multiplayer preferences.
    /// All keys use the <c>SWEF_MP_</c> prefix.
    /// Call <see cref="ApplySettings"/> to propagate values to runtime components.
    /// </summary>
    public class MultiplayerSettings : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        /// <summary>Singleton instance; set during <see cref="Awake"/>.</summary>
        public static MultiplayerSettings Instance { get; private set; }

        // ── PlayerPrefs Keys ─────────────────────────────────────────────────────
        private const string KEY_PLAYER_NAME        = "SWEF_MP_PlayerName";
        private const string KEY_AVATAR_COLOR       = "SWEF_MP_AvatarColorIndex";
        private const string KEY_SHOW_PLAYERS       = "SWEF_MP_ShowOtherPlayers";
        private const string KEY_SHOW_LABELS        = "SWEF_MP_ShowNameLabels";
        private const string KEY_SHOW_TRAILS        = "SWEF_MP_ShowTrails";
        private const string KEY_VOICE_ENABLED      = "SWEF_MP_VoiceChatEnabled";
        private const string KEY_VOICE_VOLUME       = "SWEF_MP_VoiceChatVolume";
        private const string KEY_MAX_VISIBLE        = "SWEF_MP_MaxVisiblePlayers";
        private const string KEY_AUTO_JOIN          = "SWEF_MP_AutoJoinPublicRoom";
        private const string KEY_REGION             = "SWEF_MP_PreferredRegion";

        // ── Settings Properties ───────────────────────────────────────────────────

        /// <summary>Player display name shown to other players in multiplayer rooms.</summary>
        public string PlayerName { get; private set; }

        /// <summary>Avatar colour index (0–7) corresponding to the colour palette.</summary>
        public int AvatarColorIndex { get; private set; }

        /// <summary>Master toggle — whether remote players are rendered at all.</summary>
        public bool ShowOtherPlayers { get; private set; }

        /// <summary>Whether player name labels are displayed above remote players.</summary>
        public bool ShowNameLabels { get; private set; }

        /// <summary>Whether jet trails are rendered on remote player avatars.</summary>
        public bool ShowTrails { get; private set; }

        /// <summary>Whether proximity voice-chat simulation is active.</summary>
        public bool VoiceChatEnabled { get; private set; }

        /// <summary>Proximity voice-chat master volume (0–1).</summary>
        public float VoiceChatVolume { get; private set; }

        /// <summary>Maximum number of simultaneously visible remote players (performance cap).</summary>
        public int MaxVisiblePlayers { get; private set; }

        /// <summary>Whether to automatically join a public room on app start.</summary>
        public bool AutoJoinPublicRoom { get; private set; }

        /// <summary>Preferred server region for matchmaking ("auto" for server default).</summary>
        public string PreferredRegion { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Load();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Pushes all current settings values to the dependent runtime components
        /// (<see cref="SWEF.Multiplayer.RoomManager"/>, <see cref="SWEF.Multiplayer.PlayerSyncController"/>,
        /// <see cref="SWEF.Multiplayer.RemotePlayerRenderer"/>, <see cref="SWEF.Multiplayer.ProximityChat"/>).
        /// </summary>
        public void ApplySettings()
        {
            var proximityChat = SWEF.Multiplayer.ProximityChat.Instance != null
                ? SWEF.Multiplayer.ProximityChat.Instance
                : FindFirstObjectByType<SWEF.Multiplayer.ProximityChat>();

            if (proximityChat != null)
            {
                proximityChat.SetMasterVolume(VoiceChatVolume);
                if (!VoiceChatEnabled) proximityChat.SetAllMuted(true);
            }

            Debug.Log("[SWEF][MultiplayerSettings] Settings applied.");
        }

        /// <summary>
        /// Restores all multiplayer settings to their default values and saves them.
        /// </summary>
        public void ResetToDefaults()
        {
            PlayerName        = GenerateDefaultPlayerName();
            AvatarColorIndex  = 0;
            ShowOtherPlayers  = true;
            ShowNameLabels    = true;
            ShowTrails        = true;
            VoiceChatEnabled  = true;
            VoiceChatVolume   = 0.7f;
            MaxVisiblePlayers = 8;
            AutoJoinPublicRoom = false;
            PreferredRegion   = "auto";

            Save();
            Debug.Log("[SWEF][MultiplayerSettings] Reset to defaults.");
        }

        // ── Setters ───────────────────────────────────────────────────────────────

        /// <summary>Sets and saves the player's display name.</summary>
        public void SetPlayerName(string name)
        {
            PlayerName = string.IsNullOrWhiteSpace(name) ? GenerateDefaultPlayerName() : name;
            PlayerPrefs.SetString(KEY_PLAYER_NAME, PlayerName);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the avatar colour index (clamped 0–7).</summary>
        public void SetAvatarColorIndex(int index)
        {
            AvatarColorIndex = Mathf.Clamp(index, 0, 7);
            PlayerPrefs.SetInt(KEY_AVATAR_COLOR, AvatarColorIndex);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the show-other-players toggle.</summary>
        public void SetShowOtherPlayers(bool value)
        {
            ShowOtherPlayers = value;
            PlayerPrefs.SetInt(KEY_SHOW_PLAYERS, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the show-name-labels toggle.</summary>
        public void SetShowNameLabels(bool value)
        {
            ShowNameLabels = value;
            PlayerPrefs.SetInt(KEY_SHOW_LABELS, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the show-trails toggle.</summary>
        public void SetShowTrails(bool value)
        {
            ShowTrails = value;
            PlayerPrefs.SetInt(KEY_SHOW_TRAILS, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the voice-chat enabled toggle.</summary>
        public void SetVoiceChatEnabled(bool value)
        {
            VoiceChatEnabled = value;
            PlayerPrefs.SetInt(KEY_VOICE_ENABLED, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the voice-chat volume (clamped 0–1).</summary>
        public void SetVoiceChatVolume(float value)
        {
            VoiceChatVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KEY_VOICE_VOLUME, VoiceChatVolume);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the maximum visible players (clamped 1–16).</summary>
        public void SetMaxVisiblePlayers(int value)
        {
            MaxVisiblePlayers = Mathf.Clamp(value, 1, 16);
            PlayerPrefs.SetInt(KEY_MAX_VISIBLE, MaxVisiblePlayers);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the auto-join-public-room preference.</summary>
        public void SetAutoJoinPublicRoom(bool value)
        {
            AutoJoinPublicRoom = value;
            PlayerPrefs.SetInt(KEY_AUTO_JOIN, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Sets and saves the preferred server region.</summary>
        public void SetPreferredRegion(string region)
        {
            PreferredRegion = string.IsNullOrWhiteSpace(region) ? "auto" : region;
            PlayerPrefs.SetString(KEY_REGION, PreferredRegion);
            PlayerPrefs.Save();
        }

        // ── Private Helpers ───────────────────────────────────────────────────────

        private void Load()
        {
            PlayerName        = PlayerPrefs.GetString(KEY_PLAYER_NAME, GenerateDefaultPlayerName());
            AvatarColorIndex  = PlayerPrefs.GetInt(KEY_AVATAR_COLOR, 0);
            ShowOtherPlayers  = PlayerPrefs.GetInt(KEY_SHOW_PLAYERS,  1) == 1;
            ShowNameLabels    = PlayerPrefs.GetInt(KEY_SHOW_LABELS,   1) == 1;
            ShowTrails        = PlayerPrefs.GetInt(KEY_SHOW_TRAILS,   1) == 1;
            VoiceChatEnabled  = PlayerPrefs.GetInt(KEY_VOICE_ENABLED, 1) == 1;
            VoiceChatVolume   = PlayerPrefs.GetFloat(KEY_VOICE_VOLUME, 0.7f);
            MaxVisiblePlayers = PlayerPrefs.GetInt(KEY_MAX_VISIBLE, 8);
            AutoJoinPublicRoom = PlayerPrefs.GetInt(KEY_AUTO_JOIN, 0) == 1;
            PreferredRegion   = PlayerPrefs.GetString(KEY_REGION, "auto");
        }

        private void Save()
        {
            PlayerPrefs.SetString(KEY_PLAYER_NAME,  PlayerName);
            PlayerPrefs.SetInt(KEY_AVATAR_COLOR,    AvatarColorIndex);
            PlayerPrefs.SetInt(KEY_SHOW_PLAYERS,    ShowOtherPlayers  ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SHOW_LABELS,     ShowNameLabels    ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SHOW_TRAILS,     ShowTrails        ? 1 : 0);
            PlayerPrefs.SetInt(KEY_VOICE_ENABLED,   VoiceChatEnabled  ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_VOICE_VOLUME,  VoiceChatVolume);
            PlayerPrefs.SetInt(KEY_MAX_VISIBLE,     MaxVisiblePlayers);
            PlayerPrefs.SetInt(KEY_AUTO_JOIN,       AutoJoinPublicRoom ? 1 : 0);
            PlayerPrefs.SetString(KEY_REGION,       PreferredRegion);
            PlayerPrefs.Save();
        }

        private static string GenerateDefaultPlayerName()
        {
            int suffix = UnityEngine.Random.Range(1000, 9999);
            return $"Pilot{suffix}";
        }
    }
}
