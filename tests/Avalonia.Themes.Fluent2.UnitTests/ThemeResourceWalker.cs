using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent2.UnitTests;

/// <summary>
/// Walks a theme's style/resource tree collecting every resource key, so the
/// live Avalonia.Themes.Fluent (v1) instance can serve as the compatibility
/// baseline for Fluent2Theme. C#-provided keys (accent colors) are not
/// discoverable by walking and are listed explicitly.
/// </summary>
internal static class ThemeResourceWalker
{
    public static readonly string[] AccentKeys =
    {
        "SystemAccentColor",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
    };

    public static HashSet<object> CollectKeys(IStyle style)
    {
        var keys = new HashSet<object>();
        Walk(style, keys);
        foreach (var key in AccentKeys)
            keys.Add(key);
        return keys;
    }

    private static void Walk(IStyle style, HashSet<object> keys)
    {
        if (style is StyleBase styleBase)
            Walk(styleBase.Resources, keys);
        if (style is Styles styles)
            Walk(styles.Resources, keys);

        foreach (var child in style.Children)
            Walk(child, keys);
    }

    private static void Walk(IResourceDictionary resources, HashSet<object> keys)
    {
        foreach (var key in resources.Keys)
        {
            // The keyed CompactStyles ResourceInclude is an implementation detail
            // handled by the DensityStyle tests; its inner keys are density overrides.
            if (key is string s && s == "CompactStyles")
                continue;
            keys.Add(key);
        }

        foreach (var merged in resources.MergedDictionaries)
            WalkProvider(merged, keys);

        foreach (var themed in resources.ThemeDictionaries.Values)
            WalkProvider(themed, keys);
    }

    private static void WalkProvider(IResourceProvider provider, HashSet<object> keys)
    {
        switch (provider)
        {
            case ResourceDictionary dictionary:
                Walk(dictionary, keys);
                break;
            case ResourceInclude include:
                Walk(include.Loaded, keys);
                break;
            // C# ResourceProviders (SystemAccentColors, palette collections) are
            // not enumerable; their keys are covered by AccentKeys above.
        }
    }
}
