using System;
using System.Linq;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentPublicApiTests() : PublicApiTests(typeof(FluentTheme));
public class SimplePublicApiTests() : PublicApiTests(typeof(SimpleTheme));

public abstract class PublicApiTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Should_Not_Include_Any_Reachable_Xaml_Files()
    {
        var xamlMethods = FindXamlPopulateMethods().Concat(FindXamlBuildMethods()).ToArray();
        Assert.NotEmpty(xamlMethods);
        Assert.DoesNotContain(xamlMethods, m => m.IsPublic);
    }
}
