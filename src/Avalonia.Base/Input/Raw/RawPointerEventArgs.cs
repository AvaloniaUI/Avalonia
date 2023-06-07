using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    public enum RawPointerEventType
    {
        LeaveWindow,
        LeftButtonDown,
        LeftButtonUp,
        RightButtonDown,
        RightButtonUp,
        MiddleButtonDown,
        MiddleButtonUp,
        XButton1Down,
        XButton1Up,
        XButton2Down,
        XButton2Up,
        Move,
        Wheel,
        NonClientLeftButtonDown,
        TouchBegin,
        TouchUpdate,
        TouchEnd,
        TouchCancel,
        Magnify,
        Rotate,
        Swipe
    }

    /// <summary>
    /// A raw mouse event.
    /// </summary>
    [PrivateApi]
    public class RawPointerEventArgs : RawInputEventArgs
    {
        private RawPointerPoint _point;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RawPointerEventArgs"/> class.
        /// </summary>
        /// <param name="device">The associated device.</param>
        /// <param name="timestamp">The event timestamp.</param>
        /// <param name="root">The root from which the event originates.</param>
        /// <param name="type">The type of the event.</param>
        /// <param name="position">The mouse position, in client DIPs.</param>
        /// <param name="inputModifiers">The input modifiers.</param>
        public RawPointerEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawPointerEventType type,
            Point position, 
            RawInputModifiers inputModifiers)
            : base(device, timestamp, root)
        {
            Point = new RawPointerPoint();
            Position = position;
            Type = type;
            InputModifiers = inputModifiers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawPointerEventArgs"/> class.
        /// </summary>
        /// <param name="device">The associated device.</param>
        /// <param name="timestamp">The event timestamp.</param>
        /// <param name="root">The root from which the event originates.</param>
        /// <param name="type">The type of the event.</param>
        /// <param name="point">The point properties and position, in client DIPs.</param>
        /// <param name="inputModifiers">The input modifiers.</param>
        public RawPointerEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawPointerEventType type,
            RawPointerPoint point, 
            RawInputModifiers inputModifiers)
            : base(device, timestamp, root)
        {
            Point = point;
            Type = type;
            InputModifiers = inputModifiers;
        }

        /// <summary>
        /// Gets the raw pointer identifier.
        /// </summary>
        public long RawPointerId { get; set; }

        /// <summary>
        /// Gets the pointer properties and position, in client DIPs.
        /// </summary>
        public RawPointerPoint Point
        {
            get => _point;
            set => _point = value;
        }

        /// <summary>
        /// Gets the mouse position, in client DIPs.
        /// </summary>
        public Point Position
        {
            get => _point.Position;
            set => _point.Position = value;
        }

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        public RawPointerEventType Type { get; set; }

        /// <summary>
        /// Gets the input modifiers.
        /// </summary>
        public RawInputModifiers InputModifiers { get; set; }
        
        /// <summary>
        /// Points that were traversed by a pointer since the previous relevant event,
        /// only valid for Move and TouchUpdate
        /// </summary>
        public Lazy<IReadOnlyList<RawPointerPoint>?>? IntermediatePoints { get; set; }

        internal IInputElement? InputHitTestResult { get; set; }
    }

    [PrivateApi]
    public record struct RawPointerPoint
    {
        /// <summary>
        /// Pointer position, in client DIPs.
        /// </summary>
        public Point Position { get; set; }

        /// <inheritdoc cref="PointerPointProperties.Twist" />
        public float Twist { get; set; }
        /// <inheritdoc cref="PointerPointProperties.Pressure" />
        public float Pressure { get; set; }
        /// <inheritdoc cref="PointerPointProperties.XTilt" />
        public float XTilt { get; set; }
        /// <inheritdoc cref="PointerPointProperties.YTilt" />
        public float YTilt { get; set; }


        public RawPointerPoint()
        {
            this = default;
            Pressure = 0.5f;
        }
    }
}
