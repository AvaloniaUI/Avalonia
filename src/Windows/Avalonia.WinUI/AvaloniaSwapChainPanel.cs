using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using global::Avalonia;
using global::Avalonia.Controls.Embedding;
using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
using global::Avalonia.Logging;
using global::Avalonia.Platform;
using global::Avalonia.Win32;
using global::Avalonia.Win32.OpenGl.Angle;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using WinUIPointerPoint = Microsoft.UI.Input.PointerPoint;
using AvControl = global::Avalonia.Controls.Control;
using AvSize = global::Avalonia.Size;
using AvPoint = global::Avalonia.Point;
using AvVector = global::Avalonia.Vector;
using AvRect = global::Avalonia.Rect;

namespace Avalonia.WinUI;

public partial class AvaloniaSwapChainPanel : SwapChainPanel
{
    private SwapChainGlSurface? _glSurface;
    private SwapChainTopLevelImpl? _topLevelImpl;
    private EmbeddableControlRoot? _root;
    private AvControl? _content;
    private readonly MouseDevice _mouseDevice = new();
    private readonly TouchDevice _touchDevice = new();
    private readonly PenDevice _penDevice = new(releasePointerOnPenUp: false);
    private PixelSize _cachedPixelSize = new(1, 1);
    private double _cachedScaling = 1.0;

    private static readonly List<RawPointerPoint> s_intermediatePoints = new();

    private WinUITextInputMethod? _textInputMethod;
    private bool _ignoreCharacterReceived;

    private readonly CursorOverlay _cursorOverlay;

    // ProtectedCursor is protected on UIElement; derive to expose it.
    private sealed partial class CursorOverlay : Microsoft.UI.Xaml.Controls.Grid
    {
        public void SetCursor(Microsoft.UI.Input.InputCursor? cursor) => ProtectedCursor = cursor;
    }

    static AvaloniaSwapChainPanel()
    {
        AvaloniaLocator.CurrentMutable
            .Bind<ICursorFactory>()
            .ToConstant(WinUICursorFactory.Instance);
        AvaloniaLocator.CurrentMutable
            .Bind<global::Avalonia.Input.Platform.IPlatformDragSource>()
            .ToSingleton<WinUIDragSource>();
    }

    // Looking up panels from the drag source: keyed by the Avalonia toplevel
    // PlatformImpl so multi-panel hosts work.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<
        global::Avalonia.Platform.ITopLevelImpl, AvaloniaSwapChainPanel> s_panelsByImpl = new();

    internal static AvaloniaSwapChainPanel? GetPanelFor(global::Avalonia.Platform.ITopLevelImpl impl)
        => s_panelsByImpl.TryGetValue(impl, out var p) ? p : null;

    // Most recent WinUI PointerPoint observed by the panel — needed by
    // StartDragAsync. Avalonia's IPointer.Id is assigned sequentially and
    // does not correspond to WinUI's PointerId, so we can't look up per
    // pointer; the latest is good enough for the dominant mouse-drag case.
    private WinUIPointerPoint? _lastPointerPoint;

    // Cleared in DragStarting and OnUnloaded.
    private IDataTransfer? _outgoingDragData;
    private DragDropEffects _outgoingDragAllowed;

