using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class MouseDeviceTests : PointerTestsBase
    {
        [Fact]
        public void Initial_Buttons_Are_Not_Set_Without_Corresponding_Mouse_Down()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var settingsMock = new Mock<IPlatformSettings>();
            var dispatcherMock = new Mock<IDispatcherImpl>();

            dispatcherMock.Setup(x => x.CurrentThreadIsLoopThread).Returns(true);

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(settingsMock.Object);

            using var app = UnitTestApplication.Start(
                new TestServices(
                    inputManager: new InputManager(),
                    dispatcherImpl: dispatcherMock.Object));

            var renderer = new Mock<IHitTester>();
            var device = new MouseDevice();
            var impl = CreateTopLevelImplMock();

            var control = new Control();
            var root = CreateInputRoot(impl.Object, control, renderer.Object);
           
            MouseButton button = default;

            root.PointerReleased += (s, e) => button = e.InitialPressMouseButton;

            var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
            var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

            SetHit(renderer, control);

            impl.Object.Input!(up);

            Assert.Equal(MouseButton.None, button);

            impl.Object.Input!(down);
            impl.Object.Input!(up);

            Assert.Equal(MouseButton.Left, button);
           
            impl.Object.Input!(up);

            Assert.Equal(MouseButton.None, button);        
        }

        [Fact]
        public void Capture_Is_Transferred_To_Parent_When_Control_Removed()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IHitTester>();
            var device = new MouseDevice();
            var impl = CreateTopLevelImplMock();

            Canvas control;
            Panel rootChild;
            var root = CreateInputRoot(impl.Object, rootChild = new Panel
            {
                Children =
                {
                    (control = new Canvas())
                }
            }, renderer.Object);

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

            var renderer = new Mock<IHitTester>();
            var device = new MouseDevice();
            var impl = CreateTopLevelImplMock();

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
            }, renderer.Object);
           
           
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
