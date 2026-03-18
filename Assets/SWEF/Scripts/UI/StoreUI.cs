using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.IAP;
using SWEF.Core;

namespace SWEF.UI
{
    /// <summary>
    /// In-app store panel.  Populates a scrollable product list from
    /// <see cref="IAPProductCatalog.All"/>, shows prices and purchase status,
    /// and delegates purchase / restore actions to <see cref="IAPManager"/>.
    /// The panel opens and closes with a CanvasGroup alpha-fade animation.
    /// </summary>
    public class StoreUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Panel references")]
        [Tooltip("Root GameObject of the store panel.")]
        [SerializeField] private GameObject storePanel;

        [Tooltip("CanvasGroup used for the open/close fade animation.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Parent Transform that receives product item instances.")]
        [SerializeField] private Transform contentParent;

        [Tooltip("Prefab spawned for each product. Must contain ProductItemUI.")]
        [SerializeField] private ProductItemUI productItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button restoreButton;

        [Header("Animation")]
        [Tooltip("Duration of the open/close fade in seconds.")]
        [SerializeField] private float fadeDuration = 0.25f;

        #endregion

        #region Private Fields

        private bool _isOpen;
        private Coroutine _fadeRoutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (storePanel != null)
                storePanel.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (restoreButton != null)
                restoreButton.onClick.AddListener(OnRestoreClicked);

            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchaseCompleted += HandlePurchaseCompleted;
                IAPManager.Instance.OnPurchaseFailed    += HandlePurchaseFailed;
            }
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (restoreButton != null)
                restoreButton.onClick.RemoveListener(OnRestoreClicked);

            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnPurchaseCompleted -= HandlePurchaseCompleted;
                IAPManager.Instance.OnPurchaseFailed    -= HandlePurchaseFailed;
            }
        }

        #endregion

        #region Public API

        /// <summary>Opens the store panel with a fade-in animation and populates the product list.</summary>
        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            if (storePanel != null)
                storePanel.SetActive(true);

            PopulateProducts();
            StartFade(1f);
        }

        /// <summary>Closes the store panel with a fade-out animation.</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            StartFade(0f, () =>
            {
                if (storePanel != null)
                    storePanel.SetActive(false);
            });
        }

        #endregion

        #region Product List

        private void PopulateProducts()
        {
            if (contentParent == null || productItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            foreach (var product in IAPProductCatalog.All)
            {
                var item = Instantiate(productItemPrefab, contentParent);
                item.Setup(product, OnBuyClicked);
            }
        }

        private void OnBuyClicked(string productId)
        {
            if (IAPManager.Instance != null)
                IAPManager.Instance.BuyProduct(productId);
        }

        private void OnRestoreClicked()
        {
            if (IAPManager.Instance != null)
                IAPManager.Instance.RestorePurchases();
        }

        #endregion

        #region IAP Callbacks

        private void HandlePurchaseCompleted(string productId)
        {
            Debug.Log($"[SWEF] StoreUI: purchase completed for '{productId}' — refreshing list.");
            PopulateProducts();
        }

        private void HandlePurchaseFailed(string productId, string reason)
        {
            Debug.LogWarning($"[SWEF] StoreUI: purchase failed for '{productId}' — {reason}.");
        }

        #endregion

        #region Fade Animation

        private void StartFade(float targetAlpha, Action onComplete = null)
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed    = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        #endregion
    }

    // ── Nested helper — attach to the product item prefab ──────────────────────

    /// <summary>
    /// Represents a single row in the store product list.
    /// Configure via <see cref="Setup"/>.
    /// </summary>
    public class ProductItemUI : MonoBehaviour
    {
        #region Inspector Fields

        [SerializeField] private Text nameLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Text priceLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buyButtonLabel;
        [SerializeField] private GameObject purchasedBadge;

        #endregion

        #region Setup

        /// <summary>
        /// Populates the item UI for the given <paramref name="product"/>.
        /// </summary>
        /// <param name="product">Product metadata.</param>
        /// <param name="onBuy">Callback invoked when the Buy button is clicked.</param>
        public void Setup(ProductInfo product, Action<string> onBuy)
        {
            if (nameLabel != null)
                nameLabel.text = product.DisplayName;

            if (descriptionLabel != null)
                descriptionLabel.text = product.Description;

            if (priceLabel != null)
                priceLabel.text = $"${product.DefaultPrice:0.00}";

            bool purchased = product.Type == IAPProductType.NonConsumable
                             && IAPManager.Instance != null
                             && IAPManager.Instance.HasPurchased(product.Id);

            if (purchasedBadge != null)
                purchasedBadge.SetActive(purchased);

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(!purchased);
                buyButton.onClick.RemoveAllListeners();
                string id = product.Id;
                buyButton.onClick.AddListener(() => onBuy?.Invoke(id));
            }

            if (buyButtonLabel != null && !purchased)
                buyButtonLabel.text = product.Type == IAPProductType.Consumable
                    ? "Tip"
                    : "Buy";
        }

        #endregion
    }
}
