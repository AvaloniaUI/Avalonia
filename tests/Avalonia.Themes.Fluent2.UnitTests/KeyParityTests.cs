using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent2.UnitTests;

/// <summary>
/// The drop-in compatibility contract: every resource key resolvable from
/// Avalonia.Themes.Fluent (v1) must resolve from Fluent2Theme, in both theme
/// variants, with a compatible runtime type. Values are allowed to differ —
/// they are the redesign.
/// </summary>
public class KeyParityTests
{
    private static readonly ThemeVariant[] s_variants = { ThemeVariant.Light, ThemeVariant.Dark };

    [AvaloniaFact]
    public void Every_v1_resource_key_resolves_in_fluent2_with_compatible_type()
    {
        var v1 = new Avalonia.Themes.Fluent.FluentTheme();
        var v2 = new Fluent2Theme();
        var keys = ThemeResourceWalker.CollectKeys(v1);

        // Guard against a silent enumeration bug: v1 has well over a thousand keys.
        Assert.True(keys.Count > 1000, $"Only enumerated {keys.Count} v1 keys; walker is broken.");

        var problems = new List<string>();

        foreach (var key in keys)
        {
            foreach (var variant in s_variants)
            {
                object? expected = null;
                try
                {
                    if (!((IResourceNode)v1).TryGetResource(key, variant, out expected))
                        continue; // not resolvable in v1 for this variant — nothing to guarantee
                }
                catch (Exception e)
                {
                    problems.Add($"[v1!] {key} ({variant}): {e.Message}");
                    continue;
                }

                object? actual = null;
                try
                {
                    if (!((IResourceNode)v2).TryGetResource(key, variant, out actual))
                    {
                        problems.Add($"[missing] {key} ({variant})");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    problems.Add($"[throws] {key} ({variant}): {e.Message}");
                    continue;
                }

                var expectedCategory = Category(expected);
                var actualCategory = Category(actual);
                if (expectedCategory != actualCategory)
                    problems.Add($"[type] {key} ({variant}): v1={expectedCategory} v2={actualCategory}");
            }
        }

        Assert.True(problems.Count == 0,
            $"{problems.Count} compatibility problems:\n" + string.Join("\n", problems.Take(50)));
    }

    [AvaloniaFact]
    public void Every_v1_implicit_control_theme_exists_in_fluent2()
    {
        var v1 = new Avalonia.Themes.Fluent.FluentTheme();
        var v2 = new Fluent2Theme();
        var types = ThemeResourceWalker.CollectKeys(v1).OfType<Type>().ToList();

        Assert.True(types.Count > 70, $"Only enumerated {types.Count} implicit theme types; walker is broken.");

        var problems = new List<string>();
        foreach (var type in types)
        {
            if (!((IResourceNode)v2).TryGetResource(type, ThemeVariant.Light, out var value))
            {
                problems.Add($"[missing] {type.Name}");
            }
            else if (value is not ControlTheme theme)
            {
                problems.Add($"[not-a-theme] {type.Name}: {value?.GetType().Name}");
            }
            else if (theme.TargetType != type)
            {
                problems.Add($"[target-type] {type.Name}: theme targets {theme.TargetType?.Name}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [AvaloniaFact]
    public void Compact_density_dictionary_keeps_v1_key_set()
    {
        var v1Compact = GetCompactStyles(new Avalonia.Themes.Fluent.FluentTheme());
        var v2Compact = GetCompactStyles(new Fluent2Theme());

        var missing = v1Compact.Keys.Where(k => !v2Compact.ContainsKey(k)).ToList();
        Assert.True(missing.Count == 0,
            "Compact density keys missing in Fluent2: " + string.Join(", ", missing));
    }

    private static ResourceDictionary GetCompactStyles(object theme)
    {
        // Both themes stash the keyed CompactStyles dictionary in a private field;
        // there is no public way to enumerate it, and compiled XAML for plain
        // resource dictionaries cannot be loaded by URI at runtime.
        var field = theme.GetType().GetField("_compactStyles",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        return Assert.IsType<ResourceDictionary>(field!.GetValue(theme));
    }

    private static string Category(object? value) => value switch
    {
        null => "null",
        IBrush => "brush",
        Color => "color",
        Thickness => "thickness",
        CornerRadius => "cornerradius",
        double => "double",
        bool => "bool",
        FontFamily => "fontfamily",
        ControlTheme ct => $"controltheme({ct.TargetType?.FullName})",
        Geometry => "geometry",
        ITransform => "transform",
        TimeSpan => "timespan",
        string => "string",
        int => "int",
        _ => value.GetType().FullName!,
    };
}
