using UnityEngine;

namespace SWEF.Localization
{
    /// <summary>
    /// Handles pluralization rules per language, mapping a count to the correct
    /// plural form key suffix (<c>_zero</c>, <c>_one</c>, <c>_few</c>, <c>_many</c>, <c>_other</c>).
    /// </summary>
    public static class PluralResolver
    {
        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the correct pluralized localized string for the given
        /// <paramref name="key"/> and <paramref name="count"/>.
        /// </summary>
        /// <param name="key">Base localization key (e.g. <c>"fav.page"</c>).</param>
        /// <param name="count">Numeric quantity to pluralize against.</param>
        /// <param name="lang">Active language that determines the plural rules.</param>
        /// <returns>Localized string for the resolved plural form, or the <c>_other</c> fallback.</returns>
        public static string Resolve(string key, int count, SystemLanguage lang)
        {
            string form = GetPluralForm(count, lang);
            var mgr = LocalizationManager.Instance;
            if (mgr == null) return key;

            // Try specific plural key (e.g. "fav.page_one")
            string specificKey = $"{key}_{form}";
            string resolved = mgr.GetText(specificKey);
            if (resolved != specificKey) return resolved; // found

            // Fall back to _other
            string otherKey = $"{key}_other";
            string other = mgr.GetText(otherKey);
            if (other != otherKey) return other;

            // Last resort: base key
            return mgr.GetText(key);
        }

        // ── Plural form rules ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the CLDR plural form identifier for <paramref name="count"/> in <paramref name="lang"/>.
        /// Supported forms: <c>zero</c>, <c>one</c>, <c>few</c>, <c>many</c>, <c>other</c>.
        /// </summary>
        public static string GetPluralForm(int count, SystemLanguage lang)
        {
            switch (lang)
            {
                // ── CJK — only "other" form ──────────────────────────────────────
                case SystemLanguage.Korean:
                case SystemLanguage.Japanese:
                case SystemLanguage.ChineseSimplified:
                    return "other";

                // ── English: one/other ───────────────────────────────────────────
                case SystemLanguage.English:
                    return count == 1 ? "one" : "other";

                // ── Romance languages: one/other ─────────────────────────────────
                case SystemLanguage.Spanish:
                case SystemLanguage.French:
                case SystemLanguage.Portuguese:
                    return count == 1 ? "one" : "other";

                // ── German: one/other ────────────────────────────────────────────
                case SystemLanguage.German:
                    return count == 1 ? "one" : "other";

                default:
                    return count == 1 ? "one" : "other";
            }
        }
    }
}
