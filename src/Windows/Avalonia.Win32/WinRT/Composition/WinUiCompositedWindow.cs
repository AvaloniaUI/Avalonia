using System;
using System.Numerics;
using System.Threading;
using Avalonia.OpenGL.Egl;
using Avalonia.Reactive;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositedWindow : IDisposable
{
    public EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo WindowInfo { get; }
    private readonly WinUiCompositionShared _shared;
    private readonly ICompositionRoundedRectangleGeometry? _compositionRoundedRectangleGeometry;
    private readonly IVisual? _micaLight;
    private readonly IVisual? _micaDark;
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
            _blur.Dispose();
            _micaLight?.Dispose();
            _micaDark?.Dispose();
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
        if (shared.MicaBrushLight != null)
        {
            _micaLight = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.MicaBrushLight);
            containerChildren.InsertAtTop(_micaLight);
        }   
        
        if (shared.MicaBrushDark != null)
        {
            _micaDark = WinUiCompositionUtils.CreateBlurVisual(shared.Compositor, shared.MicaBrushDark);
            containerChildren.InsertAtTop(_micaDark);
        }

        _compositionRoundedRectangleGeometry =
            WinUiCompositionUtils.ClipVisual(shared.Compositor, backdropCornerRadius, _blur, _micaLight, _micaDark);

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
                               || (blurEffect == BlurEffect.MicaLight && _micaLight == null) ||
                               (blurEffect == BlurEffect.MicaDark && _micaDark == null) ?
                1 :
                0);
            _micaLight?.SetIsVisible(blurEffect == BlurEffect.MicaLight ? 1 : 0);
            _micaDark?.SetIsVisible(blurEffect == BlurEffect.MicaDark ? 1 : 0);
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
