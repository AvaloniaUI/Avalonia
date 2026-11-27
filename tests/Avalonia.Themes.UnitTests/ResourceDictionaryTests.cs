using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.Diagnostics;
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
    public void Should_Define_Resources_With_Valid_Dynamic_Resource_Values()
    {
        // This test only validates DynamicResource setters directly on the resource value object.
        // Validation doesn't cover nested setters, like in gradients.  

        var theme = CreateAttachedTheme();
        var allResourcesPerVariant = theme.EnumerateResources().GroupBy(r => r.ThemeVariant)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Key).ToList());

        var flatResources = theme.Resources.EnumerateResources();

        Assert.All(flatResources.Where(r => r.Value is AvaloniaObject), resource =>
        {
            var avObject = (AvaloniaObject)resource.Value!;
            var allTemplateDynamicResourceKeys = avObject.GetValueStoreDiagnostic()
                .AppliedFrames
                .SelectMany(f => f.Values)
                .Select(v => v.Property)
                .Distinct()
                .Select(prop => BindingOperations.GetBindingExpressionBase(avObject, prop))
                .OfType<DynamicResourceExpression>()
                .Select(expr => expr.ResourceKey)
                .ToArray();

            // Can be empty
            Assert.All(allTemplateDynamicResourceKeys, c =>
            {
                Assert.Contains(c, allResourcesPerVariant[ThemeVariant.Default]);
            });
        });
    }
}
