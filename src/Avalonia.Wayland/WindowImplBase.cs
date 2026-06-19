using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

/// <summary>
/// Shared base for Wayland top-level and popup window implementations.
/// Holds the common DI/threading state, render-scale, cursor, event
/// callbacks and no-op platform methods that Wayland doesn't support
/// (Move, SetTopmost, Activate, PointToClient/Screen, etc.).
/// </summary>
internal abstract partial class WindowBaseImpl : IWindowBaseImpl
{
    protected WaylandWorkerClient Client { get; }
    protected IInputRoot? InputRoot { get; private set; }
    protected MouseDevice Mouse { get; }
    protected TouchDevice Touch { get; }
    protected KeyboardDevice Keyboard  { get; }
    public double RenderScaling { get; set; } = 1;
    protected WaylandCursorImpl? CurrentCursor { get; private set; }
    protected bool IsEnabled  { get; set; } = true;
    protected bool IsDisposed  { get; private set; }
    
    internal IReadOnlyList<object> CurrentOutputIds { get; set; } = Array.Empty<object>();

    protected WindowBaseImpl(WaylandWorkerClient client)
    {
        Client = client;
        Compositor = client.Compositor;
        Mouse = MouseDevice.Primary;
        Touch = new TouchDevice();
        Keyboard = (KeyboardDevice)AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
    }

    public Compositor Compositor { get; }
    public double DesktopScaling => RenderScaling;
    public IPlatformHandle? Handle => null;

    public Size ClientSize
    {
        get;
        protected set
        {
            field = value;
            Client.AnyThreadWakeupRenderLoop();
        }
    }

    public abstract IPlatformRenderSurface[] Surfaces { get; }

    // Not supported by Wayland.
    public PixelPoint Position => default;
    public Point PointToClient(PixelPoint point) => new(point.X, point.Y);
    public PixelPoint PointToScreen(Point point) => new((int)point.X, (int)point.Y);
    public void Activate() { }
    public void SetTopmost(bool value) { }

    public abstract Size MaxAutoSizeHint { get; }
    public virtual Size? FrameSize => null;

