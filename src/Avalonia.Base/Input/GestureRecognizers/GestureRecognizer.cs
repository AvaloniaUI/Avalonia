namespace Avalonia.Input.GestureRecognizers
{
    public abstract class GestureRecognizer : StyledElement
    {
        protected internal IInputElement? Target { get; internal set; }

        protected internal abstract void PointerPressed(PointerPressedEventArgs e);
        protected internal abstract void PointerReleased(PointerReleasedEventArgs e);
        protected internal abstract void PointerMoved(PointerEventArgs e);
        protected internal abstract void PointerCaptureLost(IPointer pointer);

        protected void Capture(IPointer pointer)
        {
            (pointer as Pointer)?.CaptureGestureRecognizer(this);
        }
    }
}
