// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Input.Raw;
using Perspex.Interactivity;
using Perspex.Platform;
using Perspex.VisualTree;

namespace Perspex.Input
{
    public class MouseDevice : IMouseDevice
    {
        private int _clickCount;

        private Rect _lastClickRect;

        private uint _lastClickTime;

        private readonly List<IInputElement> _pointerOvers = new List<IInputElement>();

        public MouseDevice()
        {
            InputManager.RawEventReceived
                .OfType<RawMouseEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(ProcessRawEvent);
        }

        public static IMouseDevice Instance => PerspexLocator.Current.GetService<IMouseDevice>();

        public IInputElement Captured
        {
            get;
            protected set;
        }

        public IInputManager InputManager => PerspexLocator.Current.GetService<IInputManager>();

        public Point Position
        {
            get;
            protected set;
        }

        public virtual void Capture(IInputElement control)
        {
            Captured = control;
        }

        public Point GetPosition(IVisual relativeTo)
        {
            Point p = Position;
            IVisual v = relativeTo;

            while (v != null)
            {
                p -= v.Bounds.Position;
                v = v.VisualParent;
            }

            return p;
        }

        private void ProcessRawEvent(RawMouseEventArgs e)
        {
            var mouse = (IMouseDevice)e.Device;

            Position = e.Position;

            switch (e.Type)
            {
                case RawMouseEventType.LeaveWindow:
                    LeaveWindow(mouse, e.Root);
                    break;
                case RawMouseEventType.LeftButtonDown:
                case RawMouseEventType.RightButtonDown:
                case RawMouseEventType.MiddleButtonDown:
                    MouseDown(mouse, e.Timestamp, e.Root, e.Position,
                         e.Type == RawMouseEventType.LeftButtonDown
                            ? MouseButton.Left
                            : e.Type == RawMouseEventType.RightButtonDown ? MouseButton.Right : MouseButton.Middle,
                        e.InputModifiers);
                    break;
                case RawMouseEventType.LeftButtonUp:
                case RawMouseEventType.RightButtonUp:
                case RawMouseEventType.MiddleButtonUp:
                    MouseUp(mouse, e.Root, e.Position,
                        e.Type == RawMouseEventType.LeftButtonUp
                            ? MouseButton.Left
                            : e.Type == RawMouseEventType.RightButtonUp ? MouseButton.Right : MouseButton.Middle,
                        e.InputModifiers);
                    break;
                case RawMouseEventType.Move:
                    MouseMove(mouse, e.Root, e.Position, e.InputModifiers);
                    break;
                case RawMouseEventType.Wheel:
                    MouseWheel(mouse, e.Root, e.Position, ((RawMouseWheelEventArgs)e).Delta, e.InputModifiers);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, IInputRoot root)
        {
            ClearPointerOver(this, root);
        }

        private void MouseDown(IMouseDevice device, uint timestamp, IInputElement root, Point p, MouseButton button, InputModifiers inputModifiers)
        {
            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    var settings = PerspexLocator.Current.GetService<IPlatformSettings>();
                    var doubleClickTime = settings.DoubleClickTime.TotalMilliseconds;

                    if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
                    {
                        _clickCount = 0;
                    }

                    ++_clickCount;
                    _lastClickTime = timestamp;
                    _lastClickRect = new Rect(p, new Size())
                        .Inflate(new Thickness(settings.DoubleClickSize.Width / 2, settings.DoubleClickSize.Height / 2));

                    var e = new PointerPressEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerPressedEvent,
                        Source = source,
                        ClickCount = _clickCount,
                        MouseButton = button,
                        InputModifiers = inputModifiers
                    };

                    source.RaiseEvent(e);
                }
            }
        }

        private void MouseMove(IMouseDevice device, IInputRoot root, Point p, InputModifiers inputModifiers)
        {
            IInputElement source;

            if (Captured == null)
            {
                source = SetPointerOver(this, root, p);
            }
            else
            {
                var elements = Captured.GetSelfAndVisualAncestors().OfType<IInputElement>().ToList();
                SetPointerOver(this, root, elements);
                source = Captured;
            }

            source.RaiseEvent(new PointerEventArgs
            {
                Device = this,
                RoutedEvent = InputElement.PointerMovedEvent,
                Source = source,
                InputModifiers = inputModifiers
            });
        }

        private void MouseUp(IMouseDevice device, IInputRoot root, Point p, MouseButton button, InputModifiers inputModifiers)
        {
            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    source.RaiseEvent(new PointerReleasedEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerReleasedEvent,
                        Source = source,
                        MouseButton = button,
                        InputModifiers = inputModifiers
                    });
                }
            }
        }

        private void MouseWheel(IMouseDevice device, IInputRoot root, Point p, Vector delta, InputModifiers inputModifiers)
        {
            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    source.RaiseEvent(new PointerWheelEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerWheelChangedEvent,
                        Source = source,
                        Delta = delta,
                        InputModifiers = inputModifiers
                    });
                }
            }
        }

        private IInteractive GetSource(IVisual hit)
        {
            return Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement HitTest(IInputElement root, Point p)
        {
            return Captured ?? root.InputHitTest(p);
        }

        private void ClearPointerOver(IPointerDevice device, IInputRoot root)
        {
            foreach (var control in _pointerOvers.ToList())
            {
                PointerEventArgs e = new PointerEventArgs
                {
                    RoutedEvent = InputElement.PointerLeaveEvent,
                    Device = device,
                    Source = control,
                };

                _pointerOvers.Remove(control);
                control.RaiseEvent(e);
            }

            root.PointerOverElement = null;
        }

        private IInputElement SetPointerOver(IPointerDevice device, IInputRoot root, Point p)
        {
            var elements = root.GetInputElementsAt(p).ToList();
            return SetPointerOver(device, root, elements);
        }

        private IInputElement SetPointerOver(IPointerDevice device, IInputRoot root, IList<IInputElement> elements)
        {
            foreach (var control in _pointerOvers.Except(elements).ToList())
            {
                PointerEventArgs e = new PointerEventArgs
                {
                    RoutedEvent = InputElement.PointerLeaveEvent,
                    Device = device,
                    Source = control,
                };

                _pointerOvers.Remove(control);
                control.RaiseEvent(e);
            }

            foreach (var control in elements.Except(_pointerOvers))
            {
                PointerEventArgs e = new PointerEventArgs
                {
                    RoutedEvent = InputElement.PointerEnterEvent,
                    Device = device,
                    Source = control,
                };

                _pointerOvers.Add(control);
                control.RaiseEvent(e);
            }

            root.PointerOverElement = elements.FirstOrDefault() ?? root;
            return root.PointerOverElement;
        }
    }
}
