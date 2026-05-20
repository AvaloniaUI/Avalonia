using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using global::Avalonia;
using global::Avalonia.Controls.Embedding;
using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
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
    }

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
        };
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
        => _textInputMethod?.OnPanelFocusChanged(false);

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

        var keyboard = GetKeyboardDevice();
        if (keyboard is null)
            return;

        var args = new RawKeyEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot,
            type, key, GetCurrentModifiers(), physicalKey, keySymbol);
        input(args);

        // Only mark handled if Avalonia consumed the event — marking a KeyDown
        // handled suppresses the matching CharacterReceived and breaks text input.
        if (args.Handled)
            e.Handled = true;
    }

    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
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
