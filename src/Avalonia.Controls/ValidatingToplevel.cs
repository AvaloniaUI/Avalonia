using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal class ValidatingToplevelImpl : ITopLevelImpl, ITopLevelImplWithNativeControlHost,
    ITopLevelImplWithNativeMenuExporter, ITopLevelImplWithTextInputMethod
{
    private readonly ITopLevelImpl _impl;
    private bool _disposed;

    public ValidatingToplevelImpl(ITopLevelImpl impl)
    {
        _impl = impl ?? throw new InvalidOperationException(
            "Could not create TopLevel implementation: maybe no windowing subsystem was initialized?");
    }

    public void Dispose()
    {
        _disposed = true;
        _impl.Dispose();
    }

    protected void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(_impl.GetType().FullName);
    }

    protected ITopLevelImpl Inner
    {
        get
        {
            CheckDisposed();
            return _impl;
        }
    }

    public static ITopLevelImpl Wrap(ITopLevelImpl impl)
    {
#if DEBUG
        if (impl is ValidatingToplevelImpl)
            return impl;
        return new ValidatingToplevelImpl(impl);
#else
        return impl;
#endif
    }

    public Size ClientSize => Inner.ClientSize;
    public Size? FrameSize => Inner.FrameSize;
    public double RenderScaling => Inner.RenderScaling;
    public IEnumerable<object> Surfaces => Inner.Surfaces;

    public Action<RawInputEventArgs> Input
    {
        get => Inner.Input;
        set => Inner.Input = value;
    }

    public Action<Rect> Paint
    {
        get => Inner.Paint;
        set => Inner.Paint = value;
    }

    public Action<Size, PlatformResizeReason> Resized
    {
        get => Inner.Resized;
        set => Inner.Resized = value;
    }

    public Action<double> ScalingChanged
    {
        get => Inner.ScalingChanged;
        set => Inner.ScalingChanged = value;
    }

    public Action<WindowTransparencyLevel> TransparencyLevelChanged
    {
        get => Inner.TransparencyLevelChanged;
        set => Inner.TransparencyLevelChanged = value;
    }

    public IRenderer CreateRenderer(IRenderRoot root) => Inner.CreateRenderer(root);

    public void Invalidate(Rect rect) => Inner.Invalidate(rect);

    public void SetInputRoot(IInputRoot inputRoot) => Inner.SetInputRoot(inputRoot);

    public Point PointToClient(PixelPoint point) => Inner.PointToClient(point);

    public PixelPoint PointToScreen(Point point) => Inner.PointToScreen(point);

    public void SetCursor(ICursorImpl cursor) => Inner.SetCursor(cursor);

    public Action Closed
    {
        get => Inner.Closed;
        set => Inner.Closed = value;
    }

    public Action LostFocus
    {
        get => Inner.LostFocus;
        set => Inner.LostFocus = value;
    }

    // Exception: for some reason we are notifying platform mouse device from TopLevel.cs
    public IMouseDevice MouseDevice => _impl.MouseDevice;
    public IPopupImpl CreatePopup() => Inner.CreatePopup();

    public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel) =>
        Inner.SetTransparencyLevelHint(transparencyLevel);


    public WindowTransparencyLevel TransparencyLevel => Inner.TransparencyLevel;
    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => Inner.AcrylicCompensationLevels;
    public INativeControlHostImpl NativeControlHost => (Inner as ITopLevelImplWithNativeControlHost)?.NativeControlHost;

    public ITopLevelNativeMenuExporter NativeMenuExporter =>
        (Inner as ITopLevelImplWithNativeMenuExporter)?.NativeMenuExporter;

    public ITextInputMethodImpl TextInputMethod => (Inner as ITopLevelImplWithTextInputMethod)?.TextInputMethod;
}

internal class ValidatingWindowBaseImpl : ValidatingToplevelImpl, IWindowBaseImpl
{
    private readonly IWindowBaseImpl _impl;

    public ValidatingWindowBaseImpl(IWindowBaseImpl impl) : base(impl)
    {
        _impl = impl;
    }

    protected new IWindowBaseImpl Inner
    {
        get
        {
            CheckDisposed();
            return _impl;
        }
    }
    
    public static IWindowBaseImpl Wrap(IWindowBaseImpl impl)
    {
#if DEBUG
        if (impl is ValidatingToplevelImpl)
            return impl;
        return new ValidatingWindowBaseImpl(impl);
#else
        return impl;
#endif
    }

    public void Show(bool activate, bool isDialog) => Inner.Show(activate, isDialog);

    public void Hide() => Inner.Hide();

    public double DesktopScaling => Inner.DesktopScaling;
    public PixelPoint Position => Inner.Position;

    public Action<PixelPoint> PositionChanged
    {
        get => Inner.PositionChanged;
        set => Inner.PositionChanged = value;
    }

    public void Activate() => Inner.Activate();