    public AvaloniaSwapChainPanel()
    {
        IsTabStop = true;
        // SwapChainPanel disallows Background, but without a hit-testable
        // surface WinUI won't resolve ProtectedCursor for the panel. Add a
        // transparent overlay that fills the panel — it costs nothing
        // visually but participates in cursor hit-testing.
        _cursorOverlay = new CursorOverlay
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            IsHitTestVisible = true,
            AllowDrop = true,
            CanDrag = true,
        };
        _cursorOverlay.DragEnter += OnDragEnter;
        _cursorOverlay.DragOver += OnDragOver;
        _cursorOverlay.DragLeave += OnDragLeave;
        _cursorOverlay.Drop += OnDrop;
        _cursorOverlay.DragStarting += OnDragStarting;
        Children.Add(_cursorOverlay);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        CompositionScaleChanged += OnCompositionScaleChanged;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerCanceled += OnPointerCanceled;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
        AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), handledEventsToo: true);
        AddHandler(KeyUpEvent, new KeyEventHandler(OnKeyUp), handledEventsToo: true);
        AddHandler(CharacterReceivedEvent,
            new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>(OnCharacterReceived),
            handledEventsToo: true);
    }

    public AvControl? Content
    {
        get => _content;
        set
        {
            _content = value;
            if (_root is not null)
                _root.Content = value;
        }
    }

    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new global::Avalonia.WinUI.Automation.AvaloniaSwapChainPanelAutomationPeer(this);

    // ---- Diagnostics / internal automation hooks ----

    /// <summary>
    /// Returns the embedded control root, or null if the panel has not loaded yet.
    /// Used by the automation peer to enumerate Avalonia children; also exposed as
    /// a diagnostic surface for WinUIEmbedSample to verify peer lifecycle.
    /// </summary>
    internal global::Avalonia.Controls.Embedding.EmbeddableControlRoot? GetEmbeddedRootForAutomation() => _root;

    /// <summary>
    /// Diagnostic accessor for the Avalonia TopLevel's render scaling — used by
    /// WinUIEmbedSample to verify it stays in sync with <c>XamlRoot.RasterizationScale</c>.
    /// Returns NaN if the panel has not loaded yet.
    /// </summary>
    public double GetAvaloniaRenderScalingForDiagnostics()
        => _topLevelImpl?.RenderScaling ?? double.NaN;

    private void UpdateCachedSize()
    {
        var w = Math.Max(1, (int)(ActualWidth * CompositionScaleX));
        var h = Math.Max(1, (int)(ActualHeight * CompositionScaleY));
        _cachedPixelSize = new PixelSize(w, h);
        _cachedScaling = CompositionScaleX;
    }

    private PixelSize GetPixelSize() => _cachedPixelSize;

    private double GetScaling() => _cachedScaling;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_glSurface is not null)
            return;

        UpdateCachedSize();

        // Create the GL surface — swap chain creation is deferred to CreateGlRenderTarget
        // where we have the actual rendering context's D3D device
        _glSurface = new SwapChainGlSurface(GetPixelSize, GetScaling, OnSwapChainCreated);
        _topLevelImpl = new SwapChainTopLevelImpl(_glSurface)
        {
            ClientSize = new AvSize(ActualWidth, ActualHeight),
            RenderScaling = CompositionScaleX
        };

        _textInputMethod = new WinUITextInputMethod(
            this,
            () => _topLevelImpl?.Input,
            () => _topLevelImpl?.InputRoot,
            () => AvaloniaLocator.Current.GetService<IKeyboardDevice>());
        _topLevelImpl.TextInputMethod = _textInputMethod;
        _topLevelImpl.CursorChanged = OnAvaloniaCursorChanged;

        // Create and start the EmbeddableControlRoot
        _root = new EmbeddableControlRoot(_topLevelImpl)
        {
            Content = _content
        };
        _root.Prepare();
        _root.StartRendering();

        s_panelsByImpl[_topLevelImpl] = this;
    }

    private unsafe void OnSwapChainCreated(IntPtr swapChainPtr)
    {
        // Called from the render thread when the swap chain is first created.
        // Set it on the panel via ISwapChainPanelNative COM interop.
        DispatcherQueue.TryEnqueue(() =>
        {
            var panelUnknown = Marshal.GetIUnknownForObject(this);
            try
            {
                var iid = new Guid("63aad0b8-7c24-40ff-85a8-640d944cc325");
                Marshal.QueryInterface(panelUnknown, in iid, out var nativePtr);
                if (nativePtr != IntPtr.Zero)
                {
                    try
                    {
                        var vtable = *(IntPtr**)nativePtr;
                        var setSwapChain = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)vtable[3];
                        var hr = setSwapChain(nativePtr, swapChainPtr);
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    finally
                    {
                        Marshal.Release(nativePtr);
                    }
                }
            }
            finally
            {
                Marshal.Release(panelUnknown);
            }
        });
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_topLevelImpl is not null)
            s_panelsByImpl.TryRemove(_topLevelImpl, out _);
        _root?.StopRendering();
        _root?.Dispose();
        _root = null;
        _topLevelImpl = null;
        _glSurface?.DisposeSwapChain();
        _glSurface = null;
        _mouseDevice.Dispose();
        _touchDevice.Dispose();
        _penDevice.Dispose();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCachedSize();
        if (_topLevelImpl is not null)
            _topLevelImpl.ClientSize = new AvSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        UpdateCachedSize();
        if (_topLevelImpl is not null)
            _topLevelImpl.RenderScaling = CompositionScaleX;
    }

    // Input forwarding

    private IPointerDevice GetPointerDevice(PointerRoutedEventArgs e)
    {
        return e.Pointer.PointerDeviceType switch
        {
            Microsoft.UI.Input.PointerDeviceType.Touch => _touchDevice,
            Microsoft.UI.Input.PointerDeviceType.Pen => _penDevice,
            _ => _mouseDevice
        };
    }

    private static RawPointerPoint CreateRawPointerPoint(WinUIPointerPoint point)
    {
        var props = point.Properties;
        var pos = point.Position;

        var rawPoint = new RawPointerPoint
        {
            Position = new AvPoint(pos.X, pos.Y),
            Pressure = props.Pressure,
            Twist = props.Twist,
            XTilt = props.XTilt,
            YTilt = props.YTilt,
        };

        if (props.ContactRect is { Width: > 0 } or { Height: > 0 })
        {
            var cr = props.ContactRect;
            rawPoint.ContactRect = new AvRect(cr.X, cr.Y, cr.Width, cr.Height);
        }

        return rawPoint;
    }

    private RawPointerEventArgs CreatePointerArgs(
        IInputDevice device, ulong timestamp, IInputRoot inputRoot,
        RawPointerEventType type, RawPointerPoint point,
        RawInputModifiers modifiers, uint pointerId)
    {
        return device is TouchDevice
            ? new RawTouchEventArgs(device, timestamp, inputRoot, type, point, modifiers, pointerId)
            : new RawPointerEventArgs(device, timestamp, inputRoot, type, point, modifiers)
            {
                RawPointerId = pointerId
            };
    }

    private ulong GetTimestamp(PointerRoutedEventArgs e)
    {
        // WinUI PointerPoint.Timestamp is in microseconds; Avalonia expects milliseconds.
        return e.GetCurrentPoint(this).Timestamp / 1000;
    }

    private RawInputModifiers GetPointerModifiers(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var props = point.Properties;
        var mods = WinUIKeyInterop.ModifiersFromVirtualKeyModifiers(e.KeyModifiers);
        if (props.IsLeftButtonPressed)
            mods |= RawInputModifiers.LeftMouseButton;
        if (props.IsRightButtonPressed)
            mods |= RawInputModifiers.RightMouseButton;
        if (props.IsMiddleButtonPressed)
            mods |= RawInputModifiers.MiddleMouseButton;

        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            if (props.IsBarrelButtonPressed)
                mods |= RawInputModifiers.PenBarrelButton;
            if (props.IsEraser)
                mods |= RawInputModifiers.PenEraser;
            if (props.IsInverted)
                mods |= RawInputModifiers.PenInverted;
        }

        return mods;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _lastPointerPoint = e.GetCurrentPoint(this);
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        RawPointerEventType type;
        if (device is TouchDevice)
        {
            type = RawPointerEventType.TouchBegin;
        }
        else
        {
            var point = e.GetCurrentPoint(this);
            var props = point.Properties;
            if (props.IsLeftButtonPressed)
                type = RawPointerEventType.LeftButtonDown;
            else if (props.IsRightButtonPressed)
                type = RawPointerEventType.RightButtonDown;
            else if (props.IsMiddleButtonPressed)
                type = RawPointerEventType.MiddleButtonDown;
            else
                return;
        }

        Focus(FocusState.Pointer);
        CapturePointer(e.Pointer);

        input(CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId));
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _lastPointerPoint = e.GetCurrentPoint(this);
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        var type = device is TouchDevice ? RawPointerEventType.TouchUpdate : RawPointerEventType.Move;

        var args = CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId);
        args.IntermediatePoints = new Lazy<IReadOnlyList<RawPointerPoint>?>(() => GetIntermediatePoints(e));
        input(args);
    }

    private IReadOnlyList<RawPointerPoint>? GetIntermediatePoints(PointerRoutedEventArgs e)
    {
        // WinUI returns the points oldest-first and includes the current point as the
        // last entry; drop that last entry (it is the one we already dispatched).
        var coalesced = e.GetIntermediatePoints(this);
        if (coalesced is null || coalesced.Count <= 1)
            return null;

        s_intermediatePoints.Clear();
        if (s_intermediatePoints.Capacity < coalesced.Count - 1)
            s_intermediatePoints.Capacity = coalesced.Count - 1;

        for (var i = 0; i < coalesced.Count - 1; i++)
            s_intermediatePoints.Add(CreateRawPointerPoint(coalesced[i]));

        return s_intermediatePoints;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        RawPointerEventType type;
        if (device is TouchDevice)
        {
            type = RawPointerEventType.TouchEnd;
        }
        else
        {
            var point = e.GetCurrentPoint(this);
            var props = point.Properties;
            switch (props.PointerUpdateKind)
            {
                case Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased:
                    type = RawPointerEventType.LeftButtonUp;
                    break;
                case Microsoft.UI.Input.PointerUpdateKind.RightButtonReleased:
                    type = RawPointerEventType.RightButtonUp;
                    break;
                case Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased:
                    type = RawPointerEventType.MiddleButtonUp;
                    break;
                default:
                    return;
            }
        }

        ReleasePointerCapture(e.Pointer);
        input(CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId));
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var point = e.GetCurrentPoint(this);
        var delta = point.Properties.MouseWheelDelta;
        var pos = point.Position;

        input(new RawMouseWheelEventArgs(device as MouseDevice ?? _mouseDevice, timestamp, inputRoot,
            new AvPoint(pos.X, pos.Y), new AvVector(0, delta), GetPointerModifiers(e)));
        e.Handled = true;
    }

    private void OnAvaloniaCursorChanged(ICursorImpl? cursor)
    {
        _cursorOverlay.SetCursor((cursor as WinUICursorImpl)?.Cursor);
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
        => _textInputMethod?.OnPanelFocusChanged(true);

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _textInputMethod?.OnPanelFocusChanged(false);
        // Clear Avalonia's internal focus so the previously focused control
        // doesn't keep its :focus visual once keyboard focus moves to a
        // native WinUI element.
        if (_root is not null)
            global::Avalonia.Input.FocusManager.GetFocusManager(_root)?.Focus(null);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Touch never hovers — enter is implied by the press that produced it.
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
            return;
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        input(CreatePointerArgs(device, timestamp, inputRoot,
            RawPointerEventType.Move, rawPoint, modifiers, pointerId));
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
            return;
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        input(CreatePointerArgs(device, timestamp, inputRoot,
            RawPointerEventType.LeaveWindow, rawPoint, modifiers, pointerId));
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e.GetCurrentPoint(this));
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        var type = device is TouchDevice ? RawPointerEventType.TouchCancel : RawPointerEventType.LeaveWindow;

        input(CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId));
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        => DispatchKey(e, RawKeyEventType.KeyDown);

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
        => DispatchKey(e, RawKeyEventType.KeyUp);

    private void DispatchKey(KeyRoutedEventArgs e, RawKeyEventType type)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var (key, physicalKey, keySymbol) = WinUIKeyInterop.Resolve(e.Key, e.KeyStatus);
        if (key == Key.None && physicalKey == PhysicalKey.None)
            return;

        // Tab boundary: when Avalonia would wrap focus inside its toplevel
        // (TopLevelHost.TabNavigation = Cycle), forward to WinUI so focus
        // escapes to the next/previous native control instead.
        if (type == RawKeyEventType.KeyDown && key == Key.Tab && TryEscapeTabBoundary(e))
            return;

        var keyboard = GetKeyboardDevice();
        if (keyboard is null)
            return;

        var args = new RawKeyEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot,
            type, key, GetCurrentModifiers(), physicalKey, keySymbol);
        input(args);

        if (type == RawKeyEventType.KeyDown)
            _ignoreCharacterReceived = key == Key.ImeProcessed || args.Handled;
        else if (type == RawKeyEventType.KeyUp)
            _ignoreCharacterReceived = false;

        // Only mark handled if Avalonia consumed the event — marking a KeyDown
        // handled suppresses the matching CharacterReceived and breaks text input.
        if (args.Handled)
            e.Handled = true;
    }

    /// <summary>
    /// If the current Avalonia focus is at the first or last tab stop in the
    /// embedded content, handle Tab / Shift+Tab here by moving WinUI focus to
    /// the next or previous native element. Returns true if the event was
    /// consumed (the caller must not forward it to Avalonia).
    /// </summary>
    private bool TryEscapeTabBoundary(KeyRoutedEventArgs e)
    {
        if (_root is null)
            return false;

        var focusManager = global::Avalonia.Input.FocusManager.GetFocusManager(_root);
        var current = focusManager?.GetFocusedElement();
        if (current is null)
            return false;

        var shift = global::Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        var first = global::Avalonia.Input.KeyboardNavigationHandler.GetNext(
            _root, global::Avalonia.Input.NavigationDirection.Next);
        if (first is null)
            return false;

        bool atBoundary;
        if (shift)
        {
            // At the first tab stop — backward tab would wrap.
            atBoundary = ReferenceEquals(current, first);
        }
        else
        {
            // At the last tab stop — forward tab would wrap. Cycle mode means
            // GetNext from the last returns the first; detect that.
            var next = global::Avalonia.Input.KeyboardNavigationHandler.GetNext(
                current, global::Avalonia.Input.NavigationDirection.Next);
            atBoundary = next is null || ReferenceEquals(next, first);
        }

        if (!atBoundary)
            return false;

        var direction = shift
            ? Microsoft.UI.Xaml.Input.FocusNavigationDirection.Previous
            : Microsoft.UI.Xaml.Input.FocusNavigationDirection.Next;

        // WinUI Desktop requires SearchRoot — the parameterless TryMoveFocus
        // overload only works in UWP. Use the panel's XamlRoot.Content as the
        // search scope; that covers the whole window's visual tree.
        var searchRoot = XamlRoot?.Content as DependencyObject;
        if (searchRoot is null)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(this, "Tab boundary: no XamlRoot.Content available; letting Avalonia handle.");
            return false;
        }

        var options = new Microsoft.UI.Xaml.Input.FindNextElementOptions
        {
            SearchRoot = searchRoot,
        };
        var moved = Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(direction, options);
        if (moved)
        {
            e.Handled = true;
            Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(this, $"Tab boundary: moved WinUI focus {direction}.");
            return true;
        }

        Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(this, "Tab boundary detected but TryMoveFocus returned false; letting Avalonia handle.");
        return false;
    }

    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        if (_ignoreCharacterReceived)
            return;

        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var keyboard = GetKeyboardDevice();
        if (keyboard is null) return;

        var ch = e.Character;
        if (!char.IsControl(ch) || ch == '\r' || ch == '\n' || ch == '\t')
        {
            input(new RawTextInputEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot,
                new string(ch, 1)));
            e.Handled = true;
        }
    }

    // Incoming drag-and-drop
    private DataTransfer? _activeDrag;

    private async void OnDragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            ((IDisposable?)_activeDrag)?.Dispose();
            _activeDrag = await BuildDataTransferAsync(e.DataView);
            UpdateDragUi(e, RawDragEventType.DragEnter);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        => UpdateDragUi(e, RawDragEventType.DragOver);

    private void OnDragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        UpdateDragUi(e, RawDragEventType.DragLeave);
        ((IDisposable?)_activeDrag)?.Dispose();
        _activeDrag = null;
    }

    private void OnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        UpdateDragUi(e, RawDragEventType.Drop);
        ((IDisposable?)_activeDrag)?.Dispose();
        _activeDrag = null;
    }

    private void UpdateDragUi(Microsoft.UI.Xaml.DragEventArgs e, RawDragEventType type)
    {
        if (_topLevelImpl?.Input is not { } input
            || _topLevelImpl.InputRoot is not { } inputRoot
            || _activeDrag is null)
            return;

        var device = AvaloniaLocator.Current.GetService<IDragDropDevice>();
        if (device is null)
            return;

        var pt = e.GetPosition(this);
        var allowed = (DragDropEffects)(int)e.AllowedOperations;
        if (allowed == DragDropEffects.None)
            allowed = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;
        var modifiers = GetCurrentModifiers();

        var args = new RawDragEvent(device, type, inputRoot,
            new AvPoint(pt.X, pt.Y), _activeDrag, allowed, modifiers);
        input(args);

        e.AcceptedOperation = (Windows.ApplicationModel.DataTransfer.DataPackageOperation)(int)args.Effects;
        e.Handled = true;
    }

    private static async System.Threading.Tasks.Task<DataTransfer> BuildDataTransferAsync(
        Windows.ApplicationModel.DataTransfer.DataPackageView view)
    {
        var dt = new DataTransfer();

        if (view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            try
            {
                var items = await view.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    var path = item.Path;
                    if (string.IsNullOrEmpty(path))
                        continue;
                    // Reuse Avalonia's BclStorage* via the same helper Win32 uses
                    // (IVT-granted to Avalonia.WinUI on Avalonia.Base).
                    if (global::Avalonia.Platform.Storage.FileIO.StorageProviderHelpers
                        .TryCreateBclStorageItem(path) is { } storage)
                    {
                        dt.Add(DataTransferItem.CreateFile(storage));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(null, "Failed to resolve dragged storage items: {Exception}", ex);
            }
        }

        if (view.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
        {
            try
            {
                var text = await view.GetTextAsync();
                if (!string.IsNullOrEmpty(text))
                    dt.Add(DataTransferItem.CreateText(text));
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(null, "Failed to resolve dragged text: {Exception}", ex);
            }
        }

        return dt;
    }

    // Outgoing drag — invoked by WinUIDragSource on the UI thread.
    internal async Task<DragDropEffects> StartOutgoingDragAsync(
        IDataTransfer data, DragDropEffects allowed)
    {
        if (_lastPointerPoint is not { } pp)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(this, "StartOutgoingDragAsync called with no cached PointerPoint; drag suppressed.");
            return DragDropEffects.None;
        }

        _outgoingDragData = data;
        _outgoingDragAllowed = allowed;
        try
        {
            var op = await _cursorOverlay.StartDragAsync(pp);
            return (DragDropEffects)(int)op;
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(this, "StartDragAsync threw: {Exception}", ex);
            return DragDropEffects.None;
        }
        finally
        {
            _outgoingDragData = null;
            _outgoingDragAllowed = DragDropEffects.None;
        }
    }

    private async void OnDragStarting(UIElement sender, DragStartingEventArgs e)
    {
        if (_outgoingDragData is null)
        {
            // The overlay was the drag origin (CanDrag=true) but no Avalonia
            // drag was in flight — most likely a user grab on empty panel
            // space. Suppress the native drag rather than emit an empty one.
            Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(this, "DragStarting cancelled: no active Avalonia drag.");
            e.Cancel = true;
            return;
        }

        e.AllowedOperations = (Windows.ApplicationModel.DataTransfer.DataPackageOperation)(int)_outgoingDragAllowed;
        var deferral = e.GetDeferral();
        try
        {
            await PopulateDataPackageAsync(e.Data, _outgoingDragData);
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(this, "Populating outgoing DataPackage threw: {Exception}", ex);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static async Task PopulateDataPackageAsync(
        Windows.ApplicationModel.DataTransfer.DataPackage package, IDataTransfer source)
    {
        // Text — direct copy.
        var text = source.TryGetValue(DataFormat.Text);
        if (!string.IsNullOrEmpty(text))
            package.SetText(text);

        // Files — convert Avalonia IStorageItem paths to native
        // Windows.Storage.IStorageItem instances via the OS APIs.
        var avFiles = source.TryGetValues(DataFormat.File);
        if (avFiles is not null)
        {
            var winuiItems = new System.Collections.Generic.List<Windows.Storage.IStorageItem>();
            foreach (var av in avFiles)
            {
                string? path = null;
                if (av.Path is { IsAbsoluteUri: true, Scheme: "file" } uri)
                    path = uri.LocalPath;
                if (string.IsNullOrEmpty(path))
                    continue;
                try
                {
                    Windows.Storage.IStorageItem item =
                        System.IO.Directory.Exists(path)
                            ? await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path)
                            : await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                    winuiItems.Add(item);
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(null, "Failed to expose dragged path '{Path}' as Windows.Storage item: {Exception}", path, ex);
                }
            }
            if (winuiItems.Count > 0)
                package.SetStorageItems(winuiItems);
        }
    }

    private static IKeyboardDevice? GetKeyboardDevice()
        => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

    private static RawInputModifiers GetCurrentModifiers()
    {
        var mods = RawInputModifiers.None;
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        if (ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            mods |= RawInputModifiers.Control;
        var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
        if (shiftState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            mods |= RawInputModifiers.Shift;
        var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
        if (altState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            mods |= RawInputModifiers.Alt;
        return mods;
    }
}
