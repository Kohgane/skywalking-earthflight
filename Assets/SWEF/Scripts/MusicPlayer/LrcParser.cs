using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SWEF.MusicPlayer
{
    // ── Data models ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A single word token with optional karaoke start-time in an LRC line.
    /// </summary>
    [Serializable]
    public class LrcWord
    {
        /// <summary>The word text (may include surrounding punctuation).</summary>
        public string text;

        /// <summary>
        /// Start time of this word in seconds, or -1 if no word-level timing is provided.
        /// </summary>
        public float startTime;

        /// <summary>Initialises a new <see cref="LrcWord"/>.</summary>
        public LrcWord(string text, float startTime)
        {
            this.text      = text;
            this.startTime = startTime;
        }
    }

    /// <summary>
    /// A single lyric line, containing a timestamp, display text, and optional per-word timing.
    /// </summary>
    [Serializable]
    public class LrcLine
    {
        /// <summary>Line start time in seconds (after offset adjustment).</summary>
        public float timestamp;

        /// <summary>Plain display text of the lyric line.</summary>
        public string text;

        /// <summary>
        /// Per-word timing tokens, populated from extended LRC <c>&lt;mm:ss.xx&gt;</c> tags.
        /// Empty when no word-level timing is present in the source file.
        /// </summary>
        public List<LrcWord> words = new List<LrcWord>();

        /// <summary>Returns <c>true</c> when this line carries word-level timing data.</summary>
        public bool HasWordTiming => words != null && words.Count > 0;
    }

    /// <summary>
    /// Parsed result of an LRC lyrics file.
    /// Contains metadata extracted from the header tags and an ordered list of lyric lines.
    /// </summary>
    [Serializable]
    public class LrcData
    {
        /// <summary>Track title extracted from the <c>[ti:]</c> tag, or empty.</summary>
        public string title = string.Empty;

        /// <summary>Artist name extracted from the <c>[ar:]</c> tag, or empty.</summary>
        public string artist = string.Empty;

        /// <summary>Album name extracted from the <c>[al:]</c> tag, or empty.</summary>
        public string album = string.Empty;

        /// <summary>
        /// Global timing offset in milliseconds extracted from the <c>[offset:]</c> tag.
        /// Positive values shift lyrics later; negative values shift them earlier.
        /// </summary>
        public int offsetMs;

        /// <summary>All lyric lines in ascending timestamp order.</summary>
        public List<LrcLine> lines = new List<LrcLine>();

        /// <summary>Returns <c>true</c> when at least one lyric line is available.</summary>
        public bool HasLyrics => lines != null && lines.Count > 0;
    }

    // ── Parser ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pure static parser for the LRC lyrics format.
    /// <para>
    /// Supports:
    /// <list type="bullet">
    ///   <item>Standard line timestamps: <c>[mm:ss.xx]</c> and <c>[mm:ss:xx]</c></item>
    ///   <item>Extended word-level timestamps: <c>&lt;mm:ss.xx&gt;</c></item>
    ///   <item>Metadata tags: <c>[ti:]</c>, <c>[ar:]</c>, <c>[al:]</c>, <c>[offset:]</c></item>
    ///   <item>Multi-timestamp lines: <c>[00:01.00][00:15.00]Same lyric</c></item>
    ///   <item>BOM stripping and defensive handling of malformed input</item>
    /// </list>
    /// </para>
    /// <para>All methods are static and allocation-friendly — suitable for unit testing.</para>
    /// </summary>
    public static class LrcParser
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const float SecondsPerMinute = 60f;

        /// <summary>
        /// Matches a single <c>[mm:ss.xx]</c> or <c>[mm:ss:xx]</c> timestamp tag.
        /// Group 1 = minutes, Group 2 = seconds, Group 3 = hundredths (optional).
        /// </summary>
        private static readonly Regex TimestampRegex =
            new Regex(@"\[(\d{1,3}):(\d{2})[.:](\d{1,3})?\]", RegexOptions.Compiled);

        /// <summary>
        /// Matches a metadata tag such as <c>[ti:Some Title]</c>.
        /// Group 1 = key, Group 2 = value.
        /// </summary>
        private static readonly Regex MetaTagRegex =
            new Regex(@"^\[([a-zA-Z]+):(.*)?\]$", RegexOptions.Compiled);

        /// <summary>
        /// Matches a word-level extended LRC tag <c>&lt;mm:ss.xx&gt;</c>.
        /// Group 1 = minutes, Group 2 = seconds, Group 3 = hundredths (optional).
        /// </summary>
        private static readonly Regex WordTimestampRegex =
            new Regex(@"<(\d{1,3}):(\d{2})[.:](\d{1,3})?>", RegexOptions.Compiled);

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses an LRC-formatted string and returns a populated <see cref="LrcData"/> instance.
        /// </summary>
        /// <param name="lrcContent">Raw LRC file content (any encoding, BOM is stripped).</param>
        /// <returns>
        /// A valid <see cref="LrcData"/>. Lines are sorted by timestamp.
        /// Returns an empty <see cref="LrcData"/> (no lines) on <c>null</c> or empty input.
        /// </returns>
        public static LrcData Parse(string lrcContent)
        {
            var data = new LrcData();

            if (string.IsNullOrEmpty(lrcContent))
                return data;

            // Strip BOM
            lrcContent = StripBom(lrcContent);

            string[] rawLines = lrcContent.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

            // First pass — collect timestamped lines and metadata
            var pendingLines = new List<LrcLine>();

            foreach (string rawLine in rawLines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // Try metadata first (single tag, no digits in key, no text after closing bracket)
                if (TryParseMetaTag(line, data))
                    continue;

                // Try lyric line (may contain multiple timestamps)
                ParseLyricLine(line, pendingLines);
            }

            // Apply global offset
            float offsetSeconds = data.offsetMs / 1000f;
            foreach (LrcLine ll in pendingLines)
            {
                ll.timestamp = Mathf.Max(0f, ll.timestamp + offsetSeconds);
            }

            // Sort by timestamp
            pendingLines.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            data.lines = pendingLines;
            return data;
        }

        /// <summary>
        /// Parses LRC content from raw UTF-8 bytes, handling encoding detection.
        /// </summary>
        /// <param name="bytes">Raw file bytes.</param>
        /// <returns>Parsed <see cref="LrcData"/>.</returns>
        public static LrcData ParseBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return new LrcData();

            // Detect and strip BOM, fall back to UTF-8
            string content = DetectAndDecode(bytes);
            return Parse(content);
        }

        /// <summary>
        /// Finds the index of the active lyric line for a given playback position.
        /// </summary>
        /// <param name="data">Parsed LRC data.</param>
        /// <param name="playbackSeconds">Current playback position in seconds.</param>
        /// <returns>
        /// The index of the active line (0-based), or -1 when before the first line.
        /// </returns>
        public static int FindLineIndex(LrcData data, float playbackSeconds)
        {
            if (data == null || data.lines == null || data.lines.Count == 0)
                return -1;

            int result = -1;
            for (int i = 0; i < data.lines.Count; i++)
            {
                if (data.lines[i].timestamp <= playbackSeconds)
                    result = i;
                else
                    break;
            }
            return result;
        }

        /// <summary>
        /// Returns the active word index within a line for a given playback position.
        /// </summary>
        /// <param name="line">The lyric line containing word-timing data.</param>
        /// <param name="playbackSeconds">Current playback position in seconds.</param>
        /// <returns>Active word index, or -1 when before the first word or no words present.</returns>
        public static int FindWordIndex(LrcLine line, float playbackSeconds)
        {
            if (line == null || !line.HasWordTiming)
                return -1;

            int result = -1;
            for (int i = 0; i < line.words.Count; i++)
            {
                if (line.words[i].startTime >= 0f && line.words[i].startTime <= playbackSeconds)
                    result = i;
                else if (line.words[i].startTime > playbackSeconds)
                    break;
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static bool TryParseMetaTag(string line, LrcData data)
        {
            Match m = MetaTagRegex.Match(line);
            if (!m.Success)
                return false;

            string key   = m.Groups[1].Value.ToLowerInvariant().Trim();
            string value = m.Groups[2].Value.Trim();

            switch (key)
            {
                case "ti":
                    data.title = value;
                    return true;
                case "ar":
                    data.artist = value;
                    return true;
                case "al":
                    data.album = value;
                    return true;
                case "offset":
                    if (int.TryParse(value, out int ms))
                        data.offsetMs = ms;
                    return true;
                default:
                    // Known but unused metadata tags (length, by, re, ve…)
                    // Return true only if the entire line is a single metadata tag with no
                    // remaining timestamp content.
                    return true;
            }
        }

        private static void ParseLyricLine(string line, List<LrcLine> output)
        {
            // Collect all [mm:ss.xx] timestamps from the beginning of the line
            var timestamps = new List<float>();
            int cursor     = 0;

            while (cursor < line.Length && line[cursor] == '[')
            {
                Match m = TimestampRegex.Match(line, cursor);
                if (!m.Success || m.Index != cursor)
                    break;

                float ts = ParseTimestampMatch(m);
                if (ts >= 0f)
                    timestamps.Add(ts);

                cursor = m.Index + m.Length;
            }

            if (timestamps.Count == 0)
                return; // no valid timestamps on this line

            // Remaining text after all leading [mm:ss.xx] tags is the lyric text
            string lyricText = line.Substring(cursor);

            // Parse word-level timing from the lyric text
            List<LrcWord> words = ParseWordTiming(lyricText);

            // Strip word-timing tags from the display text
            string displayText = WordTimestampRegex.Replace(lyricText, string.Empty).Trim();

            foreach (float ts in timestamps)
            {
                var lrcLine = new LrcLine
                {
                    timestamp = ts,
                    text      = displayText
                };

                if (words != null && words.Count > 0)
                    lrcLine.words = new List<LrcWord>(words);

                output.Add(lrcLine);
            }
        }

        private static float ParseTimestampMatch(Match m)
        {
            try
            {
                int   minutes     = int.Parse(m.Groups[1].Value);
                int   seconds     = int.Parse(m.Groups[2].Value);
                float hundredths  = 0f;

                if (m.Groups[3].Success && !string.IsNullOrEmpty(m.Groups[3].Value))
                {
                    string raw = m.Groups[3].Value;
                    float  frac = float.Parse(raw) / Mathf.Pow(10f, raw.Length);
                    hundredths = frac;
                }

                return minutes * SecondsPerMinute + seconds + hundredths;
            }
            catch
            {
                return -1f;
            }
        }

        private static List<LrcWord> ParseWordTiming(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            MatchCollection matches = WordTimestampRegex.Matches(text);
            if (matches.Count == 0)
                return null;

            var words = new List<LrcWord>();

            for (int i = 0; i < matches.Count; i++)
            {
                Match  m         = matches[i];
                float  startTime = ParseWordTimestampMatch(m);
                int    textStart = m.Index + m.Length;
                int    textEnd   = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;

                string wordText  = text.Substring(textStart, textEnd - textStart).Trim();

                if (!string.IsNullOrEmpty(wordText))
                    words.Add(new LrcWord(wordText, startTime));
            }

            return words;
        }

        private static float ParseWordTimestampMatch(Match m)
        {
            try
            {
                int   minutes    = int.Parse(m.Groups[1].Value);
                int   seconds    = int.Parse(m.Groups[2].Value);
                float hundredths = 0f;

                if (m.Groups[3].Success && !string.IsNullOrEmpty(m.Groups[3].Value))
                {
                    string raw = m.Groups[3].Value;
                    float  frac = float.Parse(raw) / Mathf.Pow(10f, raw.Length);
                    hundredths = frac;
                }

                return minutes * SecondsPerMinute + seconds + hundredths;
            }
            catch
            {
                return -1f;
            }
        }

        private static string StripBom(string text)
        {
            if (text.Length > 0 && text[0] == '\uFEFF')
                return text.Substring(1);
            return text;
        }

        private static string DetectAndDecode(byte[] bytes)
        {
            // UTF-8 BOM: EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            // UTF-16 LE BOM: FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            // UTF-16 BE BOM: FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            // Default: UTF-8 (no BOM)
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
