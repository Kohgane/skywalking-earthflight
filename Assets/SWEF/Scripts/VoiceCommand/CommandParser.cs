// CommandParser.cs — SWEF Voice Command & Cockpit Voice Assistant System
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWEF.VoiceCommand
{
    /// <summary>
    /// Static utility that maps a raw spoken phrase to the best-matching
    /// <see cref="VoiceCommandDefinition"/> and extracts inline parameters.
    /// </summary>
    public static class CommandParser
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        /// <summary>Default maximum edit distance for a fuzzy match to be accepted.</summary>
        public const int DefaultFuzzyThreshold = 3;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses <paramref name="rawPhrase"/> against <paramref name="registry"/> and
        /// returns the best-matching command together with any extracted parameters.
        /// </summary>
        /// <param name="rawPhrase">The phrase spoken by the player (already lowercased).</param>
        /// <param name="registry">All registered <see cref="VoiceCommandDefinition"/>s.</param>
        /// <param name="parameters">Extracted key/value parameters (e.g. altitude → "30000").</param>
        /// <param name="fuzzyThreshold">Maximum Levenshtein distance accepted as a match.</param>
        /// <returns>Best-matching definition, or <c>null</c> if no candidate is within threshold.</returns>
        public static VoiceCommandDefinition Parse(
            string rawPhrase,
            VoiceCommandDefinition[] registry,
            out Dictionary<string, string> parameters,
            int fuzzyThreshold = DefaultFuzzyThreshold)
        {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(rawPhrase) || registry == null || registry.Length == 0)
                return null;

            string normalised = NormalisePhrase(rawPhrase);

            VoiceCommandDefinition bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var def in registry)
            {
                if (def == null) continue;

                // Try exact match against primary phrase or aliases first.
                if (MatchesExact(normalised, def, out var exactParams))
                {
                    parameters = exactParams;
                    return def;
                }

                // Fuzzy match against primary phrase.
                int dist = LevenshteinDistance(normalised, NormalisePhrase(def.primaryPhrase));
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestMatch    = def;
                }

                // Fuzzy match against each alias.
                if (def.aliases != null)
                {
                    foreach (string alias in def.aliases)
                    {
                        int aliasDist = LevenshteinDistance(normalised, NormalisePhrase(alias));
                        if (aliasDist < bestDistance)
                        {
                            bestDistance = aliasDist;
                            bestMatch    = def;
                        }
                    }
                }
            }

            if (bestDistance <= fuzzyThreshold && bestMatch != null)
            {
                // Attempt parameter extraction even on fuzzy match.
                ExtractParameters(normalised, bestMatch, out parameters);
                return bestMatch;
            }

            return null;
        }

        /// <summary>
        /// Returns up to <paramref name="maxResults"/> command suggestions whose primary phrase
        /// or aliases begin with or contain <paramref name="partial"/> (case-insensitive).
        /// Results are ranked by relevance: exact prefix match > contains > fuzzy.
        /// </summary>
        public static List<VoiceCommandDefinition> GetSuggestions(
            string partial,
            VoiceCommandDefinition[] registry,
            int maxResults = 5)
        {
            var results = new List<(VoiceCommandDefinition def, int score)>();

            if (string.IsNullOrWhiteSpace(partial) || registry == null)
                return new List<VoiceCommandDefinition>();

            string lower = partial.Trim().ToLowerInvariant();

            foreach (var def in registry)
            {
                if (def == null) continue;

                int score = ScoreForSuggestion(lower, def);
                if (score < int.MaxValue)
                    results.Add((def, score));
            }

            results.Sort((a, b) => a.score.CompareTo(b.score));

            var output = new List<VoiceCommandDefinition>(maxResults);
            for (int i = 0; i < Math.Min(maxResults, results.Count); i++)
                output.Add(results[i].def);

            return output;
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        /// <summary>Removes filler words and extra whitespace; lowercases the phrase.</summary>
        internal static string NormalisePhrase(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return string.Empty;

            string lower = phrase.Trim().ToLowerInvariant();
            // Strip common filler words so "please increase throttle" == "increase throttle"
            lower = Regex.Replace(lower, @"\b(please|hey pilot|um|uh|okay|ok)\b", string.Empty);
            lower = Regex.Replace(lower, @"\s{2,}", " ").Trim();
            return lower;
        }

        /// <summary>
        /// Checks whether <paramref name="normalised"/> exactly matches the primary phrase
        /// or any alias of <paramref name="def"/>, optionally with a trailing parameter token.
        /// </summary>
        private static bool MatchesExact(
            string normalised,
            VoiceCommandDefinition def,
            out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string primary = NormalisePhrase(def.primaryPhrase);

            if (MatchPhraseWithParams(normalised, primary, def, out parameters))
                return true;

            if (def.aliases != null)
            {
                foreach (string alias in def.aliases)
                {
                    string normAlias = NormalisePhrase(alias);
                    if (MatchPhraseWithParams(normalised, normAlias, def, out parameters))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when <paramref name="input"/> matches <paramref name="template"/>,
        /// allowing trailing numeric/word tokens to be captured as parameters.
        /// E.g. "set altitude to 30000" matches template "set altitude to" with param=30000.
        /// </summary>
        private static bool MatchPhraseWithParams(
            string input,
            string template,
            VoiceCommandDefinition def,
            out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (input == template) return true;

            // Strip placeholder hints from template (e.g. "heading [N] degrees" → "heading  degrees")
            string cleanTemplate = Regex.Replace(template, @"\[.*?\]", string.Empty);
            cleanTemplate = Regex.Replace(cleanTemplate, @"\s{2,}", " ").Trim();

            if (input == cleanTemplate) return true;

            // Check if input starts with clean template and captures trailing tokens.
            if (input.StartsWith(cleanTemplate, StringComparison.OrdinalIgnoreCase))
            {
                string trailing = input.Substring(cleanTemplate.Length).Trim();
                if (!string.IsNullOrEmpty(trailing))
                {
                    // Map first parameter hint to the trailing value.
                    string hint = (def.parameterHints != null && def.parameterHints.Length > 0)
                        ? def.parameterHints[0]
                        : "value";
                    parameters[hint] = trailing;
                }
                return true;
            }

            return false;
        }

        /// <summary>Attempts to extract parameters from a fuzzy-matched phrase.</summary>
        private static void ExtractParameters(
            string input,
            VoiceCommandDefinition def,
            out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (def.parameterHints == null || def.parameterHints.Length == 0) return;

            // Look for numeric tokens in the input.
            var numbers = Regex.Matches(input, @"\d+(\.\d+)?");
            for (int i = 0; i < numbers.Count && i < def.parameterHints.Length; i++)
                parameters[def.parameterHints[i]] = numbers[i].Value;
        }

        /// <summary>
        /// Returns a relevance score for autocomplete ranking; lower is better.
        /// <c>int.MaxValue</c> means not relevant.
        /// </summary>
        private static int ScoreForSuggestion(string partial, VoiceCommandDefinition def)
        {
            int best = int.MaxValue;

            string primary = NormalisePhrase(def.primaryPhrase);

            if (primary.StartsWith(partial, StringComparison.Ordinal)) best = Math.Min(best, 0);
            else if (primary.Contains(partial))                         best = Math.Min(best, 1);
            else
            {
                int dist = LevenshteinDistance(partial, primary);
                if (dist <= DefaultFuzzyThreshold) best = Math.Min(best, 2 + dist);
            }

            if (def.aliases != null)
            {
                foreach (string alias in def.aliases)
                {
                    string a = NormalisePhrase(alias);
                    if (a.StartsWith(partial, StringComparison.Ordinal)) best = Math.Min(best, 0);
                    else if (a.Contains(partial))                         best = Math.Min(best, 1);
                }
            }

            return best;
        }

        // ── Levenshtein Distance ──────────────────────────────────────────────────

        /// <summary>Computes the Levenshtein (edit) distance between two strings.</summary>
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int m = s.Length;
            int n = t.Length;

            int[] prev = new int[n + 1];
            int[] curr = new int[n + 1];

            for (int j = 0; j <= n; j++) prev[j] = j;

            for (int i = 1; i <= m; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= n; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                }
                Array.Copy(curr, prev, n + 1);
            }

            return prev[n];
        }
    }
}
