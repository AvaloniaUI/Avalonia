using System;
using System.Linq;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentResourceDictionaryTests() : ResourceDictionaryTests(typeof(FluentTheme));
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
}
