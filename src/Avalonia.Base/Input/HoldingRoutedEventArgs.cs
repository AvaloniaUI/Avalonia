using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class HoldingRoutedEventArgs : RoutedEventArgs
    {
        public HoldingState HoldingState { get; }
        
        public HoldingRoutedEventArgs(HoldingState holdingState) : base(Gestures.HoldingEvent)
        {
            HoldingState = holdingState;
        }
    }

    public enum HoldingState
    {
        Started,
        Completed,
        Cancelled,
    }
}
