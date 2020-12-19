using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class DoubleTappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public DoubleTappedEventArgs(PointerEventArgs lastPointerEventArgs)
            : base(Gestures.DoubleTappedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public Point GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }

    public class TappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public TappedEventArgs(PointerEventArgs lastPointerEventArgs)
            : base(Gestures.DoubleTappedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public Point GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }

    public class RightTappedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs lastPointerEventArgs;

        public RightTappedEventArgs(PointerEventArgs lastPointerEventArgs)
            : base(Gestures.DoubleTappedEvent)
        {
            this.lastPointerEventArgs = lastPointerEventArgs;
        }

        public Point GetPosition(IVisual? relativeTo) => lastPointerEventArgs.GetPosition(relativeTo);
    }
}
