using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.UnitTests
{
    public class MouseTestHelper(PointerType pointerType = PointerType.Mouse)
    {
        private readonly Pointer _pointer = new Pointer(Pointer.GetNextFreeId(), pointerType, true);
        private ulong _nextStamp = 1;
        private ulong Timestamp() => _nextStamp++;

        private RawInputModifiers _pressedButtons;
        public IInputElement? Captured => _pointer.Captured;

        private RawInputModifiers Convert(MouseButton mouseButton)
        {
            return mouseButton switch
            {
                MouseButton.Left => RawInputModifiers.LeftMouseButton,
                MouseButton.Right => RawInputModifiers.RightMouseButton,
                MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
                _ => RawInputModifiers.None,
            };
        }

        private int ButtonCount(PointerPointProperties props)
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

        private MouseButton _pressedButton;

        public void Down(Interactive target, MouseButton mouseButton = MouseButton.Left, Point? position = null,
            KeyModifiers modifiers = default, int clickCount = 1)
        {
            Down(target, target, mouseButton, position, modifiers, clickCount);
        }

        public void Down(Interactive target, Interactive source, MouseButton mouseButton = MouseButton.Left, 
            Point? position = null, KeyModifiers modifiers = default, int clickCount = 1)
        {
            _pressedButtons |= Convert(mouseButton);
            var props = new PointerPointProperties((RawInputModifiers)_pressedButtons,
                mouseButton == MouseButton.Left ? PointerUpdateKind.LeftButtonPressed
                : mouseButton == MouseButton.Middle ? PointerUpdateKind.MiddleButtonPressed
                : mouseButton == MouseButton.Right ? PointerUpdateKind.RightButtonPressed : PointerUpdateKind.Other
            );
            if (ButtonCount(props) > 1)
                Move(target, source, position ?? default);
            else
            {
                _pressedButton = mouseButton;
                _pointer.Capture((IInputElement)target);
                source.RaiseEvent(new PointerPressedEventArgs(source, _pointer, GetRoot(target), position ?? MidpointRelativeToRoot(target), Timestamp(), props,
                    modifiers, clickCount));
            }
        }

        public void Move(Interactive target, in Point position, KeyModifiers modifiers = default) => Move(target, target, position, modifiers);

        public void Move(Interactive target, Interactive source, in Point position, KeyModifiers modifiers = default)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, GetRoot(target), position,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), modifiers));
        }

        public void Up(Interactive target, MouseButton mouseButton = MouseButton.Left, Point? position = null,
            KeyModifiers modifiers = default)
            => Up(target, target, mouseButton, position, modifiers);
        
        public void Up(Interactive target, Interactive source, MouseButton mouseButton = MouseButton.Left,
            Point? position = null, KeyModifiers modifiers = default)
        {
            var conv = Convert(mouseButton);
            _pressedButtons = (_pressedButtons | conv) ^ conv;
            var props = new PointerPointProperties((RawInputModifiers)_pressedButtons,
                mouseButton == MouseButton.Left ? PointerUpdateKind.LeftButtonReleased
                : mouseButton == MouseButton.Middle ? PointerUpdateKind.MiddleButtonReleased
                : mouseButton == MouseButton.Right ? PointerUpdateKind.RightButtonReleased : PointerUpdateKind.Other
            );
            if (ButtonCount(props) == 0)
            {
                target.RaiseEvent(new PointerReleasedEventArgs(source, _pointer, GetRoot(target), position ?? MidpointRelativeToRoot(target),
                    Timestamp(), props, modifiers, _pressedButton));
                _pointer.Capture(null);
            }
            else
                Move(target, source, position ?? default);
        }

        public void Click(Interactive target, MouseButton button = MouseButton.Left, Point? position = null,
            KeyModifiers modifiers = default)
            => Click(target, target, button, position, modifiers);

        public void Click(Interactive target, Interactive source, MouseButton button = MouseButton.Left, 
            Point? position = null, KeyModifiers modifiers = default)
        {
            Down(target, source, button, position, modifiers);
            var captured = (_pointer.Captured as Interactive) ?? source;
            Up(captured, captured, button, position, modifiers);
        }

        public void DoubleClick(Interactive target, MouseButton button = MouseButton.Left, Point? position = null,
            KeyModifiers modifiers = default)
            => DoubleClick(target, target, button, position, modifiers);

        public void DoubleClick(Interactive target, Interactive source, MouseButton button = MouseButton.Left,
            Point? position = null, KeyModifiers modifiers = default)
        {
            Down(target, source, button, position, modifiers, clickCount: 1);
            var captured = (_pointer.Captured as Interactive) ?? source;
            Up(captured, captured, button, position, modifiers);
            Down(target, source, button, position, modifiers, clickCount: 2);
        }

        public void Enter(Interactive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, target, _pointer, (Visual)target, default,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
        }

        public void Leave(Interactive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, target, _pointer, (Visual)target, default,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
        }

        private Visual GetRoot(Interactive source)
        {
            return ((source as Visual)?.GetVisualRoot() as Visual) ?? (Visual)source;
        }

        private Point MidpointRelativeToRoot(Interactive element)
        {
            var root = GetRoot(element);
            return element.TranslatePoint(new(element.Bounds.Width / 2, element.Bounds.Height / 2), root).GetValueOrDefault();
        }
    }
}
