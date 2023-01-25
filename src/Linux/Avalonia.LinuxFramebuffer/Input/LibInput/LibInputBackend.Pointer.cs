using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using static Avalonia.LinuxFramebuffer.Input.LibInput.LibInputNativeUnsafeMethods;

namespace Avalonia.LinuxFramebuffer.Input.LibInput;

public partial class LibInputBackend
{
    private MouseDevice _mouse = new MouseDevice();
    private Point _mousePosition;
    private const string Pointer = LibInput + "/" + nameof(Pointer);

    private void HandlePointer(IntPtr ev, LibInputEventType type)
    {
        var modifiers = RawInputModifiers.None; //TODO: support input modifiers
        var pev = libinput_event_get_pointer_event(ev);
        var info = _screen.ScaledSize;
        var ts = libinput_event_pointer_get_time_usec(pev) / 1000;
        switch (type)
        {
            case LibInputEventType.LIBINPUT_EVENT_POINTER_MOTION_ABSOLUTE:
                _mousePosition = new Point(libinput_event_pointer_get_absolute_x_transformed(pev, (int)info.Width),
                    libinput_event_pointer_get_absolute_y_transformed(pev, (int)info.Height));
                ScheduleInput(new RawPointerEventArgs(_mouse, ts, _inputRoot, RawPointerEventType.Move, _mousePosition,
                    modifiers));
                break;
            case LibInputEventType.LIBINPUT_EVENT_POINTER_BUTTON:
                {
                    var button = (EvKey)libinput_event_pointer_get_button(pev);
                    var buttonState = libinput_event_pointer_get_button_state(pev);

                    RawPointerEventArgs evnt = button switch
                    {
                        EvKey.BTN_LEFT when buttonState == 1
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.LeftButtonDown, _mousePosition, modifiers),
                        EvKey.BTN_LEFT when buttonState == 0
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.LeftButtonUp, _mousePosition, modifiers),
                        EvKey.BTN_RIGHT when buttonState == 1
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.RightButtonUp, _mousePosition, modifiers),
                        EvKey.BTN_RIGHT when buttonState == 2
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.RightButtonDown, _mousePosition, modifiers),
                        EvKey.BTN_MIDDLE when buttonState == 1
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.MiddleButtonDown, _mousePosition, modifiers),
                        EvKey.BTN_MIDDLE when buttonState == 2
                            => new(_mouse, ts, _inputRoot, RawPointerEventType.MiddleButtonUp, _mousePosition, modifiers),
                        _ => default,
                    };
                    if (evnt is not null)
                    {
                        ScheduleInput(evnt);
                    }
                    else
                    {
                        Logger.TryGet(LogEventLevel.Warning, Pointer)
                            ?.Log(this, $"The button {button} is not associated");
                    }
                }
                break;
            // Backward compatibility with low-res wheel
            case LibInputEventType.LIBINPUT_EVENT_POINTER_AXIS:
                {
                    var sourceAxis = libinput_event_pointer_get_axis_source(pev);
                    switch (sourceAxis)
                    {
                        case LibInputPointerAxisSource.LIBINPUT_POINTER_AXIS_SOURCE_WHEEL:
                            {
                                var value = libinput_event_pointer_get_axis_value_discrete(pev,
                                    LibInputPointerAxis.LIBINPUT_POINTER_AXIS_SCROLL_VERTICAL);
                                ScheduleInput(new RawMouseWheelEventArgs(_mouse
                                    , ts
                                    , _inputRoot
                                    , _mousePosition
                                    , new Vector(0, -value)
                                    , modifiers));
                            }
                            break;
                        case LibInputPointerAxisSource.LIBINPUT_POINTER_AXIS_SOURCE_FINGER:
                        case LibInputPointerAxisSource.LIBINPUT_POINTER_AXIS_SOURCE_CONTINUOUS:
                        case LibInputPointerAxisSource.LIBINPUT_POINTER_AXIS_SOURCE_WHEEL_TILT:
                        default:
                            Logger.TryGet(LogEventLevel.Debug, Pointer)
                                ?.Log(this, $"The pointer axis {sourceAxis} is not managed.");
                            break;
                    }
                }
                break;
            // Hi-Res wheel
            case LibInputEventType.LIBINPUT_EVENT_POINTER_SCROLL_WHEEL:
                {
                    var value = new Vector(0,
                        -libinput_event_pointer_get_scroll_value_v120(pev,
                            LibInputPointerAxis.LIBINPUT_POINTER_AXIS_SCROLL_VERTICAL) / 120);
                    ScheduleInput(new RawMouseWheelEventArgs(_mouse
                        , ts
                        , _inputRoot
                        , _mousePosition
                        , value
                        , modifiers));
                }
                break;
            default:
                Logger.TryGet(LogEventLevel.Warning, Pointer)
                    ?.Log(this, $"The pointer event {type} is not mapped.");
                break;
        }

    }
}