    public WindowTransparencyLevel TransparencyLevel { get; } = WindowTransparencyLevel.Transparent;
    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = default;
    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels) { }
    public void SetFrameThemeVariant(PlatformThemeVariant? themeVariant) { }

    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }
    public Action<PixelPoint>? PositionChanged { get; set; }
    public Action? Activated { get; set; }
    public Action? Deactivated { get; set; }
    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

    public void SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

    protected Sink? CurrentSink { get; set; }

    public abstract void Show(bool activate, bool isDialog);

    public virtual void Hide()
    {
        var current = CurrentSink;
        CurrentSink = null;
        current?.Dispose();
    }

    public abstract IPopupImpl? CreatePopup();


    public virtual void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        var current = CurrentSink;
        CurrentSink = null;
        current?.Dispose();
        Closed?.Invoke();
    }

    public void SetCursor(ICursorImpl? cursor)
    {
        CurrentCursor = cursor as WaylandCursorImpl;
        ApplyCurrentCursor(SurfaceProxy);
    }

    /// <summary>
    /// Pushes <see cref="CurrentCursor"/> to the given surface proxy (<c>null</c> falls back to the
    /// default arrow). Also used by the sinks to re-apply the cursor on a freshly created surface.
    /// </summary>
    internal void ApplyCurrentCursor(WXdgShellSurfaceProxy? proxy) => proxy?.SetCursor(CurrentCursor?.Cursor);

    /// <summary>
    /// Returns the worker-side <c>xdg_surface</c>-derived proxy backing
    /// this top-level / popup, or <c>null</c> when no surface is
    /// currently created. Used by base-class operations that need to
    /// reach the surface uniformly across <see cref="WindowImpl"/> and
    /// <see cref="PopupImpl"/> (e.g. cursor application, child-popup
    /// parenting).
    /// </summary>
    internal abstract WXdgShellSurfaceProxy? SurfaceProxy { get; }

    protected void UpdateScaling(double scale)
    {
        if (Math.Abs(scale - RenderScaling) < 1e-6)
            return;
        RenderScaling = scale;
        ScalingChanged?.Invoke(scale);
        Client.AnyThreadWakeupRenderLoop();
    }

    protected void PostToUiThread(Action action) =>
        Dispatcher.UIThread.Post(action, DispatcherPriority.Input);

    public virtual object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IScreenImpl))
            return AvaloniaLocator.Current.GetService<IScreenImpl>();
        if (featureType == typeof(IClipboard))
            return AvaloniaLocator.Current.GetRequiredService<IClipboard>();
        if (featureType == typeof(ILauncher))
            return new BclLauncher();
        return null;
    }

    /// <summary>
    /// UI-thread sink for surface events. Receives worker→UI calls (already
    /// marshalled by <see cref="WSurfaceEventSinkProxy"/>) and converts them
    /// into Avalonia raw input events. Subclasses add surface-role-specific
    /// behaviour: top-levels handle xdg_toplevel configure/close (via
    /// <see cref="IWXdgTopLevelEventSink"/>) and chrome-driven move/resize;
    /// popups handle their own configure batch (via
    /// <see cref="IWXdgPopupEventSink"/>).
    /// </summary>
    internal abstract partial class Sink : IWSurfaceEventSink, IDisposable
    {
        protected WindowBaseImpl Parent { get; }
        protected bool IsDisposed { get; private set; }
        protected RawEventGrouper RawEventGrouper { get; }

        protected Sink(WindowBaseImpl parent)
        {
            Parent = parent;
            RawEventGrouper = new RawEventGrouper(DispatchInput, Parent.Client.InputDispatchQueue);
        }

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            StopKeyRepeat();
            DisconnectFromSurface();
        }

        /// <summary>
        /// Tear down the worker-side surface proxy. Subclasses null out their
        /// typed handle/proxy fields (and the corresponding fields on
        /// <see cref="Parent"/>) and post a disconnect to the worker.
        /// </summary>
        protected abstract void DisconnectFromSurface();

        protected IInputRoot? InputRoot => Parent.InputRoot;
        protected TouchDevice Touch => Parent.Touch;
        protected MouseDevice Mouse => Parent.Mouse;
        protected KeyboardDevice Keyboard => Parent.Keyboard;

        protected void ScheduleInput(RawInputEventArgs args) => RawEventGrouper.HandleEvent(args);

        protected virtual void DispatchInput(RawInputEventArgs args)
        {
            if (IsDisposed || InputRoot is null)
                return;

            if (!Parent.IsEnabled)
            {
                OnInputWhileDisabled();
                return;
            }

            if (HandleKeyboardDispatch(args))
                return;

            if (HandleDragDropDispatch(args))
                return;

            if (HandleSurfaceSpecificDispatch(args))
                return;

            Parent.Input?.Invoke(args);

            if (HandleDragDropPostDispatch(args))
                return;

            if (!args.Handled && args is RawKeyEventArgs { KeySymbol: { } text, Type: RawKeyEventType.KeyDown })
                Parent.Input?.Invoke(new RawTextInputEventArgs(Keyboard, args.Timestamp, InputRoot, text));
        }

        /// <summary>
        /// Lets a derived sink intercept input before it reaches the framework.
        /// Used by <see cref="WindowImpl.Sink"/> for title-bar move and edge resize chrome.
        /// </summary>
        protected virtual bool HandleSurfaceSpecificDispatch(RawInputEventArgs args) => false;

        protected virtual void OnInputWhileDisabled() { }

        // Configure/close are surface-role-specific (xdg_toplevel vs xdg_popup
        // carry different payloads). Subclasses implement the matching
        // worker→UI interface (IWXdgTopLevelEventSink / IWXdgPopupEventSink).
        public virtual void OnScaleChanged(double scale)
        {
            if (IsDisposed)
                return;
            Parent.UpdateScaling(scale);
        }

        public virtual void OnSurfaceOutputsChanged(IReadOnlyList<object> outputIds)
        {
            if (IsDisposed)
                return;
            Parent.CurrentOutputIds = outputIds;
        }
    }
}
