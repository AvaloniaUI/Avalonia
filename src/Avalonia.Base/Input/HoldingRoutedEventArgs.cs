using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class HoldingRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the state of the <see cref="Gestures.HoldingEvent"/> event.
        /// </summary>
        public HoldingState HoldingState { get; }

        /// <summary>
        /// Gets the location of the touch, mouse, or pen/stylus contact.
        /// </summary>
        public Point Position { get; }

        /// <summary>
        /// Gets the pointer type of the input source.
        /// </summary>
        public PointerType PointerType { get; }

        internal PointerEventArgs? PointerEventArgs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoldingRoutedEventArgs"/> class.
        /// </summary>
        public HoldingRoutedEventArgs(HoldingState holdingState, Point position, PointerType pointerType) : base(Gestures.HoldingEvent)
        {
            HoldingState = holdingState;
            Position = position;
            PointerType = pointerType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoldingRoutedEventArgs"/> class.
        /// </summary>
        internal HoldingRoutedEventArgs(HoldingState holdingState, Point position, PointerType pointerType, PointerEventArgs pointerEventArgs) : this(holdingState, position, pointerType)
        {
            PointerEventArgs = pointerEventArgs;
        }
    }

    public enum HoldingState
    {
        /// <summary>
        /// A single contact has been detected and a time threshold is crossed without the contact being lifted, another contact detected, or another gesture started.
        /// </summary>
        Started,

        /// <summary>
        /// The single contact is lifted.
        /// </summary>
        Completed,

        /// <summary>
        /// An additional contact is detected or a subsequent gesture (such as a slide) is detected.
        /// </summary>
        Cancelled,
    }
}
