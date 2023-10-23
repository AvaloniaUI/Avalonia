using System;
using System.Numerics;
using System.Threading;
using Avalonia.OpenGL.Egl;
using Avalonia.Reactive;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositedWindow : IDisposable
{
    private readonly DirectCompositionShared _shared;
    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    private readonly IDCompositionVisual _container;
    private readonly IDCompositionTarget _target;
    private readonly IDCompositionDevice _device;

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
        _device = shared.Device?.CloneReference() ?? throw new InvalidOperationException("IDCompositionDevice was not yet created.");

        using var desktopTarget = _device.CreateTargetForHwnd(WindowInfo.Handle, false);
        _target = desktopTarget.QueryInterface<IDCompositionTarget>();


        using var container = shared.Device.CreateVisual();
        _container = container.CloneReference();

        _target.SetRoot(container);
    }

    public unsafe void SetSurface(IDCompositionSurface surface) => _container.SetContent(MicroComRuntime.GetNativePointer(surface));

    public void SetBlur(BlurEffect blurEffect)
    {
        lock (_shared.SyncRoot)
        {
            // _blur.SetIsVisible(blurEffect == BlurEffect.Acrylic
            //                    || blurEffect == BlurEffect.Mica && _mica == null ?
            //     1 :
            //     0);
        }
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
