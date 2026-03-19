using System.Collections.Generic;
using UnityEngine;

namespace SWEF.Social
{
    /// <summary>
    /// 지역 코드 유틸리티. ISO 3166-1 alpha-2 코드와 국가명, 국기 이모지를 매핑합니다.
    /// Static utility for ISO 3166-1 alpha-2 region codes, country names, and flag emoji.
    /// </summary>
    public static class RegionHelper
    {
        /// <summary>
        /// ISO 3166-1 alpha-2 코드 → 국가명 매핑 (상위 30개국).
        /// Top 30+ countries: code → display name.
        /// </summary>
        public static readonly Dictionary<string, string> Regions = new()
        {
            { "KR", "대한민국" },
            { "US", "United States" },
            { "JP", "日本" },
            { "CN", "中国" },
            { "DE", "Deutschland" },
            { "FR", "France" },
            { "GB", "United Kingdom" },
            { "AU", "Australia" },
            { "CA", "Canada" },
            { "BR", "Brasil" },
            { "IN", "India" },
            { "MX", "México" },
            { "RU", "Россия" },
            { "IT", "Italia" },
            { "ES", "España" },
            { "NL", "Nederland" },
            { "SE", "Sverige" },
            { "NO", "Norge" },
            { "DK", "Danmark" },
            { "FI", "Suomi" },
            { "PL", "Polska" },
            { "TR", "Türkiye" },
            { "SA", "المملكة العربية السعودية" },
            { "AE", "الإمارات" },
            { "SG", "Singapore" },
            { "TW", "台灣" },
            { "HK", "香港" },
            { "TH", "ประเทศไทย" },
            { "ID", "Indonesia" },
            { "PH", "Pilipinas" },
            { "VN", "Việt Nam" },
            { "MY", "Malaysia" },
            { "NZ", "New Zealand" },
            { "ZA", "South Africa" },
            { "NG", "Nigeria" },
            { "EG", "مصر" },
            { "AR", "Argentina" },
            { "CL", "Chile" },
            { "CO", "Colombia" }
        };

        /// <summary>
        /// 시스템 언어로부터 지역 코드를 추측합니다.
        /// Guesses the region code from <see cref="Application.systemLanguage"/>.
        /// </summary>
        public static string DetectRegion()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Korean    => "KR",
                SystemLanguage.Japanese  => "JP",
                SystemLanguage.Chinese   => "CN",
                SystemLanguage.German    => "DE",
                SystemLanguage.French    => "FR",
                SystemLanguage.Italian   => "IT",
                SystemLanguage.Spanish   => "ES",
                SystemLanguage.Dutch     => "NL",
                SystemLanguage.Swedish   => "SE",
                SystemLanguage.Norwegian => "NO",
                SystemLanguage.Danish    => "DK",
                SystemLanguage.Finnish   => "FI",
                SystemLanguage.Polish    => "PL",
                SystemLanguage.Russian   => "RU",
                SystemLanguage.Thai      => "TH",
                SystemLanguage.Turkish   => "TR",
                SystemLanguage.Portuguese=> "BR",
                SystemLanguage.Indonesian=> "ID",
                SystemLanguage.Vietnamese=> "VN",
                SystemLanguage.Arabic    => "SA",
                _                        => "US"
            };
        }

        /// <summary>
        /// ISO 3166-1 alpha-2 코드로부터 국기 이모지를 반환합니다.
        /// Returns the flag emoji for the given ISO 3166-1 alpha-2 region code.
        /// </summary>
        /// <param name="regionCode">Two-letter uppercase region code.</param>
        /// <returns>Flag emoji string, or "🌍" if unknown.</returns>
        public static string GetFlagEmoji(string regionCode)
        {
            if (string.IsNullOrEmpty(regionCode) || regionCode.Length != 2)
                return "🌍";

            string upper = regionCode.ToUpperInvariant();

            // 두 글자가 모두 A-Z 범위인지 확인
            if (upper[0] < 'A' || upper[0] > 'Z' || upper[1] < 'A' || upper[1] > 'Z')
                return "🌍";

            // 유니코드 지역 표시 문자: 'A' = U+1F1E6
            int a = upper[0] - 'A' + 0x1F1E6;
            int b = upper[1] - 'A' + 0x1F1E6;

            return char.ConvertFromUtf32(a) + char.ConvertFromUtf32(b);
        }
    }
}
