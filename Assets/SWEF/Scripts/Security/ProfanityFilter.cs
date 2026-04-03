// ProfanityFilter.cs — SWEF Anti-Cheat & Security Hardening (Phase 92)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SWEF.Security
{
    /// <summary>
    /// Static utility that detects and censors profanity in player-supplied strings.
    ///
    /// <para>The word list is loaded from
    /// <c>Assets/SWEF/Resources/Security/profanity_wordlist.json</c> at first use.
    /// Additional words can be registered at runtime via <see cref="AddWord"/>.</para>
    ///
    /// <para>Basic leet-speak normalisation is applied before matching so that
    /// substitutions like <c>3→e</c>, <c>0→o</c>, <c>1→i/l</c>, etc. are detected.</para>
    /// </summary>
    public static class ProfanityFilter
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const string ResourcePath  = "Security/profanity_wordlist";
        private const string CensorChar    = "*";

        // ── Private state ─────────────────────────────────────────────────────

        private static HashSet<string> _wordList;
        private static bool            _loaded;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if <paramref name="input"/> contains a profane word
        /// (including basic leet-speak variants).
        /// </summary>
        /// <param name="input">String to check.</param>
        public static bool ContainsProfanity(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            EnsureLoaded();
            string normalised = NormaliseLeetSpeak(input.ToLowerInvariant());
            foreach (string word in _wordList)
                if (normalised.Contains(word)) return true;
            return false;
        }

        /// <summary>
        /// Replaces each profane word in <paramref name="input"/> with asterisks
        /// matching the original length.
        /// </summary>
        /// <param name="input">String to censor.</param>
        /// <returns>Censored string.</returns>
        public static string FilterProfanity(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            EnsureLoaded();

            string result     = input;
            string normalised = NormaliseLeetSpeak(input.ToLowerInvariant());

            foreach (string word in _wordList)
            {
                int idx = normalised.IndexOf(word, StringComparison.Ordinal);
                while (idx >= 0 && idx < result.Length)
                {
                    int len      = Mathf.Min(word.Length, result.Length - idx);
                    string stars = new string('*', len);
                    result    = result.Substring(0, idx) + stars +
                                (idx + len < result.Length
                                    ? result.Substring(idx + len)
                                    : string.Empty);
                    normalised = NormaliseLeetSpeak(result.ToLowerInvariant());
                    idx        = normalised.IndexOf(word, idx + len, StringComparison.Ordinal);
                }
            }

            return result;
        }

        /// <summary>
        /// Adds a word to the in-memory word list for the current session.
        /// Does not modify the JSON file on disk.
        /// </summary>
        /// <param name="word">Word to add (case-insensitive).</param>
        public static void AddWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            EnsureLoaded();
            _wordList.Add(word.ToLowerInvariant().Trim());
        }

        /// <summary>Forces a reload of the word list from Resources on the next access.</summary>
        public static void Reload()
        {
            _wordList = null;
            _loaded   = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _wordList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset != null)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<WordListWrapper>(asset.text);
                    if (wrapper?.words != null)
                        foreach (string w in wrapper.words)
                            if (!string.IsNullOrWhiteSpace(w))
                                _wordList.Add(w.ToLowerInvariant().Trim());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SWEF] Security: ProfanityFilter could not parse word list — {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[SWEF] Security: ProfanityFilter word list not found at " +
                                 $"Resources/{ResourcePath}. Operating with empty list.");
            }

            _loaded = true;
        }

        /// <summary>
        /// Applies a basic leet-speak normalisation mapping so that common
        /// character substitutions are detected.
        /// </summary>
        private static string NormaliseLeetSpeak(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            // Common substitutions: numbers and symbols mapped to letters
            return s
                .Replace("0", "o")
                .Replace("1", "i")
                .Replace("3", "e")
                .Replace("4", "a")
                .Replace("5", "s")
                .Replace("7", "t")
                .Replace("8", "b")
                .Replace("@", "a")
                .Replace("$", "s")
                .Replace("!", "i")
                .Replace("+", "t")
                .Replace("|", "i");
        }

        // ── Serialization helper ──────────────────────────────────────────────

        [Serializable]
        private class WordListWrapper
        {
            public List<string> words;
        }
    }
}
