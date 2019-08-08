using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Moq;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockWindowingPlatform : IWindowingPlatform
    {
        private readonly Func<IWindowImpl> _windowImpl;
        private readonly Func<IPopupImpl> _popupImpl;

        public MockWindowingPlatform(Func<IWindowImpl> windowImpl = null, Func<IPopupImpl> popupImpl = null )
        {
            _windowImpl = windowImpl;
            _popupImpl = popupImpl;
        }

        public static Mock<IWindowImpl> CreateWindowMock(Func<IPopupImpl> popupImpl = null)
        {
            var win = Mock.Of<IWindowImpl>(x => x.Scaling == 1);
            var mock = Mock.Get(win);
            mock.Setup(x => x.CreatePopup()).Returns(() =>
            {
                if (popupImpl != null)
                    return popupImpl();
                return CreatePopupMock().Object;

            });
            PixelPoint pos = default;
            mock.SetupGet(x => x.Position).Returns(() => pos);
            mock.Setup(x => x.Move(It.IsAny<PixelPoint>())).Callback(new Action<PixelPoint>(np => pos = np));
            SetupToplevel(mock);
            return mock;
        }

        static void SetupToplevel<T>(Mock<T> mock) where T : class, ITopLevelImpl
        {
            mock.SetupGet(x => x.MouseDevice).Returns(new MouseDevice());
        }

        public static Mock<IPopupImpl> CreatePopupMock()
        {
            var positioner = Mock.Of<IPopupPositioner>();
            var popup = Mock.Of<IPopupImpl>(x => x.Scaling == 1);
            var mock = Mock.Get(popup);
            mock.SetupGet(x => x.PopupPositioner).Returns(positioner);
            SetupToplevel(mock);
            
            return mock;
        }

        public IWindowImpl CreateWindow()
        {
            return _windowImpl?.Invoke() ?? CreateWindowMock(_popupImpl).Object;
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }
    }
}
