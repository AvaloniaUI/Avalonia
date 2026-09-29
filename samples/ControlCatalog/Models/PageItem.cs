using System;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;

namespace ControlCatalog.Models;

public class PageItem(string header, Func<Page> factory, StreamGeometry iconData, string description, string section)
{
    public string Header { get; } = header;
    public StreamGeometry? IconData { get; } = iconData;
    public string? Description { get; } = description;
    private string SearchKey { get; } = CreateSearchKey(header, section);

    public bool IsVisible { get; set; } = true;

    public Page CreatePage() => factory();

    public bool MatchesSearch(string searchKey)
    {
        return SearchKey.Contains(searchKey, StringComparison.Ordinal);
    }

    public static string CreateSearchKey(params ReadOnlySpan<string> values)
    {
        var builder = new StringBuilder(256);

        foreach (var value in values)
        {
            var normalizedValue = value.Normalize(NormalizationForm.FormKD);

            foreach (var c in normalizedValue)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category is UnicodeCategory.NonSpacingMark or
                    UnicodeCategory.SpacingCombiningMark or
                    UnicodeCategory.EnclosingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToUpperInvariant(c));
                }
            }
        }

        return builder.ToString();
    }
}
