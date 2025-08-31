using JonahTEC.Audio.Transcription.Configurations;
using JonahTEC.Audio.Transcription.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JonahTEC.Audio.Transcription.Services
{
    /// <summary>
    /// Provides functionality for parsing JSON files to extract word hits based on specified criteria.
    /// </summary>
    /// <remarks>The <see cref="ParsingService"/> class is designed to process JSON files and identify word
    /// hits using a configurable target phrase and matching criteria. It supports parsing structured JSON with
    /// transcription data or performing a best-effort string search within unstructured JSON content.</remarks>
    public partial class ParsingService
    {
        private readonly BackendConfiguration _backendConfiguration;
        private readonly string _normalizedPhrase;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingService"/> class with the specified backend
        /// configuration.
        /// </summary>
        /// <remarks>The provided <paramref name="backendConfiguration"/> is used to configure the
        /// service, including normalizing the target phrase for search operations.</remarks>
        /// <param name="backendConfiguration">The configuration settings used to initialize the parsing service. Cannot be null.</param>
        public ParsingService(BackendConfiguration backendConfiguration)
        {
            _backendConfiguration = backendConfiguration;
            _normalizedPhrase = NormalizeForSearch(_backendConfiguration.TargetPhrase);
        }

        /// <summary>
        /// Parses a JSON file to extract word hits based on the specified criteria.
        /// </summary>
        /// <remarks>The method attempts to parse the JSON file to identify word hits. If the JSON
        /// contains a  "transcription" field, it processes the transcription array. Otherwise, it performs a 
        /// best-effort string search within the JSON content to detect matches based on the configured  phrase and
        /// matching criteria.</remarks>
        /// <param name="jsonPath">The file path to the JSON file to be parsed.</param>
        /// <param name="audioPath">The file path to the associated Audio file, used to populate the result metadata.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
        /// <returns>A list of <see cref="WordHit"/> objects representing the extracted word hits.  Returns an empty list if no
        /// matches are found.</returns>
        public async Task<List<WordHit>> ParseFromJsonAsync(string jsonPath, string audioPath, CancellationToken ct)
        {
            var json = await File.ReadAllTextAsync(jsonPath, ct);

            if (json.Contains("\"transcription\"", StringComparison.OrdinalIgnoreCase))
            {
                return ParseTranscriptionArray(json, audioPath);
            }
            else
            {
                // Unknown JSON shape; try best-effort string search
                if (SoftContainsPhrase(json,
                                       _normalizedPhrase,
                                       _backendConfiguration.MaxTokenDistance,
                                       _backendConfiguration.AllowSubstring))
                {
                    return
                    [
                        new()
                        {
                            FilePath = audioPath,
                            Word     = _normalizedPhrase,
                            StartSec = 0,
                            EndSec   = 0,
                            Context  = "(match found in JSON blob)"
                        }
                    ];
                }
                return [];
            }
        }

        /// <summary>
        /// Parses a JSON transcription array and identifies occurrences of a specific phrase within the transcription.
        /// </summary>
        /// <remarks>This method processes the transcription data to find matches for a predefined phrase,
        /// considering configurable parameters such as token distance and substring allowance. It skips overlapping
        /// matches to avoid duplicate results.</remarks>
        /// <param name="json">The JSON string representing the transcription data.</param>
        /// <param name="wavPath">The file path of the associated audio file.</param>
        /// <returns>A list of <see cref="WordHit"/> objects, each representing an occurrence of the specified phrase, including
        /// its context, start time, and end time within the audio file.</returns>
        private List<WordHit> ParseTranscriptionArray(string json, string wavPath)
        {
            var doc = JsonSerializer.Deserialize<WhisperTranscriptionJson>(json, _jsonOptions) ?? new WhisperTranscriptionJson();

            var items = doc.Transcription
                           .Where(i => !string.IsNullOrWhiteSpace(i.Text))
                           .ToList();

            var hits = new List<WordHit>();
            for (int i = 0; i < items.Count; i++)
            {
                var sb = new StringBuilder();
                double start = -1, end = -1;

                for (int j = i; j < Math.Min(items.Count, i + _backendConfiguration.WindowSize); j++)
                {
                    var it = items[j];
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(it.Text);

                    if (start < 0) start = ParseTimeFlexible(it.Timestamps?.From ?? "");
                    end = ParseTimeFlexible(it.Timestamps?.To ?? "");

                    var combined = sb.ToString();
                    if (SoftContainsPhrase(combined,
                                           _normalizedPhrase,
                                           _backendConfiguration.MaxTokenDistance,
                                           _backendConfiguration.AllowSubstring))
                    {
                        hits.Add(new WordHit
                        {
                            FilePath = wavPath,
                            Word = _normalizedPhrase,
                            StartSec = Math.Max(start, 0),
                            EndSec = Math.Max(end, Math.Max(start, 0)),
                            Context = combined
                        });

                        // Skip ahead to avoid duplicates over the same span
                        i = j;
                        break;
                    }
                }
            }

            return hits;
        }

        /// <summary>
        /// Normalizes the specified string for search purposes by removing diacritics, converting to lowercase,  and
        /// replacing certain characters with spaces.
        /// </summary>
        /// <remarks>This method performs the following transformations: <list type="bullet">
        /// <item>Converts the string to lowercase using invariant culture.</item> <item>Removes diacritic marks (e.g.,
        /// accents) from characters.</item> <item>Replaces dashes, underscores, and whitespace characters with a single
        /// space.</item> <item>Retains only letters, digits, and apostrophes, ignoring other punctuation.</item>
        /// <item>Collapses multiple spaces into a single space and trims leading/trailing spaces.</item> </list> This
        /// method is useful for preparing strings for case-insensitive and diacritic-insensitive search
        /// operations.</remarks>
        /// <param name="s">The input string to normalize. Can be null, empty, or whitespace.</param>
        /// <returns>A normalized string suitable for search operations. Returns an empty string if the input is null, empty, or
        /// whitespace.</returns>
        private static string NormalizeForSearch(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var lower = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(lower.Length);

            foreach (var ch in lower)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue; // remove diacritics

                if (ch == '-' || ch == '_' || char.IsWhiteSpace(ch))
                {
                    sb.Append(' ');
                }
                else if (char.IsLetterOrDigit(ch) || ch == '\'')
                {
                    sb.Append(ch);
                }
                // ignore other punctuation
            }

            return NormalizedRegex().Replace(sb.ToString(), " ").Trim();
        }

        /// <summary>
        /// Reduces a given token to its base form by removing possessive suffixes or trailing 's'.
        /// </summary>
        /// <param name="token">The input string to be stemmed. Must not be null.</param>
        /// <returns>A string representing the stemmed version of the input token.  If the token ends with "'s", the possessive
        /// suffix is removed.  If the token ends with 's' and is longer than three characters, the trailing 's' is
        /// removed.  Otherwise, the original token is returned unchanged.</returns>
        private static string StemLite(string token)
        {
            if (token.EndsWith("'s")) return token[..^2];
            if (token.Length > 3 && token.EndsWith('s')) return token[..^1];
            return token;
        }

        /// <summary>
        /// Compares two strings for similarity, allowing for minor differences based on stemming and a maximum edit
        /// distance.
        /// </summary>
        /// <remarks>This method first applies a lightweight stemming process to both strings before
        /// comparing them.  If the strings are identical after stemming, the method returns <see langword="true"/>. 
        /// Otherwise, it calculates the Levenshtein distance between the stemmed strings and compares it to <paramref
        /// name="maxDist"/>.</remarks>
        /// <param name="a">The first string to compare. Cannot be <see langword="null"/>.</param>
        /// <param name="b">The second string to compare. Cannot be <see langword="null"/>.</param>
        /// <param name="maxDist">The maximum allowable Levenshtein edit distance for the strings to be considered similar. Must be
        /// non-negative.</param>
        /// <returns><see langword="true"/> if the strings are considered similar based on stemming and the specified maximum
        /// edit distance; otherwise, <see langword="false"/>.</returns>
        private static bool SoftTokenEquals(string a, string b, int maxDist)
        {
            if (a == b) return true;

            a = StemLite(a);
            b = StemLite(b);
            if (a == b) return true;

            if (maxDist <= 0) return false;
            return LevenshteinDistance(a, b) <= maxDist;
        }

        /// <summary>
        /// Determines whether a normalized version of the specified haystack string contains the given phrase, either
        /// as a substring or as a sequence of tokens within a specified token distance.
        /// </summary>
        /// <remarks>This method performs a "soft" comparison, allowing for token-level differences within
        /// the specified distance. It is useful for approximate or fuzzy matching of phrases within a larger
        /// text.</remarks>
        /// <param name="haystackRaw">The raw input string to search within. This string will be normalized for comparison.</param>
        /// <param name="phraseNorm">The normalized phrase to search for. This should already be in a normalized form.</param>
        /// <param name="maxTokenDistance">The maximum allowable token distance for a match. Tokens are considered equal if their differences are
        /// within this distance.</param>
        /// <param name="allowSubstring">A value indicating whether to allow substring matches. If <see langword="true"/>, the method will return
        /// <see langword="true"/> if the normalized haystack contains the normalized phrase as a substring.</param>
        /// <returns><see langword="true"/> if the normalized haystack contains the phrase as a substring (when <paramref
        /// name="allowSubstring"/> is <see langword="true"/>) or as a sequence of tokens within the specified token
        /// distance; otherwise, <see langword="false"/>.</returns>
        private static bool SoftContainsPhrase(string haystackRaw, string phraseNorm, int maxTokenDistance, bool allowSubstring)
        {
            var hs = NormalizeForSearch(haystackRaw);
            if (allowSubstring && hs.Contains(phraseNorm)) return true;

            var hTokens = hs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var nTokens = phraseNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nTokens.Length == 0 || hTokens.Length < nTokens.Length) return false;

            for (int i = 0; i <= hTokens.Length - nTokens.Length; i++)
            {
                bool ok = true;
                for (int j = 0; j < nTokens.Length; j++)
                {
                    var a = hTokens[i + j];
                    var b = nTokens[j];
                    int dist = b.Length <= 2 ? 0 : maxTokenDistance;
                    if (!SoftTokenEquals(a, b, dist)) { ok = false; break; }
                }
                if (ok) return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <remarks>The Levenshtein distance is a measure of the similarity between two strings. A
        /// smaller  distance indicates greater similarity. This method performs a case-sensitive comparison.</remarks>
        /// <param name="s">The first string to compare. Cannot be <see langword="null"/>.</param>
        /// <param name="t">The second string to compare. Cannot be <see langword="null"/>.</param>
        /// <returns>The minimum number of single-character edits (insertions, deletions, or substitutions)  required to
        /// transform one string into the other.</returns>
        private static int LevenshteinDistance(string s, string t)
        {
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;

            var v0 = new int[t.Length + 1];
            var v1 = new int[t.Length + 1];

            for (int i = 0; i <= t.Length; i++) v0[i] = i;

            for (int i = 0; i < s.Length; i++)
            {
                v1[0] = i + 1;
                for (int j = 0; j < t.Length; j++)
                {
                    var cost = s[i] == t[j] ? 0 : 1;
                    v1[j + 1] = Math.Min(
                        Math.Min(v1[j] + 1, v0[j + 1] + 1),
                        v0[j] + cost
                    );
                }
                Array.Copy(v1, v0, v0.Length);
            }
            return v1[t.Length];
        }

        /// <summary>
        /// Parses a time string in various formats and converts it to a total number of seconds.
        /// </summary>
        /// <param name="t">The time string to parse. The string can be in the format "hh:mm:ss.fff" or any format supported by <see
        /// cref="TimeSpan.TryParse"/>. If the string is null, empty, or whitespace, the method returns 0.0.</param>
        /// <returns>The total number of seconds represented by the parsed time string. Returns 0.0 if the input string is
        /// invalid or cannot be parsed.</returns>
        private static double ParseTimeFlexible(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return 0.0;

            var s = t.Trim().Replace(',', '.');

            if (TimeSpan.TryParseExact(s, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var ts))
                return ts.TotalSeconds;
            if (TimeSpan.TryParse(s, out ts))
                return ts.TotalSeconds;

            return 0.0;
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex NormalizedRegex();
    }
}