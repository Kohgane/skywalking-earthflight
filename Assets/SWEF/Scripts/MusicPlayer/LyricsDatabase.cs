using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.MusicPlayer
{
    /// <summary>
    /// Stores, caches, and loads <see cref="LrcData"/> objects for individual tracks.
    /// <para>
    /// Lyrics are sourced in the following priority order:
    /// <list type="number">
    ///   <item>In-memory LRU cache (instant).</item>
    ///   <item>Sidecar <c>.lrc</c> file next to the audio file.</item>
    ///   <item>File in <see cref="Application.streamingAssetsPath"/> under the configured sub-folder.</item>
    ///   <item>User-saved file in <see cref="Application.persistentDataPath"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// User-edited lyrics are persisted to <see cref="Application.persistentDataPath"/>.
    /// Cache eviction uses an LRU strategy capped at <see cref="maxCacheSize"/>.
    /// </para>
    /// </summary>
    public class LyricsDatabase : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const string PersistentFolder  = "Lyrics";
        private const int   MaxFilenameLength = 64;
        private const string LrcExtension      = ".lrc";

        private const string LogTag            = "[SWEF][LyricsDatabase]";

        [Header("Cache")]
        [Tooltip("Maximum number of LrcData entries to keep in memory.")]
        [SerializeField] private int maxCacheSize = 50;

        [Header("Paths")]
        [Tooltip("Sub-folder inside StreamingAssets where embedded .lrc files are stored.")]
        [SerializeField] private string streamingAssetsSubFolder = "Lyrics";

        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired after lyrics have been loaded (or refreshed) for a track.
        /// Parameters: trackId, loaded LrcData (may be empty if none found).
        /// </summary>
        public event Action<string, LrcData> OnLyricsLoaded;

        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>Global singleton instance.</summary>
        public static LyricsDatabase Instance { get; private set; }

        // ── LRU cache internals ───────────────────────────────────────────────────

        private readonly Dictionary<string, LinkedListNode<LruEntry>> _cacheMap =
            new Dictionary<string, LinkedListNode<LruEntry>>(StringComparer.Ordinal);

        private readonly LinkedList<LruEntry> _lruList = new LinkedList<LruEntry>();

        private struct LruEntry
        {
            public string  trackId;
            public LrcData data;
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePersistentFolder();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns cached <see cref="LrcData"/> for <paramref name="trackId"/>, or <c>null</c>
        /// if not yet loaded.
        /// </summary>
        /// <param name="trackId">The unique track identifier.</param>
        public LrcData GetCached(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return null;
            return TryGetFromCache(trackId, out LrcData data) ? data : null;
        }

        /// <summary>
        /// Starts async loading of lyrics for the given track.
        /// Fires <see cref="OnLyricsLoaded"/> when complete (even if no lyrics found).
        /// </summary>
        /// <param name="track">Track whose lyrics should be loaded.</param>
        public void LoadLyricsAsync(MusicTrack track)
        {
            if (track == null) return;
            StartCoroutine(LoadLyricsCoroutine(track));
        }

        /// <summary>
        /// Stores manually-provided plain-text or LRC lyrics for a track, saves them to
        /// persistent storage, and updates the cache.
        /// </summary>
        /// <param name="trackId">Track identifier to associate the lyrics with.</param>
        /// <param name="lrcContent">Raw LRC content (or plain text lines).</param>
        public void SaveUserLyrics(string trackId, string lrcContent)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                Debug.LogWarning($"{LogTag} SaveUserLyrics called with empty trackId.");
                return;
            }

            string path = GetPersistentPath(trackId);
            try
            {
                File.WriteAllText(path, lrcContent ?? string.Empty,
                    System.Text.Encoding.UTF8);
                Debug.Log($"{LogTag} Saved user lyrics for '{trackId}' to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogTag} Failed to save lyrics for '{trackId}': {ex.Message}");
                return;
            }

            LrcData parsed = LrcParser.Parse(lrcContent);
            AddToCache(trackId, parsed);
            OnLyricsLoaded?.Invoke(trackId, parsed);
        }

        /// <summary>
        /// Removes all cached and persistent lyrics for the given track.
        /// </summary>
        /// <param name="trackId">Track identifier.</param>
        public void DeleteUserLyrics(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return;

            RemoveFromCache(trackId);

            string path = GetPersistentPath(trackId);
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch (Exception ex)
                {
                    Debug.LogError($"{LogTag} Failed to delete lyrics for '{trackId}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Immediately adds pre-parsed <see cref="LrcData"/> to the cache without disk I/O.
        /// Fires <see cref="OnLyricsLoaded"/>.
        /// </summary>
        /// <param name="trackId">Track identifier.</param>
        /// <param name="data">Pre-parsed lyrics data.</param>
        public void RegisterParsedLyrics(string trackId, LrcData data)
        {
            if (string.IsNullOrEmpty(trackId) || data == null) return;
            AddToCache(trackId, data);
            OnLyricsLoaded?.Invoke(trackId, data);
        }

        /// <summary>
        /// Evicts all entries from the in-memory cache.
        /// </summary>
        public void ClearCache()
        {
            _cacheMap.Clear();
            _lruList.Clear();
        }

        // ── Async loading coroutine ───────────────────────────────────────────────

        private IEnumerator LoadLyricsCoroutine(MusicTrack track)
        {
            string trackId = track.trackId;

            // 1. Cache hit
            if (TryGetFromCache(trackId, out LrcData cached))
            {
                OnLyricsLoaded?.Invoke(trackId, cached);
                yield break;
            }

            // 2. Persistent storage (user-saved lyrics override embedded)
            string persistentPath = GetPersistentPath(trackId);
            if (File.Exists(persistentPath))
            {
                LrcData d = LoadFromFile(persistentPath);
                if (d != null)
                {
                    AddToCache(trackId, d);
                    OnLyricsLoaded?.Invoke(trackId, d);
                    yield break;
                }
            }

            // 3. Sidecar file (same filename as audio, .lrc extension)
            if (!string.IsNullOrEmpty(track.localFilePath))
            {
                string sidecarPath = Path.ChangeExtension(track.localFilePath, LrcExtension);
                if (File.Exists(sidecarPath))
                {
                    LrcData d = LoadFromFile(sidecarPath);
                    if (d != null)
                    {
                        AddToCache(trackId, d);
                        OnLyricsLoaded?.Invoke(trackId, d);
                        yield break;
                    }
                }
            }

            // 4. Explicit lrcFilePath on the track
            if (!string.IsNullOrEmpty(track.lrcFilePath))
            {
                // Absolute or StreamingAssets-relative
                string lrcPath = track.lrcFilePath;
                if (!Path.IsPathRooted(lrcPath))
                    lrcPath = Path.Combine(Application.streamingAssetsPath, lrcPath);

#if UNITY_ANDROID && !UNITY_EDITOR
                bool found4 = false;
                yield return LoadAndroidStreamingAssetChecked(trackId, lrcPath,
                    result => { if (result) found4 = true; });
                if (found4) yield break;
#else
                if (File.Exists(lrcPath))
                {
                    LrcData d = LoadFromFile(lrcPath);
                    if (d != null)
                    {
                        AddToCache(trackId, d);
                        OnLyricsLoaded?.Invoke(trackId, d);
                        yield break;
                    }
                }
#endif
            }

            // 5. StreamingAssets — try by track ID and by title
            string saFolder = Path.Combine(Application.streamingAssetsPath, streamingAssetsSubFolder);
            string[] candidates =
            {
                Path.Combine(saFolder, trackId + LrcExtension),
                Path.Combine(saFolder, SanitizeFilename(track.title) + LrcExtension)
            };

            foreach (string candidate in candidates)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bool foundCandidate = false;
                yield return LoadAndroidStreamingAssetChecked(trackId, candidate,
                    result => { if (result) foundCandidate = true; });
                if (foundCandidate) yield break;
#else
                if (File.Exists(candidate))
                {
                    LrcData d = LoadFromFile(candidate);
                    if (d != null)
                    {
                        AddToCache(trackId, d);
                        OnLyricsLoaded?.Invoke(trackId, d);
                        yield break;
                    }
                }
#endif
            }

            // Nothing found — notify with empty data
            var empty = new LrcData();
            AddToCache(trackId, empty);
            OnLyricsLoaded?.Invoke(trackId, empty);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator LoadAndroidStreamingAsset(string trackId, string path)
        {
            yield return LoadAndroidStreamingAssetChecked(trackId, path, null);
        }

        private IEnumerator LoadAndroidStreamingAssetChecked(string trackId, string path,
            Action<bool> onComplete)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    LrcData d = LrcParser.Parse(req.downloadHandler.text);
                    AddToCache(trackId, d);
                    OnLyricsLoaded?.Invoke(trackId, d);
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"{LogTag} Android streaming asset not found: {path}");
                    onComplete?.Invoke(false);
                }
            }
        }
