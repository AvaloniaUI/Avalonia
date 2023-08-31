using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.UnitTests
{
    public class TouchTestHelper
    {
        private readonly Pointer _pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        private ulong _nextStamp = 1;
        private ulong Timestamp() => _nextStamp++;
        public IInputElement Captured => _pointer.Captured;

        public void Down(Interactive target, Point position = default, KeyModifiers modifiers = default)
        {
            Down(target, target, position, modifiers);
        }

        public void Down(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
        {
            _pointer.Capture((IInputElement)target);
            source.RaiseEvent(new PointerPressedEventArgs(source, _pointer, (Visual)source, position, Timestamp(), PointerPointProperties.None,
                modifiers));
        }

        public void Move(Interactive target, in Point position, KeyModifiers modifiers = default) => Move(target, target, position, modifiers);

        public void Move(Interactive target, Interactive source, in Point position, KeyModifiers modifiers = default)
        {
            var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, (Visual)target, position,
                Timestamp(), PointerPointProperties.None, modifiers);
            if (_pointer.CapturedGestureRecognizer != null)
                _pointer.CapturedGestureRecognizer.PointerMovedInternal(e);
            else
                target.RaiseEvent(e);

        }

        public void Up(Interactive target, Point position = default, KeyModifiers modifiers = default)
            => Up(target, target, position, modifiers);

        public void Up(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
        {
            var e = new PointerReleasedEventArgs(source, _pointer, (Visual)target, position, Timestamp(), PointerPointProperties.None,
                modifiers, MouseButton.None);

            if (_pointer.CapturedGestureRecognizer != null)
                _pointer.CapturedGestureRecognizer.PointerReleasedInternal(e);
            else
                source.RaiseEvent(e);

            _pointer.Capture(null);
            _pointer.CaptureGestureRecognizer(null);
        }

        public void Tap(Interactive target, Point position = default,  KeyModifiers modifiers = default)
            => Tap(target, target, position, modifiers);

        public void Tap(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
        {
            Down(target, source, position, modifiers);
            Up(target, source, position, modifiers);
        }

        public void Cancel()
        {
            _pointer.Capture(null);
            _pointer.CaptureGestureRecognizer(null);
        }
    }
}
