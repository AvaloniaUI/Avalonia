using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class TappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;
        public IPointer Pointer => lastPointerEventArgs.Pointer;
        public InputModifiers InputModifiers => lastPointerEventArgs.InputModifiers;
        public KeyModifiers KeyModifiers => lastPointerEventArgs.KeyModifiers;
        public ulong Timestamp => lastPointerEventArgs.Timestamp;
        public IPointerDevice Device => lastPointerEventArgs.Device;
        
        public TappedEventArgs(RoutedEvent routedEvent, PointerEventArgs lastPointerEventArgs)
            : base(routedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public Point GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }
}
