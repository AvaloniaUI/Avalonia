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

    public BrowserInputHandler(BrowserTopLevelImpl topLevelImpl, JSObject container)
    {
        _topLevelImpl = topLevelImpl;
        _container = container ?? throw new ArgumentNullException(nameof(container));

        _touchDevice = new TouchDevice();
        _penDevice = new PenDevice();
        _wheelMouseDevice = new MouseDevice();
        _mouseDevices = new();

        InputHelper.SubscribeKeyEvents(
            container,
            OnKeyDown,
            OnKeyUp);
        InputHelper.SubscribePointerEvents(container, OnPointerMove, OnPointerDown, OnPointerUp,
            OnPointerCancel, OnWheel);
        InputHelper.SubscribeDropEvents(container, OnDragEvent);
    }

    public ulong Timestamp => (ulong)_sw.ElapsedMilliseconds;

    internal void SetInputRoot(IInputRoot inputRoot)
    {
        _inputRoot = inputRoot;
    }

    private static RawPointerPoint ExtractRawPointerFromJsArgs(JSObject args)
    {
        var point = new RawPointerPoint
        {
            Position = new Point(args.GetPropertyAsDouble("offsetX"), args.GetPropertyAsDouble("offsetY")),
            Pressure = (float)args.GetPropertyAsDouble("pressure"),
            XTilt = (float)args.GetPropertyAsDouble("tiltX"),
            YTilt = (float)args.GetPropertyAsDouble("tiltY"),
            Twist = (float)args.GetPropertyAsDouble("twist")
        };

        return point;
    }

    private bool OnPointerMove(JSObject args)
    {
        var pointerType = args.GetPropertyAsString("pointerType");
        var point = ExtractRawPointerFromJsArgs(args);
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchUpdate,
            _ => RawPointerEventType.Move
        };

        var coalescedEvents = new Lazy<IReadOnlyList<RawPointerPoint>?>(() =>
        {
            var points = InputHelper.GetCoalescedEvents(args);
            s_intermediatePointsPooledList.Clear();
            s_intermediatePointsPooledList.Capacity = points.Length - 1;

            // Skip the last one, as it is already processed point.
            for (var i = 0; i < points.Length - 1; i++)
            {
                var point = points[i];
                s_intermediatePointsPooledList.Add(ExtractRawPointerFromJsArgs(point));
            }

            return s_intermediatePointsPooledList;
        });

        return RawPointerEvent(type, pointerType!, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"),
            coalescedEvents);
    }

    private bool OnPointerDown(JSObject args)
    {
        var pointerType = args.GetPropertyAsString("pointerType") ?? "mouse";
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchBegin,
            _ => args.GetPropertyAsInt32("button") switch
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

        var point = ExtractRawPointerFromJsArgs(args);
        return RawPointerEvent(type, pointerType, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
    }

    private bool OnPointerUp(JSObject args)
    {
        var pointerType = args.GetPropertyAsString("pointerType") ?? "mouse";
        var type = pointerType switch
        {
            "touch" => RawPointerEventType.TouchEnd,
            _ => args.GetPropertyAsInt32("button") switch
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

        var point = ExtractRawPointerFromJsArgs(args);
        return RawPointerEvent(type, pointerType, point, GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
    }

    private bool OnPointerCancel(JSObject args)
    {
        var pointerType = args.GetPropertyAsString("pointerType") ?? "mouse";
        if (pointerType == "touch")
        {
            var point = ExtractRawPointerFromJsArgs(args);
            RawPointerEvent(RawPointerEventType.TouchCancel, pointerType, point,
                GetModifiers(args), args.GetPropertyAsInt32("pointerId"));
        }

        return false;
    }

    private bool OnWheel(JSObject args)
    {
        return RawMouseWheelEvent(new Point(args.GetPropertyAsDouble("offsetX"), args.GetPropertyAsDouble("offsetY")),
            new Vector(-(args.GetPropertyAsDouble("deltaX") / 50), -(args.GetPropertyAsDouble("deltaY") / 50)),
            GetModifiers(args));
    }

    private static RawInputModifiers GetModifiers(JSObject e)
    {
        var modifiers = RawInputModifiers.None;

        if (e.GetPropertyAsBoolean("ctrlKey"))
            modifiers |= RawInputModifiers.Control;
        if (e.GetPropertyAsBoolean("altKey"))
            modifiers |= RawInputModifiers.Alt;
        if (e.GetPropertyAsBoolean("shiftKey"))
            modifiers |= RawInputModifiers.Shift;
        if (e.GetPropertyAsBoolean("metaKey"))
            modifiers |= RawInputModifiers.Meta;

        var buttons = e.GetPropertyAsInt32("buttons");
        if ((buttons & 1L) == 1)
            modifiers |= RawInputModifiers.LeftMouseButton;

        if ((buttons & 2L) == 2)
            modifiers |= e.GetPropertyAsString("type") == "pen" ?
                RawInputModifiers.PenBarrelButton :
                RawInputModifiers.RightMouseButton;

        if ((buttons & 4L) == 4)
            modifiers |= RawInputModifiers.MiddleMouseButton;

        if ((buttons & 8L) == 8)
            modifiers |= RawInputModifiers.XButton1MouseButton;

        if ((buttons & 16L) == 16)
            modifiers |= RawInputModifiers.XButton2MouseButton;

        if ((buttons & 32L) == 32)
            modifiers |= RawInputModifiers.PenEraser;

        return modifiers;
    }

    public bool OnDragEvent(JSObject args)
    {
        var eventType = args?.GetPropertyAsString("type") switch
        {
            "dragenter" => RawDragEventType.DragEnter,
            "dragover" => RawDragEventType.DragOver,
            "dragleave" => RawDragEventType.DragLeave,
            "drop" => RawDragEventType.Drop,
            _ => (RawDragEventType)(int)-1
        };
        var dataObject = args?.GetPropertyAsJSObject("dataTransfer");
        if (args is null || eventType < 0 || dataObject is null)
        {
            return false;
        }

        // If file is dropped, we need storage js to be referenced.
        // TODO: restructure JS files, so it's not needed.
        _ = AvaloniaModule.ImportStorage();

        var position = new Point(args.GetPropertyAsDouble("offsetX"), args.GetPropertyAsDouble("offsetY"));
        var modifiers = GetModifiers(args);

        var effectAllowedStr = dataObject.GetPropertyAsString("effectAllowed") ?? "none";
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

        var dropEffect = RawDragEvent(eventType, position, modifiers, new BrowserDataObject(dataObject), effectAllowed);
        dataObject.SetProperty("dropEffect", dropEffect.ToString().ToLowerInvariant());

        return eventType is RawDragEventType.Drop or RawDragEventType.DragOver
               && dropEffect != DragDropEffects.None;
    }

    private bool OnKeyDown(string code, string key, string modifier)
    {
        var handled = RawKeyboardEvent(RawKeyEventType.KeyDown, code, key, (RawInputModifiers)int.Parse(modifier));

        if (!handled && key.Length == 1)
        {
            handled = RawTextEvent(key);
        }

        return handled;
    }

    private bool OnKeyUp(string code, string key, string modifier)
    {
        return RawKeyboardEvent(RawKeyEventType.KeyUp, code, key, (RawInputModifiers)int.Parse(modifier));
    }

    private bool RawPointerEvent(
        RawPointerEventType eventType, string pointerType,
        RawPointerPoint p, RawInputModifiers modifiers, long touchPointId,
        Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints = null)
    {
        if (_inputRoot is { }
            && _topLevelImpl.Input is { } input)
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

            input.Invoke(args);

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

            _topLevelImpl.Input?.Invoke(args);

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

        try
        {
            _topLevelImpl.Input?.Invoke(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return args.Handled;
    }

    internal bool RawTextEvent(string text)
    {
        if (_inputRoot is { })
        {
            var args = new RawTextInputEventArgs(BrowserWindowingPlatform.Keyboard, Timestamp, _inputRoot, text);
            _topLevelImpl.Input?.Invoke(args);

            return args.Handled;
        }

        return false;
    }

    private DragDropEffects RawDragEvent(RawDragEventType eventType, Point position, RawInputModifiers modifiers,
        BrowserDataObject dataObject, DragDropEffects dropEffect)
    {
        var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();
        var eventArgs = new RawDragEvent(device, eventType, _inputRoot!, position, dataObject, dropEffect, modifiers);
        _topLevelImpl.Input?.Invoke(eventArgs);
        return eventArgs.Effects;
    }
}
