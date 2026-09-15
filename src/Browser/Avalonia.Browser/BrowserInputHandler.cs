using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Browser;

internal class BrowserInputHandler
{
    private readonly BrowserTopLevelImpl _topLevelImpl;
    private readonly JSObject _container;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly TouchDevice _touchDevice;
    private readonly PenDevice _penDevice;
    private readonly MouseDevice _wheelMouseDevice;
    private readonly List<BrowserMouseDevice> _mouseDevices;
    private readonly RawEventGrouper _rawEventGrouper;
    private IInputRoot? _inputRoot;

    public BrowserInputHandler(BrowserTopLevelImpl topLevelImpl, JSObject container, JSObject inputElement, int topLevelId)
    {
        _topLevelImpl = topLevelImpl;
        _container = container ?? throw new ArgumentNullException(nameof(container));

        _touchDevice = new TouchDevice();
        _penDevice = new PenDevice();
        _wheelMouseDevice = new MouseDevice();
        _mouseDevices = new();

        _rawEventGrouper = new RawEventGrouper(DispatchInput, BrowserWindowingPlatform.EventGrouperDispatchQueue);

        TextInputMethod = new BrowserTextInputMethod(this, container, inputElement);
        InputPane = new BrowserInputPane();

        InputHelper.SubscribeInputEvents(container, topLevelId);
    }

    public BrowserTextInputMethod TextInputMethod { get; }
    public BrowserInputPane InputPane { get; }
    
    public ulong Timestamp => (ulong)_sw.ElapsedMilliseconds;

    internal void SetInputRoot(IInputRoot inputRoot)
    {
        _inputRoot = inputRoot;
    }

    internal void OnQueuedPointerEvent(
        RawPointerEventType eventType, BrowserPointerType pointerType, long pointerId, ulong timestamp,
        RawPointerPoint point, RawInputModifiers modifiers, PooledList<RawPointerPoint>? intermediatePoints)
    {
        if (_inputRoot is null)
        {
            intermediatePoints?.Dispose();
            return;
        }

        var device = GetPointerDevice(pointerType, pointerId);
        var lazyPoints = intermediatePoints is null
            ? null
            : new Lazy<IReadOnlyList<RawPointerPoint>?>(intermediatePoints);

        var args = device is TouchDevice
            ? new RawTouchEventArgs(device, timestamp, _inputRoot, eventType, point, modifiers, pointerId)
            {
                IntermediatePoints = lazyPoints
            }
            : new RawPointerEventArgs(device, timestamp, _inputRoot, eventType, point, modifiers)
            {
                RawPointerId = pointerId, IntermediatePoints = lazyPoints
            };

        _rawEventGrouper.HandleEvent(args);
    }

    internal void OnQueuedWheelEvent(ulong timestamp, Point position, double deltaX, double deltaY, RawInputModifiers modifiers)
    {
        if (_inputRoot is null)
            return;

        var args = new RawMouseWheelEventArgs(_wheelMouseDevice, timestamp, _inputRoot, position,
            new Vector(-(deltaX / 50), -(deltaY / 50)), modifiers);

        _rawEventGrouper.HandleEvent(args);
    }

    internal void OnQueuedKeyEvent(RawKeyEventType type, ulong timestamp, string domCode, string domKey, RawInputModifiers modifiers)
    {
        if (_inputRoot is null)
            return;

        var physicalKey = KeyInterop.PhysicalKeyFromDomCode(domCode);
        var key = KeyInterop.KeyFromDomKey(domKey, physicalKey);
        var keySymbol = KeyInterop.KeySymbolFromDomKey(domKey);

        var args = new RawKeyEventArgs(
            BrowserWindowingPlatform.Keyboard,
            timestamp,
            _inputRoot,
            type,
            key,
            modifiers,
            physicalKey,
            keySymbol
        );

        _rawEventGrouper.HandleEvent(args);
    }

