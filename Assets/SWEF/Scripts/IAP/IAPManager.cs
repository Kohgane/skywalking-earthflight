using System;
using UnityEngine;
using SWEF.Core;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace SWEF.IAP
{
    /// <summary>
    /// Singleton MonoBehaviour that wraps Unity IAP.
    /// Handles product initialization, purchase flow, and purchase restoration.
    /// Non-consumable unlocks are persisted via PlayerPrefs keys of the form
    /// <c>swef_iap_{productId}</c>.
    /// </summary>
    /// <remarks>
    /// Actual Unity IAP calls are guarded with <c>#if UNITY_PURCHASING</c> so the
    /// script compiles and runs (in stub mode) even when the package is absent.
    /// </remarks>
#if UNITY_PURCHASING
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
#else
    public class IAPManager : MonoBehaviour
#endif
    {
        #region Singleton

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static IAPManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        #endregion

        #region Events

        /// <summary>Raised when a purchase completes successfully. Argument is the product ID.</summary>
        public event Action<string> OnPurchaseCompleted;

        /// <summary>Raised when a purchase fails. Arguments are product ID and failure reason.</summary>
        public event Action<string, string> OnPurchaseFailed;

        /// <summary>Raised when restore-purchases finishes (iOS). Argument is <c>true</c> on success.</summary>
        public event Action<bool> OnRestoreComplete;

        #endregion

        #region Public Properties

        /// <summary>Returns <c>true</c> when the <c>swef_premium</c> non-consumable has been purchased.</summary>
        public bool IsPremium => HasPurchased(IAPProductCatalog.Products.Premium);

        /// <summary>Returns <c>true</c> when the <c>swef_remove_ads</c> non-consumable has been purchased.</summary>
        public bool IsAdFree => HasPurchased(IAPProductCatalog.Products.RemoveAds) || IsPremium;

        #endregion

        #region Private Fields

#if UNITY_PURCHASING
        private IStoreController _storeController;
        private IExtensionProvider _extensions;
#endif

        private bool _isInitializing;

        #endregion

        #region Initialization

        /// <summary>
        /// Configures the Unity IAP product catalog and starts the store initialization.
        /// Called automatically from <see cref="Awake"/>.
        /// </summary>
        public void Initialize()
        {
            if (_isInitializing) return;
            _isInitializing = true;

#if UNITY_PURCHASING
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (var product in IAPProductCatalog.All)
            {
                var unityType = product.Type == IAPProductType.Consumable
                    ? ProductType.Consumable
                    : ProductType.NonConsumable;

                builder.AddProduct(product.Id, unityType);
            }

            UnityPurchasing.Initialize(this, builder);
            Debug.Log("[SWEF] IAPManager: initializing Unity IAP.");
#else
            Debug.Log("[SWEF] IAPManager: UNITY_PURCHASING not defined — running in stub mode.");
#endif
        }

        #endregion

        #region Purchase API

        /// <summary>
        /// Initiates the purchase flow for the given product ID.
        /// If Unity IAP is not available or not initialized this is a no-op (logged).
        /// </summary>
        /// <param name="productId">The store product identifier (see <see cref="IAPProductCatalog.Products"/>).</param>
        public void BuyProduct(string productId)
        {
#if UNITY_PURCHASING
            if (_storeController == null)
            {
                Debug.LogWarning("[SWEF] IAPManager: store not initialized yet.");
                return;
            }

            var product = _storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[SWEF] IAPManager: initiating purchase of '{productId}'.");
                FireIapEvent(SWEF.Analytics.AnalyticsEvents.IapInitiated, productId, 0f);
                _storeController.InitiatePurchase(product);
            }
            else
            {
                string reason = product == null ? "product not found" : "not available to purchase";
                Debug.LogWarning($"[SWEF] IAPManager: cannot purchase '{productId}' — {reason}.");
                OnPurchaseFailed?.Invoke(productId, reason);
            }
#else
            Debug.Log($"[SWEF] IAPManager (stub): BuyProduct called for '{productId}'.");
            FireIapEvent(SWEF.Analytics.AnalyticsEvents.IapInitiated, productId, 0f);
            // In stub mode simulate a successful consumable purchase so the UI can be tested.
            DeliverProduct(productId);
#endif
        }

        /// <summary>
        /// Restores previously purchased non-consumable products (required on iOS).
        /// No-op on Android. Fires <see cref="OnRestoreComplete"/> when finished.
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_PURCHASING && UNITY_IOS
            if (_extensions == null)
            {
                Debug.LogWarning("[SWEF] IAPManager: extensions not ready for restore.");
                OnRestoreComplete?.Invoke(false);
                return;
            }

            var apple = _extensions.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result =>
            {
                Debug.Log($"[SWEF] IAPManager: restore result = {result}.");
                OnRestoreComplete?.Invoke(result);
            });
