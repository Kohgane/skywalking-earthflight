using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Core;
using SWEF.IAP;

namespace SWEF.UI
{
    /// <summary>
    /// Modal dialog that prompts the user to upgrade to premium when they try to
    /// access a gated feature.  Includes an optional "Watch Ad to Unlock
    /// Temporarily" path that grants 30 minutes of access via a rewarded ad.
    /// </summary>
    public class PremiumPromptUI : MonoBehaviour
    {
        #region Singleton

        /// <summary>Singleton instance for easy access from game code.</summary>
        public static PremiumPromptUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (promptPanel != null)
                promptPanel.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        #endregion

        #region Inspector Fields

        [Header("Panel")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Labels")]
        [SerializeField] private Text featureNameLabel;
        [SerializeField] private Text featureDescriptionLabel;

        [Header("Buttons")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private Button watchAdButton;

        [Header("Store UI")]
        [Tooltip("Reference to the StoreUI panel to open when 'Upgrade' is tapped.")]
        [SerializeField] private StoreUI storeUI;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;

        [Header("Temporary Access")]
        [Tooltip("How long (seconds) a rewarded-ad temp unlock lasts.")]
        [SerializeField] private float tempUnlockDurationSec = 1800f; // 30 minutes

        #endregion

        #region Private State

        private PremiumFeature _pendingFeature;
        private Action _pendingCallback;
        private Coroutine _fadeRoutine;

        // Temp unlock expiry timestamps keyed by feature
        private readonly System.Collections.Generic.Dictionary<PremiumFeature, float>
            _tempUnlocks = new System.Collections.Generic.Dictionary<PremiumFeature, float>();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
            if (laterButton   != null) laterButton.onClick.AddListener(OnLaterClicked);
            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAdClicked);
        }

        private void OnDisable()
        {
            if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            if (laterButton   != null) laterButton.onClick.RemoveListener(OnLaterClicked);
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAdClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Returns <c>true</c> if the given <paramref name="feature"/> has an active
        /// temporary unlock granted by a rewarded ad.
        /// </summary>
        public bool HasTempAccess(PremiumFeature feature)
        {
            if (_tempUnlocks.TryGetValue(feature, out float expiry))
                return Time.realtimeSinceStartup < expiry;
            return false;
        }

        /// <summary>
        /// Shows the premium prompt for the given <paramref name="feature"/>.
        /// <paramref name="onUnlocked"/> is called if the user watches an ad or
        /// completes an upgrade in this session.
        /// </summary>
        public void Show(PremiumFeature feature, Action onUnlocked = null)
        {
            _pendingFeature  = feature;
            _pendingCallback = onUnlocked;

            UpdateLabels(feature);

            if (promptPanel != null)
                promptPanel.SetActive(true);

            StartFade(1f);
        }

        /// <summary>Hides the prompt panel.</summary>
        public void Hide()
        {
            StartFade(0f, () =>
            {
                if (promptPanel != null)
                    promptPanel.SetActive(false);
            });
        }

        #endregion

        #region Button Callbacks

        private void OnUpgradeClicked()
        {
            Hide();

            if (storeUI != null)
                storeUI.Open();
            else
                Debug.LogWarning("[SWEF] PremiumPromptUI: StoreUI reference not set.");
        }

        private void OnLaterClicked()
        {
            Hide();
        }

        private void OnWatchAdClicked()
        {
            if (AdManager.Instance == null)
            {
                Debug.LogWarning("[SWEF] PremiumPromptUI: AdManager.Instance is null.");
                return;
            }

            if (watchAdButton != null)
                watchAdButton.interactable = false;

            AdManager.Instance.ShowRewarded(success =>
            {
                if (watchAdButton != null)
                    watchAdButton.interactable = true;

                if (success)
                {
                    GrantTempAccess(_pendingFeature);
                    Hide();
                    _pendingCallback?.Invoke();
                }
                else
                {
                    Debug.Log("[SWEF] PremiumPromptUI: rewarded ad did not complete.");
                }
            });
        }

        #endregion

        #region Helpers

        private void UpdateLabels(PremiumFeature feature)
        {
            if (featureNameLabel != null)
                featureNameLabel.text = FormatFeatureName(feature);

            if (featureDescriptionLabel != null)
                featureDescriptionLabel.text = GetFeatureDescription(feature);

            // Show the Watch Ad button only when AdManager is available and user is not ad-free
            bool showAdButton = AdManager.Instance != null
                                && (IAPManager.Instance == null || !IAPManager.Instance.IsAdFree);
            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(showAdButton);
        }

        private void GrantTempAccess(PremiumFeature feature)
        {
            float expiry = Time.realtimeSinceStartup + tempUnlockDurationSec;
            _tempUnlocks[feature] = expiry;
            Debug.Log($"[SWEF] PremiumPromptUI: granted 30-min temp access to {feature}.");
        }

        private static string FormatFeatureName(PremiumFeature feature)
        {
            switch (feature)
            {
                case PremiumFeature.UnlimitedFavorites:   return "Unlimited Favorites";
                case PremiumFeature.CloudSave:             return "Cloud Save";
                case PremiumFeature.AdvancedWeather:       return "Advanced Weather";
                case PremiumFeature.CustomSkins:           return "Custom Skins";
                case PremiumFeature.AdFree:                return "Remove Ads";
                case PremiumFeature.FlightJournalExport:   return "Flight Journal Export";
                case PremiumFeature.HighResScreenshot:     return "High-Res Screenshot";
                default:                                   return feature.ToString();
            }
        }

        private static string GetFeatureDescription(PremiumFeature feature)
        {
            switch (feature)
            {
                case PremiumFeature.UnlimitedFavorites:
                    return "Free accounts are limited to 10 favorites. Upgrade to save unlimited locations.";
                case PremiumFeature.CloudSave:
                    return "Back up your flight data to the cloud so it's safe on every device.";
                case PremiumFeature.AdvancedWeather:
                    return "Experience real-time weather systems with volumetric clouds and dynamic wind.";
                case PremiumFeature.CustomSkins:
                    return "Personalize your craft with exclusive premium liveries and skins.";
                case PremiumFeature.AdFree:
                    return "Remove all banner and interstitial ads for a distraction-free flight.";
                case PremiumFeature.FlightJournalExport:
                    return "Export your complete flight history to CSV or PDF for sharing and analysis.";
                case PremiumFeature.HighResScreenshot:
                    return "Capture breathtaking 2× resolution screenshots perfect for wallpapers.";
                default:
                    return "Upgrade to Premium to unlock this feature.";
            }
        }

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

            float start   = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        #endregion
    }
}
