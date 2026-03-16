using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWEF.Core
{
    /// <summary>
    /// Boot scene entry point.
    /// Acquires GPS fix, stores it in SWEFSession, then loads the World scene.
    /// </summary>
    public class BootManager : MonoBehaviour
    {
        [SerializeField] private string worldSceneName = "World";
        [SerializeField] private float locationInitTimeoutSec = 15f;

        private IEnumerator Start()
        {
            SWEFSession.Clear();

            if (!Input.location.isEnabledByUser)
            {
                Debug.LogError("[SWEF] Location service disabled by user.");
                yield break;
            }

            Input.location.Start(desiredAccuracyInMeters: 10f, updateDistanceInMeters: 5f);

            float t = locationInitTimeoutSec;
            while (Input.location.status == LocationServiceStatus.Initializing && t > 0f)
            {
                t -= Time.deltaTime;
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogError($"[SWEF] Location service failed: {Input.location.status}");
                yield break;
            }

            var d = Input.location.lastData;
            double lat = d.latitude;
            double lon = d.longitude;
            double alt = (double.IsNaN(d.altitude) || d.altitude <= 0) ? 30.0 : d.altitude;

            SWEFSession.Set(lat, lon, alt);
            Debug.Log($"[SWEF] GPS fix: lat={lat}, lon={lon}, alt={alt}");

            SceneManager.LoadScene(worldSceneName);
        }
    }
}
