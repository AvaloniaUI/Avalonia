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
    public class MouseDevice : IMouseDevice, IPointer
    {
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;
        private IInputElement _captured;
        private IDisposable _capturedSubscription;

        PointerType IPointer.Type => PointerType.Mouse;
        bool IPointer.IsPrimary => true;
        int IPointer.Id { get; } = PointerIds.Next();
        
        /// <summary>
        /// Gets the control that is currently capturing by the mouse, if any.
        /// </summary>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether the cursor is 
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
            if (!e.Handled && e is RawMouseEventArgs margs)
                ProcessRawEvent(margs);
        }

        public void SceneInvalidated(IInputRoot root, Rect rect)
        {
            var clientPoint = root.PointToClient(Position);

            if (rect.Contains(clientPoint))
            {
                if (Captured == null)
                {
                    SetPointerOver(this, root, clientPoint, InputModifiers.None);
                }
                else
                {
                    SetPointerOver(this, root, Captured, InputModifiers.None);
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
        
        private void ProcessRawEvent(RawMouseEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            var mouse = (IMouseDevice)e.Device;

            Position = e.Root.PointToScreen(e.Position);
            var props = CreateProperties(e);
            switch (e.Type)
            {
                case RawMouseEventType.LeaveWindow:
                    LeaveWindow(mouse, e.Root, e.InputModifiers);
                    break;
                case RawMouseEventType.LeftButtonDown:
                case RawMouseEventType.RightButtonDown:
                case RawMouseEventType.MiddleButtonDown:
                    if (ButtonCount(props) > 1)
                        e.Handled = MouseMove(mouse, e.Root, e.Position, props, e.InputModifiers);
                    else
                        e.Handled = MouseDown(mouse, e.Timestamp, e.Root, e.Position,
                            props, e.InputModifiers);
                    break;
                case RawMouseEventType.LeftButtonUp:
                case RawMouseEventType.RightButtonUp:
                case RawMouseEventType.MiddleButtonUp:
                    if (ButtonCount(props) != 0)
                        e.Handled = MouseMove(mouse, e.Root, e.Position, props, e.InputModifiers);
                    else
                        e.Handled = MouseUp(mouse, e.Root, e.Position, props, e.InputModifiers);
                    break;
                case RawMouseEventType.Move:
                    e.Handled = MouseMove(mouse, e.Root, e.Position, props, e.InputModifiers);
                    break;
                case RawMouseEventType.Wheel:
                    e.Handled = MouseWheel(mouse, e.Root, e.Position, props, ((RawMouseWheelEventArgs)e).Delta, e.InputModifiers);
                    break;
            }
        }

        private void LeaveWindow(IMouseDevice device, IInputRoot root, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            ClearPointerOver(this, root, inputModifiers);
        }


        PointerPointProperties CreateProperties(RawMouseEventArgs args)
        {
            var rv = new PointerPointProperties(args.InputModifiers);

            if (args.Type == RawMouseEventType.LeftButtonDown)
                rv.IsLeftButtonPressed = true;
            if (args.Type == RawMouseEventType.MiddleButtonDown)
                rv.IsMiddleButtonPressed = true;
            if (args.Type == RawMouseEventType.RightButtonDown)
                rv.IsRightButtonPressed = true;
            if (args.Type == RawMouseEventType.LeftButtonUp)
                rv.IsLeftButtonPressed = false;
            if (args.Type == RawMouseEventType.MiddleButtonUp)
                rv.IsMiddleButtonPressed = false;
            if (args.Type == RawMouseEventType.RightButtonDown)
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
                    _lastMouseDownButton = properties.GetObsoleteMouseButton();
                    var e = new PointerPressedEventArgs(source, this, root, p, properties, inputModifiers, _clickCount);

                    source.RaiseEvent(e);
                    return e.Handled;
                }
            }

            return false;
        }

        private bool MouseMove(IMouseDevice device, IInputRoot root, Point p, PointerPointProperties properties,
            InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            IInputElement source;

            if (Captured == null)
            {
                source = SetPointerOver(this, root, p, inputModifiers);
            }
            else
            {
                SetPointerOver(this, root, Captured, inputModifiers);
                source = Captured;
            }

            var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, this, root,
                p, properties, inputModifiers);

            source?.RaiseEvent(e);
            return e.Handled;
        }

        private bool MouseUp(IMouseDevice device, IInputRoot root, Point p, PointerPointProperties props,
            InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerReleasedEventArgs(source, this, root, p, props, inputModifiers, _lastMouseDownButton);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool MouseWheel(IMouseDevice device, IInputRoot root, Point p,
            PointerPointProperties props,
            Vector delta, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerWheelEventArgs(source, this, root, p, props, inputModifiers, delta);

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

        PointerEventArgs CreateSimpleEvent(RoutedEvent ev, IInteractive source, InputModifiers inputModifiers)
        {
            return new PointerEventArgs(ev, source, this, null, default,
                new PointerPointProperties(inputModifiers), inputModifiers);
        }

        private void ClearPointerOver(IPointerDevice device, IInputRoot root, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.PointerOverElement;
            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, element, inputModifiers);

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

        private IInputElement SetPointerOver(IPointerDevice device, IInputRoot root, Point p, InputModifiers inputModifiers)
        {
            Contract.Requires<ArgumentNullException>(device != null);
            Contract.Requires<ArgumentNullException>(root != null);

            var element = root.InputHitTest(p);

            if (element != root.PointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOver(device, root, element, inputModifiers);
                }
                else
                {
                    ClearPointerOver(device, root, inputModifiers);
                }
            }

            return element;
        }

        private void SetPointerOver(IPointerDevice device, IInputRoot root, IInputElement element, InputModifiers inputModifiers)
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

            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, el, inputModifiers);
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
