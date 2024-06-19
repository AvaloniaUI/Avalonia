using System;
using Avalonia.Controls.Platform;
using MonoMac.AppKit;

namespace IntegrationTestApp.Embedding;

internal class MacOSViewHandle(NSView view) : INativeControlHostDestroyableControlHandle
{
    private NSView? _view = view;

    public IntPtr Handle => _view?.Handle ?? IntPtr.Zero;
    public string HandleDescriptor => "NSView";

    public void Destroy()
    {
        _view?.Dispose();
        _view = null;
    }
}
