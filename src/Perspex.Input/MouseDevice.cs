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

        private List<IInputElement> _pointerOvers = new List<IInputElement>();

        public MouseDevice()
        {
            this.InputManager.RawEventReceived
                .OfType<RawMouseEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(this.ProcessRawEvent);
        }

        public static IMouseDevice Instance
        {
            get { return Locator.Current.GetService<IMouseDevice>(); }
        }

        public IInputElement Captured
        {
            get;
            protected set;
        }

        public IInputManager InputManager
        {
            get { return Locator.Current.GetService<IInputManager>(); }
        }

        public Point Position
        {
            get;
            protected set;
        }

        public virtual void Capture(IInputElement control)
        {
            this.Captured = control;
        }

        public Point GetPosition(IVisual relativeTo)
        {
            Point p = this.Position;
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

            this.Position = e.Position;

            switch (e.Type)
            {
                case RawMouseEventType.LeaveWindow:
                    this.LeaveWindow(mouse, e.Root);
                    break;
                case RawMouseEventType.LeftButtonDown:
                    this.MouseDown(mouse, e.Timestamp, e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    this.MouseUp(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.Move:
                    this.MouseMove(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.Wheel:
                    this.MouseWheel(mouse, e.Root, e.Position, ((RawMouseWheelEventArgs)e).Delta);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, IInputRoot root)
        {
            this.ClearPointerOver(this, root);
        }

        private void MouseDown(IMouseDevice device, uint timestamp, IInputElement root, Point p)
        {
            var hit = this.HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

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
            IInteractive source;

            if (this.Captured == null)
            {
                this.SetPointerOver(this, root, root, p);
                source = root as IInteractive;
            }
            else
            {
                Point offset = new Point();

                foreach (IVisual ancestor in this.Captured.GetVisualAncestors())
                {
                    offset += ancestor.Bounds.Position;
                }

                this.SetPointerOver(this, root, this.Captured, p - offset);
                source = this.Captured as IInteractive;
            }

            if (source != null)
            {
                source.RaiseEvent(new PointerEventArgs
                {
                    Device = this,
                    RoutedEvent = InputElement.PointerMovedEvent,
                    Source = source,
                });
            }
        }

        private void MouseUp(IMouseDevice device, IInputRoot root, Point p)
        {
            var hit = this.HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

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
            var hit = this.HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

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
            return this.Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement HitTest(IInputElement root, Point p)
        {
            return this.Captured ?? root.InputHitTest(p);
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

        private void SetPointerOver(IPointerDevice device, IInputRoot root, IInputElement element, Point p)
        {
            IEnumerable<IInputElement> hits = element.GetInputElementsAt(p);

            foreach (var control in _pointerOvers.Except(hits).ToList())
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

            foreach (var control in hits.Except(_pointerOvers))
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

            root.PointerOverElement = hits.FirstOrDefault();
        }
    }
}
