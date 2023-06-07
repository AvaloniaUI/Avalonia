using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class PinchEventArgs : RoutedEventArgs
    {
        public PinchEventArgs(double scale, Point scaleOrigin) :  base(Gestures.PinchEvent)
        {
            Scale = scale;
            ScaleOrigin = scaleOrigin;
        }

        public double Scale { get; } = 1;

        public Point ScaleOrigin { get; }
    }

    public class PinchEndedEventArgs : RoutedEventArgs
    {
        public PinchEndedEventArgs() :  base(Gestures.PinchEndedEvent)
        {
        }
    }
}
