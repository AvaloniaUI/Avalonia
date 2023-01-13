using System;
using System.Numerics;
using System.Threading;
using Avalonia.OpenGL.Egl;
using Avalonia.Reactive;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositedWindow : IDisposable
{
    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    private readonly WinUiCompositionShared _shared;
    private readonly ICompositionRoundedRectangleGeometry _compositionRoundedRectangleGeometry;
    private readonly IVisual _mica;
    private readonly IVisual _blur;
    private readonly IVisual _visual;
    private PixelSize _size;
    private readonly ICompositionSurfaceBrush _surfaceBrush;
    private readonly ICompositionTarget _target;

    public void Dispose()
    {
        lock (_shared.SyncRoot)
        {
            _compositionRoundedRectangleGeometry?.Dispose();
            _blur?.Dispose();
            _mica?.Dispose();
            _visual.Dispose();
            _surfaceBrush.Dispose();
            _target.Dispose();
        }
    }

    public WinUiCompositedWindow(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info,
        WinUiCompositionShared shared, float? backdropCornerRadius)
    {
        WindowInfo = info;
        _shared = shared;
        using var desktopTarget = shared.DesktopInterop.CreateDesktopWindowTarget(WindowInfo.Handle, 0);
        _target = desktopTarget.QueryInterface<ICompositionTarget>();

        
        using var container = shared.Compositor.CreateContainerVisual();
        using var containerVisual = container.QueryInterface<IVisual>();
        using var containerVisual2 = container.QueryInterface<IVisual2>();
        containerVisual2.SetRelativeSizeAdjustment(new Vector2(1, 1));
        using var containerChildren = container.Children;

        _target.SetRoot(containerVisual);

        _blur = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.BlurBrush);
        if (shared.MicaBrush != null)
        {
            _mica = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.MicaBrush);
            containerChildren.InsertAtTop(_mica);
        }

        _compositionRoundedRectangleGeometry =
            WinUiCompositionUtils.ClipVisual(shared.Compositor, backdropCornerRadius, _blur, _mica);

        containerChildren.InsertAtTop(_blur);
        using var spriteVisual = shared.Compositor.CreateSpriteVisual();
        _visual = spriteVisual.QueryInterface<IVisual>();
        containerChildren.InsertAtTop(_visual);

        _surfaceBrush = shared.Compositor.CreateSurfaceBrush();
        using var compositionBrush = _surfaceBrush.QueryInterface<ICompositionBrush>();
        spriteVisual.SetBrush(compositionBrush);
        _target.SetRoot(containerVisual);
        
        
        
    }

    public void SetSurface(ICompositionSurface surface) => _surfaceBrush.SetSurface(surface);

    public void SetBlur(BlurEffect blurEffect)
    {
        lock (_shared.SyncRoot)
        {

            _blur.SetIsVisible(blurEffect == BlurEffect.Acrylic
                               || blurEffect == BlurEffect.Mica && _mica == null ?
                1 :
                0);
            _mica?.SetIsVisible(blurEffect == BlurEffect.Mica ? 1 : 0);
        }
    }

    public IDisposable BeginTransaction()
    {
        Monitor.Enter(_shared.SyncRoot);
        return Disposable.Create(() => Monitor.Exit(_shared.SyncRoot));
    }

    public void ResizeIfNeeded(PixelSize size)
    {
        lock (_shared.SyncRoot)
        {
            if (_size != size)
            {
                _visual.SetSize(new Vector2(size.Width, size.Height));
                _compositionRoundedRectangleGeometry?.SetSize(new Vector2(size.Width, size.Height));
                _size = size;
            }
        }
    }
}
