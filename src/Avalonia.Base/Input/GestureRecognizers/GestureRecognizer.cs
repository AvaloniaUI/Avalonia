namespace Avalonia.Input.GestureRecognizers
{
    public abstract class GestureRecognizer : StyledElement
    {
        protected internal IInputElement? Target { get; internal set; }

        public abstract void PointerPressed(PointerPressedEventArgs e);
        public abstract void PointerReleased(PointerReleasedEventArgs e);
        public abstract void PointerMoved(PointerEventArgs e);
        public abstract void PointerCaptureLost(IPointer pointer);

        protected void Capture(IPointer pointer)
        {
            (pointer as Pointer)?.CaptureGestureRecognizer(this);
        }
    }
}
