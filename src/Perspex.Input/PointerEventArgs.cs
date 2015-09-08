





namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public class PointerEventArgs : RoutedEventArgs
    {
        public IPointerDevice Device { get; set; }

        public Point GetPosition(IVisual relativeTo)
        {
            return this.Device.GetPosition(relativeTo);
        }
    }
}
