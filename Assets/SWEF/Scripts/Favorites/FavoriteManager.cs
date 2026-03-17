using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Favorites
{
    [System.Serializable]
    public struct FavoriteLocation
    {
        public string name;
        public double lat;
        public double lon;
        public float altitude;
        public string createdAt;
    }

    [System.Serializable]
    internal class FavoriteListWrapper
    {
        public List<FavoriteLocation> items = new List<FavoriteLocation>();
    }

    /// <summary>
    /// MVP favorites system using PlayerPrefs.
    /// Stores a JSON array of FavoriteLocation. Max 50 favorites.
    /// </summary>
    public class FavoriteManager : MonoBehaviour
    {
        private const string PrefsKey = "SWEF_Favorites";
        private const int MaxFavorites = 50;

        public List<FavoriteLocation> Favorites { get; private set; } = new List<FavoriteLocation>();

        public event System.Action OnFavoritesChanged;

        private void Awake()
        {
            Load();
        }

        public bool Add(string name, double lat, double lon, float altitude)
        {
            if (Favorites.Count >= MaxFavorites)
            {
                Debug.LogWarning($"[SWEF] Max favorites ({MaxFavorites}) reached.");
                return false;
            }

            Favorites.Add(new FavoriteLocation
            {
                name = name,
                lat = lat,
                lon = lon,
                altitude = altitude,
                createdAt = System.DateTime.UtcNow.ToString("o")
            });

            Save();
            OnFavoritesChanged?.Invoke();
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= Favorites.Count) return false;
            Favorites.RemoveAt(index);
            Save();
            OnFavoritesChanged?.Invoke();
            return true;
        }

        public void ClearAll()
        {
            Favorites.Clear();
            Save();
            OnFavoritesChanged?.Invoke();
        }

        private void Save()
        {
            var wrapper = new FavoriteListWrapper { items = Favorites };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                Favorites = new List<FavoriteLocation>();
                return;
            }

            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                Favorites = new List<FavoriteLocation>();
                return;
            }

            try
            {
                var wrapper = JsonUtility.FromJson<FavoriteListWrapper>(json);
                Favorites = wrapper?.items ?? new List<FavoriteLocation>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SWEF] Failed to load favorites: {e.Message}");
                Favorites = new List<FavoriteLocation>();
            }
        }
    }
}