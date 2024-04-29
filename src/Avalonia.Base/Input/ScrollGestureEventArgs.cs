using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class ScrollGestureEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public Vector Delta { get; }
        /// <summary>
        /// When set the ScrollGestureRecognizer should stop its current active scroll gesture.
        /// </summary>
        public bool ShouldEndScrollGesture { get; set; }
        private static int _nextId = 1;

        public static int GetNextFreeId() => _nextId++;

        public ScrollGestureEventArgs(int id, Vector delta) : base(Gestures.ScrollGestureEvent)
        {
            Id = id;
            Delta = delta;
        }
    }

    public class ScrollGestureEndedEventArgs : RoutedEventArgs
    {
        public int Id { get; }

        public ScrollGestureEndedEventArgs(int id) : base(Gestures.ScrollGestureEndedEvent)
        {
            Id = id;
        }
    }

    public sealed class ScrollGestureInertiaStartingEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public Vector Inertia { get; }

        internal ScrollGestureInertiaStartingEventArgs(int id, Vector inertia) : base(Gestures.ScrollGestureInertiaStartingEvent)
        {
            Id = id;
            Inertia = inertia;
        }
    }
}
