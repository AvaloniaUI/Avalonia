using System;
using System.Globalization;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Resolves a BCP-47 culture identifier to the ISO 15924 script subtag that best represents
    /// the writing system the user is requesting. Used by the font fallback algorithm to refine
    /// ambiguous Unicode scripts (for example <see cref="TextFormatting.Unicode.Script.Han"/>)
    /// into the regional variant the candidate font should advertise.
    /// </summary>
    /// <remarks>
    /// The mapping follows the well-formed subset of
    /// <see href="https://www.unicode.org/cldr/charts/latest/supplemental/likely_subtags.html">
    /// CLDR likely subtags
    /// </see>: explicit script subtags pass through, common region/language pairs are mapped to
    /// their canonical script, and unrecognised inputs return <c>null</c>.
    /// </remarks>
    internal static class Bcp47ScriptResolver
    {
        /// <summary>
        /// Returns the ISO 15924 script subtag (e.g. <c>"Jpan"</c>, <c>"Hans"</c>, <c>"Latn"</c>)
        /// implied by the supplied culture, or <c>null</c> when no script can be inferred.
        /// </summary>
        public static string? GetScriptSubtag(CultureInfo? culture)
        {
            if (culture is null || culture == CultureInfo.InvariantCulture)
            {
                return null;
            }

            var name = culture.Name;

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // 1. Explicit script subtag wins (BCP-47 second subtag, four letters).
            if (TryExtractScriptSubtag(name, out var explicitScript))
            {
                return explicitScript;
            }

            var span = name.AsSpan();
            var languageEnd = span.IndexOfAny('-', '_');
            var language = languageEnd < 0 ? span : span.Slice(0, languageEnd);
            var rest = languageEnd < 0 ? ReadOnlySpan<char>.Empty : span.Slice(languageEnd + 1);

            // 2. Japanese (Han + Hiragana + Katakana). The Jpan supercode picks Japanese fonts.
            if (Eq(language, "ja"))
            {
                return "Jpan";
            }

            // 3. Korean (Hangul + Han). Kore picks Korean fonts.
            if (Eq(language, "ko"))
            {
                return "Kore";
            }

            // 4. Chinese — Hans vs Hant disambiguation. Default to Hans when only "zh" is supplied.
            if (Eq(language, "zh"))
            {
                if (IsTraditionalChineseRegion(rest))
                {
                    return "Hant";
                }

                return "Hans";
            }

            // 5. Other well-known language → script mappings that disambiguate Unicode scripts.
            if (Eq(language, "ru") || Eq(language, "uk") || Eq(language, "bg") ||
                Eq(language, "be") || Eq(language, "sr") || Eq(language, "mk"))
            {
                return "Cyrl";
            }

            if (Eq(language, "en") || Eq(language, "de") || Eq(language, "fr") ||
                Eq(language, "es") || Eq(language, "it") || Eq(language, "pt") ||
                Eq(language, "nl") || Eq(language, "sv") || Eq(language, "no") ||
                Eq(language, "da") || Eq(language, "fi") || Eq(language, "pl") ||
                Eq(language, "cs") || Eq(language, "tr"))
            {
                return "Latn";
            }

            if (Eq(language, "ar") || Eq(language, "fa") || Eq(language, "ur"))
            {
                return "Arab";
            }

            if (Eq(language, "he") || Eq(language, "yi"))
            {
                return "Hebr";
            }

            if (Eq(language, "th"))
            {
                return "Thai";
            }

            if (Eq(language, "el"))
            {
                return "Grek";
            }

            return null;
        }

        private static bool TryExtractScriptSubtag(string name, out string? script)
        {
            // Look for a four-letter ISO 15924 script subtag, e.g. "zh-Hans-CN" or "sr-Cyrl".
            var span = name.AsSpan();
            var start = span.IndexOfAny('-', '_');

            while (start >= 0 && start + 1 < span.Length)
            {
                var subtagStart = start + 1;
                var nextSeparator = span.Slice(subtagStart).IndexOfAny('-', '_');
                var subtagLength = nextSeparator < 0 ? span.Length - subtagStart : nextSeparator;

                if (subtagLength == 4 && IsAllAsciiLetters(span.Slice(subtagStart, 4)))
                {
                    // Normalise to title case (Latn, Hans, Cyrl, ...).
                    Span<char> buffer = stackalloc char[4];
                    buffer[0] = char.ToUpperInvariant(span[subtagStart]);
                    buffer[1] = char.ToLowerInvariant(span[subtagStart + 1]);
                    buffer[2] = char.ToLowerInvariant(span[subtagStart + 2]);
                    buffer[3] = char.ToLowerInvariant(span[subtagStart + 3]);
                    script = new string(buffer);
                    return true;
                }

                if (nextSeparator < 0)
                {
                    break;
                }

                start = subtagStart + nextSeparator;
            }

            script = null;
            return false;
        }

        private static bool IsAllAsciiLetters(ReadOnlySpan<char> span)
        {
            foreach (var c in span)
            {
                if (!char.IsAsciiLetter(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTraditionalChineseRegion(ReadOnlySpan<char> rest)
        {
            // Iterate region/script subtags following "zh-".
            while (!rest.IsEmpty)
            {
                var sep = rest.IndexOfAny('-', '_');
                var subtag = sep < 0 ? rest : rest.Slice(0, sep);

                if (Eq(subtag, "Hant"))
                {
                    return true;
                }

                if (Eq(subtag, "Hans"))
                {
                    return false;
                }

                if (Eq(subtag, "TW") || Eq(subtag, "HK") || Eq(subtag, "MO"))
                {
                    return true;
                }

                if (Eq(subtag, "CN") || Eq(subtag, "SG") || Eq(subtag, "MY"))
                {
                    return false;
                }

                if (sep < 0)
                {
                    break;
                }

                rest = rest.Slice(sep + 1);
            }

            return false;
        }

        private static bool Eq(ReadOnlySpan<char> a, string b) =>
            a.Equals(b.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}
