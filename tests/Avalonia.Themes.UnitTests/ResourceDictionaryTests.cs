using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Fluent2;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

[Trait("Category", "FluentTheme")]
public class FluentResourceDictionaryTests() : ResourceDictionaryTests(typeof(FluentTheme));
[Trait("Category", "Fluent2Theme")]
public class Fluent2ResourceDictionaryTests() : ResourceDictionaryTests(typeof(Fluent2Theme));
[Trait("Category", "SimpleTheme")]
public class SimpleResourceDictionaryTests() : ResourceDictionaryTests(typeof(SimpleTheme));

public abstract class ResourceDictionaryTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Should_Define_Identical_Keys_In_ThemeDictionaries()
    {
        // Any resource defined in one ThemeDictionary, must be defined in the rest

        var theme = CreateAttachedTheme();
        // We expect that our built-in themes define ThemeDictionaries on the root style object.
        var flatThemeDictionaryResources = theme.Resources
            .ThemeDictionaries.ToDictionary(
                dict => dict.Key,
                dict => dict.Value.EnumerateResources());

        Assert.NotEmpty(flatThemeDictionaryResources.Keys);
        Assert.All(flatThemeDictionaryResources.Keys, variant =>
        {
            var variantResourceKeys = flatThemeDictionaryResources[variant].Select(r => r.Key);
            var otherResourceKeys = flatThemeDictionaryResources
                .Where(p => p.Key != variant)
                .SelectMany(p => p.Value).Select(r => r.Key)
                .ToHashSet();

            Assert.All(variantResourceKeys, k => Assert.Contains(k, otherResourceKeys));
        });
    }

    [Fact]
    public void Should_Define_Resources_With_Valid_Dynamic_Resources()
    {
        // Validate that resources such as <SolidColorBrush Color="{DynamicResource}"> point to an actually defined resource.

        var theme = CreateAttachedTheme();
        var resources = theme.EnumerateResources().ToArray();
        var resourceKeysPerVariant = resources.GroupBy(r => r.ThemeVariant)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Key).ToHashSet());

        Assert.All(resources.Where(r => r.Value is AvaloniaObject and not ControlTheme), resource =>
        {
            var value = (AvaloniaObject)resource.Value!;
            var dynamicResourceKeys = AvaloniaPropertyRegistry.Instance.GetRegistered(value)
                .Select(prop => BindingOperations.GetBindingExpressionBase(value, prop))
                .OfType<DynamicResourceExpression>()
                .Select(expr => expr.ResourceKey)
                .ToArray();

            // Can be empty
            Assert.All(dynamicResourceKeys, key =>
            {
                Assert.True(
                    resourceKeysPerVariant[resource.ThemeVariant].Contains(key)
                    || resourceKeysPerVariant[ThemeVariant.Default].Contains(key),
                    $"Resource \"{resource.Key}\" references undefined resource \"{key}\"");
            });
        });
    }
}
