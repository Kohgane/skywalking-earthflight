using System;
using System.Collections.Generic;
using UnityEngine;
using SWEF.Core;

namespace SWEF.Favorites
{
    /// <summary>Individual saved favorite location.</summary>
    [System.Serializable]
    public struct FavoriteEntry
    {
        public string name;
        public double lat;
        public double lon;
        public float  altitude;
    }

    /// <summary>
    /// PlayerPrefs-based persistent storage for up to 50 favorite locations.
    /// Serializes the list as JSON via JsonUtility.
    /// </summary>
    public class FavoriteManager : MonoBehaviour
    {
        private const string PrefKey  = "SWEF_Favorites";
        private const int    MaxItems = 50;

        /// <summary>Free-tier maximum. Exceeding this requires <c>swef_premium</c>.</summary>
        private const int FreeTierMax = PremiumFeatureGate.FreeTierMaxFavorites;

        /// <summary>Current list of saved favorites (read-only access).</summary>
        public IReadOnlyList<FavoriteEntry> Favorites => _favorites;

        /// <summary>Raised whenever the favorites list changes.</summary>
        public event Action OnFavoritesChanged;

        private List<FavoriteEntry> _favorites = new List<FavoriteEntry>();

        private void Awake()
        {
            Load();
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Adds a favorite entry.
        /// Free-tier users are limited to <see cref="FreeTierMax"/> entries;
        /// exceeding that limit invokes the premium prompt instead of saving.
        /// Silently ignores if the list is at the hard cap.
        /// </summary>
        public void Add(string entryName, double lat, double lon, float altitude)
        {
            if (_favorites.Count >= MaxItems) return;

            // Premium gate: cap free-tier at FreeTierMax
            bool isAtFreeLimit = !PremiumFeatureGate.IsUnlocked(PremiumFeature.UnlimitedFavorites)
                                 && _favorites.Count >= FreeTierMax;
            if (isAtFreeLimit)
            {
                // Show the premium prompt if available; otherwise log a warning.
                if (SWEF.UI.PremiumPromptUI.Instance != null)
                    SWEF.UI.PremiumPromptUI.Instance.Show(PremiumFeature.UnlimitedFavorites);
                else
                    Debug.LogWarning("[SWEF] FavoriteManager: free-tier limit reached. Upgrade to Premium for unlimited favorites.");
                return;
            }

            _favorites.Add(new FavoriteEntry
            {
                name     = entryName,
                lat      = lat,
                lon      = lon,
                altitude = altitude,
            });

            Save();
            OnFavoritesChanged?.Invoke();
        }

        /// <summary>Removes the favorite at the given index. Out-of-range indices are ignored.</summary>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _favorites.Count) return;
            _favorites.RemoveAt(index);
            Save();
            OnFavoritesChanged?.Invoke();
        }

        // ── Persistence ──────────────────────────────────────────────────────────
        private void Load()
        {
            string json = PlayerPrefs.GetString(PrefKey, "");
            if (string.IsNullOrEmpty(json))
            {
                _favorites = new List<FavoriteEntry>();
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<FavoriteListWrapper>(json);
                _favorites = wrapper?.entries != null
                    ? new List<FavoriteEntry>(wrapper.entries)
                    : new List<FavoriteEntry>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SWEF] Failed to load favorites: {e.Message}");
                _favorites = new List<FavoriteEntry>();
            }
        }

        private void Save()
        {
            var wrapper = new FavoriteListWrapper { entries = _favorites.ToArray() };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PrefKey, json);
            PlayerPrefs.Save();
        }

        // ── JSON wrapper ─────────────────────────────────────────────────────────
        [System.Serializable]
        private class FavoriteListWrapper
        {
            public FavoriteEntry[] entries;
        }
    }
}
