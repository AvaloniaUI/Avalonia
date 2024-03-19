using Avalonia.Media;
using Avalonia.Platform;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;


public class StreamGeometryContextTest
{
    [Fact]
    public void Should_Ignore_Empty_Figure()
    {
        var contextImpl = new Mock<IStreamGeometryContextImpl>();
        using (var context = new StreamGeometryContext(contextImpl.Object))
        using (var parser = new PathMarkupParser(context))
        {
            parser.Parse("M50.799999,50.799999z M0,0z M 37.14073,21.33593 49.741711,33.91947 37.140724,46.50301");
            Assert.Equal(4, contextImpl.Invocations.Count);
        }
    }
}
