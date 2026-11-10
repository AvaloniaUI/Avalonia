using System;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NativeControlHostTests : ScopedTestBase
    {
        [Fact]
        public void Bounds_Are_Rounded_To_Device_Pixels_Under_LayoutTransformControl_Scale()
        {
            var bounds = default(Rect);
            var attachment = new Mock<INativeControlHostControlTopLevelAttachment>();
            attachment.Setup(x => x.ShowInBounds(It.IsAny<Rect>())).Callback<Rect>(x => bounds = x);
            var host = new Mock<INativeControlHostImpl>();
            host.Setup(x => x.CreateNewAttachment(It.IsAny<Func<IPlatformHandle, IPlatformHandle>>()))
                .Returns(attachment.Object);
            var windowingPlatform = new MockWindowingPlatform(() =>
            {
                var windowImpl = MockWindowingPlatform.CreateWindowMock();
                windowImpl.Setup(x => x.TryGetFeature(typeof(INativeControlHostImpl))).Returns(host.Object);
                return windowImpl.Object;
            });

            using (UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform: windowingPlatform)))
            {
                var target = new NativeControlHost();
                var window = new Window
                {
                    Content = new LayoutTransformControl
                    {
                        LayoutTransform = new ScaleTransform(1.15, 1.15),
                        Child = new Border { Padding = new Thickness(1), Child = target }
                    }
                };

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass();
                target.TryUpdateNativeControlPosition();

                Assert.Equal(new Rect(1, 1, 798, 598), bounds);
            }
        }
    }
}
