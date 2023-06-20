using System;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositionShared : IDisposable
{
    public ICompositor Compositor { get; }
    public ICompositor5 Compositor5 { get; }
    public ICompositorDesktopInterop DesktopInterop { get; }
    public ICompositionBrush BlurBrush { get; }
    public ICompositionBrush? MicaBrush { get; }
    public object SyncRoot { get; } = new();

    public static readonly Version MinAcrylicVersion = new(10, 0, 15063);
    public static readonly Version MinHostBackdropVersion = new(10, 0, 22000);
    
    public WinUiCompositionShared(ICompositor compositor)
    {
        Compositor = compositor.CloneReference();
        Compositor5 = compositor.QueryInterface<ICompositor5>();
        BlurBrush = WinUiCompositionUtils.CreateAcrylicBlurBackdropBrush(compositor);
        MicaBrush = WinUiCompositionUtils.CreateMicaBackdropBrush(compositor);
        DesktopInterop = compositor.QueryInterface<ICompositorDesktopInterop>();
    }
    
    public void Dispose()
    {
        BlurBrush.Dispose();
        MicaBrush?.Dispose();
        DesktopInterop.Dispose();
        Compositor.Dispose();
        Compositor5.Dispose();
    }
}