#endif

        // ── Helpers ───────────────────────────────────────────────────────────────

        private LrcData LoadFromFile(string path)
        {
            try
            {
                string content = File.ReadAllText(path, System.Text.Encoding.UTF8);
                return LrcParser.Parse(content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogTag} Failed to read {path}: {ex.Message}");
                return null;
            }
        }

        private void EnsurePersistentFolder()
        {
            string folder = Path.Combine(Application.persistentDataPath, PersistentFolder);
            if (!Directory.Exists(folder))
            {
                try { Directory.CreateDirectory(folder); }
                catch (Exception ex)
                {
                    Debug.LogError($"{LogTag} Could not create persistent folder: {ex.Message}");
                }
            }
        }

        private string GetPersistentPath(string trackId) =>
            Path.Combine(Application.persistentDataPath, PersistentFolder,
                SanitizeFilename(trackId) + LrcExtension);

        private static string SanitizeFilename(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > MaxFilenameLength ? name.Substring(0, MaxFilenameLength) : name;
        }

        // ── LRU cache helpers ─────────────────────────────────────────────────────

        private bool TryGetFromCache(string trackId, out LrcData data)
        {
            if (_cacheMap.TryGetValue(trackId, out var node))
            {
                // Move to front (most-recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                data = node.Value.data;
                return true;
            }
            data = null;
            return false;
        }

        private void AddToCache(string trackId, LrcData data)
        {
            if (_cacheMap.TryGetValue(trackId, out var existing))
            {
                _lruList.Remove(existing);
                _cacheMap.Remove(trackId);
            }

            var entry = new LruEntry { trackId = trackId, data = data };
            var node  = _lruList.AddFirst(entry);
            _cacheMap[trackId] = node;

            // Evict LRU entry when over capacity
            while (_lruList.Count > maxCacheSize && _lruList.Count > 0)
            {
                LinkedListNode<LruEntry> lru = _lruList.Last;
                _lruList.RemoveLast();
                _cacheMap.Remove(lru.Value.trackId);
            }
        }

        private void RemoveFromCache(string trackId)
        {
            if (_cacheMap.TryGetValue(trackId, out var node))
            {
                _lruList.Remove(node);
                _cacheMap.Remove(trackId);
            }
        }
    }
}
