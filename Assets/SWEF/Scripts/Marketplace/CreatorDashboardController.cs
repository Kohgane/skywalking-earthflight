// CreatorDashboardController.cs — SWEF Community Content Marketplace (Phase 94)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.Marketplace
{
    /// <summary>
    /// MonoBehaviour that manages the creator-facing dashboard:
    /// profile data, earnings tracking, per-listing analytics, and the follower system.
    ///
    /// <para>Persistence files (in <c>Application.persistentDataPath</c>):</para>
    /// <list type="bullet">
    ///   <item><c>creator_profile.json</c></item>
    ///   <item><c>creator_earnings.json</c></item>
    /// </list>
    /// </summary>
    public class CreatorDashboardController : MonoBehaviour
    {
        #region Inspector

        [Header("Creator Settings")]
        [Tooltip("Maximum biography length in characters.")]
        [SerializeField] private int _maxBioLength = 500;

        #endregion

        #region Private State

        private CreatorProfileData _profile;
        private readonly List<MarketplaceTransactionData> _earnings = new List<MarketplaceTransactionData>();
        private readonly HashSet<string> _following = new HashSet<string>();

        private string _profilePath;
        private string _earningsPath;

        private string LocalPlayerId => "local_player";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _profilePath  = Path.Combine(Application.persistentDataPath, "creator_profile.json");
            _earningsPath = Path.Combine(Application.persistentDataPath, "creator_earnings.json");
            LoadProfile();
            LoadEarnings();
        }

        #endregion

        #region Public API — Profile

        /// <summary>Returns the local creator's profile, initialising it if needed.</summary>
        public CreatorProfileData GetOrCreateProfile()
        {
            if (_profile == null)
            {
                _profile = new CreatorProfileData
                {
                    creatorId   = LocalPlayerId,
                    displayName = GetLocalDisplayName(),
                };
                SaveProfile();
            }
            return _profile;
        }

        /// <summary>Updates the creator's display name and bio.</summary>
        /// <param name="displayName">New display name.</param>
        /// <param name="bio">New biography text.</param>
        public void UpdateProfile(string displayName, string bio)
        {
            var profile = GetOrCreateProfile();

            if (!string.IsNullOrWhiteSpace(displayName))
            {
#if SWEF_SECURITY_AVAILABLE
                displayName = SWEF.Security.InputSanitizer.SanitizeDisplayName(displayName);
#endif
                profile.displayName = displayName;
            }

            if (bio != null)
            {
                if (bio.Length > _maxBioLength)
                    bio = bio.Substring(0, _maxBioLength);
                profile.bio = bio;
            }

            SaveProfile();
        }

        /// <summary>Returns aggregated statistics for the creator's dashboard.</summary>
        public CreatorProfileData GetCreatorStats()
        {
            var profile  = GetOrCreateProfile();
            var listings = MarketplaceManager.Instance?.GetMyListings() ?? new List<MarketplaceListingData>();

            profile.totalListings   = listings.Count;
            profile.totalDownloads  = listings.Sum(l => l.downloadCount);
            profile.averageRating   = listings.Count > 0
                ? listings.Average(l => l.ratingAverage)
                : 0f;

            return profile;
        }

        #endregion

        #region Public API — Earnings

        /// <summary>
        /// Records an incoming sale transaction to the earnings ledger.
        /// Called by <see cref="MarketplaceBridge"/> on purchase.
        /// </summary>
        /// <param name="transaction">Completed purchase transaction.</param>
        public void RecordEarning(MarketplaceTransactionData transaction)
        {
            if (transaction == null || transaction.sellerId != LocalPlayerId) return;
            _earnings.Add(transaction);
            SaveEarnings();
        }

        /// <summary>Returns the full earnings history for the local creator.</summary>
        public List<MarketplaceTransactionData> GetEarningsHistory() =>
            new List<MarketplaceTransactionData>(_earnings);

        /// <summary>
        /// Returns the total pending (un-withdrawn) balance for the creator.
        /// </summary>
        public int GetPendingBalance()
        {
            return _earnings
                .Where(t => t.status == TransactionStatus.Completed)
                .Sum(t => t.price);
        }

        /// <summary>
        /// Simulates withdrawing the creator's pending earnings balance.
        /// In a live game this would call a backend endpoint.
        /// </summary>
        /// <returns>The amount withdrawn, or 0 if balance was empty.</returns>
        public int WithdrawEarnings()
        {
            int balance = GetPendingBalance();
            if (balance <= 0)
            {
                Debug.LogWarning("[SWEF] Marketplace: WithdrawEarnings — no pending balance.");
                return 0;
            }

            // Mark all completed transactions as refunded (= settled) to zero the balance
            foreach (var t in _earnings.Where(t => t.status == TransactionStatus.Completed))
                t.status = TransactionStatus.Refunded; // repurposed as "settled"

            SaveEarnings();
            MarketplaceAnalytics.RecordEarningsWithdrawn(LocalPlayerId, balance);

#if SWEF_PROGRESSION_AVAILABLE
            SWEF.Progression.ProgressionManager.Instance?.AddCurrency(balance, "marketplace_earnings");
#endif

            return balance;
        }

        #endregion

        #region Public API — Followers

        /// <summary>Follows a creator (adds to local following set).</summary>
        /// <param name="creatorId">Creator to follow.</param>
        public void FollowCreator(string creatorId)
        {
            if (string.IsNullOrEmpty(creatorId) || creatorId == LocalPlayerId) return;

            if (_following.Add(creatorId))
            {
                MarketplaceAnalytics.RecordCreatorFollowed(creatorId);
                MarketplaceBridge.OnCreatorFollowed(creatorId);
            }
        }

        /// <summary>Unfollows a creator.</summary>
        /// <param name="creatorId">Creator to unfollow.</param>
        public void UnfollowCreator(string creatorId)
        {
            _following.Remove(creatorId);
        }

        /// <summary>Returns the list of creator IDs the local player follows.</summary>
        public IReadOnlyCollection<string> GetFollowing() => _following;

        /// <summary>
        /// Returns a stub follower list for the local creator profile.
        /// (In a live game this would be fetched from a backend.)
        /// </summary>
        public int GetFollowerCount() => _profile?.followerCount ?? 0;

        #endregion

        #region Persistence

        private void SaveProfile()
        {
            try
            {
                if (_profile == null) return;
                File.WriteAllText(_profilePath, JsonUtility.ToJson(_profile, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save creator profile — {ex.Message}");
            }
        }

        private void LoadProfile()
        {
            if (!File.Exists(_profilePath)) return;
            try
            {
                _profile = JsonUtility.FromJson<CreatorProfileData>(File.ReadAllText(_profilePath));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load creator profile — {ex.Message}");
            }
        }

        private void SaveEarnings()
        {
            try
            {
                string json = JsonUtility.ToJson(new EarningsWrapper { transactions = _earnings }, true);
                File.WriteAllText(_earningsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to save earnings — {ex.Message}");
            }
        }

        private void LoadEarnings()
        {
            _earnings.Clear();
            if (!File.Exists(_earningsPath)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<EarningsWrapper>(File.ReadAllText(_earningsPath));
                if (wrapper?.transactions != null)
                    _earnings.AddRange(wrapper.transactions);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] Marketplace: Failed to load earnings — {ex.Message}");
            }
        }

        [Serializable] private class EarningsWrapper { public List<MarketplaceTransactionData> transactions; }

        #endregion

        #region Private Helpers

        private static string GetLocalDisplayName()
        {
#if SWEF_MULTIPLAYER_AVAILABLE
            return SWEF.Multiplayer.PlayerProfileManager.Instance?.LocalProfile?.displayName ?? "Creator";
#else
            return "Creator";
#endif
        }

        #endregion
    }
}
