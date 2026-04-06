// CloudSaveTests.cs — NUnit EditMode tests for Phase 111: Cloud Save & Cross-Platform Sync
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using SWEF.CloudSave;

[TestFixture]
public class CloudSaveTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CloudSaveData — Enums
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CloudProviderType_AllValuesAreDefined()
    {
        var values = (CloudProviderType[])Enum.GetValues(typeof(CloudProviderType));
        Assert.Contains(CloudProviderType.LocalFile,  values);
        Assert.Contains(CloudProviderType.UnityCloud, values);
        Assert.Contains(CloudProviderType.Firebase,   values);
        Assert.Contains(CloudProviderType.CustomREST, values);
    }

    [Test]
    public void ProviderConnectionStatus_AllValuesAreDefined()
    {
        var values = (ProviderConnectionStatus[])Enum.GetValues(typeof(ProviderConnectionStatus));
        Assert.Contains(ProviderConnectionStatus.Disconnected, values);
        Assert.Contains(ProviderConnectionStatus.Connecting,   values);
        Assert.Contains(ProviderConnectionStatus.Connected,    values);
        Assert.Contains(ProviderConnectionStatus.Error,        values);
        Assert.Contains(ProviderConnectionStatus.Offline,      values);
    }

    [Test]
    public void SyncStatus_AllValuesAreDefined()
    {
        var values = (SyncStatus[])Enum.GetValues(typeof(SyncStatus));
        Assert.Contains(SyncStatus.Synced,           values);
        Assert.Contains(SyncStatus.Syncing,          values);
        Assert.Contains(SyncStatus.PendingUpload,    values);
        Assert.Contains(SyncStatus.PendingDownload,  values);
        Assert.Contains(SyncStatus.Conflict,         values);
        Assert.Contains(SyncStatus.Error,            values);
        Assert.Contains(SyncStatus.Unavailable,      values);
    }

    [Test]
    public void ConflictResolutionStrategy_AllValuesAreDefined()
    {
        var values = (ConflictResolutionStrategy[])Enum.GetValues(typeof(ConflictResolutionStrategy));
        Assert.Contains(ConflictResolutionStrategy.LastWriteWins,    values);
        Assert.Contains(ConflictResolutionStrategy.MergeByTimestamp, values);
        Assert.Contains(ConflictResolutionStrategy.PromptUser,       values);
    }

    [Test]
    public void PlatformAccountType_AllValuesAreDefined()
    {
        var values = (PlatformAccountType[])Enum.GetValues(typeof(PlatformAccountType));
        Assert.Contains(PlatformAccountType.Steam,            values);
        Assert.Contains(PlatformAccountType.AppleGameCenter,  values);
        Assert.Contains(PlatformAccountType.GooglePlayGames,  values);
        Assert.Contains(PlatformAccountType.Custom,           values);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProviderStatus
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ProviderStatus_QuotaFraction_ZeroWhenTotalIsZero()
    {
        var status = new ProviderStatus
        {
            QuotaUsedBytes  = 500,
            QuotaTotalBytes = 0
        };
        Assert.AreEqual(0f, status.QuotaFraction);
    }

    [Test]
    public void ProviderStatus_QuotaFraction_CorrectFraction()
    {
        var status = new ProviderStatus
        {
            QuotaUsedBytes  = 512,
            QuotaTotalBytes = 1024
        };
        Assert.AreApproximatelyEqual(0.5f, status.QuotaFraction, 0.001f);
    }

    [Test]
    public void ProviderStatus_QuotaFraction_ClampedAtOne()
    {
        var status = new ProviderStatus
        {
            QuotaUsedBytes  = 2048,
            QuotaTotalBytes = 1024
        };
        Assert.AreEqual(1f, status.QuotaFraction);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SaveDataRegistry
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SaveDataRegistry_HasBuiltInKeys()
    {
        var registry = SaveDataRegistry.Instance;
        Assert.IsNotNull(registry.GetRecord("player_profile"));
        Assert.IsNotNull(registry.GetRecord("achievements"));
        Assert.IsNotNull(registry.GetRecord("settings"));
        Assert.IsNotNull(registry.GetRecord("flight_journal"));
        Assert.IsNotNull(registry.GetRecord("workshop_builds"));
    }

    [Test]
    public void SaveDataRegistry_Register_AddsNewKey()
    {
        var registry = SaveDataRegistry.Instance;
        registry.Register("test_custom_key_111",
            Path.Combine(Application.persistentDataPath, "test_custom_key_111.json"));
        Assert.IsNotNull(registry.GetRecord("test_custom_key_111"));
    }

    [Test]
    public void SaveDataRegistry_Register_NullKeyThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SaveDataRegistry.Instance.Register(null, "/some/path.json"));
    }

    [Test]
    public void SaveDataRegistry_Register_NullPathThrows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SaveDataRegistry.Instance.Register("some_key_111", null));
    }

    [Test]
    public void SaveDataRegistry_AllRecords_NotEmpty()
    {
        Assert.Greater(SaveDataRegistry.Instance.AllRecords.Count, 0);
    }

    [Test]
    public void SaveDataRegistry_MarkClean_SetsCloudTimestamp()
    {
        var registry = SaveDataRegistry.Instance;
        // Ensure a known key is present
        registry.Register("mark_clean_test_111",
            Path.Combine(Application.persistentDataPath, "mark_clean_test_111.json"));

        DateTime ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        registry.MarkClean("mark_clean_test_111", ts);

        var rec = registry.GetRecord("mark_clean_test_111");
        Assert.IsFalse(rec.IsDirty);
        Assert.AreEqual(ts, rec.CloudModifiedAt);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CrossPlatformProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CrossPlatformProfile_CreateNew_HasProfileId()
    {
        var profile = CrossPlatformProfile.CreateNew("TestPilot");
        Assert.IsFalse(string.IsNullOrEmpty(profile.ProfileId));
    }

    [Test]
    public void CrossPlatformProfile_CreateNew_HasDisplayName()
    {
        var profile = CrossPlatformProfile.CreateNew("Ace");
        Assert.AreEqual("Ace", profile.DisplayName);
    }

    [Test]
    public void CrossPlatformProfile_CreateNew_DefaultsToOnePilotDevice()
    {
        var profile = CrossPlatformProfile.CreateNew("Test");
        Assert.AreEqual(1, profile.RegisteredDevices.Count);
        Assert.IsTrue(profile.RegisteredDevices[0].IsPrimaryDevice);
    }

    [Test]
    public void CrossPlatformProfile_CreateNew_NullNameDefaultsToPilot()
    {
        var profile = CrossPlatformProfile.CreateNew(null);
        Assert.AreEqual("Pilot", profile.DisplayName);
    }

    [Test]
    public void CrossPlatformProfile_GetCurrentDeviceId_ReturnsNonEmpty()
    {
        string id = CrossPlatformProfile.GetCurrentDeviceId();
        Assert.IsFalse(string.IsNullOrEmpty(id));
    }

    [Test]
    public void CrossPlatformProfile_GetCurrentDeviceId_IsDeterministic()
    {
        string id1 = CrossPlatformProfile.GetCurrentDeviceId();
        string id2 = CrossPlatformProfile.GetCurrentDeviceId();
        Assert.AreEqual(id1, id2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LocalFileProvider
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LocalFileProvider_ProviderType_IsLocalFile()
    {
        var provider = new LocalFileProvider();
        Assert.AreEqual(CloudProviderType.LocalFile, provider.ProviderType);
    }

    [Test]
    public void LocalFileProvider_ProviderName_IsLocalFile()
    {
        var provider = new LocalFileProvider();
        Assert.AreEqual("Local File", provider.ProviderName);
    }

    [Test]
    public void LocalFileProvider_GetStatus_BeforeInit_ReturnsDisconnected()
    {
        var provider = new LocalFileProvider();
        var status   = provider.GetStatus();
        Assert.AreEqual(ProviderConnectionStatus.Disconnected, status.ConnectionStatus);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UnityCloudSaveProvider
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void UnityCloudSaveProvider_ProviderType_IsUnityCloud()
    {
        var provider = new UnityCloudSaveProvider();
        Assert.AreEqual(CloudProviderType.UnityCloud, provider.ProviderType);
    }

    [Test]
    public void UnityCloudSaveProvider_GetStatus_ReturnsDisconnectedBeforeInit()
    {
        var provider = new UnityCloudSaveProvider();
        var status   = provider.GetStatus();
        // Not initialised — should not be Connected
        Assert.AreNotEqual(ProviderConnectionStatus.Connected, status.ConnectionStatus);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FirebaseProvider
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void FirebaseProvider_ProviderType_IsFirebase()
    {
        var provider = new FirebaseProvider();
        Assert.AreEqual(CloudProviderType.Firebase, provider.ProviderType);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CustomRESTProvider
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CustomRESTProvider_ProviderType_IsCustomREST()
    {
        var provider = new CustomRESTProvider("https://example.com/saves", "api-key");
        Assert.AreEqual(CloudProviderType.CustomREST, provider.ProviderType);
    }

    [Test]
    public void CustomRESTProvider_ProviderName_IsCustomREST()
    {
        var provider = new CustomRESTProvider("https://example.com/saves", "api-key");
        Assert.AreEqual("Custom REST", provider.ProviderName);
    }

    [Test]
    public void CustomRESTProvider_GetStatus_BeforeInit_NotConnected()
    {
        var provider = new CustomRESTProvider(string.Empty, string.Empty);
        var status   = provider.GetStatus();
        Assert.AreNotEqual(ProviderConnectionStatus.Connected, status.ConnectionStatus);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SaveDataMigrator
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SaveDataMigrator_CurrentVersion_GreaterThanZero()
    {
        Assert.Greater(SaveDataMigrator.CurrentVersion, 0);
    }

    [Test]
    public void SaveDataMigrator_RegisterStep_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            SaveDataMigrator.Instance.RegisterStep(new MigrationV1ToV2()));
    }

    [Test]
    public void MigrationV1ToV2_AddsSchemaVersionField()
    {
        var step   = new MigrationV1ToV2();
        string src = "{\"name\": \"test\"}";
        string result = step.Migrate(src);
        Assert.IsTrue(result.Contains("\"schemaVersion\": 2"),
            $"Expected schemaVersion in: {result}");
    }

    [Test]
    public void MigrationV1ToV2_DoesNotDuplicateSchemaVersion()
    {
        var step   = new MigrationV1ToV2();
        string src = "{\"schemaVersion\": 2, \"name\": \"test\"}";
        string result = step.Migrate(src);
        int count = CountOccurrences(result, "\"schemaVersion\"");
        Assert.AreEqual(1, count, "schemaVersion should appear exactly once.");
    }

    [Test]
    public void MigrationV1ToV2_HandlesEmptyObject()
    {
        var step   = new MigrationV1ToV2();
        string src = "{}";
        string result = step.Migrate(src);
        Assert.IsTrue(result.Contains("\"schemaVersion\": 2"));
    }

    [Test]
    public void MigrationV1ToV2_FromVersion_IsOne()
    {
        Assert.AreEqual(1, new MigrationV1ToV2().FromVersion);
    }

    [Test]
    public void MigrationV1ToV2_ToVersion_IsTwo()
    {
        Assert.AreEqual(2, new MigrationV1ToV2().ToVersion);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConflictResolver
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void ConflictResolver_LastWriteWins_CloudNewer_ReturnsCloud()
    {
        var resolver = ConflictResolver.Instance;
        var conflict = new SaveConflict
        {
            FileKey         = "test_key",
            LocalModifiedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            CloudModifiedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalData       = new byte[] { 1, 2, 3 },
            CloudData       = new byte[] { 4, 5, 6 }
        };

        byte[] result = resolver.Resolve(conflict, ConflictResolutionStrategy.LastWriteWins);
        CollectionAssert.AreEqual(conflict.CloudData, result);
    }

    [Test]
    public void ConflictResolver_LastWriteWins_LocalNewer_ReturnsLocal()
    {
        var resolver = ConflictResolver.Instance;
        var conflict = new SaveConflict
        {
            FileKey         = "test_key2",
            LocalModifiedAt = new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc),
            CloudModifiedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalData       = new byte[] { 1, 2, 3 },
            CloudData       = new byte[] { 4, 5, 6 }
        };

        byte[] result = resolver.Resolve(conflict, ConflictResolutionStrategy.LastWriteWins);
        CollectionAssert.AreEqual(conflict.LocalData, result);
    }

    [Test]
    public void ConflictResolver_NullConflict_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConflictResolver.Instance.Resolve(null, ConflictResolutionStrategy.LastWriteWins));
    }

    [Test]
    public void ConflictResolver_PromptUser_ReturnsPendingLocalData()
    {
        var resolver = ConflictResolver.Instance;
        var conflict = new SaveConflict
        {
            FileKey         = "prompt_test",
            LocalModifiedAt = DateTime.UtcNow,
            CloudModifiedAt = DateTime.UtcNow,
            LocalData       = new byte[] { 99 },
            CloudData       = new byte[] { 88 }
        };

        byte[] result = resolver.Resolve(conflict, ConflictResolutionStrategy.PromptUser);
        // PromptUser returns local data as placeholder
        CollectionAssert.AreEqual(conflict.LocalData, result);
    }

    [Test]
    public void ConflictResolver_ResolveUserChoice_SetsChoice()
    {
        var resolver = ConflictResolver.Instance;
        string key   = "user_choice_test";
        var conflict = new SaveConflict
        {
            FileKey         = key,
            LocalModifiedAt = DateTime.UtcNow,
            CloudModifiedAt = DateTime.UtcNow,
            LocalData       = new byte[] { 1 },
            CloudData       = new byte[] { 2 }
        };

        // Trigger prompt to register the key
        resolver.Resolve(conflict, ConflictResolutionStrategy.PromptUser);

        resolver.ResolveUserChoice(key, ConflictChoice.UseCloud);
        Assert.AreEqual(ConflictChoice.UseCloud, resolver.GetPendingChoice(key));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CloudSaveConfig
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void CloudSaveConfig_DefaultProvider_IsLocalFile()
    {
        var cfg = ScriptableObject.CreateInstance<CloudSaveConfig>();
        Assert.AreEqual(CloudProviderType.LocalFile, cfg.providerType);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void CloudSaveConfig_DefaultDebounce_IsThirtySeconds()
    {
        var cfg = ScriptableObject.CreateInstance<CloudSaveConfig>();
        Assert.AreEqual(30f, cfg.autoSyncDebounceSeconds);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void CloudSaveConfig_DefaultConflictStrategy_IsLastWriteWins()
    {
        var cfg = ScriptableObject.CreateInstance<CloudSaveConfig>();
        Assert.AreEqual(ConflictResolutionStrategy.LastWriteWins, cfg.conflictStrategy);
        ScriptableObject.DestroyImmediate(cfg);
    }

    [Test]
    public void CloudSaveConfig_MaxDevicesDefault_IsFive()
    {
        var cfg = ScriptableObject.CreateInstance<CloudSaveConfig>();
        Assert.AreEqual(5, cfg.maxDevices);
        ScriptableObject.DestroyImmediate(cfg);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // OfflineQueueEntry
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void OfflineQueueEntry_CanBeCreated()
    {
        var entry = new OfflineQueueEntry
        {
            FileKey    = "test",
            QueuedAt   = DateTime.UtcNow,
            RetryCount = 0
        };
        Assert.AreEqual("test", entry.FileKey);
        Assert.AreEqual(0, entry.RetryCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SaveFileRecord
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SaveFileRecord_CanBeCreated()
    {
        var record = new SaveFileRecord
        {
            FileKey          = "player_profile",
            LocalPath        = "/tmp/player_profile.json",
            LocalModifiedAt  = DateTime.UtcNow,
            CloudModifiedAt  = DateTime.MinValue,
            LocalContentHash = string.Empty,
            IsDirty          = false
        };
        Assert.AreEqual("player_profile", record.FileKey);
        Assert.IsFalse(record.IsDirty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static int CountOccurrences(string source, string token)
    {
        int count = 0, index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }
}
