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
using Splat;

namespace Perspex.Input
{
    public abstract class MouseDevice : IMouseDevice
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

        public static IMouseDevice Instance => Locator.Current.GetService<IMouseDevice>();

        public IInputElement Captured
        {
            get;
            protected set;
        }

        public IInputManager InputManager => Locator.Current.GetService<IInputManager>();

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
                    MouseDown(mouse, e.Timestamp, e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    MouseUp(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.Move:
                    MouseMove(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.Wheel:
                    MouseWheel(mouse, e.Root, e.Position, ((RawMouseWheelEventArgs)e).Delta);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, IInputRoot root)
        {
            ClearPointerOver(this, root);
        }

        private void MouseDown(IMouseDevice device, uint timestamp, IInputElement root, Point p)
        {
            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    var settings = Locator.Current.GetService<IPlatformSettings>();
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
                    };

                    source.RaiseEvent(e);
                }
            }
        }

        private void MouseMove(IMouseDevice device, IInputRoot root, Point p)
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
            });
        }

        private void MouseUp(IMouseDevice device, IInputRoot root, Point p)
        {
            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    source.RaiseEvent(new PointerEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerReleasedEvent,
                        Source = source,
                    });
                }
            }
        }

        private void MouseWheel(IMouseDevice device, IInputRoot root, Point p, Vector delta)
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
