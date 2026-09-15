using System;
using System.Linq;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

[Trait("Category", "FluentTheme")]
public class FluentPublicApiTests() : PublicApiTests(typeof(FluentTheme));
[Trait("Category", "SimpleTheme")]
public class SimplePublicApiTests() : PublicApiTests(typeof(SimpleTheme));

public abstract class PublicApiTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Should_Not_Include_Any_Reachable_Xaml_Files()
    {
        // This test looks up any control themes or other XAML files,
        // that were not marked as "internal" class modifier.
        // In our built-in themes it's expected that XAML files are internal, except theme entry point itself.

        var xamlMethods = FindXamlPopulateMethods().Concat(FindXamlBuildMethods()).ToArray();
        Assert.NotEmpty(xamlMethods);
        Assert.DoesNotContain(xamlMethods, m => m.IsPublic);
    }
}
