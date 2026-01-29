using Xunit;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

internal sealed class AvaloniaTestFrameworkDiscoverer : XunitTestFrameworkDiscoverer
{
    public AvaloniaTestFrameworkDiscoverer(
        IXunitTestAssembly testAssembly,
        IXunitTestCollectionFactory? collectionFactory = null)
        : base(testAssembly, collectionFactory)
    {
        DiscovererTypeCache[typeof(FactAttribute)] = typeof(AvaloniaFactDiscoverer);
        DiscovererTypeCache[typeof(TheoryAttribute)] = typeof(AvaloniaTheoryDiscoverer);
    }
}
