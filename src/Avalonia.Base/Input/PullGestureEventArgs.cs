using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class PullGestureEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public Vector Delta { get; }
        public PullDirection PullDirection { get; }

        private static int _nextId = 1;

        internal static int GetNextFreeId() => _nextId++;
        
        public PullGestureEventArgs(int id, Vector delta, PullDirection pullDirection) : base(Gestures.PullGestureEvent)
        {
            Id = id;
            Delta = delta;
            PullDirection = pullDirection;
        }
    }

    public class PullGestureEndedEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public PullDirection PullDirection { get; }

        public PullGestureEndedEventArgs(int id, PullDirection pullDirection) : base(Gestures.PullGestureEndedEvent)
        {
            Id = id;
            PullDirection = pullDirection;
        }
    }

    public enum PullDirection
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }
}
