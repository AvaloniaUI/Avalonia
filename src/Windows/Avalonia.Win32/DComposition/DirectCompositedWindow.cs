using System;
using System.Threading;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Win32.DirectX;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositedWindow : ISwapchainVisualHost, IDisposable
{
    private readonly DirectCompositionShared _shared;
    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    private readonly IDCompositionVisual _container;
    private readonly IDCompositionTarget _target;
    private readonly IDCompositionDevice2 _device;

    public void Dispose()
    {
        lock (_shared.SyncRoot)
        {
            _container.Dispose();
            _target.Dispose();
            _device.Dispose();
        }
    }

    public DirectCompositedWindow(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info, DirectCompositionShared shared)
    {
        WindowInfo = info;
        _shared = shared;
        _device = shared.Device.CloneReference();

        using var desktopTarget = shared.Device.CreateTargetForHwnd(WindowInfo.Handle, false);
        _target = desktopTarget.QueryInterface<IDCompositionTarget>();

        using var container = shared.Device.CreateVisual();
        _container = container.CloneReference();

        _target.SetRoot(container);
    }

    public void SetSurface(IDCompositionSurface surface) => _container.SetContent(surface);

    public void SetSwapchainContent(IUnknown swapchain) => _container.SetContent(swapchain);

    // A DComp visual displays its content at native size, nothing to resize
    public void ResizeIfNeeded(PixelSize size)
    {
    }

    // No composition effects on the DComp path
    public void ApplyEffects(CompositionTransparencyLevel transparencyLevel, PlatformThemeVariant themeVariant)
    {
    }

    public IDisposable BeginTransaction()
    {
        Monitor.Enter(_shared.SyncRoot);
        return Disposable.Create(() =>
        {
            _device.Commit();
            Monitor.Exit(_shared.SyncRoot);
        });
    }
}
