using System;
using Moq;
using Perspex.Platform;

namespace Perspex.Controls.UnitTests.Primitives
{
    class WindowingPlatformMock : IWindowingPlatform
    {
        public IWindowImpl CreateWindow()
        {
            return new Mock<IWindowImpl>().Object;
        }

        public IWindowImpl CreateDesignerFriendlyWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup() => new Mock<IPopupImpl>().Object;
    }
}