using System.Reflection;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

internal sealed class AvaloniaTestFramework : XunitTestFramework
{
    protected override ITestFrameworkDiscoverer CreateDiscoverer(Assembly assembly)
        => new AvaloniaTestFrameworkDiscoverer(new XunitTestAssembly(assembly, null, assembly.GetName().Version));

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
        => new AvaloniaTestFrameworkExecutor(new XunitTestAssembly(assembly, null, assembly.GetName().Version));
}