#elif UNITY_PURCHASING
            Debug.Log("[SWEF] IAPManager: RestorePurchases is only needed on iOS.");
            OnRestoreComplete?.Invoke(true);
#else
            Debug.Log("[SWEF] IAPManager (stub): RestorePurchases called.");
            OnRestoreComplete?.Invoke(true);
#endif
        }

        #endregion

        #region Persistence Helpers

        /// <summary>
        /// Returns <c>true</c> if the given non-consumable product has been purchased
        /// and the unlock is stored in PlayerPrefs.
        /// </summary>
        /// <param name="productId">The product identifier to query.</param>
        public bool HasPurchased(string productId)
        {
            return PlayerPrefs.GetInt(PrefKey(productId), 0) == 1;
        }

        private static string PrefKey(string productId) => $"swef_iap_{productId}";

        private void PersistNonConsumable(string productId)
        {
            PlayerPrefs.SetInt(PrefKey(productId), 1);
            PlayerPrefs.Save();
        }

        #endregion

        #region Delivery

        private void DeliverProduct(string productId)
        {
            var info = IAPProductCatalog.GetProduct(productId);
            if (info.Type == IAPProductType.NonConsumable)
                PersistNonConsumable(productId);

            Debug.Log($"[SWEF] IAPManager: purchase delivered for '{productId}'.");
            AnalyticsLogger.LogEvent("iap_purchase", productId);
            FireIapEvent(SWEF.Analytics.AnalyticsEvents.IapCompleted, productId, 0f);
            OnPurchaseCompleted?.Invoke(productId);
        }

        #endregion

        #region Unity IAP Callbacks

#if UNITY_PURCHASING

        /// <inheritdoc/>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensions      = extensions;
            _isInitializing  = false;
            Debug.Log("[SWEF] IAPManager: Unity IAP initialized successfully.");
        }

        /// <inheritdoc/>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            _isInitializing = false;
            Debug.LogError($"[SWEF] IAPManager: initialization failed — {error}.");
        }

        /// <inheritdoc/>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _isInitializing = false;
            Debug.LogError($"[SWEF] IAPManager: initialization failed — {error}: {message}.");
        }

        /// <inheritdoc/>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string id = args.purchasedProduct.definition.id;
            DeliverProduct(id);
            return PurchaseProcessingResult.Complete;
        }

        /// <inheritdoc/>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string id     = product.definition.id;
            string reason = failureReason.ToString();
            Debug.LogWarning($"[SWEF] IAPManager: purchase of '{id}' failed — {reason}.");
            FireIapEvent(SWEF.Analytics.AnalyticsEvents.IapFailed, id, 0f, reason);
            OnPurchaseFailed?.Invoke(id, reason);
        }

        /// <inheritdoc/>
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            string id     = product.definition.id;
            string reason = failureDescription?.message ?? failureDescription?.reason.ToString() ?? "unknown";
            Debug.LogWarning($"[SWEF] IAPManager: purchase of '{id}' failed — {reason}.");
            FireIapEvent(SWEF.Analytics.AnalyticsEvents.IapFailed, id, 0f, reason);
            OnPurchaseFailed?.Invoke(id, reason);
        }

#endif

        #endregion

        #region Phase 21 — Telemetry helpers

        private static void FireIapEvent(string eventName, string productId, float price, string reason = null)
        {
            var dispatcher = SWEF.Analytics.TelemetryDispatcher.Instance;
            if (dispatcher == null) return;

            var builder = SWEF.Analytics.TelemetryEventBuilder.Create(eventName)
                .WithCategory("purchase")
                .WithProperty("productId", productId)
                .WithProperty("price",     price);
            if (reason != null) builder.WithProperty("reason", reason);

            dispatcher.EnqueueCriticalEvent(builder.Build());
        }

        #endregion
    }
}
