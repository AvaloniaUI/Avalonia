using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class PinchEventArgs : RoutedEventArgs
    {
        public PinchEventArgs(double scale, Point scaleOrigin) : base(Gestures.PinchEvent)
        {
            Scale = scale;
            ScaleOrigin = scaleOrigin;
        }

        public PinchEventArgs(double scale, Point scaleOrigin, double angle, double angleDelta) : base(Gestures.PinchEvent)
        {
            Scale = scale;
            ScaleOrigin = scaleOrigin;
            Angle = angle;
            AngleDelta = angleDelta;
        }

        public double Scale { get; } = 1;

        public Point ScaleOrigin { get; }

        /// <summary>
        /// Gets the angle of the pinch gesture, in degrees.
        /// </summary>
        /// <remarks>
        /// A pinch gesture is the movement of two pressed points closer together. This property is the measured angle of the line between those two points. Remember zero degrees is a line pointing up.
        /// </remarks>
        public double Angle { get; }

        /// <summary>
        /// Gets the difference from the previous and current pinch angle.
        /// </summary>
        /// <remarks>
        /// The AngleDelta value includes the sign of rotation. Positive for clockwise, negative counterclockwise.
        /// </remarks>
        public double AngleDelta { get; }
    }

    public class PinchEndedEventArgs : RoutedEventArgs
    {
        public PinchEndedEventArgs() : base(Gestures.PinchEndedEvent)
        {
        }
    }
}
