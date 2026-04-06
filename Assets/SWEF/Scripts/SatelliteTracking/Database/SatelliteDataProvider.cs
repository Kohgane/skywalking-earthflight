// SatelliteDataProvider.cs — Phase 114: Satellite & Space Debris Tracking
// Interface for satellite data sources: CelesTrak, Space-Track.org, N2YO API.
// Namespace: SWEF.SatelliteTracking

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.SatelliteTracking
{
    /// <summary>
    /// Abstract base class for satellite data sources.  Derived classes implement
    /// the actual network fetching for each provider.
    /// </summary>
    public abstract class SatelliteDataProvider : MonoBehaviour
    {
        /// <summary>Raised when fresh TLE data has been downloaded and parsed.</summary>
        public event Action<List<TLEData>> OnDataReceived;

        /// <summary>Raised when the fetch fails.</summary>
        public event Action<string> OnFetchError;

        /// <summary>Initiates an asynchronous TLE data fetch.</summary>
        public abstract void FetchLatestTLEs();

        /// <summary>Sends a successful result to subscribers.</summary>
        protected void NotifyReceived(List<TLEData> tles) => OnDataReceived?.Invoke(tles);

        /// <summary>Sends an error to subscribers.</summary>
        protected void NotifyError(string error) => OnFetchError?.Invoke(error);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // CelesTrak provider
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches TLE data from CelesTrak (https://celestrak.org/).
    /// </summary>
    public class CelesTrakProvider : SatelliteDataProvider
    {
        [Tooltip("CelesTrak endpoint — e.g. https://celestrak.org/SOCRATES/")]
        [SerializeField] private string endpointUrl = "https://celestrak.org/SOCRATES/query.php?GROUP=stations&FORMAT=tle";

        public override void FetchLatestTLEs() => StartCoroutine(FetchCoroutine());

        private IEnumerator FetchCoroutine()
        {
            using var req = UnityWebRequest.Get(endpointUrl);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                NotifyReceived(TLEParser.ParseMultiple(req.downloadHandler.text));
            else
                NotifyError($"CelesTrak fetch failed: {req.error}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Mock / offline provider
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Provides a built-in set of mock TLE data for offline / Editor use.
    /// </summary>
    public class MockSatelliteDataProvider : SatelliteDataProvider
    {
        // Minimal valid TLE set: ISS
        private const string MockTLEs =
            "ISS (ZARYA)\n" +
            "1 25544U 98067A   24001.50000000  .00016717  00000-0  10270-3 0  9003\n" +
            "2 25544  51.6400 347.4302 0001798 356.2986 119.5127 15.49815764442996\n" +
            "NOAA 18\n" +
            "1 28654U 05018A   24001.50000000  .00000078  00000-0  65527-4 0  9009\n" +
            "2 28654  99.0187 120.4123 0013942 246.6680 113.2947 14.12401983964010\n";

        public override void FetchLatestTLEs()
            => NotifyReceived(TLEParser.ParseMultiple(MockTLEs));
    }

#if SWEF_SATELLITE_API_AVAILABLE
    // ─────────────────────────────────────────────────────────────────────────────
    // Space-Track.org provider (requires API credentials)
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches TLE data from Space-Track.org (requires login credentials).
    /// Compile-guarded by <c>SWEF_SATELLITE_API_AVAILABLE</c>.
    /// </summary>
    public class SpaceTrackProvider : SatelliteDataProvider
    {
        [SerializeField] private string username;
        [SerializeField] private string password;
        private const string LoginUrl  = "https://www.space-track.org/ajaxauth/login";
        private const string QueryUrl  = "https://www.space-track.org/basicspacedata/query/class/gp/DECAY_DATE/null-val/EPOCH/%3Enow-30/orderby/NORAD_CAT_ID/format/tle";

        public override void FetchLatestTLEs() => StartCoroutine(LoginAndFetch());

        private IEnumerator LoginAndFetch()
        {
            var form = new WWWForm();
            form.AddField("identity", username);
            form.AddField("password", password);
            using var login = UnityWebRequest.Post(LoginUrl, form);
            yield return login.SendWebRequest();
            if (login.result != UnityWebRequest.Result.Success)
            {
                NotifyError($"Space-Track login failed: {login.error}");
                yield break;
            }
            using var query = UnityWebRequest.Get(QueryUrl);
            yield return query.SendWebRequest();
            if (query.result == UnityWebRequest.Result.Success)
                NotifyReceived(TLEParser.ParseMultiple(query.downloadHandler.text));
            else
                NotifyError($"Space-Track query failed: {query.error}");
        }
    }
#endif
}
