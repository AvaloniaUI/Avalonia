using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using ControlCatalog.Controls;
using MiniMvvm;

namespace ControlCatalog.Models;

public class PageItem(string header, Func<Page> factory, StreamGeometry iconData, string description, HomeSection? section, IReadOnlyList<SampleInfo>? samples = null) : ViewModelBase
{
    public string Header { get; } = header;
    public StreamGeometry? IconData { get; } = iconData;
    public string? Description { get; } = description;
    public string Section { get; } = section?.Title ?? "";

    /// <summary>
    /// The samples a gallery page offers. The registry lives statically on the page class and is passed
    /// here at registration, so search can match the page by its samples without constructing it.
    /// </summary>
    public IReadOnlyList<SampleInfo>? Samples { get; } = samples;

    /// <summary>
    /// Whether the drawer lists this page's samples. Only gallery pages expand.
    /// </summary>
    public bool HasSamples => Samples is { Count: > 0 };

    /// <summary>
    /// True while this page is the one being shown. The drawer marks the page with it.
    /// </summary>
    public bool IsCurrent
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Whether this page's samples are listed in the drawer.
    /// </summary>
    public bool IsExpanded
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    }

    private string SearchKey { get; } = BuildSearchKey(header, description, section?.Title, samples);

    public bool IsVisible
    {
        get;
        set
        {
            RaiseAndSetIfChanged(ref field, value);
            section?.RaiseSectionVisibilityChanged();
        }
    } = true;

    public Page CreatePage() => factory();

    public bool MatchesSearch(string searchKey)
    {
        return SearchKey.Contains(searchKey, StringComparison.Ordinal);
    }

    private static string BuildSearchKey(string header, string? description, string? section, IReadOnlyList<SampleInfo>? samples)
    {
        if (samples is not { Count: > 0 })
        {
            return CreateSearchKey(header, description ?? "", section ?? "");
        }

        var parts = new string[3 + samples.Count * 2];
        parts[0] = header;
        parts[1] = description ?? "";
        parts[2] = section ?? "";
        for (var i = 0; i < samples.Count; i++)
        {
            parts[3 + i * 2] = samples[i].Title;
            parts[4 + i * 2] = samples[i].Description;
        }

        return CreateSearchKey(parts);
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
