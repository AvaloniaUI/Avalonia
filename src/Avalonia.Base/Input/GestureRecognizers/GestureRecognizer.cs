namespace Avalonia.Input.GestureRecognizers
{
    public abstract class GestureRecognizer : StyledElement
    {
        private PointerEventArgs? _currentPointerEventArgs;
        protected internal IInputElement? Target { get; internal set; }

        protected abstract void PointerPressed(PointerPressedEventArgs e);
        protected abstract void PointerReleased(PointerReleasedEventArgs e);
        protected abstract void PointerMoved(PointerEventArgs e);
        protected abstract void PointerCaptureLost(IPointer pointer);

        internal void PointerPressedInternal(PointerPressedEventArgs e)
        {
            _currentPointerEventArgs = e;
            PointerPressed(e);
            _currentPointerEventArgs = null;
        }

        internal void PointerReleasedInternal(PointerReleasedEventArgs e)
        {
            _currentPointerEventArgs = e;
            PointerReleased(e);
            _currentPointerEventArgs = null;
        }

        internal void PointerMovedInternal(PointerEventArgs e)
        {
            _currentPointerEventArgs = e;
            PointerMoved(e);
            _currentPointerEventArgs = null;
        }

        internal void PointerCaptureLostInternal(IPointer pointer)
        {
            PointerCaptureLost(pointer);
        }

        protected void Capture(IPointer pointer)
        {
            (pointer as Pointer)?.CaptureGestureRecognizer(this);

            _currentPointerEventArgs?.PreventGestureRecognition();
        }
    }
}
