using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.Core
{
    /// <summary>
    /// Boot scene entry point.
    /// Acquires GPS fix, stores it in SWEFSession, then loads the World scene.
    /// Integrates with LoadingScreen and ErrorHandler for user feedback.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string worldSceneName = "World";
        [SerializeField] private float locationInitTimeoutSec = 15f;

        [Header("UI")]
        [SerializeField] private LoadingScreen loadingScreen;

        private IEnumerator Start()
        {
            loadingScreen?.Show();

            SWEFSession.Clear();

            loadingScreen?.SetStatus("Checking location services...");

            if (!Input.location.isEnabledByUser)
            {
                Debug.LogError("[SWEF] Location service disabled by user.");
                ErrorHandler.ShowGPSError();
                loadingScreen?.Hide();
                yield break;
            }

            Input.location.Start(desiredAccuracyInMeters: 10f, updateDistanceInMeters: 5f);

            loadingScreen?.SetProgress(0.3f);
            loadingScreen?.SetStatus("Acquiring GPS fix...");

            float t = locationInitTimeoutSec;
            while (Input.location.status == LocationServiceStatus.Initializing && t > 0f)
            {
                t -= Time.deltaTime;
                loadingScreen?.SetProgress(0.3f + 0.4f * (1f - t / locationInitTimeoutSec));
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogError($"[SWEF] Location service failed: {Input.location.status}");
                ErrorHandler.ShowGPSTimeoutError();
                loadingScreen?.Hide();
                yield break;
            }

            var d = Input.location.lastData;
            double lat = d.latitude;
            double lon = d.longitude;
            double alt = (double.IsNaN(d.altitude) || d.altitude <= 0) ? 30.0 : d.altitude;

            SWEFSession.Set(lat, lon, alt);
            Debug.Log($"[SWEF] GPS fix: lat={lat}, lon={lon}, alt={alt}");

            loadingScreen?.SetProgress(0.9f);
            loadingScreen?.SetStatus("Loading world...");

            yield return null; // one frame so progress update renders

            loadingScreen?.SetProgress(1f);

            SceneManager.LoadScene(worldSceneName);
        }
    }
}
