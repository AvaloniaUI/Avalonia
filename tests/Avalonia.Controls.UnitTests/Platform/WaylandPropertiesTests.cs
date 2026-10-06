using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform;

public class WaylandPropertiesTests : ScopedTestBase
{
    [Fact]
    public void WaylandProperties_AppId_AttachedProperty_Works()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var window = new Window();
            Assert.Null(WaylandProperties.GetAppId(window));

            WaylandProperties.SetAppId(window, "org.test.app");
            Assert.Equal("org.test.app", WaylandProperties.GetAppId(window));

            WaylandProperties.SetAppId(window, null);
            Assert.Null(WaylandProperties.GetAppId(window));
        }
    }

    [Fact]
    public void WaylandProperties_AppId_NotifiesFeature()
    {
        var featureMock = new Mock<IWaylandOptionsToplevelImplFeature>();
        var windowImplMock = new Mock<IWindowImpl>();
        windowImplMock.Setup(r => r.RenderScaling).Returns(1.0);
        windowImplMock.Setup(r => r.Compositor).Returns(RendererMocks.CreateDummyCompositor());
        windowImplMock.Setup(x => x.TryGetFeature(typeof(IWaylandOptionsToplevelImplFeature)))
            .Returns(featureMock.Object);

        var services = TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(() => windowImplMock.Object));

        using (UnitTestApplication.Start(services))
        {
            var window = new Window();

            WaylandProperties.SetAppId(window, "my.custom.appid");
            featureMock.Verify(x => x.SetAppId("my.custom.appid"), Times.Once);

            WaylandProperties.SetAppId(window, null);
            featureMock.Verify(x => x.SetAppId(null), Times.Once);
        }
    }
}
