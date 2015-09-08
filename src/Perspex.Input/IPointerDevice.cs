





namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public interface IPointerDevice : IInputDevice
    {
        IInputElement Captured { get; }

        void Capture(IInputElement control);

        Point GetPosition(IVisual relativeTo);
    }
}
