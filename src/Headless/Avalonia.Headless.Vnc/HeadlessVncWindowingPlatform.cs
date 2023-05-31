using System;
using Avalonia.Platform;

namespace Avalonia.Headless.Vnc;
internal class HeadlessVncWindowingPlatform : IWindowingPlatform
{
    readonly PixelFormat _frameBufferFormat;
    readonly HeadlessVncConnectionManager _connectionManager;

    public HeadlessVncWindowingPlatform(PixelFormat frameBufferFormat, HeadlessVncConnectionManager connectionManager)
    {
        _frameBufferFormat = frameBufferFormat;
        _connectionManager = connectionManager;
    }

    public IWindowImpl CreateWindow() => new HeadlessVncWindowImpl(false, _frameBufferFormat, _connectionManager);

    public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

    public IPopupImpl CreatePopup() => new HeadlessVncWindowImpl(true, _frameBufferFormat, _connectionManager);

    public ITrayIconImpl? CreateTrayIcon() => null;
}
