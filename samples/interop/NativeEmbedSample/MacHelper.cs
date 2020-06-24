using System;
using Avalonia.Platform;
using MonoMac.AppKit;

namespace NativeEmbedSample
{
    public class MacHelper
    {
        private static bool _isInitialized;

        public static void EnsureInitialized()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            NSApplication.Init();
        }
    }

    class MacOSViewHandle : IPlatformHandle, IDisposable
    {
        private NSView _view;

        public MacOSViewHandle(NSView view)
        {
            _view = view;
        }

        public IntPtr Handle => _view?.Handle ?? IntPtr.Zero;
        public string HandleDescriptor => "NSView";

        public void Dispose()
        {
            _view.Dispose();
            _view = null;
        }
    }

}
