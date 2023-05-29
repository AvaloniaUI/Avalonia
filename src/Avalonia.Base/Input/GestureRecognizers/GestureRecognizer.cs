using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input.GestureRecognizers
{
    public abstract class GestureRecognizer : StyledElement
    {
        public abstract IInputElement? Target { get; }
        public abstract void Initialize(IInputElement target);
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
