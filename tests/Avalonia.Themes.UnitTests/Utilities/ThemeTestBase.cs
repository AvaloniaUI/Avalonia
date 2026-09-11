using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Themes.UnitTests.Utilities;

public abstract class ThemeTestBase(Type typeEntryPoint)
{
    static ThemeTestBase()
    {
        StandardAssetLoader.RegisterResUriParsers();
    }

    protected Assembly TestAssembly { get; } = typeEntryPoint.Assembly;
    protected Type EntryPointClass { get; } = typeEntryPoint;

    protected IEnumerable<MethodInfo> FindXamlPopulateMethods() =>
        TestAssembly.GetType("CompiledAvaloniaXaml.!AvaloniaResources", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Populate:"));

    protected IEnumerable<MethodInfo> FindXamlBuildMethods() =>
        TestAssembly.GetType("CompiledAvaloniaXaml.!AvaloniaResources", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Build:"));

    protected MethodInfo FindXamlLoader()
    {
        var loaderType = TestAssembly.GetType("CompiledAvaloniaXaml.!XamlLoader", throwOnError: true)!;
        var tryLoadMethod = loaderType.GetMethod("TryLoad", BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider), typeof(string)]);
        Assert.NotNull(tryLoadMethod);
        return tryLoadMethod;
    }

    protected Styles CallCtor(IServiceProvider? sp = null)
    {
        var theme = Activator.CreateInstance(EntryPointClass, sp);
        return Assert.IsAssignableFrom<Styles>(theme);
    }

    protected object? CallTryLoad(string relativePath, IServiceProvider? sp = null)
    {
        var fullPath = $"avares://{TestAssembly.GetName().Name}/{relativePath}";
        Assert.True(Uri.TryCreate(fullPath, UriKind.Absolute, out _), "Invalid TryLoad URL");
        return FindXamlLoader().Invoke(null, [sp, fullPath]);
    }
}
