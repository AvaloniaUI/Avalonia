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
            : base(Gestures.SwipeGestureEvent)
        {
            Id = id;
            Delta = delta;
            Velocity = velocity;
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
            : base(Gestures.SwipeGestureEndedEvent)
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
