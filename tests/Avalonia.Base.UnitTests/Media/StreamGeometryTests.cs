using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Media;

public class StreamGeometryTests
{
    [Fact]
    public void Clone_With_Transform_Preserves_Transform()
    {
        using (UnitTestApplication.Start(GetServices()))
        {
            var target = new StreamGeometry();

            using (target.Open())
            {
            }

            var transform = new TranslateTransform(50, 50);
            target.Transform = transform;

            var clone = Assert.IsType<StreamGeometry>(target.Clone());

            Assert.Same(transform, clone.Transform);
        }
    }

    [Fact]
    public void Open_With_Transform_Opens_Source_StreamGeometry()
    {
        using (UnitTestApplication.Start(GetServices()))
        {
            var target = new StreamGeometry();

            using (target.Open())
            {
            }

            target.Transform = new TranslateTransform(50, 50);

            using (target.Open())
            {
            }
        }
    }

    private static TestServices GetServices()
    {
        var context = Mock.Of<IStreamGeometryContextImpl>();
        var transformedGeometry = Mock.Of<ITransformedGeometryImpl>();
        var streamGeometry = Mock.Of<IStreamGeometryImpl>(x =>
            x.Open() == context &&
            x.WithTransform(It.IsAny<Matrix>()) == transformedGeometry);
        var renderInterface = Mock.Of<IPlatformRenderInterface>(x =>
            x.CreateStreamGeometry() == streamGeometry);
        return new TestServices(renderInterface: renderInterface);
    }
}
