using System;
using System.Collections.Generic;

using Android.Views;

using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Collections.Pooled;
using Avalonia.Input;
using Avalonia.Input.Raw;

#nullable enable

namespace Avalonia.Android.Platform.Specific.Helpers
{
    internal class AndroidMotionEventsHelper : IDisposable
    {
        private static readonly PooledList<RawPointerPoint> s_intermediatePointsPooledList = new(ClearMode.Never);
        private const float RadiansToDegree = (float)(180f * Math.PI);
        private readonly TouchDevice _touchDevice;
        private readonly MouseDevice _mouseDevice;
        private readonly PenDevice _penDevice;
        private readonly TopLevelImpl _view;
        private bool _disposed;

        public AndroidMotionEventsHelper(TopLevelImpl view)
        {
            _touchDevice = new TouchDevice();
            _penDevice = new PenDevice();
            _mouseDevice = new MouseDevice();
            _view = view;
        }

        public bool? DispatchMotionEvent(MotionEvent e, out bool callBase)
        {
            callBase = true;
            if (_disposed)
            {
                return null;
            }

            var eventTime = (ulong)e.EventTime;
            var inputRoot = _view.InputRoot;
            var actionMasked = e.ActionMasked;
            var modifiers = GetModifiers(e.MetaState, e.ButtonState);

            if (actionMasked == MotionEventActions.Move)
            {
                for (int index = 0; index < e.PointerCount; index++)
                {
                    var toolType = e.GetToolType(index);
                    var device = GetDevice(toolType);
                    var eventType = toolType == MotionEventToolType.Finger ? RawPointerEventType.TouchUpdate : RawPointerEventType.Move;
                    var point = CreatePoint(e, index);
                    modifiers |= GetToolModifiers(toolType);

                    // ButtonState reports only mouse buttons, but not touch or stylus pointer.
                    if (toolType != MotionEventToolType.Mouse)
                    {
                        modifiers |= RawInputModifiers.LeftMouseButton;
                    }

                    var args = new RawTouchEventArgs(device, eventTime, inputRoot, eventType, point, modifiers, e.GetPointerId(index))
                    {
                        IntermediatePoints = new Lazy<IReadOnlyList<RawPointerPoint>?>(() =>
                        {
                            var site = e.HistorySize;
                            s_intermediatePointsPooledList.Clear();
                            s_intermediatePointsPooledList.Capacity = site;

                            for (int pos = 0; pos < site; pos++)
                            {
                                s_intermediatePointsPooledList.Add(CreateHistoricalPoint(e, index, pos));
                            }

                            return s_intermediatePointsPooledList;
                        })
                    };
                    _view.Input(args);
                }
            }
            else
            {
                var index = e.ActionIndex;
                var toolType = e.GetToolType(index);
                var device = GetDevice(toolType);
                modifiers |= GetToolModifiers(toolType);
                var point = CreatePoint(e, index);

                if (actionMasked == MotionEventActions.Scroll && toolType == MotionEventToolType.Mouse)
                {
                    var delta = new Vector(e.GetAxisValue(Axis.Hscroll), e.GetAxisValue(Axis.Vscroll));
                    var args = new RawMouseWheelEventArgs(device, eventTime, inputRoot, point.Position, delta, RawInputModifiers.None);
                    _view.Input(args);
                }
                else
                {
                    var eventType = GetActionType(e, actionMasked, toolType);
                    if (eventType >= 0)
                    {
                        var args = new RawTouchEventArgs(device, eventTime, inputRoot, eventType, point, modifiers, e.GetPointerId(index));
                        _view.Input(args);
                    }
                }
            }

            return true;
        }

        private static RawInputModifiers GetModifiers(MetaKeyStates metaState, MotionEventButtonState buttonState)
        {
            var modifiers = RawInputModifiers.None;
            if (metaState.HasAnyFlag(MetaKeyStates.ShiftOn))
            {
                modifiers |= RawInputModifiers.Shift;
            }
            if (metaState.HasAnyFlag(MetaKeyStates.CtrlOn))
            {
                modifiers |= RawInputModifiers.Control;
            }
            if (metaState.HasAnyFlag(MetaKeyStates.AltOn))
            {
                modifiers |= RawInputModifiers.Alt;
            }
            if (metaState.HasAnyFlag(MetaKeyStates.MetaOn))
            {
                modifiers |= RawInputModifiers.Meta;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.Primary))
            {
                modifiers |= RawInputModifiers.LeftMouseButton;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.Secondary))
            {
                modifiers |= RawInputModifiers.RightMouseButton;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.Tertiary))
            {
                modifiers |= RawInputModifiers.MiddleMouseButton;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.Back))
            {
                modifiers |= RawInputModifiers.XButton1MouseButton;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.Forward))
            {
                modifiers |= RawInputModifiers.XButton2MouseButton;
            }
            if (buttonState.HasAnyFlag(MotionEventButtonState.StylusPrimary))
            {
                modifiers |= RawInputModifiers.PenBarrelButton;
            }
            return modifiers;
        }

