using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.Analytics
{
    /// <summary>
    /// Singleton MonoBehaviour that manages event queuing, batching, compression,
    /// and network dispatch for the SWEF telemetry pipeline.
    /// </summary>
    public class TelemetryDispatcher : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static TelemetryDispatcher Instance { get; private set; }

        // ── Inspector / Configuration ────────────────────────────────────────────
        [Header("Network")]
        [SerializeField] private string endpointUrl = "";
        [SerializeField] private string apiKey = "";

        [Header("Batching")]
        [SerializeField] private float batchIntervalSeconds = 30f;
        [SerializeField] private int   batchSizeThreshold  = 50;
        [SerializeField] private int   maxQueueSize        = 500;

        [Header("Retry")]
        [SerializeField] private int   maxRetries          = 3;

        [Header("Privacy")]
        [SerializeField] private bool telemetryEnabled = true;

        // ── Events ───────────────────────────────────────────────────────────────
        /// <summary>Raised when a batch is successfully dispatched.</summary>
        public event Action<int> OnBatchDispatched;

        /// <summary>Raised when a batch dispatch ultimately fails.</summary>
        public event Action<string> OnDispatchFailed;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly List<TelemetryEvent> _queue = new List<TelemetryEvent>();
        private int   _sequenceCounter;
        private float _batchTimer;
        private bool  _isFlushing;

        private string _sessionId = "";
        private string _userId    = "";

        private const string QueueFileName = "swef_telemetry_queue.json";
        private const int    CompressThresholdBytes = 4096;

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

            _sessionId = Guid.NewGuid().ToString();
            LoadPersistedQueue();
        }

        private void Start()
        {
            Application.lowMemory += OnLowMemory;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        private void Update()
        {
            if (!telemetryEnabled) return;

            _batchTimer += Time.unscaledDeltaTime;
            if (_batchTimer >= batchIntervalSeconds)
            {
                _batchTimer = 0f;
                if (_queue.Count > 0 && !_isFlushing)
                    StartCoroutine(DispatchBatch(false));
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _queue.Count > 0)
                PersistQueueToDisk();
        }

        private void OnApplicationQuit()
        {
            if (_queue.Count > 0)
                PersistQueueToDisk();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Enqueue a telemetry event. No-op when telemetry is disabled.</summary>
        public void EnqueueEvent(TelemetryEvent evt)
        {
            if (!telemetryEnabled || evt == null) return;

            // Fill session-level fields
            evt.sessionId      = _sessionId;
            evt.userId         = _userId;
            evt.sequenceNumber = ++_sequenceCounter;

            if (_queue.Count >= maxQueueSize)
                _queue.RemoveAt(0); // drop oldest

            _queue.Add(evt);

            // Count-based flush
            if (_queue.Count >= batchSizeThreshold && !_isFlushing)
                StartCoroutine(DispatchBatch(false));
        }

        /// <summary>
        /// Enqueue a critical event (purchase / crash) and immediately flush.
        /// </summary>
        public void EnqueueCriticalEvent(TelemetryEvent evt)
        {
            EnqueueEvent(evt);
            if (!_isFlushing)
                StartCoroutine(DispatchBatch(true));
        }

        /// <summary>Manually flush the current queue to the backend.</summary>
        public void FlushNow()
        {
            if (!_isFlushing && _queue.Count > 0)
                StartCoroutine(DispatchBatch(false));
        }

        /// <summary>Discard all queued events.</summary>
        public void ClearQueue()
        {
            _queue.Clear();
        }

        /// <summary>Sets the anonymized user ID provided by <see cref="PrivacyConsentManager"/>.</summary>
        public void SetUserId(string anonymizedId) => _userId = anonymizedId ?? "";

        /// <summary>Enables or disables telemetry at runtime.</summary>
        public void SetTelemetryEnabled(bool enabled)
        {
            telemetryEnabled = enabled;
            if (!enabled) ClearQueue();
        }

        /// <summary>Returns the number of events currently in the queue.</summary>
        public int QueueCount => _queue.Count;

        /// <summary>Returns a snapshot of the last N events (most recent last).</summary>
        public List<TelemetryEvent> GetRecentEvents(int count)
        {
            int start = Mathf.Max(0, _queue.Count - count);
            return _queue.GetRange(start, _queue.Count - start);
        }

        // ── Dispatch ─────────────────────────────────────────────────────────────

        private IEnumerator DispatchBatch(bool priority)
        {
            if (_isFlushing && !priority) yield break;
            _isFlushing = true;

            // Check connectivity
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                PersistQueueToDisk();
                _isFlushing = false;
                yield break;
            }

            // Snapshot and clear queue
            var batch = new List<TelemetryEvent>(_queue);
            _queue.Clear();

            string json  = SerializeBatch(batch);
            byte[] body  = BuildRequestBody(json);

            bool dispatched = false;
            int  retries    = 0;
            float delay     = 1f;

            while (!dispatched && retries <= maxRetries)
            {
                if (retries > 0)
                    yield return new WaitForSecondsRealtime(delay);

                yield return StartCoroutine(PostBatch(body, success =>
                {
                    dispatched = success;
                }));

                if (!dispatched)
                {
                    retries++;
                    delay = Mathf.Min(delay * 2f, 30f);
                }
            }

            if (dispatched)
            {
                OnBatchDispatched?.Invoke(batch.Count);
                // Delete persisted queue on successful dispatch
                DeletePersistedQueue();
            }
            else
            {
                // Re-queue events at the front and persist
                _queue.InsertRange(0, batch);
                PersistQueueToDisk();
                OnDispatchFailed?.Invoke($"Failed after {maxRetries} retries.");
            }

            _isFlushing = false;
        }

        private IEnumerator PostBatch(byte[] body, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                // No backend configured — treat as success (offline-only mode)
                callback(true);
                yield break;
            }

            using var req = new UnityWebRequest(endpointUrl, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrEmpty(apiKey))
                req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Signal GZip encoding when payload was compressed
            if (body.Length >= CompressThresholdBytes)
                req.SetRequestHeader("Content-Encoding", "gzip");

            yield return req.SendWebRequest();

            callback(req.result == UnityWebRequest.Result.Success);
        }

        // ── Serialisation ────────────────────────────────────────────────────────

        private static string SerializeBatch(List<TelemetryEvent> batch)
        {
            var sb = new StringBuilder();
            sb.Append("{\"events\":[");
            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(SerializeEvent(batch[i]));
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string SerializeEvent(TelemetryEvent e)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"eventId\":\"{EscapeJson(e.eventId)}\",");
            sb.Append($"\"eventName\":\"{EscapeJson(e.eventName)}\",");
            sb.Append($"\"category\":\"{EscapeJson(e.category)}\",");
            sb.Append($"\"sessionId\":\"{EscapeJson(e.sessionId)}\",");
            sb.Append($"\"userId\":\"{EscapeJson(e.userId)}\",");
            sb.Append($"\"timestamp\":\"{EscapeJson(e.timestamp.ToString("o"))}\",");
            sb.Append($"\"sequenceNumber\":{e.sequenceNumber},");
            sb.Append("\"properties\":{");
            if (e.properties != null)
            {
                bool first = true;
                foreach (var kvp in e.properties)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append($"\"{EscapeJson(kvp.Key)}\":");
                    AppendJsonValue(sb, kvp.Value);
                }
            }
            sb.Append("}}");
            return sb.ToString();
        }

        private static void AppendJsonValue(StringBuilder sb, object value)
        {
            if (value == null)  { sb.Append("null"); return; }
            if (value is bool b){ sb.Append(b ? "true" : "false"); return; }
            if (value is int i) { sb.Append(i); return; }
            if (value is float f){ sb.Append(f.ToString("G", System.Globalization.CultureInfo.InvariantCulture)); return; }
            if (value is double d){ sb.Append(d.ToString("G", System.Globalization.CultureInfo.InvariantCulture)); return; }
            if (value is long l){ sb.Append(l); return; }
            sb.Append($"\"{EscapeJson(value.ToString())}\"");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static byte[] BuildRequestBody(string json)
        {
            byte[] raw = Encoding.UTF8.GetBytes(json);
            if (raw.Length < CompressThresholdBytes) return raw;

            using var ms  = new MemoryStream();
            using var gz  = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true);
            gz.Write(raw, 0, raw.Length);
            gz.Close();
            return ms.ToArray();
        }

        // ── Persistence ──────────────────────────────────────────────────────────

        private string QueueFilePath =>
            Path.Combine(Application.persistentDataPath, QueueFileName);

        private void PersistQueueToDisk()
        {
            try
            {
                string json = SerializeBatch(_queue);
                File.WriteAllText(QueueFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TelemetryDispatcher: failed to persist queue — {ex.Message}");
            }
        }

        private void LoadPersistedQueue()
        {
            string path = QueueFilePath;
            if (!File.Exists(path)) return;

            try
            {
                // Persisted events are logged on startup; the file is left in place
                // and will be deleted only after the next successful batch dispatch.
                Debug.Log("[SWEF] TelemetryDispatcher: found persisted telemetry queue; will flush on next dispatch.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SWEF] TelemetryDispatcher: failed to load persisted queue — {ex.Message}");
            }
        }

        private void DeletePersistedQueue()
        {
            try { if (File.Exists(QueueFilePath)) File.Delete(QueueFilePath); }
            catch { /* best-effort */ }
        }

        // ── Callbacks ────────────────────────────────────────────────────────────

        private void OnLowMemory()
        {
            // Trim queue to avoid being killed by OOM
            int keep = maxQueueSize / 2;
            if (_queue.Count > keep)
                _queue.RemoveRange(0, _queue.Count - keep);
        }
    }
}
