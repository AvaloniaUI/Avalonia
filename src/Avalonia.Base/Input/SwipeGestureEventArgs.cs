using System;
using System.Threading;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides data for swipe gesture events.
    /// </summary>
    public class SwipeGestureEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwipeGestureEventArgs"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for this gesture.</param>
        /// <param name="delta">The pixel delta since the last event.</param>
        /// <param name="velocity">The current swipe velocity in pixels per second.</param>
        public SwipeGestureEventArgs(int id, Vector delta, Vector velocity)
            : base(InputElement.SwipeGestureEvent)
        {
            Id = id;
            Delta = delta;
            Velocity = velocity;
            SwipeDirection = Math.Abs(delta.X) >= Math.Abs(delta.Y)
                ? (delta.X <= 0 ? SwipeDirection.Right : SwipeDirection.Left)
                : (delta.Y <= 0 ? SwipeDirection.Down : SwipeDirection.Up);
        }

        /// <summary>
        /// Gets the unique identifier for this gesture sequence.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the pixel delta since the last event.
        /// </summary>
        public Vector Delta { get; }

        /// <summary>
        /// Gets the current swipe velocity in pixels per second.
        /// </summary>
        public Vector Velocity { get; }

        /// <summary>
        /// Gets the direction of the dominant swipe axis.
        /// </summary>
        public SwipeDirection SwipeDirection { get; }

        private static int s_nextId;

        internal static int GetNextFreeId() => Interlocked.Increment(ref s_nextId);
    }

    /// <summary>
    /// Provides data for the swipe gesture ended event.
    /// </summary>
    public class SwipeGestureEndedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwipeGestureEndedEventArgs"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for this gesture.</param>
        /// <param name="velocity">The swipe velocity at release in pixels per second.</param>
        public SwipeGestureEndedEventArgs(int id, Vector velocity)
            : base(InputElement.SwipeGestureEndedEvent)
        {
            Id = id;
            Velocity = velocity;
        }

        /// <summary>
        /// Gets the unique identifier for this gesture sequence.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the swipe velocity at release in pixels per second.
        /// </summary>
        public Vector Velocity { get; }
    }
}
