using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class PlayerNameFilter
{
    private static PlayerNameFilterSettings _settings;
    public const string DefaultName = "player";

    public static PlayerNameFilterSettings Settings
    {
        get
        {
            if (_settings == null) _settings = Resources.Load<PlayerNameFilterSettings>("PlayerNameFilter");
            return _settings;
        }
    }

    public static bool IsAllowed(string name)
    {
        return !ContainsBlockedWord(name, Settings);
    }

    public static string DisplayName(string name)
    {
        return string.IsNullOrWhiteSpace(name) || !IsAllowed(name) ? DefaultName : name.Trim();
    }

    public static bool ContainsBlockedWord(string name, PlayerNameFilterSettings settings)
    {
        if (string.IsNullOrWhiteSpace(name) || settings == null || settings.blockedWords == null) return false;
        string normalized = Normalize(name);
        foreach (string word in settings.blockedWords)
        {
            if (string.IsNullOrWhiteSpace(word)) continue;
            string forbidden = Normalize(word);
            if (forbidden.Length == 0) continue;
            if (!settings.matchInsideEnglishWords && IsEnglishLetter(forbidden[0]))
            {
                var pattern = new StringBuilder("(?<![a-z])");
                for (int i = 0; i < forbidden.Length; i++)
                {
                    if (i > 0) pattern.Append(@"[\s\p{P}\p{S}]*");
                    pattern.Append(Regex.Escape(forbidden[i].ToString()));
                }
                pattern.Append("(?![a-z])");
                if (Regex.IsMatch(Normalize(name, false), pattern.ToString(), RegexOptions.CultureInvariant)) return true;
                continue;
            }
            int start = 0;
            while (start < normalized.Length)
            {
                int match = normalized.IndexOf(forbidden, start, StringComparison.Ordinal);
                if (match < 0) break;
                int end = match + forbidden.Length;
                if (settings.matchInsideEnglishWords || !IsEnglishLetter(forbidden[0]) ||
                    ((match == 0 || !IsEnglishLetter(normalized[match - 1])) &&
                    (end == normalized.Length || !IsEnglishLetter(normalized[end])))) return true;
                start = match + 1;
            }
        }
        return false;
    }

    private static bool IsEnglishLetter(char c) => c >= 'a' && c <= 'z';

    private static string Normalize(string value, bool removeSeparators = true)
    {
        string normalized;
        try { normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant(); }
        catch (ArgumentException) { normalized = value.ToLowerInvariant(); }
        var result = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            UnicodeCategory category = char.GetUnicodeCategory(c);
            if ((removeSeparators && (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c))) ||
                category == UnicodeCategory.Format || category == UnicodeCategory.Control ||
                category == UnicodeCategory.NonSpacingMark) continue;
            result.Append(c);
        }
        return result.ToString();
    }
}
