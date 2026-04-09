using UnityEngine;

namespace SWEF.Replay
{
    /// <summary>
    /// Handles sharing and importing of SWEF replay files between players.
    /// Integrates with the existing <see cref="ShareManager"/> for native share-sheet
    /// dispatch on mobile. Supports deep-link–based replay exchange and clipboard import.
    /// </summary>
    public class ReplayShareManager : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const int MAX_INLINE_SHARE_LENGTH = 10000;
        // ── Inspector fields ──────────────────────────────────────────────────────
        [Header("Dependencies")]
        [SerializeField] private ReplayFileManager fileManager;
        [SerializeField] private MonoBehaviour      shareManager;

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when a replay has been successfully imported.</summary>
        public event System.Action<ReplayData> OnReplayImported;

        /// <summary>Fired when a share attempt fails.</summary>
        public event System.Action<string> OnReplayShareFailed;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (fileManager == null)
                fileManager = FindFirstObjectByType<ReplayFileManager>();
            if (shareManager == null)
            {
                var t = System.Type.GetType("SWEF.Social.ShareManager, SWEF.Social");
                if (t != null) shareManager = FindFirstObjectByType(t) as MonoBehaviour;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Exports <paramref name="replayId"/> as a Base64-encoded string and shares it
        /// via the native share sheet. When the encoded data exceeds 10,000 characters a
        /// short deep-link is used instead.
        /// </summary>
        public void ShareReplay(string replayId)
        {
            if (fileManager == null)
            {
                Debug.LogWarning("[SWEF] ReplayShareManager: ReplayFileManager not found.");
                OnReplayShareFailed?.Invoke("ReplayFileManager not found");
                return;
            }

            var data = fileManager.LoadReplay(replayId);
            if (data == null)
            {
                Debug.LogWarning($"[SWEF] ReplayShareManager: Could not load replay '{replayId}'.");
                OnReplayShareFailed?.Invoke($"Could not load replay '{replayId}'");
                return;
            }

            string encodedData = fileManager.ExportReplayToString(replayId);
            if (string.IsNullOrEmpty(encodedData))
            {
                OnReplayShareFailed?.Invoke("Export failed");
                return;
            }

            string shareText;
            if (encodedData.Length > MAX_INLINE_SHARE_LENGTH)
            {
                // Fall back to a short deep link when the inline payload is too large
                string link = $"swef://replay?id={replayId}";
                shareText   = BuildShareMessage(data, link);
            }
            else
            {
                shareText = BuildShareMessage(data, encodedData);
            }

            if (shareManager != null)
                shareManager.SendMessage("ShareReplayText", shareText);
            else
                GUIUtility.systemCopyBuffer = shareText;

            Debug.Log($"[SWEF] ReplayShareManager: Shared replay '{replayId}'.");

            // Fire replay-shared achievement (loose-coupled to avoid circular assembly dependency)
            var achMgrType = System.Type.GetType("SWEF.Achievement.AchievementManager, SWEF.Achievement");
            var achInstance = achMgrType?.GetProperty("Instance")?.GetValue(null);
            achMgrType?.GetMethod("TryUnlock")?.Invoke(achInstance, new object[] { "replay_shared" });
        }

        /// <summary>
        /// Shares a short <c>swef://replay</c> deep link for <paramref name="replayId"/>
        /// with basic metadata embedded as query parameters.
        /// </summary>
        public void ShareReplayAsDeepLink(string replayId)
        {
            if (fileManager == null)
            {
                OnReplayShareFailed?.Invoke("ReplayFileManager not found");
                return;
            }

            var data = fileManager.LoadReplay(replayId);
            if (data == null)
            {
                OnReplayShareFailed?.Invoke($"Could not load replay '{replayId}'");
                return;
            }

            string link = $"swef://replay?id={replayId}" +
                          $"&name={System.Uri.EscapeDataString(data.playerName ?? string.Empty)}" +
                          $"&alt={data.maxAltitudeM:F0}" +
                          $"&dur={data.totalDurationSec:F0}";

            if (shareManager != null)
                shareManager.SendMessage("ShareReplayText", link);
            else
                GUIUtility.systemCopyBuffer = link;

            Debug.Log($"[SWEF] ReplayShareManager: Shared deep link for replay '{replayId}'.");
        }

        /// <summary>
        /// Reads clipboard text and attempts to decode it as a replay import string.
        /// Returns <c>null</c> when the clipboard does not contain valid replay data.
        /// </summary>
        public ReplayData ImportFromClipboard()
        {
            string clipText = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipText))
            {
                Debug.Log("[SWEF] ReplayShareManager: Clipboard is empty.");
                return null;
            }

            if (fileManager == null) return null;

            var imported = fileManager.ImportReplayFromString(clipText);
            if (imported == null)
            {
                Debug.Log("[SWEF] ReplayShareManager: Clipboard content is not valid replay data.");
            }
            else
            {
                Debug.Log($"[SWEF] ReplayShareManager: Imported replay '{imported.replayId}' from clipboard.");
                OnReplayImported?.Invoke(imported);
            }

            return imported;
        }

        /// <summary>
        /// Handles a <c>swef://replay?…</c> deep link URL.
        /// When a <c>data=</c> parameter is present the inline payload is decoded.
        /// When only an <c>id=</c> parameter is present a cloud download stub is logged.
        /// </summary>
        public void HandleDeepLink(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            Debug.Log($"[SWEF] ReplayShareManager: Handling deep link → {url}");

            string data = ParseQueryParam(url, "data");
            if (!string.IsNullOrEmpty(data))
            {
                if (fileManager == null)
                {
                    OnReplayShareFailed?.Invoke("ReplayFileManager not found");
                    return;
                }

                var imported = fileManager.ImportReplayFromString(data);
                if (imported != null)
                {
                    Debug.Log($"[SWEF] ReplayShareManager: Imported inline replay '{imported.replayId}' from deep link.");
                    OnReplayImported?.Invoke(imported);
                }
                else
                {
                    Debug.LogWarning("[SWEF] ReplayShareManager: Failed to decode inline replay from deep link.");
                    OnReplayShareFailed?.Invoke("Failed to decode inline replay");
                }
                return;
            }

            string id = ParseQueryParam(url, "id");
            if (!string.IsNullOrEmpty(id))
            {
                // Cloud download is a stub — a future phase will add server-side storage
                Debug.Log($"[SWEF] ReplayShareManager: Cloud download stub — replay id='{id}' (not yet implemented).");
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static string BuildShareMessage(ReplayData data, string linkOrCode)
        {
            float durationS = data.totalDurationSec;
            float altKm     = data.maxAltitudeM / 1000f;
            float spdKmh    = data.maxSpeedMps   * 3.6f;

            return $"🚀 Check out my SWEF flight! " +
                   $"Duration: {durationS:F0}s | Max Alt: {altKm:F1}km | Max Speed: {spdKmh:F0}km/h\n" +
                   $"Import code: {linkOrCode}";
        }

        private static string ParseQueryParam(string url, string key)
        {
            int q = url.IndexOf('?');
            if (q < 0) return null;
            string query  = url.Substring(q + 1);
            string prefix = key + "=";
            foreach (string seg in query.Split('&'))
            {
                if (seg.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return System.Uri.UnescapeDataString(seg.Substring(prefix.Length).Replace("+", " "));
            }
            return null;
        }
    }
}
