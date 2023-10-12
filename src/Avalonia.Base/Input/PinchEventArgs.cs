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

        public PinchEventArgs(double scale, Point scaleOrigin, double degree) : base(Gestures.PinchEvent)
        {
            Scale = scale;
            ScaleOrigin = scaleOrigin;
            Angle = degree;
        }

        public double Scale { get; } = 1;

        public Point ScaleOrigin { get; }

        /// <summary>
        /// Pinch angle in degrees
        /// </summary>
        public double Angle { get; }
    }

    public class PinchEndedEventArgs : RoutedEventArgs
    {
        public PinchEndedEventArgs() : base(Gestures.PinchEndedEvent)
        {
        }
    }
}