    public Action Deactivated
    {
        get => Inner.Deactivated;
        set => Inner.Deactivated = value;
    }

    public Action Activated
    {
        get => Inner.Activated;
        set => Inner.Activated = value;
    }

    public IPlatformHandle Handle => Inner.Handle;
    public Size MaxAutoSizeHint => Inner.MaxAutoSizeHint;
    public void SetTopmost(bool value) => Inner.SetTopmost(value);
    public IScreenImpl Screen => Inner.Screen;
}

internal class ValidatingWindowImpl : ValidatingWindowBaseImpl, IWindowImpl
{
    private readonly IWindowImpl _impl;

    public ValidatingWindowImpl(IWindowImpl impl) : base(impl)
    {
        _impl = impl;
    }

    protected new IWindowImpl Inner
    {
        get
        {
            CheckDisposed();
            return _impl;
        }
    }
    
    public static IWindowImpl Unwrap(IWindowImpl impl)
    {
        if (impl is ValidatingWindowImpl v)
            return v.Inner;
        return impl;
    }

    public static IWindowImpl Wrap(IWindowImpl impl)
    {
#if DEBUG
        if (impl is ValidatingToplevelImpl)
            return impl;
        return new ValidatingWindowImpl(impl);
#else
        return impl;
#endif
    }

    public WindowState WindowState
    {
        get => Inner.WindowState;
        set => Inner.WindowState = value;
    }

    public Action<WindowState> WindowStateChanged
    {
        get => Inner.WindowStateChanged;
        set => Inner.WindowStateChanged = value;
    }

    public void SetTitle(string title) => Inner.SetTitle(title);

    public void SetParent(IWindowImpl parent)
    {
        //Workaround. SetParent will cast IWindowImpl to WindowImpl but  ValidatingWindowImpl isn't actual WindowImpl so it will fail with InvalidCastException.
        if (parent is ValidatingWindowImpl validatingToplevelImpl)
        {
            Inner.SetParent(validatingToplevelImpl.Inner);
        }
        else
        {
            Inner.SetParent(parent);
        }
    }

    public void SetEnabled(bool enable) => Inner.SetEnabled(enable);

    public Action GotInputWhenDisabled
    {
        get => Inner.GotInputWhenDisabled;
        set => Inner.GotInputWhenDisabled = value;
    }

    public void SetSystemDecorations(SystemDecorations enabled) => Inner.SetSystemDecorations(enabled);

    public void SetIcon(IWindowIconImpl icon) => Inner.SetIcon(icon);

    public void ShowTaskbarIcon(bool value) => Inner.ShowTaskbarIcon(value);

    public void CanResize(bool value) => Inner.CanResize(value);

    public Func<bool> Closing
    {
        get => Inner.Closing;
        set => Inner.Closing = value;
    }

    public bool IsClientAreaExtendedToDecorations => Inner.IsClientAreaExtendedToDecorations;

    public Action<bool> ExtendClientAreaToDecorationsChanged
    {
        get => Inner.ExtendClientAreaToDecorationsChanged;
        set => Inner.ExtendClientAreaToDecorationsChanged = value;
    }

    public bool NeedsManagedDecorations => Inner.NeedsManagedDecorations;
    public Thickness ExtendedMargins => Inner.ExtendedMargins;
    public Thickness OffScreenMargin => Inner.OffScreenMargin;
    public void BeginMoveDrag(PointerPressedEventArgs e) => Inner.BeginMoveDrag(e);

    public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e) => Inner.BeginResizeDrag(edge, e);

    public void Resize(Size clientSize, PlatformResizeReason reason) =>
        Inner.Resize(clientSize, reason);

    public void Move(PixelPoint point) => Inner.Move(point);

    public void SetMinMaxSize(Size minSize, Size maxSize) => Inner.SetMinMaxSize(minSize, maxSize);

    public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint) =>
        Inner.SetExtendClientAreaToDecorationsHint(extendIntoClientAreaHint);

    public void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints) =>
        Inner.SetExtendClientAreaChromeHints(hints);

    public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight) =>
        Inner.SetExtendClientAreaTitleBarHeightHint(titleBarHeight);
}

internal class ValidatingPopupImpl : ValidatingWindowBaseImpl, IPopupImpl
{
    private readonly IPopupImpl _impl;

    public ValidatingPopupImpl(IPopupImpl impl) : base(impl)
    {
        _impl = impl;
    }

    protected new IPopupImpl Inner
    {
        get
        {
            CheckDisposed();
            return _impl;
        }
    }
    
    public static IPopupImpl Wrap(IPopupImpl impl)
    {
#if DEBUG
        if (impl is ValidatingToplevelImpl)
            return impl;
        return new ValidatingPopupImpl(impl);
#else
        return impl;
#endif
    }

    public IPopupPositioner PopupPositioner => Inner.PopupPositioner;
    public void SetWindowManagerAddShadowHint(bool enabled) => Inner.SetWindowManagerAddShadowHint(enabled);
}
