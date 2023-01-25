using System;

using Avalonia.Controls.Platform;
using MonoMac.AppKit;

namespace ControlCatalog.NetCore;

internal class MacHelper
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

internal class MacOSViewHandle : INativeControlHostDestroyableControlHandle
{
    private NSView _view;

    public MacOSViewHandle(NSView view)
    {
        _view = view;
    }

    public IntPtr Handle => _view?.Handle ?? IntPtr.Zero;
    public string HandleDescriptor => "NSView";

    public void Destroy()
    {
        _view.Dispose();
        _view = null;
    }
}