#pragma warning disable CA1416 // Validate platform compatibility
        private static RawPointerEventType GetActionType(MotionEvent e, MotionEventActions actionMasked, MotionEventToolType toolType)
        {
            var isTouch = toolType == MotionEventToolType.Finger;
            var isMouse = toolType == MotionEventToolType.Mouse;
            switch (actionMasked)
            {
                // DOWN
                case MotionEventActions.Down when !isMouse:
                case MotionEventActions.PointerDown when !isMouse:
                    return isTouch ? RawPointerEventType.TouchBegin : RawPointerEventType.LeftButtonDown;
                case MotionEventActions.ButtonPress:
                    return e.ActionButton switch
                    {
                        MotionEventButtonState.Back => RawPointerEventType.XButton1Down,
                        MotionEventButtonState.Forward => RawPointerEventType.XButton2Down,
                        MotionEventButtonState.Primary => RawPointerEventType.LeftButtonDown,
                        MotionEventButtonState.Secondary => RawPointerEventType.RightButtonDown,
                        MotionEventButtonState.StylusPrimary => RawPointerEventType.LeftButtonDown,
                        MotionEventButtonState.StylusSecondary => RawPointerEventType.RightButtonDown,
                        MotionEventButtonState.Tertiary => RawPointerEventType.MiddleButtonDown,
                        _ => RawPointerEventType.LeftButtonDown
                    };
                // UP
                case MotionEventActions.Up when !isMouse:
                case MotionEventActions.PointerUp when !isMouse:
                    return isTouch ? RawPointerEventType.TouchEnd : RawPointerEventType.LeftButtonUp;
                case MotionEventActions.ButtonRelease:
                    return e.ActionButton switch
                    {
                        MotionEventButtonState.Back => RawPointerEventType.XButton1Up,
                        MotionEventButtonState.Forward => RawPointerEventType.XButton2Up,
                        MotionEventButtonState.Primary => RawPointerEventType.LeftButtonUp,
                        MotionEventButtonState.Secondary => RawPointerEventType.RightButtonUp,
                        MotionEventButtonState.StylusPrimary => RawPointerEventType.LeftButtonUp,
                        MotionEventButtonState.StylusSecondary => RawPointerEventType.RightButtonUp,
                        MotionEventButtonState.Tertiary => RawPointerEventType.MiddleButtonUp,
                        _ => RawPointerEventType.LeftButtonUp
                    };
                // MOVE
                case MotionEventActions.Outside:
                case MotionEventActions.HoverMove:
                case MotionEventActions.Move:
                    return isTouch ? RawPointerEventType.TouchUpdate : RawPointerEventType.Move;
                // CANCEL
                case MotionEventActions.Cancel:
                    return isTouch ? RawPointerEventType.TouchCancel : RawPointerEventType.LeaveWindow;
                default:
                    return (RawPointerEventType)(-1);
            }
        }
#pragma warning restore CA1416 // Validate platform compatibility

        private IPointerDevice GetDevice(MotionEventToolType type)
        {
            return type switch
            {
                MotionEventToolType.Mouse => _mouseDevice,
                MotionEventToolType.Stylus => _penDevice,
                MotionEventToolType.Eraser => _penDevice,
                MotionEventToolType.Finger => _touchDevice,
                _ => _touchDevice
            };
        }

        private RawPointerPoint CreatePoint(MotionEvent e, int index)
        {
            return new RawPointerPoint
            {
                Position = new Point(e.GetX(index), e.GetY(index)) / _view.RenderScaling,
                Pressure = Math.Min(e.GetPressure(index), 1), // android pressure can depend on the device, can be mixed up with "GetSize", may be larger than 1.0f on some devices
                Twist = e.GetOrientation(index) * RadiansToDegree
            };
        }

        private RawPointerPoint CreateHistoricalPoint(MotionEvent e, int index, int pos)
        {
            return new RawPointerPoint
            {
                Position = new Point(e.GetHistoricalX(index, pos), e.GetHistoricalY(index, pos)) / _view.RenderScaling,
                Pressure = Math.Min(e.GetHistoricalPressure(index, pos), 1),
                Twist = e.GetHistoricalOrientation(index, pos) * RadiansToDegree
            };
        }

        private static RawInputModifiers GetToolModifiers(MotionEventToolType toolType)
        {
            // Android "Eraser" indicates Inverted pen OR actual Eraser. So we have to go both here.
            return toolType == MotionEventToolType.Eraser ? RawInputModifiers.PenInverted | RawInputModifiers.PenEraser : RawInputModifiers.None;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
