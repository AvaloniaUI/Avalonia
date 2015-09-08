





namespace Perspex.Input.Raw
{
    using System;
    using Perspex.Layout;

    public class RawMouseWheelEventArgs : RawMouseEventArgs
    {
        public RawMouseWheelEventArgs(
            IInputDevice device,
            uint timestamp,
            IInputRoot root,
            Point position,
            Vector delta, ModifierKeys modifierKeys)
            : base(device, timestamp, root, RawMouseEventType.Wheel, position, modifierKeys)
        {
            this.Delta = delta;
        }

        public Vector Delta { get; private set; }
    }
}
