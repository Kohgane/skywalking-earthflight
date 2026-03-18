using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SWEF.Analytics
{
    /// <summary>
    /// Manages GDPR/CCPA consent and privacy controls for the SWEF analytics pipeline.
    /// </summary>
    public class PrivacyConsentManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static PrivacyConsentManager Instance { get; private set; }

        // ── Consent levels ────────────────────────────────────────────────────────
        /// <summary>Consent levels ordered from least to most permissive.</summary>
        public enum ConsentLevel
        {
            /// <summary>No analytics at all.</summary>
            None      = 0,
            /// <summary>Only strictly necessary error reporting.</summary>
            Essential = 1,
            /// <summary>Anonymous usage analytics.</summary>
            Analytics = 2,
            /// <summary>Full telemetry including behavioural data.</summary>
            Full      = 3,
        }

        // ── Inspector ────────────────────────────────────────────────────────────
        [SerializeField] private ConsentLevel defaultConsent = ConsentLevel.Essential;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Raised when the user's consent level changes.</summary>
        public event Action<ConsentLevel> OnConsentChanged;

        /// <summary>Raised after all local analytics data has been deleted.</summary>
        public event Action OnDataDeletionCompleted;

        // ── State ─────────────────────────────────────────────────────────────────
        private ConsentLevel _currentConsent;
        private string       _cachedUserId;

        private const string PrefsConsentKey = "SWEF_PrivacyConsent";
        private const string PrefsSaltKey    = "SWEF_UserIdSalt";
        private const string PrefsUserIdKey  = "SWEF_AnonymizedUserId";

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            int saved = PlayerPrefs.GetInt(PrefsConsentKey, -1);
            _currentConsent = saved < 0 ? defaultConsent : (ConsentLevel)saved;
            _cachedUserId   = BuildAnonymizedUserId();
        }

        private void Start()
        {
            // Push userId to dispatcher once both are initialised
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher != null) dispatcher.SetUserId(_cachedUserId);

            // Push userId to A/B test manager
            var abm = ABTestManager.Instance;
            if (abm != null) abm.SetUserId(_cachedUserId);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Returns the current consent level.</summary>
        public ConsentLevel GetCurrentConsent() => _currentConsent;

        /// <summary>Updates and persists the consent level.</summary>
        public void SetConsent(ConsentLevel level)
        {
            if (_currentConsent == level) return;
            _currentConsent = level;
            PlayerPrefs.SetInt(PrefsConsentKey, (int)level);
            PlayerPrefs.Save();

            // Push to dispatcher
            var dispatcher = TelemetryDispatcher.Instance;
            if (dispatcher != null)
                dispatcher.SetTelemetryEnabled(HasConsent(ConsentLevel.Analytics));

            OnConsentChanged?.Invoke(level);
        }

        /// <summary>Returns true if the current consent is at least <paramref name="required"/>.</summary>
        public bool HasConsent(ConsentLevel required) =>
            _currentConsent >= required;

        /// <summary>
        /// Deletes all local analytics data and resets the anonymized user ID.
        /// </summary>
        public void RequestDataDeletion()
        {
            // Clear telemetry queue
            TelemetryDispatcher.Instance?.ClearQueue();

            // Reset anonymized ID (new salt → new hash)
            PlayerPrefs.DeleteKey(PrefsSaltKey);
            PlayerPrefs.DeleteKey(PrefsUserIdKey);
            _cachedUserId = BuildAnonymizedUserId();
            TelemetryDispatcher.Instance?.SetUserId(_cachedUserId);
            ABTestManager.Instance?.SetUserId(_cachedUserId);

            // Remove local dashboard data
            AnalyticsDashboardData.Instance?.RefreshFromLocal();

            // Remove feature discovery
            PlayerPrefs.DeleteKey("SWEF_DiscoveredFeatures");

            PlayerPrefs.Save();
            OnDataDeletionCompleted?.Invoke();
            Debug.Log("[SWEF] PrivacyConsentManager: user data deleted.");
        }

        /// <summary>
        /// Exports all locally stored analytics data as a JSON string.
        /// </summary>
        public string ExportUserData()
        {
            var dash = AnalyticsDashboardData.Instance;
            var sb   = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"userId\":\"{_cachedUserId}\",");
            sb.Append($"\"consentLevel\":\"{_currentConsent}\",");
            sb.Append("\"stats\":{");

            if (dash != null)
            {
                foreach (var kvp in dash.GetFormattedStats())
                    sb.Append($"\"{kvp.Key}\":\"{kvp.Value}\",");
                if (sb[sb.Length - 1] == ',') sb.Remove(sb.Length - 1, 1);
            }

            sb.Append("}}");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the anonymized user ID — a SHA-256 hash of the raw device ID and a per-install salt.
        /// The raw device identifier is never stored or transmitted.
        /// </summary>
        public string GetAnonymizedUserId() => _cachedUserId;

        // ── Private ───────────────────────────────────────────────────────────────

        private string BuildAnonymizedUserId()
        {
            // Re-use cached value if already computed for this install
            string cached = PlayerPrefs.GetString(PrefsUserIdKey, "");
            if (!string.IsNullOrEmpty(cached)) return cached;

            // Generate a per-install salt
            string salt = PlayerPrefs.GetString(PrefsSaltKey, "");
            if (string.IsNullOrEmpty(salt))
            {
                salt = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PrefsSaltKey, salt);
            }

            string raw  = SystemInfo.deviceUniqueIdentifier + salt;
            string hash = ComputeSha256(raw);

            PlayerPrefs.SetString(PrefsUserIdKey, hash);
            PlayerPrefs.Save();
            return hash;
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            byte[] bytes  = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(64);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
