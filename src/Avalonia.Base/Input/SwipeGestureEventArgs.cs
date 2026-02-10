using System;
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
        /// <param name="gestureId">The unique identifier for this gesture.</param>
        /// <param name="delta">The pixel delta since the last event.</param>
        public SwipeGestureEventArgs(int gestureId, Vector delta)
            : base(Gestures.SwipeGestureEvent)
        {
            GestureId = gestureId;
            Delta = delta;
        }

        /// <summary>
        /// Gets the unique identifier for this gesture sequence.
        /// </summary>
        public int GestureId { get; }

        /// <summary>
        /// Gets the pixel delta since the last event.
        /// </summary>
        public Vector Delta { get; }

        private static int s_nextId = 1;

        internal static int GetNextFreeId() => s_nextId++;
    }

    /// <summary>
    /// Provides data for the swipe gesture ended event.
    /// </summary>
    public class SwipeGestureEndedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwipeGestureEndedEventArgs"/> class.
        /// </summary>
        /// <param name="gestureId">The unique identifier for this gesture.</param>
        public SwipeGestureEndedEventArgs(int gestureId)
            : base(Gestures.SwipeGestureEndedEvent)
        {
            GestureId = gestureId;
        }

        /// <summary>
        /// Gets the unique identifier for this gesture sequence.
        /// </summary>
        public int GestureId { get; }
    }
}
