// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    public class MouseDevice : IMouseDevice
    {
        private int _clickCount;
        private Rect _lastClickRect;
        private uint _lastClickTime;
        private IInputElement _captured;
        private IDisposable _capturedSubscription;
       
        /// <summary>
        /// Gets the control that is currently capturing by the mouse, if any.
        /// </summary>
        /// <remarks>
        /// When an element captures the mouse, it recieves mouse input whether the cursor is 
        /// within the control's bounds or not. To set the mouse capture, call the 
        /// <see cref="Capture"/> method.
        /// </remarks>
        public IInputElement Captured
        {
            get => _captured;
            protected set
            {
                _capturedSubscription?.Dispose();
                _capturedSubscription = null;

                if (value != null)
                {
                    _capturedSubscription = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                        x => value.DetachedFromVisualTree += x,
                        x => value.DetachedFromVisualTree -= x)
                        .Take(1)
                        .Subscribe(_ => Captured = null);
                }

                _captured = value;
            }
        }
        
        /// <summary>
        /// Gets the mouse position, in screen coordinates.
        /// </summary>
        public Point Position
        {
            get;
            protected set;
        }

        /// <summary>
        /// Captures mouse input to the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <remarks>
        /// When an element captures the mouse, it recieves mouse input whether the cursor is 
        /// within the control's bounds or not. The current mouse capture control is exposed
        /// by the <see cref="Captured"/> property.
        /// </remarks>
        public virtual void Capture(IInputElement control)
        {
            // TODO: Check visibility and enabled state before setting capture.
            Captured = control;
        }

        /// <summary>
        /// Gets the mouse position relative to a control.
        /// </summary>
        /// <param name="relativeTo">The control.</param>
        /// <returns>The mouse position in the control's coordinates.</returns>
        public Point GetPosition(IVisual relativeTo)
        {
            Contract.Requires<ArgumentNullException>(relativeTo != null);

            Point p = default(Point);
            IVisual v = relativeTo;
            IVisual root = null;

            while (v != null)
            {
                p += v.Bounds.Position;
                root = v;
                v = v.VisualParent;
            }

            return root.PointToClient(Position) - p;
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawMouseEventArgs margs)
                ProcessRawEvent(margs);
        }

        private void ProcessRawEvent(RawMouseEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            var mouse = (IMouseDevice)e.Device;

            Position = e.Root.PointToScreen(e.Position);

            switch (e.Type)
            {
                case RawMouseEventType.LeaveWindow:
                    LeaveWindow(mouse, e.Root);
                    break;
                case RawMouseEventType.LeftButtonDown:
                case RawMouseEventType.RightButtonDown:
                case RawMouseEventType.MiddleButtonDown:
                    e.Handled = MouseDown(mouse, e.Timestamp, e.Root, e.Position,
                         e.Type == RawMouseEventType.LeftButtonDown
                            ? MouseButton.Left
                            : e.Type == RawMouseEventType.RightButtonDown ? MouseButton.Right : MouseButton.Middle,
                        e.InputModifiers);
                    break;
                case RawMouseEventType.LeftButtonUp:
                case RawMouseEventType.RightButtonUp:
                case RawMouseEventType.MiddleButtonUp:
                    e.Handled = MouseUp(mouse, e.Root, e.Position,
                        e.Type == RawMouseEventType.LeftButtonUp
                            ? MouseButton.Left
                            : e.Type == RawMouseEventType.RightButtonUp ? MouseButton.Right : MouseButton.Middle,
                        e.InputModifiers);
                    break;
                case RawMouseEventType.Move:
                    e.Handled = MouseMove(mouse, e.Root, e.Position, e.InputModifiers);
                    break;
                case RawMouseEventType.Wheel:
                    e.Handled = MouseWheel(mouse, e.Root, e.Position, ((RawMouseWheelEventArgs)e).Delta, e.InputModifiers);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, IInputRoot root)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            ClearPointerOver(this, root);
        }

        private bool MouseDown(IMouseDevice device, uint timestamp, IInputElement root, Point p, MouseButton button, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                IInteractive source = GetSource(hit);

                if (source != null)
                {
                    var settings = AvaloniaLocator.Current.GetService<IPlatformSettings>();
                    var doubleClickTime = settings.DoubleClickTime.TotalMilliseconds;

                    if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
                    {
                        _clickCount = 0;
                    }

                    ++_clickCount;
                    _lastClickTime = timestamp;
                    _lastClickRect = new Rect(p, new Size())
                        .Inflate(new Thickness(settings.DoubleClickSize.Width / 2, settings.DoubleClickSize.Height / 2));

                    var e = new PointerPressedEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerPressedEvent,
                        Source = source,
                        ClickCount = _clickCount,
                        MouseButton = button,
                        InputModifiers = inputModifiers
                    };

                    source.RaiseEvent(e);
                    return e.Handled;
                }
            }

            return false;
        }

        private bool MouseMove(IMouseDevice device, IInputRoot root, Point p, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            IInputElement source;

            if (Captured == null)
            {
                source = SetPointerOver(this, root, p);
            }
            else
            {
                SetPointerOver(this, root, Captured);
                source = Captured;
            }

            var e = new PointerEventArgs
            {
                Device = this,
                RoutedEvent = InputElement.PointerMovedEvent,
                Source = source,
                InputModifiers = inputModifiers
            };

            source?.RaiseEvent(e);
            return e.Handled;
        }

        private bool MouseUp(IMouseDevice device, IInputRoot root, Point p, MouseButton button, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerReleasedEventArgs
                {
                    Device = this,
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    Source = source,
                    MouseButton = button,
                    InputModifiers = inputModifiers
                };

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool MouseWheel(IMouseDevice device, IInputRoot root, Point p, Vector delta, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerWheelEventArgs
                {
                    Device = this,
                    RoutedEvent = InputElement.PointerWheelChangedEvent,
                    Source = source,
                    Delta = delta,
                    InputModifiers = inputModifiers
                };

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private IInteractive GetSource(IVisual hit)
        {
            Contract.Requires<ArgumentNullException>(hit != null);

            return Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement HitTest(IInputElement root, Point p)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            return Captured ?? root.InputHitTest(p);
        }

        private void ClearPointerOver(IPointerDevice device, IInputRoot root)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.PointerOverElement;
            var e = new PointerEventArgs
            {
                RoutedEvent = InputElement.PointerLeaveEvent,
                Device = device,
            };

            while (element != null)
            {
                e.Source = element;
                element.RaiseEvent(e);
                element = (IInputElement)element.VisualParent;
            }

            root.PointerOverElement = null;
        }

        private IInputElement SetPointerOver(IPointerDevice device, IInputRoot root, Point p)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.InputHitTest(p);

            if (element != root.PointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOver(device, root, element);
                }
                else
                {
                    ClearPointerOver(device, root);
                }
            }

            return element;
        }

        private void SetPointerOver(IPointerDevice device, IInputRoot root, IInputElement element)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);
            Contract.Requires<ArgumentNullException>(element != null);

            IInputElement branch = null;

            var e = new PointerEventArgs
            {
                RoutedEvent = InputElement.PointerEnterEvent,
                Device = device,
            };

            var el = element;

            while (el != null)
            {
                if (el.IsPointerOver)
                {
                    branch = el;
                    break;
                }

                e.Source = el;
                el.RaiseEvent(e);
                el = (IInputElement)el.VisualParent;
            }

            el = root.PointerOverElement;
            e.RoutedEvent = InputElement.PointerLeaveEvent;

            while (el != null && el != branch)
            {
                e.Source = el;
                el.RaiseEvent(e);
                el = (IInputElement)el.VisualParent;
            }

            root.PointerOverElement = element;
        }
    }
}