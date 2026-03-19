using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SWEF.Performance
{
    /// <summary>
    /// Profiles asset loading times and memory cost.
    /// Other systems call <see cref="RecordLoad"/> after each asset load.
    /// Stores at most 500 events (FIFO), and provides accessors for the slowest
    /// and largest loads along with a formatted text report.
    /// </summary>
    public class AssetLoadProfiler : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static AssetLoadProfiler Instance { get; private set; }

        // ── Constants ────────────────────────────────────────────────────────────
        private const int MaxEvents = 500;

        // ── State ────────────────────────────────────────────────────────────────
        private readonly List<AssetLoadEvent> _events = new List<AssetLoadEvent>(MaxEvents);

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
        }

        // ── Public API ───────────────────────────────────────────────────────────
        /// <summary>
        /// Records a completed asset load.
        /// </summary>
        /// <param name="name">Asset name or path.</param>
        /// <param name="type">Asset type string (e.g. "Texture2D", "AudioClip").</param>
        /// <param name="timeMs">How long the load took in milliseconds.</param>
        /// <param name="bytes">Approximate memory size of the loaded asset in bytes.</param>
        public void RecordLoad(string name, string type, float timeMs, long bytes)
        {
            if (_events.Count >= MaxEvents)
                _events.RemoveAt(0);

            _events.Add(new AssetLoadEvent
            {
                assetName      = name,
                assetType      = type,
                loadTimeMs     = timeMs,
                memorySizeBytes = bytes,
                timestamp      = DateTime.Now,
            });
        }

        /// <summary>Returns the <paramref name="count"/> slowest recorded loads by time.</summary>
        public List<AssetLoadEvent> GetSlowestLoads(int count)
        {
            var sorted = new List<AssetLoadEvent>(_events);
            sorted.Sort((a, b) => b.loadTimeMs.CompareTo(a.loadTimeMs));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        /// <summary>Returns the <paramref name="count"/> largest recorded loads by memory.</summary>
        public List<AssetLoadEvent> GetLargestLoads(int count)
        {
            var sorted = new List<AssetLoadEvent>(_events);
            sorted.Sort((a, b) => b.memorySizeBytes.CompareTo(a.memorySizeBytes));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        /// <summary>Total cumulative load time across all recorded events, in milliseconds.</summary>
        public float GetTotalLoadTimeMs()
        {
            float total = 0f;
            foreach (var e in _events)
                total += e.loadTimeMs;
            return total;
        }

        /// <summary>Clears all recorded asset-load events.</summary>
        public void Clear()
        {
            _events.Clear();
        }

        /// <summary>
        /// Returns a formatted multi-line string report listing all recorded load events,
        /// sorted from slowest to fastest.
        /// </summary>
        public string GenerateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Asset Load Report ({_events.Count} events) ===");
            sb.AppendLine($"Total load time: {GetTotalLoadTimeMs():F1} ms");
            sb.AppendLine();
            sb.AppendLine($"{"Asset",-40} {"Type",-15} {"Time(ms)",10} {"Mem(KB)",10} {"Timestamp",-20}");
            sb.AppendLine(new string('-', 100));

            var sorted = new List<AssetLoadEvent>(_events);
            sorted.Sort((a, b) => b.loadTimeMs.CompareTo(a.loadTimeMs));

            foreach (var e in sorted)
            {
                sb.AppendLine(
                    $"{e.assetName,-40} {e.assetType,-15} {e.loadTimeMs,10:F2} {e.memorySizeBytes / 1024,10} {e.timestamp:HH:mm:ss.fff,-20}");
            }

            return sb.ToString();
        }
    }

    // ── Data types ────────────────────────────────────────────────────────────

    /// <summary>Record of a single asset-load event.</summary>
    [Serializable]
    public struct AssetLoadEvent
    {
        public string   assetName;
        public string   assetType;
        public float    loadTimeMs;
        public long     memorySizeBytes;
        public DateTime timestamp;
    }
}
