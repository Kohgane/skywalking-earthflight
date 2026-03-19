using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 페이지네이션된 글로벌 리더보드 응답 DTO.
    /// </summary>
    [Serializable]
    public class GlobalLeaderboardPage
    {
        public List<GlobalLeaderboardEntry> entries = new();
        public int totalCount;
        public int currentPage;
        public int totalPages;
    }

    /// <summary>
    /// 점수 제출 결과 DTO.
    /// </summary>
    [Serializable]
    public class SubmitResult
    {
        public bool success;
        public int newRank;
        public int previousRank;
        public bool isPersonalBest;
    }

    /// <summary>
    /// 플레이어 순위 정보 DTO.
    /// </summary>
    [Serializable]
    public class PlayerRankInfo
    {
        public int globalRank;
        public int regionalRank;
        public int totalPlayers;
        public float percentile;
    }

    // ── Internal DTOs for JSON serialisation ─────────────────────────────────────

    [Serializable]
    internal class LeaderboardPageDto
    {
        public List<GlobalLeaderboardEntry> entries;
        public int totalCount;
        public int currentPage;
        public int totalPages;
    }

    [Serializable]
    internal class SubmitResultDto
    {
        public bool success;
        public int newRank;
        public int previousRank;
        public bool isPersonalBest;
    }

    [Serializable]
    internal class PlayerRankInfoDto
    {
        public int globalRank;
        public int regionalRank;
        public int totalPlayers;
        public float percentile;
    }

    [Serializable]
    internal class PendingScoreList
    {
        public List<GlobalLeaderboardEntry> scores = new();
    }

    /// <summary>
    /// 글로벌 리더보드 REST API 연동 서비스 (MonoBehaviour Singleton).
    /// 오프라인 시 점수를 큐에 저장하고, 재연결 시 자동으로 전송합니다.
    /// Mock 모드: apiBaseUrl이 비어있거나 서버에 연결할 수 없는 경우 Mock 데이터를 생성합니다.
    /// </summary>
    public class GlobalLeaderboardService : MonoBehaviour
    {
        private const string PendingScoresKey = "SWEF_PendingScores";
        private const float RateLimitSec      = 2f;
        private const float CacheTtlSec       = 60f;

        private static GlobalLeaderboardService _instance;

        /// <summary>Singleton instance.</summary>
        public static GlobalLeaderboardService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<GlobalLeaderboardService>();
                return _instance;
            }
        }

        [Header("API Configuration")]
        [SerializeField] private string apiBaseUrl = "";
        [SerializeField] private string authToken   = "";

        [Tooltip("개발/테스트 시 Mock 데이터를 강제 사용합니다. / Force mock data for dev/testing.")]
        [SerializeField] private bool forceMockMode = false;

        // ── Rate limiting ─────────────────────────────────────────────────────────
        private float _lastRequestTime = -RateLimitSec;
        private readonly Queue<Action> _requestQueue = new();

        // ── In-memory page cache ──────────────────────────────────────────────────
        private readonly Dictionary<string, (GlobalLeaderboardPage page, float timestamp)> _cache = new();

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 저장된 대기 점수가 있으면 연결 성공 시 전송 시도
            StartCoroutine(FlushPendingScoresCoroutine());
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// 리더보드 페이지를 가져옵니다. 60초 캐시 적용.
        /// Fetches a paginated leaderboard page; uses in-memory cache (60 s TTL).
        /// </summary>
        public void FetchLeaderboard(
            LeaderboardCategory category,
            LeaderboardTimeFilter filter,
            int page,
            int pageSize,
            Action<GlobalLeaderboardPage> onComplete,
            Action<string> onError)
        {
            string cacheKey = $"{category}_{filter}_{page}_{pageSize}";

            if (_cache.TryGetValue(cacheKey, out var cached) &&
                Time.realtimeSinceStartup - cached.timestamp < CacheTtlSec)
            {
                onComplete?.Invoke(cached.page);
                return;
            }

            string url = $"{apiBaseUrl}/leaderboard?category={category}&filter={filter}&page={page}&pageSize={pageSize}";
            EnqueueRequest(() => StartCoroutine(GetRequest<LeaderboardPageDto>(url,
                dto =>
                {
                    var result = DtoToPage(dto);
                    _cache[cacheKey] = (result, Time.realtimeSinceStartup);
                    onComplete?.Invoke(result);
                },
                err =>
                {
                    // Mock 데이터로 대체
                    if (IsMockMode(err))
                    {
                        var mock = GenerateMockPage(category, page, pageSize);
                        _cache[cacheKey] = (mock, Time.realtimeSinceStartup);
                        onComplete?.Invoke(mock);
                    }
                    else
                    {
                        onError?.Invoke(err);
                    }
                })));
        }

        /// <summary>
        /// 점수를 제출합니다. 오프라인 시 큐에 저장.
        /// Submits a score to the global leaderboard. Queues locally when offline.
        /// </summary>
        public void SubmitScore(
            GlobalLeaderboardEntry entry,
            Action<SubmitResult> onComplete,
            Action<string> onError)
        {
            string url  = $"{apiBaseUrl}/scores";
            string body = JsonUtility.ToJson(entry);

            EnqueueRequest(() => StartCoroutine(PostRequest<SubmitResultDto>(url, body,
                dto =>
                {
                    onComplete?.Invoke(new SubmitResult
                    {
                        success        = dto.success,
                        newRank        = dto.newRank,
                        previousRank   = dto.previousRank,
                        isPersonalBest = dto.isPersonalBest
                    });
                },
                err =>
                {
                    // 오프라인 큐에 저장
                    QueuePendingScore(entry);

                    if (IsMockMode(err))
                    {
                        onComplete?.Invoke(new SubmitResult { success = true, newRank = 1, isPersonalBest = true });
                    }
                    else
                    {
                        Debug.Log($"[SWEF] GlobalLeaderboardService: score queued offline — {err}");
                        onError?.Invoke(err);
                    }
                })));
        }

        /// <summary>
        /// 특정 플레이어의 순위 정보를 가져옵니다.
        /// Fetches rank info for a specific player.
        /// </summary>
        public void FetchPlayerRank(
            string playerId,
            LeaderboardCategory category,
            Action<PlayerRankInfo> onComplete,
            Action<string> onError)
        {
            string url = $"{apiBaseUrl}/players/{playerId}/rank?category={category}";
            EnqueueRequest(() => StartCoroutine(GetRequest<PlayerRankInfoDto>(url,
                dto =>
                {
                    onComplete?.Invoke(new PlayerRankInfo
                    {
                        globalRank   = dto.globalRank,
                        regionalRank = dto.regionalRank,
                        totalPlayers = dto.totalPlayers,
                        percentile   = dto.percentile
                    });
                },
                err =>
                {
                    if (IsMockMode(err))
                        onComplete?.Invoke(new PlayerRankInfo { globalRank = 42, regionalRank = 5, totalPlayers = 1000, percentile = 95.8f });
                    else
                        onError?.Invoke(err);
                })));
        }

        /// <summary>
        /// 내 순위 ±range 주변 플레이어를 가져옵니다.
        /// Fetches players ranked within ±range of the given player.
        /// </summary>
        public void FetchNearbyPlayers(
            string playerId,
            LeaderboardCategory category,
            int range,
            Action<List<GlobalLeaderboardEntry>> onComplete,
            Action<string> onError)
        {
            string url = $"{apiBaseUrl}/leaderboard/nearby?playerId={playerId}&category={category}&range={range}";
            EnqueueRequest(() => StartCoroutine(GetRequest<LeaderboardPageDto>(url,
                dto =>
                {
                    onComplete?.Invoke(dto.entries ?? new List<GlobalLeaderboardEntry>());
                },
                err =>
                {
                    if (IsMockMode(err))
                        onComplete?.Invoke(GenerateMockEntries(range * 2 + 1, 1));
                    else
                        onError?.Invoke(err);
                })));
        }

        /// <summary>
        /// 지역별 리더보드 페이지를 가져옵니다.
        /// Fetches a regional leaderboard page for the given ISO 3166-1 region code.
        /// </summary>
        public void FetchRegionalLeaderboard(
            string regionCode,
            LeaderboardCategory category,
            int page,
            int pageSize,
            Action<GlobalLeaderboardPage> onComplete,
            Action<string> onError)
        {
            string cacheKey = $"{regionCode}_{category}_{page}_{pageSize}";

            if (_cache.TryGetValue(cacheKey, out var cached) &&
                Time.realtimeSinceStartup - cached.timestamp < CacheTtlSec)
            {
                onComplete?.Invoke(cached.page);
                return;
            }

            string url = $"{apiBaseUrl}/leaderboard/regional?region={regionCode}&category={category}&page={page}&pageSize={pageSize}";
            EnqueueRequest(() => StartCoroutine(GetRequest<LeaderboardPageDto>(url,
                dto =>
                {
                    var result = DtoToPage(dto);
                    _cache[cacheKey] = (result, Time.realtimeSinceStartup);
                    onComplete?.Invoke(result);
                },
                err =>
                {
                    if (IsMockMode(err))
                    {
                        var mock = GenerateMockPage(category, page, pageSize);
                        _cache[cacheKey] = (mock, Time.realtimeSinceStartup);
                        onComplete?.Invoke(mock);
                    }
                    else
                    {
                        onError?.Invoke(err);
                    }
                })));
        }

        // ── Rate-limited request queue ────────────────────────────────────────────

        private void EnqueueRequest(Action request)
        {
            _requestQueue.Enqueue(request);
            if (_requestQueue.Count == 1)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (_requestQueue.Count > 0)
            {
                float elapsed = Time.realtimeSinceStartup - _lastRequestTime;
                if (elapsed < RateLimitSec)
                    yield return new WaitForSeconds(RateLimitSec - elapsed);

                _lastRequestTime = Time.realtimeSinceStartup;
                var request = _requestQueue.Dequeue();
                request?.Invoke();
            }
        }

        // ── HTTP helpers ──────────────────────────────────────────────────────────

        private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(authToken))
                req.SetRequestHeader("Authorization", $"Bearer {authToken}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error ?? "Unknown network error");
                yield break;
            }

            T result = default;
            bool parsed = false;
            try
            {
                result = JsonUtility.FromJson<T>(req.downloadHandler.text);
                parsed = true;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"JSON parse error: {ex.Message}");
            }

            if (parsed)
                onSuccess?.Invoke(result);
        }

        private IEnumerator PostRequest<T>(string url, string jsonBody, Action<T> onSuccess, Action<string> onError)
        {
            using var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(authToken))
                req.SetRequestHeader("Authorization", $"Bearer {authToken}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error ?? "Unknown network error");
                yield break;
            }

            T result = default;
            bool parsed = false;
            try
            {
                result = JsonUtility.FromJson<T>(req.downloadHandler.text);
                parsed = true;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"JSON parse error: {ex.Message}");
            }

            if (parsed)
                onSuccess?.Invoke(result);
        }

        // ── Offline queue ─────────────────────────────────────────────────────────

        private void QueuePendingScore(GlobalLeaderboardEntry entry)
        {
            string json = PlayerPrefs.GetString(PendingScoresKey, "{}");
            PendingScoreList list;
            try { list = JsonUtility.FromJson<PendingScoreList>(json) ?? new PendingScoreList(); }
            catch { list = new PendingScoreList(); }

            list.scores.Add(entry);
            PlayerPrefs.SetString(PendingScoresKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();
            Debug.Log($"[SWEF] GlobalLeaderboardService: queued pending score (total queued: {list.scores.Count})");
        }

        private IEnumerator FlushPendingScoresCoroutine()
        {
            // 앱 시작 후 5초 대기 후 플러시 시도
            yield return new WaitForSeconds(5f);

            string json = PlayerPrefs.GetString(PendingScoresKey, string.Empty);
            if (string.IsNullOrEmpty(json)) yield break;

            PendingScoreList list;
            try { list = JsonUtility.FromJson<PendingScoreList>(json) ?? new PendingScoreList(); }
            catch { yield break; }

            if (list.scores == null || list.scores.Count == 0) yield break;

            Debug.Log($"[SWEF] GlobalLeaderboardService: flushing {list.scores.Count} pending score(s)…");

            var toFlush = new List<GlobalLeaderboardEntry>(list.scores);
            list.scores.Clear();
            PlayerPrefs.SetString(PendingScoresKey, JsonUtility.ToJson(list));
            PlayerPrefs.Save();

            foreach (var entry in toFlush)
            {
                bool done = false;
                SubmitScore(entry,
                    _ => { done = true; },
                    err => { Debug.LogWarning($"[SWEF] GlobalLeaderboardService: flush failed — {err}"); done = true; });
                yield return new WaitUntil(() => done);
                yield return new WaitForSeconds(RateLimitSec);
            }
        }

        // ── Mock data generation ──────────────────────────────────────────────────

        private bool IsMockMode(string error)
        {
            if (forceMockMode || string.IsNullOrEmpty(apiBaseUrl))
                return true;
            // 실제 연결 실패(네트워크 없음)만 Mock으로 폴백; HTTP 오류는 onError로 전달
            return error != null &&
                   (error.Contains("Cannot connect") ||
                    error.Contains("connection") ||
                    error.Contains("Unable to connect") ||
                    error.Contains("Network"));
        }

        private GlobalLeaderboardPage GenerateMockPage(LeaderboardCategory category, int page, int pageSize)
        {
            var entries = GenerateMockEntries(pageSize, (page - 1) * pageSize + 1);
            return new GlobalLeaderboardPage
            {
                entries     = entries,
                totalCount  = 500,
                currentPage = page,
                totalPages  = Mathf.CeilToInt(500f / pageSize)
            };
        }

        private List<GlobalLeaderboardEntry> GenerateMockEntries(int count, int startRank)
        {
            string[] names    = { "AceFlyer", "SkyHawk", "NebulaPilot", "OrbitKing", "StratoJet", "VoidWalker", "CosmosRider", "ZenithPilot" };
            string[] regions  = { "KR", "US", "JP", "CN", "DE", "FR", "GB", "AU" };
            var entries = new List<GlobalLeaderboardEntry>();

            for (int i = 0; i < count; i++)
            {
                int rank   = startRank + i;
                float alt  = Mathf.Max(0f, 120000f - rank * 230f + UnityEngine.Random.Range(-500f, 500f));
                float spd  = Mathf.Max(0f, 8000f  - rank * 15f  + UnityEngine.Random.Range(-50f, 50f));
                float dur  = Mathf.Max(0f, 3600f  - rank * 6f   + UnityEngine.Random.Range(-30f, 30f));

                entries.Add(new GlobalLeaderboardEntry
                {
                    playerId      = $"mock_{rank:D4}",
                    displayName   = names[rank % names.Length] + rank,
                    avatarUrl     = string.Empty,
                    maxAltitude   = alt,
                    maxSpeed      = spd,
                    flightDuration= dur,
                    score         = GlobalLeaderboardEntry.CalculateScore(alt, spd, dur),
                    rank          = rank,
                    region        = regions[rank % regions.Length],
                    date          = DateTime.UtcNow.AddDays(-rank % 30).ToString("yyyy-MM-dd")
                });
            }
            return entries;
        }

        private static GlobalLeaderboardPage DtoToPage(LeaderboardPageDto dto)
        {
            return new GlobalLeaderboardPage
            {
                entries     = dto.entries ?? new List<GlobalLeaderboardEntry>(),
                totalCount  = dto.totalCount,
                currentPage = dto.currentPage,
                totalPages  = dto.totalPages
            };
        }
    }
}
