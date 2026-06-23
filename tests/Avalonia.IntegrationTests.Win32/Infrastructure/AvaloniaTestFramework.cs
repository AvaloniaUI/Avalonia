using System.Reflection;
using Xunit.v3;

namespace Avalonia.IntegrationTests.Win32.Infrastructure;

internal sealed class AvaloniaTestFramework : XunitTestFramework
{
    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
        => new AvaloniaTestFrameworkExecutor(new XunitTestAssembly(assembly, null, assembly.GetName().Version));
}
