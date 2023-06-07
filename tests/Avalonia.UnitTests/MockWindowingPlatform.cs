using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Moq;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;

namespace Avalonia.UnitTests
{
    public class MockWindowingPlatform : IWindowingPlatform
    {
        private static readonly Size s_screenSize = new Size(1280, 1024);
        private readonly Func<IWindowImpl> _windowImpl;
        private readonly Func<IWindowBaseImpl, IPopupImpl> _popupImpl;

        public MockWindowingPlatform(
            Func<IWindowImpl> windowImpl = null,
            Func<IWindowBaseImpl, IPopupImpl> popupImpl = null )
        {
            _windowImpl = windowImpl;
            _popupImpl = popupImpl;
        }

        public static Mock<IWindowImpl> CreateWindowMock(double initialWidth = 800, double initialHeight = 600)
        {
            var windowImpl = new Mock<IWindowImpl>();
            var clientSize = new Size(initialWidth,  initialHeight);

            windowImpl.SetupAllProperties();
            var compositor = RendererMocks.CreateDummyCompositor();
            windowImpl.Setup(x => x.Compositor).Returns(compositor);
            windowImpl.Setup(x => x.ClientSize).Returns(() => clientSize);
            windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(s_screenSize);
            windowImpl.Setup(x => x.DesktopScaling).Returns(1);
            windowImpl.Setup(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.Screen).Returns(CreateScreenMock().Object);

            windowImpl.Setup(r => r.TryGetFeature(It.IsAny<Type>())).Returns(null);

            windowImpl.Setup(x => x.CreatePopup()).Returns(() =>
            {
                return CreatePopupMock(windowImpl.Object).Object;
            });

            windowImpl.Setup(x => x.Dispose()).Callback(() =>
            {
                windowImpl.Object.Closed?.Invoke();
            });

            windowImpl.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback<PixelPoint>(x =>
            {
                windowImpl.Setup(x => x.Position).Returns(x);
                windowImpl.Object.PositionChanged?.Invoke(x);
            });

            windowImpl.Setup(x => x.PointToScreen(It.IsAny<Point>()))
                .Returns<Point>((point) => PixelPoint.FromPoint(point, 1) + windowImpl.Object.Position);

            windowImpl.Setup(x => x.PointToClient(It.IsAny<PixelPoint>()))
                .Returns<PixelPoint>(point => (point - windowImpl.Object.Position).ToPoint(1));

            windowImpl.Setup(x => x.Resize(It.IsAny<Size>(), It.IsAny<WindowResizeReason>()))
                .Callback<Size, WindowResizeReason>((x, y) =>
            {
                var constrainedSize = x.Constrain(s_screenSize);
                
                if (constrainedSize != clientSize)
                {
                    clientSize = constrainedSize;
                    windowImpl.Object.Resized?.Invoke(clientSize, y);
                }
            });

            windowImpl.Setup(x => x.Show(true, It.IsAny<bool>())).Callback(() =>
            {
                windowImpl.Object.Resized?.Invoke(windowImpl.Object.ClientSize, WindowResizeReason.Unspecified);
                windowImpl.Object.Activated?.Invoke();
            });

            return windowImpl;
        }

        public static Mock<IPopupImpl> CreatePopupMock(IWindowBaseImpl parent)
        {
            var popupImpl = new Mock<IPopupImpl>();
            var clientSize = new Size();
            var position = new PixelPoint();

            var positionerHelper = new ManagedPopupPositionerPopupImplHelper(parent, (pos, size, scale) =>
            {
                clientSize = size.Constrain(s_screenSize);
                position = pos;
                popupImpl.Object.PositionChanged?.Invoke(pos);
                popupImpl.Object.Resized?.Invoke(clientSize, WindowResizeReason.Unspecified);
            });
            
            var positioner = new ManagedPopupPositioner(positionerHelper);

            popupImpl.SetupAllProperties();
            var compositor = RendererMocks.CreateDummyCompositor();
            popupImpl.Setup(x => x.Compositor).Returns(compositor);
            popupImpl.Setup(x => x.ClientSize).Returns(() => clientSize);
            popupImpl.Setup(x => x.MaxAutoSizeHint).Returns(s_screenSize);
            popupImpl.Setup(x => x.RenderScaling).Returns(1);
            popupImpl.Setup(x => x.PopupPositioner).Returns(positioner);
            popupImpl.Setup(x => x.Position).Returns(()=>position);

            popupImpl.Setup(x => x.PointToScreen(It.IsAny<Point>()))
                .Returns<Point>((point) => PixelPoint.FromPoint(point, 1) + position);

            popupImpl.Setup(x => x.PointToClient(It.IsAny<PixelPoint>()))
                .Returns<PixelPoint>(point => (point - position).ToPoint(1));



            popupImpl.Setup(r => r.TryGetFeature(It.IsAny<Type>())).Returns(null);
            popupImpl.Setup(x => x.Dispose()).Callback(() =>
            {
                popupImpl.Object.Closed?.Invoke();
            });
            
            return popupImpl;
        }

        public static Mock<IScreenImpl> CreateScreenMock()
        {
            var screenImpl = new Mock<IScreenImpl>();
            var bounds = new PixelRect(0, 0, (int)s_screenSize.Width, (int)s_screenSize.Height);
            var screen = new Screen(96, bounds, bounds, true);
            screenImpl.Setup(x => x.AllScreens).Returns(new[] { screen });
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            return screenImpl;
        }

        public IWindowImpl CreateWindow()
        {
            if (_windowImpl is object)
            {
                return _windowImpl();
            }
            else
            {
                var mock = CreateWindowMock();

                if (_popupImpl is object)
                {
                    mock.Setup(x => x.CreatePopup()).Returns(() => _popupImpl(mock.Object));
                }

                return mock.Object;
            }
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return null;
        }
    }
}
