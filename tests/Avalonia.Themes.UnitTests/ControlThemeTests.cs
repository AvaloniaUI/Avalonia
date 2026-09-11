using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentControlThemeTests() : ControlThemeTests(typeof(FluentTheme));
public class SimpleControlThemeTests() : ControlThemeTests(typeof(SimpleTheme));

public abstract class ControlThemeTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Should_Not_Contain_Any_Global_Styles()
    {
        // For performance reasons, we want all styles to be enclosed in ControlThemes,
        // without selectors that apply globally.

        var theme = CreateAttachedTheme();
        var allStyles = theme.EnumerateStyles();
        Assert.DoesNotContain(allStyles, s => s.Parent is null);
    }

    [Fact]
    public void Should_Define_ControlTheme_For_BuiltIn_Templated_Controls()
    {
        Assembly[] assembliesToScan = [typeof(Control).Assembly, typeof(ManagedFileChooser).Assembly];
        HashSet<Type> ignoredControls =
        [
            typeof(Control), typeof(TemplatedControl), typeof(ContentControl), typeof(NativeMenuBar),
            typeof(HeaderedItemsControl), typeof(HeaderedSelectingItemsControl), typeof(SelectingItemsControl),
            typeof(Thumb)
        ];

        var templatedControls = assembliesToScan.SelectMany(s => s.GetTypes())
            // Resolve all public non-abstract TemplatedControls
            // Technically, any StyledElement can have a control theme,
            // but templated control are ones that won't work without one.
            .Where(t => t is { DeclaringType: null, IsAbstract: false, IsPublic: true }
                        && typeof(TemplatedControl).IsAssignableFrom(t) && t.GetConstructor([]) is not null)
            // Respect StyleKeyOverride, and we don't care about initializing the object itself (skip ctor)
            .Select(t => (RuntimeHelpers.GetUninitializedObject(t) as StyledElement)?.StyleKey ?? t)
            .Distinct()
            // Filter common types that we don't style 
            .Where(t => !typeof(UserControl).IsAssignableFrom(t)
                        && (!typeof(Window).IsAssignableFrom(t) || t == typeof(Window))
                        && !ignoredControls.Contains(t))
            // WindowDrawnDecorations is the only StyleElement that is not Control but has ControlTheme
            .Prepend(typeof(WindowDrawnDecorations));

        var theme = CreateAttachedTheme();
        var defaultControlThemes = theme.EnumerateResources()
            .Where(r => r is { Value: ControlTheme, Key: Type })
            // Default ControlThemes must not be defined per specific ThemeVariant.
            .Where(r => r.ThemeVariant == ThemeVariant.Default)
            .Select(r => (Type)r.Key)
            .ToHashSet();

        Assert.All(templatedControls, c => Assert.Contains(c, defaultControlThemes));
    }

    [Fact]
    public void Should_Create_Setters_With_Valid_Dynamic_Resources()
    {
        // Validate that <Setter Value="{DynamicResource}"> points to an actually defined resource.
        // Unfortunately, we can't reliably include DynamicResource from templates

        var theme = CreateAttachedTheme();
        var allResourcesPerVariant = theme.EnumerateResources().GroupBy(r => r.ThemeVariant)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Key).ToList());

        var controlThemes = theme.EnumerateResources()
            .Where(r => r is { Value: ControlTheme })
            .Where(r => r.ThemeVariant == ThemeVariant.Default)
            .Select(r => (ControlTheme)r.Value!);

        // Enumerate all nested styles and retrieve full list of Setters
        var allSetters = controlThemes
            .SelectMany(t => t.EnumerateStyles())
            .SelectMany(t => t.Setters);
        var allDynamicResourceKeys = allSetters.OfType<Setter>()
            .Select(s => s.Value)
            .OfType<DynamicResourceExtension>()
            .Select(r => r.ResourceKey);

        Assert.All(allDynamicResourceKeys, c =>
        {
            Assert.Contains(c, allResourcesPerVariant[ThemeVariant.Default]);
        });
    }
}
