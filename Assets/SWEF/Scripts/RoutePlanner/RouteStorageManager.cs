using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SWEF.RoutePlanner
{
    /// <summary>
    /// Phase 49 — Provides persistent storage for user-created and downloaded
    /// <see cref="FlightRoute"/> objects.  Routes are saved as JSON files inside
    /// subfolders of <see cref="Application.persistentDataPath"/>.
    /// Supports export to / import from <c>.swefroute</c> files.
    /// </summary>
    public class RouteStorageManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>Global singleton instance.</summary>
        public static RouteStorageManager Instance { get; private set; }

        #endregion

        #region Constants

        private const string FolderMyRoutes    = "Routes/MyRoutes";
        private const string FolderDownloaded  = "Routes/Downloaded";
        private const string FolderDrafts      = "Routes/Drafts";
        private const string RouteExtension    = ".swefroute";
        private const string IndexFileName     = "route_index.json";

        #endregion

        #region Events

        /// <summary>Fired after a route is successfully saved.</summary>
        public event Action<FlightRoute> OnRouteSaved;

        /// <summary>Fired after a route is deleted; carries the deleted route id.</summary>
        public event Action<string> OnRouteDeleted;

        /// <summary>Fired after a route is imported from a <c>.swefroute</c> file.</summary>
        public event Action<FlightRoute> OnRouteImported;

        #endregion

        #region Private State

        private string _rootPath;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _rootPath = Application.persistentDataPath;
            EnsureFolders();
        }

        #endregion

        #region Public API — CRUD

        /// <summary>
        /// Serialises <paramref name="route"/> to a JSON file in the appropriate subfolder.
        /// Draft routes go to <c>Routes/Drafts/</c>; others to <c>Routes/MyRoutes/</c>.
        /// </summary>
        public void SaveRoute(FlightRoute route)
        {
            if (route == null) return;

            string folder  = route.routeType == RouteType.Custom && string.IsNullOrEmpty(route.creatorId)
                ? FolderDrafts : FolderMyRoutes;
            string path    = GetFilePath(folder, route.routeId);
            string json    = JsonUtility.ToJson(route, prettyPrint: true);

            try
            {
                File.WriteAllText(path, json);
                OnRouteSaved?.Invoke(route);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RouteStorageManager.SaveRoute failed: {ex.Message}");
            }
        }

        /// <summary>Loads and deserialises the route with the specified id from disk.</summary>
        /// <returns>The loaded route, or <c>null</c> on failure.</returns>
        public FlightRoute LoadRoute(string routeId)
        {
            foreach (string folder in new[] { FolderMyRoutes, FolderDownloaded, FolderDrafts })
            {
                string path = GetFilePath(folder, routeId);
                if (!File.Exists(path)) continue;

                try
                {
                    string json = File.ReadAllText(path);
                    return JsonUtility.FromJson<FlightRoute>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SWEF] RouteStorageManager.LoadRoute failed: {ex.Message}");
                    return null;
                }
            }
            return null;
        }

        /// <summary>Deletes the route file for <paramref name="routeId"/> from all subfolders.</summary>
        public void DeleteRoute(string routeId)
        {
            bool deleted = false;
            foreach (string folder in new[] { FolderMyRoutes, FolderDownloaded, FolderDrafts })
            {
                string path = GetFilePath(folder, routeId);
                if (!File.Exists(path)) continue;
                File.Delete(path);
                deleted = true;
            }
            if (deleted) OnRouteDeleted?.Invoke(routeId);
        }

        /// <summary>Returns all routes found across all subfolders.</summary>
        public List<FlightRoute> GetAllRoutes()
        {
            var all = new List<FlightRoute>();
            all.AddRange(LoadFolder(FolderMyRoutes));
            all.AddRange(LoadFolder(FolderDownloaded));
            all.AddRange(LoadFolder(FolderDrafts));
            return all;
        }

        /// <summary>Returns only routes saved in <c>Routes/MyRoutes/</c>.</summary>
        public List<FlightRoute> GetMyRoutes()     => LoadFolder(FolderMyRoutes);

        /// <summary>Returns only routes saved in <c>Routes/Downloaded/</c>.</summary>
        public List<FlightRoute> GetDownloadedRoutes() => LoadFolder(FolderDownloaded);

        /// <summary>Returns only routes saved in <c>Routes/Drafts/</c>.</summary>
        public List<FlightRoute> GetDrafts()       => LoadFolder(FolderDrafts);

        #endregion

        #region Public API — Import / Export

        /// <summary>
        /// Serialises <paramref name="route"/> to a <c>.swefroute</c> file at the specified path.
        /// </summary>
        /// <param name="route">Route to export.</param>
        /// <param name="exportPath">Full file-system path for the output file.</param>
        public void ExportRoute(FlightRoute route, string exportPath)
        {
            if (route == null || string.IsNullOrEmpty(exportPath)) return;

            if (!exportPath.EndsWith(RouteExtension, StringComparison.OrdinalIgnoreCase))
                exportPath += RouteExtension;

            try
            {
                string json = JsonUtility.ToJson(route, prettyPrint: true);
                File.WriteAllText(exportPath, json);
                Debug.Log($"[SWEF] Route exported to: {exportPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RouteStorageManager.ExportRoute failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a <c>.swefroute</c> file, validates its structure, saves it to the
        /// <c>Downloaded</c> folder, and returns the loaded route.
        /// </summary>
        /// <param name="importPath">Full file-system path to the <c>.swefroute</c> file.</param>
        /// <returns>The imported <see cref="FlightRoute"/>, or <c>null</c> on failure.</returns>
        public FlightRoute ImportRoute(string importPath)
        {
            if (!File.Exists(importPath))
            {
                Debug.LogWarning($"[SWEF] RouteStorageManager.ImportRoute: file not found: {importPath}");
                return null;
            }

            try
            {
                string json   = File.ReadAllText(importPath);
                var route = JsonUtility.FromJson<FlightRoute>(json);

                if (route == null || string.IsNullOrEmpty(route.routeId))
                {
                    Debug.LogWarning("[SWEF] RouteStorageManager.ImportRoute: invalid .swefroute file.");
                    return null;
                }

                // Preserve the original routeId so sharing/leaderboard links remain valid.
                // If a route with the same ID already exists locally, generate a new local ID.
                var existing = LoadRoute(route.routeId);
                if (existing != null)
                    route.routeId = Guid.NewGuid().ToString();
                route.downloadCount++;

                string dest = GetFilePath(FolderDownloaded, route.routeId);
                File.WriteAllText(dest, JsonUtility.ToJson(route, prettyPrint: true));

                OnRouteImported?.Invoke(route);
                RoutePlannerAnalytics.Instance?.TrackRouteImported(route);

                return route;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SWEF] RouteStorageManager.ImportRoute failed: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Private Helpers

        private void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(_rootPath, FolderMyRoutes));
            Directory.CreateDirectory(Path.Combine(_rootPath, FolderDownloaded));
            Directory.CreateDirectory(Path.Combine(_rootPath, FolderDrafts));
        }

        private string GetFilePath(string folder, string routeId) =>
            Path.Combine(_rootPath, folder, routeId + RouteExtension);

        private List<FlightRoute> LoadFolder(string folder)
        {
            var result = new List<FlightRoute>();
            string dir = Path.Combine(_rootPath, folder);

            if (!Directory.Exists(dir)) return result;

            foreach (string file in Directory.GetFiles(dir, "*" + RouteExtension))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var route   = JsonUtility.FromJson<FlightRoute>(json);
                    if (route != null) result.Add(route);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] RouteStorageManager: failed to load {file}: {ex.Message}");
                }
            }

            return result;
        }

        #endregion
    }
}
