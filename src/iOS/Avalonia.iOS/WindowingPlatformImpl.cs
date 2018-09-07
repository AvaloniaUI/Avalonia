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

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotSupportedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }
    }
}