    public bool OnDragEvent(string type, double offsetX, double offsetY, int modifiers, JSObject dataTransfer, JSObject items)
    {
        var eventType = type switch
        {
            "dragenter" => RawDragEventType.DragEnter,
            "dragover" => RawDragEventType.DragOver,
            "dragleave" => RawDragEventType.DragLeave,
            "drop" => RawDragEventType.Drop,
            _ => (RawDragEventType)(int)-1
        };
        if (eventType < 0)
        {
            return false;
        }

        // If file is dropped, we need storage js to be referenced.
        // TODO: restructure JS files, so it's not needed.
        _ = AvaloniaModule.ImportStorage();

        var position = new Point(offsetX, offsetY);

        var effectAllowedStr = dataTransfer.GetPropertyAsString("effectAllowed") ?? "none";
        var effectAllowed = DragDropEffects.None;

        if (effectAllowedStr.Contains("copy", StringComparison.OrdinalIgnoreCase))
        {
            effectAllowed |= DragDropEffects.Copy;
        }

        if (effectAllowedStr.Contains("link", StringComparison.OrdinalIgnoreCase))
        {
            effectAllowed |= DragDropEffects.Link;
        }

        if (effectAllowedStr.Contains("move", StringComparison.OrdinalIgnoreCase))
        {
            effectAllowed |= DragDropEffects.Move;
        }

        if (effectAllowedStr.Equals("all", StringComparison.OrdinalIgnoreCase)
            || effectAllowedStr.Equals("uninitialized", StringComparison.OrdinalIgnoreCase))
        {
            effectAllowed |= DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link;
        }

        if (effectAllowed == DragDropEffects.None)
        {
            return false;
        }

        var dropEffect = RawDragEvent(eventType, position, (RawInputModifiers)modifiers, new BrowserDragDataTransfer(items), effectAllowed);
        dataTransfer.SetProperty("dropEffect", dropEffect.ToString().ToLowerInvariant());

        // Note, due to complications of JS interop, we ignore this return value.
        // And instead assume, that event is handled for any "drop" and "drag-over" stages.
        return eventType is RawDragEventType.Drop or RawDragEventType.DragOver
               && dropEffect != DragDropEffects.None;
    }

    internal bool RawTextEvent(string text)
    {
        if (_inputRoot is { })
        {
            var args = new RawTextInputEventArgs(BrowserWindowingPlatform.Keyboard, Timestamp, _inputRoot, text);
            ScheduleDirectInput(args);

            return args.Handled;
        }

        return false;
    }

    private DragDropEffects RawDragEvent(RawDragEventType eventType, Point position, RawInputModifiers modifiers,
        BrowserDragDataTransfer dataTransfer, DragDropEffects dropEffect)
    {
        var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
        var eventArgs = new RawDragEvent(device, eventType, _inputRoot!, position, dataTransfer, dropEffect, modifiers);
        ScheduleDirectInput(eventArgs);
        return eventArgs.Effects;
    }

    private IPointerDevice GetPointerDevice(BrowserPointerType pointerType, long pointerId)
    {
        if (pointerType == BrowserPointerType.Touch)
            return _touchDevice;
        if (pointerType == BrowserPointerType.Pen)
            return _penDevice;

        // TODO: refactor pointer devices, so we can reuse single instance here.
        foreach (var mouseDevice in _mouseDevices)
        {
            if (mouseDevice.PointerId == pointerId)
                return mouseDevice;
        }

        var newMouseDevice = new BrowserMouseDevice(pointerId, _container);
        _mouseDevices.Add(newMouseDevice);
        return newMouseDevice;
    }

    /// <summary>
    /// Direct-path events bypass the ring. On the single-threaded runtime they are dispatched inline,
    /// because the caller needs a synchronous result (drop effect, handled state) and the JS side has
    /// already flushed the ring so ordering is preserved. With the managed dispatcher they are
    /// queued like everything else, and the result is not observable.
    /// </summary>
    private void ScheduleDirectInput(RawInputEventArgs args)
    {
        if (BrowserWindowingPlatform.IsThreadingEnabled)
            _rawEventGrouper.HandleEvent(args);
        else
            DispatchInput(args);
    }

    private void DispatchInput(RawInputEventArgs args)
    {
        if (_inputRoot is null)
            return;

        _topLevelImpl.Input?.Invoke(args);

        // An unhandled key press with a printable symbol is delivered as text input as well.
        // The handled state of the text event is what the browser gets back for preventDefault().
        if (args is RawKeyEventArgs { Type: RawKeyEventType.KeyDown, Handled: false, KeySymbol: { Length: 1 } symbol })
        {
            var textArgs = new RawTextInputEventArgs(BrowserWindowingPlatform.Keyboard, args.Timestamp, _inputRoot, symbol);
            _topLevelImpl.Input?.Invoke(textArgs);
            args.Handled = textArgs.Handled;
        }
    }
}
