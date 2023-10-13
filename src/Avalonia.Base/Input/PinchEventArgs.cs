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

        public PinchEventArgs(double scale, Point scaleOrigin, double angle) : base(Gestures.PinchEvent)
        {
            Scale = scale;
            ScaleOrigin = scaleOrigin;
            Angle = angle;
        }

        public double Scale { get; } = 1;

        public Point ScaleOrigin { get; }

        /// <summary>
        /// Gets the angle of the pinch gesture, in degrees.
        /// <summary>
        /// <remarks>
        /// A pinch gesture is the movement of two pressed points closer together. This property is the measured angle of the line between those two points. Remember zero degrees is a line pointing up.
        /// </remarks>
        public double Angle { get; }
    }

    public class PinchEndedEventArgs : RoutedEventArgs
    {
        public PinchEndedEventArgs() : base(Gestures.PinchEndedEvent)
        {
        }
    }
}
