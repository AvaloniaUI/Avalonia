using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.UnitTests
{
    public class MouseTestHelper
    {

        class TestPointer : IPointer
        {
            public void Capture(IInputElement control)
            {
                Captured = control;
            }

            public IInputElement Captured { get; set; }
            public PointerType Type => PointerType.Mouse;
            public bool IsPrimary => true;
        }
        
        TestPointer _pointer = new TestPointer();

        private InputModifiers _pressedButtons;
        public IInputElement Captured => _pointer.Captured;

        InputModifiers Convert(MouseButton mouseButton)
            => (mouseButton == MouseButton.Left ? InputModifiers.LeftMouseButton
                : mouseButton == MouseButton.Middle ? InputModifiers.MiddleMouseButton
                : mouseButton == MouseButton.Right ? InputModifiers.RightMouseButton : InputModifiers.None);
        
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

        private MouseButton _pressedButton;

        InputModifiers GetModifiers(InputModifiers modifiers) => modifiers | _pressedButtons;
        
        public void Down(IInteractive target, MouseButton mouseButton = MouseButton.Left, Point position = default,
            InputModifiers modifiers = default, int clickCount = 1)
            => Down(target, target, mouseButton, position, modifiers, clickCount);
        
        public void Down(IInteractive target, IInteractive source, MouseButton mouseButton = MouseButton.Left, 
            Point position = default, InputModifiers modifiers = default, int clickCount = 1)
        {
            _pressedButtons |= Convert(mouseButton);
            var props = new PointerPointProperties(_pressedButtons);
            if (ButtonCount(props) > 1)
                Move(target, source, position);
            else
            {
                _pressedButton = mouseButton;
                target.RaiseEvent(new PointerPressedEventArgs(source, _pointer, (IVisual)source, position, props,
                    GetModifiers(modifiers), clickCount));
            }
        }

        public void Move(IInteractive target, in Point position, InputModifiers modifiers = default) => Move(target, target, position, modifiers);
        public void Move(IInteractive target, IInteractive source, in Point position, InputModifiers modifiers = default)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, (IVisual)target, position,
                new PointerPointProperties(_pressedButtons), GetModifiers(modifiers)));
        }

        public void Up(IInteractive target, MouseButton mouseButton = MouseButton.Left, Point position = default,
            InputModifiers modifiers = default)
            => Up(target, target, mouseButton, position, modifiers);
        
        public void Up(IInteractive target, IInteractive source, MouseButton mouseButton = MouseButton.Left,
            Point position = default, InputModifiers modifiers = default)
        {
            var conv = Convert(mouseButton);
            _pressedButtons = (_pressedButtons | conv) ^ conv;
            var props = new PointerPointProperties(_pressedButtons);
            if (ButtonCount(props) == 0)
                target.RaiseEvent(new PointerReleasedEventArgs(source, _pointer, (IVisual)target, position, props,
                    GetModifiers(modifiers), _pressedButton));
            else
                Move(target, source, position);
        }

        public void Click(IInteractive target, MouseButton button = MouseButton.Left, Point position = default,
            InputModifiers modifiers = default)
            => Click(target, target, button, position, modifiers);
        public void Click(IInteractive target, IInteractive source, MouseButton button = MouseButton.Left, 
            Point position = default, InputModifiers modifiers = default)
        {
            Down(target, source, button, position, modifiers);
            Up(target, source, button, position, modifiers);
        }
        
        public void Enter(IInteractive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerEnterEvent, target, _pointer, (IVisual)target, default,
                new PointerPointProperties(_pressedButtons), _pressedButtons));
        }

        public void Leave(IInteractive target)
        {
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerLeaveEvent, target, _pointer, (IVisual)target, default,
                new PointerPointProperties(_pressedButtons), _pressedButtons));
        }

    }
}
