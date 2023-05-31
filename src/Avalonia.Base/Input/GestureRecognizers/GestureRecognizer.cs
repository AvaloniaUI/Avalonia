namespace Avalonia.Input.GestureRecognizers
{
    public abstract class GestureRecognizer : StyledElement
    {
        protected internal IInputElement? Target { get; internal set; }

        protected abstract void PointerPressed(PointerPressedEventArgs e);
        protected abstract void PointerReleased(PointerReleasedEventArgs e);
        protected abstract void PointerMoved(PointerEventArgs e);
        protected abstract void PointerCaptureLost(IPointer pointer);

        internal void PointerPressedInternal(PointerPressedEventArgs e)
        {
            PointerPressed(e);
        }

        internal void PointerReleasedInternal(PointerReleasedEventArgs e)
        {
            PointerReleased(e);
        }

        internal void PointerMovedInternal(PointerEventArgs e)
        {
            PointerMoved(e);
        }

        internal void PointerCaptureLostInternal(IPointer pointer)
        {
            PointerCaptureLost(pointer);
        }

        protected void Capture(IPointer pointer)
        {
            (pointer as Pointer)?.CaptureGestureRecognizer(this);
        }
    }
}
