using System;
using System.Runtime.InteropServices;
using global::Avalonia;
using global::Avalonia.Controls.Embedding;
using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AvControl = global::Avalonia.Controls.Control;
using AvSize = global::Avalonia.Size;
using AvPoint = global::Avalonia.Point;
using AvVector = global::Avalonia.Vector;
using AvRect = global::Avalonia.Rect;

namespace Avalonia.WinUI;

public partial class AvaloniaSwapChainPanel : SwapChainPanel
{
    private SwapChainGlSurface? _glSurface;
    private SwapChainPanelTopLevelImpl? _topLevelImpl;
    private EmbeddableControlRoot? _root;
    private AvControl? _content;
    private readonly MouseDevice _mouseDevice = new();
    private readonly TouchDevice _touchDevice = new();
    private readonly PenDevice _penDevice = new(releasePointerOnPenUp: false);
    private PixelSize _cachedPixelSize = new(1, 1);
    private double _cachedScaling = 1.0;

    public AvaloniaSwapChainPanel()
    {
        IsTabStop = true;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
        CompositionScaleChanged += OnCompositionScaleChanged;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        PointerCanceled += OnPointerCanceled;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        CharacterReceived += OnCharacterReceived;
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
        _topLevelImpl = new SwapChainPanelTopLevelImpl(_glSurface);
        _topLevelImpl.ClientSize = new AvSize(ActualWidth, ActualHeight);
        _topLevelImpl.RenderScaling = CompositionScaleX;

        // Create and start the EmbeddableControlRoot
        _root = new EmbeddableControlRoot(_topLevelImpl);
        _root.Content = _content;
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

    private RawPointerPoint CreateRawPointerPoint(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
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
        var rawPoint = CreateRawPointerPoint(e);
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
        var rawPoint = CreateRawPointerPoint(e);
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        var type = device is TouchDevice ? RawPointerEventType.TouchUpdate : RawPointerEventType.Move;

        input(CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId));
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e);
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

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var device = GetPointerDevice(e);
        var timestamp = GetTimestamp(e);
        var rawPoint = CreateRawPointerPoint(e);
        var modifiers = GetPointerModifiers(e);
        var pointerId = e.Pointer.PointerId;

        var type = device is TouchDevice ? RawPointerEventType.TouchCancel : RawPointerEventType.LeaveWindow;

        input(CreatePointerArgs(device, timestamp, inputRoot, type, rawPoint, modifiers, pointerId));
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var key = WinUIKeyInterop.KeyFromVirtualKey(e.Key);
        if (key != Key.None)
        {
            var keyboard = GetKeyboardDevice();
            if (keyboard is null) return;
            input(new RawKeyEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot,
                RawKeyEventType.KeyDown, key, GetCurrentModifiers(),
                PhysicalKey.None, null));
            e.Handled = true;
        }
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (_topLevelImpl?.Input is not { } input || _topLevelImpl.InputRoot is not { } inputRoot)
            return;

        var key = WinUIKeyInterop.KeyFromVirtualKey(e.Key);
        if (key != Key.None)
        {
            var keyboard = GetKeyboardDevice();
            if (keyboard is null) return;
            input(new RawKeyEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot,
                RawKeyEventType.KeyUp, key, GetCurrentModifiers(),
                PhysicalKey.None, null));
            e.Handled = true;
        }
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
