using System;
using UnityEngine;
using SWEF.CoreServices;
using SWEF.IAP;

namespace SWEF.Core
{
    /// <summary>
    /// Singleton ad-integration stub for SWEF.
    /// Provides banner, interstitial, and rewarded-ad surfaces without
    /// requiring a live ad SDK in the project.  All public methods are
    /// no-ops when <see cref="IAPManager.IsAdFree"/> is <c>true</c>.
    /// Replace the stub bodies with real SDK calls once an ad provider is chosen.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Singleton instance; persists across scene loads.</summary>
        public static AdManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Inspector Fields

        [Header("Interstitial settings")]
        [Tooltip("Minimum seconds that must elapse between two interstitial ads.")]
        [SerializeField] private float interstitialCooldownSeconds = 180f;

        #endregion

        #region Events

        /// <summary>Raised after a rewarded ad completes. <c>true</c> = reward earned.</summary>
        public event Action<bool> OnRewardedComplete;

        /// <summary>Raised when an interstitial ad is dismissed.</summary>
        public event Action OnInterstitialClosed;

        #endregion

        #region Private State

        private float _lastInterstitialTime = float.MinValue;
        private bool  _bannerVisible;

        #endregion

        #region Public API — Banner

        /// <summary>Displays the banner ad.  No-op when the user is ad-free.</summary>
        public void ShowBanner()
        {
            if (IsAdFree()) return;

            _bannerVisible = true;
            // TODO: replace with real SDK call (e.g. Advertisement.Banner.Show())
            Debug.Log("[SWEF] AdManager: banner shown (stub).");
            AnalyticsLogger.LogEvent("ad_shown", "banner");
        }

        /// <summary>Hides the banner ad.</summary>
        public void HideBanner()
        {
            if (!_bannerVisible) return;
            _bannerVisible = false;
            // TODO: replace with real SDK call
            Debug.Log("[SWEF] AdManager: banner hidden (stub).");
        }

        #endregion

        #region Public API — Interstitial

        /// <summary>
        /// Shows a full-screen interstitial ad if the cooldown has expired and the
        /// user is not ad-free.  Fires <see cref="OnInterstitialClosed"/> when done.
        /// </summary>
        /// <param name="onComplete">Optional callback invoked after the ad closes.</param>
        public void ShowInterstitial(Action onComplete = null)
        {
            if (IsAdFree())
            {
                onComplete?.Invoke();
                return;
            }

            float elapsed = Time.realtimeSinceStartup - _lastInterstitialTime;
            if (elapsed < interstitialCooldownSeconds)
            {
                Debug.Log($"[SWEF] AdManager: interstitial on cooldown ({interstitialCooldownSeconds - elapsed:F0}s left).");
                onComplete?.Invoke();
                return;
            }

            _lastInterstitialTime = Time.realtimeSinceStartup;

            // TODO: replace with real SDK call
            Debug.Log("[SWEF] AdManager: interstitial shown (stub).");
            AnalyticsLogger.LogEvent("ad_shown", "interstitial");

            // Stub: fire closed event on the next frame
            StartCoroutine(FireInterstitialClosed(onComplete));
        }

        #endregion

        #region Public API — Rewarded

        /// <summary>
        /// Shows a rewarded video ad.  No-op (calls <paramref name="onResult"/> with
        /// <c>false</c>) when the user is ad-free.
        /// </summary>
        /// <param name="onResult">
        /// Callback receiving <c>true</c> if the reward was earned, <c>false</c> otherwise.
        /// </param>
        public void ShowRewarded(Action<bool> onResult)
        {
            if (IsAdFree())
            {
                onResult?.Invoke(false);
                return;
            }

            // TODO: replace with real SDK call
            Debug.Log("[SWEF] AdManager: rewarded ad shown (stub).");
            AnalyticsLogger.LogEvent("ad_shown", "rewarded");

            // Stub: simulate a successful reward
            StartCoroutine(FireRewardedComplete(onResult, success: true));
        }

        #endregion

        #region Helpers

        private bool IsAdFree()
        {
            return IAPManager.Instance != null && IAPManager.Instance.IsAdFree;
        }

        private System.Collections.IEnumerator FireInterstitialClosed(Action onComplete)
        {
            yield return null;
            OnInterstitialClosed?.Invoke();
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator FireRewardedComplete(Action<bool> onResult, bool success)
        {
            yield return null;
            OnRewardedComplete?.Invoke(success);
            onResult?.Invoke(success);
        }

        #endregion
    }
}
