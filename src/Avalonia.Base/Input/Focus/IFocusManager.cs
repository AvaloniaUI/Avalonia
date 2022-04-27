using System;

namespace Avalonia.Input
{
    // TODO: Decorate with new [NotClientImplementable]
    public interface IFocusManager
    {      
        IInputElement? FocusedElement { get; }

        FocusState FocusedElementState { get; }

        void SetFocusedElement(IInputElement? element, FocusState state);
    }
}
