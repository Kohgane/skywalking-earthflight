using UnityEngine;
using UnityEngine.UI;

namespace SWEF.Leaderboard
{
    /// <summary>
    /// 단일 리더보드 행 UI 컴포넌트.
    /// UI component for a single leaderboard entry row.
    /// </summary>
    public class LeaderboardEntryUI : MonoBehaviour
    {
        // ── Top-3 colours ─────────────────────────────────────────────────────────
        private static readonly Color GoldColor   = new Color(1f,   0.84f, 0f,   1f);
        private static readonly Color SilverColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color BronzeColor = new Color(0.80f, 0.50f, 0.20f, 1f);
        private static readonly Color PlayerColor = new Color(0.20f, 0.60f, 1f,   0.35f);

        [Header("Row UI")]
        [SerializeField] private Text  rankText;
        [SerializeField] private Text  nameText;
        [SerializeField] private Text  scoreText;
        [SerializeField] private Text  altitudeText;
        [SerializeField] private Text  speedText;
        [SerializeField] private Text  regionFlagText;
        [SerializeField] private Image highlightBg;

        /// <summary>
        /// 항목 데이터를 설정합니다. 현재 플레이어 행은 별도 색상으로 표시됩니다.
        /// Populates this row with entry data. Current-player row receives a distinct highlight.
        /// </summary>
        /// <param name="entry">Entry data.</param>
        /// <param name="isCurrentPlayer">True when this row represents the local player.</param>
        public void SetData(GlobalLeaderboardEntry entry, bool isCurrentPlayer)
        {
            if (entry == null) return;

            if (rankText != null)
                rankText.text = $"#{entry.rank}";

            if (nameText != null)
                nameText.text = entry.displayName;

            if (scoreText != null)
                scoreText.text = $"{entry.score:N0}";

            if (altitudeText != null)
            {
                altitudeText.text = entry.maxAltitude >= 1000f
                    ? $"{entry.maxAltitude / 1000f:0.0} km"
                    : $"{entry.maxAltitude:0} m";
            }

            if (speedText != null)
            {
                float kmh  = entry.maxSpeed * 3.6f;
                float mach = entry.maxSpeed / 343f;
                speedText.text = entry.maxSpeed >= 343f
                    ? $"{kmh:0} km/h (Mach {mach:0.0})"
                    : $"{kmh:0} km/h";
            }

            if (regionFlagText != null)
                regionFlagText.text = string.IsNullOrEmpty(entry.region)
                    ? "🌍"
                    : Social.RegionHelper.GetFlagEmoji(entry.region);

            // 배경 색상: 상위 3위 또는 현재 플레이어
            if (highlightBg != null)
            {
                if (isCurrentPlayer)
                    highlightBg.color = PlayerColor;
                else if (entry.rank == 1)
                    highlightBg.color = new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.30f);
                else if (entry.rank == 2)
                    highlightBg.color = new Color(SilverColor.r, SilverColor.g, SilverColor.b, 0.30f);
                else if (entry.rank == 3)
                    highlightBg.color = new Color(BronzeColor.r, BronzeColor.g, BronzeColor.b, 0.30f);
                else
                    highlightBg.color = Color.clear;
            }

            // 1-3위 이름 색상
            if (nameText != null)
            {
                nameText.color = entry.rank switch
                {
                    1 => GoldColor,
                    2 => SilverColor,
                    3 => BronzeColor,
                    _ => isCurrentPlayer ? new Color(0.40f, 0.80f, 1f) : Color.white
                };
            }
        }
    }
}
