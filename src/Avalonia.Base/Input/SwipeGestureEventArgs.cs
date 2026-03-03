using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Specifies the direction of a swipe gesture.
    /// </summary>
    public enum SwipeDirection { Left, Right, Up, Down }

    /// <summary>
    /// Provides data for the <see cref="InputElement.SwipeGestureEvent"/> routed event.
    /// </summary>
    public class SwipeGestureEventArgs : RoutedEventArgs
    {
        private static int _nextId = 1;
        internal static int GetNextFreeId() => _nextId++;

        /// <summary>
        /// Gets the unique identifier for this swipe gesture instance.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the direction of the swipe gesture.
        /// </summary>
        public SwipeDirection SwipeDirection { get; }

        /// <summary>
        /// Gets the total translation vector of the swipe gesture.
        /// </summary>
        public Vector Delta { get; }

        /// <summary>
        /// Gets the position, relative to the target element, where the swipe started.
        /// </summary>
        public Point StartPoint { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SwipeGestureEventArgs"/>.
        /// </summary>
        public SwipeGestureEventArgs(int id, SwipeDirection direction, Vector delta, Point startPoint)
            : base(InputElement.SwipeGestureEvent)
        {
            Id = id;
            SwipeDirection = direction;
            Delta = delta;
            StartPoint = startPoint;
        }
    }
}
