using System;
using Avalonia.Platform;

namespace Avalonia.iOS
{
    class WindowingPlatformImpl : IWindowingPlatform
    {
        public IWindowImpl CreateWindow()
        {
            throw new NotSupportedException();
        }

        public WindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }
    }
}
