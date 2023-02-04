using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class MouseDeviceTests : PointerTestsBase
    {
        [Fact]
        public void Capture_Is_Transferred_To_Parent_When_Control_Removed()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = RendererMocks.CreateRenderer();
            var device = new MouseDevice();
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas control;
            Panel rootChild;
            var root = CreateInputRoot(impl.Object, rootChild = new Panel
            {
                Children =
                {
                    (control = new Canvas())
                }
            });

            // Synthesize event to receive a pointer.
            IPointer result = null;
            root.PointerMoved += (_, a) =>
            {
                result = a.Pointer;
            };
            SetHit(renderer, control);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root));

            Assert.NotNull(result);
            
            result.Capture(control);
            Assert.Same(control, result.Captured);

            rootChild.Children.Clear();

            Assert.Same(rootChild, result.Captured);
        }

        [Fact]
        public void GetPosition_Should_Respect_Control_RenderTransform()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = RendererMocks.CreateRenderer();
            var device = new MouseDevice();
            var impl = CreateTopLevelImplMock(renderer.Object);

            Border border;
            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (border = new Border
                    {
                        Background = Brushes.Black,
                        RenderTransform = new TranslateTransform(10, 0),
                    })
                }
            });
            
            
            Point? result = null;
            root.PointerMoved += (_, a) =>
            {
                result = a.GetPosition(border);
            };

            SetHit(renderer, border);
            impl.Object.Input!(CreateRawPointerMovedArgs(device, root, new Point(11, 11)));

            Assert.Equal(new Point(1, 11), result);
        }
    }
}
