using System;
using Moq;
using Perspex.Platform;

namespace Perspex.Controls.UnitTests.Primitives
{
    public class WindowingPlatformMock : IWindowingPlatform
    {
        private readonly Func<IWindowImpl> _windowImpl;
        private readonly Func<IPopupImpl> _popupImpl;

        public WindowingPlatformMock(Func<IWindowImpl> windowImpl = null, Func<IPopupImpl> popupImpl = null )
        {
            _windowImpl = windowImpl;
            _popupImpl = popupImpl;
        }

        public IWindowImpl CreateWindow()
        {
            return _windowImpl?.Invoke() ?? new Mock<IWindowImpl>().Object;
        }

        public IWindowImpl CreateDesignerFriendlyWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup() => _popupImpl?.Invoke() ?? new Mock<IPopupImpl>().Object;
    }
}