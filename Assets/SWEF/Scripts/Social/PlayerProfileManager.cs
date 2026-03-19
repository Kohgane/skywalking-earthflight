using System;
using UnityEngine;

namespace SWEF.Social
{
    /// <summary>
    /// 로컬 플레이어 프로필 관리자 (MonoBehaviour Singleton).
    /// Manages the local player's identity for the global leaderboard:
    /// UUID, display name, region, and optional avatar URL.
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        private const string PrefsPlayerId   = "SWEF_PlayerId";
        private const string PrefsDisplayName = "SWEF_DisplayName";
        private const string PrefsRegion     = "SWEF_Region";
        private const string PrefsAvatarUrl  = "SWEF_AvatarUrl";

        private static PlayerProfileManager _instance;

        /// <summary>Singleton instance.</summary>
        public static PlayerProfileManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<PlayerProfileManager>();
                return _instance;
            }
        }

        /// <summary>
        /// 플레이어 고유 UUID. 최초 실행 시 생성되어 PlayerPrefs에 저장됩니다.
        /// Player UUID — generated once on first run and persisted.
        /// </summary>
        public string PlayerId  { get; private set; }

        /// <summary>
        /// 표시 이름. "Pilot_XXXX" 형식의 기본값. 2–20자, 저장됩니다.
        /// Display name (2–20 chars). Defaults to "Pilot_XXXX".
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 지역 코드. 기기 로케일에서 자동 감지.
        /// ISO 3166-1 alpha-2 region code, auto-detected from device locale.
        /// </summary>
        public string Region { get; private set; }

        /// <summary>Optional avatar URL (may be empty).</summary>
        public string AvatarUrl { get; private set; }

        /// <summary>프로필이 업데이트될 때 발생합니다.</summary>
        public event Action OnProfileUpdated;

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
            LoadProfile();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// 표시 이름을 변경합니다. 2–20자, 공백 외의 문자가 포함되어야 합니다.
        /// Updates the display name after validation (2–20 chars, non-blank).
        /// </summary>
        /// <param name="name">New display name.</param>
        /// <returns>True when valid and saved; false when validation fails.</returns>
        public bool SetDisplayName(string name)
        {
            if (!IsValidDisplayName(name, out string reason))
            {
                Debug.LogWarning($"[SWEF] PlayerProfileManager: invalid name — {reason}");
                return false;
            }

            DisplayName = name.Trim();
            PlayerPrefs.SetString(PrefsDisplayName, DisplayName);
            PlayerPrefs.Save();
            OnProfileUpdated?.Invoke();
            Debug.Log($"[SWEF] PlayerProfileManager: display name set to '{DisplayName}'");
            return true;
        }

        /// <summary>
        /// ISO 3166-1 alpha-2 지역 코드를 설정합니다.
        /// Sets the ISO 3166-1 alpha-2 region code.
        /// </summary>
        public void SetRegion(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode) || regionCode.Length != 2)
            {
                Debug.LogWarning($"[SWEF] PlayerProfileManager: invalid region code '{regionCode}'");
                return;
            }

            Region = regionCode.ToUpperInvariant();
            PlayerPrefs.SetString(PrefsRegion, Region);
            PlayerPrefs.Save();
            OnProfileUpdated?.Invoke();
            Debug.Log($"[SWEF] PlayerProfileManager: region set to '{Region}'");
        }

        /// <summary>Sets the avatar URL (optional).</summary>
        public void SetAvatarUrl(string url)
        {
            AvatarUrl = url ?? string.Empty;
            PlayerPrefs.SetString(PrefsAvatarUrl, AvatarUrl);
            PlayerPrefs.Save();
            OnProfileUpdated?.Invoke();
        }

        /// <summary>
        /// 표시 이름 유효성 검사.
        /// Validates a candidate display name. Returns false with a reason when invalid.
        /// </summary>
        public static bool IsValidDisplayName(string name, out string reason)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                reason = "이름이 비어있습니다 / Name is empty.";
                return false;
            }

            string trimmed = name.Trim();

            if (trimmed.Length < 2)
            {
                reason = "이름은 최소 2자 이상이어야 합니다 / Name must be at least 2 characters.";
                return false;
            }

            if (trimmed.Length > 20)
            {
                reason = "이름은 최대 20자 이하여야 합니다 / Name must be 20 characters or fewer.";
                return false;
            }

            // 간단한 비속어 필터 placeholder (전체 단어 일치만 차단)
            string lower = trimmed.ToLowerInvariant();
            string[] reserved = { "admin", "moderator" };
            foreach (var word in reserved)
            {
                if (lower == word)
                {
                    reason = "사용할 수 없는 이름입니다 / This name is reserved.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void LoadProfile()
        {
            // UUID: 없으면 새로 생성
            PlayerId = PlayerPrefs.GetString(PrefsPlayerId, string.Empty);
            if (string.IsNullOrEmpty(PlayerId))
            {
                PlayerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(PrefsPlayerId, PlayerId);
                PlayerPrefs.Save();
                Debug.Log($"[SWEF] PlayerProfileManager: new player UUID generated — {PlayerId}");
            }

            // 표시 이름: 없으면 "Pilot_XXXX" 기본값
            DisplayName = PlayerPrefs.GetString(PrefsDisplayName, string.Empty);
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = $"Pilot_{UnityEngine.Random.Range(1000, 9999)}";
                PlayerPrefs.SetString(PrefsDisplayName, DisplayName);
                PlayerPrefs.Save();
            }

            // 지역: 없으면 시스템 언어로 추측
            Region = PlayerPrefs.GetString(PrefsRegion, string.Empty);
            if (string.IsNullOrEmpty(Region))
            {
                Region = RegionHelper.DetectRegion();
                PlayerPrefs.SetString(PrefsRegion, Region);
                PlayerPrefs.Save();
            }

            AvatarUrl = PlayerPrefs.GetString(PrefsAvatarUrl, string.Empty);

            Debug.Log($"[SWEF] PlayerProfileManager: loaded — id={PlayerId}, name={DisplayName}, region={Region}");
        }
    }
}
