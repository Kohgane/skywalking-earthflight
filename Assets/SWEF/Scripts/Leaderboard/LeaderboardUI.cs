using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SWEF.Social;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 글로벌 리더보드 UI 컨트롤러. World 씬 HUD에 연결하여 사용합니다.
    /// Full leaderboard UI controller for the World scene HUD.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Filters")]
        [SerializeField] private Dropdown categoryDropdown;
        [SerializeField] private Dropdown timeFilterDropdown;
        [SerializeField] private Dropdown regionDropdown;

        [Header("Entry List")]
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;

        [Header("Player Info")]
        [SerializeField] private Text playerRankText;
        [SerializeField] private Text playerScoreText;
        [SerializeField] private Text playerPercentileText;

        [Header("Pagination")]
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Text pageIndicatorText;

        [Header("State")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private Text errorText;
        [SerializeField] private Button submitButton;

        // ── Internal state ────────────────────────────────────────────────────────
        private const int PageSize = 20;
        private int _currentPage  = 1;
        private int _totalPages   = 1;

        private LeaderboardCategory  _selectedCategory   = LeaderboardCategory.BestOverallScore;
        private LeaderboardTimeFilter _selectedFilter     = LeaderboardTimeFilter.AllTime;
        private string               _selectedRegion     = string.Empty;

        private readonly List<LeaderboardEntryUI> _entryPool = new();
        private Social.LeaderboardManager _leaderboardManager;

        // ── Unity lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            openButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
            prevPageButton?.onClick.AddListener(PrevPage);
            nextPageButton?.onClick.AddListener(NextPage);
            submitButton?.onClick.AddListener(SubmitCurrentScore);

            _leaderboardManager = FindFirstObjectByType<Social.LeaderboardManager>();

            SetupCategoryDropdown();
            SetupTimeFilterDropdown();
            SetupRegionDropdown();

            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        // ── Public interface ──────────────────────────────────────────────────────

        /// <summary>리더보드 패널을 열고 첫 페이지를 로드합니다.</summary>
        public void Open()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);

            _currentPage = 1;
            Refresh();
        }

        /// <summary>리더보드 패널을 닫습니다.</summary>
        public void Close()
        {
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(false);
        }

        /// <summary>현재 필터 설정으로 리더보드를 새로 고칩니다.</summary>
        public void Refresh()
        {
            ShowLoading(true);
            ShowError(string.Empty);

            var service = GlobalLeaderboardService.Instance;
            if (service == null)
            {
                ShowLoading(false);
                ShowError("GlobalLeaderboardService not found.");
                return;
            }

            if (!string.IsNullOrEmpty(_selectedRegion))
            {
                service.FetchRegionalLeaderboard(
                    _selectedRegion, _selectedCategory, _currentPage, PageSize,
                    OnPageFetched, OnFetchError);
            }
            else
            {
                service.FetchLeaderboard(
                    _selectedCategory, _selectedFilter, _currentPage, PageSize,
                    OnPageFetched, OnFetchError);
            }

            // 플레이어 개인 순위 갱신
            var profile = PlayerProfileManager.Instance;
            if (profile != null && service != null)
            {
                service.FetchPlayerRank(profile.PlayerId, _selectedCategory,
                    OnPlayerRankFetched, _ => { });
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private void OnPageFetched(GlobalLeaderboardPage page)
        {
            ShowLoading(false);

            _totalPages = page.totalPages;
            UpdatePaginationButtons();

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"{_currentPage} / {_totalPages}";

            PopulateEntries(page.entries);
        }

        private void OnFetchError(string err)
        {
            ShowLoading(false);
            ShowError($"로드 실패: {err}");
            Debug.LogWarning($"[SWEF] LeaderboardUI: fetch error — {err}");
        }

        private void OnPlayerRankFetched(PlayerRankInfo info)
        {
            if (playerRankText != null)
                playerRankText.text = $"#{info.globalRank}";
            if (playerPercentileText != null)
                playerPercentileText.text = $"상위 {100f - info.percentile:0.0}%";
        }

        private void PopulateEntries(List<GlobalLeaderboardEntry> entries)
        {
            // 기존 항목 비활성화
            foreach (var ui in _entryPool)
                ui.gameObject.SetActive(false);

            if (entryContainer == null || entryPrefab == null) return;

            string myId = PlayerProfileManager.Instance?.PlayerId ?? string.Empty;

            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntryUI ui;

                if (i < _entryPool.Count)
                {
                    ui = _entryPool[i];
                    ui.gameObject.SetActive(true);
                }
                else
                {
                    var go = Instantiate(entryPrefab, entryContainer);
                    ui = go.GetComponent<LeaderboardEntryUI>();
                    if (ui == null)
                        ui = go.AddComponent<LeaderboardEntryUI>();
                    _entryPool.Add(ui);
                }

                ui.SetData(entries[i], entries[i].playerId == myId);
            }
        }

        private void SubmitCurrentScore()
        {
            var profile = PlayerProfileManager.Instance;
            var service = GlobalLeaderboardService.Instance;
            if (profile == null || service == null) return;

            // 현재 세션 데이터로 글로벌 점수 제출
            float alt = 0f, spd = 0f, dur = 0f;
            if (_leaderboardManager != null)
            {
                var top = _leaderboardManager.GetTopEntry();
                if (top != null) { alt = top.maxAltitude; spd = top.maxSpeed; dur = top.duration; }
            }

            var entry = new GlobalLeaderboardEntry
            {
                playerId       = profile.PlayerId,
                displayName    = profile.DisplayName,
                avatarUrl      = profile.AvatarUrl,
                maxAltitude    = alt,
                maxSpeed       = spd,
                flightDuration = dur,
                score          = GlobalLeaderboardEntry.CalculateScore(alt, spd, dur),
                region         = profile.Region,
                date           = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            service.SubmitScore(entry,
                result => { Debug.Log($"[SWEF] LeaderboardUI: submitted — new rank #{result.newRank}"); Refresh(); },
                err    => ShowError($"제출 실패: {err}"));
        }

        private void PrevPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                Refresh();
            }
        }

        private void NextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                Refresh();
            }
        }

        private void UpdatePaginationButtons()
        {
            if (prevPageButton != null) prevPageButton.interactable = _currentPage > 1;
            if (nextPageButton != null) nextPageButton.interactable = _currentPage < _totalPages;
        }

        private void ShowLoading(bool show)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(show);
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text     = message;
                errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        // ── Dropdown setup ────────────────────────────────────────────────────────

        private void SetupCategoryDropdown()
        {
            if (categoryDropdown == null) return;
            categoryDropdown.ClearOptions();
            var opts = new List<string>();
            foreach (LeaderboardCategory cat in System.Enum.GetValues(typeof(LeaderboardCategory)))
                opts.Add(LeaderboardCategoryHelper.GetDisplayName(cat));
            categoryDropdown.AddOptions(opts);
            categoryDropdown.onValueChanged.AddListener(v =>
            {
                _selectedCategory = (LeaderboardCategory)v;
                _currentPage = 1;
                Refresh();
            });
        }

        private void SetupTimeFilterDropdown()
        {
            if (timeFilterDropdown == null) return;
            timeFilterDropdown.ClearOptions();
            timeFilterDropdown.AddOptions(new List<string> { "전체 / All Time", "월간 / Monthly", "주간 / Weekly", "일간 / Daily" });
            timeFilterDropdown.onValueChanged.AddListener(v =>
            {
                _selectedFilter = (LeaderboardTimeFilter)v;
                _currentPage = 1;
                Refresh();
            });
        }

        private void SetupRegionDropdown()
        {
            if (regionDropdown == null) return;
            regionDropdown.ClearOptions();
            var opts = new List<string> { "전 세계 / All Regions" };
            foreach (var kv in RegionHelper.Regions)
                opts.Add($"{RegionHelper.GetFlagEmoji(kv.Key)} {kv.Value} ({kv.Key})");
            regionDropdown.AddOptions(opts);
            regionDropdown.onValueChanged.AddListener(v =>
            {
                if (v == 0)
                {
                    _selectedRegion = string.Empty;
                }
                else
                {
                    int idx = 0;
                    foreach (var kv in RegionHelper.Regions)
                    {
                        idx++;
                        if (idx == v) { _selectedRegion = kv.Key; break; }
                    }
                }
                _currentPage = 1;
                Refresh();
            });
        }
    }
}
