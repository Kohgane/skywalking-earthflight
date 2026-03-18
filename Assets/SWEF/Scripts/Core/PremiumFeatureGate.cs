using System;
using UnityEngine;
using SWEF.IAP;

namespace SWEF.Core
{
    #region PremiumFeature Enum

    /// <summary>Enumeration of all premium-gated features in SWEF.</summary>
    public enum PremiumFeature
    {
        /// <summary>Store more than 10 favorite locations.</summary>
        UnlimitedFavorites,
        /// <summary>Sync save data to the cloud.</summary>
        CloudSave,
        /// <summary>Access to advanced / real-time weather simulation.</summary>
        AdvancedWeather,
        /// <summary>Unlock custom player skins and liveries.</summary>
        CustomSkins,
        /// <summary>Persistent removal of all advertisements.</summary>
        AdFree,
        /// <summary>Export flight journal entries to CSV / PDF.</summary>
        FlightJournalExport,
        /// <summary>Capture screenshots at 2× native resolution.</summary>
        HighResScreenshot
    }

    #endregion

    /// <summary>
    /// Static utility that controls access to premium-only features.
    /// All methods are safe to call before <see cref="IAPManager"/> initializes
    /// (they fall back to the free-tier behaviour).
    /// </summary>
    public static class PremiumFeatureGate
    {
        #region Free-Tier Limits

        /// <summary>Maximum number of favorites allowed on the free tier.</summary>
        public const int FreeTierMaxFavorites = 10;

        #endregion

        #region IsUnlocked

        /// <summary>
        /// Returns <c>true</c> when <paramref name="feature"/> is available to the
        /// current user (either because they purchased the required product or the
        /// feature is free).
        /// </summary>
        public static bool IsUnlocked(PremiumFeature feature)
        {
            // All features require either swef_premium or their own specific product.
            switch (feature)
            {
                case PremiumFeature.AdFree:
                    return IAPManager.Instance != null && IAPManager.Instance.IsAdFree;

                case PremiumFeature.UnlimitedFavorites:
                case PremiumFeature.CloudSave:
                case PremiumFeature.AdvancedWeather:
                case PremiumFeature.CustomSkins:
                case PremiumFeature.FlightJournalExport:
                case PremiumFeature.HighResScreenshot:
                    return IAPManager.Instance != null && IAPManager.Instance.IsPremium;

                default:
                    return false;
            }
        }

        #endregion

        #region TryAccess

        /// <summary>
        /// Attempts to grant access to <paramref name="feature"/>.
        /// Invokes <paramref name="onGranted"/> when unlocked, otherwise invokes
        /// <paramref name="onDenied"/> (which can, for example, open the store UI).
        /// </summary>
        /// <param name="feature">The feature the caller wants to use.</param>
        /// <param name="onGranted">Called when access is allowed.</param>
        /// <param name="onDenied">Called when access is denied (user does not own required product).</param>
        public static void TryAccess(PremiumFeature feature, Action onGranted, Action onDenied = null)
        {
            if (IsUnlocked(feature))
                onGranted?.Invoke();
            else
                onDenied?.Invoke();
        }

        #endregion
    }
}
