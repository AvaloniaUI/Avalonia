using System;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositionShared : IDisposable
{
    public ICompositor Compositor { get; }
    public ICompositor5 Compositor5 { get; }
    public ICompositorDesktopInterop DesktopInterop { get; }
    public ICompositionBrush BlurBrush { get; }
    public ICompositionBrush? MicaBrushLight { get; }
    public ICompositionBrush? MicaBrushDark { get; }
    public object SyncRoot { get; } = new();

    public static readonly Version MinWinCompositionVersion = new(10, 0, 17134);
    public static readonly Version MinAcrylicVersion = new(10, 0, 15063);
    public static readonly Version MinHostBackdropVersion = new(10, 0, 22000);
    
    public WinUiCompositionShared(ICompositor compositor)
    {
        Compositor = compositor.CloneReference();
        Compositor5 = compositor.QueryInterface<ICompositor5>();
        BlurBrush = WinUiCompositionUtils.CreateAcrylicBlurBackdropBrush(compositor);
        MicaBrushLight = WinUiCompositionUtils.CreateMicaBackdropBrush(compositor, 242, 0.6f);
        MicaBrushDark = WinUiCompositionUtils.CreateMicaBackdropBrush(compositor, 32, 0.8f);
        DesktopInterop = compositor.QueryInterface<ICompositorDesktopInterop>();
    }
    
    public void Dispose()
    {
        BlurBrush.Dispose();
        MicaBrushLight?.Dispose();
        MicaBrushDark?.Dispose();
        DesktopInterop.Dispose();
        Compositor.Dispose();
        Compositor5.Dispose();
    }
}
