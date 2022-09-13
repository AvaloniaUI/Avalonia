using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Arguments for <see cref="InputElement.Tapped"/> and <see cref="InputElement.DoubleTapped"/> events.
    /// </summary>
    public class TappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public TappedEventArgs(RoutedEvent routedEvent, PointerEventArgs lastPointerEventArgs)
            : base(routedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        /// <inheritdoc cref="PointerEventArgs.Pointer" />
        public IPointer Pointer => lastPointerEventArgs.Pointer;

        /// <inheritdoc cref="PointerEventArgs.KeyModifiers" />
        public KeyModifiers KeyModifiers => lastPointerEventArgs.KeyModifiers;

        /// <inheritdoc cref="PointerEventArgs.Timestamp" />
        public ulong Timestamp => lastPointerEventArgs.Timestamp;

        /// <inheritdoc cref="PointerEventArgs.GetPosition(IVisual?)" />
        public Point? GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }
}
