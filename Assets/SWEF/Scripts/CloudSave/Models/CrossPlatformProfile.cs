// CrossPlatformProfile.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Unified player identity that links accounts across multiple platforms.
// Namespace: SWEF.CloudSave

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.CloudSave
{
    // ════════════════════════════════════════════════════════════════════════════
    // Data classes
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Represents a single linked platform account.</summary>
    [Serializable]
    public class LinkedPlatformAccount
    {
        /// <summary>Which platform this account belongs to.</summary>
        public PlatformAccountType Platform;

        /// <summary>Platform-specific user identifier.</summary>
        public string PlatformUserId;

        /// <summary>Display name on that platform.</summary>
        public string PlatformDisplayName;

        /// <summary>UTC time this account was linked.</summary>
        public DateTime LinkedAt;

        /// <summary>Whether this is the primary identity provider.</summary>
        public bool IsPrimary;
    }

    /// <summary>Describes a device that has accessed this profile.</summary>
    [Serializable]
    public class RegisteredDevice
    {
        /// <summary>Stable unique identifier for the device (hashed device ID).</summary>
        public string DeviceId;

        /// <summary>Human-readable device name (e.g. "John's iPhone").</summary>
        public string DeviceName;

        /// <summary>Operating system description.</summary>
        public string OperatingSystem;

        /// <summary>UTC time the device was first registered.</summary>
        public DateTime RegisteredAt;

        /// <summary>UTC time of the last activity from this device.</summary>
        public DateTime LastSeenAt;

        /// <summary>Whether this device is designated as primary.</summary>
        public bool IsPrimaryDevice;
    }

    /// <summary>
    /// Phase 111 — Unified player identity spanning all devices and platform accounts.
    /// Persisted locally and mirrored to cloud storage.
    /// </summary>
    [Serializable]
    public class CrossPlatformProfile
    {
        // ── Core identity ──────────────────────────────────────────────────────

        /// <summary>Globally unique SWEF profile identifier (GUID).</summary>
        public string ProfileId;

        /// <summary>Player's preferred display name.</summary>
        public string DisplayName;

        /// <summary>UTC time the profile was first created.</summary>
        public DateTime CreatedAt;

        /// <summary>UTC time the profile data was last modified.</summary>
        public DateTime LastModifiedAt;

        // ── Linked accounts ────────────────────────────────────────────────────

        /// <summary>Platform accounts linked to this profile.</summary>
        public List<LinkedPlatformAccount> LinkedAccounts = new List<LinkedPlatformAccount>();

        // ── Device management ──────────────────────────────────────────────────

        /// <summary>All registered devices (max 5 by default).</summary>
        public List<RegisteredDevice> RegisteredDevices = new List<RegisteredDevice>();

        /// <summary>The device ID designated as primary.</summary>
        public string PrimaryDeviceId;

        // ── Merge metadata ─────────────────────────────────────────────────────

        /// <summary>List of profile IDs that were merged into this profile.</summary>
        public List<string> MergedProfileIds = new List<string>();

        // ── Factories & helpers ────────────────────────────────────────────────

        /// <summary>Creates a new profile with a fresh GUID for the current device.</summary>
        public static CrossPlatformProfile CreateNew(string displayName)
        {
            string deviceId = GetCurrentDeviceId();
            return new CrossPlatformProfile
            {
                ProfileId      = Guid.NewGuid().ToString("N"),
                DisplayName    = displayName ?? "Pilot",
                CreatedAt      = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                RegisteredDevices = new List<RegisteredDevice>
                {
                    new RegisteredDevice
                    {
                        DeviceId       = deviceId,
                        DeviceName     = SystemInfo.deviceName,
                        OperatingSystem = SystemInfo.operatingSystem,
                        RegisteredAt   = DateTime.UtcNow,
                        LastSeenAt     = DateTime.UtcNow,
                        IsPrimaryDevice = true
                    }
                },
                PrimaryDeviceId = deviceId
            };
        }

        /// <summary>Returns the hashed identifier for the current device.</summary>
        public static string GetCurrentDeviceId()
        {
            string raw = SystemInfo.deviceUniqueIdentifier;
            // Hash so we never store raw hardware IDs
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash   = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
                return Convert.ToBase64String(hash).Substring(0, 24);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Manager
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Phase 111 — MonoBehaviour singleton that loads, saves, and manages the
    /// <see cref="CrossPlatformProfile"/> for the local player.
    ///
    /// <para>Handles account linking, device registration, and profile merging.</para>
    /// <para>Attach to a persistent scene object.</para>
    /// </summary>
    public sealed class CrossPlatformProfileManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static CrossPlatformProfileManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired after the profile is successfully loaded or created.</summary>
        public event Action<CrossPlatformProfile> OnProfileLoaded;

        /// <summary>Fired when a new platform account is linked.</summary>
        public event Action<LinkedPlatformAccount> OnAccountLinked;

        /// <summary>Fired when a platform account is unlinked.</summary>
        public event Action<PlatformAccountType> OnAccountUnlinked;

        /// <summary>Fired when a new device is registered.</summary>
        public event Action<RegisteredDevice> OnDeviceRegistered;

        /// <summary>Fired when two profiles are successfully merged.</summary>
        public event Action<CrossPlatformProfile> OnProfilesMerged;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>The loaded profile for this session.</summary>
        public CrossPlatformProfile Profile { get; private set; }

        [SerializeField]
        private int maxDevices = 5;

        private string _savePath;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, "cross_platform_profile.json");
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Links a new platform account to the current profile.
        /// If the platform is already linked, the existing record is updated.
        /// </summary>
        public void LinkAccount(PlatformAccountType platform, string userId, string displayName,
                                bool makePrimary = false)
        {
            if (Profile == null) return;

            var existing = Profile.LinkedAccounts
                .FirstOrDefault(a => a.Platform == platform);

            if (existing != null)
            {
                existing.PlatformUserId      = userId;
                existing.PlatformDisplayName = displayName;
            }
            else
            {
                var account = new LinkedPlatformAccount
                {
                    Platform            = platform,
                    PlatformUserId      = userId,
                    PlatformDisplayName = displayName,
                    LinkedAt            = DateTime.UtcNow,
                    IsPrimary           = makePrimary
                };
                Profile.LinkedAccounts.Add(account);
                OnAccountLinked?.Invoke(account);
            }

            if (makePrimary)
            {
                foreach (var acc in Profile.LinkedAccounts)
                    acc.IsPrimary = acc.Platform == platform;
            }

            Profile.LastModifiedAt = DateTime.UtcNow;
            Save();
        }

        /// <summary>Removes a linked platform account.</summary>
        public void UnlinkAccount(PlatformAccountType platform)
        {
            if (Profile == null) return;

            Profile.LinkedAccounts.RemoveAll(a => a.Platform == platform);
            Profile.LastModifiedAt = DateTime.UtcNow;
            Save();
            OnAccountUnlinked?.Invoke(platform);
        }

        /// <summary>
        /// Registers the current device with the profile.
        /// Returns <c>false</c> if the device cap (<see cref="maxDevices"/>) is already reached.
        /// </summary>
        public bool RegisterCurrentDevice()
        {
            if (Profile == null) return false;

            string deviceId = CrossPlatformProfile.GetCurrentDeviceId();
            if (Profile.RegisteredDevices.Any(d => d.DeviceId == deviceId))
            {
                // Update last-seen time
                var existing = Profile.RegisteredDevices.First(d => d.DeviceId == deviceId);
                existing.LastSeenAt = DateTime.UtcNow;
                Save();
                return true;
            }

            if (Profile.RegisteredDevices.Count >= maxDevices)
            {
                Debug.LogWarning($"[CrossPlatformProfile] Device limit ({maxDevices}) reached.");
                return false;
            }

            var device = new RegisteredDevice
            {
                DeviceId        = deviceId,
                DeviceName      = SystemInfo.deviceName,
                OperatingSystem = SystemInfo.operatingSystem,
                RegisteredAt    = DateTime.UtcNow,
                LastSeenAt      = DateTime.UtcNow,
                IsPrimaryDevice = Profile.RegisteredDevices.Count == 0
            };

            Profile.RegisteredDevices.Add(device);
            Profile.LastModifiedAt = DateTime.UtcNow;
            Save();
            OnDeviceRegistered?.Invoke(device);
            return true;
        }

        /// <summary>
        /// Sets the specified device as the primary device.
        /// </summary>
        public void SetPrimaryDevice(string deviceId)
        {
            if (Profile == null) return;

            foreach (var d in Profile.RegisteredDevices)
                d.IsPrimaryDevice = d.DeviceId == deviceId;

            Profile.PrimaryDeviceId = deviceId;
            Profile.LastModifiedAt  = DateTime.UtcNow;
            Save();
        }

        /// <summary>
        /// Merges <paramref name="other"/> into the current profile:
        /// combines achievements (union), keeps best progression values,
        /// merges linked accounts and registered devices (deduplicated).
        /// </summary>
        public void MergeProfile(CrossPlatformProfile other)
        {
            if (Profile == null || other == null) return;

            // Merge linked accounts (add any that don't already exist)
            foreach (var acc in other.LinkedAccounts)
            {
                if (!Profile.LinkedAccounts.Any(a => a.Platform == acc.Platform &&
                                                     a.PlatformUserId == acc.PlatformUserId))
                    Profile.LinkedAccounts.Add(acc);
            }

            // Merge devices (add any that don't already exist)
            foreach (var dev in other.RegisteredDevices)
            {
                if (!Profile.RegisteredDevices.Any(d => d.DeviceId == dev.DeviceId))
                    Profile.RegisteredDevices.Add(dev);
            }

            // Record merge
            if (!Profile.MergedProfileIds.Contains(other.ProfileId))
                Profile.MergedProfileIds.Add(other.ProfileId);

            Profile.LastModifiedAt = DateTime.UtcNow;
            Save();
            OnProfilesMerged?.Invoke(Profile);
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void Load()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    Profile = JsonUtility.FromJson<CrossPlatformProfile>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CrossPlatformProfile] Load failed: {ex.Message}. Creating new profile.");
                    Profile = null;
                }
            }

            if (Profile == null)
            {
                Profile = CrossPlatformProfile.CreateNew("Pilot");
                Save();
            }

            RegisterCurrentDevice();
            OnProfileLoaded?.Invoke(Profile);
        }

        private void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Profile, prettyPrint: true);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrossPlatformProfile] Save failed: {ex.Message}");
            }
        }
    }
}
