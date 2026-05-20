using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition;

namespace Avalonia.Win32;

/// <summary>
/// A minimal <see cref="ITopLevelImpl"/> that hosts an Avalonia content tree on top of an
/// externally managed swap-chain surface (e.g. a WinUI <c>SwapChainPanel</c> or any other
/// host that supplies an <see cref="IGlPlatformSurface"/>). Sizing, scaling and input
/// pumping are driven by the host.
/// </summary>
internal class SwapChainTopLevelImpl : ITopLevelImpl
{
    private readonly IGlPlatformSurface _glSurface;
    private Size _clientSize;
    private double _scaling = 1.0;

    public SwapChainTopLevelImpl(IGlPlatformSurface glSurface)
    {
        _glSurface = glSurface;
        var platformGraphics = AvaloniaLocator.Current.GetService<IPlatformGraphics>();
        Compositor = new Compositor(platformGraphics);
    }

    public Size ClientSize
    {
        get => _clientSize;
        set
        {
            _clientSize = value;
            Resized?.Invoke(value, WindowResizeReason.Unspecified);
        }
    }

    public double RenderScaling
    {
        get => _scaling;
        set
        {
            _scaling = value;
            ScalingChanged?.Invoke(value);
        }
    }

    public double DesktopScaling => _scaling;

    public IPlatformHandle? Handle => null;

    public Compositor Compositor { get; }

    public IPlatformRenderSurface[] Surfaces => [_glSurface];

    public Action<RawInputEventArgs>? Input { get; set; }

    public Action<Rect>? Paint { get; set; }

    public Action<Size, WindowResizeReason>? Resized { get; set; }

    public Action<double>? ScalingChanged { get; set; }

    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

    public Action? Closed { get; set; }

    public Action? LostFocus { get; set; }

    public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new(1, 1, 1);

    public IInputRoot? InputRoot { get; private set; }

    public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

    public Point PointToClient(PixelPoint point) => point.ToPoint(_scaling);

    public PixelPoint PointToScreen(Point point) => PixelPoint.FromPoint(point, _scaling);

    public void SetCursor(ICursorImpl? cursor) { }

    // Uses overlays instead of popups.
    public IPopupImpl? CreatePopup() => null;

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels) { }

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant) { }

    /// <summary>
    /// Optional IME implementation provided by the host.
    /// </summary>
    public ITextInputMethodImpl? TextInputMethod { get; set; }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IClipboard))
            return AvaloniaLocator.Current.GetService<IClipboard>();
        if (featureType == typeof(ITextInputMethodImpl))
            return TextInputMethod;
        return null;
    }

    public void Dispose()
    {
        Closed?.Invoke();
    }
}
