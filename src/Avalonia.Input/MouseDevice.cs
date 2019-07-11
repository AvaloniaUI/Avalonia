// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        private ulong _lastClickTime;

        private readonly Pointer _pointer;

        public MouseDevice(Pointer pointer = null)
        {
            _pointer = pointer ?? new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        }
        
        /// <summary>
        /// Gets the control that is currently capturing by the mouse, if any.
        /// </summary>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether the cursor is 
        /// within the control's bounds or not. To set the mouse capture, call the 
        /// <see cref="Capture"/> method.
        /// </remarks>
        [Obsolete("Use IPointer instead")]
        public IInputElement Captured => _pointer.Captured;

        /// <summary>
        /// Gets the mouse position, in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get;
            protected set;
        }

        /// <summary>
        /// Captures mouse input to the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether the cursor is 
        /// within the control's bounds or not. The current mouse capture control is exposed
        /// by the <see cref="Captured"/> property.
        /// </remarks>
        public void Capture(IInputElement control)
        {
            _pointer.Capture(control);
        }

        /// <summary>
        /// Gets the mouse position relative to a control.
        /// </summary>
        /// <param name="relativeTo">The control.</param>
        /// <returns>The mouse position in the control's coordinates.</returns>
        public Point GetPosition(IVisual relativeTo)
        {
            Contract.Requires<ArgumentNullException>(relativeTo != null);

            if (relativeTo.VisualRoot == null)
            {
                throw new InvalidOperationException("Control is not attached to visual tree.");
            }

            var rootPoint = relativeTo.VisualRoot.PointToClient(Position);
            var transform = relativeTo.VisualRoot.TransformToVisual(relativeTo);
            return rootPoint * transform.Value;
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawPointerEventArgs margs)
                ProcessRawEvent(margs);
        }

        public void SceneInvalidated(IInputRoot root, Rect rect)
        {
            var clientPoint = root.PointToClient(Position);

            if (rect.Contains(clientPoint))
            {
                if (_pointer.Captured == null)
                {
                    SetPointerOver(this, 0 /* TODO: proper timestamp */, root, clientPoint, InputModifiers.None);
                }
                else
                {
                    SetPointerOver(this, 0 /* TODO: proper timestamp */, root, _pointer.Captured, InputModifiers.None);
                }
            }
        }

        int ButtonCount(PointerPointProperties props)
        {
            var rv = 0;
            if (props.IsLeftButtonPressed)
                rv++;
            if (props.IsMiddleButtonPressed)
                rv++;
            if (props.IsRightButtonPressed)
                rv++;
            return rv;
        }
        
        private void ProcessRawEvent(RawPointerEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            var mouse = (IMouseDevice)e.Device;

            Position = e.Root.PointToScreen(e.Position);
            var props = CreateProperties(e);
            switch (e.Type)
            {
                case RawPointerEventType.LeaveWindow:
                    LeaveWindow(mouse, e.Timestamp, e.Root, e.InputModifiers);
                    break;
                case RawPointerEventType.LeftButtonDown:
                case RawPointerEventType.RightButtonDown:
                case RawPointerEventType.MiddleButtonDown:
                    if (ButtonCount(props) > 1)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, e.InputModifiers);
                    else
                        e.Handled = MouseDown(mouse, e.Timestamp, e.Root, e.Position,
                            props, e.InputModifiers);
                    break;
                case RawPointerEventType.LeftButtonUp:
                case RawPointerEventType.RightButtonUp:
                case RawPointerEventType.MiddleButtonUp:
                    if (ButtonCount(props) != 0)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, e.InputModifiers);
                    else
                        e.Handled = MouseUp(mouse, e.Timestamp, e.Root, e.Position, props, e.InputModifiers);
                    break;
                case RawPointerEventType.Move:
                    e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, e.InputModifiers);
                    break;
                case RawPointerEventType.Wheel:
                    e.Handled = MouseWheel(mouse, e.Timestamp, e.Root, e.Position, props, ((RawMouseWheelEventArgs)e).Delta, e.InputModifiers);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, ulong timestamp, IInputRoot root, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            ClearPointerOver(this, timestamp, root, inputModifiers);
        }


        PointerPointProperties CreateProperties(RawPointerEventArgs args)
        {
            var rv = new PointerPointProperties(args.InputModifiers);

            if (args.Type == RawPointerEventType.LeftButtonDown)
                rv.IsLeftButtonPressed = true;
            if (args.Type == RawPointerEventType.MiddleButtonDown)
                rv.IsMiddleButtonPressed = true;
            if (args.Type == RawPointerEventType.RightButtonDown)
                rv.IsRightButtonPressed = true;
            if (args.Type == RawPointerEventType.LeftButtonUp)
                rv.IsLeftButtonPressed = false;
            if (args.Type == RawPointerEventType.MiddleButtonUp)
                rv.IsMiddleButtonPressed = false;
            if (args.Type == RawPointerEventType.RightButtonUp)
                rv.IsRightButtonPressed = false;
            return rv;
        }

        private MouseButton _lastMouseDownButton;
        private bool MouseDown(IMouseDevice device, ulong timestamp, IInputElement root, Point p,
            PointerPointProperties properties,
            InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                _pointer.Capture(hit);
                var source = GetSource(hit);
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
                    _lastMouseDownButton = properties.GetObsoleteMouseButton();
                    var e = new PointerPressedEventArgs(source, _pointer, root, p, timestamp, properties, inputModifiers, _clickCount);
                    source.RaiseEvent(e);
                    return e.Handled;
                }
            }

            return false;
        }

        private bool MouseMove(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties,
            InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            IInputElement source;

            if (_pointer.Captured == null)
            {
                source = SetPointerOver(this, timestamp, root, p, inputModifiers);
            }
            else
            {
                SetPointerOver(this, timestamp, root, _pointer.Captured, inputModifiers);
                source = _pointer.Captured;
            }

            var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, root,
                p, timestamp, properties, inputModifiers);

            source?.RaiseEvent(e);
            return e.Handled;
        }

        private bool MouseUp(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props,
            InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerReleasedEventArgs(source, _pointer, root, p, timestamp, props, inputModifiers,
                    _lastMouseDownButton);

                source?.RaiseEvent(e);
                _pointer.Capture(null);
                return e.Handled;
            }

            return false;
        }

        private bool MouseWheel(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props,
            Vector delta, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerWheelEventArgs(source, _pointer, root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private IInteractive GetSource(IVisual hit)
        {
            Contract.Requires<ArgumentNullException>(hit != null);

            return _pointer.Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement HitTest(IInputElement root, Point p)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            return _pointer.Captured ?? root.InputHitTest(p);
        }

        PointerEventArgs CreateSimpleEvent(RoutedEvent ev, ulong timestamp, IInteractive source, InputModifiers inputModifiers)
        {
            return new PointerEventArgs(ev, source, _pointer, null, default,
                timestamp, new PointerPointProperties(inputModifiers), inputModifiers);
        }

        private void ClearPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.PointerOverElement;
            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, timestamp, element, inputModifiers);

            if (element!=null && !element.IsAttachedToVisualTree)
            {
                // element has been removed from visual tree so do top down cleanup
                if (root.IsPointerOver)
                    ClearChildrenPointerOver(e, root,true);
            }
            while (element != null)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
                element = (IInputElement)element.VisualParent;
            }
            
            root.PointerOverElement = null;
        }

        private void ClearChildrenPointerOver(PointerEventArgs e, IInputElement element,bool clearRoot)
        {
            foreach (IInputElement el in element.VisualChildren)
            {
                if (el.IsPointerOver)
                {
                    ClearChildrenPointerOver(e, el, true);
                    break;
                }
            }
            if(clearRoot)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
            }
        }

        private IInputElement SetPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root, Point p, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.InputHitTest(p);

            if (element != root.PointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOver(device, timestamp, root, element, inputModifiers);
                }
                else
                {
                    ClearPointerOver(device, timestamp, root, inputModifiers);
                }
            }

            return element;
        }

        private void SetPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root, IInputElement element, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);
            Contract.Requires<ArgumentNullException>(element != null);

            IInputElement branch = null;

            var el = element;

            while (el != null)
            {
                if (el.IsPointerOver)
                {
                    branch = el;
                    break;
                }
                el = (IInputElement)el.VisualParent;
            }

            el = root.PointerOverElement;

            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, timestamp, el, inputModifiers);
            if (el!=null && branch!=null && !el.IsAttachedToVisualTree)
            {
                ClearChildrenPointerOver(e,branch,false);
            }
            
            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement)el.VisualParent;
            }            

            el = root.PointerOverElement = element;
            e.RoutedEvent = InputElement.PointerEnterEvent;

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement)el.VisualParent;
            }
        }
    }
}
