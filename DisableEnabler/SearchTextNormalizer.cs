using System;
using System.Globalization;
using System.Text;

namespace DisableEnabler;

internal static class SearchTextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool Contains(string? haystack, string? needle)
    {
        if (string.IsNullOrEmpty(needle))
            return true;

        return Normalize(haystack).Contains(Normalize(needle), StringComparison.OrdinalIgnoreCase);
    }
}
