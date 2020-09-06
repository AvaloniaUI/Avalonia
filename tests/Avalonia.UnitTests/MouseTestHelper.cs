using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.UnitTests
{
    public class MouseTestHelper
    {
        private readonly Pointer _pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        private ulong _nextStamp = 1;
        private ulong Timestamp() => _nextStamp++;

        private RawInputModifiers _pressedButtons;
        public IInputElement Captured => _pointer.Captured;

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

        public void Down(IInteractive target, MouseButton mouseButton = MouseButton.Left, Point position = default,
            KeyModifiers modifiers = default, int clickCount = 1)
        {
            Down(target, target, mouseButton, position, modifiers, clickCount);
        }

        public void Down(IInteractive target, IInteractive source, MouseButton mouseButton = MouseButton.Left, 
            Point position = default, KeyModifiers modifiers = default, int clickCount = 1)
        {
            _pressedButtons |= Convert(mouseButton);
            var props = new PointerPointProperties((RawInputModifiers)_pressedButtons,
                mouseButton == MouseButton.Left ? PointerUpdateKind.LeftButtonPressed
                : mouseButton == MouseButton.Middle ? PointerUpdateKind.MiddleButtonPressed
                : mouseButton == MouseButton.Right ? PointerUpdateKind.RightButtonPressed : PointerUpdateKind.Other
            );
            if (ButtonCount(props) > 1)
                Move(target, source, position);
            else
            {
                _pressedButton = mouseButton;
                _pointer.Capture((IInputElement)target);
                source.RaiseEvent(new PointerPressedEventArgs(source, _pointer, (IVisual)source, position, Timestamp(), props,
                    modifiers, clickCount));
            }
        }

        public void Move(IInteractive target, in Point position, KeyModifiers modifiers = default) => Move(target, target, position, modifiers);
        public void Move(IInteractive target, IInteractive source, in Point position, KeyModifiers modifiers = default)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, (IVisual)target, position,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), modifiers));
        }

        public void Up(IInteractive target, MouseButton mouseButton = MouseButton.Left, Point position = default,
            KeyModifiers modifiers = default)
            => Up(target, target, mouseButton, position, modifiers);
        
        public void Up(IInteractive target, IInteractive source, MouseButton mouseButton = MouseButton.Left,
            Point position = default, KeyModifiers modifiers = default)
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
                target.RaiseEvent(new PointerReleasedEventArgs(source, _pointer, (IVisual)target, position,
                    Timestamp(), props, modifiers, _pressedButton));
                _pointer.Capture(null);
            }
            else
                Move(target, source, position);
        }

        public void Click(IInteractive target, MouseButton button = MouseButton.Left, Point position = default,
            KeyModifiers modifiers = default)
            => Click(target, target, button, position, modifiers);
        public void Click(IInteractive target, IInteractive source, MouseButton button = MouseButton.Left, 
            Point position = default, KeyModifiers modifiers = default)
        {
            Down(target, source, button, position, modifiers);
            Up(target, source, button, position, modifiers);
        }
        
        public void Enter(IInteractive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerEnterEvent, target, _pointer, (IVisual)target, default,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
        }

        public void Leave(IInteractive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerLeaveEvent, target, _pointer, (IVisual)target, default,
                Timestamp(), new PointerPointProperties((RawInputModifiers)_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
        }

    }
}
