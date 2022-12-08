using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class HoldGestureEventArgs : RoutedEventArgs
    {
        public int Id { get; }
        public Vector Delta { get; }
        public HoldingState HoldingState { get; }
        public PointerEventArgs? PointerEventArgs { get; }

        private static int _nextId = 1;

        internal static int GetNextFreeId() => _nextId++;
        
        public HoldGestureEventArgs(int id, PointerEventArgs? pointerEventArgs, HoldingState holdingState) : base(Gestures.HoldGestureEvent)
        {
            Id = id;
            HoldingState = holdingState;
            PointerEventArgs = pointerEventArgs;
        }
    }

    public enum HoldingState
    {
        Started,
        Completed,
        Cancelled,
    }
}
