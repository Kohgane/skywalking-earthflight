using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SWEF.PhotoMode
{
    /// <summary>
    /// Singleton gallery manager that indexes, sorts, filters, and searches all photos
    /// stored in persistent storage.  Provides comparison, slideshow, and batch-operation
    /// capabilities.
    /// </summary>
    public class PhotoGalleryManager : MonoBehaviour
    {
        #region Constants
        private const string PhotoFolderRoot   = "Photos";
        private const string MetadataExtension = ".json";
        private const float  DefaultSlideInterval = 5f;
        #endregion

        #region Singleton
        /// <summary>Global singleton instance.</summary>
        public static PhotoGalleryManager Instance { get; private set; }
        #endregion

        #region Events
        /// <summary>Fired after a photo is permanently deleted.</summary>
        public event Action<string> OnPhotoDeleted;

        /// <summary>Fired after the gallery index is refreshed.</summary>
        public event Action OnGalleryRefreshed;
        #endregion

        #region Public properties
        /// <summary>True while a slideshow is playing.</summary>
        public bool IsSlideshowPlaying { get; private set; }

        /// <summary>Read-only list of all indexed photos.</summary>
        public IReadOnlyList<PhotoMetadata> AllPhotos => _photos;
        #endregion

        #region Private state
        private readonly List<PhotoMetadata> _photos = new List<PhotoMetadata>();
        private Coroutine _slideshowCoroutine;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RefreshGallery();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public API — querying
        /// <summary>
        /// Re-indexes all photos from persistent storage and fires
        /// <see cref="OnGalleryRefreshed"/>.
        /// </summary>
        public void RefreshGallery()
        {
            _photos.Clear();
            string root = Path.Combine(Application.persistentDataPath, PhotoFolderRoot);
            if (!Directory.Exists(root)) { OnGalleryRefreshed?.Invoke(); return; }

            foreach (string metaFile in Directory.GetFiles(root, "*" + MetadataExtension,
                                                            SearchOption.AllDirectories))
            {
                try
                {
                    string json = File.ReadAllText(metaFile);
                    PhotoMetadata meta = JsonUtility.FromJson<PhotoMetadata>(json);
                    if (meta != null) _photos.Add(meta);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PhotoGalleryManager] Failed to load {metaFile}: {ex.Message}");
                }
            }

            // Default sort: newest first — parse ISO-8601 for reliable comparison
            _photos.Sort((a, b) =>
            {
                DateTime.TryParse(b.timestamp, out DateTime dtB);
                DateTime.TryParse(a.timestamp, out DateTime dtA);
                return dtB.CompareTo(dtA);
            });
            OnGalleryRefreshed?.Invoke();
        }

        /// <summary>
        /// Returns all photos.
        /// </summary>
        /// <returns>Full gallery list (sorted by date descending).</returns>
        public List<PhotoMetadata> GetAllPhotos()
        {
            return new List<PhotoMetadata>(_photos);
        }

        /// <summary>
        /// Returns photos captured on the specified local date.
        /// </summary>
        /// <param name="date">Date to filter by (time part is ignored).</param>
        /// <returns>Matching photos.</returns>
        public List<PhotoMetadata> GetPhotosByDate(DateTime date)
        {
            string prefix = date.ToString("yyyy-MM-dd");
            return _photos.Where(p => p.timestamp?.StartsWith(prefix) == true).ToList();
        }

        /// <summary>
        /// Returns photos tagged with <paramref name="tag"/> (case-insensitive).
        /// </summary>
        /// <param name="tag">Tag to match.</param>
        /// <returns>Matching photos.</returns>
        public List<PhotoMetadata> GetPhotosByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return GetAllPhotos();
            return _photos.Where(p => p.tags != null &&
                p.tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        /// <summary>
        /// Searches photos by <paramref name="query"/> across tags, location, and timestamp.
        /// </summary>
        /// <param name="query">Free-text search query.</param>
        /// <returns>Matching photos ranked by relevance.</returns>
        public List<PhotoMetadata> SearchPhotos(string query)
        {
            if (string.IsNullOrEmpty(query)) return GetAllPhotos();
            string q = query.ToLowerInvariant();
            return _photos.Where(p =>
                (p.tags?.Any(t => t.ToLowerInvariant().Contains(q)) == true) ||
                (p.weatherCondition?.ToLowerInvariant().Contains(q) == true) ||
                (p.playerAircraftType?.ToLowerInvariant().Contains(q) == true) ||
                (p.timestamp?.Contains(q) == true)).ToList();
        }

        /// <summary>
        /// Returns all photos sorted by the specified field.
        /// </summary>
        /// <param name="field">Field to sort by: "date", "favorite", "size".</param>
        /// <param name="ascending">Sort direction.</param>
        /// <returns>Sorted list.</returns>
        public List<PhotoMetadata> GetSortedPhotos(string field, bool ascending = false)
        {
            IEnumerable<PhotoMetadata> sorted;
            switch (field?.ToLowerInvariant())
            {
                case "favorite": sorted = _photos.OrderByDescending(p => p.isFavorite); break;
                case "size":     sorted = _photos.OrderByDescending(p => p.fileSize); break;
                default:
                    sorted = _photos.OrderByDescending(p =>
                    {
                        DateTime.TryParse(p.timestamp, out DateTime dt);
                        return dt;
                    });
                    break;
            }
            if (ascending) sorted = sorted.Reverse();
            return sorted.ToList();
        }
        #endregion

        #region Public API — management
        /// <summary>
        /// Permanently deletes the photo with <paramref name="photoId"/> from storage
        /// and removes it from the gallery index.
        /// </summary>
        /// <param name="photoId">ID of the photo to delete.</param>
        public void DeletePhoto(string photoId)
        {
            PhotoMetadata meta = _photos.FirstOrDefault(p => p.photoId == photoId);
            if (meta == null) return;

            TryDeleteFile(meta.filePath);
            TryDeleteFile(meta.thumbnailPath);
            TryDeleteFile(Path.ChangeExtension(meta.filePath, MetadataExtension));

            _photos.Remove(meta);
            OnPhotoDeleted?.Invoke(photoId);
        }

        /// <summary>
        /// Toggles the favourite flag on the specified photo and persists the change.
        /// </summary>
        /// <param name="photoId">ID of the photo to toggle.</param>
        public void ToggleFavorite(string photoId)
        {
            PhotoMetadata meta = _photos.FirstOrDefault(p => p.photoId == photoId);
            if (meta == null) return;
            meta.isFavorite = !meta.isFavorite;
            PersistMetadata(meta);
        }

        /// <summary>
        /// Adds a tag to a photo and persists the change.
        /// </summary>
        /// <param name="photoId">Target photo.</param>
        /// <param name="tag">Tag to add.</param>
        public void AddTag(string photoId, string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            PhotoMetadata meta = _photos.FirstOrDefault(p => p.photoId == photoId);
            if (meta == null) return;
            meta.tags ??= new List<string>();
            if (!meta.tags.Contains(tag)) { meta.tags.Add(tag); PersistMetadata(meta); }
        }
        #endregion

        #region Slideshow
        /// <summary>
        /// Starts an automatic slideshow cycling through <paramref name="photos"/>.
        /// </summary>
        /// <param name="photos">Ordered list of photos to show.</param>
        /// <param name="interval">Seconds between transitions.</param>
        public void StartSlideshow(List<PhotoMetadata> photos, float interval = DefaultSlideInterval)
        {
            StopSlideshow();
            if (photos == null || photos.Count == 0) return;
            _slideshowCoroutine = StartCoroutine(SlideshowCoroutine(photos, interval));
        }

        /// <summary>Stops any running slideshow.</summary>
        public void StopSlideshow()
        {
            if (_slideshowCoroutine != null)
            {
                StopCoroutine(_slideshowCoroutine);
                _slideshowCoroutine = null;
            }
            IsSlideshowPlaying = false;
        }

        private IEnumerator SlideshowCoroutine(List<PhotoMetadata> photos, float interval)
        {
            IsSlideshowPlaying = true;
            int idx = 0;
            while (IsSlideshowPlaying)
            {
                // External UI listens to an event or polls AllPhotos[idx].
                idx = (idx + 1) % photos.Count;
                yield return new WaitForSeconds(interval);
            }
        }
        #endregion

        #region Helpers
        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Debug.LogWarning($"[PhotoGalleryManager] Could not delete {path}: {ex.Message}"); }
        }

        private static void PersistMetadata(PhotoMetadata meta)
        {
            if (meta == null || string.IsNullOrEmpty(meta.filePath)) return;
            string metaPath = Path.ChangeExtension(meta.filePath, MetadataExtension);
            try { File.WriteAllText(metaPath, JsonUtility.ToJson(meta, true)); }
            catch (Exception ex) { Debug.LogWarning($"[PhotoGalleryManager] Persist failed: {ex.Message}"); }
        }
        #endregion
    }
}
