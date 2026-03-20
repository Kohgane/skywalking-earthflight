using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.SaveSystem
{
    /// <summary>
    /// Phase 35 — Central save manager.
    /// Supports 5 slots (0–2 manual, 3 auto-save, 4 quicksave), ISaveable
    /// auto-discovery, optional GZip compression, AES-256 encryption, and
    /// cloud sync integration via <see cref="CloudSyncManager"/>.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        /// <summary>Global singleton instance.</summary>
        public static SaveManager Instance { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Raised when a save operation begins for <paramref name="slotIndex"/>.</summary>
        public event Action<int>       OnSaveStarted;

        /// <summary>Raised when a save operation finishes. <c>success</c> is <c>false</c> on error.</summary>
        public event Action<int, bool> OnSaveCompleted;

        /// <summary>Raised when a load operation begins for <paramref name="slotIndex"/>.</summary>
        public event Action<int>       OnLoadStarted;

        /// <summary>Raised when a load operation finishes. <c>success</c> is <c>false</c> on error.</summary>
        public event Action<int, bool> OnLoadCompleted;

        /// <summary>Raised each time the auto-save timer fires.</summary>
        public event Action            OnAutoSaveTriggered;

        /// <summary>Raised after a slot is deleted.</summary>
        public event Action<int>       OnSlotDeleted;

        // ── Inspector config ─────────────────────────────────────────────────
        [Header("Auto-Save")]
        [Tooltip("Enable periodic auto-saving.")]
        [SerializeField] private bool  enableAutoSave              = true;
        [Tooltip("Seconds between auto-saves (default 300 = 5 minutes).")]
        [SerializeField] private float autoSaveIntervalSec         = 300f;
        [Tooltip("Trigger an auto-save on every scene transition.")]
        [SerializeField] private bool  autoSaveOnSceneTransition   = true;

        [Header("Security")]
        [Tooltip("Compress save data with GZip before writing.")]
        [SerializeField] private bool  enableCompression = true;
        [Tooltip("Encrypt save data with AES-256 before writing.")]
        [SerializeField] private bool  enableEncryption  = true;

        // ── Public state ─────────────────────────────────────────────────────
        /// <summary><c>true</c> while a save or load operation is running.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>Gets or sets whether the periodic auto-save is active.</summary>
        public bool AutoSaveEnabled
        {
            get => enableAutoSave;
            set => enableAutoSave = value;
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private float            _autoSaveTimer;
        private bool             _autoSaveDisabled;
        private SaveSlotInfo[]   _slotInfos;
        private List<ISaveable>  _saveables       = new List<ISaveable>();
        private string           _saveDirectory;

        private const string EncryptionSeed = "SWEF_SaveSystem_Phase35_v1";

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveDirectory = Path.Combine(Application.persistentDataPath, SaveSystemConstants.SaveDirectory);
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);

            LoadAllSlotInfos();
        }

        private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Update()
        {
            if (!enableAutoSave || _autoSaveDisabled || IsBusy) return;
            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= autoSaveIntervalSec)
            {
                _autoSaveTimer = 0f;
                StartCoroutine(AutoSaveCoroutine());
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && enableAutoSave && !_autoSaveDisabled && !IsBusy)
                StartCoroutine(AutoSaveCoroutine());
        }

        private void OnApplicationQuit()
        {
            if (enableAutoSave && !_autoSaveDisabled && !IsBusy)
                StartCoroutine(AutoSaveCoroutine());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DiscoverSaveables();
            if (autoSaveOnSceneTransition && enableAutoSave && !_autoSaveDisabled && !IsBusy)
                StartCoroutine(AutoSaveCoroutine());
        }

        // ── ISaveable discovery ───────────────────────────────────────────────
        /// <summary>
        /// Scans the scene for all <see cref="ISaveable"/> MonoBehaviours and registers them.
        /// Called automatically on scene load; may also be called manually.
        /// </summary>
        public void DiscoverSaveables()
        {
            _saveables.Clear();
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is ISaveable saveable)
                    _saveables.Add(saveable);
            }
            Debug.Log($"[SWEF] SaveManager: discovered {_saveables.Count} ISaveable component(s).");
        }

        /// <summary>Manually registers an <see cref="ISaveable"/> (e.g., spawned at runtime).</summary>
        public void Register(ISaveable saveable)
        {
            if (saveable != null && !_saveables.Contains(saveable))
                _saveables.Add(saveable);
        }

        /// <summary>Removes an <see cref="ISaveable"/> from the registry.</summary>
        public void Unregister(ISaveable saveable)
        {
            _saveables.Remove(saveable);
        }

        // ── Slot metadata ─────────────────────────────────────────────────────
        /// <summary>Returns slot metadata for <paramref name="slotIndex"/>, or <c>null</c>.</summary>
        public SaveSlotInfo GetSlotInfo(int slotIndex) =>
            (slotIndex >= 0 && slotIndex < SaveSystemConstants.TotalSlots) ? _slotInfos[slotIndex] : null;

        /// <summary>Returns a copy of the metadata array for all slots.</summary>
        public SaveSlotInfo[] GetAllSlotInfos() => _slotInfos;

        // ── Public save / load API ────────────────────────────────────────────
        /// <summary>Saves to a manual slot (0–2). Queues if a save/load is already running.</summary>
        /// <param name="slotIndex">Manual slot index 0–2.</param>
        /// <param name="displayName">Display name shown in the save-slot UI. Defaults to "Save N".</param>
        public void Save(int slotIndex, string displayName = null)
        {
            if (slotIndex < 0 || slotIndex >= SaveSystemConstants.ManualSlotCount)
            {
                Debug.LogError($"[SWEF] SaveManager: Save() — invalid slot {slotIndex}. Manual slots are 0–{SaveSystemConstants.ManualSlotCount - 1}.");
                return;
            }
            StartCoroutine(SaveCoroutine(slotIndex, displayName ?? $"Save {slotIndex + 1}"));
        }

        /// <summary>Loads from any slot (0–4). Queues if a save/load is already running.</summary>
        public void Load(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SaveSystemConstants.TotalSlots)
            {
                Debug.LogError($"[SWEF] SaveManager: Load() — invalid slot {slotIndex}.");
                return;
            }
            StartCoroutine(LoadCoroutine(slotIndex));
        }

        /// <summary>Deletes save data for <paramref name="slotIndex"/> from disk.</summary>
        public void Delete(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SaveSystemConstants.TotalSlots)
            {
                Debug.LogError($"[SWEF] SaveManager: Delete() — invalid slot {slotIndex}.");
                return;
            }
            DeleteSlot(slotIndex);
        }

        /// <summary>Saves to the dedicated quick-save slot (slot 4).</summary>
        public void QuickSave() => StartCoroutine(SaveCoroutine(SaveSystemConstants.QuickSaveSlot, "Quick Save"));

        /// <summary>Loads from the dedicated quick-save slot (slot 4).</summary>
        public void QuickLoad() => StartCoroutine(LoadCoroutine(SaveSystemConstants.QuickSaveSlot));

        /// <summary>Temporarily disables auto-save (e.g., during cutscenes or tutorials).</summary>
        public void SuspendAutoSave()  => _autoSaveDisabled = true;

        /// <summary>Re-enables auto-save after a temporary suspension.</summary>
        public void ResumeAutoSave()   => _autoSaveDisabled = false;

        // ── File path helpers ─────────────────────────────────────────────────
        /// <summary>Full path to the save blob for <paramref name="slotIndex"/>.</summary>
        public string GetSavePath(int slotIndex) =>
            Path.Combine(_saveDirectory, $"slot_{slotIndex}{SaveSystemConstants.SaveExtension}");

        /// <summary>Full path to the sidecar metadata file for <paramref name="slotIndex"/>.</summary>
        public string GetMetaPath(int slotIndex) =>
            Path.Combine(_saveDirectory, $"slot_{slotIndex}{SaveSystemConstants.MetaExtension}");

        // ── Coroutines ────────────────────────────────────────────────────────
        private IEnumerator AutoSaveCoroutine()
        {
            OnAutoSaveTriggered?.Invoke();
            Debug.Log("[SWEF] SaveManager: auto-save triggered.");
            yield return SaveCoroutine(SaveSystemConstants.AutoSaveSlot, "Auto Save");
        }

        private IEnumerator SaveCoroutine(int slotIndex, string displayName)
        {
            // Queue behind any running operation
            while (IsBusy) yield return null;

            IsBusy = true;
            OnSaveStarted?.Invoke(slotIndex);

            // Yield one frame so any in-progress UI renders before we block
            yield return null;

            bool   success = false;
            string errMsg  = null;

            try
            {
                // ── 1. Gather ISaveable state ──────────────────────────────────
                var payload = new SavePayload();
                foreach (var saveable in _saveables)
                {
                    try
                    {
                        object state = saveable.CaptureState();
                        payload.Set(saveable.SaveKey, JsonUtility.ToJson(state));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SWEF] SaveManager: CaptureState failed for '{saveable.SaveKey}' — {ex.Message}");
                    }
                }

                // ── 2. Gather known subsystem data ─────────────────────────────
                GatherSubsystemData(payload);

                // ── 3. Build SaveFile ──────────────────────────────────────────
                long nowTicks   = DateTime.UtcNow.Ticks;
                var  slotInfo   = _slotInfos[slotIndex];
                long createTick = slotInfo.isEmpty ? nowTicks : slotInfo.creationTimestampTicks;

                var saveFile = new SaveFile
                {
                    header = new SaveFileHeader
                    {
                        magic                  = "SWEF",
                        formatVersion          = SaveSystemConstants.CurrentSaveVersion,
                        creationTimestamp      = createTick,
                        lastModifiedTimestamp  = nowTicks,
                        totalPlayTimeSec       = Time.realtimeSinceStartup,
                        gameVersion            = Application.version,
                        platform               = Application.platform.ToString()
                    },
                    payload  = payload,
                    progress = CaptureProgress()
                };

                // ── 4. Serialize ───────────────────────────────────────────────
                string json = JsonUtility.ToJson(saveFile, prettyPrint: false);
                byte[] data = Encoding.UTF8.GetBytes(json);

                // ── 5. Compress ────────────────────────────────────────────────
                if (enableCompression)
                    data = Compress(data);

                // ── 6. Encrypt ─────────────────────────────────────────────────
                if (enableEncryption)
                    data = Encrypt(data);

                // ── 7. Checksum ────────────────────────────────────────────────
                string checksum = SaveIntegrityChecker.ComputeChecksum(data);

                // ── 8. Write to disk ───────────────────────────────────────────
                File.WriteAllBytes(GetSavePath(slotIndex), data);

                // ── 9. Update and persist slot metadata ────────────────────────
                var newInfo = new SaveSlotInfo
                {
                    slotIndex               = slotIndex,
                    displayName             = displayName,
                    timestamp               = new DateTime(nowTicks, DateTimeKind.Utc).ToString("o"),
                    playTimeSec             = Time.realtimeSinceStartup,
                    thumbnailPath           = "",
                    saveVersion             = SaveSystemConstants.CurrentSaveVersion,
                    checksum                = checksum,
                    creationTimestampTicks  = createTick,
                    cloudSyncStatus         = CloudSyncStatus.LocalAhead,
                    isEmpty                 = false
                };
                _slotInfos[slotIndex] = newInfo;
                PersistSlotMeta(slotIndex, newInfo);

                success = true;
                Debug.Log($"[SWEF] SaveManager: slot {slotIndex} ('{displayName}') saved.");
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Debug.LogError($"[SWEF] SaveManager: save to slot {slotIndex} failed — {ex.Message}");
            }

            IsBusy = false;
            OnSaveCompleted?.Invoke(slotIndex, success);
            if (!success)
                Debug.LogWarning($"[SWEF] SaveManager: save slot {slotIndex} error — {errMsg}");
        }

        private IEnumerator LoadCoroutine(int slotIndex)
        {
            // Queue behind any running operation
            while (IsBusy) yield return null;

            IsBusy = true;
            OnLoadStarted?.Invoke(slotIndex);
            yield return null;

            bool   success = false;
            string errMsg  = null;

            try
            {
                string filePath = GetSavePath(slotIndex);
                if (!File.Exists(filePath))
                {
                    errMsg = "no save file";
                    Debug.LogWarning($"[SWEF] SaveManager: slot {slotIndex} has no save file.");
                }
                else
                {
                    // ── 1. Read ────────────────────────────────────────────────
                    byte[] data = File.ReadAllBytes(filePath);

                    // ── 2. Verify checksum ─────────────────────────────────────
                    string storedChecksum = _slotInfos[slotIndex]?.checksum;
                    if (!string.IsNullOrEmpty(storedChecksum))
                    {
                        string actual = SaveIntegrityChecker.ComputeChecksum(data);
                        if (!string.Equals(actual, storedChecksum, StringComparison.OrdinalIgnoreCase))
                        {
                            errMsg = "checksum mismatch — save may be corrupted";
                            Debug.LogError($"[SWEF] SaveManager: slot {slotIndex} checksum mismatch.");
                        }
                    }

                    if (errMsg == null)
                    {
                        // ── 3. Decrypt ─────────────────────────────────────────
                        if (enableEncryption)
                            data = Decrypt(data);

                        // ── 4. Decompress ──────────────────────────────────────
                        if (enableCompression)
                            data = Decompress(data);

                        // ── 5. Deserialize ─────────────────────────────────────
                        string   json     = Encoding.UTF8.GetString(data);
                        SaveFile saveFile = JsonUtility.FromJson<SaveFile>(json);
                        if (saveFile == null)
                            throw new Exception("Deserialization returned null.");

                        // ── 6. Migrate if schema version is outdated ───────────
                        if (saveFile.header.formatVersion < SaveSystemConstants.CurrentSaveVersion)
                        {
                            var migrator = FindFirstObjectByType<SaveMigrationSystem>();
                            migrator?.Migrate(saveFile,
                                saveFile.header.formatVersion,
                                SaveSystemConstants.CurrentSaveVersion);
                        }

                        // ── 7. Distribute to ISaveable components ──────────────
                        foreach (var saveable in _saveables)
                        {
                            string stored = saveFile.payload.Get(saveable.SaveKey);
                            if (stored != null)
                            {
                                try   { saveable.RestoreState(stored); }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"[SWEF] SaveManager: RestoreState failed for '{saveable.SaveKey}' — {ex.Message}");
                                }
                            }
                        }

                        // ── 8. Distribute to known subsystems ──────────────────
                        DistributeSubsystemData(saveFile.payload);

                        // ── 9. Restore progress snapshot ───────────────────────
                        RestoreProgress(saveFile.progress);

                        success = true;
                        Debug.Log($"[SWEF] SaveManager: slot {slotIndex} loaded.");
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                Debug.LogError($"[SWEF] SaveManager: load from slot {slotIndex} failed — {ex.Message}");
            }

            IsBusy = false;
            OnLoadCompleted?.Invoke(slotIndex, success);
            if (!success)
                Debug.LogWarning($"[SWEF] SaveManager: load slot {slotIndex} error — {errMsg}");
        }

        private void DeleteSlot(int slotIndex)
        {
            try
            {
                string savePath = GetSavePath(slotIndex);
                string metaPath = GetMetaPath(slotIndex);

                if (File.Exists(savePath)) File.Delete(savePath);
                if (File.Exists(metaPath)) File.Delete(metaPath);

                _slotInfos[slotIndex] = CreateEmptySlotInfo(slotIndex);
                Debug.Log($"[SWEF] SaveManager: slot {slotIndex} deleted.");
                OnSlotDeleted?.Invoke(slotIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveManager: delete slot {slotIndex} failed — {ex.Message}");
            }
        }

        // ── Subsystem integration ─────────────────────────────────────────────

        // Serialisation helper for achievement state lists
        [Serializable]
        private class AchievementStateList
        {
            public List<Achievement.AchievementState> states = new List<Achievement.AchievementState>();
        }

        private void GatherSubsystemData(SavePayload payload)
        {
            // Achievement system
            var achievementMgr = FindFirstObjectByType<Achievement.AchievementManager>();
            if (achievementMgr != null)
            {
                var wrapper = new AchievementStateList { states = achievementMgr.GetAllStates() };
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "achievements",
                    JsonUtility.ToJson(wrapper));
            }

            // Settings
            var settingsMgr = FindFirstObjectByType<Settings.SettingsManager>();
            if (settingsMgr != null)
            {
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "settings.masterVolume",
                    settingsMgr.MasterVolume.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "settings.sfxVolume",
                    settingsMgr.SfxVolume.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }

            // Accessibility
            var accessibilityMgr = FindFirstObjectByType<Accessibility.AccessibilityManager>();
            if (accessibilityMgr != null)
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "accessibility",
                    JsonUtility.ToJson(accessibilityMgr.Profile));

            // Localization
            var locMgr = FindFirstObjectByType<Localization.LocalizationManager>();
            if (locMgr != null)
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "localization.language",
                    locMgr.CurrentLanguage.ToString());

            // IAP / premium
            var iapMgr = FindFirstObjectByType<IAP.IAPManager>();
            if (iapMgr != null)
                payload.Set(SaveSystemConstants.SubsystemKeyPrefix + "iap.premium",
                    iapMgr.IsPremium.ToString());
        }

        private void DistributeSubsystemData(SavePayload payload)
        {
            // Accessibility
            var accessibilityMgr = FindFirstObjectByType<Accessibility.AccessibilityManager>();
            if (accessibilityMgr != null)
            {
                string json = payload.Get(SaveSystemConstants.SubsystemKeyPrefix + "accessibility");
                if (!string.IsNullOrEmpty(json))
                {
                    var profile = JsonUtility.FromJson<Accessibility.AccessibilityProfile>(json);
                    if (profile != null)
                    {
                        accessibilityMgr.SetProfileValue(p =>
                        {
                            p.activePreset            = profile.activePreset;
                            p.screenReaderEnabled     = profile.screenReaderEnabled;
                            p.colorblindFilterEnabled = profile.colorblindFilterEnabled;
                            p.subtitlesEnabled        = profile.subtitlesEnabled;
                            p.uiScale                 = profile.uiScale;
                            p.gameSpeed               = profile.gameSpeed;
                        });
                    }
                }
            }

            // Localization
            var locMgr = FindFirstObjectByType<Localization.LocalizationManager>();
            if (locMgr != null)
            {
                string langStr = payload.Get(SaveSystemConstants.SubsystemKeyPrefix + "localization.language");
                if (!string.IsNullOrEmpty(langStr) &&
                    Enum.TryParse(langStr, out SystemLanguage lang))
                {
                    locMgr.CurrentLanguage = lang;
                }
            }
        }

        // ── Progress capture / restore ────────────────────────────────────────
        private static PlayerProgressData CaptureProgress()
        {
            var progress = new PlayerProgressData
            {
                lastLatitude  = Core.SWEFSession.Lat,
                lastLongitude = Core.SWEFSession.Lon,
                lastAltitude  = Core.SWEFSession.Alt,
                lastPlayDate  = DateTime.UtcNow.ToString("o")
            };

            var coreSave = FindFirstObjectByType<Core.SaveManager>();
            if (coreSave != null)
            {
                progress.flightsCompleted   = coreSave.Data.totalFlights;
                progress.totalFlightTimeSec = coreSave.Data.totalFlightTimeSec;
                progress.totalDistanceKm    = coreSave.Data.totalDistanceKm;
                progress.furthestAltitudeKm = coreSave.Data.allTimeMaxAltitudeKm;
            }

            return progress;
        }

        private static void RestoreProgress(PlayerProgressData progress)
        {
            if (progress == null) return;
            var coreSave = FindFirstObjectByType<Core.SaveManager>();
            if (coreSave == null) return;

            coreSave.Data.totalFlights         = progress.flightsCompleted;
            coreSave.Data.totalFlightTimeSec   = progress.totalFlightTimeSec;
            coreSave.Data.totalDistanceKm      = progress.totalDistanceKm;
            coreSave.Data.allTimeMaxAltitudeKm = progress.furthestAltitudeKm;
        }

        // ── Slot metadata helpers ─────────────────────────────────────────────
        private void LoadAllSlotInfos()
        {
            _slotInfos = new SaveSlotInfo[SaveSystemConstants.TotalSlots];
            for (int i = 0; i < SaveSystemConstants.TotalSlots; i++)
            {
                string path = GetMetaPath(i);
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        _slotInfos[i] = JsonUtility.FromJson<SaveSlotInfo>(json)
                                        ?? CreateEmptySlotInfo(i);
                    }
                    catch
                    {
                        _slotInfos[i] = CreateEmptySlotInfo(i);
                    }
                }
                else
                {
                    _slotInfos[i] = CreateEmptySlotInfo(i);
                }
            }
        }

        private void PersistSlotMeta(int slotIndex, SaveSlotInfo info)
        {
            try
            {
                File.WriteAllText(GetMetaPath(slotIndex),
                    JsonUtility.ToJson(info, prettyPrint: true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] SaveManager: failed to persist slot meta {slotIndex} — {ex.Message}");
            }
        }

        private static SaveSlotInfo CreateEmptySlotInfo(int slotIndex) =>
            new SaveSlotInfo { slotIndex = slotIndex, isEmpty = true };

        // ── Compression ───────────────────────────────────────────────────────
        /// <summary>GZip-compresses <paramref name="data"/>.</summary>
        public static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                    gzip.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        /// <summary>GZip-decompresses <paramref name="data"/>.</summary>
        public static byte[] Decompress(byte[] data)
        {
            using (var input  = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                    gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        // ── Encryption (AES-256, IV prepended) ───────────────────────────────
        private byte[] DeriveKey()
        {
            // NOTE: The key is derived from a constant seed combined with a device
            // identifier, making saves device-specific.  Saves cannot be decrypted
            // on another device.  For cross-device portability use SaveExportImport
            // (which stores an unencrypted envelope) or replace this derivation with
            // a user-account-bound key from a server.
            string seed = EncryptionSeed + SystemInfo.deviceUniqueIdentifier;
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        }

        private byte[] Encrypt(byte[] data)
        {
            byte[] key = DeriveKey();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;
                using (var enc = aes.CreateEncryptor())
                using (var ms  = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, enc, CryptoStreamMode.Write))
                        cs.Write(data, 0, data.Length);
                    return ms.ToArray();
                }
            }
        }

        private byte[] Decrypt(byte[] data)
        {
            byte[] key = DeriveKey();
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                int    ivLen = aes.BlockSize / 8;
                byte[] iv   = new byte[ivLen];
                Array.Copy(data, 0, iv, 0, ivLen);
                aes.IV = iv;

                using (var dec  = aes.CreateDecryptor())
                using (var ms   = new MemoryStream(data, ivLen, data.Length - ivLen))
                using (var cs   = new CryptoStream(ms, dec, CryptoStreamMode.Read))
                using (var outp = new MemoryStream())
                {
                    cs.CopyTo(outp);
                    return outp.ToArray();
                }
            }
        }
    }
}
