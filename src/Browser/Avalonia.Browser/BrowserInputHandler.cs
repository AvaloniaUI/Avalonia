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
    private IInputRoot? _inputRoot;

    private static readonly PooledList<RawPointerPoint> s_intermediatePointsPooledList = new(ClearMode.Never);
    private readonly RawEventGrouper? _rawEventGrouper;

    public BrowserInputHandler(BrowserTopLevelImpl topLevelImpl, JSObject container, JSObject inputElement, int topLevelId)
    {
        _topLevelImpl = topLevelImpl;
        _container = container ?? throw new ArgumentNullException(nameof(container));

        _touchDevice = new TouchDevice();
        _penDevice = new PenDevice();
        _wheelMouseDevice = new MouseDevice();
        _mouseDevices = new();

        _rawEventGrouper = BrowserWindowingPlatform.EventGrouperDispatchQueue is not null
            ? new RawEventGrouper(DispatchInput, BrowserWindowingPlatform.EventGrouperDispatchQueue)
            : null;

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

    private static RawPointerPoint CreateRawPointer(double offsetX, double offsetY,
        double pressure, double tiltX, double tiltY, double twist) => new()
    {
        Position = new Point(offsetX, offsetY),
        Pressure = (float)pressure,
        XTilt = (float)tiltX,
        YTilt = (float)tiltY,
        Twist = (float)twist
    };

    public bool OnPointerMove(string pointerType, long pointerId, double offsetX, double offsetY,
        double pressure, double tiltX, double tiltY, double twist, int modifier, JSObject argsObj)
    {
        var point = CreateRawPointer(offsetX, offsetY, pressure, tiltX, tiltY, twist);
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchUpdate,
            _ => RawPointerEventType.Move
        };

        Lazy<IReadOnlyList<RawPointerPoint>?>? coalescedEvents = null;
        // Rely on native GetCoalescedEvents only when managed event grouping is not available.
        if (_rawEventGrouper is null)
        {
            coalescedEvents = new Lazy<IReadOnlyList<RawPointerPoint>?>(() =>
            {
                // To minimize JS interop usage, we resolve all points properties in a single call.
                const int itemsPerPoint = 6;
                var pointsProps = InputHelper.GetCoalescedEvents(argsObj);
                argsObj.Dispose();
                s_intermediatePointsPooledList.Clear();

                var pointsCount = pointsProps.Length / itemsPerPoint;
                s_intermediatePointsPooledList.Capacity = pointsCount - 1;

                // Skip the last one, as it is already processed point.
                for (var i = 0; i < pointsCount - 1; i += itemsPerPoint)
                {
                    s_intermediatePointsPooledList.Add(CreateRawPointer(
                        pointsProps[i], pointsProps[i + 1],
                        pointsProps[i + 2], pointsProps[i + 3],
                        pointsProps[i + 4], pointsProps[i + 5]));
                }

                return s_intermediatePointsPooledList;
            });
        }

        return RawPointerEvent(type, pointerType!, point, (RawInputModifiers)modifier, pointerId,
            coalescedEvents);
    }

    public bool OnPointerDown(string pointerType, long pointerId, int buttons, double offsetX, double offsetY,
        double pressure, double tiltX, double tiltY, double twist, int modifier)
    {
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchBegin,
            _ => buttons switch
            {
                0 => RawPointerEventType.LeftButtonDown,
                1 => RawPointerEventType.MiddleButtonDown,
                2 => RawPointerEventType.RightButtonDown,
                3 => RawPointerEventType.XButton1Down,
                4 => RawPointerEventType.XButton2Down,
                5 => RawPointerEventType.XButton1Down, // should be pen eraser button,
                _ => RawPointerEventType.Move
            }
        };

        var point = CreateRawPointer(offsetX, offsetY, pressure, tiltX, tiltY, twist);
        return RawPointerEvent(type, pointerType, point, (RawInputModifiers)modifier, pointerId);
    }

    public bool OnPointerUp(string pointerType, long pointerId, int buttons, double offsetX, double offsetY,
        double pressure, double tiltX, double tiltY, double twist, int modifier)
    {
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchEnd,
            _ => buttons switch
            {
                0 => RawPointerEventType.LeftButtonUp,
                1 => RawPointerEventType.MiddleButtonUp,
                2 => RawPointerEventType.RightButtonUp,
                3 => RawPointerEventType.XButton1Up,
                4 => RawPointerEventType.XButton2Up,
                5 => RawPointerEventType.XButton1Up, // should be pen eraser button,
                _ => RawPointerEventType.Move
            }
        };

        var point = CreateRawPointer(offsetX, offsetY, pressure, tiltX, tiltY, twist);
        return RawPointerEvent(type, pointerType, point, (RawInputModifiers)modifier, pointerId);
    }

    public bool OnPointerCancel(string pointerType, long pointerId, double offsetX, double offsetY,
        double pressure, double tiltX, double tiltY, double twist, int modifier)
    {
        if (pointerType == "touch")
        {
            var point = CreateRawPointer(offsetX, offsetY, pressure, tiltX, tiltY, twist);
            RawPointerEvent(RawPointerEventType.TouchCancel, pointerType, point,
                (RawInputModifiers)modifier, pointerId);
        }

        return false;
    }

    public bool OnWheel(double offsetX, double offsetY, double deltaX, double deltaY, int modifier)
    {
        return RawMouseWheelEvent(new Point(offsetX, offsetY),
            new Vector(-(deltaX / 50), -(deltaY / 50)),
            (RawInputModifiers)modifier);
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

        if (effectAllowedStr.Equals("all", StringComparison.OrdinalIgnoreCase))
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

    public bool OnKeyDown(string code, string key, int modifier)
    {
        var handled = RawKeyboardEvent(RawKeyEventType.KeyDown, code, key, (RawInputModifiers)modifier);

        if (!handled && key.Length == 1)
        {
            handled = RawTextEvent(key);
        }

        return handled;
    }

    public bool OnKeyUp(string code, string key, int modifier)
    {
        return RawKeyboardEvent(RawKeyEventType.KeyUp, code, key, (RawInputModifiers)modifier);
    }

    private bool RawPointerEvent(
        RawPointerEventType eventType, string pointerType,
        RawPointerPoint p, RawInputModifiers modifiers, long touchPointId,
        Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints = null)
    {
        if (_inputRoot is not null)
        {
            var device = GetPointerDevice(pointerType, touchPointId);
            var args = device is TouchDevice ?
                new RawTouchEventArgs(device, Timestamp, _inputRoot, eventType, p, modifiers, touchPointId)
                {
                    IntermediatePoints = intermediatePoints
                } :
                new RawPointerEventArgs(device, Timestamp, _inputRoot, eventType, p, modifiers)
                {
                    RawPointerId = touchPointId, IntermediatePoints = intermediatePoints
                };

            ScheduleInput(args);

            return args.Handled;
        }

        return false;
    }

    private IPointerDevice GetPointerDevice(string pointerType, long pointerId)
    {
        if (pointerType == "touch")
            return _touchDevice;
        else if (pointerType == "pen")
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

    private bool RawMouseWheelEvent(Point p, Vector v, RawInputModifiers modifiers)
    {
        if (_inputRoot is { })
        {
            var args = new RawMouseWheelEventArgs(_wheelMouseDevice, Timestamp, _inputRoot, p, v, modifiers);

            ScheduleInput(args);

            return args.Handled;
        }

        return false;
    }

    private bool RawKeyboardEvent(RawKeyEventType type, string domCode, string domKey, RawInputModifiers modifiers)
    {
        if (_inputRoot is null)
            return false;

        var physicalKey = KeyInterop.PhysicalKeyFromDomCode(domCode);
        var key = KeyInterop.KeyFromDomKey(domKey, physicalKey);
        var keySymbol = KeyInterop.KeySymbolFromDomKey(domKey);

        var args = new RawKeyEventArgs(
            BrowserWindowingPlatform.Keyboard,
            Timestamp,
            _inputRoot,
            type,
            key,
            modifiers,
            physicalKey,
            keySymbol
        );

        ScheduleInput(args);

        return args.Handled;
    }

    internal bool RawTextEvent(string text)
    {
        if (_inputRoot is { })
        {
            var args = new RawTextInputEventArgs(BrowserWindowingPlatform.Keyboard, Timestamp, _inputRoot, text);
            ScheduleInput(args);

            return args.Handled;
        }

        return false;
    }

    private DragDropEffects RawDragEvent(RawDragEventType eventType, Point position, RawInputModifiers modifiers,
        BrowserDragDataTransfer dataTransfer, DragDropEffects dropEffect)
    {
        var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
        var eventArgs = new RawDragEvent(device, eventType, _inputRoot!, position, dataTransfer, dropEffect, modifiers);
        ScheduleInput(eventArgs);
        return eventArgs.Effects;
    }

    private void ScheduleInput(RawInputEventArgs args)
    {
        // _rawEventGrouper is available only when we use managed dispatcher.
        if (_rawEventGrouper is not null)
        {
            _rawEventGrouper.HandleEvent(args);
        }
        else
        {
            DispatchInput(args);
        }
    }

    private void DispatchInput(RawInputEventArgs args)
    {
        if (_inputRoot is null)
            return;

        _topLevelImpl.Input?.Invoke(args);
    }
}
