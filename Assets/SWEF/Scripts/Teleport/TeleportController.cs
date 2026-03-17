using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.Teleport
{
    /// <summary>
    /// Searches for places via Google Places API (Text Search) and teleports
    /// the player by updating the CesiumGeoreference origin.
    /// MVP: uses UnityWebRequest directly. Production: proxy via backend.
    /// </summary>
    public class TeleportController : MonoBehaviour
    {
        [Header("Google Places API")]
        [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";

        [Header("Refs")]
        [SerializeField] private GameObject georeference;
        [SerializeField] private Transform playerRig;
        [SerializeField] private CanvasGroup fadeOverlay;

        [Header("Teleport Settings")]
        [SerializeField] private float teleportAltitude = 500f;
        [SerializeField] private float fadeDuration = 0.8f;

        public event Action<PlaceResult[]> OnSearchResults;
        public event Action<string> OnSearchError;
        public event Action OnTeleportStarted;
        public event Action OnTeleportCompleted;

        public void Search(string query)
        {
            StartCoroutine(SearchCoroutine(query));
        }

        public void TeleportTo(double lat, double lon, string placeName = "")
        {
            StartCoroutine(TeleportCoroutine(lat, lon, placeName));
        }

        private IEnumerator SearchCoroutine(string query)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                Debug.LogError("[SWEF] Google Places API key not configured.");
                OnSearchError?.Invoke("API key not configured");
                yield break;
            }

            string url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={UnityWebRequest.EscapeURL(query)}&key={apiKey}";

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[SWEF] Places search failed: {req.error}");
                    OnSearchError?.Invoke(req.error);
                    yield break;
                }

                string json = req.downloadHandler.text;
                var response = JsonUtility.FromJson<PlacesTextSearchResponse>(json);

                if (response == null || response.results == null || response.results.Length == 0)
                {
                    OnSearchError?.Invoke("No results found");
                    yield break;
                }

                PlaceResult[] results = new PlaceResult[Mathf.Min(response.results.Length, 5)];
                for (int i = 0; i < results.Length; i++)
                {
                    var r = response.results[i];
                    results[i] = new PlaceResult
                    {
                        name = r.name,
                        address = r.formatted_address,
                        lat = r.geometry.location.lat,
                        lon = r.geometry.location.lng
                    };
                }

                OnSearchResults?.Invoke(results);
            }
        }

        private IEnumerator TeleportCoroutine(double lat, double lon, string placeName)
        {
            OnTeleportStarted?.Invoke();

            if (fadeOverlay != null)
                yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

            SetGeoreferenceOrigin(lat, lon);

            if (playerRig != null)
            {
                playerRig.localPosition = new Vector3(0f, teleportAltitude, 0f);
                playerRig.localRotation = Quaternion.identity;
            }

            yield return new WaitForSeconds(0.5f);

            if (fadeOverlay != null)
                yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

            Debug.Log($"[SWEF] Teleported to {placeName} ({lat}, {lon})");

            // Achievement: first teleport
            if (SWEF.Achievement.AchievementManager.Instance != null)
                SWEF.Achievement.AchievementManager.Instance.TryUnlock("first_teleport");

            OnTeleportCompleted?.Invoke();
        }

        private void SetGeoreferenceOrigin(double lat, double lon)
        {
#if CESIUM_FOR_UNITY
            if (georeference != null)
            {
                var geo = georeference.GetComponent<CesiumForUnity.CesiumGeoreference>();
                if (geo != null)
                    geo.SetOriginLongitudeLatitudeHeight(lon, lat, 0.0);
            }
#else
            Debug.LogWarning("[SWEF] Cesium for Unity not installed. Cannot teleport.");
#endif
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (fadeOverlay == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fadeOverlay.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            fadeOverlay.alpha = to;
        }
    }

    [System.Serializable]
    public struct PlaceResult
    {
        public string name;
        public string address;
        public double lat;
        public double lon;
    }

    [System.Serializable]
    public class PlacesTextSearchResponse
    {
        public PlacesTextSearchResult[] results;
        public string status;
    }

    [System.Serializable]
    public class PlacesTextSearchResult
    {
        public string name;
        public string formatted_address;
        public PlaceGeometry geometry;
    }

    [System.Serializable]
    public class PlaceGeometry
    {
        public PlaceLocation location;
    }

    [System.Serializable]
    public class PlaceLocation
    {
        public double lat;
        public double lng;
    }
}
