using System.Threading.Tasks;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

internal sealed class AvaloniaTestFrameworkExecutor(IXunitTestAssembly testAssembly)
    : XunitTestFrameworkExecutor(testAssembly)
{
    private readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.GetOrStartForAssembly(testAssembly.Assembly);

    protected override ITestFrameworkDiscoverer CreateDiscoverer()
        => new AvaloniaTestFrameworkDiscoverer(TestAssembly);

    public override async ValueTask DisposeAsync()
    {
        await _session.DisposeAsync();
        await base.DisposeAsync();
    }
}
