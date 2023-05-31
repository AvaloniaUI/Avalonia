using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Headless.Vnc;
internal class HeadlessVncWindowImpl : HeadlessWindowImpl
{
    readonly HeadlessVncConnectionManager _connectionManager;

    Window? _rootWindowControl;
    bool _closed;
    IDisposable? _windowPropObserver;

    public HeadlessVncWindowImpl(bool isPopup, PixelFormat frameBufferFormat, HeadlessVncConnectionManager connectionManager) 
        : base(isPopup, frameBufferFormat)
    {
        _connectionManager = connectionManager;
        if(connectionManager.ClientSize != null)
        {
            ClientSize = connectionManager.ClientSize.Value;
            FrameSize = ClientSize;
        }

        Closed += OnClose;
    }

    private void OnClose()
    {
        if (_closed)
            return;

        _connectionManager.WindowClosed();
        _closed = true;
    }

    public override void Dispose()
    {
        OnClose();
        base.Dispose();
        _windowPropObserver?.Dispose();
    }

    public override void Show(bool activate, bool isDialog)
    {
        base.Show(activate, isDialog);
        if (_rootWindowControl != null)
            _connectionManager.SetCurrentWindow(_rootWindowControl);
    }

    public override void SetInputRoot(IInputRoot inputRoot)
    {
        base.SetInputRoot(inputRoot);
        if (inputRoot is Window window)
        {
            _rootWindowControl = window;
            //we don't want the window to resize to the content since changing the framebuffer size can break the vnc session
            if (!_connectionManager.ResizeSessionIfContentSizeChanges)
            {
                window.SizeToContent = SizeToContent.Manual;
                _windowPropObserver = window
                    .GetPropertyChangedObservable(Window.SizeToContentProperty)
                    .Subscribe(e =>
                    {
                        window.SizeToContent = SizeToContent.Manual;
                    });
            }
        }
    }
}
