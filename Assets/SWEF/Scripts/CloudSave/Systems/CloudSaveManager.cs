// CloudSaveManager.cs — Phase 111: Cloud Save & Cross-Platform Sync
// Central singleton MonoBehaviour — provider selection, initialisation, and facade API.
// Namespace: SWEF.CloudSave

using System;
using UnityEngine;

namespace SWEF.CloudSave
{
    /// <summary>
    /// Phase 111 — Central singleton for the Cloud Save system.
    ///
    /// <para>Manages the active <see cref="ICloudSaveProvider"/> instance,
    /// exposes the top-level save/load API, and co-ordinates with
    /// <see cref="CloudSyncEngine"/>, <see cref="SaveDataMigrator"/>, and
    /// <see cref="ConflictResolver"/>.</para>
    ///
    /// <para>Attach to a persistent scene object.</para>
    /// </summary>
    public sealed class CloudSaveManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static CloudSaveManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Configuration")]
        [Tooltip("Cloud save configuration asset. If null a default LocalFile config is used.")]
        [SerializeField] private CloudSaveConfig _config;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when the active provider is successfully initialised.</summary>
        public event Action<CloudProviderType> OnProviderReady;

        /// <summary>Fired when the provider fails to initialise.</summary>
        public event Action<CloudProviderType, string> OnProviderError;

        /// <summary>Fired when the active provider type changes at runtime.</summary>
        public event Action<CloudProviderType> OnProviderChanged;

        // ── State ──────────────────────────────────────────────────────────────

        /// <summary>The currently active cloud save provider.</summary>
        public ICloudSaveProvider ActiveProvider { get; private set; }

        /// <summary>The loaded configuration asset.</summary>
        public CloudSaveConfig Config => _config;

        /// <summary><c>true</c> if a provider has been successfully initialised.</summary>
        public bool IsReady { get; private set; }

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

            if (_config == null)
                _config = ScriptableObject.CreateInstance<CloudSaveConfig>();
        }

        private void Start()
        {
            SelectProvider(_config.providerType);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ActiveProvider?.Shutdown();
                Instance = null;
            }
        }

        // ── Provider management ────────────────────────────────────────────────

        /// <summary>
        /// Switches to the specified provider type and re-initialises it.
        /// The provider type is also written back to the config so the
        /// <see cref="CloudSyncEngine"/> picks it up on next cycle.
        /// </summary>
        public void SelectProvider(CloudProviderType type)
        {
            ActiveProvider?.Shutdown();
            IsReady = false;

            _config.providerType = type;
            ActiveProvider       = CreateProvider(type);

            StartCoroutine(ActiveProvider.InitialiseAsync((success, error) =>
            {
                IsReady = success;
                if (success)
                {
                    Debug.Log($"[CloudSaveManager] Provider ready: {ActiveProvider.ProviderName}");
                    OnProviderReady?.Invoke(type);
                }
                else
                {
                    Debug.LogWarning(
                        $"[CloudSaveManager] Provider '{ActiveProvider.ProviderName}' failed: {error}. " +
                        "Falling back to LocalFile.");
                    OnProviderError?.Invoke(type, error);

                    if (type != CloudProviderType.LocalFile)
                        SelectProvider(CloudProviderType.LocalFile);
                }
            }));

            OnProviderChanged?.Invoke(type);
        }

        /// <summary>Returns a fresh snapshot of the provider status.</summary>
        public ProviderStatus GetProviderStatus() =>
            ActiveProvider?.GetStatus() ?? new ProviderStatus
            {
                ProviderType     = CloudProviderType.LocalFile,
                ConnectionStatus = ProviderConnectionStatus.Disconnected,
                SyncStatus       = SyncStatus.Unavailable
            };

        // ── Factory ───────────────────────────────────────────────────────────

        private ICloudSaveProvider CreateProvider(CloudProviderType type)
        {
            switch (type)
            {
                case CloudProviderType.UnityCloud:
                    return new UnityCloudSaveProvider();

                case CloudProviderType.Firebase:
                    return new FirebaseProvider();

                case CloudProviderType.CustomREST:
                    return new CustomRESTProvider(_config.restBaseUrl, _config.restApiKey);

                default:
                    return new LocalFileProvider();
            }
        }
    }
}
