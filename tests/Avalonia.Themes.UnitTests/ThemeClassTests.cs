using System;
using System.Reflection;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentThemeClassTests() : ThemeClassTests(typeof(FluentTheme));
public class SimpleThemeClassTests() : ThemeClassTests(typeof(SimpleTheme));

public abstract class ThemeClassTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Should_Have_ServiceProvider_Ctor()
    {
        var ctor = EntryPointClass.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance, [typeof(IServiceProvider)]);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void Should_Be_Reachable_With_Ctor()
    {
        var theme = CallCtor();
        Assert.NotNull(theme);
        Assert.IsType(EntryPointClass, theme);
    }

    [Fact]
    public void Should_Be_Reachable_With_Avares_Loader()
    {
        var theme = CallTryLoad($"{EntryPointClass.Name}.xaml");
        Assert.NotNull(theme);
        Assert.IsType(EntryPointClass, theme);
    }
}
