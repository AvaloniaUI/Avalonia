using System;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
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

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(settingsMock.Object);

            using var app = UnitTestApplication.Start(
                new TestServices(
                    inputManager: new InputManager()));

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
            IPointer? result = null;
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

        private IDisposable SetupCrossTreePositionRequest(PixelPoint topLevelPosition, out PointerEventArgs pointerEvent, out Control elementA, out Control elementB)
        {
            var app = UnitTestApplication.Start(new TestServices(
                inputManager: new InputManager(),
                renderInterface: new HeadlessPlatformRenderInterface()));

            var renderer = new Mock<IHitTester>();
            var deviceMock = CreatePointerDeviceMock();
            var impl1 = CreateTopLevelImplMock();
            // Mocked position: topLevelPosition
            impl1.Setup(w => w.PointToScreen(default)).Returns<Point>(p => (PixelPoint.FromPoint(p, 1) + topLevelPosition));

            elementA = new Border();
            PointerEventArgs? moveEventArgs = null;

            elementA.PointerMoved += (s, e) => moveEventArgs = e;
            var root1 = CreateInputRoot(impl1.Object, elementA, renderer.Object);

            SetMove(deviceMock, root1.InputRoot, elementA);
            impl1.Object.Input!(CreateRawPointerMovedArgs(deviceMock.Object, root1));

            Assert.NotNull(moveEventArgs);
            pointerEvent = moveEventArgs;

            var impl2 = CreateTopLevelImplMock();
            // Mocked position: topLevelPosition * 2
            impl2.Setup(w => w.PointToClient(default)).Returns<PixelPoint>(p => (p - topLevelPosition - topLevelPosition).ToPoint(1));

            elementB = new Border();
            var root2 = CreateInputRoot(impl2.Object, elementB, renderer.Object);

            return app;
        }

        [Fact]
        public void GetPosition_Should_Support_Cross_Tree_Requests()
        {
            var topLevelOffset = new PixelPoint(5, 0);
            using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out _, out var elementB))
            {
                Assert.Equal(topLevelOffset.ToPoint(1), pointerEvent.GetPosition(elementB));
            }
        }

        [Fact]
        public void GetPosition_Should_Return_Default_When_Cross_Tree_Source_Closed()
        {
            var topLevelOffset = new PixelPoint(5, 0);
            using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out var elementA, out var elementB))
            {
                ((PresentationSource)elementA.PresentationSource!).Dispose();

                Assert.Equal(default, pointerEvent.GetPosition(elementB));
            }
        }

        [Fact]
        public void GetPosition_Should_Return_Default_When_Cross_Tree_Target_Closed()
        {
            var topLevelOffset = new PixelPoint(5, 0);
            using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out _, out var elementB))
            {
                ((PresentationSource)elementB.PresentationSource!).Dispose();

                Assert.Equal(default, pointerEvent.GetPosition(elementB));
            }
        }

        [Fact]
        public void Mouse_Pointer_Should_Set_Focus_On_Pointer_Pressed()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var settingsMock = new Mock<IPlatformSettings>();

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(settingsMock.Object);

            using var app = UnitTestApplication.Start(
               TestServices.RealFocus);

            var renderer = new Mock<IHitTester>();
            var impl = CreateTopLevelImplMock();

            var control = new Button()
            {
                Focusable = true
            };
            var root = CreateInputRoot(impl.Object, control, renderer.Object);

            var device = new MouseDevice();

            var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
            var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

            SetHit(renderer, control);

            Assert.False(control.IsFocused);

            impl.Object.Input!(down);

            Assert.True(control.IsFocused);
            impl.Object.Input!(up);

            Assert.True(control.IsFocused);
        }

        [Fact]
        public void Control_Should_Not_Gain_Focus_On_Mouse_Release()
        {
            using var scope = AvaloniaLocator.EnterScope();
            var settingsMock = new Mock<IPlatformSettings>();

            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformSettings>().ToConstant(settingsMock.Object);

            using var app = UnitTestApplication.Start(
               TestServices.RealFocus);

            var renderer = new Mock<IHitTester>();
            var impl = CreateTopLevelImplMock();

            var control1 = new Button()
            {
                Focusable = true
            };

            var control2 = new Button()
            {
                Focusable = true
            };
            var stack = new StackPanel()
            {
                Children = { control1, control2 }
            };
            var root = CreateInputRoot(impl.Object, stack, renderer.Object);

            var device = new MouseDevice();

            var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
            var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

            SetHit(renderer, control1);

            Assert.False(control1.IsFocused);

            impl.Object.Input!(down);

            Assert.True(control1.IsFocused);

            control2.Focus();

            impl.Object.Input!(up);

            Assert.False(control1.IsFocused);
        }
    }
}
