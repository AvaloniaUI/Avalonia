using Avalonia.Platform;
using System;

namespace Avalonia.iOS
{
    // This is somewhat generic, could probably put this elsewhere. But I don't think
    // it should part of the iOS App Delegate
    //
    class WindowingPlatformImpl : IWindowingPlatform
    {
        private readonly IWindowImpl _window;

        public WindowingPlatformImpl(IWindowImpl window)
        {
            _window = window;
        }

        public IWindowImpl CreateWindow()
        {
            return _window;
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }
    }
}
