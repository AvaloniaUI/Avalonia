using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a pen/stylus device.
    /// </summary>
    public class PenDevice : IPenDevice, IDisposable
    {
        private readonly Pointer _pointer;
        private bool _disposed;
        private PixelPoint? _position; 

        public PenDevice(Pointer? pointer = null)
        {
            _pointer = pointer ?? new Pointer(Pointer.GetNextFreeId(), PointerType.Pen, true);
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
        public IInputElement? Captured => _pointer.Captured;

        public bool IsEraser { get; set; }
        public bool IsInverted { get; set; }
        public bool IsBarrel { get; set; }
        public int XTilt { get; set; }
        public int YTilt { get; set; }
        public uint Pressure { get; set; }
        public uint Twist { get; set; }

        /// <summary>
        /// Captures mouse input to the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <remarks>
        /// When an element captures the mouse, it receives mouse input whether the cursor is 
        /// within the control's bounds or not. The current mouse capture control is exposed
        /// by the <see cref="Captured"/> property.
        /// </remarks>
        public void Capture(IInputElement? control)
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
            relativeTo = relativeTo ?? throw new ArgumentNullException(nameof(relativeTo));

            if (relativeTo.VisualRoot == null)
            {
                throw new InvalidOperationException("Control is not attached to visual tree.");
            }

#pragma warning disable CS0618 // Type or member is obsolete
            var rootPoint = relativeTo.VisualRoot.PointToClient(_position ?? new PixelPoint(-1, -1));
#pragma warning restore CS0618 // Type or member is obsolete
            var transform = relativeTo.VisualRoot.TransformToVisual(relativeTo);
            return rootPoint * transform!.Value;
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawPointerEventArgs margs)
                ProcessRawEvent(margs);
        }

        public void TopLevelClosed(IInputRoot root)
        {
            ClearPointerOver(this, 0, root, PointerPointProperties.None, KeyModifiers.None);
        }

        public void SceneInvalidated(IInputRoot root, Rect rect)
        {
            // Pointer is outside of the target area
            if (_position == null )
            {
                if (root.PointerOverElement != null)
                    ClearPointerOver(this, 0, root, PointerPointProperties.None, KeyModifiers.None);
                return;
            }
            
            
            var clientPoint = root.PointToClient(_position.Value);

            if (rect.Contains(clientPoint))
            {
                if (_pointer.Captured == null)
                {
                    SetPointerOver(this, 0 /* TODO: proper timestamp */, root, clientPoint,
                        PointerPointProperties.None, KeyModifiers.None);
                }
                else
                {
                    SetPointerOver(this, 0 /* TODO: proper timestamp */, root, _pointer.Captured,
                        PointerPointProperties.None, KeyModifiers.None);
                }
            }
        }
        
        private void ProcessRawEvent(RawPointerEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var pen = (PenDevice)e.Device;
            if(pen._disposed)
                return;

            _position = e.Root.PointToScreen(e.Position);
            var props = CreateProperties(e);
            var keyModifiers = KeyModifiersUtils.ConvertToKey(e.InputModifiers);
            switch (e.Type)
            {
                case RawPointerEventType.LeaveWindow:
                    LeaveWindow(pen, e.Timestamp, e.Root, props, keyModifiers);
                    break;
                case RawPointerEventType.LeftButtonDown:
                    e.Handled = PenDown(pen, e.Timestamp, e.Root, e.Position, props, keyModifiers);
                    break;
                case RawPointerEventType.LeftButtonUp:
                    e.Handled = PenUp(pen, e.Timestamp, e.Root, e.Position, props, keyModifiers);
                    break;
                case RawPointerEventType.Move:
                    e.Handled = PenMove(pen, e.Timestamp, e.Root, e.Position, props, keyModifiers);
                    break;
            }
        }

        private void LeaveWindow(IPenDevice device, ulong timestamp, IInputRoot root, PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            _position = null;
            ClearPointerOver(this, timestamp, root, properties, inputModifiers);
        }


        private PointerPointProperties CreateProperties(RawPointerEventArgs args)
        {
            var kind = PointerUpdateKind.Other;

            if (args.Type == RawPointerEventType.LeftButtonDown)
                kind = PointerUpdateKind.LeftButtonPressed;
            if (args.Type == RawPointerEventType.LeftButtonUp)
                kind = PointerUpdateKind.LeftButtonReleased;

            return new PointerPointProperties(args.InputModifiers, kind, 
                Twist, Pressure, XTilt, YTilt, IsEraser, IsInverted, IsBarrel);
        }

        private MouseButton _lastMouseDownButton;
        private bool PenDown(IPenDevice device, ulong timestamp, IInputElement root, Point p,
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);

            if (hit != null)
            {
                _pointer.Capture(hit);
                var source = GetSource(hit);
                if (source != null)
                {
                    _lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
                    var e = new PointerPressedEventArgs(source, _pointer, root, p, timestamp, properties, inputModifiers, 1);
                    source.RaiseEvent(e);
                    return e.Handled;
                }
            }

            return false;
        }

        private bool PenMove(IPenDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            IInputElement? source;

            if (_pointer.Captured == null)
            {
                source = SetPointerOver(this, timestamp, root, p,  properties, inputModifiers);
            }
            else
            {
                SetPointerOver(this, timestamp, root, _pointer.Captured, properties, inputModifiers);
                source = _pointer.Captured;
            }

            if (source is object)
            {
                var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, root,
                    p, timestamp, properties, inputModifiers);

                source.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool PenUp(IPenDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);
            var source = GetSource(hit);

            if (source is not null)
            {
                var e = new PointerReleasedEventArgs(source, _pointer, root, p, timestamp, props, inputModifiers,
                    _lastMouseDownButton);

                source?.RaiseEvent(e);
                _pointer.Capture(null);
                return e.Handled;
            }

            return false;
        }

        private IInteractive? GetSource(IVisual? hit)
        {
            if (hit is null)
                return null;

            return _pointer.Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement? HitTest(IInputElement root, Point p)
        {
            root = root ?? throw new ArgumentNullException(nameof(root));

            return _pointer.Captured ?? root.InputHitTest(p);
        }

        PointerEventArgs CreateSimpleEvent(RoutedEvent ev, ulong timestamp, IInteractive? source,
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            return new PointerEventArgs(ev, source, _pointer, null, default,
                timestamp, properties, inputModifiers);
        }

        private void ClearPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root,
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var element = root.PointerOverElement;
            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, timestamp, element, properties, inputModifiers);

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
                element = (IInputElement?)element.VisualParent;
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

        private IInputElement? SetPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root, Point p, 
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var element = root.InputHitTest(p);

            if (element != root.PointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOver(device, timestamp, root, element, properties, inputModifiers);
                }
                else
                {
                    ClearPointerOver(device, timestamp, root, properties, inputModifiers);
                }
            }

            return element;
        }

        private void SetPointerOver(IPointerDevice device, ulong timestamp, IInputRoot root, IInputElement element,
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));
            element = element ?? throw new ArgumentNullException(nameof(element));

            IInputElement? branch = null;

            IInputElement? el = element;

            while (el != null)
            {
                if (el.IsPointerOver)
                {
                    branch = el;
                    break;
                }
                el = (IInputElement?)el.VisualParent;
            }

            el = root.PointerOverElement;

            var e = CreateSimpleEvent(InputElement.PointerLeaveEvent, timestamp, el, properties, inputModifiers);
            if (el!=null && branch!=null && !el.IsAttachedToVisualTree)
            {
                ClearChildrenPointerOver(e,branch,false);
            }
            
            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement?)el.VisualParent;
            }            

            el = root.PointerOverElement = element;
            e.RoutedEvent = InputElement.PointerEnterEvent;

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement?)el.VisualParent;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _pointer?.Dispose();
        }
    }
}
