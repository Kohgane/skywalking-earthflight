using System;

namespace SWEF.IAP
{
    #region Product Type Enum

    /// <summary>Whether a product is consumed on purchase or persists indefinitely.</summary>
    public enum IAPProductType
    {
        /// <summary>Purchased once; unlock persists forever.</summary>
        NonConsumable,
        /// <summary>Can be purchased repeatedly; consumed on delivery.</summary>
        Consumable
    }

    #endregion

    #region ProductInfo Struct

    /// <summary>Metadata describing a single in-app product.</summary>
    [Serializable]
    public struct ProductInfo
    {
        /// <summary>Store product identifier (e.g. "swef_premium").</summary>
        public string Id;

        /// <summary>Whether the product is consumable or non-consumable.</summary>
        public IAPProductType Type;

        /// <summary>Suggested default price in USD (informational only).</summary>
        public float DefaultPrice;

        /// <summary>Short display name shown in the store UI.</summary>
        public string DisplayName;

        /// <summary>Longer description shown in the store UI.</summary>
        public string Description;
    }

    #endregion

    /// <summary>
    /// Static catalog of all SWEF in-app products.
    /// Provides product ID constants and rich metadata for use by
    /// <see cref="IAPManager"/> and the store UI.
    /// </summary>
    public static class IAPProductCatalog
    {
        #region Product ID Constants

        /// <summary>Well-known product identifiers used throughout the IAP system.</summary>
        public static class Products
        {
            /// <summary>Non-consumable — unlocks all premium features.</summary>
            public const string Premium = "swef_premium";

            /// <summary>Non-consumable — removes all banner and interstitial ads.</summary>
            public const string RemoveAds = "swef_remove_ads";

            /// <summary>Consumable tip — $0.99.</summary>
            public const string DonationSmall = "swef_donation_small";

            /// <summary>Consumable tip — $2.99.</summary>
            public const string DonationMedium = "swef_donation_medium";

            /// <summary>Consumable tip — $9.99.</summary>
            public const string DonationLarge = "swef_donation_large";
        }

        #endregion

        #region Product Metadata

        private static readonly ProductInfo[] _all = new ProductInfo[]
        {
            new ProductInfo
            {
                Id           = Products.Premium,
                Type         = IAPProductType.NonConsumable,
                DefaultPrice = 4.99f,
                DisplayName  = "SWEF Premium",
                Description  = "Unlock unlimited favorites, cloud save, advanced weather, custom skins, high-res screenshots, and flight journal export."
            },
            new ProductInfo
            {
                Id           = Products.RemoveAds,
                Type         = IAPProductType.NonConsumable,
                DefaultPrice = 1.99f,
                DisplayName  = "Remove Ads",
                Description  = "Remove all banner and interstitial advertisements permanently."
            },
            new ProductInfo
            {
                Id           = Products.DonationSmall,
                Type         = IAPProductType.Consumable,
                DefaultPrice = 0.99f,
                DisplayName  = "Small Tip ☕",
                Description  = "Buy the developer a coffee. Thank you for your support!"
            },
            new ProductInfo
            {
                Id           = Products.DonationMedium,
                Type         = IAPProductType.Consumable,
                DefaultPrice = 2.99f,
                DisplayName  = "Medium Tip 🍕",
                Description  = "Buy the developer lunch. Your generosity keeps SWEF flying!"
            },
            new ProductInfo
            {
                Id           = Products.DonationLarge,
                Type         = IAPProductType.Consumable,
                DefaultPrice = 9.99f,
                DisplayName  = "Large Tip 🚀",
                Description  = "Fuel the rocket! A big thank you from the whole team."
            }
        };

        /// <summary>Returns an array containing metadata for every product in the catalog.</summary>
        public static ProductInfo[] All => _all;

        /// <summary>
        /// Finds the <see cref="ProductInfo"/> entry whose <see cref="ProductInfo.Id"/>
        /// matches <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The product identifier to look up.</param>
        /// <returns>
        /// The matching <see cref="ProductInfo"/>, or a default-value struct if
        /// <paramref name="id"/> is not found.
        /// </returns>
        public static ProductInfo GetProduct(string id)
        {
            foreach (var p in _all)
            {
                if (p.Id == id)
                    return p;
            }
            return default;
        }

        #endregion
    }
}
