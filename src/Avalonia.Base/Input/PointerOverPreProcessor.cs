using System;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    internal class PointerOverPreProcessor : IObserver<RawInputEventArgs>
    {
        private IPointerDevice? _lastActivePointerDevice;
        private (IPointer pointer, PixelPoint position)? _lastPointer;

        private readonly IInputRoot _inputRoot;

        public PointerOverPreProcessor(IInputRoot inputRoot)
        {
            _inputRoot = inputRoot ?? throw new ArgumentNullException(nameof(inputRoot));
        }

        public PixelPoint? LastPosition => _lastPointer?.position;
        
        public void OnCompleted()
        {
            ClearPointerOver();
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(RawInputEventArgs value)
        {
            if (value is RawPointerEventArgs args
                && args.Root == _inputRoot
                && value.Device is IPointerDevice pointerDevice)
            {
                if (pointerDevice != _lastActivePointerDevice)
                {
                    ClearPointerOver();

                    // Set last active device before processing input, because ClearPointerOver might be called and clear last device.
                    _lastActivePointerDevice = pointerDevice;
                }

                if (args.Type is RawPointerEventType.LeaveWindow or RawPointerEventType.NonClientLeftButtonDown
                    && _lastPointer is (var lastPointer, var lastPosition))
                {
                    _lastPointer = null;
                    ClearPointerOver(lastPointer, args.Root, 0, args.Root.PointToClient(lastPosition),
                        new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind()),
                        args.InputModifiers.ToKeyModifiers());
                }
                else if (pointerDevice.TryGetPointer(args) is IPointer pointer
                    && pointer.Type != PointerType.Touch)
                {
                    var element = pointer.Captured ?? args.InputHitTestResult;

                    SetPointerOver(pointer, args.Root, element, args.Timestamp, args.Position,
                        new PointerPointProperties(args.InputModifiers, args.Type.ToUpdateKind()),
                        args.InputModifiers.ToKeyModifiers());
                }
            }
        }

        public void SceneInvalidated(Rect dirtyRect)
        {
            if (_lastPointer is (var pointer, var position))
            {
                var clientPoint = _inputRoot.PointToClient(position);

                if (dirtyRect.Contains(clientPoint))
                {
                    var element = pointer.Captured ?? _inputRoot.InputHitTest(clientPoint);
                    SetPointerOver(pointer, _inputRoot, element, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
                }
                else if (!_inputRoot.Bounds.Contains(clientPoint))
                {
                    ClearPointerOver(pointer, _inputRoot, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
                }
            }
        }

        private void ClearPointerOver()
        {
            if (_lastPointer is (var pointer, var position))
            {
                var clientPoint = _inputRoot.PointToClient(position);
                ClearPointerOver(pointer, _inputRoot, 0, clientPoint, PointerPointProperties.None, KeyModifiers.None);
            }
            _lastPointer = null;
            _lastActivePointerDevice = null;
        }

        private void ClearPointerOver(IPointer pointer, IInputRoot root,
            ulong timestamp, Point? position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            var element = root.PointerOverElement;
            if (element is null)
            {
                return;
            }

            // Do not pass rootVisual, when we have unknown position,
            // so GetPosition won't return invalid values.
            var e = new PointerEventArgs(InputElement.PointerExitedEvent, element, pointer,
                position.HasValue ? root : null, position.HasValue ? position.Value : default,
                timestamp, properties, inputModifiers);

            if (element != null && !element.IsAttachedToVisualTree)
            {
                // element has been removed from visual tree so do top down cleanup
                if (root.IsPointerOver)
                {
                    ClearChildrenPointerOver(e, root, true);
                }
            }
            while (element != null)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
                element = (IInputElement?)element.VisualParent;
            }

            root.PointerOverElement = null;
            _lastActivePointerDevice = null;
            _lastPointer = null;
        }

        private void ClearChildrenPointerOver(PointerEventArgs e, IInputElement element, bool clearRoot)
        {
            foreach (IInputElement el in element.VisualChildren)
            {
                if (el.IsPointerOver)
                {
                    ClearChildrenPointerOver(e, el, true);
                    break;
                }
            }
            if (clearRoot)
            {
                e.Source = element;
                e.Handled = false;
                element.RaiseEvent(e);
            }
        }

        private void SetPointerOver(IPointer pointer, IInputRoot root, IInputElement? element,
            ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            var pointerOverElement = root.PointerOverElement;

            if (element != pointerOverElement)
            {
                if (element != null)
                {
                    SetPointerOverToElement(pointer, root, element, timestamp, position, properties, inputModifiers);
                }
                else
                {
                    ClearPointerOver(pointer, root, timestamp, position, properties, inputModifiers);
                }
            }

            _lastPointer = (pointer, root.PointToScreen(position));
        }

        private void SetPointerOverToElement(IPointer pointer, IInputRoot root, IInputElement element,
            ulong timestamp, Point position, PointerPointProperties properties, KeyModifiers inputModifiers)
        {
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

            var e = new PointerEventArgs(InputElement.PointerExitedEvent, el, pointer, root, position,
                timestamp, properties, inputModifiers);
            if (el != null && branch != null && !el.IsAttachedToVisualTree)
            {
                ClearChildrenPointerOver(e, branch, false);
            }

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement?)el.VisualParent;
            }

            el = root.PointerOverElement = element;

            e.RoutedEvent = InputElement.PointerEnteredEvent;

            while (el != null && el != branch)
            {
                e.Source = el;
                e.Handled = false;
                el.RaiseEvent(e);
                el = (IInputElement?)el.VisualParent;
            }
        }
    }
}
