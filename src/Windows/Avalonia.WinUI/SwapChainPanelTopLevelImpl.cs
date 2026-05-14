using System;
using System.Collections.Generic;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Input.Platform;
using global::Avalonia.Input.Raw;
using global::Avalonia.OpenGL.Surfaces;
using global::Avalonia.Platform;
using global::Avalonia.Platform.Surfaces;
using global::Avalonia.Rendering.Composition;
using Size = global::Avalonia.Size;
using Point = global::Avalonia.Point;
using Rect = global::Avalonia.Rect;
using PixelPoint = global::Avalonia.PixelPoint;

namespace Avalonia.WinUI;

internal class SwapChainPanelTopLevelImpl : ITopLevelImpl
{
    private readonly IGlPlatformSurface _glSurface;
    private Size _clientSize;
    private double _scaling = 1.0;

    public SwapChainPanelTopLevelImpl(IGlPlatformSurface glSurface)
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

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IClipboard))
            return AvaloniaLocator.Current.GetService<IClipboard>();
        return null;
    }

    public void Dispose()
    {
        Closed?.Invoke();
    }
}
